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

        internal CopyDecisionTrace(long requestId, CopyDecisionCandidatesV2 candidates, int? action,
            CopyDecisionTarget? target, string status, string reason, double? latencyMs)
        {
            RequestId = requestId;
            Candidates = candidates;
            Action = action;
            Target = target;
            Status = status;
            Reason = reason;
            LatencyMs = latencyMs;
        }
    }
}
