using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TheCopy.AI
{
    // Opt-in, separate training actor. No physical action or production inference replacement.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CopyDecisionInputV2), typeof(BehaviorParameters))]
    public sealed class CopyAttentionTrainingAgent : Agent
    {
        private readonly float[] _frame = new float[50];
        private readonly bool[] _mask = new bool[7];
        private bool _armed, _registered, _controlledTest, _pending, _finishing;
        private double _capturedAt;
        private CopyDecisionCandidatesV2 _candidates;
        public bool IsTrainingActive => _registered;
        public bool HasPendingDecision => _pending;
        public int RewardedEpisodes { get; private set; }
        public int InterruptedEpisodes { get; private set; }
        public int ResetCount { get; private set; }
        public float? LastAssignedReward { get; private set; }
        public float LastEpisodeCumulativeReward { get; private set; }
        public string LastTermination { get; private set; }
        public int? LastSelectedAction { get; private set; }
        public CopyDecisionTarget? LastSelectedTarget { get; private set; }
        public bool IsControlledTest => _controlledTest;
        public event Action<float?> EpisodeFinished;

        private bool HasAuthority => TryGetComponent(out CopyDecisionInputV2 input) && input.IsSpawned &&
            input.IsServer && input.isActiveAndEnabled && input.NetworkObject.IsOwnedByServer &&
            !input.NetworkObject.IsPlayerObject;

        private bool Configured
        {
            get
            {
                var behavior = GetComponent<BehaviorParameters>();
                var brain = behavior.BrainParameters;
                var spec = brain.ActionSpec;
                return !TryGetComponent<CopyDecisionAgent>(out _) && !TryGetComponent<DecisionRequester>(out _) &&
                    behavior.Model == null && behavior.BehaviorName == "TheCopyDraft2" &&
                    brain.VectorObservationSize == 50 && brain.NumStackedVectorObservations == 1 &&
                    spec.NumContinuousActions == 0 && spec.BranchSizes != null && spec.BranchSizes.Length == 1 &&
                    spec.BranchSizes[0] == 7 && MaxStep == 0 && GetComponentsInChildren<SensorComponent>(true).Length == 0 &&
                    GetComponentsInChildren<ActuatorComponent>(true).Length == 0 &&
                    behavior.BehaviorType == (_controlledTest ? BehaviorType.HeuristicOnly : BehaviorType.Default);
            }
        }

        public bool TryActivateTraining(bool controlledHeuristicTest = false)
        {
            if (_registered) return false;
            _controlledTest = controlledHeuristicTest;
            if (!HasAuthority || !Configured) return false;
            if (!_controlledTest && !Academy.Instance.IsCommunicatorOn) return false;
            _armed = true;
            enabled = true;
            return _registered;
        }

        protected override void OnEnable()
        {
            if (!_armed || !HasAuthority || !Configured) { enabled = false; return; }
            _registered = true;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            InterruptPending("disabled");
            if (_registered) base.OnDisable();
            _registered = false;
            _armed = false;
        }

        private void Update()
        {
            if (_registered && (!HasAuthority || !Configured)) DeactivateTraining();
        }

        public void DeactivateTraining()
        {
            _armed = false;
            enabled = false;
        }

        public override void OnEpisodeBegin()
        {
            // Coordinator prepares the next recipe after this reset, then explicitly requests one decision.
            // A trainer/Academy reset can arrive with an outstanding request; do not fabricate reward.
            bool interrupted = _pending;
            ClearFrame();
            ClearSoundMemory();
            ResetCount++;
            if (interrupted)
            {
                InterruptedEpisodes++;
                LastAssignedReward = null;
                LastTermination = "academy_reset";
                bool wasFinishing = _finishing;
                _finishing = true;
                try { EpisodeFinished?.Invoke(null); }
                finally { _finishing = wasFinishing; }
            }
        }

        private void ClearSoundMemory()
        {
            if (!TryGetComponent(out CopyHearing hearing)) return;
            bool active = hearing.enabled;
            hearing.enabled = false;
            hearing.enabled = active;
        }

        public bool TryBeginPreparedEpisode()
        {
            if (!_registered || !isActiveAndEnabled || !HasAuthority || !Configured || _pending || _finishing)
                return false;
            if (!GetComponent<CopyDecisionInputV2>().TryCapture(_frame, out var candidates)) return false;
            for (int i = 0; i < 7; i++) _mask[i] = candidates.IsAllowed((CopyDecisionAction)i);
            if (!CopyAttentionReward.TryEvaluate(_frame, _mask, 0, out _)) { ClearFrame(); return false; }
            _capturedAt = candidates.CapturedAt;
            LastAssignedReward = null;
            LastSelectedAction = null;
            LastSelectedTarget = null;
            _candidates = candidates;
            _pending = true;
            RequestDecision();
            return true;
        }

        public bool TryCopyPendingFrame(out float[] observations, out bool[] mask)
        {
            observations = null; mask = null;
            if (!_pending) return false;
            observations = (float[])_frame.Clone();
            mask = (bool[])_mask.Clone();
            return true;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (_pending && (!HasAuthority || !Configured)) InterruptPending("authority_or_configuration");
            sensor.AddObservation(_frame);
        }

        public override void WriteDiscreteActionMask(IDiscreteActionMask mask)
        {
            for (int i = 0; i < 7; i++) mask.SetActionEnabled(0, i, _pending && HasAuthority && Configured ? _mask[i] : i == 0);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!_pending) return;
            if (!_registered || !HasAuthority || !Configured) { InterruptPending("authority_or_configuration"); return; }
            if (Time.timeAsDouble - _capturedAt > 0.5) { InterruptPending("stale"); return; }
            int selected = actions.DiscreteActions.Length == 1 && actions.ContinuousActions.Length == 0
                ? actions.DiscreteActions[0] : int.MinValue;
            if (!CopyAttentionReward.TryEvaluate(_frame, _mask, selected, out float reward))
            { InterruptPending("invalid_frame"); return; }
            _finishing = true;
            _pending = false;
            _capturedAt = 0;
            try
            {
                AddReward(reward);
                LastAssignedReward = reward;
                LastSelectedAction = selected == int.MinValue ? (int?)null : selected;
                LastSelectedTarget = _candidates.TryGetTarget((CopyDecisionAction)selected, out var target)
                    ? target : (CopyDecisionTarget?)null;
                LastEpisodeCumulativeReward = GetCumulativeReward();
                LastTermination = "decision";
                RewardedEpisodes++;
                EndEpisode();
                EpisodeFinished?.Invoke(reward);
            }
            finally { _finishing = false; }
        }

        public void InterruptPending(string reason = "cancelled")
        {
            if (!_pending) return;
            _finishing = true;
            _pending = false;
            LastAssignedReward = null;
            LastEpisodeCumulativeReward = GetCumulativeReward();
            LastTermination = reason;
            InterruptedEpisodes++;
            try
            {
                ClearFrame();
                if (_registered) EpisodeInterrupted();
                EpisodeFinished?.Invoke(null);
            }
            finally { _finishing = false; }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            if (!_controlledTest) throw new InvalidOperationException("Training requires an active ML-Agents communicator");
            var discrete = actionsOut.DiscreteActions;
            discrete[0] = 0; // Explicit fixture control only; never a gameplay policy or an oracle.
        }

        private void ClearFrame()
        {
            _pending = false;
            _capturedAt = 0;
            Array.Clear(_frame, 0, _frame.Length);
            Array.Clear(_mask, 0, _mask.Length);
        }
    }
}
