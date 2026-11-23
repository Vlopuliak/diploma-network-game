using System;
using System.IO;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CopyDecisionAgent))]
    public sealed class CopyDecisionTraceRecorder : NetworkBehaviour
    {
        [SerializeField] private string outputDirectory;
        [SerializeField] private string fileName = "copy-intent-records.jsonl";
        [SerializeField] private string runId;
        [SerializeField] private string policyId;
        [SerializeField] private string scenario = "unspecified";
        [SerializeField] private string split = "diagnostic";
        [SerializeField, Min(0)] private int seed = 11;
        private CopyDecisionAgent _agent;
        private string _session;
        public int WrittenRecords { get; private set; }
        public string OutputPath { get; private set; }
        public string LastWriteError { get; private set; }

        public override void OnNetworkSpawn() => Subscribe();
        public override void OnNetworkDespawn()
        {
            if (_agent != null) _agent.CancelPendingDecision("despawned");
            Unsubscribe();
        }
        private void OnEnable() { if (IsSpawned) Subscribe(); }
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (!IsServer || !IsSpawned || !isActiveAndEnabled || _agent != null) return;
            if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(policyId) ||
                string.IsNullOrWhiteSpace(scenario) || string.IsNullOrWhiteSpace(fileName) ||
                seed < 0 ||
                (split != "train" && split != "id" && split != "ood" && split != "diagnostic"))
            { LastWriteError = "Invalid trace configuration"; enabled = false; return; }
            try
            {
                if (Path.GetFileName(fileName) != fileName) throw new ArgumentException("Use a filename without directories");
                string folder = string.IsNullOrWhiteSpace(outputDirectory)
                    ? Path.Combine(Application.persistentDataPath, "AI-NPC") : outputDirectory;
                OutputPath = Path.GetFullPath(Path.Combine(folder, fileName));
            }
            catch (Exception error) when (error is ArgumentException || error is NotSupportedException || error is IOException)
            { LastWriteError = error.Message; enabled = false; return; }
            _session = Guid.NewGuid().ToString("N");
            LastWriteError = null;
            _agent = GetComponent<CopyDecisionAgent>();
            _agent.DecisionCompleted += Write;
        }

        private void Unsubscribe()
        {
            if (_agent != null) _agent.DecisionCompleted -= Write;
            _agent = null;
        }

        private void Write(CopyDecisionTrace trace)
        {
            // Only a server subscribes. Keep terminal records while NGO is clearing spawn flags.
            if (_agent == null) return;
            var mask = new bool[CopyDecisionCandidatesV2.ActionCount];
            for (int i = 0; i < mask.Length; i++) mask[i] = trace.Candidates.IsAllowed((CopyDecisionAction)i);
            object target = trace.Target.HasValue ? new
            {
                source_client_id = trace.Target.Value.SourceClientId,
                position = new[] { trace.Target.Value.Position.x, trace.Target.Value.Position.y, trace.Target.Value.Position.z }
            } : null;
            try
            {
                var record = new
                {
                    schema = "the-copy.intent-evaluation/1", input_schema = CopyDecisionEncoderV2.SchemaId,
                    case_id = _session + ":" + trace.RequestId, scenario, run_id = runId, policy_id = policyId, split, seed,
                    allowed_actions = mask, action = trace.Action, target, status = trace.Status,
                    rejection_reason = trace.Reason, latency_ms = trace.LatencyMs, reference_outcomes = (object)null
                };
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.AppendAllText(OutputPath, JsonConvert.SerializeObject(record) + Environment.NewLine);
                WrittenRecords++;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                error is JsonException || error is ArgumentException || error is NotSupportedException)
            {
                LastWriteError = error.Message;
                Debug.LogWarning("[The Copy trace] Recording disabled: " + error.Message, this);
                enabled = false;
            }
        }
    }
}
