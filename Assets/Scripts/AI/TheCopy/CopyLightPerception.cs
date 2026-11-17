using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    public readonly struct CopyLightObservation
    {
        public ulong SourceClientId { get; }
        public Vector3 Position { get; }
        public float Strength { get; }
        public bool IsAimApproximate { get; }

        internal CopyLightObservation(ulong sourceClientId, Vector3 position, float strength, bool isAimApproximate)
        {
            SourceClientId = sourceClientId;
            Position = position;
            Strength = strength;
            IsAimApproximate = isAimApproximate;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CopyLightPerception : NetworkBehaviour
    {
        [SerializeField, Min(0f)] private float eyeHeight = 0.6f;
        [SerializeField, Tooltip("Select opaque scene layers. The NPC and source player's colliders are ignored.")]
        private LayerMask occlusionLayers;

        public bool TryGetStrongestLight(out CopyLightObservation observation)
        {
            observation = default;
            if (!IsSpawned || !IsServer || !isActiveAndEnabled)
                return false;

            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                NetworkObject playerObject = client.PlayerObject;
                if (playerObject == null || !playerObject.IsSpawned ||
                    !playerObject.TryGetComponent(out PlayerController player) || !player.IsFlashlightOn.Value)
                {
                    continue;
                }

                // The current player prefab exposes one spot light below the synchronized camera holder.
                Light flashlight = playerObject.GetComponentInChildren<Light>();
                if (flashlight == null || flashlight.type != LightType.Spot)
                    continue;

                Vector3 origin = flashlight.transform.position;
                Vector3 offset = eye - origin;
                float distance = offset.magnitude;
                if (distance >= flashlight.range)
                    continue;

                float strength = 1f - distance / flashlight.range;
                if (strength <= observation.Strength)
                    continue;

                float halfAngle = flashlight.spotAngle * 0.5f * Mathf.Deg2Rad;
                if (distance > 0.001f &&
                    Vector3.Dot(flashlight.transform.forward, offset / distance) < Mathf.Cos(halfAngle))
                {
                    continue;
                }

                if (CopyPerceptionOcclusion.IsBlocked(origin, eye, occlusionLayers, transform, playerObject.transform))
                    continue;

                observation = new CopyLightObservation(client.ClientId, origin, strength, !player.HasServerCameraPose);
            }

            return observation.Strength > 0f;
        }
    }
}
