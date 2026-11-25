#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheCopy.AI;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace TheCopy.Tests
{
    internal static class P406CoordinatorVerification
    {
        public static bool Bootstrap => Environment.GetCommandLineArgs().Contains("-p406TrainingBootstrap");
        public static bool Communicator => Bootstrap || Environment.GetCommandLineArgs().Contains("-p406CommunicatorSmoke");
        public static bool Requested => Communicator || Environment.GetCommandLineArgs().Contains("-p406Coordinator");
        private static int _passed;
        private static void Unlock() { EditorApplication.UnlockReloadAssemblies(); EditorApplication.quitting -= Unlock; }

        public static IEnumerator Verify(NetworkManager host, NetworkManager client)
        {
            EditorApplication.LockReloadAssemblies();
            EditorApplication.quitting += Unlock;
            var args = Environment.GetCommandLineArgs();
            bool diagnostic = Bootstrap ? args.Contains("-p406BootstrapDiagnostic") : Communicator || args.Contains("-p406CoordinatorDiagnostic");
            int arg = Array.IndexOf(args, "-p406CoordinatorPlan");
            if (arg < 0 || arg + 1 >= args.Length) throw new InvalidOperationException("Missing frozen train plan");
            string plan = Path.GetFullPath(args[arg + 1]);
            string schedulePath = null, signalPath = null;
            int seed = Communicator ? 2026 : 0;
            int expected = diagnostic ? 8 : 449;
            if (Bootstrap)
            {
                schedulePath = SandboxArgument(args, "-p406TrainingSchedule");
                signalPath = SandboxArgument(args, "-p406RunSignal");
                var schedule = JObject.Parse(File.ReadAllText(schedulePath));
                seed = (int)schedule["policy_seed"];
                expected = ((JArray)schedule["cases"]).Count;
                Signal(signalPath, "Running", null, 0, 0);
            }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/AI-NPC/p406-coordinator-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(root);
            Debug.Log("[P4-06 COORDINATOR] RECORDS_DIR=" + root);
            _passed = 0;
            var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(P406TrainingVerification.PrefabPath));
            actor.GetComponent<BehaviorParameters>().BehaviorType = Communicator ? BehaviorType.Default : BehaviorType.HeuristicOnly;
            actor.SetActive(true);
            var input = actor.GetComponent<CopyDecisionInputV2>();
            input.NetworkObject.SpawnWithObservers = false;
            input.NetworkObject.Spawn();
            var agent = actor.GetComponent<CopyAttentionTrainingAgent>();
            bool oldAuto = Academy.Instance.AutomaticSteppingEnabled;
            int oldRate = Application.targetFrameRate;
            var controls = UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None)
                .Where(b => b is PlayerController || b is AudioListener || b.GetType().Name == "VoiceNoiseEmitter").ToArray();
            // Two NetworkManagers share one Physics scene in this fixture, unlike real Host/Client processes.
            var clientColliders = client.SpawnManager.SpawnedObjects.Values
                .SelectMany(obj => obj.GetComponentsInChildren<Collider>(true)).Distinct().ToArray();
            var colliderStates = clientColliders.Select(c => c.enabled).ToArray();
            CopyAttentionRecipeCoordinator coordinator = null;
            try
            {
                Application.targetFrameRate = 60;
                foreach (var collider in clientColliders) collider.enabled = false;
                Check(clientColliders.Length > 0 && clientColliders.All(c => !c.enabled), "client-manager collider replicas cannot occlude server training sensors");
                Academy.Instance.AutomaticSteppingEnabled = false;
                Check(agent.TryActivateTraining(!Communicator), "training actor activated in explicit smoke mode");
                Academy.Instance.EnvironmentStep();
                if (Bootstrap)
                {
                    int rewarded = agent.RewardedEpisodes;
                    // Exchange Python's initial reset using a terminal-only notification, never an extra rewarded decision.
                    agent.EpisodeInterrupted();
                    Academy.Instance.EnvironmentStep();
                    Check(agent.RewardedEpisodes == rewarded && !agent.HasPendingDecision, "bootstrap reset adds no off-manifest rewarded episode");
                }
                else if (Communicator)
                {
                    Check(agent.TryBeginPreparedEpisode(), "bootstrap request exchanges initial Python reset before the run manifest");
                    Academy.Instance.EnvironmentStep();
                    if (agent.HasPendingDecision) agent.InterruptPending("communicator_bootstrap");
                    Academy.Instance.EnvironmentStep();
                }
                coordinator = new CopyAttentionRecipeCoordinator(agent, host.LocalClient.PlayerObject,
                    host.ConnectedClients[client.LocalClientId].PlayerObject, controls);
                if (Bootstrap)
                {
                    coordinator.RunAborted += reason => Signal(signalPath, "Aborted", reason, coordinator.PreparedCases, coordinator.RecordedCases);
                }
                bool copied = false;
                coordinator.CaseRequested += _ =>
                {
                    if (copied) return;
                    copied = true;
                    Check(agent.TryCopyPendingFrame(out var frame, out var mask), "coordinator has captured request frame");
                    frame[0] = -99; mask[5] = true;
                    Check(agent.TryCopyPendingFrame(out var stable, out var stableMask) && stable[0] != -99 && !stableMask[5],
                        "training frame/mask copies cannot mutate the request");
                };
                if (Bootstrap && args.Contains("-p406InjectTechnicalFailure"))
                    coordinator.CaseRequested += _ => agent.InterruptPending("injected_bootstrap_failure");
                var run = coordinator.Run(plan, Path.Combine(root, "complete"), seed, diagnostic, schedulePath);
                while (run.MoveNext())
                {
                    if (agent.HasPendingDecision) Academy.Instance.EnvironmentStep();
                    yield return run.Current;
                }
                Academy.Instance.EnvironmentStep(); // Flush the final terminal transition to Python before teardown.
                Check(coordinator.State == "Completed" && coordinator.RecordedCases == expected,
                    "all selected frozen train cases completed without optimizer");
                Check(coordinator.RecordedCases == coordinator.PreparedCases, "exactly one terminal record per prepared case");
                Check(File.ReadAllLines(Path.Combine(root, "complete/records.jsonl")).Length == coordinator.RecordedCases,
                    "JSONL matches the predeclared manifest count");
                if (Bootstrap) Signal(signalPath, "Completed", null, coordinator.PreparedCases, coordinator.RecordedCases);
                coordinator.Dispose(); coordinator = null;
                if (!Communicator)
                {
                    int resets = agent.ResetCount;
                    coordinator = new CopyAttentionRecipeCoordinator(agent, host.LocalClient.PlayerObject,
                        host.ConnectedClients[client.LocalClientId].PlayerObject, controls);
                    coordinator.CaseRequested += _ => agent.InterruptPending("injected_technical_failure");
                    run = coordinator.Run(plan, Path.Combine(root, "technical-failure"), 0, true);
                    while (run.MoveNext()) yield return run.Current;
                    Check(coordinator.State == "Aborted" && coordinator.PreparedCases == 1 && coordinator.RecordedCases == 0,
                        "technical failure stops before the next case and has no fabricated reward record");
                    Check(!agent.IsTrainingActive && agent.ResetCount > resets, "aborted run deactivates training after interruption");
                    Check(!(bool)JObject.Parse(File.ReadAllText(Path.Combine(root, "technical-failure/summary.json")))["valid_run"],
                        "technical run persisted as invalid");
                    coordinator.Dispose(); coordinator = null;
                    Check(agent.TryActivateTraining(true), "explicit fresh actor activation for capture-failure test");
                    coordinator = new CopyAttentionRecipeCoordinator(agent, host.LocalClient.PlayerObject,
                        host.ConnectedClients[client.LocalClientId].PlayerObject, controls);
                    coordinator.CasePreparing += _ => input.enabled = false;
                    run = coordinator.Run(plan, Path.Combine(root, "capture-failure"), 0, true);
                    while (run.MoveNext()) yield return run.Current;
                    Check(coordinator.State == "Aborted" && coordinator.PreparedCases == 0 && coordinator.RecordedCases == 0,
                        "capture failure aborts without training on a replacement zero vector");
                    input.enabled = true;
                    coordinator.Dispose(); coordinator = null;
                }
                Debug.Log("[P4-06 COORDINATOR] PASS_COUNT=" + _passed);
                Debug.Log("[P4-06 COORDINATOR] COMPLETE: " + (args.Contains("-p406ExternalTrainer") ? "external trainer run" : Bootstrap ? "training bootstrap without optimizer" : Communicator ? "communicator smoke" : "controlled train coordinator"));
            }
            finally
            {
                coordinator?.Dispose();
                agent.DeactivateTraining();
                if (input.NetworkObject.IsSpawned) input.NetworkObject.Despawn(false);
                UnityEngine.Object.Destroy(actor);
                Academy.Instance.AutomaticSteppingEnabled = oldAuto;
                for (int i = 0; i < clientColliders.Length; i++) if (clientColliders[i] != null) clientColliders[i].enabled = colliderStates[i];
                Application.targetFrameRate = oldRate;
            }
        }
        private static string SandboxArgument(string[] args, string key)
        {
            int index = Array.IndexOf(args, key);
            if (index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("Missing " + key);
            string path = Path.GetFullPath(args[index + 1]);
            string allowed = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/AI-NPC")) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe bootstrap path");
            return path;
        }
        private static void Signal(string path, string state, string reason, int prepared, int recorded)
        {
            // Atomic replacement prevents Python from observing a half-written abort notification.
            string temp = path + ".tmp";
            File.WriteAllText(temp, new JObject { ["state"] = state, ["reason"] = reason, ["prepared"] = prepared, ["recorded"] = recorded }.ToString());
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
                    break;
                }
                catch (IOException) when (attempt < 24) { System.Threading.Thread.Sleep(2); }
                catch (UnauthorizedAccessException) when (attempt < 24) { System.Threading.Thread.Sleep(2); }
            }
        }
        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            _passed++;
        }
    }
}
#endif
