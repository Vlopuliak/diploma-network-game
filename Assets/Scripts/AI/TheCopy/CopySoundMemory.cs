using TheCopy.Shared;
using UnityEngine;

namespace TheCopy.AI
{
    public readonly struct CopySoundObservation
    {
        public Vector3 Position { get; }
        public NoiseSource Source { get; }
        public ulong? SourceClientId { get; }
        public float Strength { get; }
        public float Freshness { get; }

        internal CopySoundObservation(
            Vector3 position, NoiseSource source, float strength, float freshness, ulong? sourceClientId)
        {
            Position = position;
            Source = source;
            SourceClientId = sourceClientId;
            Strength = strength;
            Freshness = freshness;
        }
    }

    internal sealed class CopySoundMemory
    {
        private CopySoundObservation _lastSound;
        private double _heardAt;

        public void Remember(NoiseEvent noise, float strength, double now, ulong? sourceClientId)
        {
            _lastSound = new CopySoundObservation(noise.Position, noise.Source, strength, 1f, sourceClientId);
            _heardAt = now;
        }

        public bool TryGetLive(double now, float duration, out CopySoundObservation sound)
        {
            sound = default;
            if (_lastSound.Strength <= 0f || now - _heardAt >= duration)
                return false;

            sound = _lastSound;
            return true;
        }

        public bool TryRecall(double now, float lifetime, out CopySoundObservation sound)
        {
            sound = default;
            if (_lastSound.Strength <= 0f)
                return false;

            float freshness = Mathf.Clamp01(1f - (float)((now - _heardAt) / lifetime));
            if (freshness <= 0f)
                return false;

            sound = new CopySoundObservation(
                _lastSound.Position, _lastSound.Source, _lastSound.Strength * freshness, freshness,
                _lastSound.SourceClientId);
            return true;
        }

        public void Clear()
        {
            _lastSound = default;
            _heardAt = 0;
        }
    }
}
