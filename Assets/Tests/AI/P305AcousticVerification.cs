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
    // Batch-only integration fixture. The launch script provides a disposable project copy.
    public sealed class P305AcousticVerification : MonoBehaviour
    {
        private const string FixtureFolder = "Assets/P305Generated";
        private const string PlayerPrefabPath = FixtureFolder + "/Player.prefab";
        private const string NpcPrefabPath = FixtureFolder + "/Hearing.prefab";
        private const string RunKey = "TheCopy.P305.Run";
        private const float Loudness = 0.8f;

        private bool _failed;

        private sealed class DispatcherProbe : INoiseListener, IDisposable
        {
            public int Count { get; private set; }
            public NoiseEvent Last { get; private set; }

            public DispatcherProbe() => NoiseDispatcher.Register(this);
            public void Dispose() => NoiseDispatcher.Unregister(this);

            public void OnNoise(NoiseEvent noiseEvent)
            {
                Last = noiseEvent;
                Count++;
            }
        }

        public static void Run()
        {
            if (!Application.isBatchMode || !Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-p305Disposable"))
                throw new InvalidOperationException("Run P3-05 from the disposable-project launch script.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!AssetDatabase.IsValidFolder(FixtureFolder))
                AssetDatabase.CreateFolder("Assets", "P305Generated");

            var player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerPlaceholder.prefab"));
            player.AddComponent<P305AcousticTestSender>();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            DestroyImmediate(player);

            var npc = new GameObject("P3-05 NPC");
            var hearing = npc.AddComponent<CopyHearing>();
            var settings = new SerializedObject(hearing);
            settings.FindProperty("occlusionLayers").intValue = 1 << 30;
            settings.FindProperty("memoryLifetime").floatValue = 1.2f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (P306PerceptionVerification.Requested)
                P306PerceptionVerification.Prepare(npc, FixtureFolder);
            PrefabUtility.SaveAsPrefabAsset(npc, NpcPrefabPath);
            DestroyImmediate(npc);

            SessionState.SetBool(RunKey, true);
            EditorApplication.EnterPlaymode();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartVerification()
        {
            if (!SessionState.GetBool(RunKey, false))
                return;

            SessionState.EraseBool(RunKey);
            var runner = new GameObject("P3-05 Verification").AddComponent<P305AcousticVerification>();
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
            Debug.Log("[P3-05] FAIL " + message);
        }

        private void Update()
        {
            if (_failed)
                EditorApplication.Exit(1);
        }

        private IEnumerator Verify()
        {
            Application.runInBackground = true;
            bool useWebSockets = Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-p305WebSockets");
            Debug.Log(useWebSockets ? "[P3-05] transport=UTP WebSocket loopback" : "[P3-05] transport=UTP UDP loopback");
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);

            var host = CreateManager("P3-05 Host", 0, playerPrefab, npcPrefab, useWebSockets);
            Expect(host.StartHost(), "NGO host started");
            ushort port = host.GetComponent<UnityTransport>().GetLocalEndpoint().Port;
            Expect(port != 0, "host transport bound to an OS-assigned port");
            yield return null;
            var client = CreateManager("P3-05 Client", port, playerPrefab, npcPrefab, useWebSockets);
            Expect(client.StartClient(), "NGO client started");
            yield return Await(() => client.IsConnectedClient && client.LocalClient.PlayerObject != null, "client player spawned", 15);

            ulong remoteId = client.LocalClientId;
            var serverPlayer = host.ConnectedClients[remoteId].PlayerObject;
            var clientPlayer = client.LocalClient.PlayerObject;
            Expect(remoteId != host.LocalClientId && serverPlayer.OwnerClientId == remoteId, "separate remote player with server-known ID");
            Debug.Log($"[P3-05] hostClientId={host.LocalClientId} remoteClientId={remoteId}");
            clientPlayer.GetComponent<VoiceNoiseEmitter>().enabled = false;
            host.LocalClient.PlayerObject.GetComponent<VoiceNoiseEmitter>().enabled = false;

            var hearing = Instantiate(npcPrefab).GetComponent<CopyHearing>();
            Vector3 sourcePosition = serverPlayer.transform.position;
            Vector3 near = sourcePosition - new Vector3(3, 0.6f, 0);
            hearing.transform.position = near;
            hearing.NetworkObject.Spawn();
            Expect(hearing.IsServer && hearing.IsSpawned, "hearing spawned on host");
            ulong npcId = hearing.NetworkObjectId;
            yield return Await(() => client.SpawnManager.SpawnedObjects.ContainsKey(npcId), "client received the NPC spawn");
            var clientHearing = client.SpawnManager.SpawnedObjects[npcId].GetComponent<CopyHearing>();

            var clientSender = clientPlayer.GetComponent<P305AcousticTestSender>();
            var serverSender = serverPlayer.GetComponent<P305AcousticTestSender>();
            Expect(!CopyPlayerNoise.TryPublish(client, remoteId, NoiseSource.PlayerVoice, Loudness), "client cannot publish directly");
            Expect(!CopyPlayerNoise.TryPublish(host, ulong.MaxValue, NoiseSource.PlayerVoice, Loudness), "unconnected sender rejected");
            Expect(!CopyPlayerNoise.TryPublish(host, remoteId, NoiseSource.Environmental, Loudness), "non-player category rejected by bridge");

            // Invoke the game's RPC send stubs with controlled inputs after the client connects.
            // NGO fills the sender on receipt. Microphone capture and movement input are outside this test.
            using var dispatcher = new DispatcherProbe();
            InvokeRpc(clientPlayer.GetComponent<VoiceNoiseEmitter>(), "SendVoicePacketServerRpc",
                new byte[1100], Loudness, false, default(ServerRpcParams));
            yield return Await(() => IsLive(hearing, NoiseSource.PlayerVoice, remoteId), "production voice RPC delivers the remote sender ID");
            Expect(dispatcher.Count == 1 && dispatcher.Last.Source == NoiseSource.PlayerVoice &&
                dispatcher.Last.Position == sourcePosition && dispatcher.Last.Loudness == Loudness,
                "voice still publishes the unchanged event through NoiseDispatcher");
            hearing.TryGetLiveSound(out var voice);
            Expect(voice.Position == sourcePosition &&
                Mathf.Abs(voice.Strength - Loudness * (1f - 3f / 18f)) < 0.001f,
                "position from server player and linear distance attenuation");
            Expect(!clientHearing.TryGetLiveSound(out _) && !clientHearing.TryGetRememberedSound(out _),
                "client replica has no local hearing state");
            Report(voice);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = 30;
            wall.transform.position = sourcePosition - Vector3.right * 1.5f;
            wall.transform.localScale = new Vector3(0.2f, 2, 2);
            Physics.SyncTransforms();
            InvokeRpc(clientPlayer.GetComponent<PlayerController>(), "EmitFootstepNoiseServerRpc",
                Loudness, default(ServerRpcParams));
            yield return Await(() => IsLive(hearing, NoiseSource.PlayerFootstep, remoteId), "production footstep RPC delivers the remote sender ID");
            Expect(dispatcher.Count == 2 && dispatcher.Last.Source == NoiseSource.PlayerFootstep &&
                dispatcher.Last.Position == sourcePosition && dispatcher.Last.Loudness == Loudness,
                "footstep still publishes the unchanged event through NoiseDispatcher");
            hearing.TryGetLiveSound(out var step);
            Expect(Mathf.Abs(step.Strength - voice.Strength * 0.3f) < 0.001f, "wall attenuates footstep");
            Report(step);
            wall.SetActive(false);

            hearing.transform.position = sourcePosition - Vector3.right * 30f;
            int next = serverSender.ReceivedCount + 1;
            clientSender.SendNoiseRpc(NoiseSource.PlayerVoice, Loudness);
            yield return Await(() => serverSender.ReceivedCount == next, "out-of-range RPC received");
            Expect(serverSender.LastAccepted && hearing.TryGetRememberedSound(out var previous) &&
                previous.Source == NoiseSource.PlayerFootstep && previous.SourceClientId == remoteId,
                "out-of-range event leaves previous memory intact");
            yield return new WaitForSeconds(0.25f);
            Expect(!hearing.TryGetLiveSound(out _) && hearing.TryGetRememberedSound(out var memory) &&
                memory.SourceClientId == remoteId && memory.Position == step.Position &&
                memory.Strength < step.Strength && memory.Freshness < 1f,
                "memory preserves remote ID and position after live window and decays");
            yield return new WaitForSeconds(1.2f);
            Expect(!hearing.TryGetRememberedSound(out _), "memory expires");

            hearing.transform.position = host.LocalClient.PlayerObject.transform.position - new Vector3(3, 0.6f, 0);
            InvokeRpc(host.LocalClient.PlayerObject.GetComponent<VoiceNoiseEmitter>(), "SendVoicePacketServerRpc",
                new byte[1100], Loudness, false, default(ServerRpcParams));
            yield return Await(() => IsLive(hearing, NoiseSource.PlayerVoice, host.LocalClientId), "host ID zero distinct from anonymous");

            if (P306PerceptionVerification.Requested)
                yield return P306PerceptionVerification.Verify(host, client, hearing, FixtureFolder);

            client.Shutdown();
            host.Shutdown();
            yield return Await(() => !client.IsListening && !host.IsListening, "both NGO managers stopped");
            Application.logMessageReceived -= TrackError;
            Debug.Log("[P3-05] COMPLETE: remote RPC and attributed acoustic subset");
            EditorApplication.Exit(_failed ? 1 : 0);
        }

        private static NetworkManager CreateManager(
            string name, ushort port, GameObject player, GameObject npc, bool useWebSockets)
        {
            var root = new GameObject(name);
            var transport = root.AddComponent<UnityTransport>();
            transport.UseWebSockets = useWebSockets;
            transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            var manager = root.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
                PlayerPrefab = player
            };
            manager.AddNetworkPrefab(npc);
            if (P306PerceptionVerification.Requested)
                P306PerceptionVerification.RegisterSources(manager, FixtureFolder);
            return manager;
        }

        private static bool IsLive(CopyHearing hearing, NoiseSource source, ulong? id)
        {
            return hearing.TryGetLiveSound(out var sound) && sound.Source == source && sound.SourceClientId == id;
        }

        private static void InvokeRpc(Component target, string method, params object[] arguments)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
        }

        private static IEnumerator Await(Func<bool> condition, string label, double timeoutSeconds = 5)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Expect(condition(), label);
        }

        private static void Expect(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException(label);
            Debug.Log("[P3-05] PASS " + label);
        }

        private static void Report(CopySoundObservation sound)
        {
            Debug.Log($"[P3-05] sound category={sound.Source} sourceClientId={sound.SourceClientId} position={sound.Position} strength={sound.Strength:F3}");
        }
    }
}
#endif
