#if UNITY_EDITOR
using TheCopy.AI;
using TheCopy.Shared;
using Unity.Netcode;

namespace TheCopy.Tests
{
    public sealed class P305AcousticTestSender : NetworkBehaviour
    {
        public int ReceivedCount { get; private set; }
        public bool LastAccepted { get; private set; }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SendNoiseRpc(NoiseSource source, float loudness, RpcParams rpcParams = default)
        {
            LastAccepted = CopyPlayerNoise.TryPublish(NetworkManager, rpcParams.Receive.SenderClientId, source, loudness);
            ReceivedCount++;
        }
    }
}
#endif
