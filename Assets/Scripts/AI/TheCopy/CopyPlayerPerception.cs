using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    public readonly struct CopyPlayerObservation
    {
        public ulong SourceClientId { get; }
        public ulong NetworkObjectId { get; }
        public Vector3 Position { get; }
        public float Distance { get; }
        public double CapturedAt { get; }

        internal CopyPlayerObservation(ulong sourceClientId, ulong networkObjectId, Vector3 position,
            float distance, double capturedAt)
        {
            SourceClientId = sourceClientId;
            NetworkObjectId = networkObjectId;
            Position = position;
            Distance = distance;
            CapturedAt = capturedAt;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CopyPlayerPerception : NetworkBehaviour
    {
        [SerializeField, Min(0.1f)] private float sightRadius = 18f;
        [SerializeField, Range(1f, 360f)] private float fieldOfView = 120f;
        [SerializeField, Min(0f)] private float eyeHeight = 0.6f;
        [SerializeField, Tooltip("Opaque layers. Observer and target colliders are ignored; triggers do not occlude.")]
        private LayerMask occlusionLayers = Physics.DefaultRaycastLayers;

        // Client IDs address targets; connection alone never constitutes a visual observation.
        public bool TryObserve(ulong clientId, out CopyPlayerObservation observation)
        {
            observation = default;
            if (!IsSpawned || !IsServer || !isActiveAndEnabled ||
                !float.IsFinite(sightRadius) || sightRadius <= 0f ||
                !float.IsFinite(fieldOfView) || fieldOfView <= 0f || fieldOfView > 360f ||
                !float.IsFinite(eyeHeight) || eyeHeight < 0f ||
                !NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
                return false;

            var player = client.PlayerObject;
            if (player == null || !player.IsSpawned || player.NetworkManager != NetworkManager ||
                !player.gameObject.activeInHierarchy || !player.TryGetComponent<PlayerController>(out _) ||
                !player.TryGetComponent(out CapsuleCollider body) || !body.enabled)
                return false;

            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 position = body.transform.TransformPoint(body.center);
            Vector3 offset = position - eye;
            float distance = offset.magnitude;
            if (!Finite(eye) || !Finite(position) || !float.IsFinite(distance) || distance >= sightRadius)
                return false;
            if (distance > 0.001f && Vector3.Dot(transform.forward, offset / distance) <
                Mathf.Cos(fieldOfView * 0.5f * Mathf.Deg2Rad))
                return false;
            if (CopyPerceptionOcclusion.IsBlocked(eye, position, occlusionLayers, transform, player.transform))
                return false;

            observation = new CopyPlayerObservation(clientId, player.NetworkObjectId, position, distance, Time.timeAsDouble);
            return true;
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
