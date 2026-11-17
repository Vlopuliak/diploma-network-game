#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using TheCopy.AI;
using TheCopy.Networking;
using TheCopy.Shared;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheCopy.Tests
{
    public sealed class P306SceneVerification : MonoBehaviour
    {
        private const string SourceScene = "Assets/Scenes/Networking_Sandbox.unity";
        private const string RunKey = "TheCopy.P306.SceneRun";
        private const string ProbeName = "P306_PerceptionProbe";
        private bool _failed;
        private double _deadline;

        [MenuItem("Tools/The Copy/Create P3-06 verification scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Create the verification scene outside Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/AI/P306_Perception_Local.unity");
            if (!AssetDatabase.CopyAsset(SourceScene, path))
                throw new InvalidOperationException("Could not copy " + SourceScene);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var probe = new GameObject(ProbeName);
            probe.transform.position = Find<Transform>("SpawnPoint_2").position + Vector3.forward * 3f;
            var hearing = probe.AddComponent<CopyHearing>();
            var light = probe.AddComponent<CopyLightPerception>();
            var context = probe.AddComponent<CopySceneContext>();
            probe.AddComponent<CopyPerception>();
            if (P403DecisionVerification.Requested)
                probe.AddComponent<CopyDecisionInput>();
            ConfigureLayers(hearing);
            ConfigureLayers(light);
            var bindings = new SerializedObject(context);
            bindings.FindProperty("zone").objectReferenceValue = Find<NetworkTriggerZone>("Sector_TriggerZone");
            bindings.FindProperty("powerNode").objectReferenceValue = Find<NetworkInteractiveObject>("Interactive_Terminal");
            bindings.FindProperty("airlock").objectReferenceValue = Find<NetworkAirlockDoor>("Airlock_Door");
            bindings.FindProperty("keypad").objectReferenceValue = Find<NetworkKeypadTerminal>("Keypad_Terminal");
            bindings.ApplyModifiedPropertiesWithoutUndo();
            if (P307ObservationVerification.Requested)
                P307ObservationVerification.PrepareScene();
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save " + path);
            Selection.activeGameObject = probe;
            Debug.Log("[P3-06 SCENE] prepared " + path);
        }

        public static void Run()
        {
            if (!Application.isBatchMode || !Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-p305Disposable"))
                throw new InvalidOperationException("Use the disposable-project verification launcher.");
            CreateScene();
            SessionState.SetBool(RunKey, true);
            EditorApplication.EnterPlaymode();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartVerification()
        {
            if (!SessionState.GetBool(RunKey, false))
                return;
            SessionState.EraseBool(RunKey);
            // Only the automated local run bypasses Relay UI initialization.
            foreach (var bootstrap in FindObjectsByType<NetworkBootstrapUI>(FindObjectsSortMode.None))
                bootstrap.enabled = false;
            var runner = new GameObject("P3-06 Scene Verification").AddComponent<P306SceneVerification>();
            runner._deadline = Time.realtimeSinceStartupAsDouble + 40;
            Application.logMessageReceived += runner.TrackError;
            runner.StartCoroutine(runner.Verify());
        }

        private void TrackError(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            if (trace.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup"))
                return;
            _failed = true;
            Debug.Log("[P3-06 SCENE] FAIL " + message);
        }

        private void Update()
        {
            if (Time.realtimeSinceStartupAsDouble > _deadline && !_failed)
            {
                _failed = true;
                Debug.Log("[P3-06 SCENE] FAIL timeout");
            }
            if (_failed)
                EditorApplication.Exit(1);
        }

        private IEnumerator Verify()
        {
            var host = Find<NetworkManager>("NetworkManager");
            var transport = host.GetComponent<UnityTransport>();
            transport.UseWebSockets = true;
            transport.SetConnectionData("127.0.0.1", 0, "127.0.0.1");
            host.NetworkConfig.EnableSceneManagement = false;
            Expect(host.StartHost(), "original scene NetworkManager starts a local host");
            yield return Await(() => host.LocalClient.PlayerObject != null, "original player prefab spawned");
            var player = host.LocalClient.PlayerObject.GetComponent<PlayerController>();
            player.GetComponent<VoiceNoiseEmitter>().enabled = false;
            player.enabled = false;
            var probe = Find<CopyHearing>(ProbeName);
            var light = probe.GetComponent<CopyLightPerception>();
            var context = probe.GetComponent<CopySceneContext>();
            var power = Find<NetworkInteractiveObject>("Interactive_Terminal");
            var door = Find<NetworkAirlockDoor>("Airlock_Door");
            var keypad = Find<NetworkKeypadTerminal>("Keypad_Terminal");
            var zone = Find<NetworkTriggerZone>("Sector_TriggerZone");
            Expect(probe.IsSpawned && power.IsSpawned && door.IsSpawned && keypad.IsSpawned && zone.IsSpawned,
                "probe and actual in-scene mechanisms spawned");
            Expect(context.TryRead(out var initial) && initial.ZoneTriggered == false && initial.PowerOn == false &&
                initial.AirlockOpen == false && initial.KeypadUnlocked == false, "actual scene bindings read initial state");

            var beam = player.GetComponentInChildren<Light>();
            probe.transform.position = beam.transform.position + beam.transform.forward * 1.5f - Vector3.up * 0.6f;
            InvokeRpc(player, "ToggleFlashlightServerRpc");
            Physics.SyncTransforms();
            Expect(light.TryGetStrongestLight(out var illumination) && illumination.SourceClientId == host.LocalClientId &&
                !illumination.IsAimApproximate, "host flashlight detected in actual scene with exact local aim");

            var ownCollider = probe.gameObject.AddComponent<SphereCollider>();
            ownCollider.center = Vector3.up * 0.6f;
            ownCollider.radius = 0.2f;
            Physics.SyncTransforms();
            Expect(light.TryGetStrongestLight(out _), "NPC collider does not block its own light perception");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = beam.transform.position + beam.transform.forward * 0.75f;
            wall.transform.rotation = beam.transform.rotation;
            wall.transform.localScale = new Vector3(1, 1, 0.1f);
            Physics.SyncTransforms();
            Expect(!light.TryGetStrongestLight(out _), "Default-layer opaque wall blocks the beam");
            wall.SetActive(false);
            Physics.SyncTransforms();

            Expect(CopyPlayerNoise.TryPublish(host, host.LocalClientId, NoiseSource.PlayerVoice, 0.8f), "identified acoustic input accepted");
            Expect(probe.TryGetLiveSound(out var sound) && Mathf.Abs(sound.Strength - 0.8f *
                (1f - Vector3.Distance(probe.transform.position + Vector3.up * 0.6f, player.transform.position) / 18f)) < 0.001f,
                "source player and NPC colliders do not attenuate sound");

            player.transform.position = power.transform.position + Vector3.left;
            probe.transform.position = power.transform.position + Vector3.left * 2f;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.6f);
            power.Interact();
            yield return Await(() => context.TryRead(out var state) && state.PowerOn == true && state.AirlockOpen == true,
                "actual switch interaction opens linked airlock and updates context");
            Expect(probe.TryGetRememberedSound(out var mechanismNoise) &&
                (mechanismNoise.Source == NoiseSource.Environmental || mechanismNoise.Source == NoiseSource.PhysicalImpact),
                "actual mechanism emits a sound remembered by the NPC");

            int changes = 0;
            context.Changed += _ => changes++;
            InvokeRpc(keypad, "SubmitPinServerRpc", "wrong", default(ServerRpcParams));
            Expect(!keypad.IsUnlocked.Value, "wrong PIN keeps actual keypad locked");
            Expect(probe.TryGetLiveSound(out var alarm) && alarm.Source == NoiseSource.Environmental,
                "wrong PIN alarm reaches the hearing sensor");
            string pin = new SerializedObject(keypad).FindProperty("secretPinCode").stringValue;
            InvokeRpc(keypad, "SubmitPinServerRpc", pin, default(ServerRpcParams));
            yield return Await(() => context.TryRead(out var state) && state.KeypadUnlocked == true && changes > 0,
                "actual correct-PIN RPC updates context and emits a change");

            player.transform.position = zone.GetComponent<BoxCollider>().bounds.center;
            Physics.SyncTransforms();
            yield return Await(() => context.TryRead(out var state) && state.ZoneTriggered == true,
                "entering original sector updates scene context");
            if (P307ObservationVerification.Requested)
                yield return P307ObservationVerification.Verify(host, probe);
            if (P403DecisionVerification.Requested)
                P403DecisionVerification.Verify(host, probe);
            host.Shutdown();
            yield return Await(() => !host.IsListening, "scene host stops");
            Application.logMessageReceived -= TrackError;
            Debug.Log("[P3-06 SCENE] COMPLETE: actual scene host integration");
            EditorApplication.Exit(_failed ? 1 : 0);
        }

        private static void ConfigureLayers(Component sensor)
        {
            var settings = new SerializedObject(sensor);
            settings.FindProperty("occlusionLayers").intValue = Physics.DefaultRaycastLayers;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T Find<T>(string name) where T : Component
        {
            var root = GameObject.Find(name);
            if (root == null || !root.TryGetComponent(out T component))
                throw new InvalidOperationException("Missing scene component " + name + ": " + typeof(T).Name);
            return component;
        }

        private static void InvokeRpc(Component target, string method, params object[] arguments)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
        }

        private static IEnumerator Await(Func<bool> condition, string label)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Expect(condition(), label);
        }

        private static void Expect(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException(label);
            Debug.Log("[P3-06 SCENE] PASS " + label);
        }
    }
}
#endif
