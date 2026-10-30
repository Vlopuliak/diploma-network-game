using TheCopy.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RetroAuditorHud : MonoBehaviour
{
    public static RetroAuditorHud Instance { get; private set; }

    [Header("1. URP VHS Камера (Fears to Fathom Style)")]
    [SerializeField] private bool enableUrpPostProcessing = true;
    [Range(0f, 1f)]
    [SerializeField] private float filmGrainIntensity = 0.85f;
    [Range(0f, 1f)]
    [SerializeField] private float chromaticAberration = 0.48f;
    [Range(0f, 1f)]
    [SerializeField] private float vignetteIntensity = 0.46f;
    [Range(-0.5f, 0f)]
    [SerializeField] private float lensCurvature = -0.18f;

    [Header("2. Аналоговий шум VHS-касети та Сканлайни")]
    [SerializeField] private bool showVhsTapeNoise = true;
    [Range(0f, 0.2f)]
    [SerializeField] private float vhsSnowOpacity = 0.055f;
    [Range(0f, 0.2f)]
    [SerializeField] private float scanlineOpacity = 0.05f;
    [SerializeField] private bool showTrackingGlitchBar = true;

    [Header("3. Репліки / Думки персонажа (Жовта плашка Fears to Fathom)")]
    [SerializeField] private string startPrologueThought = "ПРИБУТТЯ: СЕКТОР «СИНХРОНІЗАЦІЯ». ЗНАЙТИ ЖИВЛЕННЯ ШЛЮЗУ";
    [SerializeField] private string startSubThought = "Натисніть [T] для перевірки рації з напарником";

    private PlayerController _localPlayer;
    private VoiceNoiseEmitter _localVoice;
    private Camera _boundCamera;
    private Volume _vhsVolume;

    private Texture2D _whiteTex;
    private Texture2D _scanlineTex;
    private Texture2D _noiseTex;
    private Texture2D _vignetteBorderTex;
    private Texture2D _circleReticleTex;

    private float _displayedNoiseLevel;
    private float _whistleSpikeTimer;
    private float _nextNoiseFrameTime;
    private Rect _noiseUvRect = new Rect(0, 0, 1, 1);
    private float _trackingBarY = -100f;

    // Таймер для жовтої плашки думок (як у Fears to Fathom)
    private string _activeBannerQuote = "";
    private string _activeBannerSub = "";
    private float _bannerTimer = 0f;
    private bool _prologueShown = false;

    private void Awake()
    {
        Instance = this;
        GenerateProceduralTextures();
    }

    /// <summary>
    /// Виклич цей метод з будь-якого тригера чи записки, щоб показати жовту плашку Fears to Fathom!
    /// </summary>
    public static void ShowThoughtBanner(string mainQuote, string subText = "", float duration = 6.5f)
    {
        if (Instance == null) return;
        Instance._activeBannerQuote = mainQuote.ToUpper();
        Instance._activeBannerSub = subText;
        Instance._bannerTimer = duration;
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            _prologueShown = false;
            return;
        }

        // 1. Знаходимо локального гравця та підключаємо VHS Post-Processing до його камери
        if (_localPlayer == null &&
            NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            GameObject playerObj = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
            _localPlayer = playerObj.GetComponent<PlayerController>();
            _localVoice = playerObj.GetComponent<VoiceNoiseEmitter>();

            if (!_prologueShown)
            {
                _prologueShown = true;
                ShowThoughtBanner(startPrologueThought, startSubThought, 8.0f);
            }
        }

        if (_localPlayer == null) return;

        EnsureCameraPostProcessingEnabled();

        // 2. Оновлюємо таймер жовтої плашки реплік
        if (_bannerTimer > 0f)
        {
            _bannerTimer -= Time.deltaTime;
        }

        // 3. Анімація VHS-шуму (24 FPS, як на справжній касеті)
        if (Time.time >= _nextNoiseFrameTime)
        {
            _nextNoiseFrameTime = Time.time + (1f / 24f);
            _noiseUvRect = new Rect(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Screen.width / 128f,
                Screen.height / 128f
            );
        }

        // Рух смуги VHS-трекінгу зверху вниз
        _trackingBarY += 95f * Time.deltaTime;
        if (_trackingBarY > Screen.height + 300f)
        {
            _trackingBarY = -150f;
        }

        // 4. Обчислення рівня шуму для верхньої шкали
        if (Input.GetKeyDown(KeyCode.V))
        {
            _whistleSpikeTimer = 0.65f;
            ShowThoughtBanner("ВИКОРИСТАНО ЗВУКОВУ ПРИМАНКУ [V]", "Акустичні сенсори комплексу зафіксували сигнал!", 3.0f);
        }

        if (_whistleSpikeTimer > 0f) _whistleSpikeTimer -= Time.deltaTime;

        float voiceNoise = _localVoice != null ? _localVoice.SyncedVoiceLoudness.Value : 0f;
        float moveNoise = _localPlayer.CurrentMovementNoise;
        float whistleNoise = _whistleSpikeTimer > 0f ? 1.0f : 0f;

        float targetNoise = Mathf.Max(voiceNoise, moveNoise, whistleNoise);
        float lerpSpeed = targetNoise > _displayedNoiseLevel ? 18f : 4.5f;
        _displayedNoiseLevel = Mathf.Lerp(_displayedNoiseLevel, targetNoise, lerpSpeed * Time.deltaTime);
    }

    private void EnsureCameraPostProcessingEnabled()
    {
        if (!enableUrpPostProcessing) return;

        Camera cam = Camera.main;
        if (cam != null && cam != _boundCamera)
        {
            _boundCamera = cam;
            UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            camData.renderPostProcessing = true;
            SetupOrUpdateUrpVhsVolume();
        }
    }

    private void SetupOrUpdateUrpVhsVolume()
    {
        if (_vhsVolume == null)
        {
            _vhsVolume = FindFirstObjectByType<Volume>();
            if (_vhsVolume == null)
            {
                GameObject volObj = new GameObject("VHS_PostProcess_Volume");
                _vhsVolume = volObj.AddComponent<Volume>();
            }
        }

        _vhsVolume.isGlobal = true;
        _vhsVolume.priority = 10f;

        if (_vhsVolume.profile == null)
        {
            _vhsVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        VolumeProfile profile = _vhsVolume.profile;

        // 1. Крупне плівкове зерно (Film Grain — Fears to Fathom look)
        if (!profile.TryGet(out FilmGrain fg)) fg = profile.Add<FilmGrain>(true);
        fg.active = true;
        fg.type.Override(FilmGrainLookup.Large02);
        fg.intensity.Override(filmGrainIntensity);
        fg.response.Override(0.75f);

        // 2. Хроматична аберація (RGB-розшарування по краях лінзи)
        if (!profile.TryGet(out ChromaticAberration ca)) ca = profile.Add<ChromaticAberration>(true);
        ca.active = true;
        ca.intensity.Override(chromaticAberration);

        // 3. Темна віньєтка камери
        if (!profile.TryGet(out Vignette vg)) vg = profile.Add<Vignette>(true);
        vg.active = true;
        vg.intensity.Override(vignetteIntensity);
        vg.smoothness.Override(0.55f);

        // 4. Викривлення лінзи старого камкордера (Lens Distortion)
        if (!profile.TryGet(out LensDistortion ld)) ld = profile.Add<LensDistortion>(true);
        ld.active = true;
        ld.intensity.Override(lensCurvature);
        ld.scale.Override(0.99f);

        // 5. Атмосферна кольорокорекція 80-х (Контраст + Теплий VHS відтінок)
        if (!profile.TryGet(out ColorAdjustments col)) col = profile.Add<ColorAdjustments>(true);
        col.active = true;
        col.contrast.Override(24f);
        col.saturation.Override(-22f);
        col.postExposure.Override(0.15f);
        col.colorFilter.Override(new Color(1f, 0.96f, 0.87f));

        // 6. М'яке світіння ЕПТ-моніторів та ламп (Bloom)
        if (!profile.TryGet(out Bloom bl)) bl = profile.Add<Bloom>(true);
        bl.active = true;
        bl.threshold.Override(0.85f);
        bl.intensity.Override(1.25f);
        bl.scatter.Override(0.65f);
    }

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;
        bool isInGame = NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer;
        if (!isInGame || _localPlayer == null) return;

        // 1. ВІНЬЄТКА КАМКОРДЕРА + ЖИВИЙ VHS ШУМ ТА СКАНЛАЙНИ
        DrawVhsCameraOverlays();

        if (NetworkBootstrapUI.IsPauseMenuOpen) return;

        bool anyPopupOpen = NetworkKeypadTerminal.IsAnyTerminalOpen || NetworkReadableNote.IsAnyNoteOpen;

        if (!anyPopupOpen)
        {
            // 2. БІЧНІ СТРІЛКИ ВИДОШУКАЧА < > ТА КРУГЛИЙ ПРИЦІЛ "o" (1-в-1 Fears to Fathom)
            DrawFearsToFathomViewfinder();

            // 3. ЖОВТА ПЛАШКА ДУМОК / ДІАЛОГІВ (" CAR WON'T MAKE IT THERE style)
            if (_bannerTimer > 0f && !string.IsNullOrEmpty(_activeBannerQuote))
            {
                DrawYellowQuoteBanner(_activeBannerQuote, _activeBannerSub);
            }
        }

        // 4. ВЕРХНЯ СЕГМЕНТОВАНА ШКАЛА ШУМУ ТА РАЦІЇ (1-в-1 Outlast Trials — Скрін 1)
        DrawOutlastTopMeterBar();
    }

    private void DrawVhsCameraOverlays()
    {
        Color prev = GUI.color;

        // М'яка темна рамка по краях екрана
        GUI.color = new Color(1f, 1f, 1f, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _vignetteBorderTex);

        // Анімований VHS-сніг / зерно касети
        if (showVhsTapeNoise && vhsSnowOpacity > 0.001f)
        {
            GUI.color = new Color(1f, 1f, 1f, vhsSnowOpacity);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, Screen.width, Screen.height), _noiseTex, _noiseUvRect);
        }

        // Горизонтальні лінії розгортки (Scanlines)
        if (scanlineOpacity > 0.001f)
        {
            GUI.color = new Color(1f, 1f, 1f, scanlineOpacity);
            GUI.DrawTextureWithTexCoords(
                new Rect(0, 0, Screen.width, Screen.height),
                _scanlineTex,
                new Rect(0, 0, 1f, Screen.height / 4f)
            );
        }

        // Рухома смуга перешкод VHS-трекінгу
        if (showTrackingGlitchBar && _trackingBarY >= 0f && _trackingBarY <= Screen.height)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.04f);
            GUI.DrawTextureWithTexCoords(
                new Rect(0, _trackingBarY, Screen.width, 14f),
                _noiseTex,
                new Rect(_noiseUvRect.x, _noiseUvRect.y, 4f, 0.2f)
            );
        }

        GUI.color = prev;
    }

    private void DrawFearsToFathomViewfinder()
    {
        float w = Screen.width;
        float h = Screen.height;
        float cx = w / 2f;
        float cy = h / 2f;

        Color prev = GUI.color;

        // Кругле кільце прицілу "o" по центру екрана (як на скріні Fears to Fathom)
        GUI.color = new Color(0.95f, 0.92f, 0.82f, 0.55f);
        GUI.DrawTexture(new Rect(cx - 6f, cy - 6f, 12f, 12f), _circleReticleTex);

        // Тонкі кутові рамки об'єктива камери (як на Скріні 1 з Outlast Trials)
        GUI.color = new Color(0.75f, 0.8f, 0.75f, 0.28f);
        float m = 26f;
        float len = 38f;
        // Лівий верхній кут
        GUI.DrawTexture(new Rect(m, m, len, 1.5f), _whiteTex);
        GUI.DrawTexture(new Rect(m, m, 1.5f, len), _whiteTex);
        // Правий верхній кут
        GUI.DrawTexture(new Rect(w - m - len, m, len, 1.5f), _whiteTex);
        GUI.DrawTexture(new Rect(w - m, m, 1.5f, len), _whiteTex);
        // Лівий нижній кут
        GUI.DrawTexture(new Rect(m, h - m, len, 1.5f), _whiteTex);
        GUI.DrawTexture(new Rect(m, h - m - len, 1.5f, len), _whiteTex);
        // Правий нижній кут
        GUI.DrawTexture(new Rect(w - m - len, h - m, len, 1.5f), _whiteTex);
        GUI.DrawTexture(new Rect(w - m, h - m - len, 1.5f, len), _whiteTex);

        // Бічні шеврони < та > по краях екрана (як у Fears to Fathom)
        GUIStyle chevronStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        chevronStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 0.35f);
        GUI.Label(new Rect(18f, cy - 20f, 30f, 40f), "<", chevronStyle);
        GUI.Label(new Rect(w - 48f, cy - 20f, 30f, 40f), ">", chevronStyle);

        GUI.color = prev;
    }

    // --- ЖОВТА ПЛАШКА З ЛАПКАМИ 1-В-1 ЯК У FEARS TO FATHOM (" CAR WON'T MAKE IT THERE) ---
    private void DrawYellowQuoteBanner(string mainText, string subText)
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height * 0.34f; // Трохи вище центру, як на скріні Fears to Fathom!

        string fullQuote = $"“ {mainText} ";

        GUIStyle quoteStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        quoteStyle.normal.textColor = new Color(0.08f, 0.08f, 0.08f, 1f); // Чорний текст

        Vector2 textSize = quoteStyle.CalcSize(new GUIContent(fullQuote));
        float boxW = textSize.x + 22f;
        float boxH = 30f;
        float boxX = cx - boxW / 2f;
        float boxY = cy;

        Color prev = GUI.color;

        // Чорна тінь під жовтою плашкою
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(boxX + 3f, boxY + 3f, boxW, boxH), _whiteTex);

        // Фірмовий теплий жовтий фон Fears to Fathom (#F2C94C)
        GUI.color = new Color(0.95f, 0.78f, 0.24f, 0.96f);
        GUI.DrawTexture(new Rect(boxX, boxY, boxW, boxH), _whiteTex);

        // Чорні бічні акценти по краях плашки
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        GUI.DrawTexture(new Rect(boxX - 4f, boxY + 3f, 4f, boxH - 6f), _whiteTex);
        GUI.DrawTexture(new Rect(boxX + boxW, boxY + 3f, 4f, boxH - 6f), _whiteTex);
        GUI.color = prev;

        GUI.Label(new Rect(boxX, boxY, boxW, boxH), fullQuote, quoteStyle);

        // Нижній білий підпис з тінню ("WHY CAN'T YOU FIX IT?" style)
        if (!string.IsNullOrEmpty(subText))
        {
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            Rect subRect = new Rect(cx - 300f, boxY + boxH + 8f, 600f, 28f);
            subStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(subRect.x + 1.5f, subRect.y + 1.5f, subRect.width, subRect.height), subText, subStyle);

            subStyle.normal.textColor = new Color(0.92f, 0.90f, 0.82f, 0.95f);
            GUI.Label(subRect, subText, subStyle);
        }
    }

    // --- ВЕРХНЯ СЕГМЕНТОВАНА ШКАЛА 1-В-1 ЯК У OUTLAST TRIALS (СКРІН 1: MURKOFF PROPERTY) ---
    private void DrawOutlastTopMeterBar()
    {
        float cx = Screen.width / 2f;
        float topY = 24f;

        Color prev = GUI.color;

        // Верхній заголовок над шкалою ("MURKOFF PROPERTY" style)
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        bool isRadioHeld = Input.GetKey(KeyCode.T);
        string statusHeader = isRadioHeld
            ? "РАДІОХВИЛЯ 2200 Hz [ПЕРЕДАЧА]"
            : (_localPlayer.IsCrouching ? "SYNCHRONIZATION PROPERTY [СТЕЛС]" : "SYNCHRONIZATION PROPERTY");

        headerStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(cx - 150f + 1f, topY + 1f, 300f, 20f), statusHeader, headerStyle);
        headerStyle.normal.textColor = isRadioHeld ? new Color(0.95f, 0.8f, 0.25f) : new Color(0.78f, 0.82f, 0.76f);
        GUI.Label(new Rect(cx - 150f, topY, 300f, 20f), statusHeader, headerStyle);

        // Коробка шкали (іконка зліва + 16 вертикальних поділок)
        int totalBars = 16;
        float barW = 8f;
        float barH = 18f;
        float gap = 2f;
        float iconBoxW = 22f;
        float totalWidth = iconBoxW + 4f + totalBars * (barW + gap) + 4f;
        float startX = cx - totalWidth / 2f;
        float barY = topY + 20f;

        // Темна підкладка всієї шкали
        GUI.color = new Color(0.08f, 0.09f, 0.08f, 0.85f);
        GUI.DrawTexture(new Rect(startX - 2f, barY - 2f, totalWidth + 4f, barH + 4f), _whiteTex);

        // Квадратик іконки зліва (як жовта блискавка на Скріні 1)
        GUI.color = new Color(0.18f, 0.18f, 0.16f, 0.95f);
        GUI.DrawTexture(new Rect(startX, barY, iconBoxW, barH), _whiteTex);

        GUIStyle iconStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        iconStyle.normal.textColor = _localPlayer.IsFlashlightOn.Value
            ? new Color(0.95f, 0.8f, 0.2f)
            : new Color(0.5f, 0.55f, 0.5f);
        GUI.Label(new Rect(startX, barY - 1f, iconBoxW, barH + 2f), "⚡", iconStyle);

        // Малюємо 16 вертикальних сегментів
        float segStartX = startX + iconBoxW + 4f;
        for (int i = 0; i < totalBars; i++)
        {
            float threshold = (i + 1) / (float)totalBars;
            bool isActive = _displayedNoiseLevel >= (i / (float)totalBars) && _displayedNoiseLevel > 0.02f;

            Color segCol;
            if (!isActive)
            {
                // Неактивні поділки — темно-сірі, а перші дві трохи червонуваті, як на скріні Outlast Trials
                segCol = (i < 2) ? new Color(0.35f, 0.15f, 0.15f, 0.65f) : new Color(0.22f, 0.24f, 0.22f, 0.65f);
            }
            else if (threshold <= 0.6f)
            {
                segCol = new Color(0.88f, 0.90f, 0.86f, 0.95f); // Чистий біло-сірий (як на Скріні 1)
            }
            else if (threshold <= 0.82f)
            {
                segCol = new Color(0.95f, 0.78f, 0.24f, 0.98f); // Жовтий (попередження)
            }
            else
            {
                segCol = new Color(0.9f, 0.2f, 0.18f, 1f);      // Червоний (NPC чує!)
            }

            GUI.color = segCol;
            GUI.DrawTexture(new Rect(segStartX + i * (barW + gap), barY, barW, barH), _whiteTex);
        }

        GUI.color = prev;
    }

    private void GenerateProceduralTextures()
    {
        _whiteTex = new Texture2D(1, 1);
        _whiteTex.SetPixel(0, 0, Color.white);
        _whiteTex.Apply();

        // Сканлайни (4 пікселі)
        _scanlineTex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        _scanlineTex.filterMode = FilterMode.Point;
        _scanlineTex.wrapMode = TextureWrapMode.Repeat;
        _scanlineTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 1f));
        _scanlineTex.SetPixel(0, 1, new Color(0f, 0f, 0f, 0f));
        _scanlineTex.SetPixel(0, 2, new Color(0f, 0f, 0f, 0.35f));
        _scanlineTex.SetPixel(0, 3, new Color(0f, 0f, 0f, 0f));
        _scanlineTex.Apply();

        // Текстура VHS-шуму (128x128)
        _noiseTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        _noiseTex.filterMode = FilterMode.Point;
        _noiseTex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float v = Random.value;
                _noiseTex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        }
        _noiseTex.Apply();

        // Темна рамка віньєтки об'єктива (64x64)
        _vignetteBorderTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        _vignetteBorderTex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float nx = (x / 63f) * 2f - 1f;
                float ny = (y / 63f) * 2f - 1f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01((dist - 0.72f) / 0.65f);
                _vignetteBorderTex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha * alpha));
            }
        }
        _vignetteBorderTex.Apply();

        // Порожнисте кільце прицілу "o" (16x16, як у Fears to Fathom)
        _circleReticleTex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                float a = (d >= 4.2f && d <= 6.2f) ? 1f : 0f;
                _circleReticleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        _circleReticleTex.Apply();
    }
}