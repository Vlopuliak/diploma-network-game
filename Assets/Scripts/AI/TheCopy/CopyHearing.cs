using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CopyHearing : NetworkBehaviour, INoiseListener
    {
        private const float LiveDuration = 0.18f;

        [SerializeField, Min(0.1f)] private float hearingRadius = 18f;
        [SerializeField, Min(0f)] private float earHeight = 0.6f;
        [SerializeField, Min(LiveDuration)] private float memoryLifetime = 4f;
        [SerializeField, Range(0f, 1f)] private float occludedStrengthMultiplier = 0.3f;
        [SerializeField, Tooltip("Select sound occluders. The NPC and identified source player's colliders are ignored.")]
        private LayerMask occlusionLayers;

        private readonly CopySoundMemory _memory = new CopySoundMemory();
        public float MemoryLifetime => Mathf.Max(LiveDuration, memoryLifetime);
        public float LiveWindow => LiveDuration;

        private bool CanListen => IsServer && IsSpawned && isActiveAndEnabled;

        private void OnEnable()
        {
            if (CanListen)
                StartListening();
        }

        private void OnDisable()
        {
            StopListening();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (CanListen)
                StartListening();
        }

        public override void OnNetworkDespawn()
        {
            StopListening();
            base.OnNetworkDespawn();
        }

        public void OnNoise(NoiseEvent noiseEvent)
        {
            Hear(noiseEvent, null);
        }

        private void OnPlayerNoise(NetworkManager server, ulong sourceClientId, NoiseEvent noiseEvent)
        {
            if (server == NetworkManager)
                Hear(noiseEvent, sourceClientId);
        }

        private void Hear(NoiseEvent noiseEvent, ulong? sourceClientId)
        {
            if (!CanListen)
                return;

            Vector3 ear = transform.position + Vector3.up * earHeight;
            float distance = Vector3.Distance(ear, noiseEvent.Position);
            if (!(distance < hearingRadius) || !(noiseEvent.Loudness > 0f))
                return;

            float strength = Mathf.Clamp01(noiseEvent.Loudness) * (1f - distance / hearingRadius);
            Transform source = null;
            if (sourceClientId.HasValue && NetworkManager.ConnectedClients.TryGetValue(sourceClientId.Value, out var client))
                source = client.PlayerObject != null ? client.PlayerObject.transform : null;

            if (CopyPerceptionOcclusion.IsBlocked(ear, noiseEvent.Position, occlusionLayers, transform, source))
            {
                strength *= occludedStrengthMultiplier;
            }

            if (strength > 0f)
                _memory.Remember(noiseEvent, strength, Time.timeAsDouble, sourceClientId);
        }

        public bool TryGetLiveSound(out CopySoundObservation sound)
        {
            sound = default;
            return CanListen && _memory.TryGetLive(Time.timeAsDouble, LiveDuration, out sound);
        }

        public bool TryGetRememberedSound(out CopySoundObservation sound)
        {
            sound = default;
            return CanListen && _memory.TryRecall(
                Time.timeAsDouble, Mathf.Max(LiveDuration, memoryLifetime), out sound);
        }

        private void StartListening()
        {
            NoiseDispatcher.Register(this);
            CopyPlayerNoise.Received -= OnPlayerNoise;
            CopyPlayerNoise.Received += OnPlayerNoise;
        }

        private void StopListening()
        {
            NoiseDispatcher.Unregister(this);
            CopyPlayerNoise.Received -= OnPlayerNoise;
            _memory.Clear();
        }
    }
}
