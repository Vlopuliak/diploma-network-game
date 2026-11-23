using System;
using Unity.InferenceEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TheCopy.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CopyDecisionInputV2), typeof(BehaviorParameters))]
    public sealed class CopyDecisionAgent : Agent
    {
        private readonly float[] _observations = new float[CopyDecisionEncoderV2.ObservationCount];
        private readonly float[] _fresh = new float[CopyDecisionEncoderV2.ObservationCount];
        private CopyDecisionCandidatesV2 _frame;
        private bool _registered;
        private bool _pending;
        private long _requestId;
        private double _requestTime;
        private double _responseLatencyMs;
        private ModelAsset _checkedModel;
        private bool _modelCompatible;
        public string ModelValidationIssue { get; private set; }
        public bool IsInitializedOnServer => _registered;
        public bool HasPendingDecision => _pending;
        public int AcceptedDecisions { get; private set; }
        public int DiscardedDecisions { get; private set; }
        public event Action<CopyDecisionAction, CopyDecisionTarget?> DecisionAccepted;
        public event Action<CopyDecisionTrace> DecisionCompleted;

        private bool HasAuthority => TryGetComponent(out CopyDecisionInputV2 input) &&
            input.IsSpawned && input.IsServer && input.isActiveAndEnabled;

        public bool HasValidConfiguration
        {
            get
            {
                var behavior = GetComponent<BehaviorParameters>();
                var brain = behavior.BrainParameters;
                var actions = brain.ActionSpec;
                return behavior.Model != null && behavior.BehaviorType == BehaviorType.InferenceOnly &&
                    brain.VectorObservationSize == 50 && brain.NumStackedVectorObservations == 1 &&
                    actions.NumContinuousActions == 0 && actions.BranchSizes != null &&
                    actions.BranchSizes.Length == 1 && actions.BranchSizes[0] == 7 && IsModelCompatible(behavior.Model);
            }
        }

        private bool IsModelCompatible(ModelAsset model)
        {
            if (_checkedModel == model) return _modelCompatible;
            _checkedModel = model;
            try
            {
                _modelCompatible = CopyDecisionModelValidator.TryValidate(ModelLoader.Load(model), out string reason);
                ModelValidationIssue = reason;
            }
            catch (Exception error)
            {
                _modelCompatible = false;
                ModelValidationIssue = error.Message;
            }
            return _modelCompatible;
        }

        protected override void OnEnable()
        {
            // Before NGO spawn and on client replicas, never initialize the Academy/model runner.
            if (!HasAuthority || !HasValidConfiguration) { enabled = false; return; }
            _registered = true;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            CancelPendingDecision("disabled_or_despawned");
            if (_registered) base.OnDisable();
            _registered = false;
            _checkedModel = null;
        }

        public override void OnEpisodeBegin() => CancelPendingDecision("episode_reset");

        public void CancelPendingDecision(string reason = "cancelled")
        {
            bool wasPending = _pending;
            var cancelled = new CopyDecisionTrace(_requestId, _frame, null, null, "no_response", reason, null);
            _pending = false;
            _frame = default;
            Array.Clear(_observations, 0, _observations.Length);
            if (wasPending) DecisionCompleted?.Invoke(cancelled);
        }

        public bool TryRequestDecision()
        {
            if (!isActiveAndEnabled || !_registered || !HasAuthority || !HasValidConfiguration || _pending ||
                !GetComponent<CopyDecisionInputV2>().TryCapture(_observations, out _frame))
                return false;
            _pending = true;
            _requestId++;
            _requestTime = Time.realtimeSinceStartupAsDouble;
            RequestDecision();
            return true;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (!HasAuthority) CancelPendingDecision("authority_lost");
            sensor.AddObservation(_observations);
        }

        public override void WriteDiscreteActionMask(IDiscreteActionMask mask)
        {
            for (int i = 0; i < CopyDecisionCandidatesV2.ActionCount; i++)
                mask.SetActionEnabled(0, i, HasAuthority && _pending ? _frame.IsAllowed((CopyDecisionAction)i) : i == 0);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!_pending) return;
            _responseLatencyMs = (Time.realtimeSinceStartupAsDouble - _requestTime) * 1000d;
            int? selected = actions.DiscreteActions.Length == 1 ? actions.DiscreteActions[0] : (int?)null;
            CopyDecisionTarget? selectedTarget = selected.HasValue && _frame.TryGetTarget((CopyDecisionAction)selected.Value, out var captured)
                ? captured : (CopyDecisionTarget?)null;
            if (!isActiveAndEnabled || !HasAuthority)
            { Finish(selected, selectedTarget, "rejected", "authority_denied"); return; }
            if (!HasValidConfiguration)
            { Finish(selected, selectedTarget, "rejected", "configuration_invalid"); return; }
            if (!selected.HasValue || actions.ContinuousActions.Length != 0)
            { Finish(selected, selectedTarget, "rejected", "malformed"); return; }
            if (Time.timeAsDouble - _frame.CapturedAt > 0.5)
            { Finish(selected, selectedTarget, "rejected", "stale"); return; }
            if (!GetComponent<CopyDecisionInputV2>().TryCapture(_fresh, out var current))
            { Finish(selected, selectedTarget, "rejected", "perception_unavailable"); return; }
            int index = selected.Value;
            var action = (CopyDecisionAction)index;
            if (index < 0 || index >= 7 || !_frame.IsAllowed(action) || !current.IsAllowed(action))
            { Finish(selected, selectedTarget, "rejected", "masked_or_unavailable"); return; }
            CopyDecisionTarget? acceptedTarget = null;
            if (action != CopyDecisionAction.Continue)
            {
                if (!_frame.TryGetTarget(action, out var previous) || !current.TryGetTarget(action, out var now) ||
                    previous.SourceClientId != now.SourceClientId || previous.Position != now.Position)
                { Finish(selected, selectedTarget, "rejected", "target_changed"); return; }
                acceptedTarget = previous;
            }
            AcceptedDecisions++;
            Finish(index, acceptedTarget, "accepted", null);
            DecisionAccepted?.Invoke(action, acceptedTarget);
        }

        private void Finish(int? action, CopyDecisionTarget? target, string status, string reason)
        {
            var trace = new CopyDecisionTrace(_requestId, _frame, action, target, status, reason,
                _responseLatencyMs);
            _pending = false;
            _frame = default;
            Array.Clear(_observations, 0, _observations.Length);
            if (status == "rejected") DiscardedDecisions++;
            DecisionCompleted?.Invoke(trace);
        }
    }
}
