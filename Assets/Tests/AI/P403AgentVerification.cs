#if UNITY_EDITOR
using System;
using System.Collections;
using TheCopy.AI;
using Unity.InferenceEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace TheCopy.Tests
{
    internal static class P403AgentVerification
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), a => a == "-p403AgentInference");
        public static void Prepare(GameObject npc)
        {
            npc.AddComponent<CopyDecisionAgentHost>();
            var behavior = npc.GetComponent<BehaviorParameters>();
            behavior.BrainParameters.VectorObservationSize = 50;
            behavior.BrainParameters.NumStackedVectorObservations = 1;
            behavior.BrainParameters.ActionSpec = new ActionSpec(0, new[] { 7 });
            behavior.BehaviorName = "TheCopyDraft2";
            behavior.BehaviorType = BehaviorType.InferenceOnly;
            behavior.InferenceDevice = InferenceDevice.Burst;
            behavior.Model = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/P305SmokeModel.onnx");
            if (behavior.Model == null) throw new InvalidOperationException("Missing generated untrained smoke model");
            npc.GetComponent<CopyDecisionAgent>().enabled = false;
        }

        public static IEnumerator Verify(NetworkManager host, NetworkManager client, CopyHearing npc)
        {
            VerifyInputSignatures();
            var agent = npc.GetComponent<CopyDecisionAgent>();
            var driver = npc.GetComponent<CopyDecisionAgentHost>();
            var replica = client.SpawnManager.SpawnedObjects[npc.NetworkObjectId];
            var clientAgent = replica.GetComponent<CopyDecisionAgent>();
            Check(agent.IsInitializedOnServer && agent.enabled && agent.HasValidConfiguration, "server Agent initializes with 50 inputs and branch seven");
            Check(!clientAgent.enabled && !clientAgent.IsInitializedOnServer && !replica.GetComponent<CopyDecisionAgentHost>().TryRequestDecision(),
                "client replica never initializes model or requests a decision");
            Check(npc.GetComponent<BehaviorParameters>().InferenceDevice == InferenceDevice.Burst &&
                npc.GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly, "actual model inference uses CPU without heuristic fallback");
            var behavior = npc.GetComponent<BehaviorParameters>();
            var originalModel = behavior.Model;
            var badModel = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/P305WrongWidthModel.onnx");
            Check(badModel != null, "valid 27-input negative ONNX fixture imported");
            driver.enabled = false;
            behavior.Model = badModel;
            Check(!agent.HasValidConfiguration && agent.ModelValidationIssue.Contains("obs_0"),
                "actual 27-input asset rejected despite 50-input BehaviorParameters");
            driver.enabled = true;
            Check(!agent.enabled && !agent.IsInitializedOnServer && !driver.TryRequestDecision(),
                "incompatible model never initializes an Agent or schedules a forward");
            driver.enabled = false;
            behavior.Model = originalModel;
            driver.enabled = true;
            Check(agent.IsInitializedOnServer && agent.HasValidConfiguration, "valid model restores server initialization");
            Academy.Instance.AutomaticSteppingEnabled = false;
            Academy.Instance.EnvironmentStep();
            var sight = npc.GetComponent<CopyPlayerPerception>();
            var light = npc.GetComponent<CopyLightPerception>();
            Vector3 oldNpc = npc.transform.position;
            Quaternion oldRotation = npc.transform.rotation;
            bool sightEnabled = sight.enabled, lightEnabled = light.enabled;
            npc.enabled = false;
            light.enabled = false;
            sight.enabled = false;
            int eventCount = 0;
            CopyDecisionAction last = default;
            Action<CopyDecisionAction, CopyDecisionTarget?> received = (action, target) => { eventCount++; last = action; };
            agent.DecisionAccepted += received;
            try
            {
                Check(driver.TryRequestDecision() && !driver.TryRequestDecision(), "one outstanding request holds one observation and mask frame");
                Academy.Instance.EnvironmentStep();
                Check(eventCount == 1 && last == CopyDecisionAction.Continue && !agent.HasPendingDecision,
                    "untrained ONNX CPU forward returns the only unmasked Continue action");
                int accepted = agent.AcceptedDecisions;
                for (int i = 0; i < 8; i++)
                {
                    Check(driver.TryRequestDecision(), "repeat CPU inference request " + i);
                    Academy.Instance.EnvironmentStep();
                }
                Check(agent.AcceptedDecisions == accepted + 8 && last == CopyDecisionAction.Continue,
                    "repeated neural forwards respect all unavailable-action masks");
                driver.TryRequestDecision();
                int discarded = agent.DiscardedDecisions;
                agent.OnActionReceived(Discrete(5)); // Controlled rejection test, not an inference result.
                Check(agent.DiscardedDecisions == discarded + 1 && agent.AcceptedDecisions == accepted + 8,
                    "forged masked mechanism action is rejected");
                Academy.Instance.EnvironmentStep();

                npc.transform.SetPositionAndRotation(new Vector3(1000, 0, 1000), Quaternion.identity);
                var player = host.LocalClient.PlayerObject;
                Vector3 oldPlayer = player.transform.position;
                var body = player.GetComponent<CapsuleCollider>();
                player.transform.position += npc.transform.position + new Vector3(0, 0.6f, 3) - body.transform.TransformPoint(body.center);
                Physics.SyncTransforms();
                sight.enabled = true;
                try
                {
                    Check(driver.TryRequestDecision(), "visible-player frame requested");
                    player.transform.position += Vector3.back * 10;
                    Physics.SyncTransforms();
                    discarded = agent.DiscardedDecisions;
                    agent.OnActionReceived(Discrete(3));
                    Check(agent.DiscardedDecisions == discarded + 1, "player lost after request invalidates old response target");
                    Academy.Instance.EnvironmentStep();
                }
                finally { player.transform.position = oldPlayer; }
                sight.enabled = false;
                Check(driver.TryRequestDecision(), "expiry frame requested");
                yield return new WaitForSeconds(0.6f);
                discarded = agent.DiscardedDecisions;
                agent.OnActionReceived(Discrete(0));
                Check(agent.DiscardedDecisions == discarded + 1, "expired neural response cannot publish an intent");
                Academy.Instance.EnvironmentStep();
                driver.enabled = false;
                Check(!agent.enabled && !agent.IsInitializedOnServer && !driver.TryRequestDecision(), "disabled host driver disposes model and refuses requests");
                driver.enabled = true;
                Check(agent.IsInitializedOnServer && driver.TryRequestDecision(), "server Agent can reinitialize after driver returns");
                Academy.Instance.EnvironmentStep();
                Check(!agent.HasPendingDecision, "reinitialized CPU model finishes a request");
            }
            finally
            {
                agent.DecisionAccepted -= received;
                npc.transform.SetPositionAndRotation(oldNpc, oldRotation);
                sight.enabled = sightEnabled;
                light.enabled = lightEnabled;
                npc.enabled = true;
                Academy.Instance.AutomaticSteppingEnabled = true;
                Physics.SyncTransforms();
            }
            Debug.Log("[P4-03 AGENT] COMPLETE: untrained neural CPU inference only");
        }

        internal static void VerifyDespawned(CopyHearing npc)
        {
            var agent = npc.GetComponent<CopyDecisionAgent>();
            Check(!agent.enabled && !agent.IsInitializedOnServer && !agent.HasPendingDecision &&
                !npc.GetComponent<CopyDecisionAgentHost>().TryRequestDecision(), "NGO despawn disables Agent and invalidates pending frame");
        }
        private static ActionBuffers Discrete(int action) => new ActionBuffers(ActionSegment<float>.Empty,
            new ActionSegment<int>(new[] { action }));

        private static void VerifyInputSignatures()
        {
            Model Good()
            {
                var model = new Model();
                model.inputs.Add(new Model.Input { name = "obs_0", dataType = DataType.Float, shape = new DynamicTensorShape(-1, 50) });
                model.inputs.Add(new Model.Input { name = "action_masks", dataType = DataType.Float, shape = new DynamicTensorShape(-1, 7) });
                foreach (string name in new[] { "discrete_actions", "discrete_action_output_shape", "version_number", "memory_size" })
                    model.outputs.Add(new Model.Output { name = name });
                return model;
            }
            Check(!CopyDecisionModelValidator.TryValidate(null, out _), "null model signature rejected");
            Check(CopyDecisionModelValidator.TryValidate(Good(), out _), "dynamic-batch 50/7 float signature accepted");
            var model = Good();
            var input = model.inputs[0];
            input.shape = new DynamicTensorShape(1, 50); model.inputs[0] = input;
            Check(CopyDecisionModelValidator.TryValidate(model, out _), "static single-batch signature accepted");
            input.shape = new DynamicTensorShape(-1, 27); model.inputs[0] = input;
            Check(!CopyDecisionModelValidator.TryValidate(model, out _), "legacy observation width rejected");
            model = Good(); input = model.inputs[1]; input.shape = new DynamicTensorShape(-1, 6); model.inputs[1] = input;
            Check(!CopyDecisionModelValidator.TryValidate(model, out _), "wrong action-mask width rejected");
            model = Good(); input = model.inputs[0]; input.shape = new DynamicTensorShape(-1, -1); model.inputs[0] = input;
            Check(!CopyDecisionModelValidator.TryValidate(model, out _), "unknown observation width rejected");
            model = Good(); input = model.inputs[0]; input.dataType = DataType.Int; model.inputs[0] = input;
            Check(!CopyDecisionModelValidator.TryValidate(model, out _), "integer observations rejected");
            model = Good(); model.inputs.Add(model.inputs[0]);
            Check(!CopyDecisionModelValidator.TryValidate(model, out _), "extra input rejected");
            model = Good(); model.inputs[1] = model.inputs[0];
            Check(!CopyDecisionModelValidator.TryValidate(model, out _), "duplicate input name rejected");
            model = Good(); model.outputs.RemoveAt(0);
            Check(!CopyDecisionModelValidator.TryValidate(model, out _), "missing discrete output rejected");
            model = Good(); model.inputs.Reverse();
            Check(CopyDecisionModelValidator.TryValidate(model, out _), "named inputs are independent of input order");
        }
        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            Debug.Log("[P4-03 AGENT] PASS " + label);
        }
    }
}
#endif
