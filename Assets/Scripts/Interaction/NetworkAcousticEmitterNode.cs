using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Акустичний генератор тонів усередині однієї з 4 тестових кабін Зони 4
/// (Бетон / Дерево / Метал / Акустичний поролон).
/// Гравець 2 активує потрібну кабіну та підбирає зсув фази, щоб через реальні
/// властивості матеріалу стін сформувати 3-тю гармоніку для осцилографа Гравця 1.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkAcousticEmitterNode : NetworkBehaviour, IInteractable
{
    [Header("Параметри акустичної кабіни")]
    [SerializeField] private string boothTitle = "Кабіна 1 (Бетон)";
    [SerializeField] private AcousticMaterialType boothMaterial = AcousticMaterialType.Concrete;

    [Tooltip("Частота 3-ї гармоніки, яку випромінює ця кабіна (відповідає резонансу матеріалу)")]
    [SerializeField] private float harmonicFrequency = 1.5f;

    [Tooltip("Амплітуда сигналу після проходження крізь матеріал кабіни (0.14..0.62)")]
    [SerializeField] private float effectiveAmplitude = 0.30f;

    [Header("Зв'язок із головним осцилографом Зони 4")]
    [SerializeField] private NetworkAcousticOscilloscope masterOscilloscope;

    [Header("Візуальний індикатор")]
    [SerializeField] private Renderer statusLampRenderer;

    // 0 = вимкнено, 1..4 = активна з фазою 0°, 90°, 180°, 270°
    public NetworkVariable<int> PhaseStepIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public AcousticMaterialType BoothMaterial => boothMaterial;
    public float HarmonicFrequency => harmonicFrequency;
    public float EffectiveAmplitude => effectiveAmplitude;
    public bool IsEmitting => PhaseStepIndex.Value > 0;
    public float CurrentPhaseRadians => PhaseStepIndex.Value > 0 ? (PhaseStepIndex.Value - 1) * (Mathf.PI * 0.5f) : 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        PhaseStepIndex.OnValueChanged += OnPhaseStepChanged;
        UpdateVisual();
    }

    public override void OnNetworkDespawn()
    {
        PhaseStepIndex.OnValueChanged -= OnPhaseStepChanged;
        base.OnNetworkDespawn();
    }

    public string GetPromptText()
    {
        if (PhaseStepIndex.Value == 0)
        {
            return $"[E] Увімкнути акустичний генератор: {boothTitle} ({boothMaterial})";
        }

        int deg = (PhaseStepIndex.Value - 1) * 90;
        return $"[E] Змінити фазу генератора ({boothTitle} | Фаза: {deg}°) / Вимкнути";
    }

    public void Interact()
    {
        CycleEmitterServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CycleEmitterServerRpc()
    {
        int nextStep = (PhaseStepIndex.Value + 1) % 5; // 0 (OFF) -> 1 (0°) -> 2 (90°) -> 3 (180°) -> 4 (270°) -> 0
        PhaseStepIndex.Value = nextStep;

        if (masterOscilloscope != null)
        {
            masterOscilloscope.OnBoothEmitterUpdatedOnServer(this);
        }

        // Генератор звуку в кабіні створює акустичну подію для NPC (гучність залежить від матеріалу!)
        if (nextStep > 0)
        {
            NoiseDispatcher.EmitOnServer(new NoiseEvent
            {
                Position = transform.position,
                Loudness = Mathf.Clamp(effectiveAmplitude * 1.15f, 0.12f, 0.85f),
                Source = NoiseSource.Environmental
            });
        }
    }

    public void ForceDeactivateOnServer()
    {
        if (!IsServer) return;
        PhaseStepIndex.Value = 0;
    }

    private void OnPhaseStepChanged(int previous, int current)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        Renderer r = statusLampRenderer != null ? statusLampRenderer : GetComponent<Renderer>();
        if (r == null) return;

        Color c = PhaseStepIndex.Value > 0
            ? new Color(0.25f, 1.0f, 0.45f)
            : new Color(0.75f, 0.48f, 0.12f);

        r.material.color = c;
        if (r.material.HasProperty("_EmissionColor"))
        {
            r.material.SetColor("_EmissionColor", PhaseStepIndex.Value > 0 ? c * 2.2f : c * 0.4f);
        }
    }
}