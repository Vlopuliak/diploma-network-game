using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    public readonly struct CopyPowerCandidate
    {
        public ulong NetworkObjectId { get; }
        public Vector3 Position { get; }
        public bool RequestedState { get; }
        public bool CanInteractNow { get; }

        internal CopyPowerCandidate(ulong networkObjectId, Vector3 position, bool requestedState, bool canInteractNow)
        {
            NetworkObjectId = networkObjectId;
            Position = position;
            RequestedState = requestedState;
            CanInteractNow = canInteractNow;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CopyPerception))]
    public sealed class CopyPowerInteraction : NetworkBehaviour
    {
        [SerializeField] private NetworkInteractiveObject target;
        [SerializeField] private bool allowPowerOn;
        [SerializeField] private bool allowPowerOff;

        public bool TryGetCandidate(bool active, out CopyPowerCandidate candidate)
        {
            candidate = default;
            if (!IsSpawned || !IsServer || !isActiveAndEnabled || target == null || !target.IsSpawned ||
                !target.isActiveAndEnabled || target.NetworkManager != NetworkManager ||
                !TryGetComponent(out CopyPerception perception) || !perception.isActiveAndEnabled ||
                !(active ? allowPowerOn : allowPowerOff) || target.IsActive.Value == active)
                return false;

            candidate = new CopyPowerCandidate(target.NetworkObjectId, target.transform.position, active,
                target.CanSetActiveFromNpc(NetworkObject, active));
            return true;
        }

        public bool TryApply(bool active)
        {
            return TryGetCandidate(active, out var candidate) && candidate.CanInteractNow &&
                target.TrySetActiveFromNpc(NetworkObject, active);
        }
    }
}
