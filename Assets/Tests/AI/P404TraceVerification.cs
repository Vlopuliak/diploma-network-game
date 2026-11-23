#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using TheCopy.AI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace TheCopy.Tests
{
    internal static class P404TraceVerification
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-p404DecisionTrace");
        public static void Prepare(GameObject npc)
        {
            var recorder = npc.AddComponent<CopyDecisionTraceRecorder>();
            recorder.enabled = false;
            Configure(recorder, "p404-smoke-records.jsonl", "untrained-neural-smoke", "quiet-controlled-smoke");
        }

        public static void Verify(NetworkManager host, NetworkManager client, CopyHearing npc)
        {
            var recorder = npc.GetComponent<CopyDecisionTraceRecorder>();
            var replica = client.SpawnManager.SpawnedObjects[npc.NetworkObjectId].GetComponent<CopyDecisionTraceRecorder>();
            Check(!recorder.enabled && recorder.WrittenRecords == 0 && replica.WrittenRecords == 0,
                "recording is opt-in and previous controlled Agent tests were not logged");
            replica.enabled = true;
            Check(replica.OutputPath == null, "client recorder does not subscribe or create a file");
            replica.enabled = false;
            var agent = npc.GetComponent<CopyDecisionAgent>();
            var driver = npc.GetComponent<CopyDecisionAgentHost>();
            var sight = npc.GetComponent<CopyPlayerPerception>();
            var light = npc.GetComponent<CopyLightPerception>();
            bool wasHearing = npc.enabled, wasSight = sight.enabled, wasLight = light.enabled;
            Academy.Instance.AutomaticSteppingEnabled = false;
            Academy.Instance.EnvironmentStep();
            npc.enabled = false; sight.enabled = false; light.enabled = false;
            try
            {
                recorder.enabled = true;
                for (int i = 0; i < 3; i++)
                {
                    Check(driver.TryRequestDecision(), "real CPU trace request " + i);
                    Academy.Instance.EnvironmentStep();
                }
                Check(recorder.WrittenRecords == 3 && recorder.LastWriteError == null, "three actual neural completions written");
                string[] lines = File.ReadAllLines(recorder.OutputPath);
                Check(lines.Length == 3, "one JSONL record per real request");
                foreach (string line in lines)
                {
                    var row = JObject.Parse(line);
                    Check((string)row["status"] == "accepted" && (int)row["action"] == 0 &&
                        row["target"].Type == JTokenType.Null && (double)row["latency_ms"] >= 0 &&
                        row["reference_outcomes"].Type == JTokenType.Null && (bool)row["allowed_actions"][0] &&
                        !(bool)row["allowed_actions"][5] && !(bool)row["allowed_actions"][6],
                        "real trace carries initial mask, nullable target/reference and measured response latency");
                }
                Check((string)JObject.Parse(lines[0])["case_id"] != (string)JObject.Parse(lines[1])["case_id"],
                    "case identities remain unique");
                Debug.Log("[P4-04 TRACE] RECORDS_DIR=" + Path.GetDirectoryName(recorder.OutputPath));
                recorder.enabled = false;
                Check(driver.TryRequestDecision(), "unrecorded request after recorder disabled");
                Academy.Instance.EnvironmentStep();
                Check(File.ReadAllLines(recorder.OutputPath).Length == 3, "disabled recorder does not write or alter decisions");

                var ioSettings = new SerializedObject(recorder);
                string originalDirectory = ioSettings.FindProperty("outputDirectory").stringValue;
                string blockedDirectory = Path.Combine(originalDirectory, "path-is-a-file");
                File.WriteAllText(blockedDirectory, "controlled I/O failure fixture");
                ioSettings.FindProperty("outputDirectory").stringValue = blockedDirectory;
                ioSettings.ApplyModifiedPropertiesWithoutUndo();
                recorder.enabled = true;
                int beforeIo = agent.AcceptedDecisions;
                Check(driver.TryRequestDecision(), "I/O failure neural request scheduled");
                Academy.Instance.EnvironmentStep();
                Check(!recorder.enabled && recorder.LastWriteError != null && agent.AcceptedDecisions == beforeIo + 1,
                    "recorder I/O failure disables logging while neural decision is still accepted");
                ioSettings.Update();
                ioSettings.FindProperty("outputDirectory").stringValue = originalDirectory;
                ioSettings.ApplyModifiedPropertiesWithoutUndo();

                Configure(recorder, "p404-controlled-records.jsonl", "controlled-callback-fixture", "controlled-rejection");
                recorder.enabled = true;
                Check(driver.TryRequestDecision(), "controlled masked response requested");
                agent.OnActionReceived(new ActionBuffers(ActionSegment<float>.Empty, new ActionSegment<int>(new[] { 5 })));
                Academy.Instance.EnvironmentStep();
                Check(driver.TryRequestDecision(), "controlled cancellation requested");
                agent.CancelPendingDecision("controlled_test_cancellation");
                agent.CancelPendingDecision("duplicate_cancel");
                Academy.Instance.EnvironmentStep();
                var controlled = File.ReadAllLines(recorder.OutputPath);
                Check(controlled.Length == 2, "rejection and cancellation each produce one terminal record");
                var rejected = JObject.Parse(controlled[0]);
                var cancelled = JObject.Parse(controlled[1]);
                Check((string)rejected["status"] == "rejected" && (int)rejected["action"] == 5 &&
                    (string)rejected["rejection_reason"] == "masked_or_unavailable", "controlled masked rejection has explicit reason");
                Check((string)cancelled["status"] == "no_response" && cancelled["action"].Type == JTokenType.Null &&
                    cancelled["target"].Type == JTokenType.Null && cancelled["latency_ms"].Type == JTokenType.Null,
                    "cancelled request has no invented response action or latency");
            }
            finally
            {
                recorder.enabled = false;
                npc.enabled = wasHearing; sight.enabled = wasSight; light.enabled = wasLight;
                Academy.Instance.AutomaticSteppingEnabled = true;
            }
            Debug.Log("[P4-04 TRACE] COMPLETE: neural records separated from controlled callbacks");
        }

        internal static void PrepareDespawn(CopyHearing npc)
        {
            var recorder = npc.GetComponent<CopyDecisionTraceRecorder>();
            Configure(recorder, "p404-pending-despawn.jsonl", "controlled-lifecycle-fixture", "despawn");
            recorder.enabled = true;
            Check(npc.GetComponent<CopyDecisionAgentHost>().TryRequestDecision(), "pending request prepared before real NGO despawn");
        }

        internal static void VerifyDespawn(CopyHearing npc)
        {
            var recorder = npc.GetComponent<CopyDecisionTraceRecorder>();
            var lines = File.ReadAllLines(recorder.OutputPath);
            Check(lines.Length == 1 && (string)JObject.Parse(lines[0])["status"] == "no_response",
                "real NGO despawn records exactly one cancelled request after spawn flags clear");
        }

        private static void Configure(CopyDecisionTraceRecorder recorder, string file, string policy, string scenario)
        {
            var settings = new SerializedObject(recorder);
            var directory = settings.FindProperty("outputDirectory");
            if (string.IsNullOrWhiteSpace(directory.stringValue))
                directory.stringValue = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/AI-NPC/p404-records-" + Guid.NewGuid().ToString("N")));
            settings.FindProperty("fileName").stringValue = file;
            settings.FindProperty("runId").stringValue = "p404-" + System.Diagnostics.Process.GetCurrentProcess().Id;
            settings.FindProperty("policyId").stringValue = policy;
            settings.FindProperty("scenario").stringValue = scenario;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            Debug.Log("[P4-04 TRACE] PASS " + label);
        }
    }
}
#endif
