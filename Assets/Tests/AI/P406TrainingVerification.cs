#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheCopy.AI;
using TheCopy.Shared;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace TheCopy.Tests
{
    internal static class P406TrainingVerification
    {
        public static bool Requested => Environment.GetCommandLineArgs().Contains("-p406TrainingAdapter");
        private static int _passed;
        internal const string PrefabPath = "Assets/P305Generated/P406TrainingActor.prefab";

        public static void Prepare()
        {
            var actor = CreateActor();
            PrefabUtility.SaveAsPrefabAsset(actor, PrefabPath);
            UnityEngine.Object.DestroyImmediate(actor);
        }

        private static GameObject CreateActor()
        {
            var actor = new GameObject("P4-06 training actor");
            actor.SetActive(false);
            actor.AddComponent<CopyDecisionInputV2>();
            actor.AddComponent<CopyHearing>();
            actor.AddComponent<CopyLightPerception>().enabled = false;
            actor.GetComponent<CopyPlayerPerception>().enabled = false;
            var behavior = actor.AddComponent<BehaviorParameters>();
            behavior.BehaviorName = "TheCopyDraft2";
            behavior.BrainParameters.VectorObservationSize = 50;
            behavior.BrainParameters.NumStackedVectorObservations = 1;
            behavior.BrainParameters.ActionSpec = new ActionSpec(0, new[] { 7 });
            behavior.BehaviorType = BehaviorType.HeuristicOnly;
            actor.AddComponent<CopyAttentionTrainingAgent>().MaxStep = 0;
            return actor;
        }
        public static IEnumerator Verify(NetworkManager host, NetworkManager client)
        {
            _passed = 0;
            VerifyRewardParity();
            VerifyNegativeFrames();
            yield return VerifyEpisodes(host, client);
            Debug.Log("[P4-06 ADAPTER] PASS_COUNT=" + _passed);
            Debug.Log("[P4-06 ADAPTER] COMPLETE: reward parity and controlled episode lifecycle, no optimizer");
        }

        private static void VerifyRewardParity()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/AI-NPC/P406RewardParity.json"));
            var data = JObject.Parse(File.ReadAllText(path));
            Check((string)data["profile_id"] == CopyAttentionReward.ProfileId &&
                (string)data["input_schema"] == CopyDecisionEncoderV2.SchemaId && (int)data["frame_count"] == 674 &&
                (int)data["training_steps"] == 0 && !(bool)data["ood_vectors_read"], "audited Python fixture identifies current profile and train/ID only");
            foreach (var frame in (JArray)data["frames"])
            {
                float[] values = frame["observations"].ToObject<float[]>();
                bool[] mask = frame["allowed_actions"].ToObject<bool[]>();
                double[] expected = frame["expected_rewards"].ToObject<double[]>();
                for (int action = 0; action < 7; action++)
                    Check(CopyAttentionReward.TryEvaluate(values, mask, action, out float reward) &&
                        Math.Abs(reward - expected[action]) <= 0.000001, (string)frame["case_id"] + " parity " + action);
            }
        }

        private static void VerifyNegativeFrames()
        {
            var values = new float[50];
            var mask = new[] { true, false, false, false, false, false, false };
            Check(CopyAttentionReward.TryEvaluate(values, mask, 0, out float reward) && reward == 1, "quiet Continue reward");
            Check(CopyAttentionReward.TryEvaluate(values, mask, 7, out reward) && reward == -1, "invalid action is minus one on a valid frame");
            Check(!CopyAttentionReward.TryEvaluate(null, mask, 0, out _), "null data is rejected rather than penalized");
            Check(!CopyAttentionReward.TryEvaluate(new float[49], mask, 0, out _), "wrong vector width rejected");
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -0.1f, 1.1f })
            {
                values[1] = invalid;
                Check(!CopyAttentionReward.TryEvaluate(values, mask, 5, out _), "invalid data precedes invalid choice");
            }
            Array.Clear(values, 0, values.Length);
            values[0] = values[12] = values[24] = values[2] = values[7] = values[19] = 1;
            values[1] = 0.9f; values[13] = 0.2f; values[14] = 0.01f; values[25] = 0.8f;
            mask[1] = mask[2] = true;
            Check(CopyAttentionReward.TryEvaluate(values, mask, 1, out reward) && Math.Abs(reward - 0.25f) < 0.000001,
                "memory wins over stronger live and freshness is not multiplied twice");
            values[30] = 1;
            Check(!CopyAttentionReward.TryEvaluate(values, mask, 2, out _), "approximate light cannot be allowed");
            mask[2] = false;
            Check(CopyAttentionReward.TryEvaluate(values, mask, 2, out reward) && reward == -1, "approximate light remains an unavailable choice");
        }

        private sealed class MaskProbe : IDiscreteActionMask
        {
            public readonly bool[] Allowed = new bool[7];
            public void SetActionEnabled(int branch, int actionIndex, bool isEnabled)
            {
                if (branch != 0) throw new InvalidOperationException("Unexpected action branch");
                Allowed[actionIndex] = isEnabled;
            }
        }

        private static IEnumerator VerifyEpisodes(NetworkManager host, NetworkManager client)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var actor = UnityEngine.Object.Instantiate(prefab);
            var input = actor.GetComponent<CopyDecisionInputV2>();
            var hearing = actor.GetComponent<CopyHearing>();
            var behavior = actor.GetComponent<BehaviorParameters>();
            var agent = actor.GetComponent<CopyAttentionTrainingAgent>();
            actor.SetActive(true);
            Check(!agent.enabled && !agent.IsTrainingActive && !agent.TryActivateTraining(true), "unspawned training actor never initializes");
            var net = input.NetworkObject;
            net.SpawnWithObservers = true;
            net.Spawn();
            bool oldStepping = Academy.Instance.AutomaticSteppingEnabled;
            Academy.Instance.AutomaticSteppingEnabled = false;
            try
            {
                double deadline = Time.realtimeSinceStartupAsDouble + 5;
                while (!client.SpawnManager.SpawnedObjects.ContainsKey(net.NetworkObjectId) && Time.realtimeSinceStartupAsDouble < deadline)
                    yield return null;
                Check(client.SpawnManager.SpawnedObjects.ContainsKey(net.NetworkObjectId), "training prefab reaches remote replica before ownership tests");
                var replica = client.SpawnManager.SpawnedObjects[net.NetworkObjectId].GetComponent<CopyAttentionTrainingAgent>();
                replica.gameObject.SetActive(true);
                Check(!replica.enabled && !replica.IsTrainingActive && !replica.TryActivateTraining(true), "client training replica never initializes or activates");
                behavior.BehaviorType = BehaviorType.Default;
                Check(!agent.TryActivateTraining(), "Default mode refuses absent Python communicator instead of heuristic fallback");
                behavior.BehaviorType = BehaviorType.HeuristicOnly;
                net.ChangeOwnership(client.LocalClientId);
                Check(!agent.TryActivateTraining(true), "client-owned training actor denied");
                net.RemoveOwnership();
                behavior.BrainParameters.VectorObservationSize = 49;
                Check(!agent.TryActivateTraining(true), "wrong training shape denied");
                behavior.BrainParameters.VectorObservationSize = 50;
                Check(agent.TryActivateTraining(true), "explicit controlled heuristic fixture activates on server");
                Academy.Instance.EnvironmentStep();
                Check(agent.ResetCount > 0, "ML-Agents initialization calls episode reset");
                actor.transform.position = new Vector3(1000, 0, 1000);
                Check(agent.TryBeginPreparedEpisode() && !agent.TryBeginPreparedEpisode(), "one immutable prepared request at a time");
                var mask = new MaskProbe();
                agent.WriteDiscreteActionMask(mask);
                Check(mask.Allowed.SequenceEqual(new[] { true, false, false, false, false, false, false }), "quiet training mask closes all sensory and mechanism actions");
                var sensor = new VectorSensor(50);
                agent.CollectObservations(sensor);
                var stored = (System.Collections.ICollection)typeof(VectorSensor)
                    .GetMethod("GetObservations", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(sensor, null);
                Check(stored.Count == 50, "training sends exactly 50 observations");
                int before = agent.RewardedEpisodes;
                agent.OnActionReceived(Discrete(0));
                Check(agent.RewardedEpisodes == before + 1 && agent.LastAssignedReward == 1 &&
                    agent.LastEpisodeCumulativeReward == 1 && !agent.HasPendingDecision, "quiet choice assigns one reward and ends one episode");
                agent.OnActionReceived(Discrete(0));
                Check(agent.RewardedEpisodes == before + 1, "duplicate callback cannot assign reward twice");
                Academy.Instance.EnvironmentStep();
                int resets = agent.ResetCount;
                Check(agent.TryBeginPreparedEpisode(), "next independent episode can be prepared");
                agent.OnActionReceived(Discrete(5));
                Check(agent.LastAssignedReward == -1 && agent.LastEpisodeCumulativeReward == -1 &&
                    agent.ResetCount == resets + 1, "masked choice gets minus one without carrying previous reward");
                Academy.Instance.EnvironmentStep();
                hearing.OnNoise(new NoiseEvent { Position = actor.transform.position + Vector3.forward, Loudness = 0.8f, Source = NoiseSource.Environmental });
                Check(hearing.TryGetRememberedSound(out _), "real sound memory seeded through sensor");
                Check(agent.TryBeginPreparedEpisode(), "sound episode captures real sensors");
                agent.WriteDiscreteActionMask(mask);
                Check(mask.Allowed.SequenceEqual(new[] { true, true, false, false, false, false, false }), "sound training mask shares the prepared capture");
                agent.OnActionReceived(Discrete(1));
                Check(agent.LastAssignedReward == 1 && !hearing.TryGetRememberedSound(out _), "sound choice earns reward and reset clears actual sensor memory");
                Academy.Instance.EnvironmentStep();
                Check(agent.TryBeginPreparedEpisode(), "cancellation frame requested");
                int interrupted = agent.InterruptedEpisodes;
                agent.InterruptPending("fixture_cancel");
                agent.InterruptPending("duplicate_cancel");
                Check(agent.InterruptedEpisodes == interrupted + 1 && agent.LastAssignedReward == null &&
                    agent.LastEpisodeCumulativeReward == 0, "technical cancellation has no reward and only one terminal interruption");
                Academy.Instance.EnvironmentStep();
                Check(agent.TryBeginPreparedEpisode(), "external reset frame requested");
                bool callbackRequest = false;
                Action<float?> onReset = reward => { if (!reward.HasValue) callbackRequest = agent.TryBeginPreparedEpisode(); };
                interrupted = agent.InterruptedEpisodes;
                agent.EpisodeFinished += onReset;
                agent.OnEpisodeBegin(); // Controlled external Academy-reset callback, not a policy result.
                agent.EpisodeFinished -= onReset;
                Check(!callbackRequest && agent.InterruptedEpisodes == interrupted + 1 && agent.LastAssignedReward == null &&
                    agent.LastTermination == "academy_reset", "external reset interrupts once and cannot reenter preparation from terminal callback");
                Academy.Instance.EnvironmentStep();
                Check(agent.TryBeginPreparedEpisode(), "stale callback frame requested");
                yield return new WaitForSeconds(0.6f);
                agent.OnActionReceived(Discrete(0));
                Check(agent.LastAssignedReward == null && agent.LastTermination == "stale", "stale response interrupts without an assigned reward");
                Academy.Instance.EnvironmentStep();
                Check(agent.TryBeginPreparedEpisode(), "authority-loss frame requested");
                net.ChangeOwnership(client.LocalClientId);
                agent.OnActionReceived(Discrete(0));
                Check(agent.LastAssignedReward == null && !agent.HasPendingDecision, "lost authority cannot award selection reward");
                net.RemoveOwnership();
                Academy.Instance.EnvironmentStep();
                agent.DeactivateTraining();
                Check(!agent.IsTrainingActive && !agent.enabled && !agent.TryBeginPreparedEpisode(), "deactivation prevents any further training decisions");
            }
            finally
            {
                agent.DeactivateTraining();
                if (net.IsSpawned) net.Despawn(false);
                UnityEngine.Object.Destroy(actor);
                Academy.Instance.AutomaticSteppingEnabled = oldStepping;
            }
        }

        private static ActionBuffers Discrete(int action) => new ActionBuffers(ActionSegment<float>.Empty, new ActionSegment<int>(new[] { action }));
        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            _passed++;
        }
    }
}
#endif
