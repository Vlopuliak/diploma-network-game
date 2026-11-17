using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    public readonly struct CopyPerceptionSnapshot
    {
        public const string SchemaId = "the-copy.perception/1";
        public double CapturedAt { get; }
        public CopySoundObservation? LiveSound { get; }
        public CopySoundObservation? RememberedSound { get; }
        public CopyLightObservation? Light { get; }
        public CopySceneSnapshot? Context { get; }

        internal CopyPerceptionSnapshot(double capturedAt, CopySoundObservation? liveSound,
            CopySoundObservation? rememberedSound, CopyLightObservation? light, CopySceneSnapshot? context)
        {
            CapturedAt = capturedAt;
            LiveSound = liveSound;
            RememberedSound = rememberedSound;
            Light = light;
            Context = context;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CopyPerception : NetworkBehaviour
    {
        public bool TryCapture(out CopyPerceptionSnapshot snapshot)
        {
            snapshot = default;
            if (!IsServer || !IsSpawned || !isActiveAndEnabled)
                return false;

            CopySoundObservation? live = null;
            CopySoundObservation? memory = null;
            CopyLightObservation? light = null;
            CopySceneSnapshot? context = null;
            if (TryGetComponent(out CopyHearing hearing))
            {
                if (hearing.TryGetLiveSound(out var liveSound))
                    live = liveSound;
                if (hearing.TryGetRememberedSound(out var rememberedSound))
                    memory = rememberedSound;
            }
            if (TryGetComponent(out CopyLightPerception lightSensor) && lightSensor.TryGetStrongestLight(out var illumination))
                light = illumination;
            if (TryGetComponent(out CopySceneContext sceneContext) && sceneContext.TryRead(out var scene))
                context = scene;

            snapshot = new CopyPerceptionSnapshot(Time.timeAsDouble, live, memory, light, context);
            return true;
        }
    }
}
