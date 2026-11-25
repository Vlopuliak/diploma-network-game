using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    // Disposable training-sandbox orchestration only. Caller owns the coroutine and must Dispose on shutdown.
    public sealed class CopyAttentionRecipeCoordinator : IDisposable
    {
        private readonly CopyAttentionTrainingAgent _agent;
        private readonly CopyDecisionInputV2 _input;
        private readonly NetworkObject _a, _b;
        private readonly Behaviour[] _controls;
        private bool[] _controlStates;
        private bool _terminal, _cancelled, _subscribed;
        private float? _terminalReward;
        private IDisposable _worldState;
        private string _output;
        public string State { get; private set; } = "Created";
        public string AbortReason { get; private set; }
        public string CurrentCaseId { get; private set; }
        public int PreparedCases { get; private set; }
        public int RecordedCases { get; private set; }
        public int PlannedCases { get; private set; }
        public event Action<string> CaseRequested;
        public event Action<string> CasePreparing;
        public event Action<string> RunAborted;

        public CopyAttentionRecipeCoordinator(CopyAttentionTrainingAgent agent, NetworkObject playerA, NetworkObject playerB,
            Behaviour[] sandboxControls)
        {
            _agent = agent != null ? agent : throw new ArgumentNullException(nameof(agent));
            _input = agent.GetComponent<CopyDecisionInputV2>();
            _a = playerA; _b = playerB;
            _controls = sandboxControls ?? Array.Empty<Behaviour>();
        }

        public IEnumerator Run(string planDirectory, string outputDirectory, int policySeed, bool diagnosticFirstPerFamily = false,
            string schedulePath = null)
        {
            if (State != "Created") throw new InvalidOperationException("Coordinator runs are single-use");
            State = "Running";
            IEnumerator execution = null;
            try
            {
                // Handle setup/MoveNext failures outside yield points so every failure produces an aborted run.
                try { execution = Execute(planDirectory, outputDirectory, policySeed, diagnosticFirstPerFamily, schedulePath); }
                catch (Exception error) { Abort(error.Message); }
                while (State == "Running")
                {
                    bool next = false;
                    object yielded = null;
                    try { next = execution.MoveNext(); if (next) yielded = execution.Current; }
                    catch (Exception error) { Abort(error.Message); }
                    if (State != "Running" || !next) break;
                    yield return yielded;
                }
            }
            finally
            {
                (execution as IDisposable)?.Dispose();
                if (State == "Running") Abort("coordinator_coroutine_disposed");
                Cleanup();
                WriteSummary();
            }
        }

        private IEnumerator Execute(string planDirectory, string outputDirectory, int policySeed, bool diagnostic, string schedulePath)
        {
            if (!Application.isBatchMode || !_agent.IsTrainingActive || !_input.IsServer || !_input.IsSpawned ||
                _a == null || _b == null || !_a.IsSpawned || !_b.IsSpawned || _a == _b ||
                _a.NetworkManager != _input.NetworkManager || _b.NetworkManager != _input.NetworkManager ||
                !_a.IsPlayerObject || !_b.IsPlayerObject || _a.OwnerClientId >= _b.OwnerClientId ||
                _input.NetworkManager.ConnectedClients.Count != 2 || policySeed < 0)
                throw new InvalidOperationException("Expected active server training actor and two distinct sandbox players");
            string plan = InLogs(planDirectory), output = InLogs(outputDirectory);
            if (Directory.Exists(output)) throw new IOException("Never overwrite a training run");
            string recipePath = Path.Combine(plan, "train-recipes.json");
            string manifestPath = Path.Combine(plan, "train-manifest.json");
            var audit = JObject.Parse(File.ReadAllText(Path.Combine(plan, "generation-audit.json")));
            foreach (string path in new[] { recipePath, manifestPath })
                if (Hash(path) != (string)audit["artifact_sha256"][Path.GetFileName(path)])
                    throw new InvalidOperationException("Frozen train artifact hash changed");
            var bundle = JObject.Parse(File.ReadAllText(recipePath));
            var originalManifest = JObject.Parse(File.ReadAllText(manifestPath));
            var all = ((JArray)bundle["cases"]).Cast<JObject>().ToArray();
            string[] families = { "quiet", "sound-live", "sound-memory", "light", "player-a", "player-b", "both-players", "competition" };
            if ((string)bundle["split"] != "train" || (int)bundle["generation_seed"] != 1001 || all.Length != 449 ||
                (string)bundle["dataset_id"] != "the-copy.attention-cases/1" ||
                (string)bundle["input_schema"] != CopyDecisionEncoderV2.SchemaId ||
                (string)bundle["profile_id"] != CopyAttentionReward.ProfileId ||
                (string)bundle["protocol_id"] != "the-copy.expert-attention-evaluation/1" ||
                (string)originalManifest["schema"] != "the-copy.attention-case-manifest/1" ||
                (string)originalManifest["split"] != "train" ||
                (string)originalManifest["dataset_id"] != (string)bundle["dataset_id"] ||
                all.Select(c => (string)c["case_id"]).Distinct().Count() != all.Length ||
                !families.All(f => all.Count(c => (string)c["scenario"] == f) == (f == "quiet" ? 1 : 64)) ||
                !JToken.DeepEquals(originalManifest["cases"], new JArray(all.Select(c => new JObject
                    { ["case_id"] = c["case_id"], ["scenario"] = c["scenario"] }))))
                throw new InvalidOperationException("Wrong train bundle/manifest contract");
            var selected = diagnostic ? families.Select(f => all.First(c => (string)c["scenario"] == f)).ToArray() : all;
            JObject schedule = null;
            if (schedulePath != null)
            {
                schedulePath = InLogs(schedulePath);
                schedule = JObject.Parse(File.ReadAllText(schedulePath));
                selected = Schedule(schedule, selected, recipePath, manifestPath, policySeed, diagnostic);
            }
            string split = diagnostic ? "diagnostic" : "train";
            string dataset = schedule != null ? "the-copy.attention-training-schedule/1" :
                diagnostic ? "the-copy.attention-coordinator-smoke/1" : (string)bundle["dataset_id"];
            string run = "training-" + Guid.NewGuid().ToString("N");
            PlannedCases = selected.Length;
            _output = output;
            Directory.CreateDirectory(_output);
            File.WriteAllText(Path.Combine(_output, "manifest.json"), JsonConvert.SerializeObject(new
            {
                schema = "the-copy.attention-case-manifest/1", dataset_id = dataset, split,
                cases = selected.Select(c => new { case_id = (string)c["case_id"], scenario = (string)c["scenario"] }).ToArray()
            }, Formatting.Indented));
            File.WriteAllText(Path.Combine(_output, "run-context.json"), JsonConvert.SerializeObject(new
            {
                source_split = "train", source_recipe_sha256 = Hash(recipePath), source_manifest_sha256 = Hash(manifestPath),
                profile_id = CopyAttentionReward.ProfileId, protocol_id = "the-copy.expert-attention-evaluation/1",
                input_schema = CopyDecisionEncoderV2.SchemaId, generation_seed = 1001, policy_seed = policySeed,
                selection = schedule != null ? "scheduled-train" : diagnostic ? "first-per-family-diagnostic-not-a-research-subset" : "all-frozen-train",
                schedule_sha256 = schedulePath == null ? null : Hash(schedulePath),
                shuffle_seed = (int?)schedule?["shuffle_seed"], epochs = schedule == null ? 1 : (int)schedule["epochs"],
                mode = _agent.IsControlledTest ? "controlled-heuristic-api-test" : "external-communicator",
                optimizer_status = "not_observed_by_unity", ood_vectors_read = false
            }, Formatting.Indented));
            _worldState = new WorldState(_input, _a, _b);
            _controlStates = _controls.Select(c => c != null && c.enabled).ToArray();
            foreach (var control in _controls) if (control != null) control.enabled = false;
            _agent.EpisodeFinished += OnTerminal;
            _subscribed = true;
            foreach (var recipe in selected)
            {
                if (_cancelled || !_agent.IsTrainingActive) throw new InvalidOperationException(AbortReason ?? "training_actor_unavailable");
                CurrentCaseId = (string)recipe["case_id"];
                CasePreparing?.Invoke(CurrentCaseId);
                float delay = ApplyRecipe(recipe);
                double started = Time.timeAsDouble, deadline = Time.realtimeSinceStartupAsDouble + 10;
                while (Time.timeAsDouble - started < delay)
                {
                    if (_cancelled || Time.realtimeSinceStartupAsDouble > deadline) throw new InvalidOperationException("case_preparation_cancelled_or_timed_out");
                    yield return null;
                }
                _terminal = false; _terminalReward = null;
                if (!_agent.TryBeginPreparedEpisode() || !_agent.TryCopyPendingFrame(out var observations, out var mask))
                    throw new InvalidOperationException("case_capture_failed");
                try { ValidatePresence(recipe, observations); }
                catch (Exception error)
                {
                    var sight = _input.GetComponent<CopyPlayerPerception>();
                    throw new InvalidOperationException(error.Message + " sightSpawned=" + sight.IsSpawned + " sightServer=" + sight.IsServer +
                        " sightEnabled=" + sight.isActiveAndEnabled + " a=" + _a.transform.position + " npc=" + _input.transform.position +
                        " observeA=" + sight.TryObserve(_a.OwnerClientId, out _));
                }
                PreparedCases++;
                CaseRequested?.Invoke(CurrentCaseId);
                deadline = Time.realtimeSinceStartupAsDouble + 20;
                while (!_terminal)
                {
                    if (_cancelled || Time.realtimeSinceStartupAsDouble > deadline) throw new InvalidOperationException("decision_cancelled_or_timed_out");
                    yield return null;
                }
                if (!_terminalReward.HasValue || _agent.LastTermination != "decision")
                    throw new InvalidOperationException("technical_interruption:" + _agent.LastTermination);
                int? action = _agent.LastSelectedAction;
                if (!CopyAttentionReward.TryEvaluate(observations, mask, action ?? int.MinValue, out float expectedReward) ||
                    Math.Abs(expectedReward - _terminalReward.Value) > 0.000001f)
                    throw new InvalidOperationException("reward_frame_mismatch");
                bool allowed = action.HasValue && action.Value >= 0 && action.Value < 7 && mask[action.Value];
                object target = _agent.LastSelectedTarget.HasValue ? new
                {
                    source_client_id = _agent.LastSelectedTarget.Value.SourceClientId,
                    position = new[] { _agent.LastSelectedTarget.Value.Position.x, _agent.LastSelectedTarget.Value.Position.y, _agent.LastSelectedTarget.Value.Position.z }
                } : null;
                File.AppendAllText(Path.Combine(_output, "records.jsonl"), JsonConvert.SerializeObject(new
                {
                    schema = "the-copy.attention-evaluation/1", input_schema = CopyDecisionEncoderV2.SchemaId,
                    profile_id = CopyAttentionReward.ProfileId, dataset_id = dataset, split, run_id = run,
                    policy_id = _agent.IsControlledTest ? "controlled-coordinator-api" : "communicator-policy",
                    seed = policySeed, generation_seed = 1001, case_id = CurrentCaseId, scenario = (string)recipe["scenario"], observations, allowed_actions = mask,
                    source_case_id = (string)(recipe["source_case_id"] ?? recipe["case_id"]), epoch = (int?)recipe["epoch"] ?? 0,
                    action, target, status = allowed ? "accepted" : "rejected",
                    rejection_reason = allowed ? null : action.HasValue ? "masked_or_unavailable" : "malformed",
                    latency_ms = (object)null, reference_outcomes = (object)null, decision_outcome = "decision",
                    assigned_reward = _terminalReward, cumulative_reward = _agent.LastEpisodeCumulativeReward
                }) + Environment.NewLine);
                RecordedCases++;
                yield return null; // EndEpisode/reset/reentrancy guard must finish before preparing another case.
            }
            if (_cancelled) throw new InvalidOperationException(AbortReason ?? "cancelled_at_completion");
            State = "Completed";
        }

        private static JObject[] Schedule(JObject schedule, JObject[] selected, string recipes, string manifest, int seed, bool diagnostic)
        {
            int epochs = (int)schedule["epochs"];
            if ((string)schedule["schema"] != "the-copy.attention-training-schedule/1" ||
                (string)schedule["source_split"] != "train" || (int)schedule["generation_seed"] != 1001 ||
                (int)schedule["policy_seed"] != seed || (int)schedule["shuffle_seed"] < 0 || epochs < 1 || epochs > 10000 ||
                (string)schedule["source_recipe_sha256"] != Hash(recipes) || (string)schedule["source_manifest_sha256"] != Hash(manifest) ||
                (string)schedule["selection"] != (diagnostic ? "first-per-family-diagnostic" : "all-frozen-train"))
                throw new InvalidOperationException("Invalid train schedule contract");
            var entries = ((JArray)schedule["cases"]).Cast<JObject>().ToArray();
            if (entries.Length != selected.Length * epochs) throw new InvalidOperationException("Incomplete epoch schedule");
            var source = selected.ToDictionary(c => (string)c["case_id"]);
            var result = new JObject[entries.Length];
            for (int epoch = 0; epoch < epochs; epoch++)
            {
                var seen = new System.Collections.Generic.HashSet<string>();
                for (int index = 0; index < selected.Length; index++)
                {
                    int slot = epoch * selected.Length + index;
                    var entry = entries[slot]; string id = (string)entry["source_case_id"];
                    if (id == null || !source.TryGetValue(id, out var recipe) || !seen.Add(id) || (int)entry["epoch"] != epoch ||
                        (string)entry["case_id"] != $"epoch/{epoch:D4}/{id}" || (string)entry["scenario"] != (string)recipe["scenario"])
                        throw new InvalidOperationException("Invalid/duplicate/out-of-split epoch recipe");
                    result[slot] = (JObject)recipe.DeepClone();
                    result[slot]["case_id"] = entry["case_id"]; result[slot]["source_case_id"] = id; result[slot]["epoch"] = epoch;
                }
            }
            return result;
        }

        private float ApplyRecipe(JObject recipe)
        {
            string family = (string)recipe["scenario"];
            var hearing = _input.GetComponent<CopyHearing>();
            var light = _input.GetComponent<CopyLightPerception>();
            var sight = _input.GetComponent<CopyPlayerPerception>();
            var player = _a.GetComponent<PlayerController>();
            var flashlight = _a.GetComponentInChildren<Light>();
            if (hearing == null || light == null || sight == null || player == null || flashlight == null)
                throw new InvalidOperationException("Missing training world bindings");
            hearing.enabled = false; hearing.enabled = true;
            light.enabled = family == "light" || family == "competition";
            sight.enabled = family == "player-a" || family == "player-b" || family == "both-players" || family == "competition";
            player.IsFlashlightOn.Value = light.enabled;
            _input.transform.SetPositionAndRotation(new Vector3(1000, 0, 1000), Quaternion.identity);
            var eye = _input.transform.position + Vector3.up * 0.6f;
            Place(_a, eye - Vector3.forward * 8); Place(_b, eye - Vector3.forward * 9);
            if (recipe["player_a_distance"] != null) Place(_a, eye + Direction(Number(recipe, "player_a_angle")) * Number(recipe, "player_a_distance"));
            if (recipe["player_b_distance"] != null) Place(_b, eye + Direction(Number(recipe, "player_b_angle")) * Number(recipe, "player_b_distance"));
            if (light.enabled)
            {
                var origin = eye + Direction(Number(recipe, "player_a_angle")) * Number(recipe, "light_range_fraction") * flashlight.range;
                _a.transform.position += origin - flashlight.transform.position;
                flashlight.transform.rotation = Quaternion.LookRotation(eye - flashlight.transform.position);
            }
            Physics.SyncTransforms();
            if (recipe["sound_loudness"] != null && !CopyPlayerNoise.TryPublish(_input.NetworkManager, _b.OwnerClientId,
                    TheCopy.Shared.NoiseSource.PlayerFootstep, Number(recipe, "sound_loudness")))
                throw new InvalidOperationException("Sound recipe rejected");
            float delay = family == "sound-memory" ? Number(recipe, "delay_fraction") * hearing.MemoryLifetime : 0;
            if (family == "sound-memory" && !(delay > hearing.LiveWindow && delay < hearing.MemoryLifetime))
                throw new InvalidOperationException("Infeasible memory delay");
            return delay;
        }

        private static void ValidatePresence(JObject recipe, float[] v)
        {
            string f = (string)recipe["scenario"];
            bool sound = f == "sound-live" || f == "sound-memory" || f == "competition";
            if (v[0] != (sound && f != "sound-memory" ? 1 : 0) || v[12] != (sound ? 1 : 0) ||
                v[24] != (f == "light" || f == "competition" ? 1 : 0) || v[30] != 0 ||
                v[40] != (f == "player-a" || f == "both-players" || f == "competition" ? 1 : 0) ||
                v[45] != (f == "player-b" || f == "both-players" || f == "competition" ? 1 : 0))
                throw new InvalidOperationException("Recipe sensor state mismatch: [live,memory,light,A,B,approx]=" +
                    string.Join(",", new[] { v[0], v[12], v[24], v[40], v[45], v[30] }));
        }

        private void OnTerminal(float? reward) { _terminal = true; _terminalReward = reward; }
        public void Cancel(string reason = "coordinator_cancelled")
        {
            if (State != "Running") return;
            _cancelled = true; AbortReason = reason;
            _agent.InterruptPending(reason);
        }
        private void Abort(string reason)
        {
            if (State != "Running") return;
            AbortReason = reason; State = "Aborted";
            _agent.InterruptPending(reason);
            _agent.DeactivateTraining();
            RunAborted?.Invoke(reason);
        }
        private void Cleanup()
        {
            if (_subscribed) _agent.EpisodeFinished -= OnTerminal;
            _subscribed = false;
            _worldState?.Dispose(); _worldState = null;
            if (_controlStates != null)
                for (int i = 0; i < _controls.Length; i++) if (_controls[i] != null) _controls[i].enabled = _controlStates[i];
        }
        private void WriteSummary()
        {
            if (_output == null) return;
            File.WriteAllText(Path.Combine(_output, "summary.json"), JsonConvert.SerializeObject(new
            { state = State, valid_run = State == "Completed", abort_reason = AbortReason, current_case_id = CurrentCaseId,
                planned_cases = PlannedCases, prepared_cases = PreparedCases, recorded_cases = RecordedCases,
                ood_vectors_read = false }, Formatting.Indented));
        }
        public void Dispose()
        {
            if (State == "Running") { Cancel("coordinator_disposed"); Abort("coordinator_disposed"); }
            Cleanup(); WriteSummary();
        }
        private static string InLogs(string path)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/AI-NPC")) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(path);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Training artifacts must stay in sandbox Logs/AI-NPC");
            return full;
        }
        private static string Hash(string path)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        private static float Number(JObject recipe, string key)
        { float value = (float)recipe[key]; if (!float.IsFinite(value)) throw new InvalidOperationException("Non-finite recipe parameter"); return value; }
        private static Vector3 Direction(float angle) => Quaternion.Euler(0, angle, 0) * Vector3.forward;
        private static void Place(NetworkObject obj, Vector3 position)
        { var body = obj.GetComponent<CapsuleCollider>(); obj.transform.position += position - body.transform.TransformPoint(body.center); }

        private sealed class WorldState : IDisposable
        {
            private readonly CopyDecisionInputV2 _input;
            private readonly NetworkObject _a, _b;
            private readonly Vector3 _npc, _pa, _pb;
            private readonly Quaternion _rotation, _aim;
            private readonly bool _hearing, _sight, _light, _flash;
            public WorldState(CopyDecisionInputV2 input, NetworkObject a, NetworkObject b)
            {
                _input = input; _a = a; _b = b;
                _npc = input.transform.position; _rotation = input.transform.rotation; _pa = a.transform.position; _pb = b.transform.position;
                _aim = a.GetComponentInChildren<Light>().transform.rotation;
                _hearing = input.GetComponent<CopyHearing>().enabled; _sight = input.GetComponent<CopyPlayerPerception>().enabled;
                _light = input.GetComponent<CopyLightPerception>().enabled; _flash = a.GetComponent<PlayerController>().IsFlashlightOn.Value;
            }
            public void Dispose()
            {
                if (_input == null || _a == null || _b == null) return;
                _input.transform.SetPositionAndRotation(_npc, _rotation); _a.transform.position = _pa; _b.transform.position = _pb;
                _a.GetComponentInChildren<Light>().transform.rotation = _aim;
                _a.GetComponent<PlayerController>().IsFlashlightOn.Value = _flash;
                _input.GetComponent<CopyHearing>().enabled = false;
                _input.GetComponent<CopyHearing>().enabled = _hearing;
                _input.GetComponent<CopyPlayerPerception>().enabled = _sight; _input.GetComponent<CopyLightPerception>().enabled = _light;
                Physics.SyncTransforms();
            }
        }
    }
}
