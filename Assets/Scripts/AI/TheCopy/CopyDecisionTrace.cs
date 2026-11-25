using System;

namespace TheCopy.AI
{
    public readonly struct CopyDecisionTrace
    {
        public long RequestId { get; }
        public CopyDecisionCandidatesV2 Candidates { get; }
        public int? Action { get; }
        public CopyDecisionTarget? Target { get; }
        public string Status { get; }
        public string Reason { get; }
        public double? LatencyMs { get; }
        private readonly float[] _observations;

        // Caller receives an independent copy, never the Agent's pending buffer or trace storage.
        public float[] CopyObservations() => _observations == null ? Array.Empty<float>() : (float[])_observations.Clone();

        internal CopyDecisionTrace(long requestId, CopyDecisionCandidatesV2 candidates, int? action,
            CopyDecisionTarget? target, string status, string reason, double? latencyMs, float[] observations)
        {
            RequestId = requestId;
            Candidates = candidates;
            Action = action;
            Target = target;
            Status = status;
            Reason = reason;
            LatencyMs = latencyMs;
            _observations = (float[])observations.Clone();
        }
    }
}
