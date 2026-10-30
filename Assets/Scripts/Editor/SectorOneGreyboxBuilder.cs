#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SectorOneGreyboxBuilder
{
    [MenuItem("The Copy/Побудувати сірий каркас Сектора 1 (Greybox)")]
    public static void BuildSectorOneGreybox()
    {
        GameObject oldRoot = GameObject.Find("Sector1_Greybox_Geometry");
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }

        GameObject oldFloor = GameObject.Find("Floor");
        if (oldFloor != null)
        {
            oldFloor.SetActive(false);
        }

        GameObject root = new GameObject("Sector1_Greybox_Geometry");
        Undo.RegisterCreatedObjectUndo(root, "Build Detailed Sector 1 Greybox");

        // Палітра матеріалів у стилі референсів Outlast Trials / 80s Lab
        Material floorMat = CreateMat("Mat_TileFloor", new Color(0.20f, 0.22f, 0.21f), 0.4f);
        Material wallUpperMat = CreateMat("Mat_WallUpper", new Color(0.38f, 0.41f, 0.39f), 0.15f);
        Material wallLowerMat = CreateMat("Mat_WallLowerPanel", new Color(0.22f, 0.26f, 0.25f), 0.25f);
        Material ceilingMat = CreateMat("Mat_CeilingGrid", new Color(0.30f, 0.32f, 0.31f), 0.1f);
        Material darkMetalMat = CreateMat("Mat_DarkMetal", new Color(0.13f, 0.15f, 0.16f), 0.5f);
        Material consoleBeigeMat = CreateMat("Mat_RetroBeige", new Color(0.55f, 0.54f, 0.48f), 0.3f);
        Material chairWhiteMat = CreateMat("Mat_ChairWhite", new Color(0.78f, 0.80f, 0.78f), 0.4f);
        Material glassMat = CreateGlassMat("Mat_ObservationGlass", new Color(0.55f, 0.75f, 0.72f, 0.22f));

        // Матеріали екранів, що світяться (Emission)
        Material crtGreenGlow = CreateEmissiveMat("Mat_CRT_Green", new Color(0.45f, 0.65f, 0.50f), new Color(0.35f, 0.65f, 0.45f) * 1.4f);
        Material crtStaticGlow = CreateEmissiveMat("Mat_CRT_White", new Color(0.75f, 0.82f, 0.78f), new Color(0.6f, 0.7f, 0.65f) * 1.8f);
        Material lampRedGlow = CreateEmissiveMat("Mat_AlarmRed", Color.red, new Color(1f, 0.15f, 0.1f) * 3f);
        Material lampCoolGlow = CreateEmissiveMat("Mat_Fluorescent", Color.white, new Color(0.75f, 0.92f, 1f) * 2.5f);

        float wallH = 4.0f;

        // =========================================================================
        // 1. ПІДЛОГА ТА ПІДВІСНА ПЛИТКОВА СТЕЛЯ (АРМСТРОНГ)
        // =========================================================================
        CreateBox("Floor_Main", root.transform, new Vector3(0f, -0.25f, 11f), new Vector3(32f, 0.5f, 42f), floorMat);
        CreateCeilingWithGrid(root.transform, new Vector3(0f, wallH + 0.15f, 11f), 30f, 40f, ceilingMat, darkMetalMat);

        // =========================================================================
        // 2. ЗОВНІШНІ СТІНИ ТА КОРИДОР ВХОДУ (З ЧЕРВОНИМ АВАРІЙНИМ СВІТЛОМ)
        // =========================================================================
        BuildTwoToneWall("Wall_Entry_Back", root.transform, new Vector3(0f, 0f, -8f), new Vector3(10f, wallH, 0.5f), wallUpperMat, wallLowerMat);
        BuildTwoToneWall("Wall_Entry_Left", root.transform, new Vector3(-5f, 0f, -4f), new Vector3(0.5f, wallH, 8f), wallUpperMat, wallLowerMat);
        BuildTwoToneWall("Wall_Entry_Right", root.transform, new Vector3(5f, 0f, -4f), new Vector3(0.5f, wallH, 8f), wallUpperMat, wallLowerMat);

        // Перегородка Першого Шлюзу (Z = 0) з дверним прорізом
        BuildTwoToneWall("Wall_Gate1_Left", root.transform, new Vector3(-8.5f, 0f, 0f), new Vector3(13f, wallH, 0.5f), wallUpperMat, wallLowerMat);
        BuildTwoToneWall("Wall_Gate1_Right", root.transform, new Vector3(8.5f, 0f, 0f), new Vector3(13f, wallH, 0.5f), wallUpperMat, wallLowerMat);
        CreateBox("Gate1_FrameTop", root.transform, new Vector3(0f, 3.5f, 0f), new Vector3(4.2f, 1.0f, 0.65f), darkMetalMat);

        // Червона аварійна лампа над першим шлюзом (як на Скріні 2 у коридорі)
        CreateWallLamp("RedAlarmLamp_Entry", root.transform, new Vector3(0f, 3.3f, -0.4f), lampRedGlow, new Color(1f, 0.2f, 0.15f), 4.5f, 9f);

        // Зовнішній периметр крил (X = -15 та X = +15)
        BuildTwoToneWall("Wall_Outer_Left", root.transform, new Vector3(-15f, 0f, 15f), new Vector3(0.5f, wallH, 30f), wallUpperMat, wallLowerMat);
        BuildTwoToneWall("Wall_Outer_Right", root.transform, new Vector3(15f, 0f, 15f), new Vector3(0.5f, wallH, 30f), wallUpperMat, wallLowerMat);
        BuildTwoToneWall("Wall_Archive_Back", root.transform, new Vector3(0f, 0f, 30f), new Vector3(30f, wallH, 0.5f), wallUpperMat, wallLowerMat);

        // =========================================================================
        // 3. ПРАВА КІМНАТА: ДИСПЕТЧЕРСЬКА З ВІКНОМ ТА Г-ПОДІБНИМ СТОЛОМ (СКРІНИ 2 і 3)
        // =========================================================================
        BuildObservationWall("Wall_RightControl_Glass", root.transform, new Vector3(4.5f, 0f, 7f), 14f, wallH, wallUpperMat, wallLowerMat, darkMetalMat, glassMat);
        BuildControlRoomInterior(root.transform, new Vector3(9.8f, 0f, 7f), darkMetalMat, consoleBeigeMat, crtGreenGlow, crtStaticGlow, lampCoolGlow);

        // =========================================================================
        // 4. ЛІВА КІМНАТА: КАМЕРА КАЛІБРУВАННЯ «СЕАНС 214» З ПІВКРУГОМ ТВ (СКРІН 1)
        // =========================================================================
        // Стіна відділення лівої кімнати з дверима
        BuildTwoToneWall("Wall_LeftLab_A", root.transform, new Vector3(-4.5f, 0f, 3f), new Vector3(0.4f, wallH, 6f), wallUpperMat, wallLowerMat);
        BuildTwoToneWall("Wall_LeftLab_B", root.transform, new Vector3(-4.5f, 0f, 11.5f), new Vector3(0.4f, wallH, 5f), wallUpperMat, wallLowerMat);
        BuildCurvedTvChairRoom(root.transform, new Vector3(-10.2f, 0f, 7f), darkMetalMat, chairWhiteMat, crtGreenGlow);

        // =========================================================================
        // 5. ПЕРЕХІД ТА ЗАЛА ПЕРВИННОГО АРХІВУ (Z від 14 до 30)
        // =========================================================================
        BuildTwoToneWall("Wall_Gate2_Left", root.transform, new Vector3(-8.5f, 0f, 14f), new Vector3(13f, wallH, 0.5f), wallUpperMat, wallLowerMat);
        BuildTwoToneWall("Wall_Gate2_Right", root.transform, new Vector3(8.5f, 0f, 14f), new Vector3(13f, wallH, 0.5f), wallUpperMat, wallLowerMat);
        CreateBox("Gate2_FrameTop", root.transform, new Vector3(0f, 3.5f, 14f), new Vector3(4.2f, 1.0f, 0.65f), darkMetalMat);

        // Масивні серверні шафи та колони в Архіві для стелсу від NPC
        for (int i = -1; i <= 1; i += 2)
        {
            BuildRetroMainframe($"Archive_Mainframe_Row1_{i}", root.transform, new Vector3(i * 5.5f, 0f, 19f), Quaternion.identity, darkMetalMat, consoleBeigeMat, crtGreenGlow);
            BuildRetroMainframe($"Archive_Mainframe_Row2_{i}", root.transform, new Vector3(i * 5.5f, 0f, 24.5f), Quaternion.identity, darkMetalMat, consoleBeigeMat, crtGreenGlow);
        }
        CreateBox("Archive_CenterPillar", root.transform, new Vector3(0f, 2f, 22f), new Vector3(2.8f, 4f, 1.4f), darkMetalMat);

        // Холодне люмінесцентне світло в коридорі та архіві
        CreateWallLamp("Hall_Fluorescent_1", root.transform, new Vector3(0f, 3.8f, 7f), lampCoolGlow, new Color(0.75f, 0.92f, 0.88f), 3.5f, 12f);
        CreateWallLamp("Archive_RedLight", root.transform, new Vector3(0f, 3.7f, 22f), lampRedGlow, new Color(1f, 0.35f, 0.2f), 4f, 14f);

        // =========================================================================
        // 6. АВТОМАТИЧНЕ РОЗМІЩЕННЯ ВСІХ ТВОЇХ МЕРЕЖЕВИХ МЕХАНІК НА НОВІ МІСЦЯ
        // =========================================================================
        PlaceIfFound("SpawnPoint_1", new Vector3(-1.6f, 1.05f, -5.5f), Quaternion.identity);
        PlaceIfFound("SpawnPoint_2", new Vector3(1.6f, 1.05f, -5.5f), Quaternion.identity);

        PlaceIfFound("Airlock_Door", new Vector3(0f, 1.6f, 0f), Quaternion.identity);
        PlaceIfFound("Interactive_Terminal", new Vector3(2.6f, 1.1f, -0.8f), Quaternion.identity);

        // Батарея лежить біля крісла калібрування в Лівій Кімнаті (Скрін 1)
        PlaceIfFound("Power_Battery", new Vector3(-8.2f, 0.5f, 7.0f), Quaternion.identity);

        // Секретна записка з паролем 1987 лежить прямо під лампою на Г-подібному столі в Диспетчерській (Скрін 2)
        PlaceIfFound("Secret_Note_1987", new Vector3(8.2f, 0.84f, 5.5f), Quaternion.Euler(0f, -90f, 0f));

        // Кодовий термінал «Електроніка-88» висить біля входу в Архів
        PlaceIfFound("Keypad_Terminal", new Vector3(2.5f, 1.4f, 13.6f), Quaternion.identity);

        // Шаховий стіл у кутку Диспетчерської
        PlaceIfFound("Chess_Table", new Vector3(11.5f, 0.4f, 2.2f), Quaternion.identity);
        PlaceIfFound("Seat_Player1", new Vector3(11.5f, 0.4f, 1.1f), Quaternion.identity);
        PlaceIfFound("Seat_Player2", new Vector3(11.5f, 0.4f, 3.3f), Quaternion.Euler(0f, 180f, 0f));

        // Тригер-зона та NPC в залі Архіву
        PlaceIfFound("Sector_TriggerZone", new Vector3(0f, 1.5f, 17.5f), Quaternion.identity);
        PlaceIfFound("Dummy_NPC", new Vector3(0f, 1.0f, 27.5f), Quaternion.Euler(0f, 180f, 0f));

        Selection.activeGameObject = root;
        Debug.Log("[Detailed Greybox] Професійний каркас Сектора 1 за референсами Outlast Trials побудовано!");
    }

    // --- БУДІВНИЦТВО КІМНАТИ 1: ПІВКРУГЛА СТІНА ТЕЛЕВІЗОРІВ + БІЛЕ КРІСЛО (СКРІН 1) ---
    private static void BuildCurvedTvChairRoom(Transform parent, Vector3 center, Material metalMat, Material chairMat, Material screenGlow)
    {
        GameObject room = new GameObject("Room_Calibration_TV_Arc");
        room.transform.SetParent(parent);
        room.transform.position = center;

        // Півкругла арка з 7 колон по 4 ряди = 28 ЕПТ-моніторів!
        int cols = 7;
        int rows = 4;
        float radius = 3.8f;
        float startAngle = -75f;
        float endAngle = 75f;

        for (int c = 0; c < cols; c++)
        {
            float t = c / (float)(cols - 1);
            float angleDeg = Mathf.Lerp(startAngle, endAngle, t);
            float rad = angleDeg * Mathf.Deg2Rad;

            // Дуга вигнута в бік -X (до зовнішньої стіни)
            Vector3 colPos = center + new Vector3(-Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            Quaternion lookRot = Quaternion.LookRotation((center - colPos).normalized, Vector3.up);

            for (int r = 0; r < rows; r++)
            {
                float yPos = 0.55f + r * 0.82f;
                Vector3 tvPos = new Vector3(colPos.x, yPos, colPos.z);

                // Корпус телевізора
                GameObject tvBody = CreateBox($"TV_Body_{c}_{r}", room.transform, tvPos, new Vector3(0.85f, 0.76f, 0.6f), metalMat);
                tvBody.transform.rotation = lookRot;

                // Світний екран
                GameObject tvScreen = CreateBox($"TV_Screen_{c}_{r}", tvBody.transform, Vector3.zero, Vector3.one, screenGlow);
                tvScreen.transform.localPosition = new Vector3(0f, 0.04f, 0.31f);
                tvScreen.transform.localScale = new Vector3(0.82f, 0.72f, 0.04f);
            }
        }

        // Центральне біле крісло піддослідного (як на Скріні 1)
        GameObject chairRoot = new GameObject("Subject_Calibration_Chair");
        chairRoot.transform.SetParent(room.transform);
        chairRoot.transform.position = center + new Vector3(0.2f, 0f, 0f);
        chairRoot.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        CreateCylinder("Chair_BaseDisk", chairRoot.transform, chairRoot.transform.position + new Vector3(0f, 0.06f, 0f), new Vector3(0.9f, 0.06f, 0.9f), chairMat);
        CreateCylinder("Chair_Pedestal", chairRoot.transform, chairRoot.transform.position + new Vector3(0f, 0.35f, 0f), new Vector3(0.35f, 0.35f, 0.35f), metalMat);
        CreateBox("Chair_Seat", chairRoot.transform, chairRoot.transform.position + new Vector3(0f, 0.65f, 0f), new Vector3(0.75f, 0.12f, 0.75f), chairMat);
        CreateBox("Chair_Backrest", chairRoot.transform, chairRoot.transform.position + new Vector3(0f, 1.25f, -0.32f), new Vector3(0.72f, 1.15f, 0.16f), chairMat);
        CreateBox("Chair_ArmLeft", chairRoot.transform, chairRoot.transform.position + new Vector3(-0.42f, 0.9f, 0.05f), new Vector3(0.12f, 0.1f, 0.65f), chairMat);
        CreateBox("Chair_ArmRight", chairRoot.transform, chairRoot.transform.position + new Vector3(0.42f, 0.9f, 0.05f), new Vector3(0.12f, 0.1f, 0.65f), chairMat);

        // Зеленкуватий прожектор зверху прямо над кріслом
        CreateWallLamp("Calibration_SpotLight", room.transform, center + new Vector3(0f, 3.6f, 0f), screenGlow, new Color(0.55f, 0.85f, 0.6f), 5f, 9f);
    }

    // --- БУДІВНИЦТВО КІМНАТИ 2: ДИСПЕТЧЕРСЬКА З Г-СТОЛОМ, МАГНІТОФОНОМ І СТІНОЮ ЕКРАНІВ (СКРІНИ 2 і 3) ---
    private static void BuildControlRoomInterior(Transform parent, Vector3 center, Material metalMat, Material beigeMat, Material greenGlow, Material whiteGlow, Material lampGlow)
    {
        GameObject room = new GameObject("Room_Control_And_Monitoring");
        room.transform.SetParent(parent);
        room.transform.position = center;

        // 1. Великий Г-подібний стіл (L-Shaped Desk) по центру кімнати (Скрін 2)
        CreateBox("LDesk_MainLong", room.transform, new Vector3(9.5f, 0.78f, 7.5f), new Vector3(1.1f, 0.08f, 4.6f), beigeMat);
        CreateBox("LDesk_SideWing", room.transform, new Vector3(8.2f, 0.78f, 5.5f), new Vector3(1.8f, 0.08f, 1.1f), beigeMat);
        CreateBox("LDesk_Leg1", room.transform, new Vector3(9.5f, 0.38f, 9.5f), new Vector3(0.9f, 0.76f, 0.1f), metalMat);
        CreateBox("LDesk_Leg2", room.transform, new Vector3(9.5f, 0.38f, 5.5f), new Vector3(0.9f, 0.76f, 0.1f), metalMat);
        CreateBox("LDesk_Leg3", room.transform, new Vector3(7.4f, 0.38f, 5.5f), new Vector3(0.1f, 0.76f, 0.9f), metalMat);

        // Настільні пульти та друкарський термінал на столі (Скрін 2 і 3)
        GameObject deskConsole1 = CreateBox("Desk_MiniConsole_1", room.transform, new Vector3(9.4f, 0.92f, 7.2f), new Vector3(0.55f, 0.22f, 0.75f), metalMat);
        deskConsole1.transform.rotation = Quaternion.Euler(0f, 0f, 15f);
        GameObject deskConsole2 = CreateBox("Desk_MiniConsole_2", room.transform, new Vector3(9.4f, 0.92f, 8.3f), new Vector3(0.55f, 0.22f, 0.75f), beigeMat);
        deskConsole2.transform.rotation = Quaternion.Euler(0f, 0f, 15f);

        // Бобінний магнітофон (Reel-to-Reel Tape Recorder) ПІД СТОЛОМ (як на Скріні 2!)
        CreateBox("ReelRecorder_Cabinet", room.transform, new Vector3(9.6f, 0.38f, 6.8f), new Vector3(0.5f, 0.72f, 0.65f), metalMat);
        CreateCylinder("TapeReel_Left", room.transform, new Vector3(9.32f, 0.48f, 6.65f), new Vector3(0.22f, 0.03f, 0.22f), beigeMat).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        CreateCylinder("TapeReel_Right", room.transform, new Vector3(9.32f, 0.48f, 6.95f), new Vector3(0.22f, 0.03f, 0.22f), beigeMat).transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        // Настільна лампа над крилом столу (світить прямо на Secret_Note_1987!)
        CreateBox("DeskLamp_Stand", room.transform, new Vector3(8.2f, 1.05f, 5.1f), new Vector3(0.08f, 0.45f, 0.08f), metalMat);
        CreateWallLamp("DeskLamp_Bulb", room.transform, new Vector3(8.2f, 1.28f, 5.3f), lampGlow, new Color(1f, 0.95f, 0.75f), 3.0f, 5f);

        // 2. Скошена консоль керування вздовж задньої стіни (Скрін 2)
        BuildRetroMainframe("Mainframe_Wall_1", room.transform, new Vector3(14.1f, 0f, 6.5f), Quaternion.Euler(0f, -90f, 0f), metalMat, beigeMat, greenGlow);
        BuildRetroMainframe("Mainframe_Wall_2", room.transform, new Vector3(14.1f, 0f, 9.5f), Quaternion.Euler(0f, -90f, 0f), metalMat, beigeMat, greenGlow);

        // 3. Стіна моніторів спостереження (6x3 матриця на північній стіні Диспетчерської — Скрін 3)
        for (int c = 0; c < 6; c++)
        {
            for (int r = 0; r < 3; r++)
            {
                Vector3 tvPos = new Vector3(6.8f + c * 1.15f, 1.5f + r * 0.75f, 13.55f);
                CreateBox($"CtrlTV_{c}_{r}", room.transform, tvPos, new Vector3(1.05f, 0.68f, 0.4f), metalMat);
                Material scrMat = ((c + r) % 3 == 0) ? whiteGlow : greenGlow;
                CreateBox($"CtrlTV_Scr_{c}_{r}", room.transform, tvPos + new Vector3(0f, 0f, -0.21f), new Vector3(0.92f, 0.56f, 0.02f), scrMat);
            }
        }
    }

    // --- СТІНА З ПРОЗОРИМ ОГЛЯДОВИМ ВІКНОМ ТА ДВЕРИМА (СКРІНИ 2 і 3) ---
    private static void BuildObservationWall(string name, Transform parent, Vector3 center, float lengthZ, float height, Material upperMat, Material lowerMat, Material frameMat, Material glassMat)
    {
        GameObject wallGrp = new GameObject(name);
        wallGrp.transform.SetParent(parent);

        // Ліва глуха частина + дверний проріз
        BuildTwoToneWall(name + "_DoorSide", wallGrp.transform, new Vector3(center.x, 0f, 2.0f), new Vector3(0.4f, height, 4.0f), upperMat, lowerMat);
        // Верхня перемичка над дверима (проріз Z від 4.0 до 5.6)
        CreateBox(name + "_DoorHeader", wallGrp.transform, new Vector3(center.x, 3.4f, 4.8f), new Vector3(0.42f, 1.2f, 1.6f), upperMat);

        // Секція під великим оглядовим вікном (Z від 5.6 до 14.0)
        CreateBox(name + "_WindowLowWall", wallGrp.transform, new Vector3(center.x, 0.6f, 9.8f), new Vector3(0.42f, 1.2f, 8.4f), lowerMat);
        CreateBox(name + "_WindowTopWall", wallGrp.transform, new Vector3(center.x, 3.5f, 9.8f), new Vector3(0.42f, 1.0f, 8.4f), upperMat);

        // Металева рама вікна та прозоре скло
        CreateBox(name + "_WindowSill", wallGrp.transform, new Vector3(center.x, 1.22f, 9.8f), new Vector3(0.52f, 0.08f, 8.4f), frameMat);
        GameObject glass = CreateBox(name + "_GlassPane", wallGrp.transform, new Vector3(center.x, 2.1f, 9.8f), new Vector3(0.08f, 1.75f, 8.2f), glassMat);
        // Переводимо скло в шар Ignore Raycast (2), щоб крізь вікно проходив погляд, але воно виглядало як скло
        glass.layer = 2;
    }

    // --- БУДІВНИЦТВО СКОШЕНОГО РЕТРО-МЕЙНФРЕЙМА 80-Х (СКРІН 2) ---
    private static void BuildRetroMainframe(string name, Transform parent, Vector3 pos, Quaternion rot, Material metalMat, Material beigeMat, Material screenGlow)
    {
        GameObject mf = new GameObject(name);
        mf.transform.SetParent(parent);
        mf.transform.position = pos;
        mf.transform.rotation = rot;

        // Нижня тумба
        CreateBox("BaseCabinet", mf.transform, pos, new Vector3(2.4f, 0.9f, 0.9f), metalMat).transform.localPosition = new Vector3(0f, 0.45f, 0f);
        // Верхня висока шафа з перфокартами/індикаторами
        CreateBox("UpperTower", mf.transform, pos, new Vector3(2.4f, 1.5f, 0.65f), beigeMat).transform.localPosition = new Vector3(0f, 1.65f, -0.12f);
        // Скошена панель керування
        GameObject sloped = CreateBox("SlopedPanel", mf.transform, pos, new Vector3(2.35f, 0.6f, 0.5f), beigeMat);
        sloped.transform.localPosition = new Vector3(0f, 1.05f, 0.18f);
        sloped.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);

        // Вбудовані осцилографи / монітори
        CreateBox("MiniCRT_1", mf.transform, pos, new Vector3(0.45f, 0.35f, 0.05f), screenGlow).transform.localPosition = new Vector3(-0.5f, 1.7f, 0.22f);
        CreateBox("MiniCRT_2", mf.transform, pos, new Vector3(0.45f, 0.35f, 0.05f), screenGlow).transform.localPosition = new Vector3(0.5f, 1.7f, 0.22f);
    }

    // --- ДВОКОЛІРНА СТІНА З НИЖНЬОЮ ПАНЕЛЛЮ (WAINSCOTING — ЯК НА СКРІНАХ) ---
    private static void BuildTwoToneWall(string name, Transform parent, Vector3 bottomCenter, Vector3 size, Material upperMat, Material lowerMat)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);

        float lowerH = 1.35f;
        float upperH = size.y - lowerH;

        CreateBox(name + "_Lower", wall.transform, bottomCenter + new Vector3(0f, lowerH / 2f, 0f), new Vector3(size.x, lowerH, size.z), lowerMat);
        CreateBox(name + "_Upper", wall.transform, bottomCenter + new Vector3(0f, lowerH + upperH / 2f, 0f), new Vector3(size.x, upperH, size.z), upperMat);
    }

    // --- ПІДВІСНА СТЕЛЯ З БАЛКАМИ ---
    private static void CreateCeilingWithGrid(Transform parent, Vector3 center, float width, float length, Material ceilMat, Material beamMat)
    {
        GameObject ceil = CreateBox("Ceiling_MainPlate", parent, center, new Vector3(width, 0.3f, length), ceilMat);
        for (float z = -6f; z <= 28f; z += 4f)
        {
            CreateBox($"Ceiling_Beam_Z_{z}", ceil.transform, new Vector3(0f, center.y - 0.18f, z), new Vector3(width, 0.08f, 0.12f), beamMat);
        }
    }

    // --- СТВОРЕННЯ ЛАМПИ З РЕАЛЬНИМ POINT LIGHT ---
    private static void CreateWallLamp(string name, Transform parent, Vector3 pos, Material emissiveMat, Color lightColor, float intensity, float range)
    {
        GameObject lamp = CreateBox(name, parent, pos, new Vector3(0.9f, 0.12f, 0.25f), emissiveMat);
        GameObject lightObj = new GameObject(name + "_PointLight");
        lightObj.transform.SetParent(lamp.transform);
        lightObj.transform.localPosition = new Vector3(0f, -0.25f, 0f);

        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.Soft;
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.position = pos;
        box.transform.localScale = scale;
        if (mat != null) box.GetComponent<Renderer>().sharedMaterial = mat;
        return box;
    }

    private static GameObject CreateCylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = name;
        cyl.transform.SetParent(parent);
        cyl.transform.position = pos;
        cyl.transform.localScale = scale;
        if (mat != null) cyl.GetComponent<Renderer>().sharedMaterial = mat;
        return cyl;
    }

    private static void PlaceIfFound(string objName, Vector3 pos, Quaternion rot)
    {
        GameObject obj = GameObject.Find(objName);
        if (obj != null)
        {
            Undo.RecordObject(obj.transform, "Move Object to Detailed Greybox");
            obj.transform.position = pos;
            obj.transform.rotation = rot;
        }
    }

    private static Material CreateMat(string name, Color color, float smoothness)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(sh) { name = name, color = color };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    private static Material CreateEmissiveMat(string name, Color baseCol, Color emissionCol)
    {
        Material m = CreateMat(name, baseCol, 0.8f);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emissionCol);
        return m;
    }

    private static Material CreateGlassMat(string name, Color glassCol)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(sh) { name = name, color = glassCol };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", glassCol);
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // Transparent in URP
        m.renderQueue = 3000;
        return m;
    }
}
#endif