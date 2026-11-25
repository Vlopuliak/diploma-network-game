using TheCopy.AI;
using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement & Stealth Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform cameraHolder;

    [Header("Footstep Noise for NPC")]
    [Tooltip("Гучність тупоту при бігу на [Left Shift] (ходьба і присідання — безшумні)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float sprintFootstepLoudness = 0.65f;
    [SerializeField] private float footstepInterval = 0.38f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactDistance = 3f;

    [Header("Flashlight & 3D Character Model")]
    [SerializeField] private Light flashlightLight;
    [Tooltip("Перетягни сюди дочірній об'єкт character всередині префаба гравця")]
    [SerializeField] private GameObject playerVisualRoot;

    public NetworkVariable<bool> IsFlashlightOn = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Публічні властивості для відображення на ретро-візорі аудитора (RetroAuditorHud)
    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }
    public float CurrentMovementNoise { get; private set; }

    private float verticalRotation = 0f;
    private IInteractable currentInteractable;

    private bool isSeated = false;
    private ChessSeat currentSeat = null;

    private float _defaultCamLocalY = 0.6f;
    private float _nextFootstepTime;
    private double _nextCameraPoseSendTime;
    private Vector2 _lastSentCameraPose;
    private bool _hasSentCameraPose;
    private bool _hasServerCameraPose;

    public bool HasServerCameraPose => IsServer && cameraHolder != null && (IsOwner || _hasServerCameraPose);

    private void Awake()
    {
        if (cameraHolder != null)
        {
            _defaultCamLocalY = cameraHolder.localPosition.y;
        }
    }

    public override void OnNetworkSpawn()
    {
        _hasSentCameraPose = false;
        _hasServerCameraPose = false;
        _nextCameraPoseSendTime = 0;
        IsFlashlightOn.OnValueChanged += OnFlashlightChanged;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = IsFlashlightOn.Value;
        }

        if (IsOwner)
        {
            string targetSpawnName = IsServer ? "SpawnPoint_1" : "SpawnPoint_2";
            GameObject spawnPoint = GameObject.Find(targetSpawnName);

            if (spawnPoint != null)
            {
                transform.position = spawnPoint.transform.position;
                transform.rotation = spawnPoint.transform.rotation;
            }

            // Ховаємо полігони власної голови/тіла для локальної камери (залишаємо тільки тінь),
            // при цьому другий гравець у мережі бачить нашу 3D-модель повністю!
            if (playerVisualRoot != null)
            {
                Renderer[] renderers = playerVisualRoot.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }

            NetworkBootstrapUI.SetPauseMenu(false);
        }
        else
        {
            if (cameraHolder != null)
            {
                Camera cam = cameraHolder.GetComponentInChildren<Camera>();
                if (cam != null) cam.enabled = false;

                AudioListener listener = cameraHolder.GetComponentInChildren<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        _hasServerCameraPose = false;
        _hasSentCameraPose = false;
        IsFlashlightOn.OnValueChanged -= OnFlashlightChanged;
    }

    private void LateUpdate()
    {
        // Runs after input and seat changes, including frames where Update returns for a menu.
        if (!IsSpawned || !IsOwner || IsServer || cameraHolder == null ||
            Time.unscaledTimeAsDouble < _nextCameraPoseSendTime)
            return;

        var pose = new Vector2(Mathf.DeltaAngle(0f, cameraHolder.localEulerAngles.x), cameraHolder.localPosition.y);
        if (_hasSentCameraPose && Mathf.Abs(pose.x - _lastSentCameraPose.x) < 0.1f &&
            Mathf.Abs(pose.y - _lastSentCameraPose.y) < 0.001f)
            return;

        SyncCameraPoseServerRpc(pose.x, pose.y);
        _lastSentCameraPose = pose;
        _hasSentCameraPose = true;
        _nextCameraPoseSendTime = Time.unscaledTimeAsDouble + 0.1;
    }

    [ServerRpc]
    private void SyncCameraPoseServerRpc(float pitch, float localHeight, ServerRpcParams rpcParams = default)
    {
        if (!IsServer || cameraHolder == null || rpcParams.Receive.SenderClientId != OwnerClientId ||
            !float.IsFinite(pitch) || !float.IsFinite(localHeight))
            return;

        // The same limits are used by mouse look and crouching in the local controller.
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        localHeight = Mathf.Clamp(localHeight, _defaultCamLocalY - 0.45f, _defaultCamLocalY);
        if (!IsOwner)
        {
            cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 position = cameraHolder.localPosition;
            position.y = localHeight;
            cameraHolder.localPosition = position;
        }
        _hasServerCameraPose = true;
    }

    private void OnFlashlightChanged(bool previousValue, bool newValue)
    {
        if (flashlightLight != null)
        {
            flashlightLight.enabled = newValue;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // 0. Обробка клавіші [Escape] (спочатку закриває комп'ютер GRiD-87, записку, термінал або стіл, інакше відкриває паузу)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (NetworkAcousticOscilloscope.IsAnyOscilloscopeOpen)
            {
                NetworkAcousticOscilloscope.ActiveLocalOscilloscope.CloseOscilloscope();
                return;
            }

            if (NetworkGridOsTerminal.IsAnyGridTerminalOpen)
            {
                NetworkGridOsTerminal.ActiveLocalTerminal.CloseTerminal();
                return;
            }

            if (NetworkReadableNote.IsAnyNoteOpen)
            {
                NetworkReadableNote.ActiveLocalNote.CloseNoteUi();
                return;
            }

            if (NetworkKeypadTerminal.IsAnyTerminalOpen)
            {
                NetworkKeypadTerminal.ActiveLocalTerminal.CloseTerminalUi();
                return;
            }

            if (isSeated)
            {
                if (currentSeat != null)
                {
                    currentSeat.StandUp();
                }
                return;
            }

            NetworkBootstrapUI.TogglePauseMenu();
            return;
        }

        // Якщо відкрита пауза, комп'ютер GRiD-87, термінал, записка або гравець сидить за столом — рух заблоковано
        if (NetworkBootstrapUI.IsPauseMenuOpen ||
            NetworkAcousticOscilloscope.IsAnyOscilloscopeOpen ||
            NetworkGridOsTerminal.IsAnyGridTerminalOpen ||
            NetworkKeypadTerminal.IsAnyTerminalOpen ||
            NetworkReadableNote.IsAnyNoteOpen ||
            isSeated)
        {
            IsSprinting = false;
            CurrentMovementNoise = Mathf.MoveTowards(CurrentMovementNoise, 0f, 3f * Time.deltaTime);
            return;
        }

        // 1. Огляд мишкою
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        if (cameraHolder != null)
        {
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        // 2. Рух WASD + Біг [Left Shift] + Тихе присідання [Left Ctrl]
        IsCrouching = Input.GetKey(KeyCode.LeftControl);
        IsSprinting = !IsCrouching && Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = IsCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);

        // Плавне присідання камери
        if (cameraHolder != null)
        {
            float targetCamY = IsCrouching ? (_defaultCamLocalY - 0.45f) : _defaultCamLocalY;
            Vector3 camPos = cameraHolder.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCamY, 12f * Time.deltaTime);
            cameraHolder.localPosition = camPos;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * horizontal + vertical * transform.forward).normalized;
        if (moveDirection.magnitude >= 0.1f)
        {
            transform.position += moveDirection * currentSpeed * Time.deltaTime;

            // Автоматичне слідування висоті сходинок та похилого пандуса бункера:
            if (Physics.Raycast(transform.position + Vector3.up * 0.8f, Vector3.down, out RaycastHit groundHit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(p.y, groundHit.point.y + 1.0f, 18f * Time.deltaTime);
                transform.position = p;
            }

            // Якщо гравець БІЖИТЬ — генеруємо шум кроків для NPC
            if (IsSprinting && Time.time >= _nextFootstepTime)
            {
                _nextFootstepTime = Time.time + footstepInterval;
                CurrentMovementNoise = sprintFootstepLoudness;
                EmitFootstepNoiseServerRpc(sprintFootstepLoudness);
            }
        }

        // Плавне згасання індикатора шуму кроків на HUD
        CurrentMovementNoise = Mathf.MoveTowards(CurrentMovementNoise, 0f, 1.8f * Time.deltaTime);

        // 3. Взаємодія (Raycast)
        CheckForInteractable();

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.Interact();
        }

        // 4. Ліхтарик на [F]
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlightServerRpc();
        }
    }

    [ServerRpc]
    private void EmitFootstepNoiseServerRpc(float loudness, ServerRpcParams rpcParams = default)
    {
        loudness = Mathf.Clamp01(loudness);
        NoiseDispatcher.EmitOnServer(new NoiseEvent
        {
            Position = transform.position,
            Loudness = loudness,
            Source = NoiseSource.PlayerFootstep
        });
        CopyPlayerNoise.TryPublish(NetworkManager, rpcParams.Receive.SenderClientId, NoiseSource.PlayerFootstep, loudness);
    }

    public void EnterSeat(ChessSeat seat, Transform seatViewPoint)
    {
        isSeated = true;
        currentSeat = seat;

        transform.position = seatViewPoint.position;
        transform.rotation = seatViewPoint.rotation;

        if (cameraHolder != null)
        {
            verticalRotation = 25f;
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitSeat()
    {
        isSeated = false;
        currentSeat = null;

        if (cameraHolder != null)
        {
            verticalRotation = 0f;
            cameraHolder.localRotation = Quaternion.identity;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    [ServerRpc]
    private void ToggleFlashlightServerRpc()
    {
        IsFlashlightOn.Value = !IsFlashlightOn.Value;
    }

    private void CheckForInteractable()
    {
        if (cameraHolder == null) return;

        Ray ray = new Ray(cameraHolder.position, cameraHolder.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            currentInteractable = hit.collider.GetComponent<IInteractable>();
        }
        else
        {
            currentInteractable = null;
        }
    }

    private void OnGUI()
    {
        if (!IsOwner ||
            NetworkBootstrapUI.IsPauseMenuOpen ||
            NetworkAcousticOscilloscope.IsAnyOscilloscopeOpen ||
            NetworkGridOsTerminal.IsAnyGridTerminalOpen ||
            NetworkKeypadTerminal.IsAnyTerminalOpen ||
            NetworkReadableNote.IsAnyNoteOpen) return;

        string prompt = isSeated
            ? "[Escape] ВСТАТИ З-ЗА СТОЛУ"
            : (currentInteractable != null ? currentInteractable.GetPromptText() : "");

        if (string.IsNullOrEmpty(prompt)) return;

        // Стиль жовтої плашки Fears to Fathom для підказок взаємодії [E]
        string formatted = $"“ {prompt.ToUpper()} ";
        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        textStyle.normal.textColor = new Color(0.08f, 0.08f, 0.08f, 1f);

        Vector2 size = textStyle.CalcSize(new GUIContent(formatted));
        float w = size.x + 20f;
        float h = 28f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height / 2f + 38f;

        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(x + 3f, y + 3f, w, h), Texture2D.whiteTexture);

        GUI.color = new Color(0.95f, 0.78f, 0.24f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = prev;

        GUI.Label(new Rect(x, y, w, h), formatted, textStyle);
    }
}