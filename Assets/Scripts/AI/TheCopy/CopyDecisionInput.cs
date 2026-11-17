using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CopyPerception))]
    public sealed class CopyDecisionInput : NetworkBehaviour
    {
        public bool TryCapture(float[] observations, out CopyDecisionCandidates candidates)
        {
            candidates = default;
            if (!IsSpawned || !IsServer || !isActiveAndEnabled ||
                !TryGetComponent(out CopyPerception perception) || !perception.TryCapture(out var snapshot))
            {
                CopyDecisionEncoder.ClearObservations(observations);
                return false;
            }

            return CopyDecisionEncoder.TryEncode(snapshot, transform.position, transform.rotation, observations, out candidates);
        }
    }
}
