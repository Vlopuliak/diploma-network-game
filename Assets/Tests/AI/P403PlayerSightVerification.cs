#if UNITY_EDITOR
using System;
using System.Collections;
using TheCopy.AI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace TheCopy.Tests
{
    internal static class P403PlayerSightVerification
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), a => a == "-p403PlayerSight");

        public static IEnumerator Verify(NetworkManager host, NetworkManager client, CopyHearing npc)
        {
            var sight = npc.GetComponent<CopyPlayerPerception>();
            var replica = client.SpawnManager.SpawnedObjects[npc.NetworkObjectId].GetComponent<CopyPlayerPerception>();
            var remote = host.ConnectedClients[client.LocalClientId].PlayerObject;
            var local = host.LocalClient.PlayerObject;
            Vector3 oldNpc = npc.transform.position;
            Quaternion oldRotation = npc.transform.rotation;
            Vector3 oldRemote = remote.transform.position;
            Vector3 oldLocal = local.transform.position;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.SetActive(false);
            try
            {
                // Controlled geometry on real spawned player prefabs, away from fixture mechanisms.
                npc.transform.SetPositionAndRotation(new Vector3(1000, 0, 1000), Quaternion.identity);
                Vector3 eye = npc.transform.position + Vector3.up * 0.6f;
                Place(remote, eye + Vector3.forward * 3f);
                Place(local, eye + Vector3.forward * 4f + Vector3.right * 3f);
                Check(sight.TryObserve(client.LocalClientId, out var observed) && observed.SourceClientId == client.LocalClientId &&
                    observed.NetworkObjectId == remote.NetworkObjectId && observed.Position == eye + Vector3.forward * 3f &&
                    Mathf.Abs(observed.Distance - 3f) < 0.001f && observed.CapturedAt == Time.timeAsDouble,
                    "remote player yields identified immutable server observation");
                Check(sight.TryObserve(0, out var hostObservation) && hostObservation.SourceClientId == 0 &&
                    hostObservation.NetworkObjectId == local.NetworkObjectId, "host ID zero is a valid independent visual target");
                Check(!replica.TryObserve(client.LocalClientId, out var denied) && denied.Equals(default(CopyPlayerObservation)),
                    "client replica cannot read server vision and clears output");
                Check(!sight.TryObserve(ulong.MaxValue, out denied) && denied.Equals(default(CopyPlayerObservation)),
                    "unknown ID rejected without a stale target");
                Check(!remote.GetComponent<PlayerController>().IsFlashlightOn.Value && sight.TryObserve(client.LocalClientId, out _),
                    "visual geometry does not require target flashlight");

                wall.transform.position = eye + Vector3.forward * 1.5f;
                wall.transform.localScale = new Vector3(1, 2, 0.2f);
                wall.SetActive(true);
                Physics.SyncTransforms();
                Check(!sight.TryObserve(client.LocalClientId, out denied) && denied.Equals(default(CopyPlayerObservation)),
                    "opaque Default wall removes current target and clears output");
                Check(sight.TryObserve(0, out _), "occlusion is evaluated separately for each ID");
                wall.GetComponent<BoxCollider>().isTrigger = true;
                Physics.SyncTransforms();
                Check(sight.TryObserve(client.LocalClientId, out _), "trigger volume does not occlude sight");
                wall.GetComponent<BoxCollider>().isTrigger = false;
                Set(sight, "occlusionLayers", 1 << 30);
                Check(sight.TryObserve(client.LocalClientId, out _), "unselected opaque layer is ignored");
                Set(sight, "occlusionLayers", Physics.DefaultRaycastLayers);
                wall.SetActive(false);
                Physics.SyncTransforms();
                Check(sight.TryObserve(client.LocalClientId, out _), "disabling wall restores observation");

                Place(remote, eye - Vector3.forward * 3f);
                Check(!sight.TryObserve(client.LocalClientId, out _), "player behind observer is hidden");
                Check(observed.Position == eye + Vector3.forward * 3f && observed.Distance == 3f,
                    "previous observation retains copied values after hidden movement");
                npc.transform.rotation = Quaternion.Euler(0, 180, 0);
                Check(sight.TryObserve(client.LocalClientId, out _), "observer rotation changes visual cone");
                npc.transform.rotation = Quaternion.identity;
                Place(remote, eye + Vector3.up * 3f);
                Check(!sight.TryObserve(client.LocalClientId, out _), "vertical direction also respects 3D cone");
                Place(remote, eye + Quaternion.Euler(0, 59, 0) * Vector3.forward * 3f);
                Check(sight.TryObserve(client.LocalClientId, out _), "inside configured half-angle is visible");
                Place(remote, eye + Quaternion.Euler(0, 61, 0) * Vector3.forward * 3f);
                Check(!sight.TryObserve(client.LocalClientId, out _), "outside configured half-angle is hidden");
                Place(remote, eye + Vector3.forward * 18f);
                Check(!sight.TryObserve(client.LocalClientId, out _), "exact radius is excluded");
                Place(remote, eye + Vector3.forward * 17.9f);
                Check(sight.TryObserve(client.LocalClientId, out _), "point just inside radius is visible");
                Place(remote, eye + Vector3.forward * 18.1f);
                Check(!sight.TryObserve(client.LocalClientId, out _), "point beyond radius is hidden");
                Place(remote, eye);
                Check(sight.TryObserve(client.LocalClientId, out var coincident) && coincident.Distance < 0.001f,
                    "coincident eye and body point remain finite");
                Place(remote, eye + Vector3.forward * 3f);
                var body = remote.GetComponent<CapsuleCollider>();
                Vector3 originalCenter = body.center;
                body.center = originalCenter - Vector3.up * 0.3f;
                Check(sight.TryObserve(client.LocalClientId, out var lowered) && lowered.Position == eye + Vector3.forward * 3f - Vector3.up * 0.3f,
                    "aim point follows authoritative body center");
                body.center = originalCenter;
                sight.enabled = false;
                Check(!sight.TryObserve(client.LocalClientId, out _), "disabled sensor rejects capture");
                sight.enabled = true;
                Set(sight, "sightRadius", float.NaN);
                Check(!sight.TryObserve(client.LocalClientId, out _), "non-finite radius fails closed");
                Set(sight, "sightRadius", 18f);
                Set(sight, "fieldOfView", 0f);
                Check(!sight.TryObserve(client.LocalClientId, out _), "invalid cone fails closed");
                Set(sight, "fieldOfView", 120f);
                Set(sight, "eyeHeight", float.PositiveInfinity);
                Check(!sight.TryObserve(client.LocalClientId, out _), "non-finite eye height fails closed");
                Set(sight, "eyeHeight", 0.6f);
                remote.gameObject.SetActive(false);
                Check(!sight.TryObserve(client.LocalClientId, out _), "inactive target rejected");
                remote.gameObject.SetActive(true);
            }
            finally
            {
                UnityEngine.Object.Destroy(wall);
                npc.transform.SetPositionAndRotation(oldNpc, oldRotation);
                remote.transform.position = oldRemote;
                local.transform.position = oldLocal;
                Physics.SyncTransforms();
            }

            // Disconnect exercises real NGO roster removal, without trusting a cached player object.
            ulong remoteId = client.LocalClientId;
            client.Shutdown();
            double deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (host.ConnectedClients.ContainsKey(remoteId) && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Check(!host.ConnectedClients.ContainsKey(remoteId) && !sight.TryObserve(remoteId, out _),
                "disconnected remote ID loses its visual target");
            if (P403DecisionV2Verification.Requested)
                P403DecisionV2Verification.VerifyDisconnected(npc);
            if (P404TraceVerification.Requested)
                P404TraceVerification.PrepareDespawn(npc);
            npc.NetworkObject.Despawn(false);
            Check(!sight.TryObserve(0, out _), "despawned observer rejects capture");
            if (P403DecisionV2Verification.Requested)
                P403DecisionV2Verification.VerifyDespawned(npc);
            if (P403AgentVerification.Requested)
                P403AgentVerification.VerifyDespawned(npc);
            if (P404TraceVerification.Requested)
                P404TraceVerification.VerifyDespawn(npc);
            Debug.Log("[P4-03 SIGHT] COMPLETE: authoritative player visibility");
        }

        private static void Place(NetworkObject player, Vector3 bodyPosition)
        {
            var body = player.GetComponent<CapsuleCollider>();
            player.transform.position += bodyPosition - body.transform.TransformPoint(body.center);
            Physics.SyncTransforms();
        }

        private static void Set(CopyPlayerPerception sensor, string field, float value)
        {
            var settings = new SerializedObject(sensor);
            settings.FindProperty(field).floatValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(CopyPlayerPerception sensor, string field, int value)
        {
            var settings = new SerializedObject(sensor);
            settings.FindProperty(field).intValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            Debug.Log("[P4-03 SIGHT] PASS " + label);
        }
    }
}
#endif
