#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheCopy.AI;
using TheCopy.Shared;
using Unity.MLAgents;
using Unity.Netcode;
using UnityEngine;

namespace TheCopy.Tests
{
    // Diagnostic cases only. Never opens held-out OOD or starts a trainer.
    internal static class P405AttentionCaseVerification
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-p405AttentionCases");
        private const string Dataset = "the-copy.attention-diagnostic/1";
        private static readonly string[] Cases = { "quiet", "sound-live", "sound-memory", "light", "player-a",
            "player-b", "both-players", "competition" };

        public static IEnumerator Verify(NetworkManager host, NetworkManager client, CopyHearing npc)
        {
            var agent = npc.GetComponent<CopyDecisionAgent>();
            var input = npc.GetComponent<CopyDecisionInputV2>();
            var driver = npc.GetComponent<CopyDecisionAgentHost>();
            var sight = npc.GetComponent<CopyPlayerPerception>();
            var light = npc.GetComponent<CopyLightPerception>();
            var local = host.LocalClient.PlayerObject;
            var remote = host.ConnectedClients[client.LocalClientId].PlayerObject;
            var player = local.GetComponent<PlayerController>();
            var flashlight = local.GetComponentInChildren<Light>();
            var oldNpc = npc.transform.position;
            var oldRotation = npc.transform.rotation;
            var oldLocal = local.transform.position;
            var oldRemote = remote.transform.position;
            var oldAim = flashlight.transform.rotation;
            bool oldFlashlight = player.IsFlashlightOn.Value;
            bool oldHearing = npc.enabled, oldSight = sight.enabled, oldLight = light.enabled;
            bool oldStepping = Academy.Instance.AutomaticSteppingEnabled;
            // No live microphone packets or uncontrolled player input may contaminate the cases.
            var controls = UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None)
                .Where(b => b is PlayerController || b is AudioListener || b.GetType().Name == "VoiceNoiseEmitter").ToArray();
            var enabledStates = controls.Select(b => b.enabled).ToArray();
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/AI-NPC/p405-cases-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(folder);
            string recordsPath = Path.Combine(folder, "records.jsonl");
            // Manifest and provenance are persisted BEFORE the first request.
            File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonConvert.SerializeObject(new
            {
                schema = "the-copy.attention-case-manifest/1", dataset_id = Dataset, split = "diagnostic",
                cases = Cases.Select(id => new { case_id = id, scenario = id }).ToArray()
            }, Formatting.Indented));
            string modelPath = Path.Combine(Application.dataPath, "P305SmokeModel.onnx");
            using (var hash = SHA256.Create())
                File.WriteAllText(Path.Combine(folder, "provenance.json"), JsonConvert.SerializeObject(new
                {
                    purpose = "UNTRAINED_DIAGNOSTIC_ONLY", model_sha256 = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(modelPath))).Replace("-", "").ToLowerInvariant(),
                    training_steps = 0, unity = Application.unityVersion, dataset_id = Dataset,
                    profile_id = "the-copy.expert-attention/1", protocol_id = "the-copy.expert-attention-evaluation/1",
                    input_schema = CopyDecisionEncoderV2.SchemaId, case_count = Cases.Length,
                    geometry_origin = new[] { 1000, 0, 1000 }, network = "NGO Host/Client fixture"
                }, Formatting.Indented));
            var captured = new System.Collections.Generic.List<CopyDecisionTrace>();
            Action<CopyDecisionTrace> completed = trace => captured.Add(trace);
            string run = "p405-" + Guid.NewGuid().ToString("N");
            try
            {
                foreach (var control in controls) control.enabled = false;
                Academy.Instance.AutomaticSteppingEnabled = false;
                Academy.Instance.EnvironmentStep();
                agent.DecisionCompleted += completed;
                for (int i = 0; i < Cases.Length; i++)
                {
                    string id = Cases[i];
                    npc.enabled = false; // Public sensor lifecycle clears the previous case's memory.
                    npc.enabled = true;
                    sight.enabled = id == "player-a" || id == "player-b" || id == "both-players" || id == "competition";
                    light.enabled = id == "light" || id == "competition";
                    player.IsFlashlightOn.Value = light.enabled;
                    npc.transform.SetPositionAndRotation(new Vector3(1000, 0, 1000), Quaternion.identity);
                    Vector3 eye = npc.transform.position + Vector3.up * 0.6f;
                    Place(local, eye + new Vector3(-2, 0, id == "player-b" ? -4 : 4));
                    Place(remote, eye + new Vector3(2, 0, id == "player-a" ? -4 : 4));
                    flashlight.transform.rotation = Quaternion.LookRotation(eye - flashlight.transform.position);
                    Physics.SyncTransforms();
                    bool sound = id == "sound-live" || id == "sound-memory" || id == "competition";
                    if (sound)
                        Check(CopyPlayerNoise.TryPublish(host, client.LocalClientId, NoiseSource.PlayerFootstep, 0.8f),
                            id + " publishes identified sound through production NPC adapter");
                    if (id == "sound-memory") yield return new WaitForSeconds(0.25f);
                    var expected = new float[50];
                    Check(input.TryCapture(expected, out var candidates), id + " captures real sensor input");
                    Check(expected[0] == (sound && id != "sound-memory" ? 1f : 0f) && expected[12] == (sound ? 1f : 0f),
                        id + " has expected live/memory presence after reset");
                    Check(expected[24] == (light.enabled ? 1f : 0f) && expected[30] == 0,
                        id + " has expected exact light observation");
                    Check(expected[40] == (sight.enabled && id != "player-b" ? 1f : 0f) &&
                        expected[45] == (sight.enabled && id != "player-a" ? 1f : 0f), id + " has expected visible A/B");
                    int before = captured.Count;
                    Check(driver.TryRequestDecision(), id + " schedules actual neural forward");
                    Academy.Instance.EnvironmentStep();
                    Check(captured.Count == before + 1 && !agent.HasPendingDecision, id + " emits exactly one terminal trace");
                    var trace = captured[before];
                    var observations = trace.CopyObservations();
                    Check(observations.SequenceEqual(expected), id + " terminal trace retains the original request vector");
                    observations[0] = -123;
                    Check(trace.CopyObservations().SequenceEqual(expected), id + " caller cannot mutate stored observations");
                    var mask = Enumerable.Range(0, 7).Select(a => trace.Candidates.IsAllowed((CopyDecisionAction)a)).ToArray();
                    Check(Enumerable.Range(0, 7).All(a => mask[a] == candidates.IsAllowed((CopyDecisionAction)a)),
                        id + " mask belongs to the same request frame");
                    Check(trace.Status == "accepted" && trace.Action.HasValue && mask[trace.Action.Value],
                        id + " untrained inference selects an available intent without action execution");
                    object target = trace.Target.HasValue ? new
                    {
                        source_client_id = trace.Target.Value.SourceClientId,
                        position = new[] { trace.Target.Value.Position.x, trace.Target.Value.Position.y, trace.Target.Value.Position.z }
                    } : null;
                    File.AppendAllText(recordsPath, JsonConvert.SerializeObject(new
                    {
                        schema = "the-copy.attention-evaluation/1", input_schema = CopyDecisionEncoderV2.SchemaId,
                        profile_id = "the-copy.expert-attention/1", protocol_id = "the-copy.expert-attention-evaluation/1",
                        dataset_id = Dataset, case_id = id, scenario = id, run_id = run, policy_id = "untrained-neural-diagnostic",
                        split = "diagnostic", seed = 11, observations = trace.CopyObservations(), allowed_actions = mask,
                        action = trace.Action, target, status = trace.Status, rejection_reason = trace.Reason,
                        latency_ms = trace.LatencyMs, reference_outcomes = (object)null, decision_outcome = "decision"
                    }) + Environment.NewLine);
                }
                Check(File.ReadAllLines(recordsPath).Length == Cases.Length, "one record per declared diagnostic case");
                var previous = captured.Last();
                npc.enabled = false;
                sight.enabled = false;
                light.enabled = false;
                Check(previous.CopyObservations()[24] == 1f, "later sensor changes cannot overwrite the previous frame");
                var cancellationInput = new float[50];
                Check(input.TryCapture(cancellationInput, out _), "cancellation expected input captured");
                Check(driver.TryRequestDecision(), "cancellation capture request");
                agent.CancelPendingDecision("p405-copy-test");
                var cancelled = captured.Last();
                Check(cancelled.Status == "no_response" && cancelled.CopyObservations().SequenceEqual(cancellationInput),
                    "cancellation preserves exact request input before buffer reset");
                Academy.Instance.EnvironmentStep();
                Debug.Log("[P4-05 CASES] RECORDS_DIR=" + folder);
                Debug.Log("[P4-05 CASES] COMPLETE: real sensors to captured neural request to diagnostic JSONL");
            }
            finally
            {
                agent.DecisionCompleted -= completed;
                agent.CancelPendingDecision("p405-cleanup");
                Academy.Instance.AutomaticSteppingEnabled = oldStepping;
                npc.transform.SetPositionAndRotation(oldNpc, oldRotation);
                local.transform.position = oldLocal;
                remote.transform.position = oldRemote;
                flashlight.transform.rotation = oldAim;
                player.IsFlashlightOn.Value = oldFlashlight;
                npc.enabled = oldHearing; sight.enabled = oldSight; light.enabled = oldLight;
                for (int i = 0; i < controls.Length; i++) if (controls[i] != null) controls[i].enabled = enabledStates[i];
                Physics.SyncTransforms();
            }
        }

        private static void Place(NetworkObject player, Vector3 position)
        {
            var body = player.GetComponent<CapsuleCollider>();
            player.transform.position += position - body.transform.TransformPoint(body.center);
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            Debug.Log("[P4-05 CASES] PASS " + label);
        }
    }
}
#endif
