using System;
using UnityEngine;

namespace TheCopy.AI
{
    public readonly struct CopyDecisionSnapshotV2
    {
        public const string SchemaId = "the-copy.decision-snapshot/sensory-intent-v1";
        public CopyPerceptionSnapshot Perception { get; }
        public CopyPlayerObservation? PlayerA { get; }
        public CopyPlayerObservation? PlayerB { get; }

        internal CopyDecisionSnapshotV2(CopyPerceptionSnapshot perception, CopyPlayerObservation? playerA,
            CopyPlayerObservation? playerB)
        {
            Perception = perception;
            PlayerA = playerA;
            PlayerB = playerB;
        }
    }

    public readonly struct CopyDecisionCandidatesV2
    {
        public const int ActionCount = 7;
        public double CapturedAt => _sensory.CapturedAt;
        private readonly bool _valid;
        private readonly CopyDecisionCandidates _sensory;
        private readonly CopyPlayerObservation? _playerA;
        private readonly CopyPlayerObservation? _playerB;

        internal CopyDecisionCandidatesV2(CopyDecisionCandidates sensory, CopyPlayerObservation? playerA,
            CopyPlayerObservation? playerB)
        {
            _valid = true;
            _sensory = sensory;
            _playerA = playerA;
            _playerB = playerB;
        }

        public bool IsAllowed(CopyDecisionAction action) => _valid &&
            (action == CopyDecisionAction.Continue || TryGetTarget(action, out _));

        public bool TryGetTarget(CopyDecisionAction action, out CopyDecisionTarget target)
        {
            target = default;
            if (!_valid)
                return false;
            CopyPlayerObservation? player = action == CopyDecisionAction.ObservePlayerA ? _playerA :
                action == CopyDecisionAction.ObservePlayerB ? _playerB : null;
            if (player.HasValue)
            {
                target = new CopyDecisionTarget(player.Value.Position, player.Value.SourceClientId);
                return true;
            }
            return _sensory.TryGetTarget(action, out target);
        }
    }

    public static class CopyDecisionEncoderV2
    {
        public const string SchemaId = "the-copy.decision-input/sensory-intent-v1";
        public const int ObservationCount = 50;

        public static bool TryEncode(CopyDecisionSnapshotV2 snapshot, Vector3 observerPosition,
            Quaternion observerRotation, float[] observations, out CopyDecisionCandidatesV2 candidates)
        {
            ClearObservations(observations);
            candidates = default;
            double time = snapshot.Perception.CapturedAt;
            if (!ValidPlayer(snapshot.PlayerA, time, observerPosition) || !ValidPlayer(snapshot.PlayerB, time, observerPosition) ||
                (snapshot.PlayerA.HasValue && snapshot.PlayerB.HasValue &&
                 (snapshot.PlayerA.Value.SourceClientId == snapshot.PlayerB.Value.SourceClientId ||
                  snapshot.PlayerA.Value.NetworkObjectId == snapshot.PlayerB.Value.NetworkObjectId)))
                return false;
            if (!CopyDecisionEncoder.TryEncodeCore(snapshot.Perception, observerPosition, observerRotation, observations, out var sensory))
                return false;

            Quaternion inverse = Quaternion.Inverse(observerRotation);
            WritePlayer(snapshot.PlayerA, 40, observerPosition, inverse, observations);
            WritePlayer(snapshot.PlayerB, 45, observerPosition, inverse, observations);
            candidates = new CopyDecisionCandidatesV2(sensory, snapshot.PlayerA, snapshot.PlayerB);
            return true;
        }

        internal static void ClearObservations(float[] observations)
        {
            if (observations == null)
                throw new ArgumentNullException(nameof(observations));
            if (observations.Length != ObservationCount)
                throw new ArgumentException("Expected exactly 50 observations for " + SchemaId, nameof(observations));
            Array.Clear(observations, 0, observations.Length);
        }

        private static bool ValidPlayer(CopyPlayerObservation? observation, double time, Vector3 origin)
        {
            if (!observation.HasValue)
                return true;
            var value = observation.Value;
            return value.CapturedAt == time && float.IsFinite(value.Distance) && value.Distance >= 0f &&
                float.IsFinite(value.Position.x) && float.IsFinite(value.Position.y) && float.IsFinite(value.Position.z) &&
                float.IsFinite((value.Position - origin).sqrMagnitude);
        }

        private static void WritePlayer(CopyPlayerObservation? player, int offset, Vector3 origin,
            Quaternion inverse, float[] observations)
        {
            if (!player.HasValue)
                return;
            observations[offset] = 1f;
            CopyDecisionEncoder.WritePosition(player.Value.Position, offset + 1, origin, inverse, observations);
        }
    }
}
