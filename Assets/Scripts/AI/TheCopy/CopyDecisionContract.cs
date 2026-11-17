using System;
using TheCopy.Shared;
using UnityEngine;

namespace TheCopy.AI
{
    public enum CopyDecisionAction
    {
        Continue = 0,
        InvestigateSound = 1,
        InvestigateLight = 2,
        ObservePlayerA = 3,
        ObservePlayerB = 4,
        AssistMechanism = 5,
        ObstructMechanism = 6
    }

    public readonly struct CopyDecisionTarget
    {
        public Vector3 Position { get; }
        public ulong? SourceClientId { get; }

        internal CopyDecisionTarget(Vector3 position, ulong? sourceClientId)
        {
            Position = position;
            SourceClientId = sourceClientId;
        }
    }

    public readonly struct CopyDecisionCandidates
    {
        public const int ActionCount = 7;
        public double CapturedAt { get; }
        private readonly CopyDecisionTarget? _sound;
        private readonly CopyDecisionTarget? _light;

        internal CopyDecisionCandidates(double capturedAt, CopyDecisionTarget? sound, CopyDecisionTarget? light)
        {
            CapturedAt = capturedAt;
            _sound = sound;
            _light = light;
        }

        public bool IsAllowed(CopyDecisionAction action) => action == CopyDecisionAction.Continue || TryGetTarget(action, out _);

        public bool TryGetTarget(CopyDecisionAction action, out CopyDecisionTarget target)
        {
            target = default;
            switch (action)
            {
                case CopyDecisionAction.InvestigateSound:
                    if (!_sound.HasValue)
                        return false;
                    target = _sound.Value;
                    return true;
                case CopyDecisionAction.InvestigateLight:
                    if (!_light.HasValue)
                        return false;
                    target = _light.Value;
                    return true;
                default:
                    return false;
            }
        }
    }

    public static class CopyDecisionEncoder
    {
        public const string SchemaId = "the-copy.decision-input/draft-1";
        public const int ObservationCount = 40;
        public const float DistanceScale = 18f;

        public static bool TryEncode(CopyPerceptionSnapshot snapshot, Vector3 observerPosition,
            Quaternion observerRotation, float[] observations, out CopyDecisionCandidates candidates)
        {
            ClearObservations(observations);
            candidates = default;
            float rotationNorm = Quaternion.Dot(observerRotation, observerRotation);
            if (!double.IsFinite(snapshot.CapturedAt) || snapshot.CapturedAt < 0 || !Finite(observerPosition) ||
                !float.IsFinite(rotationNorm) || Mathf.Abs(rotationNorm - 1f) > 0.001f ||
                !ValidSound(snapshot.LiveSound, observerPosition) || !ValidSound(snapshot.RememberedSound, observerPosition) ||
                !ValidLight(snapshot.Light, observerPosition))
                return false;

            Quaternion inverse = Quaternion.Inverse(observerRotation);
            WriteSound(snapshot.LiveSound, 0, observerPosition, inverse, observations);
            WriteSound(snapshot.RememberedSound, 12, observerPosition, inverse, observations);
            if (snapshot.Light.HasValue)
            {
                var light = snapshot.Light.Value;
                observations[24] = 1f;
                observations[25] = light.Strength;
                WritePosition(light.Position, 26, observerPosition, inverse, observations);
                observations[30] = light.IsAimApproximate ? 1f : 0f;
            }
            var sound = snapshot.RememberedSound ?? snapshot.LiveSound;
            if (sound.HasValue && sound.Value.SourceClientId.HasValue && snapshot.Light.HasValue &&
                sound.Value.SourceClientId.Value == snapshot.Light.Value.SourceClientId)
                observations[31] = 1f;

            if (snapshot.Context.HasValue)
            {
                var context = snapshot.Context.Value;
                WriteFact(context.ZoneTriggered, 32, observations);
                WriteFact(context.PowerOn, 34, observations);
                WriteFact(context.AirlockOpen, 36, observations);
                WriteFact(context.KeypadUnlocked, 38, observations);
            }

            CopyDecisionTarget? soundTarget = sound.HasValue
                ? new CopyDecisionTarget(sound.Value.Position, sound.Value.SourceClientId) : (CopyDecisionTarget?)null;
            CopyDecisionTarget? lightTarget = snapshot.Light.HasValue && !snapshot.Light.Value.IsAimApproximate
                ? new CopyDecisionTarget(snapshot.Light.Value.Position, snapshot.Light.Value.SourceClientId) : (CopyDecisionTarget?)null;
            candidates = new CopyDecisionCandidates(snapshot.CapturedAt, soundTarget, lightTarget);
            return true;
        }

        internal static void ClearObservations(float[] observations)
        {
            if (observations == null)
                throw new ArgumentNullException(nameof(observations));
            if (observations.Length != ObservationCount)
                throw new ArgumentException("Expected exactly 40 observations for " + SchemaId, nameof(observations));
            Array.Clear(observations, 0, observations.Length);
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static bool ValidPosition(Vector3 position, Vector3 origin) =>
            Finite(position) && float.IsFinite((position - origin).sqrMagnitude);

        private static bool ValidSound(CopySoundObservation? sound, Vector3 origin)
        {
            if (!sound.HasValue)
                return true;
            var value = sound.Value;
            return ValidPosition(value.Position, origin) && value.Strength > 0f && value.Strength <= 1f &&
                value.Freshness > 0f && value.Freshness <= 1f && (byte)value.Source <= (byte)NoiseSource.Environmental;
        }

        private static bool ValidLight(CopyLightObservation? light, Vector3 origin) =>
            !light.HasValue || (ValidPosition(light.Value.Position, origin) && light.Value.Strength > 0f && light.Value.Strength <= 1f);

        private static void WriteSound(CopySoundObservation? sound, int offset, Vector3 origin,
            Quaternion inverse, float[] observations)
        {
            if (!sound.HasValue)
                return;
            var value = sound.Value;
            observations[offset] = 1f;
            observations[offset + 1] = value.Strength;
            observations[offset + 2] = value.Freshness;
            WritePosition(value.Position, offset + 3, origin, inverse, observations);
            observations[offset + 7 + (int)value.Source] = 1f;
            observations[offset + 11] = value.SourceClientId.HasValue ? 1f : 0f;
        }

        private static void WritePosition(Vector3 position, int offset, Vector3 origin,
            Quaternion inverse, float[] observations)
        {
            Vector3 local = inverse * (position - origin);
            observations[offset] = Mathf.Clamp(local.x / DistanceScale, -1f, 1f);
            observations[offset + 1] = Mathf.Clamp(local.y / DistanceScale, -1f, 1f);
            observations[offset + 2] = Mathf.Clamp(local.z / DistanceScale, -1f, 1f);
            observations[offset + 3] = Mathf.Clamp01(local.magnitude / DistanceScale);
        }

        private static void WriteFact(bool? value, int offset, float[] observations)
        {
            observations[offset] = value.HasValue ? 1f : 0f;
            observations[offset + 1] = value == true ? 1f : 0f;
        }
    }
}
