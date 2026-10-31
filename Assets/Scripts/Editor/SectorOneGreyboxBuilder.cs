// Повний генератор занедбаного комплексу «Синхронізація»: Ліс і трава + Пролом у паркані + Відкриті наземні корпуси з інтер'єрами + Спуск у бункер (-4.5м) + Підземна лабораторія[cite: 52, 53, 54]
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SectorOneGreyboxBuilder
{
    private const float UndergroundY = -4.5f;

    private static Material _matGrass;
    private static Material _matGrassDark;
    private static Material _matMoss;
    private static Material _matDirtPath;
    private static Material _matWetAsphalt;
    private static Material _matConcreteLight;
    private static Material _matConcreteDark;
    private static Material _matConcreteDamaged;
    private static Material _matBunkerWall;
    private static Material _matBrickWall;
    private static Material _matLabTileFloor;
    private static Material _matCheckerTile;
    private static Material _matWallUpper;
    private static Material _matWallLower;
    private static Material _matCeilingGrid;
    private static Material _matRustedSteel;
    private static Material _matDarkIron;
    private static Material _matCopperPipe;
    private static Material _matHazardStripes;
    private static Material _matRetroBeige;
    private static Material _matOldWood;
    private static Material _matCrackedWood;
    private static Material _matChairWhite;
    private static Material _matSiloWhite;
    private static Material _matCargoRed;
    private static Material _matCargoGreen;
    private static Material _matCargoBlue;
    private static Material _matPineNeedles;
    private static Material _matBirchLeaves;
    private static Material _matTreeTrunk;
    private static Material _matBirchTrunk;
    private static Material _matGlassClear;
    private static Material _matGlassBroken;
    private static Material _matWaterPuddle;
    private static Material _matCrtGreen;
    private static Material _matCrtAmber;
    private static Material _matCrtStatic;
    private static Material _matLampRed;
    private static Material _matLampAmber;
    private static Material _matLampCool;
    private static Material _matLampWarm;

    [MenuItem("The Copy/1. Побудувати ВЕСЬ КОМПЛЕКС (Заброшка + Ліс + Інтер'єри + Спуск -4.5м)")]
    public static void BuildFullComplexGreybox()
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

        ConfigureAtmosphereAndLighting();
        InitializeAllProceduralMaterials();

        GameObject root = new GameObject("Sector1_Greybox_Geometry");
        Undo.RegisterCreatedObjectUndo(root, "Build Ultra Detailed Abandoned Complex");

        // =========================================================================================
        // 0. ЗОВНІШНЯ ЛІСОВА ЗОНА З ТРАВОЮ, ПАГОРБАМИ, ЛЕП ТА ПОКИНУТОЮ ТЕХНІКОЮ (Z: -95 .. -53)
        // =========================================================================================
        GameObject natureRoot = new GameObject("0_Outer_Wilderness_And_Grass_Y0");
        natureRoot.transform.SetParent(root.transform);

        BuildWildernessTerrainAndGrass(natureRoot.transform);
        BuildForestApproachRoadAndWreckage(natureRoot.transform);
        BuildSurroundingDenseForest(natureRoot.transform);

        // =========================================================================================
        // 1. ПЕРИМЕТР ЗАБРОШКИ, ПРОЛОМ У СТІНІ З ДОШКАМИ ТА КПП ОХОРОНИ (Z = -53)
        // =========================================================================================
        GameObject perimeterRoot = new GameObject("1_Abandoned_Perimeter_And_Breach_Puzzle");
        perimeterRoot.transform.SetParent(root.transform);

        BuildDamagedPerimeterFenceWithBreach(perimeterRoot.transform);
        BuildDetailedMainCheckpointAndGuardhouse(perimeterRoot.transform, new Vector3(0f, 0f, -53f));

        // =========================================================================================
        // 2. ВЕРХНІЙ ПОВЕРХ — ЗАРОСЛИЙ ДВІР ТА 5 ВІДКРИТИХ КОРПУСІВ З ІНТЕР'ЄРАМИ (Y = 0.0m)
        // =========================================================================================
        GameObject surfaceRoot = new GameObject("2_Surface_Abandoned_Complex_Interiors_Y0");
        surfaceRoot.transform.SetParent(root.transform);

        BuildCourtyardRoadsCracksAndOvergrowth(surfaceRoot.transform);
        BuildWestPowerGeneratorHallWithInterior(surfaceRoot.transform, new Vector3(-19.5f, 0f, -34f));
        BuildWestBarracksAndGarageWithInterior(surfaceRoot.transform, new Vector3(-21f, 0f, -12f));
        BuildEastDecontaminationLabWithInterior(surfaceRoot.transform, new Vector3(19.5f, 0f, -12f));
        BuildEastBoilerHouseAndChemicalSilos(surfaceRoot.transform, new Vector3(21f, 0f, -35f));
        BuildMainOvergroundInstituteLobbyWithInterior(surfaceRoot.transform, new Vector3(0f, 0f, 8.5f));
        BuildCourtyardPipeBridgesAndDebris(surfaceRoot.transform);

        // =========================================================================================
        // 3. СПУСК У БУНКЕР З ТРІЩИНАМИ, ТРУБАМИ, КАЛЮЖАМИ ТА ГЕРМОПОРТАЛОМ (Y: 0 -> -4.5m)
        // =========================================================================================
        GameObject rampRoot = new GameObject("3_Sunken_Bunker_Descent_Ramp");
        rampRoot.transform.SetParent(root.transform);

        BuildUltraDetailedSunkenBunkerRamp(rampRoot.transform);

        // =========================================================================================
        // 4. ПІДЗЕМНИЙ КОМПЛЕКС «СИНХРОНІЗАЦІЯ» НА ГЛИБИНІ Y = -4.5m
        // =========================================================================================
        GameObject undergroundRoot = new GameObject("4_Underground_Sector1_Lab_Y_Minus4.5");
        undergroundRoot.transform.SetParent(root.transform);

        BuildUndergroundLaboratoryComplex(undergroundRoot.transform);

        // =========================================================================================
        // 5. РОЗМІЩЕННЯ ІНТЕРАКТИВНИХ ОБ'ЄКТІВ ТА СПАВНУ
        // =========================================================================================
        PositionAllGameplayObjects();

        Selection.activeGameObject = root;
    }

    [MenuItem("The Copy/2. Спавн: За парканом у лісі (Початок гри — пошук пролому)")]
    public static void SpawnOutsidePerimeter()
    {
        PlaceIfFound("SpawnPoint_1", new Vector3(-2.5f, 1.05f, -66f), Quaternion.identity);
        PlaceIfFound("SpawnPoint_2", new Vector3(2.5f, 1.05f, -66f), Quaternion.identity);
    }

    [MenuItem("The Copy/3. Спавн: Перед спуском у бункер (Двір комплексу)")]
    public static void SpawnAtCourtyardRamp()
    {
        PlaceIfFound("SpawnPoint_1", new Vector3(-2.0f, 1.05f, -26f), Quaternion.identity);
        PlaceIfFound("SpawnPoint_2", new Vector3(2.0f, 1.05f, -26f), Quaternion.identity);
    }

    [MenuItem("The Copy/4. Спавн: Всередині підземної лабораторії (Y = -4.5м)")]
    public static void SpawnInsideUndergroundLab()
    {
        PlaceIfFound("SpawnPoint_1", new Vector3(-1.8f, UndergroundY + 1.05f, 2.5f), Quaternion.identity);
        PlaceIfFound("SpawnPoint_2", new Vector3(1.8f, UndergroundY + 1.05f, 2.5f), Quaternion.identity);
    }

    private static void ConfigureAtmosphereAndLighting()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.09f, 0.13f, 0.11f);
        RenderSettings.fogDensity = 0.014f;
        RenderSettings.ambientLight = new Color(0.23f, 0.27f, 0.25f);
    }

    private static void PositionAllGameplayObjects()
    {
        float uy = UndergroundY;

        PlaceIfFound("SpawnPoint_1", new Vector3(-2.2f, 1.05f, -66f), Quaternion.identity);
        PlaceIfFound("SpawnPoint_2", new Vector3(2.2f, 1.05f, -66f), Quaternion.identity);

        PlaceIfFound("Power_Battery", new Vector3(-16.2f, 0.95f, -32.5f), Quaternion.identity);
        PlaceIfFound("Airlock_Door", new Vector3(0f, uy + 1.6f, 0f), Quaternion.identity);
        PlaceIfFound("Interactive_Terminal", new Vector3(2.45f, uy + 1.1f, -1.15f), Quaternion.identity);

        PlaceIfFound("Secret_Note_1987", new Vector3(8.2f, uy + 0.84f, 5.5f), Quaternion.Euler(0f, -90f, 0f));
        PlaceIfFound("Keypad_Terminal", new Vector3(2.5f, uy + 1.4f, 13.6f), Quaternion.identity);
        PlaceIfFound("Chess_Table", new Vector3(11.5f, uy + 0.4f, 2.2f), Quaternion.identity);
        PlaceIfFound("Seat_Player1", new Vector3(11.5f, uy + 0.4f, 1.1f), Quaternion.identity);
        PlaceIfFound("Seat_Player2", new Vector3(11.5f, uy + 0.4f, 3.3f), Quaternion.Euler(0f, 180f, 0f));
        PlaceIfFound("Sector_TriggerZone", new Vector3(0f, uy + 1.5f, 17.5f), Quaternion.identity);
        PlaceIfFound("Dummy_NPC", new Vector3(0f, uy + 1.0f, 27.5f), Quaternion.Euler(0f, 180f, 0f));
    }

    private static void InitializeAllProceduralMaterials()
    {
        Texture2D grassTex = GenerateGrassTexture(256, 256);
        Texture2D concreteTex = GenerateConcreteBlocksTexture(256, 256);
        Texture2D damagedConcreteTex = GenerateDamagedConcreteTexture(256, 256);
        Texture2D asphaltTex = GenerateNoisyTexture(256, 256, 0.09f, 0.16f);
        Texture2D brickTex = GenerateBrickTexture(256, 256);
        Texture2D checkerTex = GenerateCheckerTileTexture(256, 256);
        Texture2D hazardTex = GenerateHazardStripeTexture(256, 256);
        Texture2D woodTex = GenerateWoodPlankTexture(256, 256);
        Texture2D metalTex = GenerateNoisyTexture(128, 128, 0.14f, 0.24f);

        _matGrass = CreateTexturedMat("Mat_OvergrownGrass", grassTex, new Color(0.22f, 0.34f, 0.18f), 0.10f, new Vector2(28f, 28f));
        _matGrassDark = CreateTexturedMat("Mat_ForestMossGrass", grassTex, new Color(0.16f, 0.26f, 0.14f), 0.08f, new Vector2(16f, 16f));
        _matMoss = CreateTexturedMat("Mat_WallMoss", grassTex, new Color(0.18f, 0.31f, 0.15f), 0.05f, new Vector2(4f, 4f));
        _matDirtPath = CreateTexturedMat("Mat_DirtTrack", asphaltTex, new Color(0.24f, 0.20f, 0.15f), 0.12f, new Vector2(10f, 20f));
        _matWetAsphalt = CreateTexturedMat("Mat_CrackedAsphalt", asphaltTex, new Color(0.14f, 0.15f, 0.16f), 0.68f, new Vector2(12f, 24f));

        _matConcreteLight = CreateTexturedMat("Mat_SovietConcrete", concreteTex, new Color(0.45f, 0.46f, 0.43f), 0.18f, new Vector2(4f, 3f));
        _matConcreteDark = CreateTexturedMat("Mat_DarkConcreteTrim", concreteTex, new Color(0.27f, 0.29f, 0.28f), 0.25f, new Vector2(3f, 2f));
        _matConcreteDamaged = CreateTexturedMat("Mat_DamagedConcrete", damagedConcreteTex, new Color(0.36f, 0.37f, 0.34f), 0.15f, new Vector2(3f, 3f));
        _matBunkerWall = CreateTexturedMat("Mat_BunkerHeavyWall", damagedConcreteTex, new Color(0.34f, 0.38f, 0.36f), 0.28f, new Vector2(5f, 2f));
        _matBrickWall = CreateTexturedMat("Mat_OldSovietBrick", brickTex, new Color(0.48f, 0.28f, 0.22f), 0.12f, new Vector2(6f, 4f));

        _matLabTileFloor = CreateTexturedMat("Mat_LabFloor", concreteTex, new Color(0.21f, 0.23f, 0.22f), 0.45f, new Vector2(12f, 12f));
        _matCheckerTile = CreateTexturedMat("Mat_SubfloorTile", checkerTex, new Color(0.55f, 0.58f, 0.55f), 0.5f, new Vector2(8f, 8f));
        _matWallUpper = CreateTexturedMat("Mat_WallUpper", concreteTex, new Color(0.38f, 0.41f, 0.39f), 0.2f, new Vector2(6f, 2f));
        _matWallLower = CreateTexturedMat("Mat_WallLower", concreteTex, new Color(0.20f, 0.25f, 0.24f), 0.3f, new Vector2(6f, 1f));
        _matCeilingGrid = CreateMat("Mat_CeilingGrid", new Color(0.28f, 0.30f, 0.29f), 0.1f);

        _matRustedSteel = CreateTexturedMat("Mat_RustedSteel", metalTex, new Color(0.28f, 0.21f, 0.18f), 0.38f, new Vector2(3f, 3f));
        _matDarkIron = CreateTexturedMat("Mat_DarkIron", metalTex, new Color(0.14f, 0.16f, 0.17f), 0.55f, new Vector2(2f, 2f));
        _matCopperPipe = CreateTexturedMat("Mat_IndustrialPipe", metalTex, new Color(0.38f, 0.32f, 0.24f), 0.6f, new Vector2(2f, 1f));
        _matHazardStripes = CreateTexturedMat("Mat_HazardYellow", hazardTex, Color.white, 0.3f, new Vector2(4f, 1f));

        _matRetroBeige = CreateTexturedMat("Mat_ConsoleBeige", concreteTex, new Color(0.55f, 0.54f, 0.47f), 0.3f, new Vector2(2f, 2f));
        _matOldWood = CreateTexturedMat("Mat_OldWoodPlanks", woodTex, new Color(0.38f, 0.28f, 0.19f), 0.15f, new Vector2(2f, 1f));
        _matCrackedWood = CreateTexturedMat("Mat_CrateWood", woodTex, new Color(0.45f, 0.35f, 0.23f), 0.12f, new Vector2(1f, 1f));
        _matChairWhite = CreateMat("Mat_MedicalWhite", new Color(0.76f, 0.78f, 0.75f), 0.4f);
        _matSiloWhite = CreateTexturedMat("Mat_SiloTank", damagedConcreteTex, new Color(0.65f, 0.67f, 0.64f), 0.3f, new Vector2(2f, 2f));

        _matCargoRed = CreateTexturedMat("Mat_CargoRed", metalTex, new Color(0.42f, 0.17f, 0.14f), 0.28f, new Vector2(3f, 1f));
        _matCargoGreen = CreateTexturedMat("Mat_CargoGreen", metalTex, new Color(0.18f, 0.27f, 0.19f), 0.28f, new Vector2(3f, 1f));
        _matCargoBlue = CreateTexturedMat("Mat_CargoBlue", metalTex, new Color(0.16f, 0.24f, 0.32f), 0.28f, new Vector2(3f, 1f));

        _matPineNeedles = CreateTexturedMat("Mat_PineFoliage", grassTex, new Color(0.09f, 0.19f, 0.11f), 0.05f, new Vector2(3f, 3f));
        _matBirchLeaves = CreateTexturedMat("Mat_AutumnFoliage", grassTex, new Color(0.28f, 0.33f, 0.14f), 0.05f, new Vector2(3f, 3f));
        _matTreeTrunk = CreateTexturedMat("Mat_PineBark", woodTex, new Color(0.18f, 0.13f, 0.09f), 0.08f, new Vector2(1f, 3f));
        _matBirchTrunk = CreateTexturedMat("Mat_BirchBark", concreteTex, new Color(0.75f, 0.73f, 0.68f), 0.15f, new Vector2(1f, 4f));

        _matGlassClear = CreateGlassMat("Mat_ObsGlass", new Color(0.55f, 0.75f, 0.72f, 0.22f));
        _matGlassBroken = CreateGlassMat("Mat_DirtyWindow", new Color(0.35f, 0.45f, 0.42f, 0.32f));
        _matWaterPuddle = CreateMat("Mat_ReflectivePuddle", new Color(0.08f, 0.11f, 0.12f), 0.96f);

        _matCrtGreen = CreateEmissiveMat("Mat_CRT_Green", new Color(0.4f, 0.65f, 0.45f), new Color(0.35f, 0.65f, 0.45f) * 1.5f);
        _matCrtAmber = CreateEmissiveMat("Mat_CRT_Amber", new Color(0.8f, 0.5f, 0.15f), new Color(0.85f, 0.55f, 0.15f) * 1.5f);
        _matCrtStatic = CreateEmissiveMat("Mat_CRT_Static", new Color(0.75f, 0.82f, 0.78f), new Color(0.6f, 0.7f, 0.65f) * 1.8f);
        _matLampRed = CreateEmissiveMat("Mat_AlarmRed", Color.red, new Color(1f, 0.15f, 0.1f) * 3.2f);
        _matLampAmber = CreateEmissiveMat("Mat_AmberGlow", new Color(1f, 0.65f, 0.15f), new Color(1f, 0.6f, 0.1f) * 3.2f);
        _matLampCool = CreateEmissiveMat("Mat_Fluorescent", Color.white, new Color(0.75f, 0.92f, 1f) * 2.5f);
        _matLampWarm = CreateEmissiveMat("Mat_WarmBulb", new Color(1f, 0.85f, 0.55f), new Color(1f, 0.8f, 0.45f) * 2.4f);
    }

    // =========================================================================================
    // РОЗДІЛ 0: ДИКА ПРИРОДА, ТРАВА, ВАЛУНИ, ЛІСОВА ЛЕП ТА ПОКИНУТИЙ ТРАНСПОРТ
    // =========================================================================================
    private static void BuildWildernessTerrainAndGrass(Transform parent)
    {
        GameObject terrainGrp = new GameObject("Wilderness_Grass_And_Hills");
        terrainGrp.transform.SetParent(parent);

        CreateBox("Grass_Meadow_South", terrainGrp.transform, new Vector3(0f, -2.5f, -68f), new Vector3(150f, 5.0f, 70f), _matGrass);
        CreateBox("Grass_Meadow_West", terrainGrp.transform, new Vector3(-42f, -2.5f, -10f), new Vector3(56f, 5.0f, 80f), _matGrass);
        CreateBox("Grass_Meadow_East", terrainGrp.transform, new Vector3(42f, -2.5f, -10f), new Vector3(56f, 5.0f, 80f), _matGrass);
        CreateBox("Grass_Meadow_North", terrainGrp.transform, new Vector3(0f, -0.2f, 22f), new Vector3(150f, 0.4f, 44f), _matGrassDark);

        CreateBox("Courtyard_Grass_South", terrainGrp.transform, new Vector3(0f, -2.5f, -38f), new Vector3(52f, 5.0f, 34f), _matGrassDark);
        CreateBox("Courtyard_Grass_West", terrainGrp.transform, new Vector3(-16.5f, -2.5f, -10.5f), new Vector3(21f, 5.0f, 21f), _matGrassDark);
        CreateBox("Courtyard_Grass_East", terrainGrp.transform, new Vector3(16.5f, -2.5f, -10.5f), new Vector3(21f, 5.0f, 21f), _matGrassDark);

        CreateHillMound("Hill_SW_1", terrainGrp.transform, new Vector3(-38f, 0f, -72f), new Vector3(28f, 4.5f, 22f), _matGrassDark);
        CreateHillMound("Hill_SE_1", terrainGrp.transform, new Vector3(38f, 0f, -72f), new Vector3(28f, 5.0f, 24f), _matGrassDark);
        CreateHillMound("Hill_FarSouth", terrainGrp.transform, new Vector3(0f, 0f, -96f), new Vector3(96f, 7.5f, 20f), _matGrassDark);
        CreateHillMound("Hill_WestRidge", terrainGrp.transform, new Vector3(-58f, 0f, -30f), new Vector3(24f, 7.5f, 84f), _matGrassDark);
        CreateHillMound("Hill_EastRidge", terrainGrp.transform, new Vector3(58f, 0f, -30f), new Vector3(24f, 7.5f, 84f), _matGrassDark);

        // Моховиті валуни й каміння вздовж лісової зони
        Vector3[] boulderPositions =
        {
            new Vector3(-14f, 0f, -68f), new Vector3(12f, 0f, -71f), new Vector3(-28f, 0f, -59f),
            new Vector3(-19f, 0f, -55f), new Vector3(19f, 0f, -58f), new Vector3(27f, 0f, -64f),
            new Vector3(-8f, 0f, -77f), new Vector3(9f, 0f, -81f)
        };
        for (int i = 0; i < boulderPositions.Length; i++)
        {
            BuildMossyBoulderCluster($"Forest_Boulder_{i}", terrainGrp.transform, boulderPositions[i], 0.8f + (i % 3) * 0.35f);
        }

        // Густі кущі та бур'яни в лісі й у дворі комплексу
        for (int i = 0; i < 36; i++)
        {
            float angle = i * 19.5f * Mathf.Deg2Rad;
            float rx = Mathf.Cos(angle) * (10f + (i % 5) * 5.2f);
            float rz = -62f + Mathf.Sin(angle) * 14f;
            if (Mathf.Abs(rx) > 4.5f)
            {
                BuildWildBushCluster($"Wilderness_Bush_{i}", terrainGrp.transform, new Vector3(rx, 0f, rz));
            }
        }
    }

    private static void BuildForestApproachRoadAndWreckage(Transform parent)
    {
        GameObject approachGrp = new GameObject("Forest_Approach_Road_And_Props");
        approachGrp.transform.SetParent(parent);

        CreateBox("Approach_DirtBase", approachGrp.transform, new Vector3(0f, 0.01f, -72f), new Vector3(10.5f, 0.04f, 38f), _matDirtPath);

        for (int i = 0; i < 9; i++)
        {
            float zPos = -86f + i * 3.8f;
            float xOff = Mathf.Sin(i * 1.4f) * 0.45f;
            GameObject patch = CreateBox($"BrokenAsphaltPatch_{i}", approachGrp.transform, new Vector3(xOff, 0.03f, zPos), new Vector3(7.6f - (i % 3) * 0.6f, 0.05f, 3.1f), _matWetAsphalt);
            patch.transform.rotation = Quaternion.Euler(0f, Mathf.Cos(i) * 3.5f, 0f);
        }

        // Витоптана стежка в траві від головної дороги ліворуч до ПРОЛОМУ В ПАРКАНІ (X = -24.5, Z = -53)
        for (int s = 0; s < 12; s++)
        {
            float t = s / 11f;
            Vector3 stonePos = Vector3.Lerp(new Vector3(-3.5f, 0.02f, -62f), new Vector3(-24.5f, 0.02f, -55.2f), t);
            stonePos.z += Mathf.Sin(t * Mathf.PI) * -1.8f;
            GameObject slab = CreateBox($"BreachTrail_Slab_{s}", approachGrp.transform, stonePos, new Vector3(1.6f, 0.05f, 1.25f), _matDirtPath);
            slab.transform.rotation = Quaternion.Euler(0f, -68f + s * 3f, 0f);
        }

        // Покинута військова вантажівка ЗІЛ-131 із відкритим кузовом та ящиками
        BuildAbandonedSovietTruck("Abandoned_Expedition_Truck", approachGrp.transform, new Vector3(6.2f, 0f, -63f), Quaternion.Euler(0f, -24f, 2f));

        // Розбитий експедиційний УАЗ-469 із увімкненою однією фарою, що світить у бік пролому в стіні!
        BuildAbandonedSovietJeep("Abandoned_UAZ_Jeep", approachGrp.transform, new Vector3(-6.4f, 0f, -61.5f), Quaternion.Euler(0f, -52f, -3f));

        // Дерев'яні стовпи старої лісової лінії електропередач із провислими проводами
        for (int p = 0; p < 3; p++)
        {
            float pz = -82f + p * 12f;
            BuildWoodenTelegraphPole($"Forest_TelegraphPole_L_{p}", approachGrp.transform, new Vector3(-8.5f, 0f, pz), 4f * (p - 1));
            BuildWoodenTelegraphPole($"Forest_TelegraphPole_R_{p}", approachGrp.transform, new Vector3(8.5f, 0f, pz), -3f * (p - 1));
        }

        BuildWarningSignpost("Sign_RestrictedZone_Main", approachGrp.transform, new Vector3(-5.5f, 0f, -57.5f), Quaternion.Euler(0f, 15f, -3f));
        BuildWarningSignpost("Sign_Biohazard_Warning", approachGrp.transform, new Vector3(-21.2f, 0f, -55.5f), Quaternion.Euler(0f, -12f, 4f));

        BuildConcreteRoadblock("Roadblock_Outer_1", approachGrp.transform, new Vector3(-3.2f, 0f, -56.2f), 14f);
        BuildConcreteRoadblock("Roadblock_Outer_2", approachGrp.transform, new Vector3(3.4f, 0f, -56.8f), -22f);
        BuildMetalHedgehog("AntiTank_Hedgehog_1", approachGrp.transform, new Vector3(0.2f, 0f, -55.8f), 28f);
    }

    private static void BuildSurroundingDenseForest(Transform parent)
    {
        GameObject forestGrp = new GameObject("Surrounding_Pine_And_Birch_Forest");
        forestGrp.transform.SetParent(parent);

        Vector3[] southTrees =
        {
            new Vector3(-16f, 0f, -66f), new Vector3(-22f, 0f, -69f), new Vector3(-30f, 0f, -64f),
            new Vector3(-12f, 0f, -75f), new Vector3(-26f, 0f, -78f), new Vector3(-35f, 0f, -74f),
            new Vector3(15f, 0f, -65f), new Vector3(23f, 0f, -68f), new Vector3(31f, 0f, -63f),
            new Vector3(13f, 0f, -76f), new Vector3(25f, 0f, -79f), new Vector3(36f, 0f, -72f),
            new Vector3(-8f, 0f, -84f), new Vector3(9f, 0f, -85f), new Vector3(0f, 0f, -88f),
            new Vector3(-19f, 0f, -82f), new Vector3(20f, 0f, -83f), new Vector3(-32f, 0f, -85f)
        };

        for (int i = 0; i < southTrees.Length; i++)
        {
            float scale = 0.85f + (i % 4) * 0.14f;
            if (i % 3 == 0)
                BuildDetailedBirchTree($"Forest_Birch_S_{i}", forestGrp.transform, southTrees[i], scale);
            else
                BuildDetailedPineTree($"Forest_Pine_S_{i}", forestGrp.transform, southTrees[i], scale);
        }

        for (float z = -62f; z <= 26f; z += 5.0f)
        {
            float offsetW = Mathf.Sin(z * 0.7f) * 3.5f;
            float offsetE = Mathf.Cos(z * 0.6f) * 3.5f;
            float scale = 0.9f + Mathf.Abs(Mathf.Sin(z)) * 0.35f;

            BuildDetailedPineTree($"Pine_WestPerimeter_{z}", forestGrp.transform, new Vector3(-41f + offsetW, 0f, z), scale);
            BuildDetailedPineTree($"Pine_EastPerimeter_{z}", forestGrp.transform, new Vector3(41f + offsetE, 0f, z), scale);

            if ((int)Mathf.Abs(z) % 2 == 0)
            {
                BuildDetailedBirchTree($"Birch_WestInner_{z}", forestGrp.transform, new Vector3(-47f - offsetW, 0f, z + 2f), scale * 0.9f);
                BuildDetailedBirchTree($"Birch_EastInner_{z}", forestGrp.transform, new Vector3(47f - offsetE, 0f, z + 2f), scale * 0.9f);
            }
        }

        // Дерева, що проросли крізь асфальт у самому дворі комплексу
        BuildDetailedBirchTree("Courtyard_Overgrown_Birch_1", forestGrp.transform, new Vector3(-9.5f, 0f, -45f), 0.85f);
        BuildDetailedBirchTree("Courtyard_Overgrown_Birch_2", forestGrp.transform, new Vector3(10.5f, 0f, -22f), 0.95f);
        BuildDetailedPineTree("Courtyard_Overgrown_Pine_1", forestGrp.transform, new Vector3(-10.8f, 0f, -21f), 0.80f);
        BuildDetailedBirchTree("Courtyard_Overgrown_Birch_3", forestGrp.transform, new Vector3(11.8f, 0f, -44f), 0.78f);
    }

    // =========================================================================================
    // РОЗДІЛ 1: ПЕРИМЕТР ЗАБРОШКИ, ПРОЛОМ У СТІНІ ТА КПП ОХОРОНИ З ІНТЕР'ЄРОМ
    // =========================================================================================
    private static void BuildDamagedPerimeterFenceWithBreach(Transform parent)
    {
        GameObject fenceGrp = new GameObject("Concrete_Perimeter_Wall_With_Breach");
        fenceGrp.transform.SetParent(parent);

        BuildSegmentedPerimeterWall("Wall_West", fenceGrp.transform, new Vector3(-36f, 0f, -53f), new Vector3(-36f, 0f, 16f), 15);
        BuildSegmentedPerimeterWall("Wall_East", fenceGrp.transform, new Vector3(36f, 0f, -53f), new Vector3(36f, 0f, 16f), 15);
        BuildSegmentedPerimeterWall("Wall_South_Right", fenceGrp.transform, new Vector3(7.5f, 0f, -53f), new Vector3(36f, 0f, -53f), 6);

        // Ліва частина південного паркану з ПРОЛОМОМ на X = -24.5
        BuildSegmentedPerimeterWall("Wall_South_Left_Inner", fenceGrp.transform, new Vector3(-7.5f, 0f, -53f), new Vector3(-21.0f, 0f, -53f), 3);
        BuildSegmentedPerimeterWall("Wall_South_Left_Outer", fenceGrp.transform, new Vector3(-28.0f, 0f, -53f), new Vector3(-36f, 0f, -53f), 2);

        BuildWallBreachAndClimbableRubble(fenceGrp.transform, new Vector3(-24.5f, 0f, -53f));

        BuildCornerWatchtower("Watchtower_SW", fenceGrp.transform, new Vector3(-36f, 0f, -53f), 45f);
        BuildCornerWatchtower("Watchtower_SE", fenceGrp.transform, new Vector3(36f, 0f, -53f), -45f);
        BuildCornerWatchtower("Watchtower_NW", fenceGrp.transform, new Vector3(-36f, 0f, 16f), 135f);
        BuildCornerWatchtower("Watchtower_NE", fenceGrp.transform, new Vector3(36f, 0f, 16f), -135f);
    }

    private static void BuildWallBreachAndClimbableRubble(Transform parent, Vector3 breachCenter)
    {
        GameObject breach = new GameObject("Coop_Wall_Breach_Climb_Entry");
        breach.transform.SetParent(parent);
        breach.transform.position = breachCenter;

        GameObject brokenLeft = CreateBox("BrokenSlab_L", breach.transform, breachCenter + new Vector3(-2.8f, 1.2f, 0f), new Vector3(1.6f, 2.4f, 0.55f), _matConcreteDamaged);
        brokenLeft.transform.rotation = Quaternion.Euler(0f, 0f, -14f);

        GameObject brokenRight = CreateBox("BrokenSlab_R", breach.transform, breachCenter + new Vector3(2.8f, 1.1f, 0f), new Vector3(1.6f, 2.2f, 0.55f), _matConcreteDamaged);
        brokenRight.transform.rotation = Quaternion.Euler(0f, 0f, 18f);

        CreateBox("Breach_LowSill", breach.transform, breachCenter + new Vector3(0f, 0.42f, 0f), new Vector3(4.2f, 0.84f, 0.65f), _matConcreteDamaged);

        // Похила бетонна плита зовні та дерев'яний настил зсередини (плавний перехід без застрягання)
        GameObject fallenSlabOuter = CreateBox("Fallen_Climb_Slab_Outer", breach.transform, breachCenter + new Vector3(0f, 0.45f, -1.65f), new Vector3(3.4f, 0.25f, 3.5f), _matConcreteDamaged);
        fallenSlabOuter.transform.rotation = Quaternion.Euler(-15f, 0f, 0f);

        GameObject fallenSlabInner = CreateBox("Fallen_Climb_Planks_Inner", breach.transform, breachCenter + new Vector3(0f, 0.45f, 1.65f), new Vector3(3.0f, 0.22f, 3.5f), _matOldWood);
        fallenSlabInner.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        for (int r = -2; r <= 2; r++)
        {
            CreateCylinder($"Rebar_Rod_{r}", breach.transform, breachCenter + new Vector3(r * 0.65f, 0.95f, 0f), new Vector3(0.04f, 0.35f, 0.04f), _matRustedSteel);
        }

        // Будівельні риштування (Scaffolding) та ящики біля пролому
        BuildWoodenCrate("Breach_Crate_1", breach.transform, breachCenter + new Vector3(-2.3f, 0.45f, -1.8f), new Vector3(0.95f, 0.9f, 0.95f), 14f);
        BuildWoodenCrate("Breach_Crate_2", breach.transform, breachCenter + new Vector3(-2.2f, 1.25f, -1.7f), new Vector3(0.75f, 0.7f, 0.75f), -22f);
        BuildRustedBarrel("Breach_Barrel_1", breach.transform, breachCenter + new Vector3(2.2f, 0.55f, -1.4f));
        BuildRustedBarrel("Breach_Barrel_2_Tipped", breach.transform, breachCenter + new Vector3(2.5f, 0.32f, 1.5f)).transform.rotation = Quaternion.Euler(86f, 35f, 0f);

        CreateWallLamp("Breach_HintLight", breach.transform, breachCenter + new Vector3(-2.6f, 2.4f, -0.5f), _matLampAmber, new Color(1f, 0.65f, 0.2f), 3.4f, 11f);
    }

    private static void BuildDetailedMainCheckpointAndGuardhouse(Transform parent, Vector3 pos)
    {
        GameObject cp = new GameObject("South_MainGate_And_Enterable_Guardhouse");
        cp.transform.SetParent(parent);

        CreateBox("GatePillar_L", cp.transform, pos + new Vector3(-5.8f, 2.6f, 0f), new Vector3(1.5f, 5.2f, 1.5f), _matConcreteLight);
        CreateBox("GatePillar_R", cp.transform, pos + new Vector3(5.8f, 2.6f, 0f), new Vector3(1.5f, 5.2f, 1.5f), _matConcreteLight);
        CreateBox("Gate_CanopyBeam", cp.transform, pos + new Vector3(0f, 4.8f, 0f), new Vector3(15.2f, 0.85f, 2.4f), _matConcreteDark);
        CreateBox("Gate_HazardTrim", cp.transform, pos + new Vector3(0f, 4.3f, -1.18f), new Vector3(14.8f, 0.22f, 0.08f), _matHazardStripes);

        // Напіввідірвані літери вивіски на даху КПП
        for (int c = -5; c <= 5; c++)
        {
            float tilt = (c == 2 || c == -3) ? 24f : 0f;
            GameObject letter = CreateBox($"SignBoard_Letter_{c}", cp.transform, pos + new Vector3(c * 0.85f, 5.5f, -1.0f), new Vector3(0.55f, 0.65f, 0.12f), _matRustedSteel);
            letter.transform.rotation = Quaternion.Euler(0f, 0f, tilt);
        }

        // Заблоковані металеві гратчасті ворота з вертикальними прутами
        CreateBox("LockedGate_FrameL", cp.transform, pos + new Vector3(-2.5f, 1.9f, 0f), new Vector3(4.8f, 3.8f, 0.14f), _matRustedSteel);
        CreateBox("LockedGate_FrameR", cp.transform, pos + new Vector3(2.5f, 1.9f, 0f), new Vector3(4.8f, 3.8f, 0.14f), _matRustedSteel);
        CreateBox("Gate_PadlockChain", cp.transform, pos + new Vector3(0f, 1.4f, -0.15f), new Vector3(0.9f, 0.25f, 0.25f), _matHazardStripes);

        CreateBox("Barrier_Pedestal", cp.transform, pos + new Vector3(-4.6f, 0.6f, 2.8f), new Vector3(0.45f, 1.2f, 0.45f), _matHazardStripes);
        GameObject brokenArm = CreateBox("Barrier_BrokenArm", cp.transform, pos + new Vector3(-0.5f, 0.85f, 2.8f), new Vector3(8.2f, 0.12f, 0.12f), _matCargoRed);
        brokenArm.transform.rotation = Quaternion.Euler(0f, 0f, -8f);

        // --- БУДКА КПП ОХОРОНИ З ДЕТАЛІЗОВАНИМ ІНТЕР'ЄРОМ (X = -10.5, Z = -49.5) ---
        Vector3 ghCenter = pos + new Vector3(-10.5f, 0f, 3.5f);
        GameObject gh = new GameObject("Enterable_Guardhouse_Building");
        gh.transform.SetParent(cp.transform);

        float gw = 6.8f;
        float gd = 6.4f;
        float ghH = 3.5f;

        CreateBox("GH_Floor", gh.transform, ghCenter + new Vector3(0f, 0.08f, 0f), new Vector3(gw, 0.16f, gd), _matCheckerTile);
        CreateBox("GH_RoofSlab", gh.transform, ghCenter + new Vector3(0f, ghH + 0.18f, 0f), new Vector3(gw + 0.8f, 0.36f, gd + 0.8f), _matConcreteDark);

        CreateBox("GH_Wall_West", gh.transform, ghCenter + new Vector3(-gw / 2f, ghH / 2f, 0f), new Vector3(0.32f, ghH, gd), _matBrickWall);
        CreateWallWithWindow("GH_Wall_South", gh.transform, ghCenter + new Vector3(0f, 0f, -gd / 2f), gw, ghH, 2.8f, 1.3f, false, _matBrickWall, _matGlassBroken);
        CreateWallWithWindow("GH_Wall_East_RoadView", gh.transform, ghCenter + new Vector3(gw / 2f, 0f, 0f), gd, ghH, 3.2f, 1.4f, true, _matBrickWall, _matGlassBroken);

        CreateBox("GH_Wall_North_L", gh.transform, ghCenter + new Vector3(-2.1f, ghH / 2f, gd / 2f), new Vector3(2.6f, ghH, 0.32f), _matBrickWall);
        CreateBox("GH_Wall_North_R", gh.transform, ghCenter + new Vector3(2.2f, ghH / 2f, gd / 2f), new Vector3(2.4f, ghH, 0.32f), _matBrickWall);
        CreateBox("GH_Wall_North_Top", gh.transform, ghCenter + new Vector3(0.1f, ghH - 0.45f, gd / 2f), new Vector3(1.8f, 0.9f, 0.32f), _matBrickWall);

        GameObject openDoor = CreateBox("GH_OpenDoor", gh.transform, ghCenter + new Vector3(-0.45f, 1.2f, gd / 2f - 0.65f), new Vector3(1.4f, 2.4f, 0.08f), _matOldWood);
        openDoor.transform.rotation = Quaternion.Euler(0f, 65f, 0f);

        // Деталі інтер'єру КПП: стіл чергового, пульт шлагбаума, телефон, настінна аптечка, чавунна батарея опалення, щиток і шафи
        CreateBox("GH_Desk_Top", gh.transform, ghCenter + new Vector3(1.7f, 0.78f, -1.2f), new Vector3(1.3f, 0.08f, 2.8f), _matOldWood);
        CreateBox("GH_Desk_Leg1", gh.transform, ghCenter + new Vector3(1.7f, 0.38f, -2.4f), new Vector3(1.1f, 0.76f, 0.1f), _matDarkIron);
        CreateBox("GH_Desk_Leg2", gh.transform, ghCenter + new Vector3(1.7f, 0.38f, 0.0f), new Vector3(1.1f, 0.76f, 0.1f), _matDarkIron);

        CreateBox("GH_GateControlConsole", gh.transform, ghCenter + new Vector3(1.8f, 0.95f, -0.4f), new Vector3(0.55f, 0.28f, 0.65f), _matRetroBeige);
        CreateBox("GH_RotaryPhone", gh.transform, ghCenter + new Vector3(1.5f, 0.9f, -1.9f), new Vector3(0.32f, 0.18f, 0.28f), _matCargoRed);
        CreateBox("GH_Logbook", gh.transform, ghCenter + new Vector3(1.4f, 0.84f, -1.1f), new Vector3(0.42f, 0.03f, 0.55f), _matChairWhite);

        // Настінна медична аптечка з хрестом та електричний щиток із запобіжниками
        CreateBox("GH_FirstAidBox", gh.transform, ghCenter + new Vector3(-3.18f, 1.6f, 1.4f), new Vector3(0.18f, 0.55f, 0.45f), _matChairWhite);
        CreateBox("GH_FirstAidCross", gh.transform, ghCenter + new Vector3(-3.08f, 1.6f, 1.4f), new Vector3(0.02f, 0.25f, 0.25f), _matCargoRed);
        BuildWallFuseBox("GH_FuseBox", gh.transform, ghCenter + new Vector3(-3.18f, 1.6f, 2.2f), Quaternion.Euler(0f, 90f, 0f));

        // Чавунний радіатор опалення під розбитим вікном
        BuildCastIronRadiator("GH_Radiator", gh.transform, ghCenter + new Vector3(0f, 0.45f, -2.95f), Quaternion.identity);

        // Збройові шафки охоронців (одна відчинена, інша повалена)
        CreateBox("GH_Locker_1", gh.transform, ghCenter + new Vector3(-2.7f, 1.1f, -1.9f), new Vector3(0.75f, 2.2f, 0.85f), _matCargoGreen);
        CreateBox("GH_Locker_2", gh.transform, ghCenter + new Vector3(-2.7f, 1.1f, -0.95f), new Vector3(0.75f, 2.2f, 0.85f), _matCargoGreen);
        GameObject fallenLocker = CreateBox("GH_Locker_Fallen", gh.transform, ghCenter + new Vector3(-1.4f, 0.38f, 0.6f), new Vector3(0.75f, 2.2f, 0.85f), _matCargoGreen);
        fallenLocker.transform.rotation = Quaternion.Euler(0f, 25f, -88f);

        CreateWallLamp("GH_InteriorBulb", gh.transform, ghCenter + new Vector3(0f, ghH - 0.15f, 0f), _matLampWarm, new Color(1f, 0.78f, 0.42f), 3.4f, 9f);
    }

    // =========================================================================================
    // РОЗДІЛ 2: ВЕРХНІЙ ПОВЕРХ — КОМПЛЕКС З ВІДКРИТИМИ КОРПУСАМИ ТА ІНТЕР'ЄРАМИ (Y = 0.0m)
    // =========================================================================================
    private static void BuildCourtyardRoadsCracksAndOvergrowth(Transform parent)
    {
        GameObject court = new GameObject("Courtyard_BrokenPavement_And_Puddles");
        court.transform.SetParent(parent);

        for (int row = 0; row < 8; row++)
        {
            for (int col = -1; col <= 1; col++)
            {
                float pz = -49f + row * 3.8f;
                float px = col * 3.6f;
                float tiltX = Mathf.Sin(row * 1.7f + col) * 0.9f;
                float tiltZ = Mathf.Cos(row + col * 2.1f) * 0.9f;

                GameObject slab = CreateBox($"ConcreteRoadSlab_{row}_{col}", court.transform, new Vector3(px, 0.02f, pz), new Vector3(3.32f, 0.08f, 3.52f), _matConcreteDamaged);
                slab.transform.rotation = Quaternion.Euler(tiltX, 0f, tiltZ);
            }
        }

        // Поперечні бетонні доріжки до Західного та Східного корпусів
        for (int c = -4; c <= 4; c++)
        {
            if (Mathf.Abs(c) <= 1) continue;
            CreateBox($"CrossSlab_South_{c}", court.transform, new Vector3(c * 3.4f, 0.02f, -34f), new Vector3(3.15f, 0.07f, 3.4f), _matConcreteDamaged);
            CreateBox($"CrossSlab_North_{c}", court.transform, new Vector3(c * 3.4f, 0.02f, -23f), new Vector3(3.15f, 0.07f, 3.4f), _matConcreteDamaged);
        }

        CreateBox("WaterPuddle_Main_1", court.transform, new Vector3(-1.4f, 0.055f, -34f), new Vector3(3.8f, 0.01f, 4.5f), _matWaterPuddle);
        CreateBox("WaterPuddle_Main_2", court.transform, new Vector3(2.1f, 0.055f, -24f), new Vector3(4.2f, 0.01f, 3.2f), _matWaterPuddle);
        CreateBox("WaterPuddle_East_3", court.transform, new Vector3(9.5f, 0.055f, -13f), new Vector3(3.2f, 0.01f, 3.6f), _matWaterPuddle);
    }

    // --- КОРПУС 1: ЗАХІДНИЙ МАШИННИЙ ЗАЛ ТА ЕЛЕКТРОПІДСТАНЦІЯ З МІСТКОМ CATWALK (X = -19.5, Z = -34) ---
    private static void BuildWestPowerGeneratorHallWithInterior(Transform parent, Vector3 center)
    {
        GameObject hall = new GameObject("West_Enterable_PowerGenerator_Hall");
        hall.transform.SetParent(parent);

        float w = 15.5f;
        float d = 15.0f;
        float h = 6.5f;

        CreateBox("GenHall_Floor", hall.transform, center + new Vector3(0f, 0.1f, 0f), new Vector3(w, 0.2f, d), _matLabTileFloor);
        CreateBox("GenHall_Roof", hall.transform, center + new Vector3(0f, h + 0.2f, 0f), new Vector3(w + 0.6f, 0.4f, d + 0.6f), _matConcreteDark);

        CreateBox("GenHall_Wall_West", hall.transform, center + new Vector3(-w / 2f, h / 2f, 0f), new Vector3(0.45f, h, d), _matConcreteLight);
        CreateWallWithWindow("GenHall_Wall_South", hall.transform, center + new Vector3(0f, 0f, -d / 2f), w, h, 6.4f, 2.4f, false, _matConcreteLight, _matGlassBroken);
        CreateWallWithWindow("GenHall_Wall_North", hall.transform, center + new Vector3(0f, 0f, d / 2f), w, h, 6.4f, 2.4f, false, _matConcreteLight, _matGlassBroken);

        // Східний фасад із широкими розсунутими індустріальними воротами
        CreateBox("GenHall_EastWall_L", hall.transform, center + new Vector3(w / 2f, h / 2f, -4.8f), new Vector3(0.45f, h, 5.2f), _matConcreteLight);
        CreateBox("GenHall_EastWall_R", hall.transform, center + new Vector3(w / 2f, h / 2f, 4.8f), new Vector3(0.45f, h, 5.2f), _matConcreteLight);
        CreateBox("GenHall_EastWall_Header", hall.transform, center + new Vector3(w / 2f, h - 1.15f, 0f), new Vector3(0.45f, 2.3f, 4.6f), _matConcreteDark);
        CreateBox("GenHall_HazardFrame", hall.transform, center + new Vector3(w / 2f + 0.24f, 4.1f, 0f), new Vector3(0.08f, 0.35f, 4.8f), _matHazardStripes);

        // Дві масивні дизель-генераторні турбіни з трубами охолодження та шафами реле
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 tPos = center + new Vector3(-1.8f, 0f, i * 3.6f);
            CreateBox($"Turbine_Base_{i}", hall.transform, tPos + new Vector3(0f, 0.35f, 0f), new Vector3(5.4f, 0.7f, 2.5f), _matHazardStripes);
            GameObject cyl = CreateCylinder($"Turbine_Cylinder_{i}", hall.transform, tPos + new Vector3(0f, 1.55f, 0f), new Vector3(1.85f, 2.4f, 1.85f), _matCargoGreen);
            cyl.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            CreateCylinder($"Turbine_Flywheel_{i}", hall.transform, tPos + new Vector3(2.5f, 1.55f, 0f), new Vector3(2.1f, 0.25f, 2.1f), _matRustedSteel).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            CreateBox($"Turbine_ExhaustPipe_{i}", hall.transform, tPos + new Vector3(-1.6f, 3.8f, 0f), new Vector3(0.55f, 4.5f, 0.55f), _matRustedSteel);
        }

        // Металевий місток обслуговування (Catwalk) уздовж західної стіни зі сходами, куди гравець може піднятися!
        CreateBox("Catwalk_Platform", hall.transform, center + new Vector3(-5.8f, 2.4f, 0f), new Vector3(2.4f, 0.15f, 13.5f), _matDarkIron);
        CreateBox("Catwalk_Railing", hall.transform, center + new Vector3(-4.65f, 2.95f, 0f), new Vector3(0.08f, 1.0f, 13.5f), _matRustedSteel);
        GameObject catwalkRamp = CreateBox("Catwalk_StairRamp", hall.transform, center + new Vector3(-3.2f, 1.2f, -5.8f), new Vector3(3.6f, 0.18f, 1.4f), _matDarkIron);
        catwalkRamp.transform.rotation = Quaternion.Euler(0f, 0f, -34f);

        // Стіл зарядки енергоблоків (тут лежить Power_Battery на X=-16.2, Z=-32.5)
        CreateBox("Battery_Charging_Bench", hall.transform, new Vector3(-16.2f, 0.42f, -32.5f), new Vector3(1.6f, 0.84f, 2.8f), _matDarkIron);
        CreateBox("Battery_Charger_Panel", hall.transform, new Vector3(-16.8f, 1.45f, -32.5f), new Vector3(0.45f, 1.3f, 2.2f), _matRetroBeige);
        CreateWallLamp("Battery_SpotLight", hall.transform, new Vector3(-15.8f, 3.6f, -32.5f), _matLampAmber, new Color(1f, 0.68f, 0.22f), 4.5f, 11f);

        // Обвалена металева ферма даху та розкидані кабельні котушки
        GameObject fallenBeam = CreateBox("Collapsed_Roof_Girder", hall.transform, center + new Vector3(2.2f, 2.2f, -2.5f), new Vector3(0.35f, 0.35f, 7.2f), _matRustedSteel);
        fallenBeam.transform.rotation = Quaternion.Euler(32f, 18f, 0f);
        CreateCylinder("Cable_Spool_1", hall.transform, center + new Vector3(3.8f, 0.65f, 4.2f), new Vector3(1.3f, 0.45f, 1.3f), _matOldWood);

        // Зовнішня підстанція з трансформаторами та ЛЕП-вежею
        CreateBox("Substation_Pylon_Tower", hall.transform, center + new Vector3(6.2f, 6.0f, -5.5f), new Vector3(1.2f, 12f, 1.2f), _matDarkIron);
        CreateBox("Substation_CrossArm1", hall.transform, center + new Vector3(6.2f, 9.2f, -5.5f), new Vector3(5.8f, 0.35f, 0.35f), _matDarkIron);
        CreateBox("Substation_CrossArm2", hall.transform, center + new Vector3(6.2f, 11.2f, -5.5f), new Vector3(4.2f, 0.35f, 0.35f), _matDarkIron);
    }

    // --- КОРПУС 2: ЗАХІДНИЙ ГАРАЖНИЙ БОКС ТА КАЗАРМА ОХОРОНИ З ІНТЕР'ЄРОМ (X = -21, Z = -12) ---
    private static void BuildWestBarracksAndGarageWithInterior(Transform parent, Vector3 center)
    {
        GameObject zone = new GameObject("West_Enterable_Barracks_And_Garage");
        zone.transform.SetParent(parent);

        float w = 14f;
        float d = 17f;
        float h = 9.8f;

        // Нижній поверх відкритий (ремонтний гараж + кімната казарми), верхні 2 поверхи — бруталістський фасад
        CreateBox("Barracks_Floor1", zone.transform, center + new Vector3(0f, 0.1f, 0f), new Vector3(w, 0.2f, d), _matConcreteDark);
        CreateBox("Barracks_UpperMass", zone.transform, center + new Vector3(0f, 6.8f, 0f), new Vector3(w, 6.0f, d), _matConcreteDamaged);
        CreateBox("Barracks_RoofCornice", zone.transform, center + new Vector3(0f, h + 0.3f, 0f), new Vector3(w + 0.7f, 0.6f, d + 0.7f), _matConcreteDark);

        // Стіни 1-го поверху з відкритими гаражними воротами на схід (у бік двору та бункера)
        CreateBox("Barracks_1F_West", zone.transform, center + new Vector3(-w / 2f, 1.9f, 0f), new Vector3(0.4f, 3.8f, d), _matBrickWall);
        CreateBox("Barracks_1F_South", zone.transform, center + new Vector3(0f, 1.9f, -d / 2f), new Vector3(w, 3.8f, 0.4f), _matBrickWall);
        CreateBox("Barracks_1F_North", zone.transform, center + new Vector3(0f, 1.9f, d / 2f), new Vector3(w, 3.8f, 0.4f), _matBrickWall);

        CreateBox("Barracks_1F_East_L", zone.transform, center + new Vector3(w / 2f, 1.9f, -5.5f), new Vector3(0.4f, 3.8f, 6f), _matBrickWall);
        CreateBox("Barracks_1F_East_R", zone.transform, center + new Vector3(w / 2f, 1.9f, 5.5f), new Vector3(0.4f, 3.8f, 6f), _matBrickWall);
        CreateBox("Barracks_1F_GateHeader", zone.transform, center + new Vector3(w / 2f, 3.3f, 0f), new Vector3(0.45f, 1.0f, 5.0f), _matHazardStripes);

        // Всередині 1-го поверху: верстак, стелажі з запчастинами, двоярусні металеві ліжка персоналу, бочки та ящики
        CreateBox("Garage_Workbench", zone.transform, center + new Vector3(-4.8f, 0.45f, -4.5f), new Vector3(1.4f, 0.9f, 4.8f), _matOldWood);
        BuildMetalShelvingRack("Garage_StorageRack_1", zone.transform, center + new Vector3(-4.8f, 0f, 2.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildMetalShelvingRack("Garage_StorageRack_2", zone.transform, center + new Vector3(-4.8f, 0f, 5.5f), Quaternion.Euler(0f, 90f, 0f));

        // Двоярусні залізні ліжка в кутку чергової зміни
        for (int b = 0; b < 2; b++)
        {
            Vector3 bPos = center + new Vector3(1.5f, 0f, -6.2f + b * 2.8f);
            CreateBox($"BunkBed_Lower_{b}", zone.transform, bPos + new Vector3(0f, 0.45f, 0f), new Vector3(2.1f, 0.15f, 1.1f), _matOldWood);
            CreateBox($"BunkBed_Upper_{b}", zone.transform, bPos + new Vector3(0f, 1.35f, 0f), new Vector3(2.1f, 0.15f, 1.1f), _matOldWood);
            CreateBox($"BunkBed_FrameL_{b}", zone.transform, bPos + new Vector3(-1.0f, 0.9f, 0f), new Vector3(0.08f, 1.8f, 1.1f), _matDarkIron);
            CreateBox($"BunkBed_FrameR_{b}", zone.transform, bPos + new Vector3(1.0f, 0.9f, 0f), new Vector3(0.08f, 1.8f, 1.1f), _matDarkIron);
        }

        CreateWallLamp("Garage_WorkLight", zone.transform, center + new Vector3(0f, 3.55f, 0f), _matLampWarm, new Color(1f, 0.72f, 0.38f), 3.5f, 11f);

        // Морські контейнери зовні біля гаража
        CreateBox("Container_Red", zone.transform, center + new Vector3(9.5f, 1.3f, -5.2f), new Vector3(2.5f, 2.6f, 6.0f), _matCargoRed);
        GameObject tiltedCont = CreateBox("Container_Blue_Tilted", zone.transform, center + new Vector3(9.5f, 3.85f, -4.9f), new Vector3(2.5f, 2.6f, 6.0f), _matCargoBlue);
        tiltedCont.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
    }

    // --- КОРПУС 3: СХІДНИЙ САНІТАРНО-КАРАНТИННИЙ ЛАБОРАТОРНИЙ БЛОК З ІНТЕР'ЄРОМ (X = +19.5, Z = -12) ---
    private static void BuildEastDecontaminationLabWithInterior(Transform parent, Vector3 center)
    {
        GameObject lab = new GameObject("East_Enterable_Decontamination_Lab_Wing");
        lab.transform.SetParent(parent);

        float w = 15f;
        float d = 18f;
        float h = 9.5f;

        CreateBox("EastLab_Floor1", lab.transform, center + new Vector3(0f, 0.1f, 0f), new Vector3(w, 0.2f, d), _matCheckerTile);
        CreateBox("EastLab_UpperFloorsMass", lab.transform, center + new Vector3(0f, 6.65f, 0f), new Vector3(w, 5.7f, d), _matConcreteLight);
        CreateBox("EastLab_RoofCornice", lab.transform, center + new Vector3(0f, h + 0.3f, 0f), new Vector3(w + 0.8f, 0.6f, d + 0.8f), _matConcreteDark);

        for (int z = -7; z <= 7; z += 3)
        {
            CreateBox($"EastLab_Pilaster_{z}", lab.transform, center + new Vector3(-w / 2f - 0.18f, h / 2f, z), new Vector3(0.36f, h, 0.45f), _matConcreteDark);
            CreateBox($"EastLab_Win2F_{z}", lab.transform, center + new Vector3(-w / 2f - 0.04f, 5.2f, z + 1.5f), new Vector3(0.08f, 1.4f, 1.8f), _matGlassBroken);
            CreateBox($"EastLab_Win3F_{z}", lab.transform, center + new Vector3(-w / 2f - 0.04f, 8.0f, z + 1.5f), new Vector3(0.08f, 1.4f, 1.8f), _matGlassBroken);
        }

        CreateBox("EastLab_1F_BackEast", lab.transform, center + new Vector3(w / 2f, 1.9f, 0f), new Vector3(0.4f, 3.8f, d), _matWallUpper);
        CreateBox("EastLab_1F_South", lab.transform, center + new Vector3(0f, 1.9f, -d / 2f), new Vector3(w, 3.8f, 0.4f), _matWallUpper);
        CreateBox("EastLab_1F_North", lab.transform, center + new Vector3(0f, 1.9f, d / 2f), new Vector3(w, 3.8f, 0.4f), _matWallUpper);

        CreateBox("EastLab_1F_West_L", lab.transform, center + new Vector3(-w / 2f, 1.9f, -5.5f), new Vector3(0.4f, 3.8f, 7f), _matWallLower);
        CreateBox("EastLab_1F_West_R", lab.transform, center + new Vector3(-w / 2f, 1.9f, 5.5f), new Vector3(0.4f, 3.8f, 7f), _matWallLower);
        CreateBox("EastLab_1F_DoorHeader", lab.transform, center + new Vector3(-w / 2f, 3.2f, 0f), new Vector3(0.45f, 1.2f, 4.0f), _matConcreteDark);

        // Лабораторне обладнання всередині: витяжні шафи, центрифуги, скляний ізолятор біозахисту, стелажі з колбами
        CreateBox("LabBench_1", lab.transform, center + new Vector3(2.2f, 0.45f, -4.8f), new Vector3(1.4f, 0.9f, 5.5f), _matChairWhite);
        CreateBox("LabBench_2", lab.transform, center + new Vector3(2.2f, 0.45f, 4.8f), new Vector3(1.4f, 0.9f, 5.5f), _matChairWhite);
        CreateBox("Biohazard_Chamber_Glass", lab.transform, center + new Vector3(4.2f, 1.4f, 0f), new Vector3(2.8f, 2.8f, 3.4f), _matGlassClear);
        CreateBox("Biohazard_Chamber_Base", lab.transform, center + new Vector3(4.2f, 0.2f, 0f), new Vector3(3.0f, 0.4f, 3.6f), _matHazardStripes);
        CreateBox("Biohazard_Chamber_Top", lab.transform, center + new Vector3(4.2f, 2.9f, 0f), new Vector3(3.0f, 0.35f, 3.6f), _matHazardStripes);

        BuildMetalShelvingRack("EastLab_SampleRack_1", lab.transform, center + new Vector3(-2.5f, 0f, -7.2f), Quaternion.identity);
        BuildMetalShelvingRack("EastLab_SampleRack_2", lab.transform, center + new Vector3(1.5f, 0f, -7.2f), Quaternion.identity);

        // Обвалені плитки підвісної стелі на підлозі лабораторії та мох
        CreateBox("MossPatch_LabFloor", lab.transform, center + new Vector3(-3.5f, 0.21f, -2.2f), new Vector3(3.5f, 0.02f, 4.2f), _matMoss);
        CreateWallLamp("EastLab_InteriorLight", lab.transform, center + new Vector3(0f, 3.55f, 0f), _matLampCool, new Color(0.65f, 0.95f, 0.82f), 3.8f, 12f);
    }

    // --- КОРПУС 4: СХІДНА ЦЕГЛЯНА КОТЕЛЬНЯ З КОТЛАМИ ТА ХІМІЧНИМИ РЕЗЕРВУАРАМИ (X = +21, Z = -35) ---
    private static void BuildEastBoilerHouseAndChemicalSilos(Transform parent, Vector3 center)
    {
        GameObject depot = new GameObject("East_Enterable_BoilerHouse_And_Silos");
        depot.transform.SetParent(parent);

        // Відкрита цегляна котельня з двома паровими котлами всередині
        Vector3 bCenter = center + new Vector3(-2.5f, 0f, 0f);
        float bw = 11f;
        float bd = 13f;
        float bh = 7.0f;

        CreateBox("Boiler_Floor", depot.transform, bCenter + new Vector3(0f, 0.1f, 0f), new Vector3(bw, 0.2f, bd), _matConcreteDark);
        CreateBox("Boiler_Roof", depot.transform, bCenter + new Vector3(0f, bh + 0.2f, 0f), new Vector3(bw + 0.5f, 0.4f, bd + 0.5f), _matConcreteDark);
        CreateBox("Boiler_Wall_East", depot.transform, bCenter + new Vector3(bw / 2f, bh / 2f, 0f), new Vector3(0.4f, bh, bd), _matBrickWall);
        CreateBox("Boiler_Wall_South", depot.transform, bCenter + new Vector3(0f, bh / 2f, -bd / 2f), new Vector3(bw, bh, 0.4f), _matBrickWall);
        CreateBox("Boiler_Wall_North", depot.transform, bCenter + new Vector3(0f, bh / 2f, bd / 2f), new Vector3(bw, bh, 0.4f), _matBrickWall);

        // Західна стіна котельні з широким входом із двору
        CreateBox("Boiler_Wall_West_L", depot.transform, bCenter + new Vector3(-bw / 2f, bh / 2f, -4.2f), new Vector3(0.4f, bh, 4.6f), _matBrickWall);
        CreateBox("Boiler_Wall_West_R", depot.transform, bCenter + new Vector3(-bw / 2f, bh / 2f, 4.2f), new Vector3(0.4f, bh, 4.6f), _matBrickWall);
        CreateBox("Boiler_Wall_West_Top", depot.transform, bCenter + new Vector3(-bw / 2f, bh - 1.2f, 0f), new Vector3(0.4f, 2.4f, 3.8f), _matBrickWall);

        // Парові котли високого тиску всередині котельні з червоним підсвічуванням топки!
        for (int k = -1; k <= 1; k += 2)
        {
            GameObject boilerDrum = CreateCylinder($"SteamBoiler_Drum_{k}", depot.transform, bCenter + new Vector3(1.2f, 1.8f, k * 3.2f), new Vector3(2.4f, 2.6f, 2.4f), _matRustedSteel);
            boilerDrum.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            CreateBox($"Boiler_Firebox_{k}", depot.transform, bCenter + new Vector3(-1.2f, 0.8f, k * 3.2f), new Vector3(0.6f, 1.2f, 1.4f), _matDarkIron);
        }
        CreateWallLamp("Boiler_FurnaceGlow", depot.transform, bCenter + new Vector3(-1.5f, 1.2f, 0f), _matLampRed, new Color(1f, 0.32f, 0.12f), 4.0f, 10f);

        // Висока димова труба котельні з сигнальним маяком
        CreateCylinder("Boiler_TallChimney", depot.transform, bCenter + new Vector3(2.5f, 10.5f, -3.5f), new Vector3(1.9f, 10.5f, 1.9f), _matBrickWall);
        CreateWallLamp("Chimney_RedAircraftWarning", depot.transform, bCenter + new Vector3(2.5f, 21.2f, -3.5f), _matLampRed, Color.red, 4.5f, 20f);

        // 4 хімічні циліндричні резервуари позаду котельні
        CreateCylinder("Silo_1", depot.transform, center + new Vector3(6.8f, 2.4f, -3.2f), new Vector3(3.8f, 2.4f, 3.8f), _matSiloWhite);
        CreateCylinder("Silo_2", depot.transform, center + new Vector3(6.8f, 2.4f, 3.2f), new Vector3(3.8f, 2.4f, 3.8f), _matSiloWhite);
        CreateCylinder("Silo_3", depot.transform, center + new Vector3(11.4f, 2.4f, -3.2f), new Vector3(3.8f, 2.4f, 3.8f), _matSiloWhite);

        GameObject collapsedSilo = CreateCylinder("Silo_4_Collapsed", depot.transform, center + new Vector3(11.4f, 1.6f, 3.2f), new Vector3(3.6f, 2.4f, 3.6f), _matRustedSteel);
        collapsedSilo.transform.rotation = Quaternion.Euler(28f, 15f, 65f);
    }

    // --- КОРПУС 5: ГОЛОВНИЙ НАЗЕМНИЙ ВЕСТИБЮЛЬ НДІ «СИНХРОНІЗАЦІЯ» З КОЛОНАДОЮ ТА ХОЛОМ (Z = +8.5) ---
    private static void BuildMainOvergroundInstituteLobbyWithInterior(Transform parent, Vector3 center)
    {
        GameObject hq = new GameObject("North_Enterable_Institute_HQ_Lobby");
        hq.transform.SetParent(parent);

        float w = 36f;
        float d = 14f;
        float h = 14f;

        // Підлога наземного вестибюля (вона ж є дахом над частиною підземного бункера!)
        CreateBox("HQ_LobbyFloor", hq.transform, center + new Vector3(0f, 0.15f, 0f), new Vector3(w, 0.3f, d), _matCheckerTile);
        CreateBox("HQ_UpperMass_Floors2To4", hq.transform, center + new Vector3(0f, 9.2f, 0f), new Vector3(w, 9.6f, d), _matConcreteLight);
        CreateBox("HQ_RoofCrown", hq.transform, center + new Vector3(0f, h + 0.45f, 0f), new Vector3(w + 1.2f, 0.9f, d + 1.2f), _matConcreteDark);

        // Стіни 1-го поверху вестибюля (вхід у вестибюль з боків від котловану бункера по сходах!)
        CreateBox("HQ_Lobby_BackNorth", hq.transform, center + new Vector3(0f, 2.2f, d / 2f), new Vector3(w, 4.4f, 0.5f), _matWallUpper);
        CreateBox("HQ_Lobby_WestWall", hq.transform, center + new Vector3(-w / 2f, 2.2f, 0f), new Vector3(0.5f, 4.4f, d), _matWallUpper);
        CreateBox("HQ_Lobby_EastWall", hq.transform, center + new Vector3(w / 2f, 2.2f, 0f), new Vector3(0.5f, 4.4f, d), _matWallUpper);

        // Колонада вздовж фасаду та панорамне оглядове скло з вестибюля вниз на спуск у бункер!
        for (float x = -15f; x <= 15f; x += 5.0f)
        {
            CreateBox($"HQ_MonumentalPillar_{x}", hq.transform, center + new Vector3(x, h / 2f, -d / 2f - 0.25f), new Vector3(0.95f, h, 0.95f), _matConcreteDark);
        }
        CreateBox("HQ_Lobby_GlassOverlook", hq.transform, center + new Vector3(0f, 2.2f, -d / 2f), new Vector3(12f, 3.8f, 0.1f), _matGlassBroken);

        // Стійка реєстрації (Reception), турнікети, дошка пошани та завалені сходи на 2-й поверх усередині вестибюля
        CreateBox("HQ_Reception_Desk", hq.transform, center + new Vector3(0f, 0.6f, 2.5f), new Vector3(6.5f, 1.1f, 1.2f), _matOldWood);
        CreateBox("HQ_HonorBoard_Frame", hq.transform, center + new Vector3(-9.5f, 2.1f, 6.6f), new Vector3(5.5f, 2.2f, 0.18f), _matCargoRed);
        CreateWallLamp("HQ_Lobby_Chandelier_L", hq.transform, center + new Vector3(-8f, 4.1f, 0f), _matLampWarm, new Color(1f, 0.82f, 0.5f), 4.0f, 14f);
        CreateWallLamp("HQ_Lobby_Chandelier_R", hq.transform, center + new Vector3(8f, 4.1f, 0f), _matLampWarm, new Color(1f, 0.82f, 0.5f), 4.0f, 14f);

        // Монументальний радянський барельєф з атомом на фасаді над бункером
        CreateBox("HQ_Relief_Backdrop", hq.transform, center + new Vector3(0f, 6.8f, -d / 2f - 0.45f), new Vector3(14f, 3.6f, 0.35f), _matConcreteDark);
        CreateCylinder("HQ_Relief_AtomRing", hq.transform, center + new Vector3(0f, 6.8f, -d / 2f - 0.68f), new Vector3(2.8f, 0.12f, 2.8f), _matCopperPipe).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static void BuildCourtyardPipeBridgesAndDebris(Transform parent)
    {
        GameObject pipes = new GameObject("Courtyard_Overhead_Pipes_And_Debris");
        pipes.transform.SetParent(parent);

        CreateBox("PipeBridge_Truss", pipes.transform, new Vector3(0f, 5.2f, -28f), new Vector3(28f, 0.65f, 0.85f), _matDarkIron);
        GameObject mainPipe = CreateCylinder("PipeBridge_Tube1", pipes.transform, new Vector3(0f, 5.75f, -27.7f), new Vector3(0.55f, 14f, 0.55f), _matCopperPipe);
        mainPipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        GameObject secondPipe = CreateCylinder("PipeBridge_Tube2", pipes.transform, new Vector3(0f, 5.75f, -28.3f), new Vector3(0.45f, 14f, 0.45f), _matRustedSteel);
        secondPipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        CreateBox("PipeSupport_L", pipes.transform, new Vector3(-8.2f, 2.6f, -28f), new Vector3(0.55f, 5.2f, 0.85f), _matConcreteDark);
        CreateBox("PipeSupport_R", pipes.transform, new Vector3(8.2f, 2.6f, -28f), new Vector3(0.55f, 5.2f, 0.85f), _matConcreteDark);

        BuildStreetLightPost("Lamp_Court_L1", pipes.transform, new Vector3(-6.5f, 0f, -44f), 90f, 0f);
        BuildStreetLightPost("Lamp_Court_R1", pipes.transform, new Vector3(6.5f, 0f, -44f), -90f, 6f);
        BuildStreetLightPost("Lamp_Court_L2", pipes.transform, new Vector3(-6.5f, 0f, -25f), 90f, -4f);
        BuildStreetLightPost("Lamp_Court_R2", pipes.transform, new Vector3(6.5f, 0f, -25f), -90f, 0f);
    }

    // =========================================================================================
    // РОЗДІЛ 3: СПУСК У БУНКЕР З ТРІЩИНАМИ, ТРУБАМИ, КАЛЮЖАМИ ТА ГЕРМОПОРТАЛОМ (Y: 0 -> -4.5m)
    // =========================================================================================
    private static void BuildUltraDetailedSunkenBunkerRamp(Transform parent)
    {
        float uy = UndergroundY;

        for (int i = 0; i < 6; i++)
        {
            float stepY = -0.08f - i * 0.15f;
            float stepZ = -20.8f + i * 0.48f;
            CreateBox($"Bunker_Step_{i}", parent, new Vector3(0f, stepY - 0.2f, stepZ), new Vector3(10.6f, 0.4f, 0.52f), _matConcreteDamaged);
            CreateBox($"Bunker_StepNose_{i}", parent, new Vector3(0f, stepY + 0.01f, stepZ - 0.24f), new Vector3(10.6f, 0.03f, 0.06f), _matRustedSteel);

            if (i % 2 == 0)
            {
                CreateBox($"Step_MossPatch_{i}", parent, new Vector3((i % 4 == 0 ? -4.2f : 4.1f), stepY + 0.015f, stepZ), new Vector3(1.8f, 0.02f, 0.4f), _matMoss);
            }
        }

        Vector3 rampStart = new Vector3(0f, -0.9f, -18.2f);
        Vector3 rampEnd = new Vector3(0f, uy, -3.2f);
        Vector3 rampCenter = (rampStart + rampEnd) * 0.5f;
        float rampLen = Vector3.Distance(rampStart, rampEnd);
        float rampAng = Mathf.Atan2(rampStart.y - rampEnd.y, rampEnd.z - rampStart.z) * Mathf.Rad2Deg;

        GameObject rampSlab = CreateBox("Ramp_SlopedConcreteSlab", parent, rampCenter + new Vector3(0f, -0.25f, 0f), new Vector3(10.6f, 0.5f, rampLen + 0.5f), _matConcreteDamaged);
        rampSlab.transform.rotation = Quaternion.Euler(rampAng, 0f, 0f);

        for (int r = 1; r < 14; r++)
        {
            Vector3 ribPos = Vector3.Lerp(rampStart, rampEnd, r / 14f);
            GameObject rib = CreateBox($"Ramp_GripRib_{r}", parent, ribPos + new Vector3(0f, 0.02f, 0f), new Vector3(9.6f, 0.04f, 0.14f), _matDarkIron);
            rib.transform.rotation = Quaternion.Euler(rampAng, 0f, 0f);
        }

        CreateBox("Bunker_BottomLanding", parent, new Vector3(0f, uy - 0.25f, -1.7f), new Vector3(10.8f, 0.5f, 3.8f), _matConcreteDark);
        CreateBox("Bunker_BottomDrainGrate", parent, new Vector3(0f, uy + 0.01f, -2.85f), new Vector3(9.2f, 0.03f, 0.55f), _matDarkIron);
        CreateBox("Bunker_BottomPuddle", parent, new Vector3(-1.2f, uy + 0.015f, -1.5f), new Vector3(4.5f, 0.01f, 2.2f), _matWaterPuddle);

        for (int side = -1; side <= 1; side += 2)
        {
            float sx = side * 5.5f;
            string sTag = side < 0 ? "L" : "R";

            CreateBox($"RetainingWall_{sTag}", parent, new Vector3(sx, uy / 2f + 0.2f, -10.5f), new Vector3(0.75f, Mathf.Abs(uy) + 0.8f, 21.2f), _matBunkerWall);
            CreateBox($"RetainingWall_HazardCurb_{sTag}", parent, new Vector3(sx - side * 0.42f, 0.12f, -10.5f), new Vector3(0.18f, 0.24f, 21.2f), _matHazardStripes);

            for (float z = -19f; z <= -1f; z += 3.0f)
            {
                CreateBox($"Buttress_{sTag}_{z}", parent, new Vector3(sx - side * 0.35f, uy / 2f + 0.1f, z), new Vector3(0.32f, Mathf.Abs(uy) + 0.5f, 0.5f), _matConcreteDark);
            }

            Vector3 pipeStart = new Vector3(sx - side * 0.48f, -0.4f, -19.5f);
            Vector3 pipeEnd = new Vector3(sx - side * 0.48f, uy + 1.8f, -0.5f);
            Vector3 pipeMid = (pipeStart + pipeEnd) * 0.5f;
            float pipeLen = Vector3.Distance(pipeStart, pipeEnd);
            float pipeAng = Mathf.Atan2(pipeStart.y - pipeEnd.y, pipeEnd.z - pipeStart.z) * Mathf.Rad2Deg;

            GameObject wallPipe = CreateBox($"Ramp_WallPipe_{sTag}", parent, pipeMid, new Vector3(0.18f, 0.18f, pipeLen), _matCopperPipe);
            wallPipe.transform.rotation = Quaternion.Euler(pipeAng, 0f, 0f);

            float railX = sx - side * 0.2f;
            Vector3 railTop = new Vector3(railX, 0.95f, -20.5f);
            Vector3 railBot = new Vector3(railX, uy + 1.05f, -3.2f);
            Vector3 railMid = (railTop + railBot) * 0.5f;
            float railLen = Vector3.Distance(railTop, railBot);
            float railAng = Mathf.Atan2(railTop.y - railBot.y, railBot.z - railTop.z) * Mathf.Rad2Deg;

            GameObject topBar = CreateBox($"Handrail_Top_{sTag}", parent, railMid, new Vector3(0.08f, 0.08f, railLen), _matRustedSteel);
            topBar.transform.rotation = Quaternion.Euler(railAng, 0f, 0f);

            GameObject midBar = CreateBox($"Handrail_Mid_{sTag}", parent, railMid + new Vector3(0f, -0.4f, 0f), new Vector3(0.05f, 0.05f, railLen), _matRustedSteel);
            midBar.transform.rotation = Quaternion.Euler(railAng, 0f, 0f);

            for (int p = 0; p <= 8; p++)
            {
                Vector3 postPos = Vector3.Lerp(railTop, railBot, p / 8f);
                CreateBox($"Handrail_Post_{sTag}_{p}", parent, postPos + new Vector3(0f, -0.45f, 0f), new Vector3(0.09f, 0.95f, 0.09f), _matDarkIron);
            }
        }

        CreateBox("Ramp_OverheadConcreteArch_1", parent, new Vector3(0f, -0.35f, -6.5f), new Vector3(11.6f, 0.75f, 1.1f), _matConcreteDark);
        CreateBox("Ramp_OverheadConcreteArch_2", parent, new Vector3(0f, -0.35f, -3.2f), new Vector3(11.6f, 0.75f, 1.1f), _matConcreteDark);

        CreateBox("Portal_Headwall", parent, new Vector3(0f, -0.7f, 0f), new Vector3(11.8f, 3.0f, 0.95f), _matBunkerWall);
        CreateBox("Portal_Pillar_L", parent, new Vector3(-3.65f, uy + 2.2f, -0.28f), new Vector3(3.3f, 4.4f, 0.85f), _matBunkerWall);
        CreateBox("Portal_Pillar_R", parent, new Vector3(3.65f, uy + 2.2f, -0.28f), new Vector3(3.3f, 4.4f, 0.85f), _matBunkerWall);
        CreateBox("Portal_HazardHeader", parent, new Vector3(0f, uy + 3.45f, -0.45f), new Vector3(4.4f, 0.35f, 0.95f), _matHazardStripes);

        CreateWallLamp("Bunker_SideLamp_L", parent, new Vector3(-2.65f, uy + 2.2f, -0.75f), _matLampAmber, new Color(1f, 0.65f, 0.2f), 4.5f, 12f);
        CreateWallLamp("Bunker_SideLamp_R", parent, new Vector3(2.65f, uy + 2.2f, -0.75f), _matLampAmber, new Color(1f, 0.65f, 0.2f), 4.5f, 12f);
        CreateWallLamp("Bunker_TopCenterBeacon", parent, new Vector3(0f, uy + 3.85f, -0.88f), _matLampAmber, new Color(1f, 0.7f, 0.2f), 5.5f, 15f);
    }

    // =========================================================================================
    // РОЗДІЛ 4: ПІДЗЕМНИЙ КОМПЛЕКС «СИНХРОНІЗАЦІЯ» НА ГЛИБИНІ Y = -4.5m
    // =========================================================================================
    private static void BuildUndergroundLaboratoryComplex(Transform parent)
    {
        float uy = UndergroundY;
        float wallH = 4.0f;

        CreateBox("Floor_Underground_Main", parent, new Vector3(0f, uy - 0.25f, 15f), new Vector3(32f, 0.5f, 32f), _matLabTileFloor);
        CreateCeilingWithGridAndDucts(parent, new Vector3(0f, uy + wallH + 0.15f, 15f), 32f, 32f);

        BuildTwoToneWall("Wall_Gate1_Left", parent, new Vector3(-8.5f, uy, 0f), new Vector3(13f, wallH, 0.6f));
        BuildTwoToneWall("Wall_Gate1_Right", parent, new Vector3(8.5f, uy, 0f), new Vector3(13f, wallH, 0.6f));
        CreateBox("Gate1_FrameTop", parent, new Vector3(0f, uy + 3.5f, 0f), new Vector3(4.2f, 1.0f, 0.7f), _matDarkIron);

        BuildTwoToneWall("Wall_Outer_Left", parent, new Vector3(-15f, uy, 15f), new Vector3(0.5f, wallH, 30f));
        BuildTwoToneWall("Wall_Outer_Right", parent, new Vector3(15f, uy, 15f), new Vector3(0.5f, wallH, 30f));
        BuildTwoToneWall("Wall_Archive_Back", parent, new Vector3(0f, uy, 30f), new Vector3(30f, wallH, 0.5f));

        BuildObservationWall("Wall_RightControl_Glass", parent, new Vector3(4.5f, uy, 7f), 14f, wallH);
        BuildControlRoomInterior(parent, new Vector3(9.8f, uy, 7f), uy);

        BuildTwoToneWall("Wall_LeftLab_A", parent, new Vector3(-4.5f, uy, 3f), new Vector3(0.4f, wallH, 6f));
        BuildTwoToneWall("Wall_LeftLab_B", parent, new Vector3(-4.5f, uy, 11.5f), new Vector3(0.4f, wallH, 5f));
        BuildCurvedTvChairRoom(parent, new Vector3(-10.2f, uy, 7f));

        BuildTwoToneWall("Wall_Gate2_Left", parent, new Vector3(-8.5f, uy, 14f), new Vector3(13f, wallH, 0.5f));
        BuildTwoToneWall("Wall_Gate2_Right", parent, new Vector3(8.5f, uy, 14f), new Vector3(13f, wallH, 0.5f));
        CreateBox("Gate2_FrameTop", parent, new Vector3(0f, uy + 3.5f, 14f), new Vector3(4.2f, 1.0f, 0.65f), _matDarkIron);

        for (int i = -1; i <= 1; i += 2)
        {
            BuildRetroMainframe($"Archive_Mainframe_Row1_{i}", parent, new Vector3(i * 5.5f, uy, 19f), Quaternion.identity);
            BuildRetroMainframe($"Archive_Mainframe_Row2_{i}", parent, new Vector3(i * 5.5f, uy, 24.5f), Quaternion.identity);
        }
        CreateBox("Archive_CenterPillar", parent, new Vector3(0f, uy + 2f, 22f), new Vector3(2.8f, 4f, 1.4f), _matDarkIron);

        CreateWallLamp("Hall_Fluorescent_1", parent, new Vector3(0f, uy + 3.8f, 7f), _matLampCool, new Color(0.75f, 0.92f, 0.88f), 3.5f, 12f);
        CreateWallLamp("Archive_RedLight", parent, new Vector3(0f, uy + 3.7f, 22f), _matLampRed, new Color(1f, 0.35f, 0.2f), 4f, 14f);
    }

    // =========================================================================================
    // ДЕТАЛІЗОВАНІ ДОПОМІЖНІ ОБ'ЄКТИ (АВТОМОБІЛІ, РАДІАТОРИ, СТЕЛАЖІ, ЩИТКИ, КАМІННЯ)
    // =========================================================================================
    private static void BuildAbandonedSovietJeep(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject jeep = new GameObject(name);
        jeep.transform.SetParent(parent);
        jeep.transform.position = pos;
        jeep.transform.rotation = rot;

        CreateBox("UAZ_Body", jeep.transform, pos, new Vector3(1.85f, 0.95f, 3.8f), _matCargoGreen).transform.localPosition = new Vector3(0f, 0.85f, 0f);
        CreateBox("UAZ_CabinTop", jeep.transform, pos, new Vector3(1.75f, 0.75f, 2.3f), _matRetroBeige).transform.localPosition = new Vector3(0f, 1.65f, -0.45f);
        CreateBox("UAZ_Windshield", jeep.transform, pos, new Vector3(1.65f, 0.55f, 0.08f), _matGlassBroken).transform.localPosition = new Vector3(0f, 1.6f, 0.72f);

        float[] wx = { -0.92f, 0.92f };
        float[] wz = { -1.25f, 1.25f };
        foreach (float x in wx)
        {
            foreach (float z in wz)
            {
                GameObject w = CreateCylinder($"Wheel_{x}_{z}", jeep.transform, pos, new Vector3(0.78f, 0.18f, 0.78f), _matDarkIron);
                w.transform.localPosition = new Vector3(x, 0.39f, z);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        // Одна вціліла фара УАЗа світить прямо в бік стежки до пролому в паркані!
        CreateWallLamp("UAZ_Headlight_L", jeep.transform, pos, _matLampAmber, new Color(1f, 0.85f, 0.45f), 3.8f, 14f).transform.localPosition = new Vector3(-0.65f, 0.9f, 1.95f);
    }

    private static void BuildAbandonedSovietTruck(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject truck = new GameObject(name);
        truck.transform.SetParent(parent);
        truck.transform.position = pos;
        truck.transform.rotation = rot;

        CreateBox("Chassis_Frame", truck.transform, pos, new Vector3(2.2f, 0.35f, 6.2f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.65f, 0f);
        CreateBox("Cab_Lower", truck.transform, pos, new Vector3(2.15f, 1.1f, 2.1f), _matCargoGreen).transform.localPosition = new Vector3(0f, 1.35f, 1.95f);
        CreateBox("Cab_Roof", truck.transform, pos, new Vector3(2.0f, 0.85f, 1.4f), _matCargoGreen).transform.localPosition = new Vector3(0f, 2.25f, 1.65f);
        CreateBox("Cab_Windshield", truck.transform, pos, new Vector3(1.85f, 0.65f, 0.08f), _matGlassBroken).transform.localPosition = new Vector3(0f, 2.25f, 2.36f);
        CreateBox("Cargo_Bed", truck.transform, pos, new Vector3(2.3f, 2.1f, 3.8f), _matRetroBeige).transform.localPosition = new Vector3(0f, 1.85f, -1.1f);

        float[] wx = { -1.05f, 1.05f };
        float[] wz = { -2.0f, 1.9f };
        foreach (float x in wx)
        {
            foreach (float z in wz)
            {
                GameObject wheel = CreateCylinder($"Wheel_{x}_{z}", truck.transform, pos, new Vector3(0.95f, 0.22f, 0.95f), _matDarkIron);
                wheel.transform.localPosition = new Vector3(x, 0.48f, z);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }
    }

    private static void BuildWoodenTelegraphPole(string name, Transform parent, Vector3 pos, float tiltZ)
    {
        GameObject pole = new GameObject(name);
        pole.transform.SetParent(parent);
        pole.transform.position = pos;
        pole.transform.rotation = Quaternion.Euler(0f, 0f, tiltZ);

        CreateCylinder("WoodMast", pole.transform, pos, new Vector3(0.24f, 3.8f, 0.24f), _matTreeTrunk).transform.localPosition = new Vector3(0f, 3.8f, 0f);
        CreateBox("Crossbar", pole.transform, pos, new Vector3(2.2f, 0.14f, 0.14f), _matOldWood).transform.localPosition = new Vector3(0f, 7.1f, 0f);
    }

    private static void BuildMetalHedgehog(string name, Transform parent, Vector3 pos, float rotY)
    {
        GameObject hh = new GameObject(name);
        hh.transform.SetParent(parent);
        hh.transform.position = pos + new Vector3(0f, 0.65f, 0f);
        hh.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        CreateBox("Beam_1", hh.transform, hh.transform.position, new Vector3(0.16f, 1.8f, 0.16f), _matRustedSteel).transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        CreateBox("Beam_2", hh.transform, hh.transform.position, new Vector3(0.16f, 1.8f, 0.16f), _matRustedSteel).transform.localRotation = Quaternion.Euler(-45f, 0f, 0f);
        CreateBox("Beam_3", hh.transform, hh.transform.position, new Vector3(0.16f, 1.8f, 0.16f), _matRustedSteel).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static void BuildMossyBoulderCluster(string name, Transform parent, Vector3 pos, float scale)
    {
        GameObject b = new GameObject(name);
        b.transform.SetParent(parent);
        b.transform.position = pos;

        GameObject r1 = CreateBox("Rock_Main", b.transform, pos + new Vector3(0f, 0.55f * scale, 0f), new Vector3(2.2f * scale, 1.3f * scale, 1.8f * scale), _matConcreteDamaged);
        r1.transform.rotation = Quaternion.Euler(12f, 28f, -8f);
        GameObject r2 = CreateBox("Rock_MossCap", b.transform, pos + new Vector3(0.2f * scale, 1.15f * scale, 0f), new Vector3(1.6f * scale, 0.35f * scale, 1.4f * scale), _matMoss);
        r2.transform.rotation = Quaternion.Euler(12f, 28f, -8f);
    }

    private static void BuildMetalShelvingRack(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject rack = new GameObject(name);
        rack.transform.SetParent(parent);
        rack.transform.position = pos;
        rack.transform.rotation = rot;

        for (int s = 0; s < 4; s++)
        {
            CreateBox($"Shelf_{s}", rack.transform, pos, new Vector3(2.2f, 0.05f, 0.75f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.25f + s * 0.65f, 0f);
        }
        CreateBox("Post_FL", rack.transform, pos, new Vector3(0.06f, 2.3f, 0.06f), _matDarkIron).transform.localPosition = new Vector3(-1.05f, 1.15f, -0.34f);
        CreateBox("Post_FR", rack.transform, pos, new Vector3(0.06f, 2.3f, 0.06f), _matDarkIron).transform.localPosition = new Vector3(1.05f, 1.15f, -0.34f);
        CreateBox("Post_BL", rack.transform, pos, new Vector3(0.06f, 2.3f, 0.06f), _matDarkIron).transform.localPosition = new Vector3(-1.05f, 1.15f, 0.34f);
        CreateBox("Post_BR", rack.transform, pos, new Vector3(0.06f, 2.3f, 0.06f), _matDarkIron).transform.localPosition = new Vector3(1.05f, 1.15f, 0.34f);
    }

    private static void BuildCastIronRadiator(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject rad = new GameObject(name);
        rad.transform.SetParent(parent);
        rad.transform.position = pos;
        rad.transform.rotation = rot;

        for (int f = -4; f <= 4; f++)
        {
            CreateBox($"Fin_{f}", rad.transform, pos, new Vector3(0.08f, 0.65f, 0.18f), _matRustedSteel).transform.localPosition = new Vector3(f * 0.14f, 0f, 0f);
        }
    }

    private static void BuildWallFuseBox(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject fb = new GameObject(name);
        fb.transform.SetParent(parent);
        fb.transform.position = pos;
        fb.transform.rotation = rot;

        CreateBox("BoxBody", fb.transform, pos, new Vector3(0.55f, 0.75f, 0.18f), _matDarkIron).transform.localPosition = Vector3.zero;
        CreateBox("HazardLabel", fb.transform, pos, new Vector3(0.35f, 0.2f, 0.02f), _matHazardStripes).transform.localPosition = new Vector3(0f, 0.15f, 0.1f);
    }

    private static void BuildWarningSignpost(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject sign = new GameObject(name);
        sign.transform.SetParent(parent);
        sign.transform.position = pos;
        sign.transform.rotation = rot;

        CreateCylinder("Post_L", sign.transform, pos, new Vector3(0.1f, 1.2f, 0.1f), _matRustedSteel).transform.localPosition = new Vector3(-0.7f, 1.2f, 0f);
        CreateCylinder("Post_R", sign.transform, pos, new Vector3(0.1f, 1.2f, 0.1f), _matRustedSteel).transform.localPosition = new Vector3(0.7f, 1.2f, 0f);
        CreateBox("Board", sign.transform, pos, new Vector3(1.8f, 1.0f, 0.08f), _matHazardStripes).transform.localPosition = new Vector3(0f, 1.85f, 0f);
    }

    private static void BuildConcreteRoadblock(string name, Transform parent, Vector3 pos, float rotY)
    {
        GameObject rb = CreateBox(name, parent, pos + new Vector3(0f, 0.55f, 0f), new Vector3(2.4f, 1.1f, 0.65f), _matConcreteDamaged);
        rb.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        CreateBox("HazardStripe", rb.transform, Vector3.zero, Vector3.one, _matHazardStripes).transform.localScale = new Vector3(1.02f, 0.25f, 1.02f);
    }

    private static void BuildSegmentedPerimeterWall(string name, Transform parent, Vector3 start, Vector3 end, int segments)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);

        float totalLen = Vector3.Distance(start, end);
        float segLen = totalLen / segments;
        Quaternion rot = Quaternion.LookRotation((end - start).normalized, Vector3.up);

        for (int i = 0; i < segments; i++)
        {
            Vector3 p1 = Vector3.Lerp(start, end, i / (float)segments);
            Vector3 p2 = Vector3.Lerp(start, end, (i + 1) / (float)segments);
            Vector3 mid = (p1 + p2) * 0.5f;

            GameObject post = CreateBox($"FencePost_{i}", wall.transform, p1 + new Vector3(0f, 2.0f, 0f), new Vector3(0.55f, 4.0f, 0.55f), _matConcreteDark);
            post.transform.rotation = rot;

            float tilt = Mathf.Sin(i * 2.3f) * 1.4f;
            GameObject panel = CreateBox($"FencePanel_{i}", wall.transform, mid + new Vector3(0f, 1.85f, 0f), new Vector3(0.32f, 3.7f, segLen - 0.1f), _matConcreteDamaged);
            panel.transform.rotation = rot * Quaternion.Euler(0f, 0f, tilt);

            GameObject topBar = CreateBox($"BarbedWireTop_{i}", wall.transform, mid + new Vector3(0f, 3.95f, 0f), new Vector3(0.45f, 0.15f, segLen), _matRustedSteel);
            topBar.transform.rotation = rot;
        }
    }

    private static void BuildCornerWatchtower(string name, Transform parent, Vector3 pos, float rotY)
    {
        GameObject tw = new GameObject(name);
        tw.transform.SetParent(parent);
        tw.transform.position = pos;
        tw.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        CreateBox("Tower_Shaft", tw.transform, pos, new Vector3(2.8f, 6.5f, 2.8f), _matConcreteDark).transform.localPosition = new Vector3(0f, 3.25f, 0f);
        CreateBox("Tower_Cabin", tw.transform, pos, new Vector3(3.4f, 2.4f, 3.4f), _matConcreteLight).transform.localPosition = new Vector3(0f, 7.5f, 0f);
        CreateBox("Tower_Roof", tw.transform, pos, new Vector3(3.8f, 0.35f, 3.8f), _matDarkIron).transform.localPosition = new Vector3(0f, 8.85f, 0f);
    }

    private static void CreateWallWithWindow(string name, Transform parent, Vector3 bottomCenter, float width, float height, float winW, float winH, bool alongZ, Material wallMat, Material glassMat)
    {
        GameObject wGrp = new GameObject(name);
        wGrp.transform.SetParent(parent);

        float sillH = 1.1f;
        float topH = Mathf.Max(0.4f, height - sillH - winH);
        float sideW = Mathf.Max(0.4f, (width - winW) * 0.5f);

        if (!alongZ)
        {
            CreateBox("BottomWall", wGrp.transform, bottomCenter + new Vector3(0f, sillH * 0.5f, 0f), new Vector3(width, sillH, 0.35f), wallMat);
            CreateBox("TopWall", wGrp.transform, bottomCenter + new Vector3(0f, height - topH * 0.5f, 0f), new Vector3(width, topH, 0.35f), wallMat);
            CreateBox("SideL", wGrp.transform, bottomCenter + new Vector3(-width * 0.5f + sideW * 0.5f, sillH + winH * 0.5f, 0f), new Vector3(sideW, winH, 0.35f), wallMat);
            CreateBox("SideR", wGrp.transform, bottomCenter + new Vector3(width * 0.5f - sideW * 0.5f, sillH + winH * 0.5f, 0f), new Vector3(sideW, winH, 0.35f), wallMat);
            CreateBox("WindowGlass", wGrp.transform, bottomCenter + new Vector3(0f, sillH + winH * 0.5f, 0f), new Vector3(winW, winH, 0.06f), glassMat);
        }
        else
        {
            CreateBox("BottomWall", wGrp.transform, bottomCenter + new Vector3(0f, sillH * 0.5f, 0f), new Vector3(0.35f, sillH, width), wallMat);
            CreateBox("TopWall", wGrp.transform, bottomCenter + new Vector3(0f, height - topH * 0.5f, 0f), new Vector3(0.35f, topH, width), wallMat);
            CreateBox("SideF", wGrp.transform, bottomCenter + new Vector3(0f, sillH + winH * 0.5f, -width * 0.5f + sideW * 0.5f), new Vector3(0.35f, winH, sideW), wallMat);
            CreateBox("SideB", wGrp.transform, bottomCenter + new Vector3(0f, sillH + winH * 0.5f, width * 0.5f - sideW * 0.5f), new Vector3(0.35f, winH, sideW), wallMat);
            CreateBox("WindowGlass", wGrp.transform, bottomCenter + new Vector3(0f, sillH + winH * 0.5f, 0f), new Vector3(0.06f, winH, winW), glassMat);
        }
    }

    private static void CreateHillMound(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject h1 = CreateBox(name + "_Base", parent, pos + new Vector3(0f, scale.y * 0.25f, 0f), new Vector3(scale.x, scale.y * 0.5f, scale.z), mat);
        h1.transform.rotation = Quaternion.Euler(0f, 7f, 0f);
        GameObject h2 = CreateBox(name + "_Top", parent, pos + new Vector3(0f, scale.y * 0.6f, 0f), new Vector3(scale.x * 0.68f, scale.y * 0.45f, scale.z * 0.68f), mat);
        h2.transform.rotation = Quaternion.Euler(0f, -12f, 0f);
    }

    private static void BuildWildBushCluster(string name, Transform parent, Vector3 pos)
    {
        GameObject bush = new GameObject(name);
        bush.transform.SetParent(parent);

        GameObject b1 = CreateBox("Tuft_1", bush.transform, pos + new Vector3(0f, 0.35f, 0f), new Vector3(1.4f, 0.7f, 0.25f), _matPineNeedles);
        b1.transform.rotation = Quaternion.Euler(8f, 25f, 0f);
        GameObject b2 = CreateBox("Tuft_2", bush.transform, pos + new Vector3(0f, 0.35f, 0f), new Vector3(1.4f, 0.7f, 0.25f), _matBirchLeaves);
        b2.transform.rotation = Quaternion.Euler(-8f, 115f, 0f);
    }

    private static void BuildDetailedPineTree(string name, Transform parent, Vector3 pos, float scale)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent);
        tree.transform.position = pos;

        CreateCylinder("Trunk", tree.transform, pos + new Vector3(0f, 2.2f * scale, 0f), new Vector3(0.55f * scale, 2.2f * scale, 0.55f * scale), _matTreeTrunk);
        CreateCylinder("Crown_1", tree.transform, pos + new Vector3(0f, 4.6f * scale, 0f), new Vector3(4.0f * scale, 1.8f * scale, 4.0f * scale), _matPineNeedles);
        CreateCylinder("Crown_2", tree.transform, pos + new Vector3(0f, 7.2f * scale, 0f), new Vector3(3.0f * scale, 1.7f * scale, 3.0f * scale), _matPineNeedles);
        CreateCylinder("Crown_3", tree.transform, pos + new Vector3(0f, 9.5f * scale, 0f), new Vector3(1.9f * scale, 1.5f * scale, 1.9f * scale), _matPineNeedles);
        CreateCylinder("Crown_Top", tree.transform, pos + new Vector3(0f, 11.4f * scale, 0f), new Vector3(0.9f * scale, 1.1f * scale, 0.9f * scale), _matPineNeedles);
    }

    private static void BuildDetailedBirchTree(string name, Transform parent, Vector3 pos, float scale)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent);
        tree.transform.position = pos;

        CreateCylinder("BirchTrunk", tree.transform, pos + new Vector3(0f, 2.8f * scale, 0f), new Vector3(0.42f * scale, 2.8f * scale, 0.42f * scale), _matBirchTrunk);
        CreateBox("Foliage_Main", tree.transform, pos + new Vector3(0f, 6.2f * scale, 0f), new Vector3(3.4f * scale, 3.2f * scale, 3.4f * scale), _matBirchLeaves);
        CreateBox("Foliage_Side", tree.transform, pos + new Vector3(0.8f * scale, 5.2f * scale, 0.6f * scale), new Vector3(2.4f * scale, 2.2f * scale, 2.4f * scale), _matPineNeedles);
    }

    private static void BuildWoodenCrate(string name, Transform parent, Vector3 pos, Vector3 size, float rotY)
    {
        GameObject crate = CreateBox(name, parent, pos, size, _matCrackedWood);
        crate.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
    }

    private static GameObject BuildRustedBarrel(string name, Transform parent, Vector3 pos)
    {
        return CreateCylinder(name, parent, pos, new Vector3(0.65f, 0.55f, 0.65f), _matCargoRed);
    }

    private static void BuildStreetLightPost(string name, Transform parent, Vector3 basePos, float rotY, float tiltZ)
    {
        GameObject post = new GameObject(name);
        post.transform.SetParent(parent);
        post.transform.position = basePos;
        post.transform.rotation = Quaternion.Euler(0f, rotY, tiltZ);

        CreateCylinder("Pole", post.transform, basePos, new Vector3(0.18f, 3.2f, 0.18f), _matDarkIron).transform.localPosition = new Vector3(0f, 3.2f, 0f);
        CreateBox("Arm", post.transform, basePos, new Vector3(0.12f, 0.12f, 1.6f), _matDarkIron).transform.localPosition = new Vector3(0f, 6.3f, 0.8f);
        CreateWallLamp("StreetBulb", post.transform, basePos, _matLampAmber, new Color(1f, 0.72f, 0.35f), 2.5f, 16f).transform.localPosition = new Vector3(0f, 6.2f, 1.4f);
    }

    private static void BuildCurvedTvChairRoom(Transform parent, Vector3 center)
    {
        GameObject room = new GameObject("Room_Calibration_TV_Arc");
        room.transform.SetParent(parent);
        room.transform.position = center;

        int cols = 7;
        int rows = 4;
        float radius = 3.8f;
        for (int c = 0; c < cols; c++)
        {
            float t = c / (float)(cols - 1);
            float rad = Mathf.Lerp(-75f, 75f, t) * Mathf.Deg2Rad;
            Vector3 colPos = center + new Vector3(-Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            Quaternion lookRot = Quaternion.LookRotation((center - colPos).normalized, Vector3.up);

            for (int r = 0; r < rows; r++)
            {
                Vector3 tvPos = new Vector3(colPos.x, center.y + 0.55f + r * 0.82f, colPos.z);
                GameObject tvBody = CreateBox($"TV_Body_{c}_{r}", room.transform, tvPos, new Vector3(0.85f, 0.76f, 0.6f), _matDarkIron);
                tvBody.transform.rotation = lookRot;

                Material scr = ((c + r) % 5 == 0) ? _matCrtAmber : _matCrtGreen;
                GameObject tvScreen = CreateBox($"TV_Screen_{c}_{r}", tvBody.transform, Vector3.zero, Vector3.one, scr);
                tvScreen.transform.localPosition = new Vector3(0f, 0.04f, 0.31f);
                tvScreen.transform.localScale = new Vector3(0.82f, 0.72f, 0.04f);
            }
        }

        GameObject chairRoot = new GameObject("Subject_Calibration_Chair");
        chairRoot.transform.SetParent(room.transform);
        chairRoot.transform.position = center + new Vector3(0.2f, 0f, 0f);
        chairRoot.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        CreateCylinder("Chair_BaseDisk", chairRoot.transform, chairRoot.transform.position + new Vector3(0f, 0.06f, 0f), new Vector3(0.9f, 0.06f, 0.9f), _matChairWhite);
        CreateCylinder("Chair_Pedestal", chairRoot.transform, chairRoot.transform.position + new Vector3(0f, 0.35f, 0f), new Vector3(0.35f, 0.35f, 0.35f), _matDarkIron);
        CreateBox("Chair_Seat", chairRoot.transform, chairRoot.transform.position + new Vector3(0f, 0.65f, 0f), new Vector3(0.75f, 0.12f, 0.75f), _matChairWhite);
        CreateBox("Chair_Backrest", chairRoot.transform, chairRoot.transform.position + new Vector3(0f, 1.25f, -0.32f), new Vector3(0.72f, 1.15f, 0.16f), _matChairWhite);
        CreateWallLamp("Calibration_SpotLight", room.transform, center + new Vector3(0f, 3.6f, 0f), _matCrtGreen, new Color(0.55f, 0.85f, 0.6f), 5f, 9f);
    }

    private static void BuildControlRoomInterior(Transform parent, Vector3 center, float baseY)
    {
        GameObject room = new GameObject("Room_Control_And_Monitoring");
        room.transform.SetParent(parent);
        room.transform.position = center;

        CreateBox("LDesk_MainLong", room.transform, new Vector3(9.5f, baseY + 0.78f, 7.5f), new Vector3(1.1f, 0.08f, 4.6f), _matRetroBeige);
        CreateBox("LDesk_SideWing", room.transform, new Vector3(8.2f, baseY + 0.78f, 5.5f), new Vector3(1.8f, 0.08f, 1.1f), _matRetroBeige);
        CreateBox("ReelRecorder_Cabinet", room.transform, new Vector3(9.6f, baseY + 0.38f, 6.8f), new Vector3(0.5f, 0.72f, 0.65f), _matDarkIron);
        CreateWallLamp("DeskLamp_Bulb", room.transform, new Vector3(8.2f, baseY + 1.28f, 5.3f), _matLampWarm, new Color(1f, 0.95f, 0.75f), 3.0f, 5f);

        BuildRetroMainframe("Mainframe_Wall_1", room.transform, new Vector3(14.1f, baseY, 6.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildRetroMainframe("Mainframe_Wall_2", room.transform, new Vector3(14.1f, baseY, 9.5f), Quaternion.Euler(0f, -90f, 0f));

        for (int c = 0; c < 6; c++)
        {
            for (int r = 0; r < 3; r++)
            {
                Vector3 tvPos = new Vector3(6.8f + c * 1.15f, baseY + 1.5f + r * 0.75f, 13.55f);
                CreateBox($"CtrlTV_{c}_{r}", room.transform, tvPos, new Vector3(1.05f, 0.68f, 0.4f), _matDarkIron);
                Material scrMat = ((c + r) % 3 == 0) ? _matCrtStatic : _matCrtGreen;
                CreateBox($"CtrlTV_Scr_{c}_{r}", room.transform, tvPos + new Vector3(0f, 0f, -0.21f), new Vector3(0.92f, 0.56f, 0.02f), scrMat);
            }
        }
    }

    private static void BuildObservationWall(string name, Transform parent, Vector3 center, float lengthZ, float height)
    {
        GameObject wallGrp = new GameObject(name);
        wallGrp.transform.SetParent(parent);
        float by = center.y;

        BuildTwoToneWall(name + "_DoorSide", wallGrp.transform, new Vector3(center.x, by, 2.0f), new Vector3(0.4f, height, 4.0f));
        CreateBox(name + "_DoorHeader", wallGrp.transform, new Vector3(center.x, by + 3.4f, 4.8f), new Vector3(0.42f, 1.2f, 1.6f), _matWallUpper);
        CreateBox(name + "_WindowLowWall", wallGrp.transform, new Vector3(center.x, by + 0.6f, 9.8f), new Vector3(0.42f, 1.2f, 8.4f), _matWallLower);
        CreateBox(name + "_WindowTopWall", wallGrp.transform, new Vector3(center.x, by + 3.5f, 9.8f), new Vector3(0.42f, 1.0f, 8.4f), _matWallUpper);
        CreateBox(name + "_WindowSill", wallGrp.transform, new Vector3(center.x, by + 1.22f, 9.8f), new Vector3(0.52f, 0.08f, 8.4f), _matDarkIron);

        GameObject glass = CreateBox(name + "_GlassPane", wallGrp.transform, new Vector3(center.x, by + 2.1f, 9.8f), new Vector3(0.08f, 1.75f, 8.2f), _matGlassClear);
        glass.layer = 2;
    }

    private static void BuildRetroMainframe(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject mf = new GameObject(name);
        mf.transform.SetParent(parent);
        mf.transform.position = pos;
        mf.transform.rotation = rot;

        CreateBox("BaseCabinet", mf.transform, pos, new Vector3(2.4f, 0.9f, 0.9f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.45f, 0f);
        CreateBox("UpperTower", mf.transform, pos, new Vector3(2.4f, 1.5f, 0.65f), _matRetroBeige).transform.localPosition = new Vector3(0f, 1.65f, -0.12f);
        GameObject sloped = CreateBox("SlopedPanel", mf.transform, pos, new Vector3(2.35f, 0.6f, 0.5f), _matRetroBeige);
        sloped.transform.localPosition = new Vector3(0f, 1.05f, 0.18f);
        sloped.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
    }

    private static void BuildTwoToneWall(string name, Transform parent, Vector3 bottomCenter, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        float lowerH = 1.35f;
        float upperH = size.y - lowerH;
        CreateBox(name + "_Lower", wall.transform, bottomCenter + new Vector3(0f, lowerH / 2f, 0f), new Vector3(size.x, lowerH, size.z), _matWallLower);
        CreateBox(name + "_Upper", wall.transform, bottomCenter + new Vector3(0f, lowerH + upperH / 2f, 0f), new Vector3(size.x, upperH, size.z), _matWallUpper);
    }

    private static void CreateCeilingWithGridAndDucts(Transform parent, Vector3 center, float width, float length)
    {
        GameObject ceil = CreateBox("Ceiling_MainPlate", parent, center, new Vector3(width, 0.3f, length), _matCeilingGrid);
        for (float z = 2f; z <= 28f; z += 4f)
        {
            CreateBox($"Ceiling_Beam_Z_{z}", ceil.transform, new Vector3(0f, center.y - 0.18f, z), new Vector3(width, 0.08f, 0.12f), _matDarkIron);
        }
        CreateBox("HVAC_AirDuct_Main", ceil.transform, new Vector3(-2.1f, center.y - 0.45f, 14f), new Vector3(0.85f, 0.5f, 26f), _matRustedSteel);
    }

    // =========================================================================================
    // ГЕНЕРАТОРИ ТЕКСТУР ТА БАЗОВІ ПРИМІТИВИ
    // =========================================================================================
    private static Texture2D GenerateGrassTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float n1 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                float n2 = Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.4f;
                float v = Mathf.Clamp(0.55f + (n1 + n2 - 0.65f) * 0.45f + Random.Range(-0.08f, 0.08f), 0.25f, 0.95f);
                tex.SetPixel(x, y, new Color(v * 0.78f, v, v * 0.65f, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateConcreteBlocksTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float n1 = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                float n2 = Mathf.PerlinNoise(x * 0.22f, y * 0.22f) * 0.35f;
                float val = 0.68f + (n1 + n2 - 0.6f) * 0.28f;
                if ((x % 64) < 2 || (y % 64) < 2) val *= 0.62f;
                tex.SetPixel(x, y, new Color(val, val, val, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateDamagedConcreteTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float n1 = Mathf.PerlinNoise(x * 0.05f + 12f, y * 0.05f + 12f);
                float n2 = Mathf.PerlinNoise(x * 0.28f, y * 0.28f) * 0.4f;
                float val = 0.62f + (n1 + n2 - 0.65f) * 0.38f;
                if ((x % 64) < 3 || (y % 64) < 3) val *= 0.52f;
                if (Mathf.PerlinNoise(x * 0.14f, y * 0.02f) > 0.62f) val *= 0.72f;
                tex.SetPixel(x, y, new Color(val, val * 0.98f, val * 0.94f, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBrickTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        int brickW = 32;
        int brickH = 16;
        for (int y = 0; y < height; y++)
        {
            int offsetX = ((y / brickH) % 2) * (brickW / 2);
            for (int x = 0; x < width; x++)
            {
                bool mortar = ((x + offsetX) % brickW < 2) || (y % brickH < 2);
                float v = mortar ? 0.42f : (0.68f + Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.2f);
                tex.SetPixel(x, y, new Color(v, v * 0.88f, v * 0.82f, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateCheckerTileTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isDark = ((x / 32) + (y / 32)) % 2 == 0;
                bool seam = (x % 32 < 2) || (y % 32 < 2);
                float v = seam ? 0.2f : (isDark ? 0.42f : 0.78f);
                tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateHazardStripeTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool yellow = ((x + y) / 24) % 2 == 0;
                tex.SetPixel(x, y, yellow ? new Color(0.88f, 0.68f, 0.12f) : new Color(0.12f, 0.12f, 0.12f));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateWoodPlankTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float v = (y % 32 < 2) ? 0.35f : (0.58f + Mathf.PerlinNoise(x * 0.03f, y * 0.25f) * 0.28f);
                tex.SetPixel(x, y, new Color(v, v * 0.9f, v * 0.8f, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateNoisyTexture(int width, int height, float minVal, float maxVal)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float v = Mathf.Lerp(minVal, maxVal, Mathf.PerlinNoise(x * 0.12f, y * 0.12f)) + Random.Range(-0.03f, 0.03f);
                tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    private static GameObject CreateWallLamp(string name, Transform parent, Vector3 pos, Material emissiveMat, Color lightColor, float intensity, float range)
    {
        GameObject lamp = CreateBox(name, parent, pos, new Vector3(0.65f, 0.22f, 0.25f), emissiveMat);
        GameObject lightObj = new GameObject(name + "_PointLight");
        lightObj.transform.SetParent(lamp.transform);
        lightObj.transform.localPosition = new Vector3(0f, -0.25f, 0f);

        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor;
        l.intensity = intensity;
        l.range = range;
        return lamp;
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
            Undo.RecordObject(obj.transform, "Move Gameplay Object");
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

    private static Material CreateTexturedMat(string name, Texture2D tex, Color tint, float smoothness, Vector2 tiling)
    {
        Material m = CreateMat(name, tint, smoothness);
        m.mainTexture = tex;
        m.mainTextureScale = tiling;
        if (m.HasProperty("_BaseMap"))
        {
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tiling);
        }
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
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        m.renderQueue = 3000;
        return m;
    }
}
#endif