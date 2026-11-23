#if UNITY_EDITOR
using System;
using TheCopy.AI;
using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;

namespace TheCopy.Tests
{
    internal static class P403DecisionV2Verification
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), a => a == "-p403DecisionInputV2");

        public static void Verify(NetworkManager host, NetworkManager client, CopyHearing npc)
        {
            VerifySlots();
            VerifyEncoder();
            var input = npc.GetComponent<CopyDecisionInputV2>();
            var sight = npc.GetComponent<CopyPlayerPerception>();
            var remote = host.ConnectedClients[client.LocalClientId].PlayerObject;
            var local = host.LocalClient.PlayerObject;
            Vector3 oldNpc = npc.transform.position, oldLocal = local.transform.position, oldRemote = remote.transform.position;
            Quaternion oldRotation = npc.transform.rotation;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.SetActive(false);
            var values = new float[50];
            try
            {
                npc.transform.SetPositionAndRotation(new Vector3(1000, 0, 1000), Quaternion.identity);
                Vector3 eye = npc.transform.position + Vector3.up * 0.6f;
                Place(local, eye + new Vector3(-3, 0, 4));
                Place(remote, eye + Vector3.forward * 3f);
                Check(input.TryCaptureSnapshot(out var snapshot) && snapshot.PlayerA?.SourceClientId == 0 &&
                    snapshot.PlayerB?.SourceClientId == client.LocalClientId, "real roster maps Host to A and remote Client to B");
                Check(input.TryCapture(values, out var both) && values[40] == 1f && values[45] == 1f &&
                    both.IsAllowed(CopyDecisionAction.ObservePlayerA) && both.IsAllowed(CopyDecisionAction.ObservePlayerB),
                    "both visible players enable their own masks");
                Check(both.TryGetTarget(CopyDecisionAction.ObservePlayerB, out var target) &&
                    target.SourceClientId == client.LocalClientId && target.Position == snapshot.PlayerB.Value.Position,
                    "remote visual candidate carries observed position and ID");
                Check(!both.IsAllowed(CopyDecisionAction.AssistMechanism) && !both.IsAllowed(CopyDecisionAction.ObstructMechanism),
                    "both mechanism actions stay closed by user decision");

                wall.transform.position = eye + Vector3.forward * 1.5f;
                wall.transform.localScale = new Vector3(1, 2, 0.2f);
                wall.SetActive(true);
                Physics.SyncTransforms();
                Check(input.TryCapture(values, out var blocked) && values[40] == 1f && ZeroBlock(values, 45) &&
                    !blocked.TryGetTarget(CopyDecisionAction.ObservePlayerB, out _), "wall clears only hidden B features and candidate");
                var hidden = (float[])values.Clone();
                Place(remote, eye - Vector3.forward * 10f);
                Check(input.TryCapture(values, out _) && Equal(values, hidden), "unseen movement cannot leak into numeric input");
                wall.SetActive(false);
                Place(remote, eye + Vector3.forward * 6f);
                Place(local, eye - Vector3.forward * 4f);
                Check(input.TryCapture(values, out var onlyB) && ZeroBlock(values, 40) && values[45] == 1f &&
                    !onlyB.IsAllowed(CopyDecisionAction.ObservePlayerA) && onlyB.IsAllowed(CopyDecisionAction.ObservePlayerB),
                    "visibility swap preserves remote slot B");
                Check(target.Position == snapshot.PlayerB.Value.Position, "previous candidate remains an immutable observation");

                var replica = client.SpawnManager.SpawnedObjects[npc.NetworkObjectId].GetComponent<CopyDecisionInputV2>();
                Array.Fill(values, 1f);
                Check(!replica.TryCapture(values, out var denied) && Array.TrueForAll(values, x => x == 0) &&
                    !denied.IsAllowed(CopyDecisionAction.Continue) && !replica.TryCaptureSnapshot(out _),
                    "client refuses snapshot and clears stale vector and masks");
                input.enabled = false;
                Array.Fill(values, 1f);
                Check(!input.TryCapture(values, out _) && Array.TrueForAll(values, x => x == 0), "disabled input clears buffer");
                input.enabled = true;
                sight.enabled = false;
                Check(input.TryCapture(values, out var blind) && ZeroBlock(values, 40) && ZeroBlock(values, 45) &&
                    !blind.IsAllowed(CopyDecisionAction.ObservePlayerB), "disabled sight clears players while sensory input remains available");
                sight.enabled = true;
                var perception = npc.GetComponent<CopyPerception>();
                perception.enabled = false;
                Array.Fill(values, 1f);
                Check(!input.TryCapture(values, out _) && Array.TrueForAll(values, x => x == 0), "failed base perception clears all 50 values");
                perception.enabled = true;
            }
            finally
            {
                UnityEngine.Object.Destroy(wall);
                npc.transform.SetPositionAndRotation(oldNpc, oldRotation);
                local.transform.position = oldLocal;
                remote.transform.position = oldRemote;
                Physics.SyncTransforms();
            }
            Debug.Log("[P4-03 V2] COMPLETE: versioned player inputs and stable slots");
        }

        internal static void VerifyDisconnected(CopyHearing npc)
        {
            Check(npc.GetComponent<CopyDecisionInputV2>().TryCaptureSnapshot(out var snapshot) && !snapshot.PlayerB.HasValue,
                "real disconnect removes B from decision snapshot");
        }

        internal static void VerifyDespawned(CopyHearing npc)
        {
            var values = new float[50];
            Array.Fill(values, 1f);
            Check(!npc.GetComponent<CopyDecisionInputV2>().TryCapture(values, out var denied) &&
                Array.TrueForAll(values, x => x == 0) && !denied.IsAllowed(CopyDecisionAction.ObservePlayerB),
                "despawned input clears vector and visual candidates");
        }

        private static void VerifySlots()
        {
            var slots = new CopyPlayerSlots();
            slots.Refresh(new ulong[] { 8, 0 });
            Check(slots.A == 0 && slots.B == 8, "initial assignment sorts IDs independently of enumeration order");
            slots.Refresh(new ulong[] { 0, 8 });
            Check(slots.A == 0 && slots.B == 8, "roster reorder preserves slots");
            slots.Refresh(new ulong[] { 8 });
            Check(!slots.A.HasValue && slots.B == 8, "disconnect of A does not compact surviving B");
            slots.Refresh(new ulong[] { 8, 12 });
            Check(slots.A == 12 && slots.B == 8, "new connection fills vacancy without moving survivor");
            slots.Refresh(new ulong[] { 14, 12, 8 });
            Check(slots.A == 12 && slots.B == 8, "extra connection does not displace existing players");
            slots.Refresh(Array.Empty<ulong>());
            Check(!slots.A.HasValue && !slots.B.HasValue, "empty roster removes old identities");
            slots.Refresh(new ulong[] { ulong.MaxValue });
            Check(slots.A == ulong.MaxValue && !slots.B.HasValue, "maximum ID is not used as an empty sentinel");
            slots.Clear();
            slots.Refresh(new ulong[] { 2, 1 });
            Check(slots.A == 1 && slots.B == 2, "new observer lifetime starts a fresh deterministic mapping");
        }

        private static void VerifyEncoder()
        {
            var raw = new CopyPerceptionSnapshot(10, new CopySoundObservation(new Vector3(1, 2, 3),
                NoiseSource.PlayerVoice, 0.5f, 1f, 0), null, null, new CopySceneSnapshot(true, false, true, null));
            var a = new CopyPlayerObservation(0, 10, new Vector3(9, 0, 0), 9, 10);
            var b = new CopyPlayerObservation(1, 11, new Vector3(0, 0, 18), 18, 10);
            var values = new float[50];
            var v1 = new float[40];
            Check(CopyDecisionEncoderV2.SchemaId == "the-copy.decision-input/sensory-intent-v1" &&
                CopyDecisionEncoderV2.ObservationCount == 50 && CopyDecisionCandidatesV2.ActionCount == 7,
                "draft-2 declares 50 observations with unchanged seven action IDs");
            Check(CopyDecisionEncoderV2.TryEncode(new CopyDecisionSnapshotV2(raw, a, b), Vector3.zero, Quaternion.identity, values, out var candidates) &&
                values[40] == 1 && values[41] == 0.5f && values[42] == 0 && values[43] == 0 && values[44] == 0.5f &&
                values[45] == 1 && values[46] == 0 && values[47] == 0 && values[48] == 1 && values[49] == 1,
                "documented A and B offsets and normalization are exact");
            CopyDecisionEncoder.TryEncode(raw, Vector3.zero, Quaternion.identity, v1, out _);
            Check(PrefixEqual(values, v1) && candidates.IsAllowed(CopyDecisionAction.InvestigateSound),
                "sensory prefix and sound mask retain draft-1 semantics");
            var original = (float[])values.Clone();
            var renamedA = new CopyPlayerObservation(100, 20, a.Position, a.Distance, 10);
            var renamedB = new CopyPlayerObservation(200, 21, b.Position, b.Distance, 10);
            Check(CopyDecisionEncoderV2.TryEncode(new CopyDecisionSnapshotV2(raw, renamedA, renamedB), Vector3.zero, Quaternion.identity, values, out _) &&
                Equal(values, original), "player IDs and object IDs never become numerical features");
            Check(CopyDecisionEncoderV2.TryEncode(new CopyDecisionSnapshotV2(raw, null, b), Vector3.zero, Quaternion.identity, values, out var missing) &&
                ZeroBlock(values, 40) && values[45] == 1 && !missing.IsAllowed(CopyDecisionAction.ObservePlayerA),
                "absent A is zeros without shifting B");
            var stale = new CopyPlayerObservation(1, 11, b.Position, b.Distance, 9);
            Check(!CopyDecisionEncoderV2.TryEncode(new CopyDecisionSnapshotV2(raw, a, stale), Vector3.zero, Quaternion.identity, values, out _) &&
                Array.TrueForAll(values, x => x == 0), "stale sight timestamp rejects the whole vector");
            var duplicateId = new CopyPlayerObservation(0, 11, b.Position, b.Distance, 10);
            Check(!CopyDecisionEncoderV2.TryEncode(new CopyDecisionSnapshotV2(raw, a, duplicateId), Vector3.zero, Quaternion.identity, values, out _),
                "duplicate player identity in two slots rejected");
            var duplicateObject = new CopyPlayerObservation(1, 10, b.Position, b.Distance, 10);
            Check(!CopyDecisionEncoderV2.TryEncode(new CopyDecisionSnapshotV2(raw, a, duplicateObject), Vector3.zero, Quaternion.identity, values, out _),
                "duplicate network object in two slots rejected");
            var invalid = new CopyPlayerObservation(1, 11, new Vector3(float.NaN, 0, 0), 1, 10);
            Check(!CopyDecisionEncoderV2.TryEncode(new CopyDecisionSnapshotV2(raw, a, invalid), Vector3.zero, Quaternion.identity, values, out _),
                "non-finite player coordinates rejected");
            bool wrongLength = false;
            try { CopyDecisionEncoderV2.TryEncode(default, Vector3.zero, Quaternion.identity, new float[40], out _); }
            catch (ArgumentException) { wrongLength = true; }
            Check(wrongLength, "draft-1 buffer is refused by draft-2");
            Check(!default(CopyDecisionCandidatesV2).IsAllowed(CopyDecisionAction.Continue), "failed capture has no usable default mask");
        }

        private static void Place(NetworkObject player, Vector3 position)
        {
            var body = player.GetComponent<CapsuleCollider>();
            player.transform.position += position - body.transform.TransformPoint(body.center);
            Physics.SyncTransforms();
        }
        private static bool ZeroBlock(float[] values, int offset)
        {
            for (int i = offset; i < offset + 5; i++) if (values[i] != 0) return false;
            return true;
        }
        private static bool PrefixEqual(float[] values, float[] prefix)
        {
            for (int i = 0; i < prefix.Length; i++) if (Mathf.Abs(values[i] - prefix[i]) > 0.00001f) return false;
            return true;
        }
        private static bool Equal(float[] a, float[] b) => a.Length == b.Length && PrefixEqual(a, b);
        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            Debug.Log("[P4-03 V2] PASS " + label);
        }
    }
}
#endif
