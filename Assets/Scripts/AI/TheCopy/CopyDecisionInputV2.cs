using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    // IDs are roster metadata, never numerical observations. Surviving slots are never compacted.
    internal sealed class CopyPlayerSlots
    {
        internal ulong? A { get; private set; }
        internal ulong? B { get; private set; }

        internal void Clear() { A = null; B = null; }

        internal void Refresh(IEnumerable<ulong> connectedIds)
        {
            bool aPresent = false, bPresent = false;
            foreach (ulong id in connectedIds)
            {
                aPresent |= A == id;
                bPresent |= B == id;
            }
            if (!aPresent) A = null;
            if (!bPresent) B = null;
            if (!A.HasValue) A = Next(connectedIds);
            if (!B.HasValue) B = Next(connectedIds);
        }

        private ulong? Next(IEnumerable<ulong> ids)
        {
            ulong? selected = null;
            foreach (ulong id in ids)
                if (A != id && B != id && (!selected.HasValue || id < selected.Value))
                    selected = id;
            return selected;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CopyPerception), typeof(CopyPlayerPerception))]
    public sealed class CopyDecisionInputV2 : NetworkBehaviour
    {
        private readonly CopyPlayerSlots _slots = new CopyPlayerSlots();

        public override void OnNetworkSpawn() { _slots.Clear(); }
        public override void OnNetworkDespawn() { _slots.Clear(); }

        public bool TryCapture(float[] observations, out CopyDecisionCandidatesV2 candidates)
        {
            candidates = default;
            if (!TryCaptureSnapshot(out var snapshot))
            {
                CopyDecisionEncoderV2.ClearObservations(observations);
                return false;
            }
            return CopyDecisionEncoderV2.TryEncode(snapshot, transform.position, transform.rotation, observations, out candidates);
        }

        public bool TryCaptureSnapshot(out CopyDecisionSnapshotV2 snapshot)
        {
            snapshot = default;
            if (!IsSpawned || !IsServer || !isActiveAndEnabled ||
                !TryGetComponent(out CopyPerception perception) || !perception.TryCapture(out var sensory))
                return false;

            _slots.Refresh(NetworkManager.ConnectedClients.Keys);
            CopyPlayerObservation? a = null, b = null;
            if (TryGetComponent(out CopyPlayerPerception sight))
            {
                if (_slots.A.HasValue && sight.TryObserve(_slots.A.Value, out var seenA)) a = seenA;
                if (_slots.B.HasValue && sight.TryObserve(_slots.B.Value, out var seenB)) b = seenB;
            }
            snapshot = new CopyDecisionSnapshotV2(sensory, a, b);
            return true;
        }
    }
}
