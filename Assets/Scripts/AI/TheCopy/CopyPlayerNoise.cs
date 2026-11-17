using System;
using TheCopy.Shared;
using Unity.Netcode;

namespace TheCopy.AI
{
    public static class CopyPlayerNoise
    {
        internal static event Action<NetworkManager, ulong, NoiseEvent> Received;

        // Call from a server RPC with NGO's Receive.SenderClientId, never an ID in the payload.
        public static bool TryPublish(NetworkManager server, ulong senderClientId, NoiseSource source, float loudness)
        {
            if (server == null || !server.IsServer ||
                (source != NoiseSource.PlayerVoice && source != NoiseSource.PlayerFootstep) ||
                !(loudness > 0f && loudness <= 1f) ||
                !server.ConnectedClients.TryGetValue(senderClientId, out NetworkClient client))
            {
                return false;
            }

            NetworkObject player = client.PlayerObject;
            if (player == null || !player.IsSpawned || player.OwnerClientId != senderClientId)
                return false;

            Received?.Invoke(server, senderClientId, new NoiseEvent
            {
                Position = player.transform.position,
                Loudness = loudness,
                Source = source
            });
            return true;
        }
    }
}
