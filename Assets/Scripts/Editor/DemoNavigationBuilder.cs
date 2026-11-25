#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Генератор навігаційних смуг, 3D-вивісок (без просвічування крізь стіни й без дзеркалення)
/// та акцентного підсвічування для демонстрації проєкту «Копія».
///
/// Кольорове кодування ліній на підлозі:
///   - ЖОВТА / БУРШТИНОВА: Головний сюжетний маршрут (Ліс -> Пролом -> Спуск -> Атріум -> Зона 4)
///   - ЗЕЛЕНА:             Роль Гравця 1 (Термінали GRiD-87 / Звукорежисерська з Осцилографом С1-87)
///   - БЛАКИТНА (CYAN):    Роль Гравця 2 (КПП Діагностика / 4 Акустичні кабіни VoIP: Бетон, Дерево, Метал, Поролон)
///   - ЧЕРВОНА:            Вихід до Північної Серверної (Зона 8 — Стелс та NPC напарника)
/// </summary>
public static class DemoNavigationBuilder
{
    private const float SurfaceY = 0.06f;
    private const float MainY = -4.5f;
    private const float LineY = -4.46f;

    private static Material _matAmberLine;
    private static Material _matGreenLine;
    private static Material _matCyanLine;
    private static Material _matRedLine;
    private static Material _matSignFrame;
    private static Shader _worldTextShader;

    [MenuItem("The Copy/12. Додати НАВІГАЦІЙНІ СМУГИ ТА ВИВІСКИ (Для демонстрації)")]
    public static void BuildDemoNavigationOverlay()
    {
        GameObject old = GameObject.Find("Demo_Navigation_Lines_And_Signs");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
        }

        InitMaterials();

        if (_worldTextShader == null)
        {
            Debug.LogWarning("[DemoNavigationBuilder] Шейдер 'TheCopy/WorldTextURP' не знайдено! Додай TheCopy_WorldText.shader у проєкт, щоб текст не просвічувався крізь стіни.");
        }

        GameObject root = new GameObject("Demo_Navigation_Lines_And_Signs");
        Undo.RegisterCreatedObjectUndo(root, "Build Demo Navigation Overlay");

        BuildSurfacePrologueGuidance(CreateGroup("01_Surface_Prologue_Routes", root.transform).transform);
        BuildUndergroundMainRoute(CreateGroup("02_Underground_Atrium_To_Zone4", root.transform).transform);
        BuildZone4AcousticPuzzleGuidance(CreateGroup("03_Zone4_Oscilloscope_And_VoIP_Booths", root.transform).transform);
        BuildServerHallExitGuidance(CreateGroup("04_Zone8_ServerHall_NPC_Exit", root.transform).transform);

        Selection.activeGameObject = root;
        Debug.Log("[DemoNavigationBuilder] Навігаційні смуги, чіткі 3D-вивіски (без просвічування) та підсвічування успішно додано!");
    }

    // =============================================================================================
    // 1. НАЗЕМНИЙ ПРОЛОГ (Y = 0.0m): СПАВН -> ПРОЛОМ -> КПП / СХІДНИЙ КОРПУС -> РАКЕТНИЙ ЛЮК
    // =============================================================================================
    private static void BuildSurfacePrologueGuidance(Transform parent)
    {
        // Жовта лінія: від спавну (-2, -65) до пролому в паркані (-24.5, -53)
        DrawPathPolyline("Path_SpawnToBreach", parent, _matAmberLine, 0.24f, new[]
        {
            new Vector3(0f, SurfaceY, -64.5f),
            new Vector3(-6.5f, SurfaceY, -62.0f),
            new Vector3(-16.0f, SurfaceY, -58.5f),
            new Vector3(-24.5f, SurfaceY, -55.5f)
        });
        CreateFloatingSign("Sign_Breach", parent, new Vector3(-24.5f, 2.35f, -55.2f), Vector3.back,
            "ПРОЛОМ У ПАРКАНІ\n[ВХІД У КОМПЛЕКС]", new Color(1f, 0.78f, 0.22f));

        // Блакитна лінія (Гравець 2): від пролому всередині двору до будки КПП (-10.5, -46.0)
        DrawPathPolyline("Path_BreachToGuardhouse_P2", parent, _matCyanLine, 0.20f, new[]
        {
            new Vector3(-24.5f, SurfaceY, -50.2f),
            new Vector3(-18.0f, SurfaceY, -46.0f),
            new Vector3(-10.5f, SurfaceY, -46.0f),
            new Vector3(-10.5f, SurfaceY + 0.12f, -49.5f)
        });
        // Вивіска КПП дивиться на ПІВНІЧ (Vector3.forward) — у двір до гравця
        CreateFloatingSign("Sign_Guardhouse_P2", parent, new Vector3(-10.5f, 3.15f, -45.95f), Vector3.forward,
            "ГРАВЕЦЬ 2: КПП ОХОРОНИ\nКОД GRiD-87 (РАЦІЯ [T])", new Color(0.35f, 0.90f, 1f));
        CreateAccentSpot("Spot_GuardhousePC", parent, new Vector3(-8.8f, 2.8f, -50.8f), new Color(0.35f, 0.90f, 1f), 4.5f, 8f);

        // Зелена лінія (Гравець 1): від пролому по двору до Східного корпусу (X = 12, Z = -12 -> 21.6, -16.5)
        DrawPathPolyline("Path_BreachToEastLab_P1", parent, _matGreenLine, 0.20f, new[]
        {
            new Vector3(-24.5f, SurfaceY, -50.2f),
            new Vector3(-15.0f, SurfaceY, -34.0f),
            new Vector3(0.0f, SurfaceY, -34.0f),
            new Vector3(9.5f, SurfaceY, -23.0f),
            new Vector3(9.5f, SurfaceY, -12.0f),
            new Vector3(16.0f, SurfaceY + 0.16f, -12.0f),
            new Vector3(16.0f, SurfaceY + 0.16f, -16.5f),
            new Vector3(20.8f, SurfaceY + 0.16f, -16.5f)
        });
        // Вивіска Східного корпусу дивиться на ЗАХІД (Vector3.left) — у двір
        CreateFloatingSign("Sign_EastLab_P1", parent, new Vector3(11.65f, 3.35f, -12.0f), Vector3.left,
            "ГРАВЕЦЬ 1: СХІДНИЙ КОРПУС\nТЕРМІНАЛ GRiD-87", new Color(0.35f, 1f, 0.48f));
        CreateAccentSpot("Spot_MasterGridPC", parent, new Vector3(21.6f, 3.1f, -16.5f), new Color(0.35f, 1f, 0.48f), 5.0f, 9f);

        // Жовта лінія до Ракетного Люка спуску в бункер (Z = -22.5)
        DrawPathPolyline("Path_CourtyardToSiloHatch", parent, _matAmberLine, 0.26f, new[]
        {
            new Vector3(0.0f, SurfaceY, -34.0f),
            new Vector3(0.0f, SurfaceY, -22.8f),
            new Vector3(-4.2f, SurfaceY, -22.8f)
        });
        // Вивіска спуску дивиться на ПІВДЕНЬ (Vector3.back)
        CreateFloatingSign("Sign_SiloHatch", parent, new Vector3(0.0f, 2.75f, -21.8f), Vector3.back,
            "СПУСК У ПІДЗЕМНИЙ КОМПЛЕКС\n[РАКЕТНИЙ ГЕРМОЛЮК]", new Color(1f, 0.78f, 0.22f));

        // Внизу спуску біля парних синхронних важелів (Y = -4.5m, Z = -0.75) — дивиться на ПІВДЕНЬ (назустріч гравцям на спуску)
        CreateFloatingSign("Sign_SyncLevers", parent, new Vector3(0.0f, MainY + 3.05f, -1.1f), Vector3.back,
            "СИНХРОННІ ВАЖЕЛІ [E] ОДНОЧАСНО\nГРАВЕЦЬ 1 (ЗЛІВА) + ГРАВЕЦЬ 2 (СПРАВА)", new Color(1f, 0.78f, 0.22f));
        CreateAccentSpot("Spot_LeftLever", parent, new Vector3(-2.3f, MainY + 2.6f, -1.2f), new Color(1f, 0.72f, 0.2f), 4.0f, 7f);
        CreateAccentSpot("Spot_RightLever", parent, new Vector3(2.3f, MainY + 2.6f, -1.2f), new Color(1f, 0.72f, 0.2f), 4.0f, 7f);
    }

    // =============================================================================================
    // 2. ПІДЗЕМНИЙ МАРШРУТ (Y = -4.5m): САНПРОПУСКНИК -> АТРІУМ (ОБХІД КОТЛОВАНУ) -> ЗОНА 4
    // =============================================================================================
    private static void BuildUndergroundMainRoute(Transform parent)
    {
        DrawPathPolyline("Path_Main_BunkerToZone4", parent, _matAmberLine, 0.26f, new[]
        {
            new Vector3(0.0f, LineY, 0.8f),
            new Vector3(0.0f, LineY, 22.0f),
            new Vector3(0.0f, LineY, 28.5f),
            new Vector3(-11.5f, LineY, 28.5f),
            new Vector3(-11.5f, LineY, 47.0f),
            new Vector3(-25.0f, LineY, 47.0f),
            new Vector3(-25.0f, LineY, 55.0f)
        });

        CreateFloorChevron("Arrow_Z01_1", parent, new Vector3(0.0f, LineY + 0.01f, 8.0f), 0f, _matAmberLine);
        CreateFloorChevron("Arrow_Z01_2", parent, new Vector3(0.0f, LineY + 0.01f, 16.0f), 0f, _matAmberLine);
        CreateFloorChevron("Arrow_Z02_TurnW", parent, new Vector3(-5.5f, LineY + 0.01f, 28.5f), -90f, _matAmberLine);
        CreateFloorChevron("Arrow_Z02_North1", parent, new Vector3(-11.5f, LineY + 0.01f, 35.0f), 0f, _matAmberLine);
        CreateFloorChevron("Arrow_Z02_North2", parent, new Vector3(-11.5f, LineY + 0.01f, 43.0f), 0f, _matAmberLine);
        CreateFloorChevron("Arrow_Z02_IntoZ04", parent, new Vector3(-18.5f, LineY + 0.01f, 47.0f), -90f, _matAmberLine);

        // Портал в Атріум — дивиться на ПІВДЕНЬ (назустріч гравцям із Санпропускника)
        CreateFloatingSign("Sign_Portal_Atrium", parent, new Vector3(0.0f, MainY + 3.25f, 21.55f), Vector3.back,
            "↑ ЦЕНТРАЛЬНИЙ АТРІУМ «СИНХРОНІЗАЦІЯ»", new Color(1f, 0.82f, 0.25f));

        // Поворот ліворуч в Атріумі — дивиться на ПІВДЕНЬ
        CreateFloatingSign("Sign_Atrium_TurnLeft", parent, new Vector3(0.0f, MainY + 2.85f, 30.65f), Vector3.back,
            "← ЛІВОРУЧ ПО ЖОВТІЙ: ЗОНА 4 (ПАЗЛ №1)", new Color(1f, 0.82f, 0.25f));

        // Портал у Зону 4 — дивиться на СХІД (Vector3.right) в Атріум
        CreateFloatingSign("Sign_Portal_Zone4", parent, new Vector3(-15.55f, MainY + 3.25f, 47.0f), Vector3.right,
            "ЗОНА 4: АКУСТИЧНА ЛАБОРАТОРІЯ\n[ПАЗЛ №1 + ТЕСТ VoIP]", new Color(0.35f, 1f, 0.55f));

        // Інші крила Атріуму
        CreateFloatingSign("Sign_Portal_Zone3", parent, new Vector3(-15.55f, MainY + 3.25f, 30.5f), Vector3.right,
            "ЗОНА 3: КАЛІБРУВАННЯ (28 ЕПТ)", new Color(0.75f, 0.90f, 0.82f));

        CreateFloatingSign("Sign_Portal_Zone5", parent, new Vector3(15.55f, MainY + 3.25f, 30.5f), Vector3.left,
            "ЗОНА 5: АДМІНІСТРАЦІЯ (ДЕРЕВО)", new Color(0.95f, 0.75f, 0.45f));

        CreateFloatingSign("Sign_Portal_Zone6", parent, new Vector3(15.55f, MainY + 3.25f, 47.0f), Vector3.left,
            "ЗОНА 6: МЕДБЛОК І КРІО (ПЛИТКА)", new Color(0.55f, 0.90f, 1f));

        CreateFloatingSign("Sign_Portal_Zone7", parent, new Vector3(0.0f, MainY + 3.25f, 51.45f), Vector3.back,
            "↑ ЗОНА 7: КОНТУР БЕЗПЕКИ (CCTV)", new Color(1f, 0.42f, 0.32f));
    }

    // =============================================================================================
    // 3. ЗОНА 4: РОЗГАЛУЖЕННЯ РОЛЕЙ (ЗЕЛЕНА — ГРАВЕЦЬ 1 ОСЦИЛОГРАФ, БЛАКИТНА — ГРАВЕЦЬ 2 КАБІНИ)
    // =============================================================================================
    private static void BuildZone4AcousticPuzzleGuidance(Transform parent)
    {
        // ЗЕЛЕНА ЛІНІЯ (Гравець 1): у Звукорежисерську до Осцилографа «С1-87»
        DrawPathPolyline("Path_Z04_Player1_Oscilloscope", parent, _matGreenLine, 0.22f, new[]
        {
            new Vector3(-25.0f, LineY, 55.0f),
            new Vector3(-25.0f, LineY, 65.5f),
            new Vector3(-27.2f, LineY, 65.5f)
        });
        CreateFloorChevron("Arrow_Z04_P1", parent, new Vector3(-25.0f, LineY + 0.01f, 59.5f), 0f, _matGreenLine);

        CreateFloatingSign("Sign_Z04_SoundRoom_P1", parent, new Vector3(-25.0f, MainY + 3.15f, 59.1f), Vector3.back,
            "ГРАВЕЦЬ 1: ЗВУКОРЕЖИСЕРСЬКА\nОСЦИЛОГРАФ «С1-87» [E]", new Color(0.35f, 1f, 0.48f));
        CreateAccentSpot("Spot_Oscilloscope", parent, new Vector3(-28.4f, MainY + 2.8f, 65.5f), new Color(0.30f, 1f, 0.48f), 5.5f, 9f);

        // БЛАКИТНА ЛІНІЯ (Гравець 2): уздовж 4 акустичних кабін
        DrawPathPolyline("Path_Z04_Player2_BoothsMain", parent, _matCyanLine, 0.22f, new[]
        {
            new Vector3(-25.0f, LineY, 55.0f),
            new Vector3(-45.5f, LineY, 55.0f)
        });
        CreateFloorChevron("Arrow_Z04_P2", parent, new Vector3(-30.0f, LineY + 0.01f, 55.0f), -90f, _matCyanLine);

        (float bx, string label, Color col)[] booths =
        {
            (-45.5f, "КАБІНА 1: БЕТОН\nLowPass: 650 Гц | 30%", new Color(1f, 0.72f, 0.25f)),
            (-41.5f, "КАБІНА 2: ДЕРЕВО\nLowPass: 1650 Гц | 62%", new Color(0.35f, 1f, 0.50f)),
            (-37.5f, "КАБІНА 3: МЕТАЛ\nLowPass: 950 Гц | 42%", new Color(0.38f, 0.82f, 1f)),
            (-33.5f, "КАБІНА 4: ПОРОЛОН\nLowPass: 420 Гц | 14%", new Color(1f, 0.42f, 0.35f))
        };

        for (int i = 0; i < booths.Length; i++)
        {
            float bx = booths[i].bx;
            DrawPathPolyline($"Path_BoothBranch_{i}", parent, _matCyanLine, 0.16f, new[]
            {
                new Vector3(bx - 0.8f, LineY, 55.0f),
                new Vector3(bx - 0.8f, LineY, 50.8f),
                new Vector3(bx, LineY, 50.2f)
            });

            // Вивіски кабін дивляться на ПІВНІЧ (Vector3.forward) — у коридор до гравця
            CreateFloatingSign($"Sign_Booth_{i}", parent, new Vector3(bx, MainY + 2.85f, 52.35f), Vector3.forward, booths[i].label, booths[i].col, 2.75f);
            CreateAccentSpot($"Spot_Booth_{i}", parent, new Vector3(bx, MainY + 2.65f, 49.8f), booths[i].col, 3.8f, 7f);
        }

        // Вивіска Безлунної камери дивиться на СХІД (Vector3.right)
        CreateFloatingSign("Sign_AnechoicChamber", parent, new Vector3(-35.25f, MainY + 3.15f, 65.8f), Vector3.right,
            "БЕЗЛУННА КАМЕРА\n(АКУСТИЧНИЙ ПОРОЛОН)", new Color(0.85f, 0.75f, 1f));
    }

    // =============================================================================================
    // 4. ЧЕРВОНА ЛІНІЯ: ВИХІД ІЗ ЗОНИ 4 ЧЕРЕЗ РОЗБЛОКОВАНИЙ ШЛЮЗ У СЕРВЕРНУ (ЗОНА 8 — NPC)
    // =============================================================================================
    private static void BuildServerHallExitGuidance(Transform parent)
    {
        DrawPathPolyline("Path_Zone4_To_ServerHall_NPC", parent, _matRedLine, 0.24f, new[]
        {
            new Vector3(-32.5f, LineY, 55.0f),
            new Vector3(-33.2f, LineY, 70.5f),
            new Vector3(-27.5f, LineY, 70.5f),
            new Vector3(-27.5f, LineY, 84.0f)
        });
        CreateFloorChevron("Arrow_ToServerGate", parent, new Vector3(-27.5f, LineY + 0.01f, 72.2f), 0f, _matRedLine);

        CreateFloatingSign("Sign_ServerHallGate", parent, new Vector3(-27.5f, MainY + 3.25f, 73.45f), Vector3.back,
            "↑ ШЛЮЗ СЕРВЕРНОЇ (ЗОНА 8 — NPC)\nВІДКРИВАЄТЬСЯ ПІСЛЯ ОСЦИЛОГРАФА", new Color(1f, 0.35f, 0.28f));
        CreateAccentSpot("Spot_ServerGate", parent, new Vector3(-27.5f, MainY + 3.1f, 72.5f), new Color(1f, 0.32f, 0.22f), 4.5f, 10f);
    }

    // =============================================================================================
    // ДОПОМІЖНІ МЕТОДИ МАЛЮВАННЯ ЛІНІЙ, СТРІЛОК, 3D-ВИВІСОК ТА СВІТЛА
    // =============================================================================================
    private static void DrawPathPolyline(string name, Transform parent, Material mat, float width, Vector3[] points)
    {
        GameObject grp = CreateGroup(name, parent);
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            float len = Vector3.Distance(a, b);
            if (len < 0.05f) continue;

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"Seg_{i}";
            seg.transform.SetParent(grp.transform);
            seg.transform.position = (a + b) * 0.5f;
            seg.transform.rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
            seg.transform.localScale = new Vector3(width, 0.025f, len + width * 0.5f);

            Object.DestroyImmediate(seg.GetComponent<Collider>());
            Renderer r = seg.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private static void CreateFloorChevron(string name, Transform parent, Vector3 pos, float yawDeg, Material mat)
    {
        GameObject g = CreateGroup(name, parent);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

        for (int s = -1; s <= 1; s += 2)
        {
            GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = s < 0 ? "Wing_L" : "Wing_R";
            wing.transform.SetParent(g.transform);
            wing.transform.localPosition = new Vector3(s * 0.26f, 0f, -0.18f);
            wing.transform.localRotation = Quaternion.Euler(0f, s * 42f, 0f);
            wing.transform.localScale = new Vector3(0.14f, 0.03f, 0.72f);
            Object.DestroyImmediate(wing.GetComponent<Collider>());
            Renderer r = wing.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>
    /// Створює 3D-вивіску, лицьова сторона якої гарантовано дивиться у напрямку faceDirection (без дзеркалення і без просвічування).
    /// </summary>
    private static void CreateFloatingSign(string name, Transform parent, Vector3 pos, Vector3 faceDirection, string text, Color textColor, float boardWidth = 3.5f)
    {
        GameObject sign = CreateGroup(name, parent);
        sign.transform.position = pos;
        // У Unity TextMesh читається з боку -Z, тому орієнтуємо +Z у протилежний бік від faceDirection
        sign.transform.rotation = Quaternion.LookRotation(-faceDirection.normalized, Vector3.up);

        // Темна металева табличка позаду тексту
        GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backing.name = "SignBacking";
        backing.transform.SetParent(sign.transform);
        backing.transform.localPosition = new Vector3(0f, 0f, 0.04f);
        backing.transform.localScale = new Vector3(boardWidth, 0.76f, 0.06f);
        Object.DestroyImmediate(backing.GetComponent<Collider>());
        backing.GetComponent<Renderer>().sharedMaterial = _matSignFrame;

        // Кольорова верхня смужка-акцент на табличці
        GameObject topTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topTrim.name = "SignAccentBar";
        topTrim.transform.SetParent(sign.transform);
        topTrim.transform.localPosition = new Vector3(0f, 0.36f, 0.03f);
        topTrim.transform.localScale = new Vector3(boardWidth + 0.02f, 0.05f, 0.07f);
        Object.DestroyImmediate(topTrim.GetComponent<Collider>());
        topTrim.GetComponent<Renderer>().sharedMaterial = MakeEmissive("SignTrim_" + name, textColor, 1.5f);

        // 3D-текст із правильним URP-шейдером (ZTest LEqual + Cull Back)
        GameObject txtObj = new GameObject("SignText");
        txtObj.transform.SetParent(sign.transform);
        txtObj.transform.localPosition = new Vector3(0f, -0.02f, -0.015f);
        txtObj.transform.localRotation = Quaternion.identity;

        TextMesh tm = txtObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = 0.052f;
        tm.fontSize = 42;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = textColor;

        MeshRenderer mr = txtObj.GetComponent<MeshRenderer>();
        if (mr != null && _worldTextShader != null && mr.sharedMaterial != null)
        {
            Material fontMat = new Material(_worldTextShader)
            {
                name = "Mat_WorldText_" + name,
                mainTexture = mr.sharedMaterial.mainTexture
            };
            fontMat.SetColor("_Color", Color.white);
            mr.sharedMaterial = fontMat;
        }
    }

    private static void CreateAccentSpot(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        Light l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
    }

    private static GameObject CreateGroup(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent);
        return g;
    }

    private static void InitMaterials()
    {
        _matAmberLine = MakeEmissive("Nav_AmberLine", new Color(1.0f, 0.74f, 0.14f), 2.0f);
        _matGreenLine = MakeEmissive("Nav_GreenLine", new Color(0.24f, 0.96f, 0.44f), 2.0f);
        _matCyanLine = MakeEmissive("Nav_CyanLine", new Color(0.24f, 0.86f, 1.0f), 2.0f);
        _matRedLine = MakeEmissive("Nav_RedLine", new Color(1.0f, 0.28f, 0.20f), 2.0f);

        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        _matSignFrame = new Material(sh) { name = "Nav_SignBacking", color = new Color(0.06f, 0.08f, 0.08f) };
        if (_matSignFrame.HasProperty("_BaseColor")) _matSignFrame.SetColor("_BaseColor", new Color(0.06f, 0.08f, 0.08f));

        _worldTextShader = Shader.Find("TheCopy/WorldTextURP");
    }

    private static Material MakeEmissive(string name, Color col, float boost)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(sh) { name = name, color = col };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col * boost);
        return m;
    }
}
#endif