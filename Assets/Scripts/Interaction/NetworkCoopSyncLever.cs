using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkCoopSyncLever : NetworkBehaviour, IInteractable
{
    [Header("Налаштування синхронного важеля шлюзу")]
    [SerializeField] private string leverName = "Лівий гідроважіль шлюзу";

    [Tooltip("Перетягни сюди ДРУГИЙ парний важіль з іншого боку дверей")]
    [SerializeField] private NetworkCoopSyncLever partnerLever;

    [Tooltip("Комп'ютер GRiD-87, який подає дозвіл на ці важелі (необов'язково)")]
    [SerializeField] private NetworkGridOsTerminal requiredGridComputer;

    [Tooltip("Гермодвері бункера, які піднімуться вгору при одночасному натисканні")]
    [SerializeField] private NetworkAirlockDoor targetBunkerDoor;

    [Tooltip("Вузол живлення дверей (Interactive_Terminal), якщо двері прив'язані до нього")]
    [SerializeField] private NetworkInteractiveObject targetPowerNode;

    [Header("Часове вікно синхронізації (секунди)")]
    [Tooltip("Максимальна різниця в часі між натисканням двох гравців (2.5 сек)")]
    [SerializeField] private float coopSyncWindowSeconds = 2.5f;

    [Tooltip("Час утримання важеля при тесті ОДНИМ гравцем (щоб ти сам встиг перебігти до другого важеля!)")]
    [SerializeField] private float soloTestWindowSeconds = 6.0f;

    [Tooltip("Дочірня ручка важеля, яка повертається вниз при натисканні")]
    [SerializeField] private Transform handlePivot;

    public NetworkVariable<bool> IsPulledNow = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsLockedOpen = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float _serverPulledTimer;
    private MeshRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        IsPulledNow.OnValueChanged += OnStateChanged;
        IsLockedOpen.OnValueChanged += OnStateChanged;
        UpdateVisual();
    }

    public override void OnNetworkDespawn()
    {
        IsPulledNow.OnValueChanged -= OnStateChanged;
        IsLockedOpen.OnValueChanged -= OnStateChanged;
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        // Плавний поворот ручки важеля вниз, коли він активований
        if (handlePivot != null)
        {
            float targetAngle = (IsPulledNow.Value || IsLockedOpen.Value) ? 55f : -25f;
            Quaternion targetRot = Quaternion.Euler(targetAngle, 0f, 0f);
            handlePivot.localRotation = Quaternion.Slerp(handlePivot.localRotation, targetRot, 10f * Time.deltaTime);
        }

        if (!IsServer || !IsSpawned) return;

        if (IsPulledNow.Value && !IsLockedOpen.Value)
        {
            _serverPulledTimer -= Time.deltaTime;
            if (_serverPulledTimer <= 0f)
            {
                // Час вийшов, напарник не встиг смикнути другий важіль — важіль відскакує назад!
                IsPulledNow.Value = false;
                NotifyLeverResetClientRpc();
            }
        }
    }

    private bool IsComputerAuthorized()
    {
        return requiredGridComputer == null || requiredGridComputer.IsAirlockAuthorized.Value;
    }

    public string GetPromptText()
    {
        if (IsLockedOpen.Value)
        {
            return "[Зафіксовано] Гідрозамок бункера відкрито";
        }

        if (!IsComputerAuthorized())
        {
            return $"[Заблоковано] Спочатку авторизуйте шлюз через комп'ютер GRiD-87 ({leverName})";
        }

        if (IsPulledNow.Value)
        {
            return "[ОЧІКУВАННЯ НАПАРНИКА] Поверніть другий важіль одночасно!";
        }

        return $"[E] Смикнути {leverName} (Синхронно з напарником)";
    }

    public void Interact()
    {
        if (IsLockedOpen.Value || IsPulledNow.Value) return;
        if (!IsComputerAuthorized())
        {
            RetroAuditorHud.ShowThoughtBanner(
                "ГІДРОПРИВІД ВАЖЕЛІВ ЗНЕСТРУМЛЕНО",
                "Перевірте комп'ютери GRiD-87 у КПП охорони та Східному корпусі на поверхні",
                5.5f
            );
            return;
        }

        PullLeverServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PullLeverServerRpc()
    {
        if (IsLockedOpen.Value || !IsComputerAuthorized()) return;

        bool isSoloTesting = NetworkManager.Singleton != null &&
                             NetworkManager.Singleton.ConnectedClientsIds.Count <= 1;

        float window = isSoloTesting ? soloTestWindowSeconds : coopSyncWindowSeconds;
        _serverPulledTimer = window;
        IsPulledNow.Value = true;

        NoiseDispatcher.EmitOnServer(new NoiseEvent
        {
            Position = transform.position,
            Loudness = 0.55f,
            Source = NoiseSource.PhysicalImpact
        });

        // Перевіряємо, чи напарник ВЖЕ тримає другий важіль опущеним!
        if (partnerLever != null && (partnerLever.IsPulledNow.Value || partnerLever.IsLockedOpen.Value))
        {
            LockBothLeversAndOpenBunkerOnServer();
        }
    }

    private void LockBothLeversAndOpenBunkerOnServer()
    {
        IsLockedOpen.Value = true;
        IsPulledNow.Value = true;

        if (partnerLever != null)
        {
            partnerLever.IsLockedOpen.Value = true;
            partnerLever.IsPulledNow.Value = true;
        }

        if (targetPowerNode != null)
        {
            targetPowerNode.IsActive.Value = true;
        }

        if (targetBunkerDoor != null)
        {
            targetBunkerDoor.IsDoorOpen.Value = true;
        }

        NoiseDispatcher.EmitOnServer(new NoiseEvent
        {
            Position = transform.position,
            Loudness = 0.85f,
            Source = NoiseSource.PhysicalImpact
        });

        NotifyBunkerUnlockedClientRpc();
    }

    [ClientRpc]
    private void NotifyLeverResetClientRpc()
    {
        if (RetroAuditorHud.Instance != null)
        {
            RetroAuditorHud.ShowThoughtBanner(
                "РОЗСИНХРОНІЗАЦІЯ ГІДРОЗАМКА!",
                "Обидва важелі зліва і справа потрібно натиснути одночасно!",
                4.0f
            );
        }
    }

    [ClientRpc]
    private void NotifyBunkerUnlockedClientRpc()
    {
        if (RetroAuditorHud.Instance != null)
        {
            RetroAuditorHud.ShowThoughtBanner(
                "СИНХРОНІЗАЦІЯ УСПІШНА — ГЕРМОДВЕРІ ВІДКРИТО!",
                "Вхід у підземний комплекс «Синхронізація» вільний",
                7.0f
            );
        }
    }

    private void OnStateChanged(bool prev, bool current)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_renderer == null) return;

        if (IsLockedOpen.Value)
            _renderer.material.color = Color.green;
        else if (IsPulledNow.Value)
            _renderer.material.color = new Color(1f, 0.75f, 0.1f);
        else
            _renderer.material.color = new Color(0.75f, 0.2f, 0.15f);
    }
}