using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ретро-карта комплексу та шпаргалка маршруту на кнопці [Tab] + автоматичне підсилення ліхтарика гравця.
/// Дозволяє під час демонстрації миттєво бачити свою позицію ("ВИ ТУТ"), поточну зону й наступний крок.
/// Повісь цей скрипт на той самий об'єкт, де висить RetroAuditorHud (наприклад, NetworkManager).
/// </summary>
public class DemoMapAndChecklistHud : MonoBehaviour
{
    [Header("Налаштування карти на [Tab]")]
    [SerializeField] private KeyCode toggleMapKey = KeyCode.Tab;
    [SerializeField] private bool showMiniHintTopRight = true;

    [Header("Автопідсилення ліхтарика гравця для комфортної демонстрації")]
    [SerializeField] private bool autoBoostPlayerFlashlight = true;
    [SerializeField] private float boostedFlashlightIntensity = 24f;
    [SerializeField] private float boostedFlashlightRange = 28f;
    [SerializeField] private float boostedSpotAngle = 64f;

    private bool _isMapOpen = false;
    private bool _flashlightBoosted = false;
    private Transform _localPlayerTransform;

    private Texture2D _bgTex;
    private Texture2D _borderTex;
    private Texture2D _whiteTex;

    private void Awake()
    {
        _bgTex = MakeSolid(new Color(0.06f, 0.09f, 0.07f, 0.94f));
        _borderTex = MakeSolid(new Color(0.35f, 0.92f, 0.48f, 0.85f));
        _whiteTex = MakeSolid(Color.white);
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            _localPlayerTransform = null;
            _flashlightBoosted = false;
            return;
        }

        if (_localPlayerTransform == null &&
            NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            _localPlayerTransform = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
        }

        if (autoBoostPlayerFlashlight && !_flashlightBoosted && _localPlayerTransform != null)
        {
            BoostPlayerFlashlights();
            _flashlightBoosted = true;
        }

        if (Input.GetKeyDown(toggleMapKey) &&
            !NetworkBootstrapUI.IsPauseMenuOpen &&
            !NetworkGridOsTerminal.IsAnyGridTerminalOpen &&
            !NetworkKeypadTerminal.IsAnyTerminalOpen &&
            !NetworkAcousticOscilloscope.IsAnyOscilloscopeOpen)
        {
            _isMapOpen = !_isMapOpen;
        }
    }

    private void BoostPlayerFlashlights()
    {
        Light[] lights = _localPlayerTransform.GetComponentsInChildren<Light>(true);
        foreach (Light l in lights)
        {
            if (l.type == LightType.Spot)
            {
                l.intensity = boostedFlashlightIntensity;
                l.range = boostedFlashlightRange;
                l.spotAngle = boostedSpotAngle;
                l.innerSpotAngle = boostedSpotAngle * 0.55f;
                l.color = new Color(1.0f, 0.92f, 0.78f);
            }
        }
    }

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;
        if (_localPlayerTransform == null || NetworkBootstrapUI.IsPauseMenuOpen) return;

        Vector3 pos = _localPlayerTransform.position;
        string zoneName = DetectCurrentZoneName(pos);
        string currentObjective = DetectCurrentObjective(pos);

        // Компактна підказка у правому верхньому куті (не заважає, завжди нагадує куди йти)
        if (showMiniHintTopRight && !_isMapOpen)
        {
            float bw = 330f;
            float bh = 74f;
            float bx = Screen.width - bw - 16f;
            float by = 16f;

            GUI.DrawTexture(new Rect(bx - 1f, by - 1f, bw + 2f, bh + 2f), _borderTex);
            GUI.DrawTexture(new Rect(bx, by, bw, bh), _bgTex);

            GUIStyle sTitle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            sTitle.normal.textColor = new Color(0.98f, 0.80f, 0.24f);

            GUIStyle sBody = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            sBody.normal.textColor = new Color(0.86f, 0.95f, 0.86f);

            GUI.Label(new Rect(bx + 10f, by + 6f, bw - 20f, 20f), $"ЛОКАЦІЯ: {zoneName}", sTitle);
            GUI.Label(new Rect(bx + 10f, by + 26f, bw - 20f, 30f), $"ЦІЛЬ: {currentObjective}", sBody);
            GUI.Label(new Rect(bx + 10f, by + 54f, bw - 20f, 18f), "[Tab] Схема комплексу  |  [F] Ліхтарик  |  [T] Рація", sBody);
        }

        if (_isMapOpen)
        {
            DrawSchematicMapOverlay(pos, zoneName, currentObjective);
        }
    }

    private void DrawSchematicMapOverlay(Vector3 playerPos, string zoneName, string objective)
    {
        float w = 740f;
        float h = 520f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        GUI.DrawTexture(new Rect(x - 3f, y - 3f, w + 6f, h + 6f), _borderTex);
        GUI.DrawTexture(new Rect(x, y, w, h), _bgTex);

        GUIStyle header = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        header.normal.textColor = new Color(0.35f, 1f, 0.50f);
        GUI.Label(new Rect(x, y + 10f, w, 28f), "СХЕМА КОМПЛЕКСУ «СИНХРОНІЗАЦІЯ» — МАРШРУТ ДЕМОНСТРАЦІЇ", header);

        // Область 2D-схеми зліва (X: -50..+50, Z: -70..+138)
        Rect mapRect = new Rect(x + 22f, y + 48f, 380f, 430f);
        GUI.DrawTexture(new Rect(mapRect.x - 1f, mapRect.y - 1f, mapRect.width + 2f, mapRect.height + 2f), _borderTex);
        GUI.DrawTexture(mapRect, MakeSolid(new Color(0.03f, 0.05f, 0.04f, 1f)));

        // Малюємо прямокутники зон
        DrawMapZone(mapRect, -36f, 36f, -53f, 0f, new Color(0.28f, 0.22f, 0.14f, 0.55f), "ПРОЛОГ: ДВІР");
        DrawMapZone(mapRect, -12f, 12f, 0f, 22f, new Color(0.20f, 0.35f, 0.35f, 0.70f), "Зона 1\nКПП");
        DrawMapZone(mapRect, -16f, 16f, 22f, 52f, new Color(0.38f, 0.32f, 0.14f, 0.75f), "Зона 2\nАТРІУМ");
        DrawMapZone(mapRect, -48f, -16f, 16f, 44f, new Color(0.18f, 0.30f, 0.22f, 0.65f), "Зона 3\n28 ЕПТ");
        DrawMapZone(mapRect, -48f, -16f, 44f, 74f, new Color(0.14f, 0.52f, 0.38f, 0.85f), "Зона 4\nПАЗЛ №1\nVoIP");
        DrawMapZone(mapRect, 16f, 48f, 16f, 44f, new Color(0.42f, 0.26f, 0.14f, 0.65f), "Зона 5\nОФІС");
        DrawMapZone(mapRect, 16f, 48f, 44f, 74f, new Color(0.22f, 0.42f, 0.52f, 0.65f), "Зона 6\nМЕДБЛОК");
        DrawMapZone(mapRect, -14f, 14f, 52f, 74f, new Color(0.45f, 0.22f, 0.18f, 0.70f), "Зона 7\nCCTV");
        DrawMapZone(mapRect, -48f, -14f, 74f, 108f, new Color(0.55f, 0.18f, 0.16f, 0.80f), "Зона 8\nСЕРВЕРНА\n(NPC)");
        DrawMapZone(mapRect, 14f, 48f, 74f, 108f, new Color(0.45f, 0.30f, 0.12f, 0.65f), "Зона 9\nРЕАКТОР");
        DrawMapZone(mapRect, -20f, 20f, 108f, 136f, new Color(0.50f, 0.16f, 0.36f, 0.70f), "Зона 10\nЯДРО");

        // Точка гравця "ВИ ТУТ"
        Vector2 pScreen = WorldToMap(mapRect, playerPos.x, playerPos.z);
        Color prev = GUI.color;
        GUI.color = Color.yellow;
        GUI.DrawTexture(new Rect(pScreen.x - 5f, pScreen.y - 5f, 10f, 10f), _whiteTex);
        GUI.color = prev;

        // Права панель: легенда кольорів + шпаргалка кроків для 2 гравців
        Rect infoRect = new Rect(x + 418f, y + 48f, 302f, 430f);
        GUILayout.BeginArea(infoRect);

        GUIStyle stHead = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, richText = true };
        stHead.normal.textColor = new Color(1f, 0.80f, 0.24f);

        GUIStyle stText = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, richText = true };
        stText.normal.textColor = new Color(0.88f, 0.96f, 0.88f);

        GUILayout.Label($"ВИ ЗАРАЗ: <color=#4CFF72>{zoneName}</color>", stHead);
        GUILayout.Label($"<b>Наступна дія:</b> {objective}", stText);
        GUILayout.Space(10);

        GUILayout.Label("ЛЕГЕНДА СМУГ НА ПІДЛОЗІ:", stHead);
        GUILayout.Label("• <color=#FFBE24><b>ЖОВТА:</b></color> Головний шлях по сюжету", stText);
        GUILayout.Label("• <color=#4CFF72><b>ЗЕЛЕНА:</b></color> Гравець 1 (GRiD-87 / Осцилограф)", stText);
        GUILayout.Label("• <color=#4CE0FF><b>БЛАКИТНА:</b></color> Гравець 2 (КПП код / 4 кабіни VoIP)", stText);
        GUILayout.Label("• <color=#FF4D3B><b>ЧЕРВОНА:</b></color> Вихід у Серверну (Зона 8, NPC)", stText);
        GUILayout.Space(10);

        GUILayout.Label("ПЛАН ДЕМО (5 ЕТАПІВ):", stHead);
        GUILayout.Label("1. Пролом у паркані -> КПП (код <b>8701</b>) + Східний корпус (GRiD-87: MediaRepair → Initialize → GRiDManager).", stText);
        GUILayout.Label("2. Розсувний <b>Ракетний люк</b> + парні важелі [E] внизу спуску.", stText);
        GUILayout.Label("3. Прохід через Зону 1 і Атріум по <b>ЖОВТІЙ</b> смузі в Зону 4.", stText);
        GUILayout.Label("4. Тест <b>VoIP крізь 4 матеріали</b> (Бетон/Дерево/Метал/Поролон) + <b>Осцилограф «С1-87»</b> (MSE).", stText);
        GUILayout.Label("5. Відкриття шлюзу Серверної (<b>ЧЕРВОНА</b> смуга) -> демонстрація NPC напарника.", stText);

        GUILayout.EndArea();

        GUIStyle footer = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
        footer.normal.textColor = new Color(0.7f, 0.85f, 0.72f);
        GUI.Label(new Rect(x, y + h - 30f, w, 24f), "Натисніть [Tab] щоб сховати схему", footer);
    }

    private void DrawMapZone(Rect mapRect, float x1, float x2, float z1, float z2, Color fill, string label)
    {
        Vector2 bl = WorldToMap(mapRect, x1, z1);
        Vector2 tr = WorldToMap(mapRect, x2, z2);
        Rect r = new Rect(bl.x, tr.y, tr.x - bl.x, bl.y - tr.y);

        Color prev = GUI.color;
        GUI.color = fill;
        GUI.DrawTexture(r, _whiteTex);
        GUI.color = prev;

        GUIStyle lbl = new GUIStyle(GUI.skin.label) { fontSize = 9, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        lbl.normal.textColor = Color.white;
        GUI.Label(r, label, lbl);
    }

    private static Vector2 WorldToMap(Rect mapRect, float wx, float wz)
    {
        float nx = Mathf.InverseLerp(-52f, 52f, wx);
        float nz = Mathf.InverseLerp(-70f, 140f, wz);
        return new Vector2(mapRect.x + nx * mapRect.width, mapRect.y + (1f - nz) * mapRect.height);
    }

    private static string DetectCurrentZoneName(Vector3 p)
    {
        if (p.y > -2.0f) return p.z < -53f ? "Ліс перед периметром" : "Наземний двір (Пролог)";
        if (p.y < -7.5f) return "Технічний підповерх (Y = -9м)";
        if (p.z < 22f) return "Зона 1: Санпропускник і КПП";
        if (p.z < 52f && Mathf.Abs(p.x) <= 16f) return "Зона 2: Центральний Атріум";
        if (p.x < -16f && p.z < 44f) return "Зона 3: Калібрування (28 ЕПТ)";
        if (p.x < -16f && p.z < 74f) return "Зона 4: Акустична лабораторія (Пазл №1)";
        if (p.x > 16f && p.z < 44f) return "Зона 5: Адміністрація (Дерево)";
        if (p.x > 16f && p.z < 74f) return "Зона 6: Медблок і Кріо (Плитка)";
        if (Mathf.Abs(p.x) <= 14f && p.z < 74f) return "Зона 7: Контур Безпеки (CCTV)";
        if (p.x < -14f && p.z < 108f) return "Зона 8: Серверна матриць (NPC)";
        if (p.x > 14f && p.z < 108f) return "Зона 9: Реакторний зал";
        return "Зона 10: Центральне Ядро Архіву";
    }

    private static string DetectCurrentObjective(Vector3 p)
    {
        if (p.y > -2.0f)
        {
            if (p.z < -53f) return "Йдіть по ЖОВТІЙ смузі до пролому в паркані";
            return "Г2: КПП (код 8701) | Г1: Східний корпус (GRiD-87) → Ракетний люк";
        }
        if (p.z < 0f) return "Одночасно натисніть [E] на лівому і правому важелях";
        if (p.z < 45f && p.x > -16f) return "Йдіть по ЖОВТІЙ смузі через Атріум ліворуч у Зону 4";
        if (p.x < -16f && p.z >= 44f && p.z < 74f) return "Г1: Осцилограф (ЗЕЛЕНА) | Г2: Кабіни VoIP (БЛАКИТНА) → Шлюз";
        if (p.x < -14f && p.z >= 74f) return "Серверна Зони 8: стелс та тест адаптивного NPC";
        return "Тримайтесь кольорових смуг на підлозі або натисніть [Tab]";
    }

    private static Texture2D MakeSolid(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}