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
using UnityEditor;
using UnityEngine;

namespace TheCopy.Tests
{
    // Sensor-only dry run: no policy request, reward, optimizer or held-out OOD read.
    internal static class P405RecipeVerification
    {
        public static bool Requested => Environment.GetCommandLineArgs().Contains("-p405RecipePlan");
        private static int _checks;
        private static bool _reloadLocked;

        private static void ReleaseReloadLock()
        {
            if (_reloadLocked) EditorApplication.UnlockReloadAssemblies();
            _reloadLocked = false;
            EditorApplication.quitting -= ReleaseReloadLock;
        }

        public static IEnumerator Verify(NetworkManager host, NetworkManager client, CopyHearing npc)
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Recipe replay requires disposable batch Editor");
            // A deferred import/recompile can otherwise silently destroy the coroutine mid-manifest.
            // Keep the lock through the remaining fixture checks; this disposable Editor quits at the end.
            EditorApplication.LockReloadAssemblies();
            _reloadLocked = true;
            EditorApplication.quitting += ReleaseReloadLock;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-p405RecipePlan");
            if (index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("Missing recipe plan path");
            string plan = Path.GetFullPath(args[index + 1]);
            string logs = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs")) + Path.DirectorySeparatorChar;
            if (!plan.StartsWith(logs, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Recipes must be in disposable Logs");
            var input = npc.GetComponent<CopyDecisionInputV2>();
            var sight = npc.GetComponent<CopyPlayerPerception>();
            var light = npc.GetComponent<CopyLightPerception>();
            var driver = npc.GetComponent<CopyDecisionAgentHost>();
            var local = host.LocalClient.PlayerObject;
            var remote = host.ConnectedClients[client.LocalClientId].PlayerObject;
            var player = local.GetComponent<PlayerController>();
            var flashlight = local.GetComponentInChildren<Light>();
            Vector3 oldNpc = npc.transform.position, oldLocal = local.transform.position, oldRemote = remote.transform.position;
            Quaternion oldRotation = npc.transform.rotation, oldAim = flashlight.transform.rotation;
            bool oldFlashlight = player.IsFlashlightOn.Value, oldHearing = npc.enabled,
                oldSight = sight.enabled, oldLight = light.enabled, oldDriver = driver.enabled;
            bool oldStepping = Academy.Instance.AutomaticSteppingEnabled;
            float oldTimeScale = Time.timeScale;
            int oldFrameRate = Application.targetFrameRate;
            var controls = UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None)
                .Where(b => b is PlayerController || b is AudioListener || b.GetType().Name == "VoiceNoiseEmitter").ToArray();
            var enabledStates = controls.Select(b => b.enabled).ToArray();
            float lifetime = new SerializedObject(npc).FindProperty("memoryLifetime").floatValue;
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/AI-NPC/p405-replay-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(output);
            _checks = 0;
            try
            {
                driver.enabled = false;
                Time.timeScale = 1f;
                Application.targetFrameRate = 60;
                foreach (var control in controls) control.enabled = false;
                Academy.Instance.AutomaticSteppingEnabled = false;
                foreach (string split in new[] { "train", "id" }) // Never read OOD recipes in development replay.
                {
                    string recipeFile = Path.Combine(plan, split + "-recipes.json");
                    var bundle = JObject.Parse(File.ReadAllText(recipeFile));
                    var cases = (JArray)bundle["cases"];
                    Check((string)bundle["schema"] == "the-copy.attention-recipes/1" && (string)bundle["split"] == split,
                        "valid recipe bundle");
                    Check(cases.Count == (split == "train" ? 449 : 225), "approved case count");
                    File.Copy(recipeFile, Path.Combine(output, split + "-recipes.json"));
                    File.Copy(Path.Combine(plan, split + "-manifest.json"), Path.Combine(output, split + "-manifest.json"));
                    foreach (JObject recipe in cases)
                    {
                        string id = (string)recipe["case_id"], family = (string)recipe["scenario"];
                        npc.enabled = false;
                        npc.enabled = true;
                        sight.enabled = family == "player-a" || family == "player-b" || family == "both-players" || family == "competition";
                        light.enabled = family == "light" || family == "competition";
                        player.IsFlashlightOn.Value = light.enabled;
                        npc.transform.SetPositionAndRotation(new Vector3(1000, 0, 1000), Quaternion.identity);
                        Vector3 eye = npc.transform.position + Vector3.up * 0.6f;
                        Place(local, eye - Vector3.forward * 8f);
                        Place(remote, eye - Vector3.forward * 9f);
                        if (recipe["player_a_distance"] != null)
                            Place(local, eye + Direction((float)recipe["player_a_angle"]) * (float)recipe["player_a_distance"]);
                        if (recipe["player_b_distance"] != null)
                            Place(remote, eye + Direction((float)recipe["player_b_angle"]) * (float)recipe["player_b_distance"]);
                        if (light.enabled)
                        {
                            Vector3 origin = eye + Direction((float)recipe["player_a_angle"]) * (float)recipe["light_range_fraction"] * flashlight.range;
                            local.transform.position += origin - flashlight.transform.position;
                            flashlight.transform.rotation = Quaternion.LookRotation(eye - flashlight.transform.position);
                        }
                        Physics.SyncTransforms();
                        bool sound = recipe["sound_loudness"] != null;
                        if (sound) Check(CopyPlayerNoise.TryPublish(host, client.LocalClientId, NoiseSource.PlayerFootstep,
                            (float)recipe["sound_loudness"]), id + " sound accepted");
                        if (family == "sound-memory")
                        {
                            float delay = (float)recipe["delay_fraction"] * lifetime;
                            Check(delay > 0.18f && delay < lifetime, id + " memory delay feasible");
                            double started = Time.timeAsDouble;
                            double deadline = Time.realtimeSinceStartupAsDouble + 5;
                            while (Time.timeAsDouble - started < delay)
                            {
                                if (Time.realtimeSinceStartupAsDouble > deadline)
                                    throw new InvalidOperationException(id + " memory clock stalled, scale=" + Time.timeScale);
                                yield return null;
                            }
                        }
                        var observations = new float[50];
                        Check(input.TryCapture(observations, out var candidates), id + " capture");
                        Check(observations[0] == (sound && family != "sound-memory" ? 1 : 0) &&
                            observations[12] == (sound ? 1 : 0), id + " live/memory state");
                        Check(observations[24] == (light.enabled ? 1 : 0) && observations[30] == 0, id + " exact light state");
                        Check(observations[40] == (sight.enabled && family != "player-b" ? 1 : 0) &&
                            observations[45] == (sight.enabled && family != "player-a" ? 1 : 0), id + " visibility state");
                        bool[] mask = Enumerable.Range(0, 7).Select(a => candidates.IsAllowed((CopyDecisionAction)a)).ToArray();
                        Check(mask[0] && !mask[5] && !mask[6], id + " scope mask");
                        File.AppendAllText(Path.Combine(output, split + "-inputs.jsonl"), JsonConvert.SerializeObject(new
                        {
                            schema = "the-copy.attention-sensor-input/1", dataset_id = (string)bundle["dataset_id"],
                            input_schema = CopyDecisionEncoderV2.SchemaId, profile_id = (string)bundle["profile_id"],
                            protocol_id = (string)bundle["protocol_id"], split, generation_seed = (int)bundle["generation_seed"],
                            case_id = id, scenario = family, observations, allowed_actions = mask,
                            capture_purpose = "SENSOR_REPLAY_ONLY_NO_POLICY", memory_lifetime = lifetime, flashlight_range = flashlight.range
                        }) + Environment.NewLine);
                    }
                    using (var hash = SHA256.Create())
                        File.WriteAllText(Path.Combine(output, split + "-provenance.json"), JsonConvert.SerializeObject(new
                        {
                            recipe_sha256 = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(recipeFile))).Replace("-", "").ToLowerInvariant(),
                            unity = Application.unityVersion, captured_cases = cases.Count, model_requests = 0, training_steps = 0,
                            ood_read = false, memory_lifetime = lifetime, flashlight_range = flashlight.range
                        }, Formatting.Indented));
                    Debug.Log("[P4-05 RECIPES] CAPTURED " + split + "=" + cases.Count);
                }
                Debug.Log("[P4-05 RECIPES] PASS_COUNT=" + _checks);
                Debug.Log("[P4-05 RECIPES] RECORDS_DIR=" + output);
                Debug.Log("[P4-05 RECIPES] COMPLETE: train/ID sensor replay, no policy requests, OOD unopened");
            }
            finally
            {
                npc.transform.SetPositionAndRotation(oldNpc, oldRotation);
                local.transform.position = oldLocal; remote.transform.position = oldRemote;
                flashlight.transform.rotation = oldAim; player.IsFlashlightOn.Value = oldFlashlight;
                npc.enabled = oldHearing; sight.enabled = oldSight; light.enabled = oldLight;
                for (int i = 0; i < controls.Length; i++) if (controls[i] != null) controls[i].enabled = enabledStates[i];
                Academy.Instance.AutomaticSteppingEnabled = oldStepping;
                Time.timeScale = oldTimeScale;
                Application.targetFrameRate = oldFrameRate;
                driver.enabled = oldDriver;
                Physics.SyncTransforms();
            }
        }

        private static Vector3 Direction(float angle) => Quaternion.Euler(0, angle, 0) * Vector3.forward;
        private static void Place(NetworkObject player, Vector3 position)
        {
            var body = player.GetComponent<CapsuleCollider>();
            player.transform.position += position - body.transform.TransformPoint(body.center);
        }
        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            _checks++;
        }
    }
}
#endif
