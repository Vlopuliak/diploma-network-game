using System;
using TheCopy.Networking;
using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    public readonly struct CopySceneSnapshot
    {
        public bool? ZoneTriggered { get; }
        public bool? PowerOn { get; }
        public bool? AirlockOpen { get; }
        public bool? KeypadUnlocked { get; }

        internal CopySceneSnapshot(bool? zoneTriggered, bool? powerOn, bool? airlockOpen, bool? keypadUnlocked)
        {
            ZoneTriggered = zoneTriggered;
            PowerOn = powerOn;
            AirlockOpen = airlockOpen;
            KeypadUnlocked = keypadUnlocked;
        }

        internal bool Matches(CopySceneSnapshot other)
        {
            return ZoneTriggered == other.ZoneTriggered && PowerOn == other.PowerOn &&
                AirlockOpen == other.AirlockOpen && KeypadUnlocked == other.KeypadUnlocked;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CopySceneContext : NetworkBehaviour
    {
        [SerializeField] private NetworkTriggerZone zone;
        [SerializeField] private NetworkInteractiveObject powerNode;
        [SerializeField] private NetworkAirlockDoor airlock;
        [SerializeField] private NetworkKeypadTerminal keypad;

        private CopySceneSnapshot? _previous;

        public event Action<CopySceneSnapshot> Changed;

        public bool TryRead(out CopySceneSnapshot snapshot)
        {
            snapshot = default;
            if (!IsServer || !IsSpawned || !isActiveAndEnabled)
                return false;

            snapshot = new CopySceneSnapshot(
                IsAvailable(zone) ? zone.HasTriggered.Value : (bool?)null,
                IsAvailable(powerNode) ? powerNode.IsActive.Value : (bool?)null,
                IsAvailable(airlock) ? airlock.IsDoorOpen.Value : (bool?)null,
                IsAvailable(keypad) ? keypad.IsUnlocked.Value : (bool?)null);
            return true;
        }

        private bool IsAvailable(NetworkBehaviour source)
        {
            return source != null && source.IsSpawned && source.NetworkManager == NetworkManager;
        }

        private void Update()
        {
            if (!TryRead(out var snapshot))
                return;

            bool changed = _previous.HasValue && !_previous.Value.Matches(snapshot);
            _previous = snapshot;
            if (changed)
                Changed?.Invoke(snapshot);
        }

        private void OnDisable() => _previous = null;

        public override void OnNetworkDespawn()
        {
            _previous = null;
            base.OnNetworkDespawn();
        }
    }
}
