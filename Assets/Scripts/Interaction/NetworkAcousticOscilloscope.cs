using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Пазл №1 (Зона 4 — Акустична лабораторія): «Фазова синхронізація та акустичний резонанс».
///
/// Математична модель:
///   y_target(t) = A1*sin(w1*t + p1) + A2*sin(w2*t + p2) + A3*sin(w3*t + p3)
///   E_MSE = (1 / N) * sum_{k=0}^{N-1} (y_target(t_k) - y_coop(t_k))^2
///
/// Кооперативний розподіл:
///   - Гравець 1 (Апаратна Зони 4) керує гармоніками 1 і 2 на живому ЕПТ-осцилографі
///     та бачить, який матеріал стін потрібен для 3-ї гармоніки.
///   - Гравець 2 (4 Акустичні кабіни: Бетон / Дерево / Метал / Поролон) вмикає генератор
///     у правильній кабіні та підбирає фазу (0°/90°/180°/270°) по рації [T].
///   - Коли E_MSE < 0.035 протягом 2.5 сек, сервер фіксує резонанс і відкриває гермошлюз Серверної.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkAcousticOscilloscope : NetworkBehaviour, IInteractable
{
    public static NetworkAcousticOscilloscope ActiveLocalOscilloscope { get; private set; }
    public static bool IsAnyOscilloscopeOpen => ActiveLocalOscilloscope != null;

    [Header("Зв'язок із 4 акустичними кабінами та дверима Серверної")]
    [SerializeField] private NetworkAcousticEmitterNode[] boothEmitters;
    [SerializeField] private NetworkAirlockDoor targetSecurityGate;

    [Header("Параметри математичної верифікації (Сервер)")]
    [Tooltip("Максимально допустима середньоквадратична похибка (MSE) для фіксації резонансу")]
    [SerializeField] private float maxAllowedMse = 0.038f;
    [Tooltip("Скільки секунд потрібно утримувати резонанс (сек)")]
    [SerializeField] private float requiredLockDuration = 2.5f;

    // --- ЦІЛЬОВІ ПАРАМЕТРИ (ГЕНЕРУЮТЬСЯ НА СЕРВЕРІ) ---
    public NetworkVariable<float> TargetW1 = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetP1 = new NetworkVariable<float>(0.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetW2 = new NetworkVariable<float>(2.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetP2 = new NetworkVariable<float>(1.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> TargetBoothIndex = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetW3 = new NetworkVariable<float>(3.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetA3 = new NetworkVariable<float>(0.62f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TargetP3 = new NetworkVariable<float>(1.5708f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- ПОТОЧНІ ПАРАМЕТРИ ГРАВЦІВ (СИНХРОНІЗОВАНІ ПО МЕРЕЖІ) ---
    public NetworkVariable<float> PlayerW1 = new NetworkVariable<float>(1.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> PlayerP1 = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> PlayerW2 = new NetworkVariable<float>(2.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> PlayerP2 = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> ActiveBoothIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> PlayerW3 = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> PlayerA3 = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> PlayerP3 = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- РЕЗУЛЬТАТ СЕРВЕРНИХ ОБЧИСЛЕНЬ ---
    public NetworkVariable<float> CurrentMse = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> LockProgress = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsSolved = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private const float A1 = 0.55f;
    private const float A2 = 0.42f;
    private const int SamplePoints = 64;

    // Текстура живого ЕПТ-осцилографа 320x160
    private const int ScopeTexW = 320;
    private const int ScopeTexH = 160;
    private Texture2D _scopeTex;
    private Color32[] _scopePixels;
    private Texture2D _bgTex;
    private Texture2D _borderTex;
    private float _scopeAnimPhase;

    // Локальні повзунки для плавної відправки на сервер
    private float _uiW1;
    private float _uiP1;
    private float _uiW2;
    private float _uiP2;
    private float _nextRpcSendTime;

    private void Awake()
    {
        CreateScopeTextures();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        IsSolved.OnValueChanged += OnSolvedChanged;

        if (IsServer)
        {
            GenerateProceduralTargetSignalOnServer();
            EvaluateMseOnServer();
        }
    }

    public override void OnNetworkDespawn()
    {
        IsSolved.OnValueChanged -= OnSolvedChanged;
        if (ActiveLocalOscilloscope == this)
        {
            CloseOscilloscope();
        }
        base.OnNetworkDespawn();
    }

    private void GenerateProceduralTargetSignalOnServer()
    {
        // Вибираємо випадкову цільову кабіну з 4 доступних матеріалів (Бетон/Дерево/Метал/Поролон)
        int boothIdx = 1; // За замовчуванням Wood, але рандомізуємо серед наявних
        if (boothEmitters != null && boothEmitters.Length > 0)
        {
            boothIdx = Random.Range(0, boothEmitters.Length);
            NetworkAcousticEmitterNode chosen = boothEmitters[boothIdx];
            if (chosen != null)
            {
                TargetBoothIndex.Value = boothIdx;
                TargetW3.Value = chosen.HarmonicFrequency;
                TargetA3.Value = chosen.EffectiveAmplitude;
                TargetP3.Value = Random.Range(0, 4) * (Mathf.PI * 0.5f);
            }
        }

        // Дискретні, але нетривіальні цільові частоти й фази для гармонік 1 і 2
        float[] w1Choices = { 1.0f, 1.5f, 2.0f };
        float[] w2Choices = { 2.5f, 3.0f, 3.5f };

        TargetW1.Value = w1Choices[Random.Range(0, w1Choices.Length)];
        TargetP1.Value = Mathf.Round(Random.Range(0.4f, 5.6f) * 4f) / 4f;
        TargetW2.Value = w2Choices[Random.Range(0, w2Choices.Length)];
        TargetP2.Value = Mathf.Round(Random.Range(0.4f, 5.6f) * 4f) / 4f;

        // Початковий розсинхронізований стан ручок
        PlayerW1.Value = 1.0f;
        PlayerP1.Value = 0.0f;
        PlayerW2.Value = 2.0f;
        PlayerP2.Value = 0.0f;
    }

    /// <summary>
    /// Викликається на сервері, коли Гравець 2 перемикає одну з 4 акустичних кабін.
    /// Дозволяє працювати лише 1 активній кабіні одночасно, щоб чітко демонструвати ефект її матеріалу.
    /// </summary>
    public void OnBoothEmitterUpdatedOnServer(NetworkAcousticEmitterNode updatedNode)
    {
        if (!IsServer || IsSolved.Value || boothEmitters == null) return;

        int foundIdx = -1;
        for (int i = 0; i < boothEmitters.Length; i++)
        {
            if (boothEmitters[i] == updatedNode)
            {
                foundIdx = i;
            }
            else if (updatedNode.IsEmitting && boothEmitters[i] != null && boothEmitters[i].IsEmitting)
            {
                boothEmitters[i].ForceDeactivateOnServer();
            }
        }

        if (updatedNode.IsEmitting)
        {
            ActiveBoothIndex.Value = foundIdx;
            PlayerW3.Value = updatedNode.HarmonicFrequency;
            PlayerA3.Value = updatedNode.EffectiveAmplitude;
            PlayerP3.Value = updatedNode.CurrentPhaseRadians;
        }
        else
        {
            ActiveBoothIndex.Value = -1;
            PlayerW3.Value = 0f;
            PlayerA3.Value = 0f;
            PlayerP3.Value = 0f;
        }

        EvaluateMseOnServer();
    }

    public string GetPromptText()
    {
        if (IsSolved.Value)
        {
            return "[РЕЗОНАНС ЗАФІКСОВАНО] Шлюз Серверної розблоковано";
        }
        return "[E] Аналоговий осцилограф «С1-87» (Акустичний пазл)";
    }

    public void Interact()
    {
        if (ActiveLocalOscilloscope == this)
        {
            CloseOscilloscope();
            return;
        }
        OpenOscilloscope();
    }

    public void OpenOscilloscope()
    {
        ActiveLocalOscilloscope = this;
        _uiW1 = PlayerW1.Value;
        _uiP1 = PlayerP1.Value;
        _uiW2 = PlayerW2.Value;
        _uiP2 = PlayerP2.Value;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseOscilloscope()
    {
        if (ActiveLocalOscilloscope == this)
        {
            ActiveLocalOscilloscope = null;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!IsSpawned) return;

        // 1. На сервері постійно перевіряємо умову утримання резонансу
        if (IsServer && !IsSolved.Value)
        {
            EvaluateMseOnServer();

            if (CurrentMse.Value <= maxAllowedMse)
            {
                LockProgress.Value = Mathf.MoveTowards(LockProgress.Value, 1.0f, Time.deltaTime / Mathf.Max(0.5f, requiredLockDuration));
                if (LockProgress.Value >= 0.999f)
                {
                    CompletePuzzleOnServer();
                }
            }
            else
            {
                LockProgress.Value = Mathf.MoveTowards(LockProgress.Value, 0.0f, Time.deltaTime * 0.6f);
            }
        }

        // 2. Локальна обробка відкритого вікна осцилографа
        if (ActiveLocalOscilloscope == this)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
            {
                CloseOscilloscope();
                return;
            }

            _scopeAnimPhase += Time.deltaTime * 1.4f;
            RedrawOscilloscopeTexture();
        }
    }

    private void EvaluateMseOnServer()
    {
        float sumSq = 0f;
        for (int k = 0; k < SamplePoints; k++)
        {
            float t = (k / (float)SamplePoints) * Mathf.PI * 2f;
            float yTarget = EvalWave(t, TargetW1.Value, TargetP1.Value, TargetW2.Value, TargetP2.Value, TargetA3.Value, TargetW3.Value, TargetP3.Value);
            float yCoop = EvalWave(t, PlayerW1.Value, PlayerP1.Value, PlayerW2.Value, PlayerP2.Value, PlayerA3.Value, PlayerW3.Value, PlayerP3.Value);

            float diff = yTarget - yCoop;
            sumSq += diff * diff;
        }

        CurrentMse.Value = sumSq / SamplePoints;
    }

    private void CompletePuzzleOnServer()
    {
        IsSolved.Value = true;
        LockProgress.Value = 1.0f;

        if (targetSecurityGate != null)
        {
            targetSecurityGate.IsDoorOpen.Value = true;
        }

        NoiseDispatcher.EmitOnServer(new NoiseEvent
        {
            Position = transform.position,
            Loudness = 0.75f,
            Source = NoiseSource.Environmental
        });
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerHarmonicsServerRpc(float w1, float p1, float w2, float p2)
    {
        if (IsSolved.Value) return;

        PlayerW1.Value = Mathf.Clamp(w1, 0.5f, 3.0f);
        PlayerP1.Value = Mathf.Repeat(p1, Mathf.PI * 2f);
        PlayerW2.Value = Mathf.Clamp(w2, 1.5f, 4.5f);
        PlayerP2.Value = Mathf.Repeat(p2, Mathf.PI * 2f);

        EvaluateMseOnServer();
    }

    private void OnSolvedChanged(bool prev, bool current)
    {
        if (current)
        {
            RetroAuditorHud.ShowThoughtBanner(
                "АКУСТИЧНИЙ РЕЗОНАНС ЗАФІКСОВАНО (MSE < 0.038)!",
                "Гермошлюз Північної Серверної розблоковано",
                7.0f
            );
        }
    }

    private static float EvalWave(float t, float w1, float p1, float w2, float p2, float a3, float w3, float p3)
    {
        return A1 * Mathf.Sin(w1 * t + p1)
             + A2 * Mathf.Sin(w2 * t + p2)
             + a3 * Mathf.Sin(w3 * t + p3);
    }

    private void RedrawOscilloscopeTexture()
    {
        Color32 bg = new Color32(10, 18, 12, 255);
        Color32 grid = new Color32(22, 48, 28, 255);
        Color32 targetCol = new Color32(60, 255, 110, 255); // Еталонна зелена хвиля
        Color32 coopCol = new Color32(255, 178, 40, 255);   // Поточна бурштинова хвиля гравців

        for (int i = 0; i < _scopePixels.Length; i++)
        {
            _scopePixels[i] = bg;
        }

        // Координатна сітка осцилографа
        for (int x = 0; x < ScopeTexW; x += 32)
            for (int y = 0; y < ScopeTexH; y++)
                _scopePixels[y * ScopeTexW + x] = grid;

        for (int y = 0; y < ScopeTexH; y += 32)
            for (int x = 0; x < ScopeTexW; x++)
                _scopePixels[y * ScopeTexW + x] = grid;

        // Малюємо обидві криві суперпозиції
        for (int x = 0; x < ScopeTexW; x++)
        {
            float t = (x / (float)ScopeTexW) * Mathf.PI * 2f + _scopeAnimPhase;

            float yT = EvalWave(t, TargetW1.Value, TargetP1.Value, TargetW2.Value, TargetP2.Value, TargetA3.Value, TargetW3.Value, TargetP3.Value);
            float yC = EvalWave(t, _uiW1, _uiP1, _uiW2, _uiP2, PlayerA3.Value, PlayerW3.Value, PlayerP3.Value);

            PlotPixelThick(x, MapWaveToPixelY(yT), targetCol);
            PlotPixelThick(x, MapWaveToPixelY(yC), coopCol);
        }

        _scopeTex.SetPixels32(_scopePixels);
        _scopeTex.Apply(false);
    }

    private static int MapWaveToPixelY(float waveVal)
    {
        float norm = (waveVal / 1.85f) * 0.45f + 0.5f;
        return Mathf.Clamp(Mathf.RoundToInt(norm * (ScopeTexH - 1)), 2, ScopeTexH - 3);
    }

    private void PlotPixelThick(int x, int y, Color32 col)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            int py = y + dy;
            if (py >= 0 && py < ScopeTexH)
            {
                _scopePixels[py * ScopeTexW + x] = col;
            }
        }
    }

    private void OnGUI()
    {
        if (ActiveLocalOscilloscope != this || NetworkBootstrapUI.IsPauseMenuOpen) return;

        float w = Mathf.Min(700f, Screen.width - 30f);
        float h = Mathf.Min(450f, Screen.height - 24f);
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        GUI.DrawTexture(new Rect(x - 3f, y - 3f, w + 6f, h + 6f), _borderTex);
        GUI.DrawTexture(new Rect(x, y, w, h), _bgTex);

        GUILayout.BeginArea(new Rect(x + 22f, y + 16f, w - 44f, h - 32f));

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(0.35f, 1f, 0.5f);

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            richText = true
        };
        infoStyle.normal.textColor = new Color(0.86f, 0.96f, 0.86f);

        GUILayout.Label("ОСЦИЛОГРАФ «С1-87» — ФАЗОВА СИНХРОНІЗАЦІЯ ТА АКУСТИЧНИЙ РЕЗОНАНС", titleStyle);
        GUILayout.Space(6);

        // Екран осцилографа
        Rect scopeRect = GUILayoutUtility.GetRect(w - 44f, Mathf.Clamp(h - 300f, 110f, 165f));
        GUI.DrawTexture(scopeRect, _scopeTex, ScaleMode.StretchToFill);

        GUILayout.Space(8);

        // Математична телеметрія (MSE + % збігу + підказка по кабіні для Гравця 2!)
        float matchPct = Mathf.Clamp(100f * (1f - Mathf.Sqrt(Mathf.Max(0f, CurrentMse.Value)) / 1.25f), 0f, 100f);
        string targetMatName = GetBoothMaterialLabel(TargetBoothIndex.Value);
        string activeMatName = ActiveBoothIndex.Value >= 0 ? GetBoothMaterialLabel(ActiveBoothIndex.Value) : "НЕ УВІМКНЕНО";
        int targetPhaseDeg = Mathf.RoundToInt(TargetP3.Value * Mathf.Rad2Deg) % 360;
        int activePhaseDeg = Mathf.RoundToInt(PlayerP3.Value * Mathf.Rad2Deg) % 360;

        GUILayout.Label(
            $"<b>Зелена крива:</b> Еталон  |  <b>Бурштинова:</b> Сумарний сигнал гравців  |  " +
            $"<b>MSE:</b> <color={(CurrentMse.Value <= maxAllowedMse ? "#4CFF72" : "#FFB830")}>{CurrentMse.Value:F4}</color>  |  " +
            $"<b>Збіг:</b> {matchPct:F1}%",
            infoStyle
        );

        GUILayout.Label(
            $"<b>3-тя гармоніка (Кабіна Гравця 2 — продиктуйте по рації [T]):</b> " +
            $"Потрібен матеріал <color=#FFD147>{targetMatName}</color> (Фаза {targetPhaseDeg}°)  |  " +
            $"Зараз: <color=#72F2FF>{activeMatName}</color> ({activePhaseDeg}°)",
            infoStyle
        );

        GUILayout.Space(8);

        // Повзунки Гравця 1 (Гармоніка 1 та Гармоніка 2)
        GUI.enabled = !IsSolved.Value;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Гармоніка 1 — Частота ω₁: {_uiW1:F2} (Ціль: {TargetW1.Value:F2})", infoStyle, GUILayout.Width(310));
        float newW1 = GUILayout.HorizontalSlider(_uiW1, 0.5f, 3.0f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Гармоніка 1 — Фаза φ₁: {_uiP1:F2} рад", infoStyle, GUILayout.Width(310));
        float newP1 = GUILayout.HorizontalSlider(_uiP1, 0.0f, Mathf.PI * 2f);
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Гармоніка 2 — Частота ω₂: {_uiW2:F2} (Ціль: {TargetW2.Value:F2})", infoStyle, GUILayout.Width(310));
        float newW2 = GUILayout.HorizontalSlider(_uiW2, 1.5f, 4.5f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Гармоніка 2 — Фаза φ₂: {_uiP2:F2} рад", infoStyle, GUILayout.Width(310));
        float newP2 = GUILayout.HorizontalSlider(_uiP2, 0.0f, Mathf.PI * 2f);
        GUILayout.EndHorizontal();

        GUI.enabled = true;

        bool changed = Mathf.Abs(newW1 - _uiW1) > 0.005f || Mathf.Abs(newP1 - _uiP1) > 0.005f ||
                       Mathf.Abs(newW2 - _uiW2) > 0.005f || Mathf.Abs(newP2 - _uiP2) > 0.005f;
        if (changed)
        {
            _uiW1 = Mathf.Round(newW1 * 4f) / 4f;
            _uiP1 = Mathf.Round(newP1 * 8f) / 8f;
            _uiW2 = Mathf.Round(newW2 * 4f) / 4f;
            _uiP2 = Mathf.Round(newP2 * 8f) / 8f;

            if (Time.time >= _nextRpcSendTime)
            {
                _nextRpcSendTime = Time.time + 0.08f;
                UpdatePlayerHarmonicsServerRpc(_uiW1, _uiP1, _uiW2, _uiP2);
            }
        }

        GUILayout.Space(10);

        // Прогрес фіксації резонансу
        string lockText = IsSolved.Value
            ? "РЕЗОНАНС ЗАФІКСОВАНО — ШЛЮЗ СЕРВЕРНОЇ ВІДКРИТО!"
            : $"Фіксація резонансу (утримуйте MSE ≤ {maxAllowedMse:F3}): {Mathf.RoundToInt(LockProgress.Value * 100f)}%";
        GUILayout.Label(lockText, infoStyle);

        Rect barBg = GUILayoutUtility.GetRect(w - 44f, 16f);
        GUI.DrawTexture(barBg, _borderTex);
        GUI.DrawTexture(new Rect(barBg.x + 2f, barBg.y + 2f, (barBg.width - 4f) * LockProgress.Value, barBg.height - 4f), Texture2D.whiteTexture);

        GUILayout.Space(10);
        if (GUILayout.Button("ВИЙТИ З ОСЦИЛОГРАФА [Esc / Q]", GUILayout.Height(28)))
        {
            CloseOscilloscope();
        }

        GUILayout.EndArea();
    }

    private string GetBoothMaterialLabel(int idx)
    {
        if (boothEmitters != null && idx >= 0 && idx < boothEmitters.Length && boothEmitters[idx] != null)
        {
            return boothEmitters[idx].BoothMaterial switch
            {
                AcousticMaterialType.Concrete => "Кабіна 1 — БЕТОН (650 Гц)",
                AcousticMaterialType.Wood => "Кабіна 2 — ДЕРЕВО (1650 Гц)",
                AcousticMaterialType.Metal => "Кабіна 3 — МЕТАЛ (950 Гц)",
                AcousticMaterialType.AcousticFoam => "Кабіна 4 — ПОРОЛОН (420 Гц)",
                _ => boothEmitters[idx].BoothMaterial.ToString()
            };
        }
        return $"Кабіна #{idx + 1}";
    }

    private void CreateScopeTextures()
    {
        _scopeTex = new Texture2D(ScopeTexW, ScopeTexH, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _scopePixels = new Color32[ScopeTexW * ScopeTexH];

        _bgTex = new Texture2D(1, 1);
        _bgTex.SetPixel(0, 0, new Color(0.06f, 0.09f, 0.07f, 0.97f));
        _bgTex.Apply();

        _borderTex = new Texture2D(1, 1);
        _borderTex.SetPixel(0, 0, new Color(0.32f, 0.88f, 0.44f, 0.85f));
        _borderTex.Apply();
    }
}