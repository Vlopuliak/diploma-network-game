using Unity.Netcode;
using UnityEngine;

namespace TheCopy.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CopyDecisionAgent))]
    public sealed class CopyDecisionAgentHost : NetworkBehaviour
    {
        [SerializeField] private bool automaticDecisions;
        [SerializeField, Min(0.02f)] private float decisionInterval = 0.2f;
        private float _nextDecision;
        private CopyDecisionAgent Agent => GetComponent<CopyDecisionAgent>();
        public override void OnNetworkSpawn() => Refresh();
        public override void OnNetworkDespawn() => Agent.enabled = false;
        private void OnEnable() { if (IsSpawned) Refresh(); }
        private void OnDisable() { if (TryGetComponent(out CopyDecisionAgent agent)) agent.enabled = false; }
        private void Refresh()
        {
            Agent.enabled = IsServer && IsSpawned && isActiveAndEnabled && Agent.HasValidConfiguration;
            _nextDecision = Time.time;
        }
        public bool TryRequestDecision() => IsServer && IsSpawned && isActiveAndEnabled && Agent.TryRequestDecision();
        private void Update()
        {
            if (!automaticDecisions || Time.time < _nextDecision) return;
            _nextDecision = Time.time + Mathf.Max(0.02f, decisionInterval);
            TryRequestDecision();
        }
    }
}
