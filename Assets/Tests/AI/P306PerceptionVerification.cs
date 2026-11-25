#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using TheCopy.AI;
using TheCopy.Networking;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheCopy.Tests
{
    internal static class P306PerceptionVerification
    {
        internal static bool Requested =>
            Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-p306Perception");

        internal static void Prepare(GameObject npc, string folder)
        {
            var light = npc.AddComponent<CopyLightPerception>();
            var lightSettings = new SerializedObject(light);
            lightSettings.FindProperty("occlusionLayers").intValue = 1 << 30;
            lightSettings.ApplyModifiedPropertiesWithoutUndo();
            npc.AddComponent<CopySceneContext>();
            npc.AddComponent<CopyPerception>();
            npc.AddComponent<CopyDecisionInput>();
            if (P403PlayerSightVerification.Requested)
                npc.AddComponent<CopyPlayerPerception>();
            if (P403DecisionV2Verification.Requested)
                npc.AddComponent<CopyDecisionInputV2>();
            if (P403AgentVerification.Requested)
                P403AgentVerification.Prepare(npc);
            if (P404TraceVerification.Requested)
                P404TraceVerification.Prepare(npc);
            if (P406TrainingVerification.Requested || P406CoordinatorVerification.Requested)
                P406TrainingVerification.Prepare();

            CreateSource<NetworkTriggerZone>(folder);
            CreateSource<NetworkInteractiveObject>(folder);
            CreateSource<NetworkAirlockDoor>(folder);
            CreateSource<NetworkKeypadTerminal>(folder);
        }

        internal static void RegisterSources(NetworkManager manager, string folder)
        {
            if (P406TrainingVerification.Requested || P406CoordinatorVerification.Requested)
                manager.AddNetworkPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/P406TrainingActor.prefab"));
            manager.AddNetworkPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath<NetworkTriggerZone>(folder)));
            manager.AddNetworkPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath<NetworkInteractiveObject>(folder)));
            manager.AddNetworkPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath<NetworkAirlockDoor>(folder)));
            manager.AddNetworkPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath<NetworkKeypadTerminal>(folder)));
        }

        internal static IEnumerator Verify(NetworkManager host, NetworkManager client, CopyHearing npc, string folder)
        {
            var light = npc.GetComponent<CopyLightPerception>();
            var context = npc.GetComponent<CopySceneContext>();
            var clientNpc = client.SpawnManager.SpawnedObjects[npc.NetworkObjectId];
            var remote = client.LocalClient.PlayerObject.GetComponent<PlayerController>();
            var serverPlayer = host.ConnectedClients[client.LocalClientId].PlayerObject.GetComponent<PlayerController>();
            Light flashlight = serverPlayer.GetComponentInChildren<Light>();
            Transform beam = flashlight.transform;
            Vector3 origin = beam.position;
            Vector3 forward = beam.forward;
            Vector3 eyeOffset = Vector3.up * 0.6f;
            Vector3 near = origin + forward * 3f - eyeOffset;
            npc.transform.position = near;
            Expect(!light.TryGetStrongestLight(out _), "off flashlight produces no observation");

            ToggleFlashlight(remote);
            yield return Await(() => serverPlayer.IsFlashlightOn.Value, "remote flashlight RPC replicated on state");
            yield return Await(() => serverPlayer.HasServerCameraPose, "initial remote camera pose received on server");
            Expect(light.TryGetStrongestLight(out var observation) &&
                observation.SourceClientId == client.LocalClientId && !observation.IsAimApproximate &&
                observation.Position == origin && Mathf.Abs(observation.Strength - (1f - 3f / flashlight.range)) < 0.001f,
                "server detects actual player flashlight with ID, attenuation, and synchronized aim");
            Expect(!clientNpc.GetComponent<CopyLightPerception>().TryGetStrongestLight(out _),
                "client replica cannot read server light perception");
            Expect(!clientNpc.GetComponent<CopyPerception>().TryCapture(out _),
                "client replica cannot capture authoritative perception snapshot");
            var deniedInput = new float[CopyDecisionEncoder.ObservationCount];
            Array.Fill(deniedInput, 1f);
            Expect(!clientNpc.GetComponent<CopyDecisionInput>().TryCapture(deniedInput, out _) &&
                Array.TrueForAll(deniedInput, value => value == 0f),
                "client decision input refuses capture and clears stale observations");

            Transform remoteCamera = remote.GetComponentInChildren<Light>().transform.parent;
            Transform serverCamera = beam.parent;
            Quaternion initialAim = remoteCamera.localRotation;
            float initialHeight = remoteCamera.localPosition.y;
            remote.enabled = false; // Supply controlled input, then invoke the production LateUpdate sender.
            yield return SendCameraPose(remote, remoteCamera, 60f, initialHeight);
            yield return Await(() => Quaternion.Angle(serverCamera.localRotation, remoteCamera.localRotation) < 0.1f,
                "remote downward camera pitch reaches server through production sender");
            Expect(!light.TryGetStrongestLight(out _), "tilting away removes the original light observation");
            npc.transform.position = beam.position + beam.forward * 3f - eyeOffset;
            Expect(light.TryGetStrongestLight(out var tilted) && !tilted.IsAimApproximate,
                "NPC inside the downward beam sees synchronized light");

            yield return SendCameraPose(remote, remoteCamera, -60f, initialHeight);
            yield return Await(() => Quaternion.Angle(serverCamera.localRotation, remoteCamera.localRotation) < 0.1f,
                "remote upward pitch reaches server");
            Expect(!light.TryGetStrongestLight(out _), "upward beam leaves previous downward target");
            npc.transform.position = beam.position + beam.forward * 3f - eyeOffset;
            Expect(light.TryGetStrongestLight(out _), "NPC inside upward beam sees light");

            Vector3 standingOrigin = beam.position;
            yield return SendCameraPose(remote, remoteCamera, -60f, initialHeight - 0.45f);
            yield return Await(() => Mathf.Abs(serverCamera.localPosition.y - remoteCamera.localPosition.y) < 0.001f,
                "remote crouched camera height reaches server");
            Expect(Mathf.Abs(standingOrigin.y - beam.position.y - 0.45f) < 0.001f &&
                light.TryGetStrongestLight(out var crouched) && crouched.Position == beam.position,
                "light source position follows remote crouching");

            Quaternion validRotation = serverCamera.localRotation;
            Vector3 validPosition = serverCamera.localPosition;
            InvokeCameraRpc(remote, float.NaN, initialHeight);
            InvokeCameraRpc(remote, 0f, float.PositiveInfinity);
            yield return new WaitForSecondsRealtime(0.2f);
            Expect(serverCamera.localRotation == validRotation && serverCamera.localPosition == validPosition,
                "non-finite camera data leaves valid server pose unchanged");
            InvokeCameraRpc(remote, -120f, initialHeight + 10f);
            yield return Await(() => Quaternion.Angle(serverCamera.localRotation, Quaternion.Euler(-80f, 0f, 0f)) < 0.1f &&
                Mathf.Abs(serverCamera.localPosition.y - initialHeight) < 0.001f,
                "server clamps pitch and height to supported limits");
            Expect(Quaternion.Angle(remoteCamera.localRotation, Quaternion.Euler(-60f, 0f, 0f)) < 0.1f &&
                Mathf.Abs(remoteCamera.localPosition.y - (initialHeight - 0.45f)) < 0.001f,
                "server validation does not overwrite owner camera");

            yield return SendCameraPose(remote, remoteCamera, 0f, initialHeight);
            yield return Await(() => Quaternion.Angle(serverCamera.localRotation, Quaternion.identity) < 0.1f &&
                Mathf.Abs(serverCamera.localPosition.y - initialHeight) < 0.001f,
                "standing camera pose restored after crouching");
            remoteCamera.localRotation = initialAim;
            remote.enabled = true;
            remote.EnterSeat(null, remote.transform);
            yield return Await(() => Quaternion.Angle(serverCamera.localRotation, Quaternion.Euler(25f, 0f, 0f)) < 0.1f,
                "seat camera pose is sent while normal movement Update returns early");
            remote.ExitSeat();
            yield return Await(() => Quaternion.Angle(serverCamera.localRotation, Quaternion.identity) < 0.1f,
                "leaving seat restores server camera aim");

            npc.transform.position = origin + beam.right * 3f - eyeOffset;
            Expect(!light.TryGetStrongestLight(out _), "point outside flashlight cone rejected");
            npc.transform.position = origin + forward * (flashlight.range + 1f) - eyeOffset;
            Expect(!light.TryGetStrongestLight(out _), "point beyond flashlight range rejected");
            npc.transform.position = near;

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = 30;
            wall.transform.SetPositionAndRotation(origin + forward * 1.5f, Quaternion.LookRotation(forward));
            wall.transform.localScale = new Vector3(2, 2, 0.2f);
            Physics.SyncTransforms();
            Expect(!light.TryGetStrongestLight(out _), "selected opaque layer blocks the flashlight");
            Object.Destroy(wall);
            ToggleFlashlight(remote);
            yield return Await(() => !serverPlayer.IsFlashlightOn.Value, "remote flashlight RPC replicated off state");
            Expect(!light.TryGetStrongestLight(out _), "switching off removes the observation");

            Expect(context.TryRead(out var unknown) && !unknown.ZoneTriggered.HasValue && !unknown.PowerOn.HasValue &&
                !unknown.AirlockOpen.HasValue && !unknown.KeypadUnlocked.HasValue,
                "unbound scene facts remain unknown");
            Expect(!clientNpc.GetComponent<CopySceneContext>().TryRead(out _), "client cannot read the NPC scene context");

            var zone = InstantiateSource<NetworkTriggerZone>(folder);
            var power = InstantiateSource<NetworkInteractiveObject>(folder);
            var door = InstantiateSource<NetworkAirlockDoor>(folder);
            var keypad = InstantiateSource<NetworkKeypadTerminal>(folder);
            var doorSettings = new SerializedObject(door);
            var switches = doorSettings.FindProperty("requiredPowerSwitches");
            switches.arraySize = 1;
            switches.GetArrayElementAtIndex(0).objectReferenceValue = power;
            doorSettings.ApplyModifiedPropertiesWithoutUndo();
            zone.NetworkObject.Spawn();
            power.NetworkObject.SpawnWithObservers = true;
            power.NetworkObject.Spawn();
            door.NetworkObject.Spawn();
            keypad.NetworkObject.Spawn();

            var bindings = new SerializedObject(context);
            bindings.FindProperty("zone").objectReferenceValue = zone;
            bindings.FindProperty("powerNode").objectReferenceValue = power;
            bindings.FindProperty("airlock").objectReferenceValue = door;
            bindings.FindProperty("keypad").objectReferenceValue = keypad;
            bindings.ApplyModifiedPropertiesWithoutUndo();
            Expect(context.TryRead(out var initial) && initial.ZoneTriggered == false && initial.PowerOn == false &&
                initial.AirlockOpen == false && initial.KeypadUnlocked == false, "real scene variables distinguish false from unknown");
            yield return null;

            int changes = 0;
            CopySceneSnapshot changed = default;
            context.Changed += snapshot => { changes++; changed = snapshot; };
            zone.transform.position = host.LocalClient.PlayerObject.transform.position;
            Physics.SyncTransforms();
            yield return Await(() => changes > 0 && changed.ZoneTriggered == true, "actual sector trigger produces context change");

            power.IsActive.Value = true;
            keypad.IsUnlocked.Value = true;
            yield return Await(() => changed.PowerOn == true && changed.AirlockOpen == true && changed.KeypadUnlocked == true,
                "context observes power, linked airlock, and keypad state");
            int settled = changes;
            yield return null;
            yield return null;
            Expect(changes == settled, "unchanged state emits no duplicate notification");

            keypad.NetworkObject.Despawn(false);
            yield return Await(() => changes > settled && changed.KeypadUnlocked == null,
                "despawned source returns to unknown");
            yield return Await(() => client.SpawnManager.SpawnedObjects.ContainsKey(power.NetworkObjectId),
                "power node replica reaches remote client");
            var clientPower = client.SpawnManager.SpawnedObjects[power.NetworkObjectId].GetComponent<NetworkInteractiveObject>();
            yield return Await(() => clientPower.IsActive.Value, "remote client receives initial powered state before NPC change");
            Expect(!clientPower.CanSetActiveFromNpc(clientNpc, false) && !clientPower.TrySetActiveFromNpc(clientNpc, false),
                "client replica cannot execute server NPC API");
            Expect(!power.TrySetActiveFromNpc(clientNpc, false), "foreign-manager actor rejected");
            Expect(!power.TrySetActiveFromNpc(door.NetworkObject, false), "unmarked server actor rejected");
            npc.transform.position = power.transform.position + Vector3.left;
            npc.NetworkObject.ChangeOwnership(client.LocalClientId);
            Expect(!power.TrySetActiveFromNpc(npc.NetworkObject, false), "client-owned NPC rejected by server");
            npc.NetworkObject.RemoveOwnership();
            power.enabled = false;
            Expect(!power.TrySetActiveFromNpc(npc.NetworkObject, false), "disabled power node rejects NPC");
            power.enabled = true;
            Expect(power.TrySetActiveFromNpc(npc.NetworkObject, false), "server-owned NPC executes validated interaction");
            yield return Await(() => !clientPower.IsActive.Value, "NPC power state replicates to remote client");
            Debug.Log("[P3-06] COMPLETE: flashlight and scene context");
            if (P403DecisionV2Verification.Requested)
                P403DecisionV2Verification.Verify(host, client, npc);
            if (P403PlayerSightVerification.Requested)
            {
                if (P403AgentVerification.Requested)
                    yield return P403AgentVerification.Verify(host, client, npc);
                if (P404TraceVerification.Requested)
                    P404TraceVerification.Verify(host, client, npc);
                if (P405AttentionCaseVerification.Requested)
                    yield return P405AttentionCaseVerification.Verify(host, client, npc);
                if (P405RecipeVerification.Requested)
                    yield return P405RecipeVerification.Verify(host, client, npc);
                if (P406TrainingVerification.Requested)
                    yield return P406TrainingVerification.Verify(host, client);
                if (P406CoordinatorVerification.Requested)
                    yield return P406CoordinatorVerification.Verify(host, client);
                yield return P403PlayerSightVerification.Verify(host, client, npc);
            }
        }

        private static void CreateSource<T>(string folder) where T : NetworkBehaviour
        {
            var source = new GameObject(typeof(T).Name);
            source.AddComponent<NetworkObject>();
            source.AddComponent<BoxCollider>();
            source.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(source, SourcePath<T>(folder));
            Object.DestroyImmediate(source);
        }

        private static T InstantiateSource<T>(string folder) where T : NetworkBehaviour
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath<T>(folder));
            var source = Object.Instantiate(prefab, new Vector3(100, 0, 0), Quaternion.identity).GetComponent<T>();
            source.NetworkObject.SpawnWithObservers = false;
            return source;
        }

        private static string SourcePath<T>(string folder) => $"{folder}/{typeof(T).Name}.prefab";

        private static void ToggleFlashlight(PlayerController player)
        {
            typeof(PlayerController).GetMethod("ToggleFlashlightServerRpc", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(player, null);
        }

        private static IEnumerator SendCameraPose(PlayerController player, Transform camera, float pitch, float height)
        {
            yield return new WaitForSecondsRealtime(0.11f);
            camera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 position = camera.localPosition;
            position.y = height;
            camera.localPosition = position;
            typeof(PlayerController).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
        }

        private static void InvokeCameraRpc(PlayerController player, float pitch, float height)
        {
            typeof(PlayerController).GetMethod("SyncCameraPoseServerRpc", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(player, new object[] { pitch, height, default(ServerRpcParams) });
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
            Debug.Log("[P3-06] PASS " + label);
        }
    }
}
#endif
