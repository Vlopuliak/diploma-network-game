using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Горизонтальний двостулковий гермолюк ракетного типу над спуском у бункер.
/// У зачиненому стані слугує міцною підлогою на рівні двору (Y = 0).
/// При розблокуванні спочатку розгерметизовується (опускається вниз у пази),
/// а потім обидві стулки роз'їжджаються вліво (-X) і вправо (+X) прямо в підземні кишені.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkSiloBlastHatch : NetworkBehaviour, IInteractable
{
    [Header("Стулки люка (Ліва та Права)")]
    [SerializeField] private Transform leftLeaf;
    [SerializeField] private Transform rightLeaf;

    [Header("Зв'язок із кооперативними механізмами")]
    [Tooltip("Комп'ютер GRiD-87, який знімає карантинне блокування (необов'язково)")]
    [SerializeField] private NetworkGridOsTerminal requiredGridComputer;

    [Tooltip("Або рубильники живлення, які мають бути активними")]
    [SerializeField] private NetworkInteractiveObject[] requiredPowerSwitches;

    [Header("Параметри анімації ракетного люка")]
    [Tooltip("На скільки метрів стулки спочатку просідають вниз при розгерметизації")]
    [SerializeField] private float unsealDropDepth = 0.16f;

    [Tooltip("На скільки метрів кожна стулка роз'їжджається вбік (вліво і вправо)")]
    [SerializeField] private float slideDistance = 5.65f;

    [Tooltip("Швидкість розгерметизації (фаза 1)")]
    [SerializeField] private float unsealSpeed = 0.45f;

    [Tooltip("Швидкість розсування стулок убік (фаза 2)")]
    [SerializeField] private float slideSpeed = 2.2f;

    [Header("Індикатори стану")]
    [SerializeField] private Renderer[] statusBeacons;
    [SerializeField] private Color lockedColor = new Color(1f, 0.15f, 0.1f);
    [SerializeField] private Color movingColor = new Color(1f, 0.72f, 0.1f);
    [SerializeField] private Color openColor = new Color(0.2f, 1f, 0.35f);

    public NetworkVariable<bool> IsHatchOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Vector3 _leftClosedLocalPos;
    private Vector3 _rightClosedLocalPos;
    private float _unsealProgress; // 0 = герметично зачинено, 1 = опущено в пази
    private float _slideProgress;  // 0 = зімкнуто по центру, 1 = повністю розсунуто вліво/вправо
    private NavMeshObstacle _navObstacle;

    private void Awake()
    {
        if (leftLeaf != null) _leftClosedLocalPos = leftLeaf.localPosition;
        if (rightLeaf != null) _rightClosedLocalPos = rightLeaf.localPosition;
        _navObstacle = GetComponent<NavMeshObstacle>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        IsHatchOpen.OnValueChanged += OnHatchStateChanged;

        if (IsServer)
        {
            if (requiredGridComputer != null)
            {
                requiredGridComputer.IsAirlockAuthorized.OnValueChanged += OnGridAuthorizedChanged;
                if (requiredGridComputer.IsAirlockAuthorized.Value)
                {
                    IsHatchOpen.Value = true;
                }
            }

            if (requiredPowerSwitches != null)
            {
                foreach (var sw in requiredPowerSwitches)
                {
                    if (sw != null) sw.OnPowerStateChanged += OnPowerSwitchChanged;
                }
            }
        }

        if (IsHatchOpen.Value)
        {
            _unsealProgress = 1f;
            _slideProgress = 1f;
            ApplyLeafTransforms();
        }

        UpdateBeacons();
    }

    public override void OnNetworkDespawn()
    {
        IsHatchOpen.OnValueChanged -= OnHatchStateChanged;

        if (IsServer)
        {
            if (requiredGridComputer != null)
            {
                requiredGridComputer.IsAirlockAuthorized.OnValueChanged -= OnGridAuthorizedChanged;
            }

            if (requiredPowerSwitches != null)
            {
                foreach (var sw in requiredPowerSwitches)
                {
                    if (sw != null) sw.OnPowerStateChanged -= OnPowerSwitchChanged;
                }
            }
        }

        base.OnNetworkDespawn();
    }

    private void OnGridAuthorizedChanged(bool previous, bool current)
    {
        if (IsServer && current && !IsHatchOpen.Value)
        {
            OpenHatchOnServer();
        }
    }

    private void OnPowerSwitchChanged(bool _)
    {
        if (!IsServer) return;
        if (HasAllRequirements() && !IsHatchOpen.Value)
        {
            OpenHatchOnServer();
        }
    }

    public void OpenHatchOnServer()
    {
        if (!IsServer) return;
        IsHatchOpen.Value = true;
        NoiseDispatcher.EmitOnServer(new NoiseEvent
        {
            Position = transform.position,
            Loudness = 0.9f,
            Source = NoiseSource.PhysicalImpact
        });
    }

    private void Update()
    {
        bool wantOpen = IsHatchOpen.Value;

        if (wantOpen)
        {
            // Фаза 1: Розгерметизація (просідання вниз у пази)
            if (_unsealProgress < 1f)
            {
                _unsealProgress = Mathf.MoveTowards(_unsealProgress, 1f, unsealSpeed * Time.deltaTime);
            }
            // Фаза 2: Розсування лівої стулки вліво (-X), а правої вправо (+X)
            else if (_slideProgress < 1f)
            {
                _slideProgress = Mathf.MoveTowards(_slideProgress, 1f, (slideSpeed / Mathf.Max(0.1f, slideDistance)) * Time.deltaTime);
            }
        }
        else
        {
            // Зворотний порядок при закритті: спочатку з'їжджаються до центру, потім піднімаються врівень із землею
            if (_slideProgress > 0f)
            {
                _slideProgress = Mathf.MoveTowards(_slideProgress, 0f, (slideSpeed / Mathf.Max(0.1f, slideDistance)) * Time.deltaTime);
            }
            else if (_unsealProgress > 0f)
            {
                _unsealProgress = Mathf.MoveTowards(_unsealProgress, 0f, unsealSpeed * Time.deltaTime);
            }
        }

        ApplyLeafTransforms();
        UpdateBeacons();

        if (_navObstacle != null)
        {
            _navObstacle.enabled = _slideProgress < 0.85f;
        }
    }

    private void ApplyLeafTransforms()
    {
        float smoothSlide = Mathf.SmoothStep(0f, 1f, _slideProgress);
        float dropY = -unsealDropDepth * _unsealProgress;
        float offsetX = slideDistance * smoothSlide;

        if (leftLeaf != null)
        {
            leftLeaf.localPosition = _leftClosedLocalPos + new Vector3(-offsetX, dropY, 0f);
        }

        if (rightLeaf != null)
        {
            rightLeaf.localPosition = _rightClosedLocalPos + new Vector3(offsetX, dropY, 0f);
        }
    }

    private bool HasAllRequirements()
    {
        if (requiredGridComputer != null && !requiredGridComputer.IsAirlockAuthorized.Value)
        {
            return false;
        }

        if (requiredPowerSwitches != null && requiredPowerSwitches.Length > 0)
        {
            foreach (var sw in requiredPowerSwitches)
            {
                if (sw != null && !sw.IsActive.Value) return false;
            }
        }

        return true;
    }

    public string GetPromptText()
    {
        if (!HasAllRequirements())
        {
            return "[Заблоковано] Розблокуйте гермолюк через термінал GRiD-87";
        }

        return IsHatchOpen.Value
            ? "[E] Зачинити гермолюк спуску в бункер"
            : "[E] Розгерметизувати та розсунути гермолюк спуску";
    }

    public void Interact()
    {
        if (!HasAllRequirements())
        {
            RetroAuditorHud.ShowThoughtBanner(
                "ГЕРМОЛЮК БУНКЕРА ЗАБЛОКОВАНО",
                "Авторизуйте відкриття через термінал GRiD-87 у Східному корпусі",
                5.0f
            );
            return;
        }

        ToggleHatchServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleHatchServerRpc()
    {
        if (!HasAllRequirements()) return;

        IsHatchOpen.Value = !IsHatchOpen.Value;
        NoiseDispatcher.EmitOnServer(new NoiseEvent
        {
            Position = transform.position,
            Loudness = 0.85f,
            Source = NoiseSource.PhysicalImpact
        });
    }

    private void OnHatchStateChanged(bool prev, bool current)
    {
        if (current && RetroAuditorHud.Instance != null)
        {
            RetroAuditorHud.ShowThoughtBanner(
                "ГЕРМОЛЮК РОЗГЕРМЕТИЗОВАНО — СТУЛКИ РОЗСУВАЮТЬСЯ!",
                "Спуск до Підземного Сектора 1 відкрито",
                6.5f
            );
        }
    }

    private void UpdateBeacons()
    {
        if (statusBeacons == null) return;

        bool isMoving = (_unsealProgress > 0.01f && _unsealProgress < 0.99f) ||
                        (_slideProgress > 0.01f && _slideProgress < 0.99f);

        Color c = isMoving ? movingColor : (IsHatchOpen.Value ? openColor : lockedColor);
        foreach (var r in statusBeacons)
        {
            if (r == null) continue;
            r.material.color = c;
            if (r.material.HasProperty("_EmissionColor"))
            {
                r.material.SetColor("_EmissionColor", c * 2.6f);
            }
        }
    }
}