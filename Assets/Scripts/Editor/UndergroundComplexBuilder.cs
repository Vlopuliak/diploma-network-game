// =================================================================================================
// ПРОЄКТ «КОПІЯ» (THE COPY / PROJECT "S") — ГЕНЕРАТОР ПІДЗЕМНОГО КОМПЛЕКСУ «СИНХРОНІЗАЦІЯ»
// Версія: 2.0 (Unity 6 URP + Netcode for GameObjects + Акустичні матеріали для VoIP та ШІ)
//
// Що будує цей скрипт:
//   0. Горизонтальний двостулковий гермолюк ракетного типу над спуском у бункер (Y = 0.0m)
//   1. Зона 1:  Санпропускник, деконтамінація та КПП підземного входу (Concrete + Tile + Glass)
//   2. Зона 2:  Дворівневий Центральний Атріум «Зал Синхронізації» (Concrete + Metal + Glass)
//   3. Зона 3:  Західне крило А — Лабораторія когнітивного калібрування та 28-ЕПТ дуга (Glass + Concrete)
//   4. Зона 4:  Західне крило Б — Акустична та безлунна лабораторія + 4 кабіни тесту матеріалів VoIP
//   5. Зона 5:  Східне крило А — Адміністрація, кабінет директора, зал нарад та кімната відпочинку (Wood)
//   6. Зона 6:  Східне крило Б — Біометричний медблок, МРТ-капсула та кріосховище (Tile + Glass)
//   7. Зона 7:  Північний Контур Безпеки — Броньований КПП, CCTV-зал та вентиляція (Metal + Concrete)
//   8. Зона 8:  Північно-Західне крило — Серверна матриць поведінки ЕС-ЕОМ (Metal + Concrete)
//   9. Зона 9:  Північно-Східне крило — Дворівневий машинний зал реактора й підстанції (Concrete + Metal)
//  10. Зона 10: Далека Північ — Центральне Ядро Архіву з 4 терміналами фінального вибору (GDD)
//  11. Зона 11: Технічний підповерх (Y = -9.0m) — Кабельні та дренажні тунелі обходу NPC
// =================================================================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using TheCopy.Shared;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public static class UndergroundComplexBuilder
{
    private const float MainY = -4.5f;
    private const float SubY = -9.0f;
    private const float WallH = 4.0f;
    private const float CeilingY = -0.35f;

    // --- ПРОЦЕДУРНІ МАТЕРІАЛИ URP ---
    private static Material _matConcreteWall;
    private static Material _matConcreteDark;
    private static Material _matConcreteDamaged;
    private static Material _matConcreteFloor;
    private static Material _matLabTileFloor;
    private static Material _matCeramicTileWall;
    private static Material _matCheckerTile;
    private static Material _matWoodOak;
    private static Material _matWoodDark;
    private static Material _matWoodPlywood;
    private static Material _matMetalBulkhead;
    private static Material _matRustedSteel;
    private static Material _matDarkIron;
    private static Material _matCopperPipe;
    private static Material _matBrassTrim;
    private static Material _matHazardStripes;
    private static Material _matAcousticFoam;
    private static Material _matAcousticFoamDark;
    private static Material _matGlassClear;
    private static Material _matGlassFrosted;
    private static Material _matRetroBeige;
    private static Material _matMedicalWhite;
    private static Material _matCarpetSoviet;
    private static Material _matFabricCurtain;
    private static Material _matLeatherDark;
    private static Material _matWaterPuddle;
    private static Material _matCeilingGrid;
    private static Material _matWallLowerGreen;
    private static Material _matWallUpperCream;
    private static Material _matBinderRed;
    private static Material _matBinderBlue;
    private static Material _matBinderGreen;
    private static Material _matBinderYellow;
    private static Material _matCrtGreen;
    private static Material _matCrtAmber;
    private static Material _matCrtBlue;
    private static Material _matCrtRed;
    private static Material _matCrtStatic;
    private static Material _matLampCool;
    private static Material _matLampWarm;
    private static Material _matLampAmber;
    private static Material _matLampRed;
    private static Material _matLampCyan;

    // =============================================================================================
    // ПУНКТИ МЕНЮ UNITY EDITOR
    // =============================================================================================
    [MenuItem("The Copy/6. Побудувати ПІДЗЕМНИЙ КОМПЛЕКС (11 зон + Ракетний люк + Акустика)")]
    public static void BuildEntireUndergroundSector()
    {
        InitializeMaterials();

        // Видаляємо старий маленький підземний блок і стару вертикальну заслінку, якщо вони є
        DestroyExistingByName("4_Underground_Sector1_Lab_Y_Minus4.5");
        DestroyExistingByName("Underground_Sector1_FullComplex");
        DestroyExistingByName("Outer_Ramp_BlastShutter");
        DestroyExistingByName("Silo_Ramp_BlastHatch_System");

        // 1. Будуємо горизонтальний двостулковий люк ракетного типу над спуском
        BuildRocketSiloBlastHatchSystem();

        // 2. Створюємо кореневий контейнер нового масштабного підземного комплексу
        GameObject root = new GameObject("Underground_Sector1_FullComplex");
        Undo.RegisterCreatedObjectUndo(root, "Build Full Underground Complex");

        BuildZone01_ReceptionAndDecontamination(CreateZoneGroup("01_Reception_Decontamination_TileGlass", root.transform).transform);
        BuildZone02_CentralAtriumRotunda(CreateZoneGroup("02_Central_Atrium_Synchronization_ConcreteMetal", root.transform).transform);
        BuildZone03_WestCalibrationLab(CreateZoneGroup("03_WestA_Cognitive_Calibration_Lab_GlassConcrete", root.transform).transform);
        BuildZone04_WestAcousticAnechoicLab(CreateZoneGroup("04_WestB_Acoustic_Anechoic_VoIP_Lab_FoamWood", root.transform).transform);
        BuildZone05_EastAdministrationWing(CreateZoneGroup("05_EastA_Administration_Director_Briefing_Wood", root.transform).transform);
        BuildZone06_EastBiometricMedicalBay(CreateZoneGroup("06_EastB_Biometric_Medical_Cryo_TileGlass", root.transform).transform);
        BuildZone07_NorthSecurityCheckpoint(CreateZoneGroup("07_North_Security_Checkpoint_MetalGlass", root.transform).transform);
        BuildZone08_NorthWestServerHall(CreateZoneGroup("08_NorthWest_Behavioral_Matrix_ServerHall_Metal", root.transform).transform);
        BuildZone09_NorthEastReactorSubstation(CreateZoneGroup("09_NorthEast_Deep_Reactor_Substation_ConcreteMetal", root.transform).transform);
        BuildZone10_FarNorthCentralCoreArchive(CreateZoneGroup("10_FarNorth_Central_Core_Archive_Climax", root.transform).transform);
        BuildZone11_SubLevelMaintenanceTunnels(CreateZoneGroup("11_SubLevel_Y_Minus9_Maintenance_Tunnels", root.transform).transform);
        BuildAllZonesEnvironmentalStorytelling(root.transform);

        // Оновлюємо позиції геймплейних об'єктів під нову геометрію
        RelocateCoreGameplayObjectsForNewUnderground();

        Selection.activeGameObject = root;
        Debug.Log("[UndergroundComplexBuilder] Підземний комплекс (11 зон + Ракетний люк + Акустичні поверхні) успішно побудовано!");
    }

    [MenuItem("The Copy/7. Встановити тільки Ракетний Люк над спуском (Bi-Parting Hatch)")]
    public static void BuildOnlyRocketSiloHatch()
    {
        InitializeMaterials();
        DestroyExistingByName("Outer_Ramp_BlastShutter");
        DestroyExistingByName("Silo_Ramp_BlastHatch_System");
        BuildRocketSiloBlastHatchSystem();
        Debug.Log("[UndergroundComplexBuilder] Горизонтальний двостулковий гермолюк ракетного типу встановлено!");
    }

    [MenuItem("The Copy/8. Спавн: Вхід у підземний комплекс (Санпропускник)")]
    public static void SpawnAtUndergroundReception()
    {
        PlaceIfFound("SpawnPoint_1", new Vector3(-2.0f, MainY + 1.05f, 3.5f), Quaternion.identity);
        PlaceIfFound("SpawnPoint_2", new Vector3(2.0f, MainY + 1.05f, 3.5f), Quaternion.identity);
    }

    [MenuItem("The Copy/9. Спавн: Акустична лабораторія (Тест матеріалів VoIP)")]
    public static void SpawnAtAcousticTestLab()
    {
        PlaceIfFound("SpawnPoint_1", new Vector3(-28.0f, MainY + 1.05f, 54.0f), Quaternion.Euler(0f, -90f, 0f));
        PlaceIfFound("SpawnPoint_2", new Vector3(-24.0f, MainY + 1.05f, 54.0f), Quaternion.Euler(0f, -90f, 0f));
    }

    [MenuItem("The Copy/10. Спавн: Центральний Атріум «Синхронізація»")]
    public static void SpawnAtCentralAtrium()
    {
        PlaceIfFound("SpawnPoint_1", new Vector3(-2.2f, MainY + 1.05f, 28.0f), Quaternion.identity);
        PlaceIfFound("SpawnPoint_2", new Vector3(2.2f, MainY + 1.05f, 28.0f), Quaternion.identity);
    }

    private static void RelocateCoreGameplayObjectsForNewUnderground()
    {
        PlaceIfFound("Airlock_Door", new Vector3(0f, MainY + 1.6f, 0f), Quaternion.identity);
        PlaceIfFound("Interactive_Terminal", new Vector3(2.5f, MainY + 1.1f, -1.15f), Quaternion.identity);
        PlaceIfFound("Secret_Note_1987", new Vector3(24.5f, MainY + 0.86f, 28.0f), Quaternion.Euler(0f, -90f, 0f));
        PlaceIfFound("Keypad_Terminal", new Vector3(3.2f, MainY + 1.4f, 52.5f), Quaternion.identity);
        PlaceIfFound("Chess_Table", new Vector3(39.0f, MainY + 0.4f, 23.0f), Quaternion.identity);
        PlaceIfFound("Seat_Player1", new Vector3(39.0f, MainY + 0.4f, 21.9f), Quaternion.identity);
        PlaceIfFound("Seat_Player2", new Vector3(39.0f, MainY + 0.4f, 24.1f), Quaternion.Euler(0f, 180f, 0f));
        PlaceIfFound("Sector_TriggerZone", new Vector3(0f, MainY + 1.5f, 58.0f), Quaternion.identity);
        PlaceIfFound("Dummy_NPC", new Vector3(-28f, MainY + 1.0f, 88.0f), Quaternion.Euler(0f, 180f, 0f));
    }


    // =============================================================================================
    // 0. ГОРИЗОНТАЛЬНИЙ ДВОСТУЛКОВИЙ ГЕРМОЛЮК РАКЕТНОГО ТИПУ НАД СПУСКОМ У БУНКЕР (Y = 0.0m)
    // =============================================================================================
    private static void BuildRocketSiloBlastHatchSystem()
    {
        GameObject hatchRoot = new GameObject("Silo_Ramp_BlastHatch_System");
        Undo.RegisterCreatedObjectUndo(hatchRoot, "Create Rocket Silo Blast Hatch");
        hatchRoot.transform.position = new Vector3(0f, 0f, -10.8f);

        // Додаємо мережеву синхронізацію та контролер люка
        hatchRoot.AddComponent<NetworkObject>();
        NetworkSiloBlastHatch hatchCtrl = hatchRoot.AddComponent<NetworkSiloBlastHatch>();

        // 1. ЗОВНІШНЯ БЕТОННА РАМА-КОМИН ТА ПІДЗЕМНІ КИШЕНІ, КУДИ ЗАХОДЯТЬ СТУЛКИ ВЛІВО І ВПРАВО
        GameObject frameGrp = CreateZoneGroup("Hatch_Concrete_Frame_And_Pockets", hatchRoot.transform);

        // Південна і північна перемички рами
        CreateAcousticBox("Frame_Rim_South", frameGrp.transform, new Vector3(0f, 0.08f, -21.2f), new Vector3(12.8f, 0.24f, 0.9f), _matHazardStripes, AcousticMaterialType.Concrete);
        CreateAcousticBox("Frame_Rim_North", frameGrp.transform, new Vector3(0f, 0.08f, -0.3f), new Vector3(12.8f, 0.24f, 0.9f), _matHazardStripes, AcousticMaterialType.Concrete);

        // Ліва і права напрямні рейки + підземні сталеві кожухи-кишені під рівнем землі двору
        for (int side = -1; side <= 1; side += 2)
        {
            string tag = side < 0 ? "Left" : "Right";
            float railX = side * 5.85f;
            float pocketX = side * 8.6f;

            CreateAcousticBox($"GuideRail_{tag}_Top", frameGrp.transform, new Vector3(railX, 0.14f, -10.8f), new Vector3(0.55f, 0.18f, 21.4f), _matDarkIron, AcousticMaterialType.Metal);
            CreateAcousticBox($"GuideRail_{tag}_Hazard", frameGrp.transform, new Vector3(railX + side * 0.35f, 0.09f, -10.8f), new Vector3(0.35f, 0.12f, 21.4f), _matHazardStripes, AcousticMaterialType.Concrete);

            // Верхня бетонна кришка підземної кишені (щоб стулка красиво заїжджала всередину під бетон!)
            CreateAcousticBox($"Pocket_RoofCover_{tag}", frameGrp.transform, new Vector3(pocketX, 0.09f, -10.8f), new Vector3(5.8f, 0.14f, 21.6f), _matConcreteDamaged, AcousticMaterialType.Concrete);
            CreateAcousticBox($"Pocket_BackStop_{tag}", frameGrp.transform, new Vector3(side * 11.45f, -0.35f, -10.8f), new Vector3(0.35f, 0.95f, 21.6f), _matConcreteDark, AcousticMaterialType.Concrete);

            // 4 гідравлічні штовхачі-поршні з кожного боку
            for (int p = 0; p < 4; p++)
            {
                float pz = -18.5f + p * 5.2f;
                CreateAcousticBox($"HydraulicHousing_{tag}_{p}", frameGrp.transform, new Vector3(side * 6.45f, 0.22f, pz), new Vector3(1.1f, 0.38f, 0.65f), _matRustedSteel, AcousticMaterialType.Metal);
                GameObject piston = CreateCylinder($"HydraulicPiston_{tag}_{p}", frameGrp.transform, new Vector3(side * 5.65f, 0.18f, pz), new Vector3(0.18f, 0.55f, 0.18f), _matBrassTrim);
                piston.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        // 2. ЛІВА ТА ПРАВА МАСИВНІ СТАЛЕВІ СТУЛКИ ЛЮКА (flush with ground Y = 0.0m)
        GameObject leftLeaf = BuildSiloHatchLeaf("BlastHatch_Leaf_Left", hatchRoot.transform, -1);
        GameObject rightLeaf = BuildSiloHatchLeaf("BlastHatch_Leaf_Right", hatchRoot.transform, 1);

        // 3. СИГНАЛЬНІ МАЯКИ ПО КУТАХ ЛЮКА ТА ПУЛЬТ РУЧНОГО СТАТУСУ
        List<Renderer> beacons = new List<Renderer>();
        Vector3[] beaconCoords =
        {
            new Vector3(-6.1f, 0.55f, -21.0f),
            new Vector3(6.1f, 0.55f, -21.0f),
            new Vector3(-6.1f, 0.55f, -0.6f),
            new Vector3(6.1f, 0.55f, -0.6f)
        };
        for (int b = 0; b < beaconCoords.Length; b++)
        {
            CreateBox($"HatchBeacon_Post_{b}", frameGrp.transform, beaconCoords[b] + new Vector3(0f, -0.25f, 0f), new Vector3(0.24f, 0.5f, 0.24f), _matDarkIron);
            GameObject lamp = CreateCylinder($"HatchBeacon_Lamp_{b}", frameGrp.transform, beaconCoords[b] + new Vector3(0f, 0.08f, 0f), new Vector3(0.22f, 0.14f, 0.22f), _matLampRed);
            beacons.Add(lamp.GetComponent<Renderer>());
        }

        // Пульт взаємодії [E] прямо перед входом у спуск (X = -4.2, Z = -22.0)
        GameObject console = CreateAcousticBox("Hatch_Surface_ControlPedestal", hatchRoot.transform, new Vector3(-4.2f, 0.68f, -22.1f), new Vector3(0.75f, 1.36f, 0.55f), _matDarkIron, AcousticMaterialType.Metal);
        CreateBox("Hatch_Pedestal_Screen", console.transform, console.transform.position + new Vector3(0f, 0.35f, -0.28f), new Vector3(0.55f, 0.35f, 0.05f), _matCrtAmber);
        BoxCollider triggerInteract = hatchRoot.AddComponent<BoxCollider>();
        triggerInteract.center = new Vector3(-4.2f, 0.7f, -11.3f); // локально біля пульта (-4.2, 0.7, -22.1 world)
        triggerInteract.size = new Vector3(1.1f, 1.5f, 0.9f);

        // Прив'язуємо поля NetworkSiloBlastHatch через SerializedObject + підключаємо до GRiD-87
        GameObject masterPcObj = GameObject.Find("GRiD87_Terminal_Master_EastLab");
        NetworkGridOsTerminal masterGridPc = masterPcObj != null ? masterPcObj.GetComponent<NetworkGridOsTerminal>() : null;

        SerializedObject soHatch = new SerializedObject(hatchCtrl);
        soHatch.FindProperty("leftLeaf").objectReferenceValue = leftLeaf.transform;
        soHatch.FindProperty("rightLeaf").objectReferenceValue = rightLeaf.transform;
        soHatch.FindProperty("requiredGridComputer").objectReferenceValue = masterGridPc;
        soHatch.FindProperty("unsealDropDepth").floatValue = 0.16f;
        soHatch.FindProperty("slideDistance").floatValue = 5.65f;

        SerializedProperty beaconsProp = soHatch.FindProperty("statusBeacons");
        beaconsProp.arraySize = beacons.Count;
        for (int i = 0; i < beacons.Count; i++)
        {
            beaconsProp.GetArrayElementAtIndex(i).objectReferenceValue = beacons[i];
        }
        soHatch.ApplyModifiedProperties();
    }

    private static GameObject BuildSiloHatchLeaf(string name, Transform parent, int side)
    {
        // side = -1 (ліва стулка), +1 (права стулка)
        GameObject leaf = new GameObject(name);
        leaf.transform.SetParent(parent);
        leaf.transform.localPosition = new Vector3(side * 2.76f, -0.02f, 0f);

        // Основна бронеплита, по якій можна ходити, коли люк зачинено
        GameObject slab = CreateAcousticBox("ArmorPlate_Main", leaf.transform, leaf.transform.position, new Vector3(5.52f, 0.28f, 20.6f), _matMetalBulkhead, AcousticMaterialType.Metal);
        slab.transform.localPosition = Vector3.zero;

        // Поперечні ребра жорсткості (як на кришках ракетних шахт)
        for (int r = -4; r <= 4; r++)
        {
            GameObject rib = CreateBox($"ReinforcementRib_{r}", leaf.transform, leaf.transform.position, new Vector3(5.35f, 0.08f, 0.36f), _matRustedSteel);
            rib.transform.localPosition = new Vector3(0f, 0.15f, r * 2.25f);
            Object.DestroyImmediate(rib.GetComponent<Collider>());
        }

        // Поздовжні балки та жовто-чорна смуга по центральному стику
        GameObject seamStrip = CreateBox("CenterSeam_HazardTrim", leaf.transform, leaf.transform.position, new Vector3(0.32f, 0.05f, 20.5f), _matHazardStripes);
        seamStrip.transform.localPosition = new Vector3(-side * 2.58f, 0.145f, 0f);
        Object.DestroyImmediate(seamStrip.GetComponent<Collider>());

        // Зубці герметичного замка вздовж центральної лінії
        for (int t = -6; t <= 6; t++)
        {
            if ((t % 2 == 0) == (side < 0))
            {
                GameObject tooth = CreateBox($"LockTooth_{t}", leaf.transform, leaf.transform.position, new Vector3(0.28f, 0.14f, 0.72f), _matDarkIron);
                tooth.transform.localPosition = new Vector3(-side * 2.78f, 0.08f, t * 1.45f);
                Object.DestroyImmediate(tooth.GetComponent<Collider>());
            }
        }

        // Масивні болти кріплення вздовж зовнішнього краю
        for (int b = -5; b <= 5; b++)
        {
            GameObject bolt = CreateCylinder($"ArmorBolt_{b}", leaf.transform, leaf.transform.position, new Vector3(0.24f, 0.06f, 0.24f), _matDarkIron);
            bolt.transform.localPosition = new Vector3(side * 2.25f, 0.16f, b * 1.85f);
            Object.DestroyImmediate(bolt.GetComponent<Collider>());
        }

        return leaf;
    }


    // =============================================================================================
    // ЗОНА 1: САНПРОПУСКНИК, ДЕКОНТАМІНАЦІЯ ТА КПП ПІДЗЕМНОГО ВХОДУ (X: -12..+12, Z: 0..22)
    // Матеріали: Бетон + Кахельна плитка + Оглядове скло
    // =============================================================================================
    private static void BuildZone01_ReceptionAndDecontamination(Transform parent)
    {
        float y = MainY;
        RegisterAcousticRoom(parent, "Зона 1: Санпропускник і КПП", new Vector3(0f, y + 2f, 11f), AcousticMaterialType.Tile, AudioReverbPreset.Bathroom, 14f);

        // Підлога та стеля
        CreateAcousticBox("Z01_Floor_Main", parent, new Vector3(0f, y - 0.2f, 11f), new Vector3(24.4f, 0.4f, 22.4f), _matCheckerTile, AcousticMaterialType.Tile);
        CreateAcousticBox("Z01_Ceiling_Main", parent, new Vector3(0f, CeilingY, 11f), new Vector3(24.4f, 0.3f, 22.4f), _matCeilingGrid, AcousticMaterialType.Concrete);

        // Південна стіна (Z = 0) з прорізом під Airlock_Door (X: -2.1 .. +2.1)
        BuildSegmentedWallX("Z01_Wall_South_L", parent, -12f, -2.1f, 0f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z01_Wall_South_R", parent, 2.1f, 12f, 0f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z01_AirlockHeader", parent, new Vector3(0f, y + 3.55f, 0f), new Vector3(4.6f, 0.9f, 0.65f), _matHazardStripes, AcousticMaterialType.Metal);

        // Північна стіна (Z = 22) з широким порталом в Атріум (X: -3.2 .. +3.2)
        BuildSegmentedWallX("Z01_Wall_North_L", parent, -12f, -3.2f, 22f, y, WallH, 0.5f, _matCeramicTileWall, AcousticMaterialType.Tile);
        BuildSegmentedWallX("Z01_Wall_North_R", parent, 3.2f, 12f, 22f, y, WallH, 0.5f, _matCeramicTileWall, AcousticMaterialType.Tile);
        CreateAcousticBox("Z01_PortalHeader_North", parent, new Vector3(0f, y + 3.5f, 22f), new Vector3(6.8f, 1.0f, 0.6f), _matDarkIron, AcousticMaterialType.Metal);

        // Бічні зовнішні стіни
        BuildSegmentedWallZ("Z01_Wall_West", parent, -12f, 0f, 22f, y, WallH, 0.5f, _matCeramicTileWall, AcousticMaterialType.Tile);
        BuildSegmentedWallZ("Z01_Wall_East", parent, 12f, 0f, 22f, y, WallH, 0.5f, _matCeramicTileWall, AcousticMaterialType.Tile);

        // --- АРКИ ДЕКОНТАМІНАЦІЇ ПО ЦЕНТРАЛЬНОМУ КОРИДОРУ (Z = 5.5, 9.5, 13.5) ---
        for (int a = 0; a < 3; a++)
        {
            float az = 5.5f + a * 4.0f;
            BuildDecontaminationArch($"Z01_DeconArch_{a}", parent, new Vector3(0f, y, az));
        }

        // --- СКЛЯНА БУДКА ЧЕРГОВОГО КПП СПРАВА (X: +5.0..+11.5, Z: 3.0..12.5) ---
        BuildGlassObservationPartition("Z01_GuardBooth_GlassWest", parent, new Vector3(5.0f, y, 7.75f), 9.5f, WallH, true);
        BuildSegmentedWallX("Z01_GuardBooth_WallSouth", parent, 5.0f, 12.0f, 3.0f, y, WallH, 0.25f, _matWallUpperCream, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z01_GuardBooth_WallNorth_L", parent, 5.0f, 7.6f, 12.5f, y, WallH, 0.25f, _matWallUpperCream, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z01_GuardBooth_WallNorth_R", parent, 9.6f, 12.0f, 12.5f, y, WallH, 0.25f, _matWallUpperCream, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z01_GuardBooth_DoorHeader", parent, new Vector3(8.6f, y + 3.4f, 12.5f), new Vector3(2.2f, 1.2f, 0.28f), _matWallUpperCream, AcousticMaterialType.Concrete);

        // Обладнання всередині будки чергового
        BuildRetroWorkstationDesk("Z01_GuardDesk_Main", parent, new Vector3(6.4f, y, 6.2f), Quaternion.Euler(0f, -90f, 0f), true);
        BuildRetroWorkstationDesk("Z01_GuardDesk_Side", parent, new Vector3(6.4f, y, 9.4f), Quaternion.Euler(0f, -90f, 0f), false);
        BuildMetalFilingCabinet("Z01_GuardFiles_1", parent, new Vector3(11.1f, y, 4.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildMetalFilingCabinet("Z01_GuardFiles_2", parent, new Vector3(11.1f, y, 5.7f), Quaternion.Euler(0f, -90f, 0f));
        BuildMetalFilingCabinet("Z01_GuardFiles_3", parent, new Vector3(11.1f, y, 6.9f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallFuseBox("Z01_GuardFuseBox", parent, new Vector3(11.65f, y + 1.65f, 10.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildOfficeChair("Z01_GuardChair_1", parent, new Vector3(7.6f, y, 6.2f), Quaternion.Euler(0f, -75f, 0f));
        BuildOfficeChair("Z01_GuardChair_2", parent, new Vector3(7.6f, y, 9.4f), Quaternion.Euler(0f, -110f, 0f));

        // --- ЛІВА СТОРОНА: ДУШОВІ ДЕКОНТАМІНАЦІЇ ТА ШАФИ ХІМЗАХИСТУ (X: -11.5..-5.2) ---
        for (int s = 0; s < 4; s++)
        {
            float sz = 3.5f + s * 3.2f;
            BuildDecontaminationShowerStall($"Z01_DeconShower_{s}", parent, new Vector3(-9.8f, y, sz));
        }

        // Ряд сталевих шафок для костюмів біозахисту
        for (int l = 0; l < 6; l++)
        {
            BuildMetalLocker($"Z01_HazmatLocker_{l}", parent, new Vector3(-11.2f, y, 15.2f + l * 1.05f), Quaternion.Euler(0f, 90f, 0f));
        }

        // Лавки для перевдягання
        BuildLockerBench("Z01_Bench_L1", parent, new Vector3(-7.6f, y, 17.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildLockerBench("Z01_Bench_R1", parent, new Vector3(8.5f, y, 16.5f), Quaternion.identity);

        // Турнікети радіаційного контролю перед входом в Атріум (Z = 18.2)
        for (int t = -2; t <= 2; t++)
        {
            if (t == 0) continue;
            BuildSecurityTurnstile($"Z01_Turnstile_{t}", parent, new Vector3(t * 1.75f, y, 18.2f));
        }

        // Настінний покажчик секторів комплексу «СИНХРОНІЗАЦІЯ»
        BuildDirectorySignBoard("Z01_DirectoryBoard", parent, new Vector3(4.8f, y + 2.1f, 21.65f), Quaternion.Euler(0f, 180f, 0f));
        BuildDirectorySignBoard("Z01_SafetyPoster", parent, new Vector3(-4.8f, y + 2.1f, 21.65f), Quaternion.Euler(0f, 180f, 0f));

        // Потолочні комунікації, кабельні лотки та люмінесцентні лампи
        BuildCeilingUtilityRun("Z01_CeilingPipes_L", parent, new Vector3(-3.8f, y + 3.65f, 11f), 20f, false);
        BuildCeilingUtilityRun("Z01_CeilingPipes_R", parent, new Vector3(3.8f, y + 3.65f, 11f), 20f, false);

        for (float lz = 4f; lz <= 19f; lz += 5f)
        {
            CreateCeilingLamp($"Z01_Lamp_C_{lz}", parent, new Vector3(0f, y + 3.88f, lz), _matLampCool, new Color(0.76f, 0.94f, 0.90f), 3.4f, 11f);
        }
        CreateCeilingLamp("Z01_GuardBooth_Lamp", parent, new Vector3(8.5f, y + 3.85f, 7.75f), _matLampWarm, new Color(1f, 0.84f, 0.55f), 2.8f, 9f);
        CreateCeilingLamp("Z01_ShowerArea_Lamp", parent, new Vector3(-8.5f, y + 3.85f, 9.5f), _matLampCool, new Color(0.72f, 0.95f, 0.92f), 2.8f, 10f);
    }


    // =============================================================================================
    // ЗОНА 2: ЦЕНТРАЛЬНИЙ ДВОРІВНЕВИЙ АТРІУМ «ЗАЛ СИНХРОНІЗАЦІЇ» (X: -16..+16, Z: 22..52)
    // Головний хаб комплексу з підземним котлованом (Y = -9.0m), 8 колонами та переходами в усі крила
    // =============================================================================================
    private static void BuildZone02_CentralAtriumRotunda(Transform parent)
    {
        float y = MainY;
        float sy = SubY;
        RegisterAcousticRoom(parent, "Зона 2: Центральний Атріум", new Vector3(0f, y + 2f, 37f), AcousticMaterialType.Concrete, AudioReverbPreset.Auditorium, 20f);

        // Підлога Атріуму по периметру навколо центрального оглядового котловану (X: -6..+6, Z: 31..43)
        CreateAcousticBox("Z02_Floor_SouthBand", parent, new Vector3(0f, y - 0.2f, 26.5f), new Vector3(32.4f, 0.4f, 9.2f), _matConcreteFloor, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z02_Floor_NorthBand", parent, new Vector3(0f, y - 0.2f, 47.5f), new Vector3(32.4f, 0.4f, 9.2f), _matConcreteFloor, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z02_Floor_WestBand", parent, new Vector3(-11.0f, y - 0.2f, 37.0f), new Vector3(10.4f, 0.4f, 12.4f), _matConcreteFloor, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z02_Floor_EastBand", parent, new Vector3(11.0f, y - 0.2f, 37.0f), new Vector3(10.4f, 0.4f, 12.4f), _matConcreteFloor, AcousticMaterialType.Concrete);

        // Дно центрального котловану на рівні SubY (-9.0m) та його бетонні стіни з проходами в підповерх
        CreateAcousticBox("Z02_PitFloor_SubY", parent, new Vector3(0f, sy - 0.2f, 37.0f), new Vector3(12.6f, 0.4f, 12.6f), _matLabTileFloor, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z02_PitWall_South", parent, -6.0f, 6.0f, 31.0f, sy, 4.5f, 0.4f, _matConcreteDark, AcousticMaterialType.Concrete);
        // Північна, західна та східна стіни котловану мають прорізи до технічних тунелів підповерху!
        BuildSegmentedWallX("Z02_PitWall_North_L", parent, -6.0f, -2.0f, 43.0f, sy, 4.5f, 0.4f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z02_PitWall_North_R", parent, 2.0f, 6.0f, 43.0f, sy, 4.5f, 0.4f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z02_PitWall_West_S", parent, -6.0f, 31.0f, 35.0f, sy, 4.5f, 0.4f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z02_PitWall_West_N", parent, -6.0f, 39.0f, 43.0f, sy, 4.5f, 0.4f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z02_PitWall_East_S", parent, 6.0f, 31.0f, 35.0f, sy, 4.5f, 0.4f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z02_PitWall_East_N", parent, 6.0f, 39.0f, 43.0f, sy, 4.5f, 0.4f, _matConcreteDark, AcousticMaterialType.Concrete);

        // Огородження по краю центрального котловану (із проходами на сходи з півдня)
        BuildSafetyRailingSegment("Z02_PitRail_North", parent, new Vector3(-6f, y, 43f), new Vector3(6f, y, 43f));
        BuildSafetyRailingSegment("Z02_PitRail_West", parent, new Vector3(-6f, y, 31f), new Vector3(-6f, y, 43f));
        BuildSafetyRailingSegment("Z02_PitRail_East", parent, new Vector3(6f, y, 31f), new Vector3(6f, y, 43f));
        BuildSafetyRailingSegment("Z02_PitRail_South_L", parent, new Vector3(-6f, y, 31f), new Vector3(-2.2f, y, 31f));
        BuildSafetyRailingSegment("Z02_PitRail_South_R", parent, new Vector3(2.2f, y, 31f), new Vector3(6f, y, 31f));

        // Промислові сходи вниз у котлован (Y = -4.5m -> -9.0m) уздовж південного боку котловану
        BuildIndustrialStaircase("Z02_Atrium_SubLevel_Stairs", parent, new Vector3(0f, y, 31.2f), new Vector3(0f, sy, 37.8f), 3.6f, 16);

        // Центральний інформаційний моноліт у котловані з 12 ЕПТ-моніторами на колоні
        BuildCentralStatusMonolith("Z02_Central_Monolith", parent, new Vector3(0f, sy, 39.6f), 7.8f);

        // Стеля Атріуму з кесонними ребрами та масивною мостовою балкою
        CreateAcousticBox("Z02_Ceiling_Main", parent, new Vector3(0f, CeilingY, 37f), new Vector3(32.4f, 0.35f, 30.4f), _matConcreteDark, AcousticMaterialType.Concrete);
        for (float bz = 25f; bz <= 49f; bz += 6f)
        {
            CreateBox($"Z02_CeilingBeam_{bz}", parent, new Vector3(0f, y + 3.72f, bz), new Vector3(31.8f, 0.42f, 0.55f), _matDarkIron);
        }

        // Зовнішні стіни Атріуму з 5 широкими порталами:
        // Південь (Z = 22) -> Зона 1 (X: -3.2..+3.2)
        BuildSegmentedWallX("Z02_Wall_South_FarL", parent, -16f, -12f, 22f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z02_Wall_South_FarR", parent, 12f, 16f, 22f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);

        // Північ (Z = 52) -> Зона 7 Контур Безпеки (X: -3.5..+3.5)
        BuildSegmentedWallX("Z02_Wall_North_L", parent, -16f, -3.5f, 52f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z02_Wall_North_R", parent, 3.5f, 16f, 52f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z02_NorthPortal_Header", parent, new Vector3(0f, y + 3.5f, 52f), new Vector3(7.4f, 1.0f, 0.65f), _matHazardStripes, AcousticMaterialType.Metal);

        // Західна стіна (X = -16) -> 2 портали: Зона 3 Калібрування (Z: 28..33) та Зона 4 Акустика (Z: 44.5..49.5)
        BuildSegmentedWallZ("Z02_Wall_West_S", parent, -16f, 22f, 28f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z02_Wall_West_Mid", parent, -16f, 33f, 44.5f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z02_Wall_West_N", parent, -16f, 49.5f, 52f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z02_WestPortalA_Header", parent, new Vector3(-16f, y + 3.5f, 30.5f), new Vector3(0.65f, 1.0f, 5.4f), _matDarkIron, AcousticMaterialType.Metal);
        CreateAcousticBox("Z02_WestPortalB_Header", parent, new Vector3(-16f, y + 3.5f, 47.0f), new Vector3(0.65f, 1.0f, 5.4f), _matDarkIron, AcousticMaterialType.Metal);

        // Східна стіна (X = +16) -> 2 портали: Зона 5 Адміністрація (Z: 28..33) та Зона 6 Медблок (Z: 44.5..49.5)
        BuildSegmentedWallZ("Z02_Wall_East_S", parent, 16f, 22f, 28f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z02_Wall_East_Mid", parent, 16f, 33f, 44.5f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z02_Wall_East_N", parent, 16f, 49.5f, 52f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z02_EastPortalA_Header", parent, new Vector3(16f, y + 3.5f, 30.5f), new Vector3(0.65f, 1.0f, 5.4f), _matWoodDark, AcousticMaterialType.Wood);
        CreateAcousticBox("Z02_EastPortalB_Header", parent, new Vector3(16f, y + 3.5f, 47.0f), new Vector3(0.65f, 1.0f, 5.4f), _matCeramicTileWall, AcousticMaterialType.Tile);

        // 8 монументальних ребристих колон навколо котловану
        Vector3[] pillarCoords =
        {
            new Vector3(-9.5f, y, 27.5f), new Vector3(9.5f, y, 27.5f),
            new Vector3(-9.5f, y, 37.0f), new Vector3(9.5f, y, 37.0f),
            new Vector3(-9.5f, y, 46.5f), new Vector3(9.5f, y, 46.5f),
            new Vector3(-4.5f, y, 25.0f), new Vector3(4.5f, y, 25.0f)
        };
        for (int i = 0; i < pillarCoords.Length; i++)
        {
            BuildRibbedConcretePillar($"Z02_AtriumPillar_{i}", parent, pillarCoords[i], WallH);
        }

        // Інтер'єр Атріуму: стійка адміністратора, бетонні клумби, лавки, термінали довідки
        BuildAtriumReceptionCounter("Z02_ReceptionCounter", parent, new Vector3(0f, y, 25.4f));
        BuildConcretePlanterWithTree("Z02_Planter_SW", parent, new Vector3(-13.2f, y, 25.2f));
        BuildConcretePlanterWithTree("Z02_Planter_SE", parent, new Vector3(13.2f, y, 25.2f));
        BuildConcretePlanterWithTree("Z02_Planter_NW", parent, new Vector3(-13.2f, y, 49.2f));
        BuildConcretePlanterWithTree("Z02_Planter_NE", parent, new Vector3(13.2f, y, 49.2f));

        BuildRetroTerminalKiosk("Z02_InfoKiosk_W", parent, new Vector3(-12.5f, y, 37.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildRetroTerminalKiosk("Z02_InfoKiosk_E", parent, new Vector3(12.5f, y, 37.0f), Quaternion.Euler(0f, -90f, 0f));

        BuildLockerBench("Z02_AtriumBench_W", parent, new Vector3(-14.2f, y, 39.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildLockerBench("Z02_AtriumBench_E", parent, new Vector3(14.2f, y, 39.0f), Quaternion.Euler(0f, -90f, 0f));

        // Освітлення Атріуму (теплі бурштинові акценти на колонах + холодне світло в котловані)
        CreateCeilingLamp("Z02_Light_CenterPit", parent, new Vector3(0f, y + 3.65f, 37.0f), _matLampAmber, new Color(1f, 0.72f, 0.34f), 5.5f, 18f);
        CreateCeilingLamp("Z02_Light_SW", parent, new Vector3(-11f, y + 3.7f, 28f), _matLampCool, new Color(0.76f, 0.90f, 0.88f), 3.4f, 13f);
        CreateCeilingLamp("Z02_Light_SE", parent, new Vector3(11f, y + 3.7f, 28f), _matLampCool, new Color(0.76f, 0.90f, 0.88f), 3.4f, 13f);
        CreateCeilingLamp("Z02_Light_NW", parent, new Vector3(-11f, y + 3.7f, 46f), _matLampCool, new Color(0.76f, 0.90f, 0.88f), 3.4f, 13f);
        CreateCeilingLamp("Z02_Light_NE", parent, new Vector3(11f, y + 3.7f, 46f), _matLampCool, new Color(0.76f, 0.90f, 0.88f), 3.4f, 13f);
        CreateCeilingLamp("Z02_SubPit_Lamp", parent, new Vector3(0f, sy + 3.8f, 35.5f), _matLampWarm, new Color(1f, 0.65f, 0.25f), 3.8f, 12f);
    }


    // =============================================================================================
    // ЗОНА 3: ЗАХІДНЕ КРИЛО А — ЛАБОРАТОРІЯ КОГНІТИВНОГО КАЛІБРУВАННЯ (X: -48..-16, Z: 16..44)
    // Матеріали: Бетон + Оглядове скло + Метал (Ідеально для кооперативу крізь скляну галерею!)
    // =============================================================================================
    private static void BuildZone03_WestCalibrationLab(Transform parent)
    {
        float y = MainY;
        RegisterAcousticRoom(parent, "Зона 3: Лабораторія калібрування", new Vector3(-32f, y + 2f, 30f), AcousticMaterialType.Glass, AudioReverbPreset.Room, 18f);

        // Підлога і стеля
        CreateAcousticBox("Z03_Floor_Main", parent, new Vector3(-32f, y - 0.2f, 30f), new Vector3(32.4f, 0.4f, 28.4f), _matLabTileFloor, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z03_Ceiling_Main", parent, new Vector3(-32f, CeilingY, 30f), new Vector3(32.4f, 0.3f, 28.4f), _matCeilingGrid, AcousticMaterialType.Concrete);

        // Зовнішні стіни крила
        BuildSegmentedWallX("Z03_Wall_South", parent, -48f, -16f, 16f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z03_Wall_West", parent, -48f, 16f, 44f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z03_Wall_East_S", parent, -16f, 16f, 22f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);

        // Північна стіна (Z = 44) з переходом у Зону 4 (Акустична лабораторія) на X: -26..-21
        BuildSegmentedWallX("Z03_Wall_North_W", parent, -48f, -26f, 44f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z03_Wall_North_E", parent, -21f, -16f, 44f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z03_NorthDoor_Header", parent, new Vector3(-23.5f, y + 3.5f, 44f), new Vector3(5.4f, 1.0f, 0.55f), _matDarkIron, AcousticMaterialType.Metal);

        // --- ВНУТРІШНЯ СКЛЯНА ГАЛЕРЕЯ СПОСТЕРЕЖЕННЯ (X = -32.0, Z: 16..44) ---
        // Відокремлює східну апаратну залу від західної камери калібрування «Копії» (прохід на Z: 38..42)
        BuildGlassObservationPartition("Z03_ObsGallery_Glass", parent, new Vector3(-32.0f, y, 27.0f), 22.0f, WallH, true);
        CreateAcousticBox("Z03_ObsGallery_DoorHeader", parent, new Vector3(-32.0f, y + 3.45f, 40.0f), new Vector3(0.42f, 1.1f, 4.4f), _matDarkIron, AcousticMaterialType.Metal);
        BuildSegmentedWallZ("Z03_ObsGallery_NorthStub", parent, -32.0f, 42.0f, 44.0f, y, WallH, 0.4f, _matConcreteWall, AcousticMaterialType.Concrete);

        // --- ЗАХІДНА КАМЕРА: ГІГАНТСЬКА ДУГА З 28 ЕПТ-ТЕЛЕВІЗОРІВ ТА КРІСЛО СУБ'ЄКТА (X = -40, Z = 28) ---
        BuildExpandedCurvedTvArcChamber("Z03_Curved_TV_Arc_28", parent, new Vector3(-40.5f, y, 28.0f));

        // Додаткова кімната проєкції та запису поведінки на півдні камери (X: -46..-34, Z: 17..21)
        BuildRetroMainframeCabinet("Z03_BehaviorRecorder_1", parent, new Vector3(-45.2f, y, 18.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildRetroMainframeCabinet("Z03_BehaviorRecorder_2", parent, new Vector3(-45.2f, y, 21.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildTapeReelStand("Z03_TapeStand_1", parent, new Vector3(-35.2f, y, 18.0f), Quaternion.identity);
        BuildTapeReelStand("Z03_TapeStand_2", parent, new Vector3(-37.2f, y, 18.0f), Quaternion.identity);
        BuildRollingLabCart("Z03_LabCart_EEG", parent, new Vector3(-37.8f, y, 31.2f), Quaternion.Euler(0f, 25f, 0f));
        BuildRollingLabCart("Z03_LabCart_Tapes", parent, new Vector3(-35.4f, y, 24.5f), Quaternion.Euler(0f, -15f, 0f));

        // --- СХІДНА АПАРАТНА ЗАЛА СПОСТЕРЕЖЕННЯ (X: -31.5..-16.5, Z: 16.5..43.5) ---
        // Довгий ряд пультів операторів уздовж оглядового скла
        for (int d = 0; d < 4; d++)
        {
            float dz = 19.5f + d * 4.5f;
            BuildOscilloscopeConsoleDesk($"Z03_OperatorConsole_{d}", parent, new Vector3(-29.8f, y, dz), Quaternion.Euler(0f, -90f, 0f));
            BuildOfficeChair($"Z03_OperatorChair_{d}", parent, new Vector3(-28.1f, y, dz), Quaternion.Euler(0f, -90f + d * 12f, 0f));
        }

        // Центральний стіл аналітики з кресленнями, папками та осцилографами
        BuildLongLabTable("Z03_AnalysisTable_Main", parent, new Vector3(-22.5f, y, 23.0f), Quaternion.identity, 5.8f);
        BuildLabWhiteboard("Z03_Whiteboard_Algorithms", parent, new Vector3(-22.5f, y, 16.45f), Quaternion.identity);
        BuildLabWhiteboard("Z03_Whiteboard_NeuralMap", parent, new Vector3(-16.45f, y, 20.0f), Quaternion.Euler(0f, -90f, 0f));

        // Сховище магнітних бобін уздовж північно-західного кута апаратної
        for (int r = 0; r < 3; r++)
        {
            BuildArchiveShelvingUnit($"Z03_TapeArchiveRack_{r}", parent, new Vector3(-29.5f + r * 3.2f, y, 42.6f), Quaternion.Euler(0f, 180f, 0f));
        }

        // Комунікації та освітлення
        BuildCeilingUtilityRun("Z03_HVAC_Main", parent, new Vector3(-24f, y + 3.65f, 30f), 26f, false);
        BuildCeilingUtilityRun("Z03_HVAC_Chamber", parent, new Vector3(-40f, y + 3.65f, 30f), 26f, false);

        CreateCeilingLamp("Z03_Lamp_ChamberCenter", parent, new Vector3(-40.5f, y + 3.85f, 28.0f), _matCrtGreen, new Color(0.45f, 0.92f, 0.58f), 4.6f, 14f);
        CreateCeilingLamp("Z03_Lamp_ControlSouth", parent, new Vector3(-23.5f, y + 3.85f, 22.0f), _matLampCool, new Color(0.78f, 0.92f, 0.88f), 3.4f, 12f);
        CreateCeilingLamp("Z03_Lamp_ControlNorth", parent, new Vector3(-23.5f, y + 3.85f, 36.0f), _matLampCool, new Color(0.78f, 0.92f, 0.88f), 3.4f, 12f);
    }


    // =============================================================================================
    // ЗОНА 4: ЗАХІДНЕ КРИЛО Б — АКУСТИЧНА ТА БЕЗЛУННА ЛАБОРАТОРІЯ (X: -48..-16, Z: 44..74)
    // СПЕЦІАЛЬНА ЗОНА ДЛЯ КУРСОВОЇ ТА ДИПЛОМА:
    //   - Безлунна камера (AcousticFoam) з 3D-клинами на стінах і стелі
    //   - 4 тестові кабіни з різними матеріалами стін (Бетон / Дерево / Метал / Поролон) для VoIP!
    //   - Звукорежисерська апаратна з мікшерним пультом та бобінними магнітофонами
    // =============================================================================================
    private static void BuildZone04_WestAcousticAnechoicLab(Transform parent)
    {
        float y = MainY;
        RegisterAcousticRoom(parent, "Зона 4: Акустична лабораторія VoIP", new Vector3(-32f, y + 2f, 59f), AcousticMaterialType.AcousticFoam, AudioReverbPreset.PaddedCell, 18f);

        // Підлога і стеля
        CreateAcousticBox("Z04_Floor_Main", parent, new Vector3(-32f, y - 0.2f, 59f), new Vector3(32.4f, 0.4f, 30.4f), _matLabTileFloor, AcousticMaterialType.Wood);
        CreateAcousticBox("Z04_Ceiling_Main", parent, new Vector3(-32f, CeilingY, 59f), new Vector3(32.4f, 0.3f, 30.4f), _matCeilingGrid, AcousticMaterialType.AcousticFoam);

        // Зовнішні стіни
        BuildSegmentedWallZ("Z04_Wall_West", parent, -48f, 44f, 74f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z04_Wall_East_N", parent, -16f, 52f, 74f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);

        // Північна стіна (Z = 74) з проходом у Серверну (Зона 8) на X: -30..-25
        BuildSegmentedWallX("Z04_Wall_North_W", parent, -48f, -30f, 74f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z04_Wall_North_E", parent, -25f, -16f, 74f, y, WallH, 0.5f, _matConcreteWall, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z04_NorthDoor_Header", parent, new Vector3(-27.5f, y + 3.5f, 74f), new Vector3(5.4f, 1.0f, 0.55f), _matDarkIron, AcousticMaterialType.Metal);

        // --- 1. БЕЗЛУННА КАМЕРА (ANECHOIC CHAMBER: X: -47.5..-35.5, Z: 58..73.5) ---
        BuildAnechoicChamberRoom("Z04_Anechoic_Chamber", parent, new Vector3(-41.5f, y, 65.8f), 11.6f, 14.8f);

        // --- 2. ГАЛЕРЕЯ ПОРІВНЯННЯ 4 АКУСТИЧНИХ МАТЕРІАЛІВ ДЛЯ ДЕМОНСТРАЦІЇ VoIP (X: -47..-33, Z: 45..56) ---
        // 4 сусідні кабіни, розділені реальними стінами різних матеріалів (Concrete, Wood, Metal, AcousticFoam)
        BuildAcousticMaterialTestBooth("Z04_VoIP_Booth_1_Concrete", parent, new Vector3(-45.5f, y, 50.0f), AcousticMaterialType.Concrete, _matConcreteWall, _matCrtAmber);
        BuildAcousticMaterialTestBooth("Z04_VoIP_Booth_2_Wood", parent, new Vector3(-41.5f, y, 50.0f), AcousticMaterialType.Wood, _matWoodOak, _matCrtGreen);
        BuildAcousticMaterialTestBooth("Z04_VoIP_Booth_3_Metal", parent, new Vector3(-37.5f, y, 50.0f), AcousticMaterialType.Metal, _matMetalBulkhead, _matCrtBlue);
        BuildAcousticMaterialTestBooth("Z04_VoIP_Booth_4_Foam", parent, new Vector3(-33.5f, y, 50.0f), AcousticMaterialType.AcousticFoam, _matAcousticFoam, _matCrtRed);

        // --- 3. ЗВУКОРЕЖИСЕРСЬКА АПАРАТНА З ОГЛЯДОВИМ СКЛОМ (X: -31..-17, Z: 58..72) ---
        BuildGlassObservationPartition("Z04_SoundBooth_GlassWest", parent, new Vector3(-31.5f, y, 65.5f), 11.0f, WallH, true);
        BuildSegmentedWallX("Z04_SoundBooth_WallSouth_L", parent, -31.5f, -26.5f, 59.5f, y, WallH, 0.3f, _matWoodOak, AcousticMaterialType.Wood);
        BuildSegmentedWallX("Z04_SoundBooth_WallSouth_R", parent, -23.5f, -16.2f, 59.5f, y, WallH, 0.3f, _matWoodOak, AcousticMaterialType.Wood);
        CreateAcousticBox("Z04_SoundBooth_DoorHeader", parent, new Vector3(-25.0f, y + 3.45f, 59.5f), new Vector3(3.2f, 1.1f, 0.32f), _matWoodDark, AcousticMaterialType.Wood);

        // 24-канальний аналоговий мікшерний пульт + студійні монітори + патч-панель
        BuildAnalogMixingConsole("Z04_AnalogMixer_24Ch", parent, new Vector3(-28.8f, y, 65.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildStudioMonitorSpeaker("Z04_Monitor_L", parent, new Vector3(-30.3f, y, 62.6f), Quaternion.Euler(0f, 65f, 0f));
        BuildStudioMonitorSpeaker("Z04_Monitor_R", parent, new Vector3(-30.3f, y, 68.4f), Quaternion.Euler(0f, 115f, 0f));
        BuildOfficeChair("Z04_EngineerChair_1", parent, new Vector3(-26.6f, y, 64.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildOfficeChair("Z04_EngineerChair_2", parent, new Vector3(-26.6f, y, 66.8f), Quaternion.Euler(0f, -80f, 0f));

        // Магнітофонні стійки запису голосу вздовж східної стіни апаратної
        for (int t = 0; t < 4; t++)
        {
            BuildTapeReelStand($"Z04_VoiceRecorderRack_{t}", parent, new Vector3(-17.4f, y, 61.5f + t * 2.6f), Quaternion.Euler(0f, -90f, 0f));
        }

        // Демонстраційний стіл радіозв'язку в коридорі (X = -22, Z = 51)
        BuildLongLabTable("Z04_RadioCalibrationDesk", parent, new Vector3(-22.0f, y, 52.0f), Quaternion.identity, 4.6f);
        BuildLabWhiteboard("Z04_FrequencyChartBoard", parent, new Vector3(-16.45f, y, 54.5f), Quaternion.Euler(0f, -90f, 0f));

        // Освітлення
        CreateCeilingLamp("Z04_Lamp_Anechoic", parent, new Vector3(-41.5f, y + 3.65f, 65.8f), _matLampAmber, new Color(1f, 0.75f, 0.35f), 3.6f, 12f);
        CreateCeilingLamp("Z04_Lamp_BoothCorridor", parent, new Vector3(-39.0f, y + 3.85f, 55.2f), _matLampCool, new Color(0.80f, 0.92f, 0.90f), 3.5f, 13f);
        CreateCeilingLamp("Z04_Lamp_SoundControl", parent, new Vector3(-24.0f, y + 3.85f, 65.5f), _matLampWarm, new Color(1f, 0.82f, 0.52f), 3.2f, 11f);
        CreateCeilingLamp("Z04_Lamp_EntryHall", parent, new Vector3(-22.5f, y + 3.85f, 48.5f), _matLampCool, new Color(0.78f, 0.92f, 0.88f), 3.2f, 11f);
    }


    // =============================================================================================
    // ЗОНА 5: СХІДНЕ КРИЛО А — АДМІНІСТРАЦІЯ, КАБІНЕТ ДИРЕКТОРА ТА ЗАЛ НАРАД (X: +16..+48, Z: 16..44)
    // Матеріали: Дерев'яні панелі (Wood) + Оглядове скло + Бетон (Тепла акустика для тесту VoIP!)
    // =============================================================================================
    private static void BuildZone05_EastAdministrationWing(Transform parent)
    {
        float y = MainY;
        RegisterAcousticRoom(parent, "Зона 5: Адміністрація (Дерево)", new Vector3(32f, y + 2f, 30f), AcousticMaterialType.Wood, AudioReverbPreset.Livingroom, 18f);

        // Підлога (паркет/дерево + килимова доріжка) та стеля
        CreateAcousticBox("Z05_Floor_Main", parent, new Vector3(32f, y - 0.2f, 30f), new Vector3(32.4f, 0.4f, 28.4f), _matWoodDark, AcousticMaterialType.Wood);
        CreateBox("Z05_Hallway_Carpet", parent, new Vector3(28.5f, y + 0.015f, 30.5f), new Vector3(24.0f, 0.02f, 3.2f), _matCarpetSoviet);
        CreateAcousticBox("Z05_Ceiling_Main", parent, new Vector3(32f, CeilingY, 30f), new Vector3(32.4f, 0.3f, 28.4f), _matCeilingGrid, AcousticMaterialType.Wood);

        // Зовнішні стіни крила (обшиті дубовими панелями)
        BuildSegmentedWallX("Z05_Wall_South", parent, 16f, 48f, 16f, y, WallH, 0.5f, _matWoodOak, AcousticMaterialType.Wood);
        BuildSegmentedWallZ("Z05_Wall_East", parent, 48f, 16f, 44f, y, WallH, 0.5f, _matWoodOak, AcousticMaterialType.Wood);
        BuildSegmentedWallZ("Z05_Wall_West_S", parent, 16f, 16f, 22f, y, WallH, 0.5f, _matWoodOak, AcousticMaterialType.Wood);

        // Північна стіна (Z = 44) з проходом у Медблок (Зона 6) на X: 22..27
        BuildSegmentedWallX("Z05_Wall_North_W", parent, 16f, 22f, 44f, y, WallH, 0.5f, _matWoodOak, AcousticMaterialType.Wood);
        BuildSegmentedWallX("Z05_Wall_North_E", parent, 27f, 48f, 44f, y, WallH, 0.5f, _matWoodOak, AcousticMaterialType.Wood);
        CreateAcousticBox("Z05_NorthDoor_Header", parent, new Vector3(24.5f, y + 3.5f, 44f), new Vector3(5.4f, 1.0f, 0.55f), _matWoodDark, AcousticMaterialType.Wood);

        // --- 1. КАБІНЕТ ДИРЕКТОРА КОМПЛЕКСУ (X: +35..+47.5, Z: 32..43.5) ---
        BuildSegmentedWallZ("Z05_Director_WallWest_S", parent, 35.0f, 32.0f, 35.5f, y, WallH, 0.28f, _matWoodOak, AcousticMaterialType.Wood);
        BuildSegmentedWallZ("Z05_Director_WallWest_N", parent, 35.0f, 38.0f, 44.0f, y, WallH, 0.28f, _matWoodOak, AcousticMaterialType.Wood);
        CreateAcousticBox("Z05_Director_DoorHeader", parent, new Vector3(35.0f, y + 3.45f, 36.75f), new Vector3(0.32f, 1.1f, 2.8f), _matWoodDark, AcousticMaterialType.Wood);
        BuildSegmentedWallX("Z05_Director_WallSouth", parent, 35.0f, 48.0f, 32.0f, y, WallH, 0.28f, _matWoodOak, AcousticMaterialType.Wood);

        // Меблі кабінету директора: масивний стіл, крісло, сейф, книжкові шафи, килим
        CreateBox("Z05_Director_Carpet", parent, new Vector3(41.5f, y + 0.015f, 38.0f), new Vector3(9.0f, 0.02f, 7.5f), _matCarpetSoviet);
        BuildExecutiveDesk("Z05_Director_ExecutiveDesk", parent, new Vector3(42.8f, y, 38.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildOfficeChair("Z05_Director_Chair", parent, new Vector3(44.4f, y, 38.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildOfficeChair("Z05_Director_GuestChair1", parent, new Vector3(40.8f, y, 37.0f), Quaternion.Euler(0f, 75f, 0f));
        BuildOfficeChair("Z05_Director_GuestChair2", parent, new Vector3(40.8f, y, 39.2f), Quaternion.Euler(0f, 105f, 0f));
        BuildHeavySafe("Z05_Director_Safe", parent, new Vector3(46.8f, y, 42.4f), Quaternion.Euler(0f, -135f, 0f));

        for (int b = 0; b < 4; b++)
        {
            BuildWoodenBookcaseWithBinders($"Z05_Director_Bookcase_{b}", parent, new Vector3(37.2f + b * 2.4f, y, 43.2f), Quaternion.Euler(0f, 180f, 0f));
        }

        // --- 2. ЗАЛ НАРАД ТА БРИФІНГІВ (X: +19..+34.5, Z: 33..43.5) ---
        BuildGlassObservationPartition("Z05_Briefing_WallSouth", parent, new Vector3(26.5f, y, 33.0f), 11.5f, WallH, false);
        BuildConferenceTableWithMics("Z05_Briefing_Table", parent, new Vector3(27.0f, y, 38.5f), 7.6f, 2.2f);
        for (int c = 0; c < 4; c++)
        {
            float cx = 24.2f + c * 1.85f;
            BuildOfficeChair($"Z05_BriefChair_S_{c}", parent, new Vector3(cx, y, 36.6f), Quaternion.identity);
            BuildOfficeChair($"Z05_BriefChair_N_{c}", parent, new Vector3(cx, y, 40.4f), Quaternion.Euler(0f, 180f, 0f));
        }
        BuildLabWhiteboard("Z05_Briefing_ProjectionScreen", parent, new Vector3(34.4f, y, 38.5f), Quaternion.Euler(0f, -90f, 0f));

        // --- 3. ВІДДІЛ АНАЛІТИКІВ ТА ДРУКАРСЬКЕ БЮРО (X: +18..+34, Z: 16.5..27.5) ---
        for (int col = 0; col < 3; col++)
        {
            float dx = 20.5f + col * 4.8f;
            // Дерев'яні офісні перегородки між столами (для акустичного тесту Wood!)
            CreateAcousticBox($"Z05_CubicleDivider_{col}", parent, new Vector3(dx + 2.3f, y + 0.9f, 21.5f), new Vector3(0.16f, 1.8f, 6.2f), _matWoodPlywood, AcousticMaterialType.Wood);
            BuildRetroWorkstationDesk($"Z05_AnalystDesk_S_{col}", parent, new Vector3(dx, y, 19.2f), Quaternion.identity, col % 2 == 0);
            BuildOfficeChair($"Z05_AnalystChair_S_{col}", parent, new Vector3(dx, y, 20.6f), Quaternion.Euler(0f, 180f, 0f));
            BuildRetroWorkstationDesk($"Z05_AnalystDesk_N_{col}", parent, new Vector3(dx, y, 24.0f), Quaternion.Euler(0f, 180f, 0f), col % 2 == 1);
            BuildOfficeChair($"Z05_AnalystChair_N_{col}", parent, new Vector3(dx, y, 22.6f), Quaternion.identity);
        }

        // --- 4. КІМНАТА ВІДПОЧИНКУ ПЕРСОНАЛУ (X: +35..+47.5, Z: 16.5..31.5) ---
        // Тут стоїть шаховий стіл гравців (X = 39, Z = 23) + холодильник, стіл, шафки
        BuildSegmentedWallZ("Z05_Breakroom_WallWest_S", parent, 35.0f, 16.0f, 22.0f, y, WallH, 0.25f, _matWoodPlywood, AcousticMaterialType.Wood);
        BuildSegmentedWallZ("Z05_Breakroom_WallWest_N", parent, 35.0f, 25.0f, 32.0f, y, WallH, 0.25f, _matWoodPlywood, AcousticMaterialType.Wood);
        CreateAcousticBox("Z05_Breakroom_DoorHeader", parent, new Vector3(35.0f, y + 3.45f, 23.5f), new Vector3(0.28f, 1.1f, 3.2f), _matWoodDark, AcousticMaterialType.Wood);

        BuildKitchenCounterAndFridge("Z05_Breakroom_Kitchen", parent, new Vector3(46.5f, y, 24.0f));
        BuildLockerBench("Z05_Breakroom_Sofa", parent, new Vector3(41.5f, y, 17.5f), Quaternion.identity);

        // Тепле лампове освітлення дерев'яного крила
        CreateCeilingLamp("Z05_Lamp_Director", parent, new Vector3(41.5f, y + 3.85f, 38.0f), _matLampWarm, new Color(1f, 0.80f, 0.45f), 3.6f, 12f);
        CreateCeilingLamp("Z05_Lamp_Briefing", parent, new Vector3(27.0f, y + 3.85f, 38.5f), _matLampWarm, new Color(1f, 0.84f, 0.52f), 3.5f, 12f);
        CreateCeilingLamp("Z05_Lamp_Analysts", parent, new Vector3(25.5f, y + 3.85f, 22.0f), _matLampCool, new Color(0.84f, 0.92f, 0.85f), 3.4f, 13f);
        CreateCeilingLamp("Z05_Lamp_Breakroom", parent, new Vector3(41.5f, y + 3.85f, 24.0f), _matLampWarm, new Color(1f, 0.82f, 0.48f), 3.4f, 12f);
    }


    // =============================================================================================
    // ЗОНА 6: СХІДНЕ КРИЛО Б — БІОМЕТРИЧНИЙ МЕДБЛОК ТА КРІОСХОВИЩЕ (X: +16..+48, Z: 44..74)
    // Матеріали: Кахельна плитка (Tile) + Скло (Glass) + Метал (Дзвінка стерильна акустика!)
    // =============================================================================================
    private static void BuildZone06_EastBiometricMedicalBay(Transform parent)
    {
        float y = MainY;
        RegisterAcousticRoom(parent, "Зона 6: Біометричний медблок (Плитка)", new Vector3(32f, y + 2f, 59f), AcousticMaterialType.Tile, AudioReverbPreset.Bathroom, 18f);

        // Стерильна підлога і стеля
        CreateAcousticBox("Z06_Floor_Main", parent, new Vector3(32f, y - 0.2f, 59f), new Vector3(32.4f, 0.4f, 30.4f), _matCheckerTile, AcousticMaterialType.Tile);
        CreateAcousticBox("Z06_Ceiling_Main", parent, new Vector3(32f, CeilingY, 59f), new Vector3(32.4f, 0.3f, 30.4f), _matCeilingGrid, AcousticMaterialType.Tile);

        // Зовнішні стіни
        BuildSegmentedWallZ("Z06_Wall_East", parent, 48f, 44f, 74f, y, WallH, 0.5f, _matCeramicTileWall, AcousticMaterialType.Tile);
        BuildSegmentedWallZ("Z06_Wall_West_N", parent, 16f, 52f, 74f, y, WallH, 0.5f, _matCeramicTileWall, AcousticMaterialType.Tile);

        // Північна стіна (Z = 74) з проходом у Реакторний зал (Зона 9) на X: 25..30
        BuildSegmentedWallX("Z06_Wall_North_W", parent, 16f, 25f, 74f, y, WallH, 0.5f, _matCeramicTileWall, AcousticMaterialType.Tile);
        BuildSegmentedWallX("Z06_Wall_North_E", parent, 30f, 48f, 74f, y, WallH, 0.5f, _matCeramicTileWall, AcousticMaterialType.Tile);
        CreateAcousticBox("Z06_NorthDoor_Header", parent, new Vector3(27.5f, y + 3.5f, 74f), new Vector3(5.4f, 1.0f, 0.55f), _matDarkIron, AcousticMaterialType.Metal);

        // --- 1. ТРИ ОГЛЯДОВІ БОКСИ З МЕДИЧНИМИ ЛІЖКАМИ ТА ШИРМАМИ (X: +36..+47.5, Z: 45..62) ---
        for (int b = 0; b < 3; b++)
        {
            float bz = 48.0f + b * 5.2f;
            BuildMedicalExaminationBay($"Z06_ExamBay_{b}", parent, new Vector3(42.5f, y, bz));
            if (b < 2)
            {
                CreateAcousticBox($"Z06_BayDivider_{b}", parent, new Vector3(42.5f, y + 1.4f, bz + 2.6f), new Vector3(9.0f, 2.8f, 0.16f), _matCeramicTileWall, AcousticMaterialType.Tile);
            }
        }

        // --- 2. ЦЕНТРАЛЬНА БІОМЕТРИЧНА ЛАБОРАТОРІЯ ТА НЕЙРО-СКАНЕР (X: +19..+34, Z: 48..60) ---
        BuildNeuralScannerPod("Z06_NeuralMRI_Pod", parent, new Vector3(28.5f, y, 54.0f));
        BuildLongLabTable("Z06_BiometricBench_1", parent, new Vector3(21.5f, y, 54.0f), Quaternion.Euler(0f, 90f, 0f), 6.2f);
        BuildMedicalCabinet("Z06_MedCabinet_1", parent, new Vector3(17.2f, y, 57.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildMedicalCabinet("Z06_MedCabinet_2", parent, new Vector3(17.2f, y, 60.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildXRayLightBoxWall("Z06_XRayWall", parent, new Vector3(16.45f, y + 1.85f, 64.5f), Quaternion.Euler(0f, 90f, 0f));

        // --- 3. СКЛЯНА КАМЕРА КРІОСХОВИЩА З 6 КАПСУЛАМИ (X: +33..+47.5, Z: 63..73.5) ---
        BuildGlassObservationPartition("Z06_CryoVault_GlassSouth", parent, new Vector3(40.5f, y, 63.0f), 11.0f, WallH, false);
        CreateAcousticBox("Z06_CryoVault_DoorHeader", parent, new Vector3(33.5f, y + 3.45f, 63.0f), new Vector3(3.2f, 1.1f, 0.35f), _matDarkIron, AcousticMaterialType.Metal);

        for (int c = 0; c < 6; c++)
        {
            float cx = 35.8f + (c % 3) * 4.2f;
            float cz = (c < 3) ? 66.2f : 70.8f;
            BuildCryoStoragePod($"Z06_CryoPod_{c}", parent, new Vector3(cx, y, cz));
        }

        // Балони рідкого азоту вздовж східної стіни кріосховища
        for (int n = 0; n < 4; n++)
        {
            CreateCylinder($"Z06_NitrogenTank_{n}", parent, new Vector3(46.8f, y + 1.1f, 65.2f + n * 1.9f), new Vector3(0.65f, 1.1f, 0.65f), _matMedicalWhite);
        }

        // Холодне хірургічне та блакитне кріо-освітлення
        CreateCeilingLamp("Z06_Lamp_MedCenter", parent, new Vector3(26.5f, y + 3.85f, 54.0f), _matLampCool, new Color(0.80f, 0.96f, 1.0f), 4.0f, 14f);
        CreateCeilingLamp("Z06_Lamp_ExamBays", parent, new Vector3(42.5f, y + 3.85f, 53.0f), _matLampCool, new Color(0.85f, 0.98f, 1.0f), 3.8f, 14f);
        CreateCeilingLamp("Z06_Lamp_CryoVault", parent, new Vector3(40.5f, y + 3.85f, 68.5f), _matLampCyan, new Color(0.35f, 0.85f, 1.0f), 4.2f, 13f);
    }


    // =============================================================================================
    // ЗОНА 7: ПІВНІЧНИЙ КОНТУР БЕЗПЕКИ (X: -14..+14, Z: 52..74)
    // Броньований пропускний вузол перед Серверною, Реактором та Центральним Ядром
    // Матеріали: Сталь (Metal) + Армований бетон + Броньоване скло
    // =============================================================================================
    private static void BuildZone07_NorthSecurityCheckpoint(Transform parent)
    {
        float y = MainY;
        RegisterAcousticRoom(parent, "Зона 7: Контур Безпеки (Метал)", new Vector3(0f, y + 2f, 63f), AcousticMaterialType.Metal, AudioReverbPreset.Hallway, 15f);

        CreateAcousticBox("Z07_Floor_Main", parent, new Vector3(0f, y - 0.2f, 63f), new Vector3(28.4f, 0.4f, 22.4f), _matLabTileFloor, AcousticMaterialType.Metal);
        CreateAcousticBox("Z07_Ceiling_Main", parent, new Vector3(0f, CeilingY, 63f), new Vector3(28.4f, 0.3f, 22.4f), _matMetalBulkhead, AcousticMaterialType.Metal);

        // Бічні бронестіни з проходами на Захід (Z: 60..65) і Схід (Z: 60..65)
        BuildSegmentedWallZ("Z07_Wall_West_S", parent, -14f, 52f, 60f, y, WallH, 0.55f, _matMetalBulkhead, AcousticMaterialType.Metal);
        BuildSegmentedWallZ("Z07_Wall_West_N", parent, -14f, 65f, 74f, y, WallH, 0.55f, _matMetalBulkhead, AcousticMaterialType.Metal);
        BuildSegmentedWallZ("Z07_Wall_East_S", parent, 14f, 52f, 60f, y, WallH, 0.55f, _matMetalBulkhead, AcousticMaterialType.Metal);
        BuildSegmentedWallZ("Z07_Wall_East_N", parent, 14f, 65f, 74f, y, WallH, 0.55f, _matMetalBulkhead, AcousticMaterialType.Metal);

        // Короткі з'єднувальні тамбури між X = +-14 та X = +-16 (до Зон 4 і 6)
        CreateAcousticBox("Z07_LinkFloor_W", parent, new Vector3(-15f, y - 0.2f, 62.5f), new Vector3(2.4f, 0.4f, 5.4f), _matLabTileFloor, AcousticMaterialType.Metal);
        CreateAcousticBox("Z07_LinkFloor_E", parent, new Vector3(15f, y - 0.2f, 62.5f), new Vector3(2.4f, 0.4f, 5.4f), _matLabTileFloor, AcousticMaterialType.Metal);

        // Північна стіна (Z = 74) з центральним масивним шлюзом до Зон 8, 9, 10 (X: -3.5..+3.5)
        BuildSegmentedWallX("Z07_Wall_North_L", parent, -14f, -3.5f, 74f, y, WallH, 0.6f, _matMetalBulkhead, AcousticMaterialType.Metal);
        BuildSegmentedWallX("Z07_Wall_North_R", parent, 3.5f, 14f, 74f, y, WallH, 0.6f, _matMetalBulkhead, AcousticMaterialType.Metal);
        CreateAcousticBox("Z07_NorthGate_Header", parent, new Vector3(0f, y + 3.45f, 74f), new Vector3(7.4f, 1.1f, 0.7f), _matHazardStripes, AcousticMaterialType.Metal);

        // --- ЦЕНТРАЛЬНА СКЛЯНА КАБІНА CCTV-ОХОРОНИ (X: -12.5..-5.5, Z: 54..61) ---
        BuildGlassObservationPartition("Z07_CctvBooth_GlassEast", parent, new Vector3(-5.5f, y, 57.5f), 7.0f, WallH, true);
        BuildSegmentedWallX("Z07_CctvBooth_WallNorth", parent, -14.0f, -8.2f, 61.0f, y, WallH, 0.3f, _matMetalBulkhead, AcousticMaterialType.Metal);
        CreateAcousticBox("Z07_CctvBooth_DoorHeader", parent, new Vector3(-6.8f, y + 3.45f, 61.0f), new Vector3(2.8f, 1.1f, 0.32f), _matMetalBulkhead, AcousticMaterialType.Metal);

        // Стіна з 18 моніторами відеоспостереження всередині поста охорони
        BuildCctvMonitorWall("Z07_CctvWall_18Screens", parent, new Vector3(-13.3f, y, 57.5f), Quaternion.Euler(0f, 90f, 0f), 6, 3);
        BuildOscilloscopeConsoleDesk("Z07_SecurityDesk", parent, new Vector3(-9.2f, y, 57.5f), Quaternion.Euler(0f, -90f, 0f));

        // --- ЗБРОЙОВА ТА ШАФИ СПОРЯДЖЕННЯ СПРАВА (X: +6..+13, Z: 54..61) ---
        for (int k = 0; k < 5; k++)
        {
            BuildMetalLocker($"Z07_ArmoryLocker_{k}", parent, new Vector3(13.1f, y, 54.0f + k * 1.25f), Quaternion.Euler(0f, -90f, 0f));
        }
        BuildLongLabTable("Z07_InspectionTable", parent, new Vector3(9.5f, y, 56.5f), Quaternion.Euler(0f, 90f, 0f), 4.5f);

        // Арки металодетекторів та сенсорні пілони по центру проходу
        BuildSecurityScannerGate("Z07_ScannerGate_1", parent, new Vector3(0f, y, 57.5f));
        BuildSecurityScannerGate("Z07_ScannerGate_2", parent, new Vector3(0f, y, 68.5f));

        // Червоне аварійне освітлення контуру безпеки
        CreateCeilingLamp("Z07_Lamp_Alarm1", parent, new Vector3(0f, y + 3.85f, 57.5f), _matLampRed, new Color(1f, 0.25f, 0.16f), 4.0f, 12f);
        CreateCeilingLamp("Z07_Lamp_Alarm2", parent, new Vector3(0f, y + 3.85f, 68.5f), _matLampAmber, new Color(1f, 0.62f, 0.18f), 3.8f, 12f);
        CreateCeilingLamp("Z07_Lamp_Cctv", parent, new Vector3(-9.5f, y + 3.85f, 57.5f), _matCrtGreen, new Color(0.45f, 0.9f, 0.55f), 2.8f, 9f);
    }


    // =============================================================================================
    // ЗОНА 8: ПІВНІЧНО-ЗАХІДНЕ КРИЛО — СЕРВЕРНА МАТРИЦЬ ПОВЕДІНКИ ЕС-ЕОМ (X: -48..-14, Z: 74..108)
    // Лабіринт із 24 мейнфреймів 80-х для стелсу та акустичного патрулювання NPC «Копія»!
    // Матеріали: Сталь (Metal) + Бетон
    // =============================================================================================
    private static void BuildZone08_NorthWestServerHall(Transform parent)
    {
        float y = MainY;
        RegisterAcousticRoom(parent, "Зона 8: Серверна матриць (Метал)", new Vector3(-31f, y + 2f, 91f), AcousticMaterialType.Metal, AudioReverbPreset.Hangar, 22f);

        // Фальшпідлога серверної з підсвіченими кабельними каналами
        CreateAcousticBox("Z08_Floor_Main", parent, new Vector3(-31f, y - 0.2f, 91f), new Vector3(34.4f, 0.4f, 34.4f), _matDarkIron, AcousticMaterialType.Metal);
        CreateAcousticBox("Z08_Ceiling_Main", parent, new Vector3(-31f, CeilingY, 91f), new Vector3(34.4f, 0.3f, 34.4f), _matConcreteDark, AcousticMaterialType.Concrete);

        // Підсвічені кабельні канали у підлозі між рядами серверів
        for (int ch = 0; ch < 3; ch++)
        {
            float cx = -40.5f + ch * 9.5f;
            CreateBox($"Z08_CableTrench_{ch}", parent, new Vector3(cx, y + 0.015f, 91.0f), new Vector3(0.65f, 0.02f, 28.0f), _matCrtAmber);
        }

        // Зовнішні стіни серверної
        BuildSegmentedWallZ("Z08_Wall_West", parent, -48f, 74f, 108f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);

        // Східна стіна (X = -14) з 2 проходами в Центральний Північний Коридор (Z: 82..87 та Z: 98..103)
        BuildSegmentedWallZ("Z08_Wall_East_S", parent, -14f, 74f, 82f, y, WallH, 0.5f, _matMetalBulkhead, AcousticMaterialType.Metal);
        BuildSegmentedWallZ("Z08_Wall_East_M", parent, -14f, 87f, 98f, y, WallH, 0.5f, _matMetalBulkhead, AcousticMaterialType.Metal);
        BuildSegmentedWallZ("Z08_Wall_East_N", parent, -14f, 103f, 108f, y, WallH, 0.5f, _matMetalBulkhead, AcousticMaterialType.Metal);

        // Північна стіна (Z = 108) з проходом у Центральне Ядро на X: -20..-15
        BuildSegmentedWallX("Z08_Wall_North_W", parent, -48f, -20f, 108f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z08_Wall_North_E", parent, -15f, -14f, 108f, y, WallH, 0.55f, _matConcreteWall, AcousticMaterialType.Concrete);

        // --- 4 РЯДИ ПО 6 МЕЙНФРЕЙМІВ ЕС-ЕОМ (24 ШАФИ) З ПРОХОДАМИ ДЛЯ СТЕЛСУ ---
        for (int row = 0; row < 4; row++)
        {
            float rx = -44.2f + row * 8.2f;
            for (int col = 0; col < 6; col++)
            {
                // Пропускаємо 1 шафу в кожному ряду для поперечних стелс-обходів
                if ((row + col) % 4 == 2) continue;

                float rz = 79.5f + col * 4.4f;
                Quaternion rot = (row % 2 == 0) ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.Euler(0f, -90f, 0f);
                BuildServerMainframeTower($"Z08_Mainframe_R{row}_C{col}", parent, new Vector3(rx, y, rz), rot, (row + col) % 3);
            }
        }

        // Операторські термінали між рядами серверів
        BuildRetroTerminalKiosk("Z08_NodeTerm_1", parent, new Vector3(-36.0f, y, 88.0f), Quaternion.identity);
        BuildRetroTerminalKiosk("Z08_NodeTerm_2", parent, new Vector3(-27.8f, y, 96.0f), Quaternion.Euler(0f, 180f, 0f));
        BuildRetroTerminalKiosk("Z08_NodeTerm_3", parent, new Vector3(-19.5f, y, 84.0f), Quaternion.Euler(0f, -90f, 0f));

        // Потолочні труби охолодження та холодне/бурштинове освітлення
        for (int p = 0; p < 3; p++)
        {
            BuildCeilingUtilityRun($"Z08_CoolingDuct_{p}", parent, new Vector3(-40.5f + p * 9.5f, y + 3.65f, 91f), 32f, false);
        }

        CreateCeilingLamp("Z08_Lamp_SW", parent, new Vector3(-40f, y + 3.85f, 82f), _matLampCool, new Color(0.65f, 0.88f, 0.96f), 3.6f, 15f);
        CreateCeilingLamp("Z08_Lamp_NW", parent, new Vector3(-40f, y + 3.85f, 100f), _matLampAmber, new Color(1f, 0.65f, 0.22f), 3.6f, 15f);
        CreateCeilingLamp("Z08_Lamp_SE", parent, new Vector3(-22f, y + 3.85f, 82f), _matLampCool, new Color(0.65f, 0.88f, 0.96f), 3.6f, 15f);
        CreateCeilingLamp("Z08_Lamp_NE", parent, new Vector3(-22f, y + 3.85f, 100f), _matLampRed, new Color(1f, 0.30f, 0.20f), 3.6f, 15f);
    }


    // =============================================================================================
    // ЗОНА 9: ПІВНІЧНО-СХІДНЕ КРИЛО — ДВОРІВНЕВИЙ РЕАКТОРНИЙ ЗАЛ ТА ПІДСТАНЦІЯ (X: +14..+48, Z: 74..108)
    // Верхні сталеві містки (Y = -4.5m) + Нижній машинний поверх (Y = -9.0m) зі сходами у підповерх!
    // =============================================================================================
    private static void BuildZone09_NorthEastReactorSubstation(Transform parent)
    {
        float y = MainY;
        float sy = SubY;
        RegisterAcousticRoom(parent, "Зона 9: Реакторний зал (Бетон/Метал)", new Vector3(31f, y + 1f, 91f), AcousticMaterialType.Metal, AudioReverbPreset.Hangar, 24f);

        // Нижня підлога машинного залу (Y = -9.0m) та верхня стеля (Y = -0.35m) — висота залу 8.5 метрів!
        CreateAcousticBox("Z09_LowerFloor_SubY", parent, new Vector3(31f, sy - 0.2f, 91f), new Vector3(34.4f, 0.4f, 34.4f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z09_Ceiling_Main", parent, new Vector3(31f, CeilingY, 91f), new Vector3(34.4f, 0.35f, 34.4f), _matConcreteDark, AcousticMaterialType.Concrete);

        // Зовнішні стіни на повну подвійну висоту (від SubY до CeilingY = 8.6м)
        BuildSegmentedWallZ("Z09_Wall_East_Full", parent, 48f, 74f, 108f, sy, 8.6f, 0.6f, _matConcreteDamaged, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z09_Wall_North_Full", parent, 14f, 48f, 108f, sy, 8.6f, 0.6f, _matConcreteDamaged, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z09_Wall_South_Lower", parent, 14f, 48f, 74f, sy, 4.5f, 0.6f, _matConcreteDamaged, AcousticMaterialType.Concrete);

        // Західна стіна (X = +14): нижній прохід у підповерх (Z: 88..94) + верхні проходи в центральний коридор (Z: 82..87, 98..103)
        BuildSegmentedWallZ("Z09_Wall_West_Lower_S", parent, 14f, 74f, 88f, sy, 4.5f, 0.55f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z09_Wall_West_Lower_N", parent, 14f, 94f, 108f, sy, 4.5f, 0.55f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z09_Wall_West_Upper_S", parent, 14f, 74f, 82f, y, WallH, 0.5f, _matMetalBulkhead, AcousticMaterialType.Metal);
        BuildSegmentedWallZ("Z09_Wall_West_Upper_M", parent, 14f, 87f, 98f, y, WallH, 0.5f, _matMetalBulkhead, AcousticMaterialType.Metal);
        BuildSegmentedWallZ("Z09_Wall_West_Upper_N", parent, 14f, 103f, 108f, y, WallH, 0.5f, _matMetalBulkhead, AcousticMaterialType.Metal);

        // --- ВЕРХНІ СТАЛЕВІ МІСТКИ ОБСЛУГОВУВАННЯ (CATWALKS НА РІВНІ Y = -4.5m) ---
        CreateAcousticBox("Z09_Catwalk_South", parent, new Vector3(31f, y - 0.15f, 77.2f), new Vector3(33.6f, 0.3f, 6.2f), _matDarkIron, AcousticMaterialType.Metal);
        CreateAcousticBox("Z09_Catwalk_West", parent, new Vector3(17.5f, y - 0.15f, 92.5f), new Vector3(6.8f, 0.3f, 29.0f), _matDarkIron, AcousticMaterialType.Metal);
        CreateAcousticBox("Z09_Catwalk_North", parent, new Vector3(33.5f, y - 0.15f, 104.5f), new Vector3(28.0f, 0.3f, 6.2f), _matDarkIron, AcousticMaterialType.Metal);

        // Перила вздовж усіх містків
        BuildSafetyRailingSegment("Z09_Rail_South", parent, new Vector3(21f, y, 80.2f), new Vector3(47.5f, y, 80.2f));
        BuildSafetyRailingSegment("Z09_Rail_West_1", parent, new Vector3(20.8f, y, 80.2f), new Vector3(20.8f, y, 88.5f));
        BuildSafetyRailingSegment("Z09_Rail_West_2", parent, new Vector3(20.8f, y, 94.5f), new Vector3(20.8f, y, 101.4f));
        BuildSafetyRailingSegment("Z09_Rail_North", parent, new Vector3(20.8f, y, 101.4f), new Vector3(47.5f, y, 101.4f));

        // Сходи з верхнього містка вниз у машинний зал (Y = -4.5m -> -9.0m)
        BuildIndustrialStaircase("Z09_Reactor_DownStairs", parent, new Vector3(21.2f, y, 91.5f), new Vector3(28.0f, sy, 91.5f), 2.8f, 16);

        // --- 3 МАСИВНІ ВЕРТИКАЛЬНІ ТУРБІНИ РЕАКТОРА З МІДНИМИ ТЕПЛООБМІННИКАМИ (Y = -9.0m) ---
        for (int t = 0; t < 3; t++)
        {
            float tx = 31.5f + t * 6.5f;
            BuildReactorTurbineTower($"Z09_ReactorTurbine_{t}", parent, new Vector3(tx, sy, 91.0f));
        }

        // Високовольтні трансформатори та пульт управління на верхній галереї
        BuildOscilloscopeConsoleDesk("Z09_ReactorControl_1", parent, new Vector3(32.0f, y, 76.2f), Quaternion.identity);
        BuildOscilloscopeConsoleDesk("Z09_ReactorControl_2", parent, new Vector3(36.0f, y, 76.2f), Quaternion.identity);
        BuildRetroMainframeCabinet("Z09_Switchgear_1", parent, new Vector3(43.5f, y, 76.2f), Quaternion.identity);
        BuildRetroMainframeCabinet("Z09_Switchgear_2", parent, new Vector3(43.5f, y, 105.5f), Quaternion.Euler(0f, 180f, 0f));

        // Калюжі охолоджувальної рідини на нижньому рівні
        CreateBox("Z09_CoolantPuddle_1", parent, new Vector3(29.0f, sy + 0.015f, 84.5f), new Vector3(5.5f, 0.01f, 3.8f), _matWaterPuddle);
        CreateBox("Z09_CoolantPuddle_2", parent, new Vector3(40.0f, sy + 0.015f, 97.5f), new Vector3(6.2f, 0.01f, 4.2f), _matWaterPuddle);

        // Жовтий мостовий кран під стелею
        CreateBox("Z09_OverheadCrane_Bridge", parent, new Vector3(34.5f, y + 3.1f, 91.0f), new Vector3(26.0f, 0.65f, 1.4f), _matHazardStripes);
        CreateBox("Z09_OverheadCrane_Trolley", parent, new Vector3(38.0f, y + 2.4f, 91.0f), new Vector3(2.2f, 0.9f, 1.8f), _matDarkIron);

        // Індустріальне освітлення
        CreateCeilingLamp("Z09_Lamp_TurbineGlow1", parent, new Vector3(31.5f, y + 3.5f, 91.0f), _matLampAmber, new Color(1f, 0.58f, 0.16f), 5.5f, 20f);
        CreateCeilingLamp("Z09_Lamp_TurbineGlow2", parent, new Vector3(44.5f, y + 3.5f, 91.0f), _matLampAmber, new Color(1f, 0.58f, 0.16f), 5.0f, 18f);
        CreateCeilingLamp("Z09_Lamp_LowerFloor", parent, new Vector3(34.0f, sy + 3.6f, 84.0f), _matLampCool, new Color(0.65f, 0.88f, 0.92f), 4.0f, 16f);
    }


    // =============================================================================================
    // ЗОНА 10: ЦЕНТРАЛЬНИЙ ПІВНІЧНИЙ КОРИДОР + ЯДРО АРХІВУ «СИНХРОНІЗАЦІЯ» (X: -20..+20, Z: 74..136)
    // Фінальна кульмінаційна зала з 4 терміналами етичного вибору (Утилізація / Автономія / Спадкоємець / Забуття)
    // =============================================================================================
    private static void BuildZone10_FarNorthCentralCoreArchive(Transform parent)
    {
        float y = MainY;
        float sy = SubY;
        RegisterAcousticRoom(parent, "Зона 10: Центральне Ядро Архіву", new Vector3(0f, y + 2f, 122f), AcousticMaterialType.Concrete, AudioReverbPreset.Concerthall, 24f);

        // --- 1. ЦЕНТРАЛЬНИЙ ПІВНІЧНИЙ ПРОХІД (X: -14..+14, Z: 74..108) ---
        // З'єднує Контур Безпеки (Зона 7), Серверну (Зона 8), Реактор (Зона 9) і Центральне Ядро
        CreateAcousticBox("Z10_NorthHall_Floor", parent, new Vector3(0f, y - 0.2f, 91f), new Vector3(28.4f, 0.4f, 34.4f), _matConcreteFloor, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z10_NorthHall_Ceiling", parent, new Vector3(0f, CeilingY, 91f), new Vector3(28.4f, 0.3f, 34.4f), _matConcreteDark, AcousticMaterialType.Concrete);

        // Колонада вздовж центрального проходу до Ядра
        for (float pz = 79f; pz <= 103f; pz += 6f)
        {
            BuildRibbedConcretePillar($"Z10_HallPillar_L_{pz}", parent, new Vector3(-6.2f, y, pz), WallH);
            BuildRibbedConcretePillar($"Z10_HallPillar_R_{pz}", parent, new Vector3(6.2f, y, pz), WallH);
        }

        // Портал входу у фінальне Ядро (Z = 108, X: -4.5..+4.5)
        BuildSegmentedWallX("Z10_CoreGate_L", parent, -14f, -4.5f, 108f, y, WallH, 0.65f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z10_CoreGate_R", parent, 4.5f, 14f, 108f, y, WallH, 0.65f, _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z10_CoreGate_Header", parent, new Vector3(0f, y + 3.45f, 108f), new Vector3(9.6f, 1.1f, 0.75f), _matHazardStripes, AcousticMaterialType.Metal);

        // --- 2. МОНУМЕНТАЛЬНА ЗАЛА ЯДРА АРХІВУ (X: -20..+20, Z: 108..136) ---
        // Глибока безодня під мостами (Y = -9.0m) та центральна платформа Ядра на Y = -4.5m
        CreateAcousticBox("Z10_CoreAbyss_Floor", parent, new Vector3(0f, sy - 0.2f, 122f), new Vector3(40.8f, 0.4f, 28.8f), _matDarkIron, AcousticMaterialType.Concrete);
        CreateAcousticBox("Z10_CoreChamber_Ceiling", parent, new Vector3(0f, CeilingY, 122f), new Vector3(40.8f, 0.35f, 28.8f), _matConcreteDark, AcousticMaterialType.Concrete);

        // Зовнішні стіни зали Ядра на повну подвійну висоту
        BuildSegmentedWallX("Z10_CoreWall_North", parent, -20f, 20f, 136f, sy, 8.6f, 0.65f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z10_CoreWall_West", parent, -20f, 108f, 136f, sy, 8.6f, 0.65f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallZ("Z10_CoreWall_East", parent, 20f, 108f, 136f, sy, 8.6f, 0.65f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z10_CoreWall_South_FarL", parent, -20f, -14f, 108f, sy, 8.6f, 0.65f, _matConcreteDark, AcousticMaterialType.Concrete);
        BuildSegmentedWallX("Z10_CoreWall_South_FarR", parent, 14f, 20f, 108f, sy, 8.6f, 0.65f, _matConcreteDark, AcousticMaterialType.Concrete);

        // Центральна восьмикутна платформа Ядра та 4 радіальні мости з перилами
        CreateAcousticBox("Z10_CorePlatform_Center", parent, new Vector3(0f, y - 0.2f, 122f), new Vector3(13.0f, 0.4f, 13.0f), _matMetalBulkhead, AcousticMaterialType.Metal);
        CreateAcousticBox("Z10_CoreBridge_South", parent, new Vector3(0f, y - 0.2f, 111.8f), new Vector3(5.6f, 0.4f, 8.2f), _matDarkIron, AcousticMaterialType.Metal);
        CreateAcousticBox("Z10_CoreBridge_North", parent, new Vector3(0f, y - 0.2f, 132.0f), new Vector3(5.6f, 0.4f, 7.6f), _matDarkIron, AcousticMaterialType.Metal);
        CreateAcousticBox("Z10_CoreBridge_West", parent, new Vector3(-13.2f, y - 0.2f, 122f), new Vector3(13.6f, 0.4f, 4.8f), _matDarkIron, AcousticMaterialType.Metal);
        CreateAcousticBox("Z10_CoreBridge_East", parent, new Vector3(13.2f, y - 0.2f, 122f), new Vector3(13.6f, 0.4f, 4.8f), _matDarkIron, AcousticMaterialType.Metal);

        // Перила мостів
        BuildSafetyRailingSegment("Z10_BridgeRail_SL", parent, new Vector3(-2.7f, y, 108.2f), new Vector3(-2.7f, y, 115.5f));
        BuildSafetyRailingSegment("Z10_BridgeRail_SR", parent, new Vector3(2.7f, y, 108.2f), new Vector3(2.7f, y, 115.5f));
        BuildSafetyRailingSegment("Z10_BridgeRail_WL", parent, new Vector3(-19.5f, y, 119.6f), new Vector3(-6.5f, y, 119.6f));
        BuildSafetyRailingSegment("Z10_BridgeRail_WR", parent, new Vector3(-19.5f, y, 124.4f), new Vector3(-6.5f, y, 124.4f));
        BuildSafetyRailingSegment("Z10_BridgeRail_EL", parent, new Vector3(6.5f, y, 119.6f), new Vector3(19.5f, y, 119.6f));
        BuildSafetyRailingSegment("Z10_BridgeRail_ER", parent, new Vector3(6.5f, y, 124.4f), new Vector3(19.5f, y, 124.4f));

        // --- ЦЕНТРАЛЬНА КОЛОНА НЕЙРОМАТРИЦІ «КОПІЇ» ---
        BuildNeuralCoreMonument("Z10_NeuralMatrix_CoreColumn", parent, new Vector3(0f, sy, 122f));

        // --- 4 ТЕРМІНАЛИ ЕТИЧНОГО ВИБОРУ ФІНАЛУ (З GDD) ---
        BuildEndingChoiceTerminal("Z10_Ending_1_Utilization_Red", parent, new Vector3(-4.8f, y, 117.2f), Quaternion.Euler(0f, 45f, 0f), _matCrtRed);
        BuildEndingChoiceTerminal("Z10_Ending_2_Autonomy_Green", parent, new Vector3(4.8f, y, 117.2f), Quaternion.Euler(0f, -45f, 0f), _matCrtGreen);
        BuildEndingChoiceTerminal("Z10_Ending_3_Heir_Amber", parent, new Vector3(-4.8f, y, 126.8f), Quaternion.Euler(0f, 135f, 0f), _matCrtAmber);
        BuildEndingChoiceTerminal("Z10_Ending_4_Oblivion_Cyan", parent, new Vector3(4.8f, y, 126.8f), Quaternion.Euler(0f, -135f, 0f), _matCrtBlue);

        // Архівні стійки по периметру північної стіни
        for (int a = -3; a <= 3; a++)
        {
            if (a == 0) continue;
            BuildServerMainframeTower($"Z10_CoreArchiveVault_{a}", parent, new Vector3(a * 4.8f, sy, 134.2f), Quaternion.Euler(0f, 180f, 0f), Mathf.Abs(a) % 3);
        }

        // Драматичне освітлення Ядра
        CreateCeilingLamp("Z10_Lamp_Hall1", parent, new Vector3(0f, y + 3.85f, 84f), _matLampCool, new Color(0.75f, 0.90f, 0.90f), 3.6f, 14f);
        CreateCeilingLamp("Z10_Lamp_Hall2", parent, new Vector3(0f, y + 3.85f, 99f), _matLampAmber, new Color(1f, 0.65f, 0.25f), 3.6f, 14f);
        CreateCeilingLamp("Z10_Lamp_CoreCrown", parent, new Vector3(0f, y + 3.5f, 122f), _matLampAmber, new Color(1f, 0.58f, 0.18f), 6.5f, 24f);
        CreateCeilingLamp("Z10_Lamp_CoreAbyss", parent, new Vector3(0f, sy + 2.5f, 122f), _matLampRed, new Color(1f, 0.22f, 0.14f), 5.0f, 18f);
    }


    // =============================================================================================
    // ЗОНА 11: ТЕХНІЧНИЙ ПІДПОВЕРХ (Y = -9.0m) — КАБЕЛЬНІ, ДРЕНАЖНІ ТА ВЕНТИЛЯЦІЙНІ ТУНЕЛІ
    // Обхідна стелс-мережа під усім комплексом: Атріум <-> Західний колектор <-> Східний дренаж <-> Реактор
    // =============================================================================================
    private static void BuildZone11_SubLevelMaintenanceTunnels(Transform parent)
    {
        float sy = SubY;
        RegisterAcousticRoom(parent, "Зона 11: Технічні тунелі (Підповерх)", new Vector3(0f, sy + 1.8f, 65f), AcousticMaterialType.Concrete, AudioReverbPreset.SewerPipe, 30f);

        // 1. Західний поперечний тунель від Атріуму (X: -32 .. -6, Z = 37)
        BuildSubLevelTunnelSegmentX("Z11_Tunnel_WestBranch", parent, -32f, -6f, 37f, sy, 4.0f, 3.6f);

        // 2. Східний поперечний тунель від Атріуму (X: +6 .. +32, Z = 37)
        BuildSubLevelTunnelSegmentX("Z11_Tunnel_EastBranch", parent, 6f, 32f, 37f, sy, 4.0f, 3.6f);

        // 3. Центральний північний магістральний тунель (X = 0, Z: 43 .. 91)
        BuildSubLevelTunnelSegmentZ("Z11_Tunnel_NorthSpine", parent, 0f, 43f, 91f, sy, 4.2f, 3.6f);

        // 4. Західний поздовжній кабельний колектор (X = -32, Z: 37 .. 91)
        BuildSubLevelTunnelSegmentZ("Z11_Tunnel_WestCollector", parent, -32f, 37f, 91f, sy, 3.8f, 3.6f);

        // 5. Північна поперечна збійка до Реакторного залу (X: -32 .. +14, Z = 91)
        BuildSubLevelTunnelSegmentX("Z11_Tunnel_NorthCrossLink", parent, -32f, 14f, 91f, sy, 4.0f, 3.6f);

        // Вузлові камери-розв'язки на перехрестях тунелів (із вентилями, калюжами та щитками)
        BuildSubLevelJunctionChamber("Z11_Junction_WestSouth", parent, new Vector3(-32f, sy, 37f));
        BuildSubLevelJunctionChamber("Z11_Junction_EastDeadEnd", parent, new Vector3(32f, sy, 37f));
        BuildSubLevelJunctionChamber("Z11_Junction_NorthCenter", parent, new Vector3(0f, sy, 91f));
        BuildSubLevelJunctionChamber("Z11_Junction_NorthWest", parent, new Vector3(-32f, sy, 91f));
    }

    private static void BuildSubLevelTunnelSegmentX(string name, Transform parent, float x1, float x2, float zCenter, float baseY, float width, float height)
    {
        GameObject grp = CreateZoneGroup(name, parent);
        float len = Mathf.Abs(x2 - x1);
        float cx = (x1 + x2) * 0.5f;

        CreateAcousticBox("Floor", grp.transform, new Vector3(cx, baseY - 0.15f, zCenter), new Vector3(len, 0.3f, width + 0.6f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Ceiling", grp.transform, new Vector3(cx, baseY + height + 0.15f, zCenter), new Vector3(len, 0.3f, width + 0.6f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Wall_S", grp.transform, new Vector3(cx, baseY + height * 0.5f, zCenter - width * 0.5f), new Vector3(len, height, 0.35f), _matConcreteDamaged, AcousticMaterialType.Concrete);
        CreateAcousticBox("Wall_N", grp.transform, new Vector3(cx, baseY + height * 0.5f, zCenter + width * 0.5f), new Vector3(len, height, 0.35f), _matConcreteDamaged, AcousticMaterialType.Concrete);

        // Труби опалення та бетонні арки через кожні 4.5м
        CreateAcousticBox("Pipe_Run_N", grp.transform, new Vector3(cx, baseY + height - 0.65f, zCenter + width * 0.5f - 0.32f), new Vector3(len, 0.24f, 0.24f), _matCopperPipe, AcousticMaterialType.Metal);
        CreateAcousticBox("Pipe_Run_S", grp.transform, new Vector3(cx, baseY + height - 0.95f, zCenter - width * 0.5f + 0.32f), new Vector3(len, 0.20f, 0.20f), _matRustedSteel, AcousticMaterialType.Metal);

        int ribs = Mathf.Max(2, Mathf.FloorToInt(len / 4.5f));
        for (int i = 0; i <= ribs; i++)
        {
            float rx = Mathf.Lerp(x1 + 1.2f, x2 - 1.2f, i / (float)ribs);
            CreateBox($"Rib_L_{i}", grp.transform, new Vector3(rx, baseY + height * 0.5f, zCenter - width * 0.5f + 0.22f), new Vector3(0.32f, height, 0.22f), _matDarkIron);
            CreateBox($"Rib_R_{i}", grp.transform, new Vector3(rx, baseY + height * 0.5f, zCenter + width * 0.5f - 0.22f), new Vector3(0.32f, height, 0.22f), _matDarkIron);
            CreateBox($"Rib_Top_{i}", grp.transform, new Vector3(rx, baseY + height - 0.14f, zCenter), new Vector3(0.32f, 0.28f, width), _matDarkIron);

            if (i % 2 == 0)
            {
                CreateCeilingLamp($"TunnelLamp_{i}", grp.transform, new Vector3(rx, baseY + height - 0.32f, zCenter), _matLampRed, new Color(1f, 0.36f, 0.18f), 2.4f, 9f);
            }
        }
    }

    private static void BuildSubLevelTunnelSegmentZ(string name, Transform parent, float xCenter, float z1, float z2, float baseY, float width, float height)
    {
        GameObject grp = CreateZoneGroup(name, parent);
        float len = Mathf.Abs(z2 - z1);
        float cz = (z1 + z2) * 0.5f;

        CreateAcousticBox("Floor", grp.transform, new Vector3(xCenter, baseY - 0.15f, cz), new Vector3(width + 0.6f, 0.3f, len), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Ceiling", grp.transform, new Vector3(xCenter, baseY + height + 0.15f, cz), new Vector3(width + 0.6f, 0.3f, len), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Wall_W", grp.transform, new Vector3(xCenter - width * 0.5f, baseY + height * 0.5f, cz), new Vector3(0.35f, height, len), _matConcreteDamaged, AcousticMaterialType.Concrete);
        CreateAcousticBox("Wall_E", grp.transform, new Vector3(xCenter + width * 0.5f, baseY + height * 0.5f, cz), new Vector3(0.35f, height, len), _matConcreteDamaged, AcousticMaterialType.Concrete);

        CreateAcousticBox("Pipe_Run_W", grp.transform, new Vector3(xCenter - width * 0.5f + 0.32f, baseY + height - 0.65f, cz), new Vector3(0.24f, 0.24f, len), _matCopperPipe, AcousticMaterialType.Metal);
        CreateAcousticBox("Pipe_Run_E", grp.transform, new Vector3(xCenter + width * 0.5f - 0.32f, baseY + height - 0.95f, cz), new Vector3(0.20f, 0.20f, len), _matRustedSteel, AcousticMaterialType.Metal);

        // Дренажна решітка по центру підлоги
        CreateBox("DrainGrate_Center", grp.transform, new Vector3(xCenter, baseY + 0.015f, cz), new Vector3(0.65f, 0.02f, len - 1.5f), _matDarkIron);

        int ribs = Mathf.Max(2, Mathf.FloorToInt(len / 4.8f));
        for (int i = 0; i <= ribs; i++)
        {
            float rz = Mathf.Lerp(z1 + 1.2f, z2 - 1.2f, i / (float)ribs);
            CreateBox($"Rib_W_{i}", grp.transform, new Vector3(xCenter - width * 0.5f + 0.22f, baseY + height * 0.5f, rz), new Vector3(0.22f, height, 0.32f), _matDarkIron);
            CreateBox($"Rib_E_{i}", grp.transform, new Vector3(xCenter + width * 0.5f - 0.22f, baseY + height * 0.5f, rz), new Vector3(0.22f, height, 0.32f), _matDarkIron);
            CreateBox($"Rib_Top_{i}", grp.transform, new Vector3(xCenter, baseY + height - 0.14f, rz), new Vector3(width, 0.28f, 0.32f), _matDarkIron);

            if (i % 2 == 0)
            {
                CreateCeilingLamp($"TunnelLamp_{i}", grp.transform, new Vector3(xCenter, baseY + height - 0.32f, rz), _matLampAmber, new Color(1f, 0.55f, 0.18f), 2.5f, 10f);
            }
        }
    }

    private static void BuildSubLevelJunctionChamber(string name, Transform parent, Vector3 pos)
    {
        GameObject j = CreateZoneGroup(name, parent);
        CreateAcousticBox("JunctionFloor", j.transform, pos + new Vector3(0f, -0.15f, 0f), new Vector3(5.6f, 0.3f, 5.6f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("JunctionCeiling", j.transform, pos + new Vector3(0f, 3.75f, 0f), new Vector3(5.6f, 0.3f, 5.6f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateBox("JunctionPuddle", j.transform, pos + new Vector3(0.5f, 0.018f, -0.4f), new Vector3(2.8f, 0.01f, 2.4f), _matWaterPuddle);
        BuildWallFuseBox("JunctionFuseBox", j.transform, pos + new Vector3(2.2f, 1.5f, 2.2f), Quaternion.Euler(0f, -135f, 0f));
        CreateCeilingLamp("JunctionBeacon", j.transform, pos + new Vector3(0f, 3.45f, 0f), _matLampAmber, new Color(1f, 0.65f, 0.2f), 3.0f, 11f);
    }


    // =============================================================================================
    // ДЕТАЛІЗОВАНІ АРХІТЕКТУРНІ ТА ІНТЕР'ЄРНІ ПРЕФАБИ (ЧАСТИНА 1: САНПРОПУСКНИК, АТРІУМ, КАЛІБРУВАННЯ)
    // =============================================================================================
    private static void BuildDecontaminationArch(string name, Transform parent, Vector3 pos)
    {
        GameObject g = CreateZoneGroup(name, parent);
        g.transform.position = pos;

        CreateAcousticBox("Arch_Pillar_L", g.transform, pos + new Vector3(-2.3f, 1.85f, 0f), new Vector3(0.45f, 3.7f, 0.65f), _matMetalBulkhead, AcousticMaterialType.Metal);
        CreateAcousticBox("Arch_Pillar_R", g.transform, pos + new Vector3(2.3f, 1.85f, 0f), new Vector3(0.45f, 3.7f, 0.65f), _matMetalBulkhead, AcousticMaterialType.Metal);
        CreateAcousticBox("Arch_TopBeam", g.transform, pos + new Vector3(0f, 3.55f, 0f), new Vector3(5.1f, 0.45f, 0.72f), _matHazardStripes, AcousticMaterialType.Metal);

        // Форсунки розпилення дезінфекції
        for (int n = -2; n <= 2; n++)
        {
            CreateCylinder($"SprayNozzle_Top_{n}", g.transform, pos + new Vector3(n * 0.8f, 3.25f, 0f), new Vector3(0.12f, 0.14f, 0.12f), _matCopperPipe);
        }
        for (int h = 1; h <= 3; h++)
        {
            GameObject nl = CreateCylinder($"SprayNozzle_L_{h}", g.transform, pos + new Vector3(-2.0f, h * 0.8f, 0f), new Vector3(0.10f, 0.12f, 0.10f), _matCopperPipe);
            nl.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject nr = CreateCylinder($"SprayNozzle_R_{h}", g.transform, pos + new Vector3(2.0f, h * 0.8f, 0f), new Vector3(0.10f, 0.12f, 0.10f), _matCopperPipe);
            nr.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        // Решітка зливу на підлозі під аркою
        CreateBox("Arch_DrainGrate", g.transform, pos + new Vector3(0f, 0.02f, 0f), new Vector3(4.2f, 0.03f, 0.85f), _matDarkIron);
    }

    private static void BuildDecontaminationShowerStall(string name, Transform parent, Vector3 pos)
    {
        GameObject s = CreateZoneGroup(name, parent);
        CreateAcousticBox("Divider_S", s.transform, pos + new Vector3(0f, 1.35f, -1.45f), new Vector3(3.2f, 2.7f, 0.14f), _matCeramicTileWall, AcousticMaterialType.Tile);
        CreateAcousticBox("Divider_N", s.transform, pos + new Vector3(0f, 1.35f, 1.45f), new Vector3(3.2f, 2.7f, 0.14f), _matCeramicTileWall, AcousticMaterialType.Tile);
        CreateBox("ShowerTray", s.transform, pos + new Vector3(0f, 0.06f, 0f), new Vector3(2.9f, 0.12f, 2.7f), _matMedicalWhite);
        CreateCylinder("ShowerPipe_Vert", s.transform, pos + new Vector3(-1.35f, 1.4f, 0f), new Vector3(0.08f, 1.4f, 0.08f), _matCopperPipe);
        CreateCylinder("ShowerHead", s.transform, pos + new Vector3(-1.05f, 2.65f, 0f), new Vector3(0.32f, 0.06f, 0.32f), _matRustedSteel);
        CreateBox("CurtainRod", s.transform, pos + new Vector3(1.45f, 2.45f, 0f), new Vector3(0.05f, 0.05f, 2.8f), _matDarkIron);
        CreateBox("CurtainHalf", s.transform, pos + new Vector3(1.45f, 1.32f, -0.75f), new Vector3(0.04f, 2.2f, 1.1f), _matFabricCurtain);
    }

    private static void BuildSecurityTurnstile(string name, Transform parent, Vector3 pos)
    {
        GameObject t = CreateZoneGroup(name, parent);
        CreateAcousticBox("Turnstile_Body", t.transform, pos + new Vector3(0f, 0.55f, 0f), new Vector3(0.38f, 1.1f, 1.15f), _matMetalBulkhead, AcousticMaterialType.Metal);
        CreateBox("Turnstile_TopPlate", t.transform, pos + new Vector3(0f, 1.12f, 0f), new Vector3(0.42f, 0.05f, 1.2f), _matDarkIron);
        CreateBox("Turnstile_StatusLed", t.transform, pos + new Vector3(0f, 1.15f, -0.45f), new Vector3(0.14f, 0.04f, 0.14f), _matCrtGreen);
        for (int a = 0; a < 3; a++)
        {
            GameObject arm = CreateCylinder($"Arm_{a}", t.transform, pos + new Vector3(0.48f, 0.78f, 0f), new Vector3(0.04f, 0.42f, 0.04f), _matRustedSteel);
            arm.transform.rotation = Quaternion.Euler(a * 35f - 25f, 0f, 90f);
        }
    }

    private static void BuildDirectorySignBoard(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject b = CreateZoneGroup(name, parent);
        b.transform.position = pos;
        b.transform.rotation = rot;

        CreateBox("BoardFrame", b.transform, pos, new Vector3(3.4f, 1.75f, 0.14f), _matDarkIron).transform.localPosition = Vector3.zero;
        CreateBox("BoardBack", b.transform, pos, new Vector3(3.15f, 1.52f, 0.06f), _matRetroBeige).transform.localPosition = new Vector3(0f, 0f, 0.06f);
        CreateBox("HeaderStrip", b.transform, pos, new Vector3(3.05f, 0.28f, 0.04f), _matHazardStripes).transform.localPosition = new Vector3(0f, 0.55f, 0.09f);
        for (int row = 0; row < 4; row++)
        {
            CreateBox($"MapLine_{row}", b.transform, pos, new Vector3(2.5f - row * 0.22f, 0.10f, 0.03f), _matDarkIron).transform.localPosition = new Vector3(-0.15f, 0.22f - row * 0.24f, 0.09f);
        }
    }

    private static void BuildRibbedConcretePillar(string name, Transform parent, Vector3 pos, float height)
    {
        GameObject p = CreateZoneGroup(name, parent);
        CreateAcousticBox("Pillar_Core", p.transform, pos + new Vector3(0f, height * 0.5f, 0f), new Vector3(1.15f, height, 1.15f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Pillar_Base", p.transform, pos + new Vector3(0f, 0.28f, 0f), new Vector3(1.48f, 0.56f, 1.48f), _matConcreteDamaged, AcousticMaterialType.Concrete);
        CreateBox("Pillar_HazardBand", p.transform, pos + new Vector3(0f, 0.72f, 0f), new Vector3(1.22f, 0.24f, 1.22f), _matHazardStripes);
        CreateAcousticBox("Pillar_Capital", p.transform, pos + new Vector3(0f, height - 0.24f, 0f), new Vector3(1.52f, 0.48f, 1.52f), _matDarkIron, AcousticMaterialType.Metal);
    }

    private static void BuildCentralStatusMonolith(string name, Transform parent, Vector3 basePos, float height)
    {
        GameObject m = CreateZoneGroup(name, parent);
        CreateAcousticBox("Monolith_Core", m.transform, basePos + new Vector3(0f, height * 0.5f, 0f), new Vector3(2.4f, height, 2.4f), _matDarkIron, AcousticMaterialType.Metal);
        CreateAcousticBox("Monolith_Pedestal", m.transform, basePos + new Vector3(0f, 0.45f, 0f), new Vector3(3.4f, 0.9f, 3.4f), _matHazardStripes, AcousticMaterialType.Concrete);

        // 12 ЕПТ-екранів по всіх 4 сторонах колони на рівні MainY
        float screenBaseY = MainY + 1.55f;
        for (int face = 0; face < 4; face++)
        {
            float ang = face * 90f;
            Quaternion rot = Quaternion.Euler(0f, ang, 0f);
            Vector3 fwd = rot * Vector3.forward;

            for (int r = 0; r < 3; r++)
            {
                Vector3 scrPos = new Vector3(basePos.x, screenBaseY + r * 0.78f, basePos.z) + fwd * 1.26f;
                GameObject housing = CreateBox($"ScreenHousing_{face}_{r}", m.transform, scrPos, new Vector3(1.65f, 0.64f, 0.16f), _matRetroBeige);
                housing.transform.rotation = rot;

                Material mat = (r == 1) ? _matCrtAmber : _matCrtGreen;
                GameObject glass = CreateBox($"ScreenGlow_{face}_{r}", m.transform, scrPos + fwd * 0.09f, new Vector3(1.48f, 0.50f, 0.02f), mat);
                glass.transform.rotation = rot;
            }
        }
    }

    private static void BuildAtriumReceptionCounter(string name, Transform parent, Vector3 pos)
    {
        GameObject r = CreateZoneGroup(name, parent);
        CreateAcousticBox("Counter_Front", r.transform, pos + new Vector3(0f, 0.55f, -0.6f), new Vector3(5.6f, 1.1f, 0.35f), _matWoodDark, AcousticMaterialType.Wood);
        CreateAcousticBox("Counter_Top", r.transform, pos + new Vector3(0f, 1.12f, -0.2f), new Vector3(5.8f, 0.12f, 1.25f), _matRetroBeige, AcousticMaterialType.Wood);
        CreateAcousticBox("Counter_SideL", r.transform, pos + new Vector3(-2.7f, 0.55f, 0.1f), new Vector3(0.35f, 1.1f, 1.4f), _matWoodDark, AcousticMaterialType.Wood);
        CreateAcousticBox("Counter_SideR", r.transform, pos + new Vector3(2.7f, 0.55f, 0.1f), new Vector3(0.35f, 1.1f, 1.4f), _matWoodDark, AcousticMaterialType.Wood);
        CreateBox("Counter_CRT", r.transform, pos + new Vector3(-1.2f, 1.42f, -0.1f), new Vector3(0.55f, 0.48f, 0.52f), _matRetroBeige);
        CreateBox("Counter_CRT_Scr", r.transform, pos + new Vector3(-1.2f, 1.44f, 0.17f), new Vector3(0.42f, 0.34f, 0.02f), _matCrtGreen);
        CreateBox("Counter_Phone", r.transform, pos + new Vector3(1.4f, 1.24f, -0.15f), new Vector3(0.32f, 0.14f, 0.28f), _matBinderRed);
    }

    private static void BuildConcretePlanterWithTree(string name, Transform parent, Vector3 pos)
    {
        GameObject p = CreateZoneGroup(name, parent);
        CreateAcousticBox("Planter_Pot", p.transform, pos + new Vector3(0f, 0.42f, 0f), new Vector3(1.75f, 0.84f, 1.75f), _matConcreteDamaged, AcousticMaterialType.Concrete);
        CreateBox("Planter_Soil", p.transform, pos + new Vector3(0f, 0.82f, 0f), new Vector3(1.45f, 0.08f, 1.45f), _matWoodDark);
        CreateCylinder("Ficus_Trunk", p.transform, pos + new Vector3(0f, 1.45f, 0f), new Vector3(0.18f, 0.65f, 0.18f), _matWoodOak);
        CreateBox("Ficus_Crown1", p.transform, pos + new Vector3(0f, 2.25f, 0f), new Vector3(1.35f, 0.95f, 1.35f), _matWallLowerGreen);
        CreateBox("Ficus_Crown2", p.transform, pos + new Vector3(0.25f, 2.85f, -0.15f), new Vector3(0.85f, 0.65f, 0.85f), _matWallLowerGreen);
    }

    private static void BuildIndustrialStaircase(string name, Transform parent, Vector3 topPos, Vector3 botPos, float width, int steps)
    {
        GameObject st = CreateZoneGroup(name, parent);
        Vector3 dir = botPos - topPos;
        Vector3 horiz = new Vector3(dir.x, 0f, dir.z);
        Quaternion rot = horiz.sqrMagnitude > 0.001f ? Quaternion.LookRotation(horiz.normalized, Vector3.up) : Quaternion.identity;
        float horizLen = horiz.magnitude;
        float stepDepth = horizLen / steps;
        float stepDrop = (topPos.y - botPos.y) / steps;

        for (int i = 0; i < steps; i++)
        {
            float t = (i + 0.5f) / steps;
            Vector3 stepPos = Vector3.Lerp(topPos, botPos, t);
            stepPos.y = topPos.y - (i + 1) * stepDrop + stepDrop * 0.5f;

            GameObject step = CreateAcousticBox($"Step_{i}", st.transform, stepPos, new Vector3(width, stepDrop, stepDepth + 0.06f), _matDarkIron, AcousticMaterialType.Metal);
            step.transform.rotation = rot;
        }

        // Похилі бічні косоури та перила
        float totalLen = Vector3.Distance(topPos, botPos);
        float pitch = Mathf.Atan2(topPos.y - botPos.y, horizLen) * Mathf.Rad2Deg;
        Vector3 mid = (topPos + botPos) * 0.5f;
        Vector3 right = rot * Vector3.right;

        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 railMid = mid + right * (side * (width * 0.5f + 0.06f)) + new Vector3(0f, 0.95f, 0f);
            GameObject rail = CreateBox($"StairRail_{side}", st.transform, railMid, new Vector3(0.08f, 0.08f, totalLen + 0.4f), _matRustedSteel);
            rail.transform.rotation = rot * Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private static void BuildExpandedCurvedTvArcChamber(string name, Transform parent, Vector3 center)
    {
        GameObject room = CreateZoneGroup(name, parent);
        int cols = 7;
        int rows = 4;
        float radius = 4.6f;

        for (int c = 0; c < cols; c++)
        {
            float t = c / (float)(cols - 1);
            float rad = Mathf.Lerp(-78f, 78f, t) * Mathf.Deg2Rad;
            Vector3 colPos = center + new Vector3(-Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            Quaternion lookRot = Quaternion.LookRotation((center - colPos).normalized, Vector3.up);

            for (int r = 0; r < rows; r++)
            {
                Vector3 tvPos = new Vector3(colPos.x, center.y + 0.55f + r * 0.82f, colPos.z);
                GameObject tvBody = CreateAcousticBox($"TV_Body_{c}_{r}", room.transform, tvPos, new Vector3(0.92f, 0.76f, 0.65f), _matDarkIron, AcousticMaterialType.Metal);
                tvBody.transform.rotation = lookRot;

                Material scr = ((c + r) % 6 == 0) ? _matCrtAmber : (((c * r) % 5 == 0) ? _matCrtStatic : _matCrtGreen);
                GameObject tvScreen = CreateBox($"TV_Screen_{c}_{r}", tvBody.transform, Vector3.zero, Vector3.one, scr);
                tvScreen.transform.localPosition = new Vector3(0f, 0.04f, 0.33f);
                tvScreen.transform.localScale = new Vector3(0.84f, 0.72f, 0.04f);
            }
        }

        // Біле крісло калібрування суб'єкта «Копії» з фіксаторами та кабелями
        GameObject chair = CreateZoneGroup("Subject_Calibration_Chair", room.transform);
        chair.transform.position = center + new Vector3(0.4f, 0f, 0f);
        chair.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        CreateCylinder("Chair_Base", chair.transform, chair.transform.position + new Vector3(0f, 0.08f, 0f), new Vector3(1.1f, 0.08f, 1.1f), _matMedicalWhite);
        CreateCylinder("Chair_Stem", chair.transform, chair.transform.position + new Vector3(0f, 0.38f, 0f), new Vector3(0.36f, 0.36f, 0.36f), _matDarkIron);
        CreateBox("Chair_Seat", chair.transform, chair.transform.position + new Vector3(0f, 0.68f, 0f), new Vector3(0.82f, 0.14f, 0.82f), _matMedicalWhite);
        CreateBox("Chair_Back", chair.transform, chair.transform.position + new Vector3(0f, 1.32f, -0.34f), new Vector3(0.76f, 1.22f, 0.16f), _matMedicalWhite);
        CreateBox("Chair_ArmL", chair.transform, chair.transform.position + new Vector3(-0.44f, 0.92f, 0f), new Vector3(0.12f, 0.12f, 0.78f), _matLeatherDark);
        CreateBox("Chair_ArmR", chair.transform, chair.transform.position + new Vector3(0.44f, 0.92f, 0f), new Vector3(0.12f, 0.12f, 0.78f), _matLeatherDark);
        CreateCylinder("EEG_HaloRing", chair.transform, chair.transform.position + new Vector3(0f, 1.88f, -0.18f), new Vector3(0.55f, 0.08f, 0.55f), _matCopperPipe);
    }


    // =============================================================================================
    // ДЕТАЛІЗОВАНІ ПРЕФАБИ (ЧАСТИНА 2: АКУСТИЧНА ЛАБОРАТОРІЯ, ОФІС, МЕДБЛОК, СЕРВЕРНА, РЕАКТОР, ЯДРО)
    // =============================================================================================
    private static void BuildAnechoicChamberRoom(string name, Transform parent, Vector3 center, float width, float depth)
    {
        GameObject ch = CreateZoneGroup(name, parent);
        float y = center.y;
        RegisterAcousticRoom(ch.transform, "Безлунна камера (AcousticFoam)", center + new Vector3(0f, 1.8f, 0f), AcousticMaterialType.AcousticFoam, AudioReverbPreset.Off, 8f);

        // Зовнішні ізольовані стіни камери (прохід зі сходу на Z = 65.8 +- 1.4)
        BuildSegmentedWallX("Anechoic_Wall_South", ch.transform, center.x - width * 0.5f, center.x + width * 0.5f, center.z - depth * 0.5f, y, WallH, 0.38f, _matAcousticFoamDark, AcousticMaterialType.AcousticFoam);
        BuildSegmentedWallX("Anechoic_Wall_North", ch.transform, center.x - width * 0.5f, center.x + width * 0.5f, center.z + depth * 0.5f, y, WallH, 0.38f, _matAcousticFoamDark, AcousticMaterialType.AcousticFoam);
        BuildSegmentedWallZ("Anechoic_Wall_West", ch.transform, center.x - width * 0.5f, center.z - depth * 0.5f, center.z + depth * 0.5f, y, WallH, 0.38f, _matAcousticFoamDark, AcousticMaterialType.AcousticFoam);
        BuildSegmentedWallZ("Anechoic_Wall_East_S", ch.transform, center.x + width * 0.5f, center.z - depth * 0.5f, center.z - 1.5f, y, WallH, 0.38f, _matAcousticFoamDark, AcousticMaterialType.AcousticFoam);
        BuildSegmentedWallZ("Anechoic_Wall_East_N", ch.transform, center.x + width * 0.5f, center.z + 1.5f, center.z + depth * 0.5f, y, WallH, 0.38f, _matAcousticFoamDark, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Anechoic_DoorHeader", ch.transform, new Vector3(center.x + width * 0.5f, y + 3.45f, center.z), new Vector3(0.42f, 1.1f, 3.2f), _matAcousticFoamDark, AcousticMaterialType.AcousticFoam);

        // 3D-масив акустичних поролонових клинів на західній, північній та південній стінах!
        for (int row = 0; row < 4; row++)
        {
            float wy = y + 0.55f + row * 0.88f;
            for (int i = -5; i <= 5; i++)
            {
                GameObject wedgeW = CreateBox($"FoamWedge_W_{row}_{i}", ch.transform, new Vector3(center.x - width * 0.5f + 0.32f, wy, center.z + i * 1.25f), new Vector3(0.32f, 0.65f, 0.95f), (i + row) % 2 == 0 ? _matAcousticFoam : _matAcousticFoamDark);
                wedgeW.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                Object.DestroyImmediate(wedgeW.GetComponent<Collider>());
            }
            for (int j = -4; j <= 4; j++)
            {
                GameObject wedgeN = CreateBox($"FoamWedge_N_{row}_{j}", ch.transform, new Vector3(center.x + j * 1.18f, wy, center.z + depth * 0.5f - 0.32f), new Vector3(0.92f, 0.65f, 0.32f), (j + row) % 2 == 0 ? _matAcousticFoam : _matAcousticFoamDark);
                wedgeN.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
                Object.DestroyImmediate(wedgeN.GetComponent<Collider>());

                GameObject wedgeS = CreateBox($"FoamWedge_S_{row}_{j}", ch.transform, new Vector3(center.x + j * 1.18f, wy, center.z - depth * 0.5f + 0.32f), new Vector3(0.92f, 0.65f, 0.32f), (j + row) % 2 == 0 ? _matAcousticFoamDark : _matAcousticFoam);
                wedgeS.transform.rotation = Quaternion.Euler(-45f, 0f, 0f);
                Object.DestroyImmediate(wedgeS.GetComponent<Collider>());
            }
        }

        // Підвісна металева сітка підлоги та центральна стійка еталонного мікрофона
        CreateAcousticBox("Anechoic_MeshFloor", ch.transform, new Vector3(center.x, y + 0.04f, center.z), new Vector3(width - 1.2f, 0.06f, depth - 1.2f), _matDarkIron, AcousticMaterialType.Metal);
        CreateCylinder("MicStand_Base", ch.transform, new Vector3(center.x, y + 0.12f, center.z), new Vector3(0.65f, 0.06f, 0.65f), _matDarkIron);
        CreateCylinder("MicStand_Pole", ch.transform, new Vector3(center.x, y + 0.85f, center.z), new Vector3(0.05f, 0.75f, 0.05f), _matRustedSteel);
        CreateBox("Mic_Capsule", ch.transform, new Vector3(center.x, y + 1.62f, center.z), new Vector3(0.14f, 0.24f, 0.14f), _matBrassTrim);

        // 4 направлені тестові колонки по кутах безлунної камери
        BuildStudioMonitorSpeaker("Anechoic_Speaker_1", ch.transform, center + new Vector3(-3.8f, 0f, -4.2f), Quaternion.Euler(0f, 45f, 0f));
        BuildStudioMonitorSpeaker("Anechoic_Speaker_2", ch.transform, center + new Vector3(3.8f, 0f, -4.2f), Quaternion.Euler(0f, -45f, 0f));
        BuildStudioMonitorSpeaker("Anechoic_Speaker_3", ch.transform, center + new Vector3(-3.8f, 0f, 4.2f), Quaternion.Euler(0f, 135f, 0f));
        BuildStudioMonitorSpeaker("Anechoic_Speaker_4", ch.transform, center + new Vector3(3.8f, 0f, 4.2f), Quaternion.Euler(0f, -135f, 0f));
    }

    private static void BuildAcousticMaterialTestBooth(string name, Transform parent, Vector3 pos, AcousticMaterialType matType, Material wallMat, Material indicatorMat)
    {
        GameObject booth = CreateZoneGroup(name, parent);
        float y = pos.y;
        RegisterAcousticRoom(booth.transform, name, pos + new Vector3(0f, 1.5f, 0f), matType, AudioReverbPreset.Room, 3.2f);

        // Бічні стіни кабіни з відповідного фізичного матеріалу (Бетон / Дерево / Метал / Поролон)
        CreateAcousticBox("Wall_West", booth.transform, pos + new Vector3(-1.85f, 1.75f, 0f), new Vector3(0.28f, 3.5f, 4.2f), wallMat, matType);
        CreateAcousticBox("Wall_East", booth.transform, pos + new Vector3(1.85f, 1.75f, 0f), new Vector3(0.28f, 3.5f, 4.2f), wallMat, matType);
        CreateAcousticBox("Wall_BackSouth", booth.transform, pos + new Vector3(0f, 1.75f, -2.05f), new Vector3(3.9f, 3.5f, 0.28f), wallMat, matType);

        // Передня оглядова перегородка зі склом і відкритим входом зліва
        CreateAcousticBox("Front_LowWall", booth.transform, pos + new Vector3(0.65f, 0.55f, 2.05f), new Vector3(2.4f, 1.1f, 0.24f), wallMat, matType);
        CreateAcousticBox("Front_GlassWindow", booth.transform, pos + new Vector3(0.65f, 1.95f, 2.05f), new Vector3(2.3f, 1.7f, 0.08f), _matGlassClear, AcousticMaterialType.Glass);
        CreateAcousticBox("Front_Header", booth.transform, pos + new Vector3(0f, 3.15f, 2.05f), new Vector3(3.9f, 0.7f, 0.28f), wallMat, matType);

        // Кольоровий індикатор типу матеріалу над входом + стійка інтеркому всередині
        CreateBox("MaterialIndicatorSign", booth.transform, pos + new Vector3(0f, 3.15f, 2.22f), new Vector3(1.6f, 0.26f, 0.05f), indicatorMat);
        CreateCylinder("IntercomStand_Pole", booth.transform, pos + new Vector3(0f, 0.68f, -0.4f), new Vector3(0.08f, 0.68f, 0.08f), _matDarkIron);
        CreateBox("IntercomStand_Head", booth.transform, pos + new Vector3(0f, 1.42f, -0.4f), new Vector3(0.38f, 0.26f, 0.28f), _matRetroBeige);
    }

    private static void BuildAnalogMixingConsole(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject m = CreateZoneGroup(name, parent);
        m.transform.position = pos;
        m.transform.rotation = rot;

        CreateAcousticBox("DeskBase", m.transform, pos, new Vector3(4.2f, 0.82f, 1.35f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.41f, 0f);
        GameObject faderPanel = CreateBox("FaderBoard", m.transform, pos, new Vector3(4.1f, 0.14f, 1.15f), _matRetroBeige);
        faderPanel.transform.localPosition = new Vector3(0f, 0.92f, 0.05f);
        faderPanel.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

        // Місток VU-індикаторів
        CreateBox("MeterBridge", m.transform, pos, new Vector3(4.1f, 0.42f, 0.28f), _matDarkIron).transform.localPosition = new Vector3(0f, 1.18f, -0.52f);
        for (int vu = -4; vu <= 4; vu++)
        {
            CreateBox($"VU_Meter_{vu}", m.transform, pos, new Vector3(0.32f, 0.22f, 0.04f), vu % 3 == 0 ? _matCrtAmber : _matCrtGreen).transform.localPosition = new Vector3(vu * 0.42f, 1.18f, -0.37f);
        }
    }

    private static void BuildStudioMonitorSpeaker(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject sp = CreateZoneGroup(name, parent);
        sp.transform.position = pos;
        sp.transform.rotation = rot;

        CreateAcousticBox("SpeakerStand", sp.transform, pos, new Vector3(0.55f, 1.05f, 0.55f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.525f, 0f);
        CreateAcousticBox("SpeakerCabinet", sp.transform, pos, new Vector3(0.68f, 0.92f, 0.62f), _matWoodDark, AcousticMaterialType.Wood).transform.localPosition = new Vector3(0f, 1.52f, 0f);
        GameObject woofer = CreateCylinder("WooferCone", sp.transform, pos, new Vector3(0.42f, 0.04f, 0.42f), _matAcousticFoamDark);
        woofer.transform.localPosition = new Vector3(0f, 1.38f, 0.31f);
        woofer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject tweeter = CreateCylinder("TweeterCone", sp.transform, pos, new Vector3(0.20f, 0.04f, 0.20f), _matBrassTrim);
        tweeter.transform.localPosition = new Vector3(0f, 1.76f, 0.31f);
        tweeter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static void BuildExecutiveDesk(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject d = CreateZoneGroup(name, parent);
        d.transform.position = pos;
        d.transform.rotation = rot;

        CreateAcousticBox("Top", d.transform, pos, new Vector3(2.8f, 0.12f, 1.35f), _matWoodDark, AcousticMaterialType.Wood).transform.localPosition = new Vector3(0f, 0.78f, 0f);
        CreateAcousticBox("Pedestal_L", d.transform, pos, new Vector3(0.75f, 0.74f, 1.15f), _matWoodOak, AcousticMaterialType.Wood).transform.localPosition = new Vector3(-0.92f, 0.37f, 0f);
        CreateAcousticBox("Pedestal_R", d.transform, pos, new Vector3(0.75f, 0.74f, 1.15f), _matWoodOak, AcousticMaterialType.Wood).transform.localPosition = new Vector3(0.92f, 0.37f, 0f);
        CreateAcousticBox("FrontModesty", d.transform, pos, new Vector3(1.2f, 0.68f, 0.12f), _matWoodDark, AcousticMaterialType.Wood).transform.localPosition = new Vector3(0f, 0.42f, 0.52f);
        CreateBox("DeskLamp_Base", d.transform, pos, new Vector3(0.24f, 0.08f, 0.24f), _matBrassTrim).transform.localPosition = new Vector3(0.95f, 0.88f, 0.32f);
        CreateBox("DeskLamp_Shade", d.transform, pos, new Vector3(0.36f, 0.18f, 0.24f), _matWallLowerGreen).transform.localPosition = new Vector3(0.95f, 1.24f, 0.32f);
        CreateBox("RedPhone", d.transform, pos, new Vector3(0.28f, 0.14f, 0.24f), _matBinderRed).transform.localPosition = new Vector3(-0.85f, 0.90f, 0.28f);
        CreateBox("DossierFolder", d.transform, pos, new Vector3(0.42f, 0.04f, 0.55f), _matBinderYellow).transform.localPosition = new Vector3(0.1f, 0.86f, 0.05f);
    }

    private static void BuildHeavySafe(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject s = CreateZoneGroup(name, parent);
        s.transform.position = pos;
        s.transform.rotation = rot;

        CreateAcousticBox("SafeBody", s.transform, pos, new Vector3(1.15f, 1.65f, 0.95f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.825f, 0f);
        GameObject dial = CreateCylinder("SafeWheel", s.transform, pos, new Vector3(0.32f, 0.08f, 0.32f), _matBrassTrim);
        dial.transform.localPosition = new Vector3(0.18f, 0.95f, 0.50f);
        dial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static void BuildWoodenBookcaseWithBinders(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject bc = CreateZoneGroup(name, parent);
        bc.transform.position = pos;
        bc.transform.rotation = rot;

        CreateAcousticBox("Back", bc.transform, pos, new Vector3(2.15f, 2.55f, 0.10f), _matWoodDark, AcousticMaterialType.Wood).transform.localPosition = new Vector3(0f, 1.275f, -0.25f);
        CreateAcousticBox("SideL", bc.transform, pos, new Vector3(0.10f, 2.55f, 0.55f), _matWoodOak, AcousticMaterialType.Wood).transform.localPosition = new Vector3(-1.05f, 1.275f, 0f);
        CreateAcousticBox("SideR", bc.transform, pos, new Vector3(0.10f, 2.55f, 0.55f), _matWoodOak, AcousticMaterialType.Wood).transform.localPosition = new Vector3(1.05f, 1.275f, 0f);

        Material[] binderMats = { _matBinderRed, _matBinderBlue, _matBinderGreen, _matBinderYellow };
        for (int sh = 0; sh < 5; sh++)
        {
            float sy = 0.15f + sh * 0.58f;
            CreateBox($"Shelf_{sh}", bc.transform, pos, new Vector3(2.05f, 0.06f, 0.52f), _matWoodOak).transform.localPosition = new Vector3(0f, sy, 0f);
            if (sh < 4)
            {
                for (int bk = -3; bk <= 3; bk++)
                {
                    if ((sh + bk) % 5 == 0) continue;
                    GameObject b = CreateBox($"Binder_{sh}_{bk}", bc.transform, pos, new Vector3(0.18f, 0.38f, 0.34f), binderMats[Mathf.Abs(sh * 3 + bk) % binderMats.Length]);
                    b.transform.localPosition = new Vector3(bk * 0.25f, sy + 0.22f, 0.04f);
                    Object.DestroyImmediate(b.GetComponent<Collider>());
                }
            }
        }
    }

    private static void BuildConferenceTableWithMics(string name, Transform parent, Vector3 pos, float length, float width)
    {
        GameObject t = CreateZoneGroup(name, parent);
        CreateAcousticBox("ConfTop", t.transform, pos + new Vector3(0f, 0.78f, 0f), new Vector3(length, 0.12f, width), _matWoodDark, AcousticMaterialType.Wood);
        CreateAcousticBox("ConfBaseL", t.transform, pos + new Vector3(-length * 0.32f, 0.38f, 0f), new Vector3(0.65f, 0.74f, width * 0.65f), _matWoodOak, AcousticMaterialType.Wood);
        CreateAcousticBox("ConfBaseR", t.transform, pos + new Vector3(length * 0.32f, 0.38f, 0f), new Vector3(0.65f, 0.74f, width * 0.65f), _matWoodOak, AcousticMaterialType.Wood);

        for (int m = -2; m <= 2; m++)
        {
            CreateBox($"TableMic_{m}", t.transform, pos + new Vector3(m * 1.45f, 0.90f, 0f), new Vector3(0.16f, 0.14f, 0.16f), _matDarkIron);
        }
    }

    private static void BuildKitchenCounterAndFridge(string name, Transform parent, Vector3 pos)
    {
        GameObject k = CreateZoneGroup(name, parent);
        CreateAcousticBox("CounterBody", k.transform, pos + new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.9f, 5.2f), _matRetroBeige, AcousticMaterialType.Wood);
        CreateBox("CounterTop", k.transform, pos + new Vector3(0f, 0.92f, 0f), new Vector3(1.15f, 0.06f, 5.3f), _matDarkIron);
        CreateBox("SinkBasin", k.transform, pos + new Vector3(0f, 0.95f, -1.2f), new Vector3(0.65f, 0.04f, 0.95f), _matMetalBulkhead);
        CreateAcousticBox("SovietFridge", k.transform, pos + new Vector3(0f, 1.05f, 3.4f), new Vector3(1.05f, 2.1f, 1.15f), _matMedicalWhite, AcousticMaterialType.Metal);
        CreateBox("FridgeHandle", k.transform, pos + new Vector3(-0.55f, 1.25f, 3.1f), new Vector3(0.08f, 0.45f, 0.08f), _matDarkIron);
    }

    private static void BuildMedicalExaminationBay(string name, Transform parent, Vector3 pos)
    {
        GameObject bay = CreateZoneGroup(name, parent);
        CreateAcousticBox("BedFrame", bay.transform, pos + new Vector3(0.6f, 0.42f, 0f), new Vector3(2.3f, 0.24f, 1.05f), _matMetalBulkhead, AcousticMaterialType.Metal);
        CreateBox("BedMattress", bay.transform, pos + new Vector3(0.6f, 0.62f, 0f), new Vector3(2.2f, 0.18f, 0.98f), _matMedicalWhite);
        CreateBox("BedPillow", bay.transform, pos + new Vector3(1.45f, 0.75f, 0f), new Vector3(0.45f, 0.12f, 0.72f), _matWallUpperCream);
        CreateCylinder("IV_Stand", bay.transform, pos + new Vector3(1.4f, 1.05f, 0.85f), new Vector3(0.05f, 1.05f, 0.05f), _matRustedSteel);
        CreateBox("VitalMonitor_Cart", bay.transform, pos + new Vector3(1.3f, 0.55f, -1.05f), new Vector3(0.65f, 1.1f, 0.55f), _matRetroBeige);
        CreateBox("VitalMonitor_Screen", bay.transform, pos + new Vector3(0.96f, 1.28f, -1.05f), new Vector3(0.04f, 0.34f, 0.42f), _matCrtGreen);
        CreateCylinder("SurgicalLamp_Dish", bay.transform, pos + new Vector3(0.6f, 2.75f, 0f), new Vector3(0.85f, 0.12f, 0.85f), _matLampCool);
    }

    private static void BuildNeuralScannerPod(string name, Transform parent, Vector3 pos)
    {
        GameObject pod = CreateZoneGroup(name, parent);
        CreateAcousticBox("ScannerBase", pod.transform, pos + new Vector3(0f, 0.42f, 0f), new Vector3(2.4f, 0.84f, 4.4f), _matMedicalWhite, AcousticMaterialType.Metal);
        GameObject ring = CreateCylinder("ScannerRing", pod.transform, pos + new Vector3(0f, 1.45f, -0.6f), new Vector3(2.35f, 0.75f, 2.35f), _matMedicalWhite);
        ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        CreateBox("ScannerBed", pod.transform, pos + new Vector3(0f, 0.92f, 0.8f), new Vector3(0.95f, 0.14f, 2.8f), _matLeatherDark);
        CreateBox("ScannerStatusStrip", pod.transform, pos + new Vector3(0f, 2.25f, 0.18f), new Vector3(1.4f, 0.14f, 0.08f), _matCrtBlue);
    }

    private static void BuildCryoStoragePod(string name, Transform parent, Vector3 pos)
    {
        GameObject cp = CreateZoneGroup(name, parent);
        CreateCylinder("CryoBase", cp.transform, pos + new Vector3(0f, 0.25f, 0f), new Vector3(1.45f, 0.25f, 1.45f), _matDarkIron);
        CreateCylinder("CryoGlassTube", cp.transform, pos + new Vector3(0f, 1.45f, 0f), new Vector3(1.18f, 1.15f, 1.18f), _matGlassFrosted);
        CreateCylinder("CryoCap", cp.transform, pos + new Vector3(0f, 2.72f, 0f), new Vector3(1.48f, 0.22f, 1.48f), _matMetalBulkhead);
        CreateBox("CryoPanel", cp.transform, pos + new Vector3(0f, 1.1f, -0.78f), new Vector3(0.45f, 0.32f, 0.12f), _matCrtBlue);
    }

    private static void BuildMedicalCabinet(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject c = CreateZoneGroup(name, parent);
        c.transform.position = pos;
        c.transform.rotation = rot;

        CreateAcousticBox("CabLower", c.transform, pos, new Vector3(1.45f, 0.95f, 0.58f), _matMedicalWhite, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.475f, 0f);
        CreateAcousticBox("CabUpperGlass", c.transform, pos, new Vector3(1.42f, 1.25f, 0.48f), _matGlassClear, AcousticMaterialType.Glass).transform.localPosition = new Vector3(0f, 1.58f, 0f);
        CreateBox("CabTop", c.transform, pos, new Vector3(1.48f, 0.12f, 0.55f), _matMedicalWhite).transform.localPosition = new Vector3(0f, 2.25f, 0f);
    }

    private static void BuildXRayLightBoxWall(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject x = CreateZoneGroup(name, parent);
        x.transform.position = pos;
        x.transform.rotation = rot;

        CreateBox("XRayFrame", x.transform, pos, new Vector3(3.2f, 1.1f, 0.14f), _matDarkIron).transform.localPosition = Vector3.zero;
        for (int i = -1; i <= 1; i++)
        {
            CreateBox($"XRayPane_{i}", x.transform, pos, new Vector3(0.88f, 0.86f, 0.04f), _matLampCool).transform.localPosition = new Vector3(i * 1.0f, 0f, 0.08f);
        }
    }

    private static void BuildCctvMonitorWall(string name, Transform parent, Vector3 pos, Quaternion rot, int cols, int rows)
    {
        GameObject w = CreateZoneGroup(name, parent);
        w.transform.position = pos;
        w.transform.rotation = rot;

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                float lx = (c - (cols - 1) * 0.5f) * 0.98f;
                float ly = 1.25f + r * 0.72f;
                CreateBox($"CctvBox_{c}_{r}", w.transform, pos, new Vector3(0.88f, 0.62f, 0.42f), _matDarkIron).transform.localPosition = new Vector3(lx, ly, 0f);
                Material mat = ((c + r) % 4 == 0) ? _matCrtStatic : (((c * r) % 3 == 0) ? _matCrtAmber : _matCrtGreen);
                CreateBox($"CctvScr_{c}_{r}", w.transform, pos, new Vector3(0.74f, 0.48f, 0.03f), mat).transform.localPosition = new Vector3(lx, ly, 0.22f);
            }
        }
    }

    private static void BuildSecurityScannerGate(string name, Transform parent, Vector3 pos)
    {
        GameObject g = CreateZoneGroup(name, parent);
        CreateAcousticBox("Pylon_L", g.transform, pos + new Vector3(-2.4f, 1.35f, 0f), new Vector3(0.45f, 2.7f, 0.75f), _matMetalBulkhead, AcousticMaterialType.Metal);
        CreateAcousticBox("Pylon_R", g.transform, pos + new Vector3(2.4f, 1.35f, 0f), new Vector3(0.45f, 2.7f, 0.75f), _matMetalBulkhead, AcousticMaterialType.Metal);
        CreateAcousticBox("TopArch", g.transform, pos + new Vector3(0f, 2.85f, 0f), new Vector3(5.3f, 0.38f, 0.8f), _matHazardStripes, AcousticMaterialType.Metal);
        CreateBox("StatusLed_L", g.transform, pos + new Vector3(-2.4f, 2.4f, -0.39f), new Vector3(0.22f, 0.22f, 0.04f), _matLampRed);
        CreateBox("StatusLed_R", g.transform, pos + new Vector3(2.4f, 2.4f, -0.39f), new Vector3(0.22f, 0.22f, 0.04f), _matLampRed);
    }

    private static void BuildServerMainframeTower(string name, Transform parent, Vector3 pos, Quaternion rot, int variant)
    {
        GameObject srv = CreateZoneGroup(name, parent);
        srv.transform.position = pos;
        srv.transform.rotation = rot;

        CreateAcousticBox("CabinetBody", srv.transform, pos, new Vector3(2.6f, 2.75f, 1.25f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 1.375f, 0f);
        CreateBox("FrontPanel_Top", srv.transform, pos, new Vector3(2.35f, 0.95f, 0.08f), _matRetroBeige).transform.localPosition = new Vector3(0f, 2.15f, 0.62f);

        // Дві магнітні бобіни у верхній частині шафи ЕС-ЕОМ
        GameObject reelL = CreateCylinder("TapeReel_L", srv.transform, pos, new Vector3(0.62f, 0.06f, 0.62f), _matRustedSteel);
        reelL.transform.localPosition = new Vector3(-0.55f, 2.18f, 0.68f);
        reelL.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject reelR = CreateCylinder("TapeReel_R", srv.transform, pos, new Vector3(0.62f, 0.06f, 0.62f), _matRustedSteel);
        reelR.transform.localPosition = new Vector3(0.55f, 2.18f, 0.68f);
        reelR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Material ledMat = variant == 0 ? _matCrtGreen : (variant == 1 ? _matCrtAmber : _matCrtRed);
        CreateBox("LedStrip", srv.transform, pos, new Vector3(2.1f, 0.16f, 0.06f), ledMat).transform.localPosition = new Vector3(0f, 1.48f, 0.64f);
    }

    private static void BuildReactorTurbineTower(string name, Transform parent, Vector3 pos)
    {
        GameObject t = CreateZoneGroup(name, parent);
        CreateAcousticBox("TurbinePedestal", t.transform, pos + new Vector3(0f, 0.65f, 0f), new Vector3(4.2f, 1.3f, 4.2f), _matHazardStripes, AcousticMaterialType.Concrete);
        CreateCylinder("TurbineCore", t.transform, pos + new Vector3(0f, 4.2f, 0f), new Vector3(3.1f, 3.6f, 3.1f), _matMetalBulkhead);
        for (int r = 0; r < 4; r++)
        {
            CreateCylinder($"CoolingRing_{r}", t.transform, pos + new Vector3(0f, 2.0f + r * 1.45f, 0f), new Vector3(3.65f, 0.22f, 3.65f), _matCopperPipe);
        }
        CreateCylinder("TopExhaust", t.transform, pos + new Vector3(0f, 7.8f, 0f), new Vector3(1.8f, 0.8f, 1.8f), _matDarkIron);
    }

    private static void BuildNeuralCoreMonument(string name, Transform parent, Vector3 basePos)
    {
        GameObject c = CreateZoneGroup(name, parent);
        CreateCylinder("CoreShaft_Inner", c.transform, basePos + new Vector3(0f, 4.3f, 0f), new Vector3(1.6f, 4.3f, 1.6f), _matLampAmber);
        CreateCylinder("CoreGlassShell", c.transform, basePos + new Vector3(0f, 4.3f, 0f), new Vector3(2.85f, 4.2f, 2.85f), _matGlassClear);
        for (int ring = 0; ring < 5; ring++)
        {
            CreateCylinder($"MatrixRing_{ring}", c.transform, basePos + new Vector3(0f, 1.2f + ring * 1.6f, 0f), new Vector3(3.6f, 0.22f, 3.6f), ring % 2 == 0 ? _matDarkIron : _matCopperPipe);
        }
    }

    private static void BuildEndingChoiceTerminal(string name, Transform parent, Vector3 pos, Quaternion rot, Material screenMat)
    {
        GameObject t = CreateZoneGroup(name, parent);
        t.transform.position = pos;
        t.transform.rotation = rot;

        CreateAcousticBox("Pedestal", t.transform, pos, new Vector3(1.25f, 1.15f, 0.95f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.575f, 0f);
        GameObject hood = CreateBox("MonitorHood", t.transform, pos, new Vector3(1.15f, 0.78f, 0.65f), _matRetroBeige);
        hood.transform.localPosition = new Vector3(0f, 1.52f, -0.10f);
        CreateBox("ChoiceScreen", t.transform, pos, new Vector3(0.95f, 0.58f, 0.04f), screenMat).transform.localPosition = new Vector3(0f, 1.54f, 0.24f);
        CreateBox("ConfirmButton", t.transform, pos, new Vector3(0.32f, 0.08f, 0.22f), screenMat).transform.localPosition = new Vector3(0f, 1.18f, 0.28f);
    }


    // =============================================================================================
    // БАЗОВІ ІНТЕР'ЄРНІ МЕБЛІ, СТІНИ, СКЛО, АКУСТИЧНІ ТЕГИ ТА ПРОЦЕДУРНІ ТЕКСТУРИ
    // =============================================================================================
    private static void BuildRetroWorkstationDesk(string name, Transform parent, Vector3 pos, Quaternion rot, bool withMonitor)
    {
        GameObject d = CreateZoneGroup(name, parent);
        d.transform.position = pos;
        d.transform.rotation = rot;

        CreateAcousticBox("DeskTop", d.transform, pos, new Vector3(2.1f, 0.08f, 0.95f), _matWoodOak, AcousticMaterialType.Wood).transform.localPosition = new Vector3(0f, 0.76f, 0f);
        CreateAcousticBox("LegL", d.transform, pos, new Vector3(0.45f, 0.72f, 0.82f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(-0.78f, 0.36f, 0f);
        CreateAcousticBox("LegR", d.transform, pos, new Vector3(0.10f, 0.72f, 0.82f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0.92f, 0.36f, 0f);

        if (withMonitor)
        {
            CreateBox("PC_Case", d.transform, pos, new Vector3(0.55f, 0.16f, 0.45f), _matRetroBeige).transform.localPosition = new Vector3(0.15f, 0.88f, -0.12f);
            CreateBox("PC_Monitor", d.transform, pos, new Vector3(0.52f, 0.46f, 0.46f), _matRetroBeige).transform.localPosition = new Vector3(0.15f, 1.18f, -0.14f);
            CreateBox("PC_Screen", d.transform, pos, new Vector3(0.40f, 0.32f, 0.03f), _matCrtGreen).transform.localPosition = new Vector3(0.15f, 1.20f, 0.10f);
            CreateBox("PC_Keyboard", d.transform, pos, new Vector3(0.52f, 0.04f, 0.22f), _matRetroBeige).transform.localPosition = new Vector3(0.15f, 0.82f, 0.24f);
        }
    }

    private static void BuildOscilloscopeConsoleDesk(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject c = CreateZoneGroup(name, parent);
        c.transform.position = pos;
        c.transform.rotation = rot;

        CreateAcousticBox("Base", c.transform, pos, new Vector3(2.8f, 0.82f, 1.05f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.41f, 0f);
        CreateBox("UpperRack", c.transform, pos, new Vector3(2.75f, 0.72f, 0.55f), _matRetroBeige).transform.localPosition = new Vector3(0f, 1.18f, -0.22f);
        CreateBox("Scope_L", c.transform, pos, new Vector3(0.68f, 0.44f, 0.04f), _matCrtGreen).transform.localPosition = new Vector3(-0.78f, 1.22f, 0.06f);
        CreateBox("Scope_M", c.transform, pos, new Vector3(0.68f, 0.44f, 0.04f), _matCrtAmber).transform.localPosition = new Vector3(0f, 1.22f, 0.06f);
        CreateBox("Scope_R", c.transform, pos, new Vector3(0.68f, 0.44f, 0.04f), _matCrtGreen).transform.localPosition = new Vector3(0.78f, 1.22f, 0.06f);
    }

    private static void BuildLongLabTable(string name, Transform parent, Vector3 pos, Quaternion rot, float length)
    {
        GameObject t = CreateZoneGroup(name, parent);
        t.transform.position = pos;
        t.transform.rotation = rot;

        CreateAcousticBox("Top", t.transform, pos, new Vector3(length, 0.10f, 1.25f), _matMedicalWhite, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.82f, 0f);
        CreateBox("LegL", t.transform, pos, new Vector3(0.12f, 0.78f, 1.12f), _matDarkIron).transform.localPosition = new Vector3(-length * 0.45f, 0.39f, 0f);
        CreateBox("LegR", t.transform, pos, new Vector3(0.12f, 0.78f, 1.12f), _matDarkIron).transform.localPosition = new Vector3(length * 0.45f, 0.39f, 0f);
        CreateBox("LowerShelf", t.transform, pos, new Vector3(length * 0.9f, 0.06f, 1.05f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.26f, 0f);
    }

    private static void BuildOfficeChair(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject ch = CreateZoneGroup(name, parent);
        ch.transform.position = pos;
        ch.transform.rotation = rot;

        CreateCylinder("Base", ch.transform, pos, new Vector3(0.58f, 0.06f, 0.58f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.06f, 0f);
        CreateCylinder("Stem", ch.transform, pos, new Vector3(0.08f, 0.24f, 0.08f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.28f, 0f);
        CreateBox("Seat", ch.transform, pos, new Vector3(0.56f, 0.10f, 0.54f), _matLeatherDark).transform.localPosition = new Vector3(0f, 0.52f, 0f);
        CreateBox("Back", ch.transform, pos, new Vector3(0.54f, 0.58f, 0.10f), _matLeatherDark).transform.localPosition = new Vector3(0f, 0.84f, -0.24f);
    }

    private static void BuildMetalLocker(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject l = CreateZoneGroup(name, parent);
        l.transform.position = pos;
        l.transform.rotation = rot;

        CreateAcousticBox("LockerBody", l.transform, pos, new Vector3(0.92f, 2.25f, 0.65f), _matWallLowerGreen, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 1.125f, 0f);
        CreateBox("VentSlits", l.transform, pos, new Vector3(0.55f, 0.22f, 0.04f), _matDarkIron).transform.localPosition = new Vector3(0f, 1.88f, 0.33f);
    }

    private static void BuildMetalFilingCabinet(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject f = CreateZoneGroup(name, parent);
        f.transform.position = pos;
        f.transform.rotation = rot;

        CreateAcousticBox("Body", f.transform, pos, new Vector3(0.65f, 1.55f, 0.72f), _matRetroBeige, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.775f, 0f);
        for (int d = 0; d < 4; d++)
        {
            CreateBox($"DrawerHandle_{d}", f.transform, pos, new Vector3(0.24f, 0.06f, 0.05f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.26f + d * 0.36f, 0.37f);
        }
    }

    private static void BuildLockerBench(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject b = CreateZoneGroup(name, parent);
        b.transform.position = pos;
        b.transform.rotation = rot;

        CreateAcousticBox("Plank", b.transform, pos, new Vector3(2.8f, 0.08f, 0.52f), _matWoodOak, AcousticMaterialType.Wood).transform.localPosition = new Vector3(0f, 0.48f, 0f);
        CreateBox("LegL", b.transform, pos, new Vector3(0.10f, 0.46f, 0.46f), _matDarkIron).transform.localPosition = new Vector3(-1.15f, 0.23f, 0f);
        CreateBox("LegR", b.transform, pos, new Vector3(0.10f, 0.46f, 0.46f), _matDarkIron).transform.localPosition = new Vector3(1.15f, 0.23f, 0f);
    }

    private static void BuildRetroTerminalKiosk(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject k = CreateZoneGroup(name, parent);
        k.transform.position = pos;
        k.transform.rotation = rot;

        CreateAcousticBox("KioskBase", k.transform, pos, new Vector3(1.05f, 1.15f, 0.85f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.575f, 0f);
        CreateBox("KioskTop", k.transform, pos, new Vector3(0.98f, 0.72f, 0.62f), _matRetroBeige).transform.localPosition = new Vector3(0f, 1.48f, -0.08f);
        CreateBox("KioskScreen", k.transform, pos, new Vector3(0.78f, 0.52f, 0.04f), _matCrtAmber).transform.localPosition = new Vector3(0f, 1.50f, 0.24f);
    }

    private static void BuildRetroMainframeCabinet(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject m = CreateZoneGroup(name, parent);
        m.transform.position = pos;
        m.transform.rotation = rot;

        CreateAcousticBox("Base", m.transform, pos, new Vector3(2.2f, 2.35f, 0.92f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 1.175f, 0f);
        CreateBox("ControlFace", m.transform, pos, new Vector3(2.0f, 0.65f, 0.06f), _matRetroBeige).transform.localPosition = new Vector3(0f, 1.65f, 0.48f);
        CreateBox("StatusLamp", m.transform, pos, new Vector3(1.65f, 0.14f, 0.04f), _matCrtGreen).transform.localPosition = new Vector3(0f, 1.82f, 0.52f);
    }

    private static void BuildTapeReelStand(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject t = CreateZoneGroup(name, parent);
        t.transform.position = pos;
        t.transform.rotation = rot;

        CreateAcousticBox("Stand", t.transform, pos, new Vector3(1.25f, 2.1f, 0.75f), _matRetroBeige, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 1.05f, 0f);
        GameObject r1 = CreateCylinder("Reel1", t.transform, pos, new Vector3(0.48f, 0.06f, 0.48f), _matDarkIron);
        r1.transform.localPosition = new Vector3(-0.30f, 1.62f, 0.40f);
        r1.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject r2 = CreateCylinder("Reel2", t.transform, pos, new Vector3(0.48f, 0.06f, 0.48f), _matDarkIron);
        r2.transform.localPosition = new Vector3(0.30f, 1.62f, 0.40f);
        r2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static void BuildRollingLabCart(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject c = CreateZoneGroup(name, parent);
        c.transform.position = pos;
        c.transform.rotation = rot;

        CreateBox("TopTray", c.transform, pos, new Vector3(1.05f, 0.06f, 0.65f), _matMetalBulkhead).transform.localPosition = new Vector3(0f, 0.88f, 0f);
        CreateBox("BotTray", c.transform, pos, new Vector3(1.05f, 0.06f, 0.65f), _matMetalBulkhead).transform.localPosition = new Vector3(0f, 0.32f, 0f);
        CreateBox("OscilloBox", c.transform, pos, new Vector3(0.48f, 0.34f, 0.42f), _matRetroBeige).transform.localPosition = new Vector3(-0.18f, 1.08f, 0f);
        CreateBox("OscilloScr", c.transform, pos, new Vector3(0.28f, 0.20f, 0.03f), _matCrtGreen).transform.localPosition = new Vector3(-0.18f, 1.08f, 0.22f);
    }

    private static void BuildArchiveShelvingUnit(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject r = CreateZoneGroup(name, parent);
        r.transform.position = pos;
        r.transform.rotation = rot;

        for (int s = 0; s < 4; s++)
        {
            CreateAcousticBox($"Shelf_{s}", r.transform, pos, new Vector3(2.4f, 0.06f, 0.72f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.35f + s * 0.62f, 0f);
        }
        CreateBox("Post_FL", r.transform, pos, new Vector3(0.08f, 2.4f, 0.08f), _matDarkIron).transform.localPosition = new Vector3(-1.16f, 1.2f, -0.32f);
        CreateBox("Post_FR", r.transform, pos, new Vector3(0.08f, 2.4f, 0.08f), _matDarkIron).transform.localPosition = new Vector3(1.16f, 1.2f, -0.32f);
        CreateBox("Post_BL", r.transform, pos, new Vector3(0.08f, 2.4f, 0.08f), _matDarkIron).transform.localPosition = new Vector3(-1.16f, 1.2f, 0.32f);
        CreateBox("Post_BR", r.transform, pos, new Vector3(0.08f, 2.4f, 0.08f), _matDarkIron).transform.localPosition = new Vector3(1.16f, 1.2f, 0.32f);
    }

    private static void BuildLabWhiteboard(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject wb = CreateZoneGroup(name, parent);
        wb.transform.position = pos;
        wb.transform.rotation = rot;

        CreateBox("Frame", wb.transform, pos, new Vector3(3.4f, 1.65f, 0.10f), _matDarkIron).transform.localPosition = new Vector3(0f, 1.95f, 0f);
        CreateBox("Board", wb.transform, pos, new Vector3(3.2f, 1.48f, 0.04f), _matMedicalWhite).transform.localPosition = new Vector3(0f, 1.95f, 0.06f);
    }

    private static void BuildWallFuseBox(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject fb = CreateZoneGroup(name, parent);
        fb.transform.position = pos;
        fb.transform.rotation = rot;

        CreateAcousticBox("Box", fb.transform, pos, new Vector3(0.62f, 0.85f, 0.22f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = Vector3.zero;
        CreateBox("Hazard", fb.transform, pos, new Vector3(0.42f, 0.22f, 0.03f), _matHazardStripes).transform.localPosition = new Vector3(0f, 0.16f, 0.12f);
    }

    private static void BuildCeilingUtilityRun(string name, Transform parent, Vector3 center, float length, bool alongX)
    {
        GameObject u = CreateZoneGroup(name, parent);
        Vector3 ductScale = alongX ? new Vector3(length, 0.42f, 0.78f) : new Vector3(0.78f, 0.42f, length);
        Vector3 pipeScale = alongX ? new Vector3(length, 0.18f, 0.18f) : new Vector3(0.18f, 0.18f, length);
        Vector3 offset = alongX ? new Vector3(0f, -0.05f, 0.62f) : new Vector3(0.62f, -0.05f, 0f);

        CreateAcousticBox("HVAC_Duct", u.transform, center, ductScale, _matRustedSteel, AcousticMaterialType.Metal);
        CreateAcousticBox("PowerPipe", u.transform, center + offset, pipeScale, _matCopperPipe, AcousticMaterialType.Metal);
    }

    private static void BuildGlassObservationPartition(string name, Transform parent, Vector3 center, float length, float height, bool alongZ)
    {
        GameObject g = CreateZoneGroup(name, parent);
        float y = center.y;
        float lowH = 1.15f;
        float glassH = 1.85f;
        float topH = height - lowH - glassH;

        if (alongZ)
        {
            CreateAcousticBox("LowWall", g.transform, new Vector3(center.x, y + lowH * 0.5f, center.z), new Vector3(0.32f, lowH, length), _matWallLowerGreen, AcousticMaterialType.Concrete);
            CreateAcousticBox("TopWall", g.transform, new Vector3(center.x, y + lowH + glassH + topH * 0.5f, center.z), new Vector3(0.32f, topH, length), _matWallUpperCream, AcousticMaterialType.Concrete);
            CreateAcousticBox("Sill", g.transform, new Vector3(center.x, y + lowH + 0.04f, center.z), new Vector3(0.44f, 0.08f, length), _matDarkIron, AcousticMaterialType.Metal);
            CreateAcousticBox("TemperedGlass", g.transform, new Vector3(center.x, y + lowH + glassH * 0.5f, center.z), new Vector3(0.08f, glassH, length - 0.1f), _matGlassClear, AcousticMaterialType.Glass);
        }
        else
        {
            CreateAcousticBox("LowWall", g.transform, new Vector3(center.x, y + lowH * 0.5f, center.z), new Vector3(length, lowH, 0.32f), _matWallLowerGreen, AcousticMaterialType.Concrete);
            CreateAcousticBox("TopWall", g.transform, new Vector3(center.x, y + lowH + glassH + topH * 0.5f, center.z), new Vector3(length, topH, 0.32f), _matWallUpperCream, AcousticMaterialType.Concrete);
            CreateAcousticBox("Sill", g.transform, new Vector3(center.x, y + lowH + 0.04f, center.z), new Vector3(length, 0.08f, 0.44f), _matDarkIron, AcousticMaterialType.Metal);
            CreateAcousticBox("TemperedGlass", g.transform, new Vector3(center.x, y + lowH + glassH * 0.5f, center.z), new Vector3(length - 0.1f, glassH, 0.08f), _matGlassClear, AcousticMaterialType.Glass);
        }
    }

    private static void BuildSafetyRailingSegment(string name, Transform parent, Vector3 start, Vector3 end)
    {
        GameObject r = CreateZoneGroup(name, parent);
        float len = Vector3.Distance(start, end);
        if (len < 0.2f) return;

        Vector3 mid = (start + end) * 0.5f;
        Quaternion rot = Quaternion.LookRotation((end - start).normalized, Vector3.up);

        GameObject topBar = CreateBox("TopRail", r.transform, mid + new Vector3(0f, 1.05f, 0f), new Vector3(0.07f, 0.07f, len), _matHazardStripes);
        topBar.transform.rotation = rot;
        GameObject midBar = CreateBox("MidRail", r.transform, mid + new Vector3(0f, 0.55f, 0f), new Vector3(0.05f, 0.05f, len), _matDarkIron);
        midBar.transform.rotation = rot;

        int posts = Mathf.Max(2, Mathf.CeilToInt(len / 2.6f) + 1);
        for (int i = 0; i < posts; i++)
        {
            Vector3 p = Vector3.Lerp(start, end, i / (float)(posts - 1));
            CreateBox($"Post_{i}", r.transform, p + new Vector3(0f, 0.525f, 0f), new Vector3(0.08f, 1.05f, 0.08f), _matDarkIron);
        }
    }

    private static void BuildSegmentedWallX(string name, Transform parent, float x1, float x2, float z, float baseY, float height, float thickness, Material mat, AcousticMaterialType acoustic)
    {
        float len = Mathf.Abs(x2 - x1);
        if (len < 0.05f) return;
        float cx = (x1 + x2) * 0.5f;
        CreateAcousticBox(name, parent, new Vector3(cx, baseY + height * 0.5f, z), new Vector3(len, height, thickness), mat, acoustic);
    }

    private static void BuildSegmentedWallZ(string name, Transform parent, float x, float z1, float z2, float baseY, float height, float thickness, Material mat, AcousticMaterialType acoustic)
    {
        float len = Mathf.Abs(z2 - z1);
        if (len < 0.05f) return;
        float cz = (z1 + z2) * 0.5f;
        CreateAcousticBox(name, parent, new Vector3(x, baseY + height * 0.5f, cz), new Vector3(thickness, height, len), mat, acoustic);
    }

    private static void RegisterAcousticRoom(Transform parent, string roomTitle, Vector3 center, AcousticMaterialType dominantMat, AudioReverbPreset preset, float radius)
    {
        GameObject zoneObj = new GameObject("AcousticZone_" + roomTitle);
        zoneObj.transform.SetParent(parent);
        zoneObj.transform.position = center;

        AcousticRoomZone info = zoneObj.AddComponent<AcousticRoomZone>();
        info.roomName = roomTitle;
        info.dominantMaterial = dominantMat;
        info.reverbPreset = preset;
        info.roomRadius = radius;

        AudioReverbZone reverb = zoneObj.AddComponent<AudioReverbZone>();
        reverb.reverbPreset = preset;
        reverb.minDistance = radius * 0.55f;
        reverb.maxDistance = radius;
    }

    private static GameObject CreateAcousticBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, AcousticMaterialType acousticType)
    {
        GameObject obj = CreateBox(name, parent, pos, scale, mat);
        AcousticSurface surface = obj.AddComponent<AcousticSurface>();
        surface.ApplyPreset(acousticType);
        return obj;
    }

    private static GameObject CreateCeilingLamp(string name, Transform parent, Vector3 pos, Material emissiveMat, Color lightColor, float intensity, float range)
    {
        GameObject fixture = CreateBox(name, parent, pos, new Vector3(1.15f, 0.14f, 0.32f), emissiveMat);
        Object.DestroyImmediate(fixture.GetComponent<Collider>());

        GameObject lightObj = new GameObject(name + "_PointLight");
        lightObj.transform.SetParent(fixture.transform);
        lightObj.transform.localPosition = new Vector3(0f, -0.35f, 0f);

        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor;
        l.intensity = intensity;
        l.range = range;
        return fixture;
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

    private static GameObject CreateDecorBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject box = CreateBox(name, parent, pos, scale, mat);
        Object.DestroyImmediate(box.GetComponent<Collider>());
        Renderer r = box.GetComponent<Renderer>();
        if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return box;
    }

    private static GameObject CreateDecorCylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject cyl = CreateCylinder(name, parent, pos, scale, mat);
        Object.DestroyImmediate(cyl.GetComponent<Collider>());
        Renderer r = cyl.GetComponent<Renderer>();
        if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return cyl;
    }

    private static GameObject CreateZoneGroup(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent);
        return g;
    }

    private static void DestroyExistingByName(string objName)
    {
        GameObject existing = GameObject.Find(objName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }
    }

    private static void PlaceIfFound(string objName, Vector3 pos, Quaternion rot)
    {
        GameObject obj = GameObject.Find(objName);
        if (obj != null)
        {
            Undo.RecordObject(obj.transform, "Relocate Underground Gameplay Object");
            obj.transform.position = pos;
            obj.transform.rotation = rot;
        }
    }


    // =============================================================================================
    // ДЕТАЛІЗАЦІЯ ІНТЕР'ЄРУ ТА ОТОЧЕННЯ ДЛЯ КОЖНОЇ З 11 ЗОН (ENVIRONMENTAL STORYTELLING PASS)
    // Кожна зона отримує унікальний набір інженерних вузлів, комунікацій, щитків, меблів та декору
    // =============================================================================================

    private static void BuildAllZonesEnvironmentalStorytelling(Transform root)
    {
        GameObject detailRoot = CreateZoneGroup("12_Environmental_Storytelling_Details", root);
        BuildDetails_Zone01_Reception(CreateZoneGroup("Details_Zone01_Reception", detailRoot.transform).transform);
        BuildDetails_Zone02_Atrium(CreateZoneGroup("Details_Zone02_Atrium", detailRoot.transform).transform);
        BuildDetails_Zone03_Calibration(CreateZoneGroup("Details_Zone03_Calibration", detailRoot.transform).transform);
        BuildDetails_Zone04_Acoustics(CreateZoneGroup("Details_Zone04_Acoustics", detailRoot.transform).transform);
        BuildDetails_Zone05_Administration(CreateZoneGroup("Details_Zone05_Administration", detailRoot.transform).transform);
        BuildDetails_Zone06_MedicalBay(CreateZoneGroup("Details_Zone06_MedicalBay", detailRoot.transform).transform);
        BuildDetails_Zone07_Security(CreateZoneGroup("Details_Zone07_Security", detailRoot.transform).transform);
        BuildDetails_Zone08_ServerHall(CreateZoneGroup("Details_Zone08_ServerHall", detailRoot.transform).transform);
        BuildDetails_Zone09_Reactor(CreateZoneGroup("Details_Zone09_Reactor", detailRoot.transform).transform);
        BuildDetails_Zone10_CentralCore(CreateZoneGroup("Details_Zone10_CentralCore", detailRoot.transform).transform);
        BuildDetails_Zone11_SubTunnels(CreateZoneGroup("Details_Zone11_SubTunnels", detailRoot.transform).transform);
    }

    private static void BuildFireExtinguisherCabinet(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject g = CreateZoneGroup(name, parent);
        g.transform.position = pos;
        g.transform.rotation = rot;

        CreateAcousticBox("Case", g.transform, pos, new Vector3(0.48f, 0.85f, 0.24f), _matBinderRed, AcousticMaterialType.Metal).transform.localPosition = Vector3.zero;
        CreateBox("GlassFront", g.transform, pos, new Vector3(0.36f, 0.65f, 0.03f), _matGlassClear).transform.localPosition = new Vector3(0f, 0f, 0.12f);
        CreateCylinder("Tank", g.transform, pos, new Vector3(0.18f, 0.30f, 0.18f), _matBinderRed).transform.localPosition = new Vector3(0f, -0.05f, 0.02f);
        CreateBox("Nozzle", g.transform, pos, new Vector3(0.08f, 0.12f, 0.08f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.28f, 0.02f);
    }

    private static void BuildWallCctvCamera(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject c = CreateZoneGroup(name, parent);
        c.transform.position = pos;
        c.transform.rotation = rot;

        CreateBox("MountArm", c.transform, pos, new Vector3(0.10f, 0.10f, 0.38f), _matDarkIron).transform.localPosition = new Vector3(0f, 0f, 0.18f);
        GameObject body = CreateBox("CameraHousing", c.transform, pos, new Vector3(0.22f, 0.20f, 0.44f), _matRetroBeige);
        body.transform.localPosition = new Vector3(0f, -0.06f, 0.42f);
        body.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
        GameObject led = CreateBox("RecLed", body.transform, Vector3.zero, Vector3.one, _matLampRed);
        led.transform.localPosition = new Vector3(0.28f, 0.28f, 0.52f);
        led.transform.localScale = new Vector3(0.18f, 0.18f, 0.08f);
    }

    private static void BuildWallClockSoviet(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject cl = CreateZoneGroup(name, parent);
        cl.transform.position = pos;
        cl.transform.rotation = rot;

        GameObject rim = CreateCylinder("ClockRim", cl.transform, pos, new Vector3(0.58f, 0.05f, 0.58f), _matDarkIron);
        rim.transform.localPosition = Vector3.zero;
        rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject face = CreateCylinder("ClockFace", cl.transform, pos, new Vector3(0.50f, 0.06f, 0.50f), _matMedicalWhite);
        face.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        CreateBox("HourHand", cl.transform, pos, new Vector3(0.03f, 0.16f, 0.02f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.06f, 0.05f);
        CreateBox("MinHand", cl.transform, pos, new Vector3(0.20f, 0.025f, 0.02f), _matBinderRed).transform.localPosition = new Vector3(0.08f, 0f, 0.055f);
    }

    private static void BuildWallRadiator(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject r = CreateZoneGroup(name, parent);
        r.transform.position = pos;
        r.transform.rotation = rot;

        for (int i = -4; i <= 4; i++)
        {
            GameObject fin = CreateBox($"Fin_{i}", r.transform, pos, new Vector3(0.08f, 0.62f, 0.16f), _matRustedSteel);
            fin.transform.localPosition = new Vector3(i * 0.14f, 0.45f, 0f);
            Object.DestroyImmediate(fin.GetComponent<Collider>());
        }
        CreateAcousticBox("RadiatorBodyCollider", r.transform, pos, new Vector3(1.32f, 0.64f, 0.18f), _matRustedSteel, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.45f, 0f);
    }

    private static void BuildEmergencyIntercomWallUnit(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject u = CreateZoneGroup(name, parent);
        u.transform.position = pos;
        u.transform.rotation = rot;

        CreateAcousticBox("IntercomBox", u.transform, pos, new Vector3(0.36f, 0.52f, 0.14f), _matHazardStripes, AcousticMaterialType.Metal).transform.localPosition = Vector3.zero;
        CreateBox("SpeakerGrille", u.transform, pos, new Vector3(0.24f, 0.18f, 0.03f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.10f, 0.08f);
        CreateBox("CallButton", u.transform, pos, new Vector3(0.10f, 0.10f, 0.04f), _matLampRed).transform.localPosition = new Vector3(0f, -0.12f, 0.08f);
    }

    private static void BuildPipeValveManifold(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject v = CreateZoneGroup(name, parent);
        v.transform.position = pos;
        v.transform.rotation = rot;

        for (int p = -1; p <= 1; p++)
        {
            CreateCylinder($"VertPipe_{p}", v.transform, pos, new Vector3(0.16f, 1.85f, 0.16f), p == 0 ? _matCopperPipe : _matRustedSteel).transform.localPosition = new Vector3(p * 0.42f, 1.85f, 0f);
            GameObject wheel = CreateCylinder($"ValveWheel_{p}", v.transform, pos, new Vector3(0.34f, 0.05f, 0.34f), _matBinderRed);
            wheel.transform.localPosition = new Vector3(p * 0.42f, 1.35f + p * 0.15f, 0.16f);
            wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private static void BuildVentilationGrille(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject vg = CreateZoneGroup(name, parent);
        vg.transform.position = pos;
        vg.transform.rotation = rot;

        CreateAcousticBox("VentFrame", vg.transform, pos, new Vector3(1.15f, 0.65f, 0.10f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = Vector3.zero;
        for (int s = -2; s <= 2; s++)
        {
            GameObject slat = CreateBox($"Slat_{s}", vg.transform, pos, new Vector3(1.02f, 0.05f, 0.06f), _matRustedSteel);
            slat.transform.localPosition = new Vector3(0f, s * 0.10f, 0.04f);
            slat.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            Object.DestroyImmediate(slat.GetComponent<Collider>());
        }
    }

    private static void BuildAcousticCeilingBaffle(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject b = CreateZoneGroup(name, parent);
        b.transform.position = pos;
        b.transform.rotation = rot;

        CreateAcousticBox("BafflePanel", b.transform, pos, new Vector3(2.4f, 0.55f, 0.12f), _matAcousticFoam, AcousticMaterialType.AcousticFoam).transform.localPosition = Vector3.zero;
        CreateCylinder("WireL", b.transform, pos, new Vector3(0.02f, 0.25f, 0.02f), _matDarkIron).transform.localPosition = new Vector3(-0.9f, 0.48f, 0f);
        CreateCylinder("WireR", b.transform, pos, new Vector3(0.02f, 0.25f, 0.02f), _matDarkIron).transform.localPosition = new Vector3(0.9f, 0.48f, 0f);
    }

    private static void BuildIndustrialCrateStack(string name, Transform parent, Vector3 pos, float yaw)
    {
        GameObject cs = CreateZoneGroup(name, parent);
        cs.transform.position = pos;
        cs.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateAcousticBox("Crate_Base1", cs.transform, pos, new Vector3(1.15f, 0.92f, 1.15f), _matWoodPlywood, AcousticMaterialType.Wood).transform.localPosition = new Vector3(-0.62f, 0.46f, 0f);
        CreateAcousticBox("Crate_Base2", cs.transform, pos, new Vector3(1.05f, 0.85f, 1.05f), _matWoodOak, AcousticMaterialType.Wood).transform.localPosition = new Vector3(0.62f, 0.425f, 0.12f);
        GameObject top = CreateAcousticBox("Crate_Top", cs.transform, pos, new Vector3(0.88f, 0.78f, 0.88f), _matWoodPlywood, AcousticMaterialType.Wood);
        top.transform.localPosition = new Vector3(-0.15f, 1.31f, 0.05f);
        top.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
    }

    private static void BuildCableSpoolAndMat(string name, Transform parent, Vector3 pos, float yaw)
    {
        GameObject sp = CreateZoneGroup(name, parent);
        sp.transform.position = pos;
        sp.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateCylinder("SpoolCore", sp.transform, pos, new Vector3(0.75f, 0.45f, 0.75f), _matCopperPipe).transform.localPosition = new Vector3(0f, 0.45f, 0f);
        CreateCylinder("FlangeBot", sp.transform, pos, new Vector3(1.15f, 0.06f, 1.15f), _matWoodOak).transform.localPosition = new Vector3(0f, 0.06f, 0f);
        CreateCylinder("FlangeTop", sp.transform, pos, new Vector3(1.15f, 0.06f, 1.15f), _matWoodOak).transform.localPosition = new Vector3(0f, 0.88f, 0f);
        CreateBox("FloorCableProtector", sp.transform, pos, new Vector3(0.45f, 0.05f, 3.2f), _matHazardStripes).transform.localPosition = new Vector3(1.1f, 0.025f, 0.4f);
    }

    private static void BuildHazmatDrumCluster(string name, Transform parent, Vector3 pos)
    {
        GameObject d = CreateZoneGroup(name, parent);
        CreateCylinder("Drum_1", d.transform, pos + new Vector3(0f, 0.56f, 0f), new Vector3(0.68f, 0.56f, 0.68f), _matBinderRed);
        CreateCylinder("Drum_2", d.transform, pos + new Vector3(0.78f, 0.56f, 0.25f), new Vector3(0.68f, 0.56f, 0.68f), _matHazardStripes);
        CreateCylinder("Drum_3", d.transform, pos + new Vector3(0.28f, 0.56f, 0.82f), new Vector3(0.68f, 0.56f, 0.68f), _matDarkIron);
    }

    private static void BuildPatchPanelRack(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject p = CreateZoneGroup(name, parent);
        p.transform.position = pos;
        p.transform.rotation = rot;

        CreateAcousticBox("RackFrame", p.transform, pos, new Vector3(1.15f, 2.35f, 0.68f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 1.175f, 0f);
        for (int u = 0; u < 6; u++)
        {
            Material led = (u % 3 == 0) ? _matCrtAmber : _matCrtGreen;
            CreateBox($"Unit_{u}", p.transform, pos, new Vector3(0.98f, 0.24f, 0.06f), _matRetroBeige).transform.localPosition = new Vector3(0f, 0.42f + u * 0.32f, 0.35f);
            CreateBox($"UnitLed_{u}", p.transform, pos, new Vector3(0.72f, 0.05f, 0.03f), led).transform.localPosition = new Vector3(0f, 0.46f + u * 0.32f, 0.39f);
        }
    }

    private static void BuildWallWarningBollard(string name, Transform parent, Vector3 pos)
    {
        GameObject b = CreateZoneGroup(name, parent);
        CreateCylinder("BollardPost", b.transform, pos + new Vector3(0f, 0.52f, 0f), new Vector3(0.22f, 0.52f, 0.22f), _matHazardStripes);
        CreateCylinder("BollardCap", b.transform, pos + new Vector3(0f, 1.06f, 0f), new Vector3(0.24f, 0.04f, 0.24f), _matDarkIron);
    }

    private static void BuildClipboardCheckStation(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject st = CreateZoneGroup(name, parent);
        st.transform.position = pos;
        st.transform.rotation = rot;

        CreateAcousticBox("StandPost", st.transform, pos, new Vector3(0.14f, 1.15f, 0.14f), _matDarkIron, AcousticMaterialType.Metal).transform.localPosition = new Vector3(0f, 0.575f, 0f);
        GameObject lect = CreateBox("LecternTop", st.transform, pos, new Vector3(0.68f, 0.06f, 0.52f), _matWoodOak);
        lect.transform.localPosition = new Vector3(0f, 1.18f, 0.05f);
        lect.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
        GameObject paper = CreateBox("LogSheet", lect.transform, Vector3.zero, Vector3.one, _matMedicalWhite);
        paper.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        paper.transform.localScale = new Vector3(0.72f, 0.35f, 0.72f);
    }

    private static void BuildCeilingPublicAddressSpeaker(string name, Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject pa = CreateZoneGroup(name, parent);
        pa.transform.position = pos;
        pa.transform.rotation = rot;

        CreateBox("Mount", pa.transform, pos, new Vector3(0.14f, 0.28f, 0.14f), _matDarkIron).transform.localPosition = new Vector3(0f, 0.14f, 0f);
        GameObject horn = CreateBox("HornBody", pa.transform, pos, new Vector3(0.46f, 0.32f, 0.38f), _matRetroBeige);
        horn.transform.localPosition = new Vector3(0f, -0.05f, 0.14f);
        horn.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
    }

    private static void BuildDetails_Zone01_Reception(Transform parent)
    {
        float y = MainY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone01_Reception
        BuildFireExtinguisherCabinet("Zone01_Reception_FireCab_W_0", parent, new Vector3(-10.45f, y + 1.45f, 4.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone01_Reception_FireCab_E_0", parent, new Vector3(10.45f, y + 1.45f, 4.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone01_Reception_FireCab_W_1", parent, new Vector3(-10.45f, y + 1.45f, 11.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone01_Reception_FireCab_E_1", parent, new Vector3(10.45f, y + 1.45f, 11.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone01_Reception_FireCab_W_2", parent, new Vector3(-10.45f, y + 1.45f, 17.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone01_Reception_FireCab_E_2", parent, new Vector3(10.45f, y + 1.45f, 17.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone01_Reception_CCTV_0", parent, new Vector3(-10.55f, y + 3.35f, 2.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone01_Reception_CCTV_1", parent, new Vector3(10.55f, y + 3.35f, 2.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone01_Reception_CCTV_2", parent, new Vector3(-10.55f, y + 3.35f, 19.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone01_Reception_CCTV_3", parent, new Vector3(10.55f, y + 3.35f, 19.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone01_Reception_CCTV_4", parent, new Vector3(-3.5f, y + 3.35f, 2.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone01_Reception_CCTV_5", parent, new Vector3(3.5f, y + 3.35f, 19.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone01_Reception_Intercom_W_0", parent, new Vector3(-10.58f, y + 1.52f, 5.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone01_Reception_Intercom_E_0", parent, new Vector3(10.58f, y + 1.52f, 5.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone01_Reception_Clock_W_0", parent, new Vector3(-10.6f, y + 2.85f, 5.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone01_Reception_Clock_E_0", parent, new Vector3(10.6f, y + 2.85f, 5.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone01_Reception_Intercom_W_1", parent, new Vector3(-10.58f, y + 1.52f, 9.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone01_Reception_Intercom_E_1", parent, new Vector3(10.58f, y + 1.52f, 9.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone01_Reception_Intercom_W_2", parent, new Vector3(-10.58f, y + 1.52f, 13.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone01_Reception_Intercom_E_2", parent, new Vector3(10.58f, y + 1.52f, 13.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone01_Reception_Clock_W_2", parent, new Vector3(-10.6f, y + 2.85f, 13.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone01_Reception_Clock_E_2", parent, new Vector3(10.6f, y + 2.85f, 13.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone01_Reception_Intercom_W_3", parent, new Vector3(-10.58f, y + 1.52f, 17.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone01_Reception_Intercom_E_3", parent, new Vector3(10.58f, y + 1.52f, 17.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_W_0", parent, new Vector3(-10.55f, y, 4.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_E_0", parent, new Vector3(10.55f, y, 4.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_W_1", parent, new Vector3(-10.55f, y, 8.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_E_1", parent, new Vector3(10.55f, y, 8.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_W_2", parent, new Vector3(-10.55f, y, 13.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_E_2", parent, new Vector3(10.55f, y, 13.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_W_3", parent, new Vector3(-10.55f, y, 18.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_E_3", parent, new Vector3(10.55f, y, 18.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_W_4", parent, new Vector3(-10.55f, y, 22.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_E_4", parent, new Vector3(10.55f, y, 22.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_W_5", parent, new Vector3(-10.55f, y, 27.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone01_Reception_Radiator_E_5", parent, new Vector3(10.55f, y, 27.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_W_0", parent, new Vector3(-10.58f, y + 3.15f, 4.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_E_0", parent, new Vector3(10.58f, y + 3.15f, 4.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_W_0", parent, new Vector3(-9.75f, y + 3.45f, 4.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_E_0", parent, new Vector3(9.75f, y + 3.45f, 4.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_W_1", parent, new Vector3(-10.58f, y + 3.15f, 8.73f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_E_1", parent, new Vector3(10.58f, y + 3.15f, 8.73f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_W_1", parent, new Vector3(-9.75f, y + 3.45f, 8.73f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_E_1", parent, new Vector3(9.75f, y + 3.45f, 8.73f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_W_2", parent, new Vector3(-10.58f, y + 3.15f, 13.27f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_E_2", parent, new Vector3(10.58f, y + 3.15f, 13.27f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_W_2", parent, new Vector3(-9.75f, y + 3.45f, 13.27f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_E_2", parent, new Vector3(9.75f, y + 3.45f, 13.27f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_W_3", parent, new Vector3(-10.58f, y + 3.15f, 17.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_E_3", parent, new Vector3(10.58f, y + 3.15f, 17.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_W_3", parent, new Vector3(-9.75f, y + 3.45f, 17.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_E_3", parent, new Vector3(9.75f, y + 3.45f, 17.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_W_4", parent, new Vector3(-10.58f, y + 3.15f, 22.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_E_4", parent, new Vector3(10.58f, y + 3.15f, 22.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_W_4", parent, new Vector3(-9.75f, y + 3.45f, 22.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_E_4", parent, new Vector3(9.75f, y + 3.45f, 22.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_W_5", parent, new Vector3(-10.58f, y + 3.15f, 26.87f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone01_Reception_Vent_E_5", parent, new Vector3(10.58f, y + 3.15f, 26.87f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_W_5", parent, new Vector3(-9.75f, y + 3.45f, 26.87f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone01_Reception_PA_E_5", parent, new Vector3(9.75f, y + 3.45f, 26.87f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone01_Reception_ValveManifold_S_0", parent, new Vector3(-8.0f, y, 2.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone01_Reception_ValveManifold_N_0", parent, new Vector3(-8.0f, y, 19.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone01_Reception_FuseBox_S_0", parent, new Vector3(-6.8f, y + 1.65f, 2.45f), Quaternion.identity);
        BuildWallFuseBox("Zone01_Reception_FuseBox_N_0", parent, new Vector3(-9.2f, y + 1.65f, 19.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone01_Reception_ValveManifold_S_1", parent, new Vector3(-2.67f, y, 2.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone01_Reception_ValveManifold_N_1", parent, new Vector3(-2.67f, y, 19.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone01_Reception_FuseBox_S_1", parent, new Vector3(-1.47f, y + 1.65f, 2.45f), Quaternion.identity);
        BuildWallFuseBox("Zone01_Reception_FuseBox_N_1", parent, new Vector3(-3.87f, y + 1.65f, 19.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone01_Reception_ValveManifold_S_2", parent, new Vector3(2.67f, y, 2.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone01_Reception_ValveManifold_N_2", parent, new Vector3(2.67f, y, 19.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone01_Reception_FuseBox_S_2", parent, new Vector3(3.87f, y + 1.65f, 2.45f), Quaternion.identity);
        BuildWallFuseBox("Zone01_Reception_FuseBox_N_2", parent, new Vector3(1.47f, y + 1.65f, 19.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone01_Reception_ValveManifold_S_3", parent, new Vector3(8.0f, y, 2.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone01_Reception_ValveManifold_N_3", parent, new Vector3(8.0f, y, 19.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone01_Reception_FuseBox_S_3", parent, new Vector3(9.2f, y + 1.65f, 2.45f), Quaternion.identity);
        BuildWallFuseBox("Zone01_Reception_FuseBox_N_3", parent, new Vector3(6.8f, y + 1.65f, 19.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_L_0", parent, new Vector3(-4.2f, y + 3.48f, 5.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_R_0", parent, new Vector3(4.2f, y + 3.48f, 5.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_L_1", parent, new Vector3(-4.2f, y + 3.48f, 8.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_R_1", parent, new Vector3(4.2f, y + 3.48f, 8.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_L_2", parent, new Vector3(-4.2f, y + 3.48f, 11.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_R_2", parent, new Vector3(4.2f, y + 3.48f, 11.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_L_3", parent, new Vector3(-4.2f, y + 3.48f, 14.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_R_3", parent, new Vector3(4.2f, y + 3.48f, 14.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_L_4", parent, new Vector3(-4.2f, y + 3.48f, 17.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone01_Reception_Baffle_R_4", parent, new Vector3(4.2f, y + 3.48f, 17.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone01_Reception_Crates_SW", parent, new Vector3(-9.4f, y, 3.6f), 14f);
        BuildCableSpoolAndMat("Zone01_Reception_Spool_SE", parent, new Vector3(9.4f, y, 3.6f), -28f);
        BuildHazmatDrumCluster("Zone01_Reception_Drums_NW", parent, new Vector3(-9.4f, y, 18.4f));
        BuildPatchPanelRack("Zone01_Reception_PatchRack_NE", parent, new Vector3(9.4f, y, 18.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone01_Reception_CheckStation_S_0", parent, new Vector3(-8.6f, y, 3.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone01_Reception_Bollard_S_0", parent, new Vector3(-9.45f, y, 2.95f));
        BuildWallWarningBollard("Zone01_Reception_Bollard_N_0", parent, new Vector3(-7.75f, y, 19.05f));
        BuildClipboardCheckStation("Zone01_Reception_CheckStation_S_1", parent, new Vector3(-2.87f, y, 3.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone01_Reception_Bollard_S_1", parent, new Vector3(-3.72f, y, 2.95f));
        BuildWallWarningBollard("Zone01_Reception_Bollard_N_1", parent, new Vector3(-2.02f, y, 19.05f));
        BuildClipboardCheckStation("Zone01_Reception_CheckStation_S_2", parent, new Vector3(2.87f, y, 3.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone01_Reception_Bollard_S_2", parent, new Vector3(2.02f, y, 2.95f));
        BuildWallWarningBollard("Zone01_Reception_Bollard_N_2", parent, new Vector3(3.72f, y, 19.05f));
        BuildClipboardCheckStation("Zone01_Reception_CheckStation_S_3", parent, new Vector3(8.6f, y, 3.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone01_Reception_Bollard_S_3", parent, new Vector3(7.75f, y, 2.95f));
        BuildWallWarningBollard("Zone01_Reception_Bollard_N_3", parent, new Vector3(9.45f, y, 19.05f));
        CreateAcousticBox("Zone01_Reception_Pilaster_W_0", parent, new Vector3(-10.75f, y + 2.0f, 3.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone01_Reception_Pilaster_E_0", parent, new Vector3(10.75f, y + 2.0f, 3.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone01_Reception_ConduitTrim_W_0", parent, new Vector3(-10.62f, y + 0.32f, 3.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_ConduitTrim_E_0", parent, new Vector3(10.62f, y + 0.32f, 3.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone01_Reception_Pilaster_W_1", parent, new Vector3(-10.75f, y + 2.0f, 7.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone01_Reception_Pilaster_E_1", parent, new Vector3(10.75f, y + 2.0f, 7.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone01_Reception_ConduitTrim_W_1", parent, new Vector3(-10.62f, y + 0.32f, 7.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_ConduitTrim_E_1", parent, new Vector3(10.62f, y + 0.32f, 7.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone01_Reception_Pilaster_W_2", parent, new Vector3(-10.75f, y + 2.0f, 11.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone01_Reception_Pilaster_E_2", parent, new Vector3(10.75f, y + 2.0f, 11.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone01_Reception_ConduitTrim_W_2", parent, new Vector3(-10.62f, y + 0.32f, 11.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_ConduitTrim_E_2", parent, new Vector3(10.62f, y + 0.32f, 11.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone01_Reception_Pilaster_W_3", parent, new Vector3(-10.75f, y + 2.0f, 14.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone01_Reception_Pilaster_E_3", parent, new Vector3(10.75f, y + 2.0f, 14.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone01_Reception_ConduitTrim_W_3", parent, new Vector3(-10.62f, y + 0.32f, 14.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_ConduitTrim_E_3", parent, new Vector3(10.62f, y + 0.32f, 14.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone01_Reception_Pilaster_W_4", parent, new Vector3(-10.75f, y + 2.0f, 18.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone01_Reception_Pilaster_E_4", parent, new Vector3(10.75f, y + 2.0f, 18.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone01_Reception_ConduitTrim_W_4", parent, new Vector3(-10.62f, y + 0.32f, 18.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_ConduitTrim_E_4", parent, new Vector3(10.62f, y + 0.32f, 18.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingRib_0", parent, new Vector3(0.0f, y + 3.82f, 3.5f), new Vector3(20.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_L_0", parent, new Vector3(-5.82f, y + 3.68f, 3.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_R_0", parent, new Vector3(5.82f, y + 3.68f, 3.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CeilingRib_1", parent, new Vector3(0.0f, y + 3.82f, 7.25f), new Vector3(20.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_L_1", parent, new Vector3(-5.82f, y + 3.68f, 7.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_R_1", parent, new Vector3(5.82f, y + 3.68f, 7.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CeilingRib_2", parent, new Vector3(0.0f, y + 3.82f, 11.0f), new Vector3(20.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_L_2", parent, new Vector3(-5.82f, y + 3.68f, 11.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_R_2", parent, new Vector3(5.82f, y + 3.68f, 11.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CeilingRib_3", parent, new Vector3(0.0f, y + 3.82f, 14.75f), new Vector3(20.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_L_3", parent, new Vector3(-5.82f, y + 3.68f, 14.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_R_3", parent, new Vector3(5.82f, y + 3.68f, 14.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CeilingRib_4", parent, new Vector3(0.0f, y + 3.82f, 18.5f), new Vector3(20.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_L_4", parent, new Vector3(-5.82f, y + 3.68f, 18.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CableTrayHanger_R_4", parent, new Vector3(5.82f, y + 3.68f, 18.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_FloorSafetyLine_S_0", parent, new Vector3(-8.2f, y + 0.014f, 3.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_FloorSafetyLine_N_0", parent, new Vector3(-8.2f, y + 0.014f, 18.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_WallDataPlaque_S_0", parent, new Vector3(-8.2f, y + 2.25f, 2.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_WallDataPlaque_N_0", parent, new Vector3(-8.2f, y + 2.25f, 19.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_FloorSafetyLine_S_1", parent, new Vector3(-2.73f, y + 0.014f, 3.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_FloorSafetyLine_N_1", parent, new Vector3(-2.73f, y + 0.014f, 18.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_WallDataPlaque_S_1", parent, new Vector3(-2.73f, y + 2.25f, 2.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_WallDataPlaque_N_1", parent, new Vector3(-2.73f, y + 2.25f, 19.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_FloorSafetyLine_S_2", parent, new Vector3(2.73f, y + 0.014f, 3.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_FloorSafetyLine_N_2", parent, new Vector3(2.73f, y + 0.014f, 18.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_WallDataPlaque_S_2", parent, new Vector3(2.73f, y + 2.25f, 2.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_WallDataPlaque_N_2", parent, new Vector3(2.73f, y + 2.25f, 19.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_FloorSafetyLine_S_3", parent, new Vector3(8.2f, y + 0.014f, 3.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_FloorSafetyLine_N_3", parent, new Vector3(8.2f, y + 0.014f, 18.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_WallDataPlaque_S_3", parent, new Vector3(8.2f, y + 2.25f, 2.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_WallDataPlaque_N_3", parent, new Vector3(8.2f, y + 2.25f, 19.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_AcousticSensor_W_0", parent, new Vector3(-10.58f, y + 2.45f, 4.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_AcousticSensorLed_W_0", parent, new Vector3(-10.49f, y + 2.48f, 4.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone01_Reception_AcousticSensor_E_0", parent, new Vector3(10.58f, y + 2.45f, 4.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_AcousticSensorLed_E_0", parent, new Vector3(10.49f, y + 2.48f, 4.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone01_Reception_AcousticSensor_W_1", parent, new Vector3(-10.58f, y + 2.45f, 8.8f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_AcousticSensorLed_W_1", parent, new Vector3(-10.49f, y + 2.48f, 8.8f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone01_Reception_AcousticSensor_E_1", parent, new Vector3(10.58f, y + 2.45f, 8.8f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_AcousticSensorLed_E_1", parent, new Vector3(10.49f, y + 2.48f, 8.8f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone01_Reception_AcousticSensor_W_2", parent, new Vector3(-10.58f, y + 2.45f, 13.2f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_AcousticSensorLed_W_2", parent, new Vector3(-10.49f, y + 2.48f, 13.2f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone01_Reception_AcousticSensor_E_2", parent, new Vector3(10.58f, y + 2.45f, 13.2f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_AcousticSensorLed_E_2", parent, new Vector3(10.49f, y + 2.48f, 13.2f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone01_Reception_AcousticSensor_W_3", parent, new Vector3(-10.58f, y + 2.45f, 17.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_AcousticSensorLed_W_3", parent, new Vector3(-10.49f, y + 2.48f, 17.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone01_Reception_AcousticSensor_E_3", parent, new Vector3(10.58f, y + 2.45f, 17.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_AcousticSensorLed_E_3", parent, new Vector3(10.49f, y + 2.48f, 17.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone01_Reception_DrainStrip_L_0", parent, new Vector3(-2.6f, y + 0.012f, 4.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_DrainStrip_R_0", parent, new Vector3(2.6f, y + 0.012f, 4.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallCableClamp_W_0", parent, new Vector3(-10.64f, y + 1.15f, 4.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_WallCableClamp_E_0", parent, new Vector3(10.64f, y + 1.15f, 4.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_DrainStrip_L_1", parent, new Vector3(-2.6f, y + 0.012f, 8.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_DrainStrip_R_1", parent, new Vector3(2.6f, y + 0.012f, 8.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallCableClamp_W_1", parent, new Vector3(-10.64f, y + 1.15f, 8.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_WallCableClamp_E_1", parent, new Vector3(10.64f, y + 1.15f, 8.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_DrainStrip_L_2", parent, new Vector3(-2.6f, y + 0.012f, 13.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_DrainStrip_R_2", parent, new Vector3(2.6f, y + 0.012f, 13.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallCableClamp_W_2", parent, new Vector3(-10.64f, y + 1.15f, 13.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_WallCableClamp_E_2", parent, new Vector3(10.64f, y + 1.15f, 13.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_DrainStrip_L_3", parent, new Vector3(-2.6f, y + 0.012f, 18.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_DrainStrip_R_3", parent, new Vector3(2.6f, y + 0.012f, 18.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallCableClamp_W_3", parent, new Vector3(-10.64f, y + 1.15f, 18.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_WallCableClamp_E_3", parent, new Vector3(10.64f, y + 1.15f, 18.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_Baseboard_S_0", parent, new Vector3(-8.8f, y + 0.12f, 2.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_Baseboard_N_0", parent, new Vector3(-8.8f, y + 0.12f, 19.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingPipe_S_0", parent, new Vector3(-8.8f, y + 3.55f, 3.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_CeilingPipe_N_0", parent, new Vector3(-8.8f, y + 3.55f, 18.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_Baseboard_S_1", parent, new Vector3(-2.93f, y + 0.12f, 2.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_Baseboard_N_1", parent, new Vector3(-2.93f, y + 0.12f, 19.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingPipe_S_1", parent, new Vector3(-2.93f, y + 3.55f, 3.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_CeilingPipe_N_1", parent, new Vector3(-2.93f, y + 3.55f, 18.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_Baseboard_S_2", parent, new Vector3(2.93f, y + 0.12f, 2.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_Baseboard_N_2", parent, new Vector3(2.93f, y + 0.12f, 19.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingPipe_S_2", parent, new Vector3(2.93f, y + 3.55f, 3.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_CeilingPipe_N_2", parent, new Vector3(2.93f, y + 3.55f, 18.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_Baseboard_S_3", parent, new Vector3(8.8f, y + 0.12f, 2.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_Baseboard_N_3", parent, new Vector3(8.8f, y + 0.12f, 19.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingPipe_S_3", parent, new Vector3(8.8f, y + 3.55f, 3.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_CeilingPipe_N_3", parent, new Vector3(8.8f, y + 3.55f, 18.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone01_Reception_AcousticPanel_W_0", parent, new Vector3(-10.64f, y + 1.85f, 4.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone01_Reception_AcousticPanel_E_0", parent, new Vector3(10.64f, y + 1.85f, 4.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateDecorBox("Zone01_Reception_StrobeMount_W_0", parent, new Vector3(-10.6f, y + 3.05f, 5.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone01_Reception_StrobeMount_E_0", parent, new Vector3(10.6f, y + 3.05f, 3.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone01_Reception_AcousticPanel_W_1", parent, new Vector3(-10.64f, y + 1.85f, 8.87f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone01_Reception_AcousticPanel_E_1", parent, new Vector3(10.64f, y + 1.85f, 8.87f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateDecorBox("Zone01_Reception_StrobeMount_W_1", parent, new Vector3(-10.6f, y + 3.05f, 9.52f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone01_Reception_StrobeMount_E_1", parent, new Vector3(10.6f, y + 3.05f, 8.22f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone01_Reception_AcousticPanel_W_2", parent, new Vector3(-10.64f, y + 1.85f, 13.13f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone01_Reception_AcousticPanel_E_2", parent, new Vector3(10.64f, y + 1.85f, 13.13f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateDecorBox("Zone01_Reception_StrobeMount_W_2", parent, new Vector3(-10.6f, y + 3.05f, 13.78f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone01_Reception_StrobeMount_E_2", parent, new Vector3(10.6f, y + 3.05f, 12.48f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone01_Reception_AcousticPanel_W_3", parent, new Vector3(-10.64f, y + 1.85f, 17.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone01_Reception_AcousticPanel_E_3", parent, new Vector3(10.64f, y + 1.85f, 17.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateDecorBox("Zone01_Reception_StrobeMount_W_3", parent, new Vector3(-10.6f, y + 3.05f, 18.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone01_Reception_StrobeMount_E_3", parent, new Vector3(10.6f, y + 3.05f, 16.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone01_Reception_CeilingVentBox_S_0", parent, new Vector3(-8.4f, y + 3.86f, 7.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CeilingVentBox_N_0", parent, new Vector3(-8.4f, y + 3.86f, 14.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_VerticalConduit_S_0", parent, new Vector3(-8.4f, y + 1.95f, 2.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_VerticalConduit_N_0", parent, new Vector3(-8.4f, y + 1.95f, 19.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_CeilingVentBox_S_1", parent, new Vector3(-2.8f, y + 3.86f, 7.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CeilingVentBox_N_1", parent, new Vector3(-2.8f, y + 3.86f, 14.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_VerticalConduit_S_1", parent, new Vector3(-2.8f, y + 1.95f, 2.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_VerticalConduit_N_1", parent, new Vector3(-2.8f, y + 1.95f, 19.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_CeilingVentBox_S_2", parent, new Vector3(2.8f, y + 3.86f, 7.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CeilingVentBox_N_2", parent, new Vector3(2.8f, y + 3.86f, 14.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_VerticalConduit_S_2", parent, new Vector3(2.8f, y + 1.95f, 2.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_VerticalConduit_N_2", parent, new Vector3(2.8f, y + 1.95f, 19.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_CeilingVentBox_S_3", parent, new Vector3(8.4f, y + 3.86f, 7.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_CeilingVentBox_N_3", parent, new Vector3(8.4f, y + 3.86f, 14.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_VerticalConduit_S_3", parent, new Vector3(8.4f, y + 1.95f, 2.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_VerticalConduit_N_3", parent, new Vector3(8.4f, y + 1.95f, 19.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_UpperTrim_W_0", parent, new Vector3(-10.66f, y + 3.65f, 3.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_UpperTrim_E_0", parent, new Vector3(10.66f, y + 3.65f, 3.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_MidStripe_W_0", parent, new Vector3(-10.67f, y + 1.32f, 3.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_MidStripe_E_0", parent, new Vector3(10.67f, y + 1.32f, 3.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_UpperTrim_W_1", parent, new Vector3(-10.66f, y + 3.65f, 7.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_UpperTrim_E_1", parent, new Vector3(10.66f, y + 3.65f, 7.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_MidStripe_W_1", parent, new Vector3(-10.67f, y + 1.32f, 7.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_MidStripe_E_1", parent, new Vector3(10.67f, y + 1.32f, 7.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_UpperTrim_W_2", parent, new Vector3(-10.66f, y + 3.65f, 11.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_UpperTrim_E_2", parent, new Vector3(10.66f, y + 3.65f, 11.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_MidStripe_W_2", parent, new Vector3(-10.67f, y + 1.32f, 11.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_MidStripe_E_2", parent, new Vector3(10.67f, y + 1.32f, 11.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_UpperTrim_W_3", parent, new Vector3(-10.66f, y + 3.65f, 14.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_UpperTrim_E_3", parent, new Vector3(10.66f, y + 3.65f, 14.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_MidStripe_W_3", parent, new Vector3(-10.67f, y + 1.32f, 14.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_MidStripe_E_3", parent, new Vector3(10.67f, y + 1.32f, 14.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_UpperTrim_W_4", parent, new Vector3(-10.66f, y + 3.65f, 18.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_UpperTrim_E_4", parent, new Vector3(10.66f, y + 3.65f, 18.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone01_Reception_MidStripe_W_4", parent, new Vector3(-10.67f, y + 1.32f, 18.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone01_Reception_MidStripe_E_4", parent, new Vector3(10.67f, y + 1.32f, 18.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_L_0", parent, new Vector3(-3.2f, y + 3.84f, 3.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_R_0", parent, new Vector3(3.2f, y + 3.84f, 3.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_WallJunction_W_0", parent, new Vector3(-10.62f, y + 0.78f, 3.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallJunction_E_0", parent, new Vector3(10.62f, y + 0.78f, 3.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_L_1", parent, new Vector3(-3.2f, y + 3.84f, 7.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_R_1", parent, new Vector3(3.2f, y + 3.84f, 7.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_WallJunction_W_1", parent, new Vector3(-10.62f, y + 0.78f, 7.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallJunction_E_1", parent, new Vector3(10.62f, y + 0.78f, 7.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_L_2", parent, new Vector3(-3.2f, y + 3.84f, 11.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_R_2", parent, new Vector3(3.2f, y + 3.84f, 11.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_WallJunction_W_2", parent, new Vector3(-10.62f, y + 0.78f, 11.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallJunction_E_2", parent, new Vector3(10.62f, y + 0.78f, 11.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_L_3", parent, new Vector3(-3.2f, y + 3.84f, 14.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_R_3", parent, new Vector3(3.2f, y + 3.84f, 14.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_WallJunction_W_3", parent, new Vector3(-10.62f, y + 0.78f, 14.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallJunction_E_3", parent, new Vector3(10.62f, y + 0.78f, 14.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_L_4", parent, new Vector3(-3.2f, y + 3.84f, 18.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone01_Reception_Sprinkler_R_4", parent, new Vector3(3.2f, y + 3.84f, 18.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_WallJunction_W_4", parent, new Vector3(-10.62f, y + 0.78f, 18.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallJunction_E_4", parent, new Vector3(10.62f, y + 0.78f, 18.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_FloorJoint_X_0", parent, new Vector3(-9.0f, y + 0.008f, 11.0f), new Vector3(0.05f, 0.01f, 15.0f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingStrut_S_0", parent, new Vector3(-9.0f, y + 3.74f, 4.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingStrut_N_0", parent, new Vector3(-9.0f, y + 3.74f, 17.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallBracket_S_0", parent, new Vector3(-9.0f, y + 2.85f, 2.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_WallBracket_N_0", parent, new Vector3(-9.0f, y + 2.85f, 19.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_FloorJoint_X_1", parent, new Vector3(-3.0f, y + 0.008f, 11.0f), new Vector3(0.05f, 0.01f, 15.0f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingStrut_S_1", parent, new Vector3(-3.0f, y + 3.74f, 4.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingStrut_N_1", parent, new Vector3(-3.0f, y + 3.74f, 17.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallBracket_S_1", parent, new Vector3(-3.0f, y + 2.85f, 2.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_WallBracket_N_1", parent, new Vector3(-3.0f, y + 2.85f, 19.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_FloorJoint_X_2", parent, new Vector3(3.0f, y + 0.008f, 11.0f), new Vector3(0.05f, 0.01f, 15.0f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingStrut_S_2", parent, new Vector3(3.0f, y + 3.74f, 4.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingStrut_N_2", parent, new Vector3(3.0f, y + 3.74f, 17.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallBracket_S_2", parent, new Vector3(3.0f, y + 2.85f, 2.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_WallBracket_N_2", parent, new Vector3(3.0f, y + 2.85f, 19.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_FloorJoint_X_3", parent, new Vector3(9.0f, y + 0.008f, 11.0f), new Vector3(0.05f, 0.01f, 15.0f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingStrut_S_3", parent, new Vector3(9.0f, y + 3.74f, 4.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_CeilingStrut_N_3", parent, new Vector3(9.0f, y + 3.74f, 17.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_WallBracket_S_3", parent, new Vector3(9.0f, y + 2.85f, 2.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_WallBracket_N_3", parent, new Vector3(9.0f, y + 2.85f, 19.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_LowerPipe_W_0", parent, new Vector3(-10.65f, y + 0.55f, 3.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_LowerPipe_E_0", parent, new Vector3(10.65f, y + 0.55f, 3.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_HighPipe_W_0", parent, new Vector3(-10.65f, y + 3.38f, 3.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_HighPipe_E_0", parent, new Vector3(10.65f, y + 3.38f, 3.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_LowerPipe_W_1", parent, new Vector3(-10.65f, y + 0.55f, 7.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_LowerPipe_E_1", parent, new Vector3(10.65f, y + 0.55f, 7.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_HighPipe_W_1", parent, new Vector3(-10.65f, y + 3.38f, 7.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_HighPipe_E_1", parent, new Vector3(10.65f, y + 3.38f, 7.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_LowerPipe_W_2", parent, new Vector3(-10.65f, y + 0.55f, 11.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_LowerPipe_E_2", parent, new Vector3(10.65f, y + 0.55f, 11.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_HighPipe_W_2", parent, new Vector3(-10.65f, y + 3.38f, 11.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_HighPipe_E_2", parent, new Vector3(10.65f, y + 3.38f, 11.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_LowerPipe_W_3", parent, new Vector3(-10.65f, y + 0.55f, 14.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_LowerPipe_E_3", parent, new Vector3(10.65f, y + 0.55f, 14.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_HighPipe_W_3", parent, new Vector3(-10.65f, y + 3.38f, 14.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_HighPipe_E_3", parent, new Vector3(10.65f, y + 3.38f, 14.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_LowerPipe_W_4", parent, new Vector3(-10.65f, y + 0.55f, 18.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_LowerPipe_E_4", parent, new Vector3(10.65f, y + 0.55f, 18.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone01_Reception_HighPipe_W_4", parent, new Vector3(-10.65f, y + 3.38f, 18.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_HighPipe_E_4", parent, new Vector3(10.65f, y + 3.38f, 18.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone01_Reception_DuctCollar_C_0", parent, new Vector3(0.0f, y + 3.76f, 3.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_TelemetryNode_W_0", parent, new Vector3(-10.62f, y + 1.58f, 3.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_TelemetryNode_E_0", parent, new Vector3(10.62f, y + 1.58f, 3.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_W_0", parent, new Vector3(-10.35f, y + 0.01f, 3.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_E_0", parent, new Vector3(10.35f, y + 0.01f, 3.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_DuctCollar_C_1", parent, new Vector3(0.0f, y + 3.76f, 7.4f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_TelemetryNode_W_1", parent, new Vector3(-10.62f, y + 1.58f, 7.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_TelemetryNode_E_1", parent, new Vector3(10.62f, y + 1.58f, 7.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_W_1", parent, new Vector3(-10.35f, y + 0.01f, 7.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_E_1", parent, new Vector3(10.35f, y + 0.01f, 7.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_DuctCollar_C_2", parent, new Vector3(0.0f, y + 3.76f, 11.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_TelemetryNode_W_2", parent, new Vector3(-10.62f, y + 1.58f, 11.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_TelemetryNode_E_2", parent, new Vector3(10.62f, y + 1.58f, 11.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_W_2", parent, new Vector3(-10.35f, y + 0.01f, 11.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_E_2", parent, new Vector3(10.35f, y + 0.01f, 11.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_DuctCollar_C_3", parent, new Vector3(0.0f, y + 3.76f, 14.6f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_TelemetryNode_W_3", parent, new Vector3(-10.62f, y + 1.58f, 14.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_TelemetryNode_E_3", parent, new Vector3(10.62f, y + 1.58f, 14.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_W_3", parent, new Vector3(-10.35f, y + 0.01f, 14.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_E_3", parent, new Vector3(10.35f, y + 0.01f, 14.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_DuctCollar_C_4", parent, new Vector3(0.0f, y + 3.76f, 18.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_TelemetryNode_W_4", parent, new Vector3(-10.62f, y + 1.58f, 18.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_TelemetryNode_E_4", parent, new Vector3(10.62f, y + 1.58f, 18.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_W_4", parent, new Vector3(-10.35f, y + 0.01f, 18.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_FloorBoltPlate_E_4", parent, new Vector3(10.35f, y + 0.01f, 18.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone01_Reception_InspHatch_L_0", parent, new Vector3(-5.2f, y + 3.88f, 3.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_InspHatch_R_0", parent, new Vector3(5.2f, y + 3.88f, 3.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_RailClamp_W_0", parent, new Vector3(-10.64f, y + 2.15f, 3.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_RailClamp_E_0", parent, new Vector3(10.64f, y + 2.15f, 3.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_InspHatch_L_1", parent, new Vector3(-5.2f, y + 3.88f, 7.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_InspHatch_R_1", parent, new Vector3(5.2f, y + 3.88f, 7.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_RailClamp_W_1", parent, new Vector3(-10.64f, y + 2.15f, 7.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_RailClamp_E_1", parent, new Vector3(10.64f, y + 2.15f, 7.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_InspHatch_L_2", parent, new Vector3(-5.2f, y + 3.88f, 11.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_InspHatch_R_2", parent, new Vector3(5.2f, y + 3.88f, 11.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_RailClamp_W_2", parent, new Vector3(-10.64f, y + 2.15f, 11.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_RailClamp_E_2", parent, new Vector3(10.64f, y + 2.15f, 11.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_InspHatch_L_3", parent, new Vector3(-5.2f, y + 3.88f, 14.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_InspHatch_R_3", parent, new Vector3(5.2f, y + 3.88f, 14.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_RailClamp_W_3", parent, new Vector3(-10.64f, y + 2.15f, 14.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_RailClamp_E_3", parent, new Vector3(10.64f, y + 2.15f, 14.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_InspHatch_L_4", parent, new Vector3(-5.2f, y + 3.88f, 18.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_InspHatch_R_4", parent, new Vector3(5.2f, y + 3.88f, 18.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone01_Reception_RailClamp_W_4", parent, new Vector3(-10.64f, y + 2.15f, 18.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone01_Reception_RailClamp_E_4", parent, new Vector3(10.64f, y + 2.15f, 18.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone02_Atrium(Transform parent)
    {
        float y = MainY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone02_Atrium
        BuildFireExtinguisherCabinet("Zone02_Atrium_FireCab_W_0", parent, new Vector3(-14.45f, y + 1.45f, 25.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone02_Atrium_FireCab_E_0", parent, new Vector3(14.45f, y + 1.45f, 25.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone02_Atrium_FireCab_W_1", parent, new Vector3(-14.45f, y + 1.45f, 37.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone02_Atrium_FireCab_E_1", parent, new Vector3(14.45f, y + 1.45f, 37.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone02_Atrium_FireCab_W_2", parent, new Vector3(-14.45f, y + 1.45f, 48.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone02_Atrium_FireCab_E_2", parent, new Vector3(14.45f, y + 1.45f, 48.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone02_Atrium_CCTV_0", parent, new Vector3(-14.55f, y + 3.35f, 23.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone02_Atrium_CCTV_1", parent, new Vector3(14.55f, y + 3.35f, 23.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone02_Atrium_CCTV_2", parent, new Vector3(-14.55f, y + 3.35f, 50.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone02_Atrium_CCTV_3", parent, new Vector3(14.55f, y + 3.35f, 50.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone02_Atrium_CCTV_4", parent, new Vector3(-3.5f, y + 3.35f, 23.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone02_Atrium_CCTV_5", parent, new Vector3(3.5f, y + 3.35f, 50.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone02_Atrium_Intercom_W_0", parent, new Vector3(-14.58f, y + 1.52f, 26.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone02_Atrium_Intercom_E_0", parent, new Vector3(14.58f, y + 1.52f, 26.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone02_Atrium_Clock_W_0", parent, new Vector3(-14.6f, y + 2.85f, 26.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone02_Atrium_Clock_E_0", parent, new Vector3(14.6f, y + 2.85f, 26.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone02_Atrium_Intercom_W_1", parent, new Vector3(-14.58f, y + 1.52f, 33.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone02_Atrium_Intercom_E_1", parent, new Vector3(14.58f, y + 1.52f, 33.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone02_Atrium_Intercom_W_2", parent, new Vector3(-14.58f, y + 1.52f, 40.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone02_Atrium_Intercom_E_2", parent, new Vector3(14.58f, y + 1.52f, 40.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone02_Atrium_Clock_W_2", parent, new Vector3(-14.6f, y + 2.85f, 40.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone02_Atrium_Clock_E_2", parent, new Vector3(14.6f, y + 2.85f, 40.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone02_Atrium_Intercom_W_3", parent, new Vector3(-14.58f, y + 1.52f, 48.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone02_Atrium_Intercom_E_3", parent, new Vector3(14.58f, y + 1.52f, 48.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_W_0", parent, new Vector3(-14.55f, y, 25.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_E_0", parent, new Vector3(14.55f, y, 25.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_W_1", parent, new Vector3(-14.55f, y, 33.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_E_1", parent, new Vector3(14.55f, y, 33.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_W_2", parent, new Vector3(-14.55f, y, 41.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_E_2", parent, new Vector3(14.55f, y, 41.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_W_3", parent, new Vector3(-14.55f, y, 49.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_E_3", parent, new Vector3(14.55f, y, 49.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_W_4", parent, new Vector3(-14.55f, y, 57.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_E_4", parent, new Vector3(14.55f, y, 57.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_W_5", parent, new Vector3(-14.55f, y, 65.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone02_Atrium_Radiator_E_5", parent, new Vector3(14.55f, y, 65.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_W_0", parent, new Vector3(-14.58f, y + 3.15f, 25.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_E_0", parent, new Vector3(14.58f, y + 3.15f, 25.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_W_0", parent, new Vector3(-13.75f, y + 3.45f, 25.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_E_0", parent, new Vector3(13.75f, y + 3.45f, 25.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_W_1", parent, new Vector3(-14.58f, y + 3.15f, 33.07f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_E_1", parent, new Vector3(14.58f, y + 3.15f, 33.07f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_W_1", parent, new Vector3(-13.75f, y + 3.45f, 33.07f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_E_1", parent, new Vector3(13.75f, y + 3.45f, 33.07f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_W_2", parent, new Vector3(-14.58f, y + 3.15f, 40.93f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_E_2", parent, new Vector3(14.58f, y + 3.15f, 40.93f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_W_2", parent, new Vector3(-13.75f, y + 3.45f, 40.93f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_E_2", parent, new Vector3(13.75f, y + 3.45f, 40.93f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_W_3", parent, new Vector3(-14.58f, y + 3.15f, 48.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_E_3", parent, new Vector3(14.58f, y + 3.15f, 48.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_W_3", parent, new Vector3(-13.75f, y + 3.45f, 48.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_E_3", parent, new Vector3(13.75f, y + 3.45f, 48.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_W_4", parent, new Vector3(-14.58f, y + 3.15f, 56.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_E_4", parent, new Vector3(14.58f, y + 3.15f, 56.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_W_4", parent, new Vector3(-13.75f, y + 3.45f, 56.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_E_4", parent, new Vector3(13.75f, y + 3.45f, 56.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_W_5", parent, new Vector3(-14.58f, y + 3.15f, 64.53f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone02_Atrium_Vent_E_5", parent, new Vector3(14.58f, y + 3.15f, 64.53f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_W_5", parent, new Vector3(-13.75f, y + 3.45f, 64.53f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone02_Atrium_PA_E_5", parent, new Vector3(13.75f, y + 3.45f, 64.53f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone02_Atrium_ValveManifold_S_0", parent, new Vector3(-12.0f, y, 23.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone02_Atrium_ValveManifold_N_0", parent, new Vector3(-12.0f, y, 50.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone02_Atrium_FuseBox_S_0", parent, new Vector3(-10.8f, y + 1.65f, 23.45f), Quaternion.identity);
        BuildWallFuseBox("Zone02_Atrium_FuseBox_N_0", parent, new Vector3(-13.2f, y + 1.65f, 50.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone02_Atrium_ValveManifold_S_1", parent, new Vector3(-4.0f, y, 23.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone02_Atrium_ValveManifold_N_1", parent, new Vector3(-4.0f, y, 50.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone02_Atrium_FuseBox_S_1", parent, new Vector3(-2.8f, y + 1.65f, 23.45f), Quaternion.identity);
        BuildWallFuseBox("Zone02_Atrium_FuseBox_N_1", parent, new Vector3(-5.2f, y + 1.65f, 50.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone02_Atrium_ValveManifold_S_2", parent, new Vector3(4.0f, y, 23.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone02_Atrium_ValveManifold_N_2", parent, new Vector3(4.0f, y, 50.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone02_Atrium_FuseBox_S_2", parent, new Vector3(5.2f, y + 1.65f, 23.45f), Quaternion.identity);
        BuildWallFuseBox("Zone02_Atrium_FuseBox_N_2", parent, new Vector3(2.8f, y + 1.65f, 50.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone02_Atrium_ValveManifold_S_3", parent, new Vector3(12.0f, y, 23.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone02_Atrium_ValveManifold_N_3", parent, new Vector3(12.0f, y, 50.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone02_Atrium_FuseBox_S_3", parent, new Vector3(13.2f, y + 1.65f, 23.45f), Quaternion.identity);
        BuildWallFuseBox("Zone02_Atrium_FuseBox_N_3", parent, new Vector3(10.8f, y + 1.65f, 50.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_L_0", parent, new Vector3(-4.2f, y + 3.48f, 26.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_R_0", parent, new Vector3(4.2f, y + 3.48f, 26.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_L_1", parent, new Vector3(-4.2f, y + 3.48f, 31.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_R_1", parent, new Vector3(4.2f, y + 3.48f, 31.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_L_2", parent, new Vector3(-4.2f, y + 3.48f, 37.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_R_2", parent, new Vector3(4.2f, y + 3.48f, 37.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_L_3", parent, new Vector3(-4.2f, y + 3.48f, 42.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_R_3", parent, new Vector3(4.2f, y + 3.48f, 42.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_L_4", parent, new Vector3(-4.2f, y + 3.48f, 48.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone02_Atrium_Baffle_R_4", parent, new Vector3(4.2f, y + 3.48f, 48.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone02_Atrium_Crates_SW", parent, new Vector3(-13.4f, y, 24.6f), 14f);
        BuildCableSpoolAndMat("Zone02_Atrium_Spool_SE", parent, new Vector3(13.4f, y, 24.6f), -28f);
        BuildHazmatDrumCluster("Zone02_Atrium_Drums_NW", parent, new Vector3(-13.4f, y, 49.4f));
        BuildPatchPanelRack("Zone02_Atrium_PatchRack_NE", parent, new Vector3(13.4f, y, 49.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone02_Atrium_CheckStation_S_0", parent, new Vector3(-12.6f, y, 24.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone02_Atrium_Bollard_S_0", parent, new Vector3(-13.45f, y, 23.95f));
        BuildWallWarningBollard("Zone02_Atrium_Bollard_N_0", parent, new Vector3(-11.75f, y, 50.05f));
        BuildClipboardCheckStation("Zone02_Atrium_CheckStation_S_1", parent, new Vector3(-4.2f, y, 24.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone02_Atrium_Bollard_S_1", parent, new Vector3(-5.05f, y, 23.95f));
        BuildWallWarningBollard("Zone02_Atrium_Bollard_N_1", parent, new Vector3(-3.35f, y, 50.05f));
        BuildClipboardCheckStation("Zone02_Atrium_CheckStation_S_2", parent, new Vector3(4.2f, y, 24.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone02_Atrium_Bollard_S_2", parent, new Vector3(3.35f, y, 23.95f));
        BuildWallWarningBollard("Zone02_Atrium_Bollard_N_2", parent, new Vector3(5.05f, y, 50.05f));
        BuildClipboardCheckStation("Zone02_Atrium_CheckStation_S_3", parent, new Vector3(12.6f, y, 24.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone02_Atrium_Bollard_S_3", parent, new Vector3(11.75f, y, 23.95f));
        BuildWallWarningBollard("Zone02_Atrium_Bollard_N_3", parent, new Vector3(13.45f, y, 50.05f));
        CreateAcousticBox("Zone02_Atrium_Pilaster_W_0", parent, new Vector3(-14.75f, y + 2.0f, 24.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone02_Atrium_Pilaster_E_0", parent, new Vector3(14.75f, y + 2.0f, 24.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_W_0", parent, new Vector3(-14.62f, y + 0.32f, 24.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_E_0", parent, new Vector3(14.62f, y + 0.32f, 24.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone02_Atrium_Pilaster_W_1", parent, new Vector3(-14.75f, y + 2.0f, 30.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone02_Atrium_Pilaster_E_1", parent, new Vector3(14.75f, y + 2.0f, 30.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_W_1", parent, new Vector3(-14.62f, y + 0.32f, 30.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_E_1", parent, new Vector3(14.62f, y + 0.32f, 30.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone02_Atrium_Pilaster_W_2", parent, new Vector3(-14.75f, y + 2.0f, 37.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone02_Atrium_Pilaster_E_2", parent, new Vector3(14.75f, y + 2.0f, 37.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_W_2", parent, new Vector3(-14.62f, y + 0.32f, 37.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_E_2", parent, new Vector3(14.62f, y + 0.32f, 37.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone02_Atrium_Pilaster_W_3", parent, new Vector3(-14.75f, y + 2.0f, 43.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone02_Atrium_Pilaster_E_3", parent, new Vector3(14.75f, y + 2.0f, 43.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_W_3", parent, new Vector3(-14.62f, y + 0.32f, 43.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_E_3", parent, new Vector3(14.62f, y + 0.32f, 43.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone02_Atrium_Pilaster_W_4", parent, new Vector3(-14.75f, y + 2.0f, 49.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone02_Atrium_Pilaster_E_4", parent, new Vector3(14.75f, y + 2.0f, 49.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_W_4", parent, new Vector3(-14.62f, y + 0.32f, 49.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_ConduitTrim_E_4", parent, new Vector3(14.62f, y + 0.32f, 49.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingRib_0", parent, new Vector3(0.0f, y + 3.82f, 24.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_L_0", parent, new Vector3(-8.06f, y + 3.68f, 24.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_R_0", parent, new Vector3(8.06f, y + 3.68f, 24.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CeilingRib_1", parent, new Vector3(0.0f, y + 3.82f, 30.75f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_L_1", parent, new Vector3(-8.06f, y + 3.68f, 30.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_R_1", parent, new Vector3(8.06f, y + 3.68f, 30.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CeilingRib_2", parent, new Vector3(0.0f, y + 3.82f, 37.0f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_L_2", parent, new Vector3(-8.06f, y + 3.68f, 37.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_R_2", parent, new Vector3(8.06f, y + 3.68f, 37.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CeilingRib_3", parent, new Vector3(0.0f, y + 3.82f, 43.25f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_L_3", parent, new Vector3(-8.06f, y + 3.68f, 43.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_R_3", parent, new Vector3(8.06f, y + 3.68f, 43.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CeilingRib_4", parent, new Vector3(0.0f, y + 3.82f, 49.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_L_4", parent, new Vector3(-8.06f, y + 3.68f, 49.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CableTrayHanger_R_4", parent, new Vector3(8.06f, y + 3.68f, 49.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_FloorSafetyLine_S_0", parent, new Vector3(-12.2f, y + 0.014f, 24.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_FloorSafetyLine_N_0", parent, new Vector3(-12.2f, y + 0.014f, 49.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_WallDataPlaque_S_0", parent, new Vector3(-12.2f, y + 2.25f, 23.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_WallDataPlaque_N_0", parent, new Vector3(-12.2f, y + 2.25f, 50.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_FloorSafetyLine_S_1", parent, new Vector3(-4.07f, y + 0.014f, 24.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_FloorSafetyLine_N_1", parent, new Vector3(-4.07f, y + 0.014f, 49.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_WallDataPlaque_S_1", parent, new Vector3(-4.07f, y + 2.25f, 23.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_WallDataPlaque_N_1", parent, new Vector3(-4.07f, y + 2.25f, 50.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_FloorSafetyLine_S_2", parent, new Vector3(4.07f, y + 0.014f, 24.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_FloorSafetyLine_N_2", parent, new Vector3(4.07f, y + 0.014f, 49.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_WallDataPlaque_S_2", parent, new Vector3(4.07f, y + 2.25f, 23.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_WallDataPlaque_N_2", parent, new Vector3(4.07f, y + 2.25f, 50.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_FloorSafetyLine_S_3", parent, new Vector3(12.2f, y + 0.014f, 24.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_FloorSafetyLine_N_3", parent, new Vector3(12.2f, y + 0.014f, 49.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_WallDataPlaque_S_3", parent, new Vector3(12.2f, y + 2.25f, 23.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_WallDataPlaque_N_3", parent, new Vector3(12.2f, y + 2.25f, 50.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_AcousticSensor_W_0", parent, new Vector3(-14.58f, y + 2.45f, 25.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_AcousticSensorLed_W_0", parent, new Vector3(-14.49f, y + 2.48f, 25.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone02_Atrium_AcousticSensor_E_0", parent, new Vector3(14.58f, y + 2.45f, 25.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_AcousticSensorLed_E_0", parent, new Vector3(14.49f, y + 2.48f, 25.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone02_Atrium_AcousticSensor_W_1", parent, new Vector3(-14.58f, y + 2.45f, 33.13f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_AcousticSensorLed_W_1", parent, new Vector3(-14.49f, y + 2.48f, 33.13f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone02_Atrium_AcousticSensor_E_1", parent, new Vector3(14.58f, y + 2.45f, 33.13f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_AcousticSensorLed_E_1", parent, new Vector3(14.49f, y + 2.48f, 33.13f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone02_Atrium_AcousticSensor_W_2", parent, new Vector3(-14.58f, y + 2.45f, 40.87f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_AcousticSensorLed_W_2", parent, new Vector3(-14.49f, y + 2.48f, 40.87f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone02_Atrium_AcousticSensor_E_2", parent, new Vector3(14.58f, y + 2.45f, 40.87f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_AcousticSensorLed_E_2", parent, new Vector3(14.49f, y + 2.48f, 40.87f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone02_Atrium_AcousticSensor_W_3", parent, new Vector3(-14.58f, y + 2.45f, 48.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_AcousticSensorLed_W_3", parent, new Vector3(-14.49f, y + 2.48f, 48.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone02_Atrium_AcousticSensor_E_3", parent, new Vector3(14.58f, y + 2.45f, 48.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_AcousticSensorLed_E_3", parent, new Vector3(14.49f, y + 2.48f, 48.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone02_Atrium_DrainStrip_L_0", parent, new Vector3(-2.6f, y + 0.012f, 25.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_DrainStrip_R_0", parent, new Vector3(2.6f, y + 0.012f, 25.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallCableClamp_W_0", parent, new Vector3(-14.64f, y + 1.15f, 25.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_WallCableClamp_E_0", parent, new Vector3(14.64f, y + 1.15f, 25.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_DrainStrip_L_1", parent, new Vector3(-2.6f, y + 0.012f, 33.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_DrainStrip_R_1", parent, new Vector3(2.6f, y + 0.012f, 33.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallCableClamp_W_1", parent, new Vector3(-14.64f, y + 1.15f, 33.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_WallCableClamp_E_1", parent, new Vector3(14.64f, y + 1.15f, 33.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_DrainStrip_L_2", parent, new Vector3(-2.6f, y + 0.012f, 41.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_DrainStrip_R_2", parent, new Vector3(2.6f, y + 0.012f, 41.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallCableClamp_W_2", parent, new Vector3(-14.64f, y + 1.15f, 41.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_WallCableClamp_E_2", parent, new Vector3(14.64f, y + 1.15f, 41.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_DrainStrip_L_3", parent, new Vector3(-2.6f, y + 0.012f, 49.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_DrainStrip_R_3", parent, new Vector3(2.6f, y + 0.012f, 49.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallCableClamp_W_3", parent, new Vector3(-14.64f, y + 1.15f, 49.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_WallCableClamp_E_3", parent, new Vector3(14.64f, y + 1.15f, 49.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_Baseboard_S_0", parent, new Vector3(-12.8f, y + 0.12f, 23.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_Baseboard_N_0", parent, new Vector3(-12.8f, y + 0.12f, 50.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingPipe_S_0", parent, new Vector3(-12.8f, y + 3.55f, 24.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_CeilingPipe_N_0", parent, new Vector3(-12.8f, y + 3.55f, 49.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_Baseboard_S_1", parent, new Vector3(-4.27f, y + 0.12f, 23.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_Baseboard_N_1", parent, new Vector3(-4.27f, y + 0.12f, 50.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingPipe_S_1", parent, new Vector3(-4.27f, y + 3.55f, 24.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_CeilingPipe_N_1", parent, new Vector3(-4.27f, y + 3.55f, 49.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_Baseboard_S_2", parent, new Vector3(4.27f, y + 0.12f, 23.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_Baseboard_N_2", parent, new Vector3(4.27f, y + 0.12f, 50.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingPipe_S_2", parent, new Vector3(4.27f, y + 3.55f, 24.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_CeilingPipe_N_2", parent, new Vector3(4.27f, y + 3.55f, 49.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_Baseboard_S_3", parent, new Vector3(12.8f, y + 0.12f, 23.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_Baseboard_N_3", parent, new Vector3(12.8f, y + 0.12f, 50.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingPipe_S_3", parent, new Vector3(12.8f, y + 3.55f, 24.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_CeilingPipe_N_3", parent, new Vector3(12.8f, y + 3.55f, 49.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone02_Atrium_AcousticPanel_W_0", parent, new Vector3(-14.64f, y + 1.85f, 25.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone02_Atrium_AcousticPanel_E_0", parent, new Vector3(14.64f, y + 1.85f, 25.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone02_Atrium_StrobeMount_W_0", parent, new Vector3(-14.6f, y + 3.05f, 26.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone02_Atrium_StrobeMount_E_0", parent, new Vector3(14.6f, y + 3.05f, 24.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone02_Atrium_AcousticPanel_W_1", parent, new Vector3(-14.64f, y + 1.85f, 33.2f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone02_Atrium_AcousticPanel_E_1", parent, new Vector3(14.64f, y + 1.85f, 33.2f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone02_Atrium_StrobeMount_W_1", parent, new Vector3(-14.6f, y + 3.05f, 33.85f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone02_Atrium_StrobeMount_E_1", parent, new Vector3(14.6f, y + 3.05f, 32.55f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone02_Atrium_AcousticPanel_W_2", parent, new Vector3(-14.64f, y + 1.85f, 40.8f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone02_Atrium_AcousticPanel_E_2", parent, new Vector3(14.64f, y + 1.85f, 40.8f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone02_Atrium_StrobeMount_W_2", parent, new Vector3(-14.6f, y + 3.05f, 41.45f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone02_Atrium_StrobeMount_E_2", parent, new Vector3(14.6f, y + 3.05f, 40.15f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone02_Atrium_AcousticPanel_W_3", parent, new Vector3(-14.64f, y + 1.85f, 48.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone02_Atrium_AcousticPanel_E_3", parent, new Vector3(14.64f, y + 1.85f, 48.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone02_Atrium_StrobeMount_W_3", parent, new Vector3(-14.6f, y + 3.05f, 49.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone02_Atrium_StrobeMount_E_3", parent, new Vector3(14.6f, y + 3.05f, 47.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone02_Atrium_CeilingVentBox_S_0", parent, new Vector3(-12.4f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CeilingVentBox_N_0", parent, new Vector3(-12.4f, y + 3.86f, 40.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_VerticalConduit_S_0", parent, new Vector3(-12.4f, y + 1.95f, 23.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_VerticalConduit_N_0", parent, new Vector3(-12.4f, y + 1.95f, 50.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_CeilingVentBox_S_1", parent, new Vector3(-4.13f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CeilingVentBox_N_1", parent, new Vector3(-4.13f, y + 3.86f, 40.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_VerticalConduit_S_1", parent, new Vector3(-4.13f, y + 1.95f, 23.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_VerticalConduit_N_1", parent, new Vector3(-4.13f, y + 1.95f, 50.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_CeilingVentBox_S_2", parent, new Vector3(4.13f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CeilingVentBox_N_2", parent, new Vector3(4.13f, y + 3.86f, 40.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_VerticalConduit_S_2", parent, new Vector3(4.13f, y + 1.95f, 23.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_VerticalConduit_N_2", parent, new Vector3(4.13f, y + 1.95f, 50.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_CeilingVentBox_S_3", parent, new Vector3(12.4f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_CeilingVentBox_N_3", parent, new Vector3(12.4f, y + 3.86f, 40.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_VerticalConduit_S_3", parent, new Vector3(12.4f, y + 1.95f, 23.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_VerticalConduit_N_3", parent, new Vector3(12.4f, y + 1.95f, 50.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_UpperTrim_W_0", parent, new Vector3(-14.66f, y + 3.65f, 24.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_UpperTrim_E_0", parent, new Vector3(14.66f, y + 3.65f, 24.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_MidStripe_W_0", parent, new Vector3(-14.67f, y + 1.32f, 24.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_MidStripe_E_0", parent, new Vector3(14.67f, y + 1.32f, 24.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_UpperTrim_W_1", parent, new Vector3(-14.66f, y + 3.65f, 30.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_UpperTrim_E_1", parent, new Vector3(14.66f, y + 3.65f, 30.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_MidStripe_W_1", parent, new Vector3(-14.67f, y + 1.32f, 30.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_MidStripe_E_1", parent, new Vector3(14.67f, y + 1.32f, 30.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_UpperTrim_W_2", parent, new Vector3(-14.66f, y + 3.65f, 37.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_UpperTrim_E_2", parent, new Vector3(14.66f, y + 3.65f, 37.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_MidStripe_W_2", parent, new Vector3(-14.67f, y + 1.32f, 37.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_MidStripe_E_2", parent, new Vector3(14.67f, y + 1.32f, 37.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_UpperTrim_W_3", parent, new Vector3(-14.66f, y + 3.65f, 43.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_UpperTrim_E_3", parent, new Vector3(14.66f, y + 3.65f, 43.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_MidStripe_W_3", parent, new Vector3(-14.67f, y + 1.32f, 43.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_MidStripe_E_3", parent, new Vector3(14.67f, y + 1.32f, 43.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_UpperTrim_W_4", parent, new Vector3(-14.66f, y + 3.65f, 49.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_UpperTrim_E_4", parent, new Vector3(14.66f, y + 3.65f, 49.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone02_Atrium_MidStripe_W_4", parent, new Vector3(-14.67f, y + 1.32f, 49.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone02_Atrium_MidStripe_E_4", parent, new Vector3(14.67f, y + 1.32f, 49.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_L_0", parent, new Vector3(-3.2f, y + 3.84f, 24.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_R_0", parent, new Vector3(3.2f, y + 3.84f, 24.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_WallJunction_W_0", parent, new Vector3(-14.62f, y + 0.78f, 24.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallJunction_E_0", parent, new Vector3(14.62f, y + 0.78f, 24.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_L_1", parent, new Vector3(-3.2f, y + 3.84f, 30.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_R_1", parent, new Vector3(3.2f, y + 3.84f, 30.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_WallJunction_W_1", parent, new Vector3(-14.62f, y + 0.78f, 30.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallJunction_E_1", parent, new Vector3(14.62f, y + 0.78f, 30.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_L_2", parent, new Vector3(-3.2f, y + 3.84f, 37.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_R_2", parent, new Vector3(3.2f, y + 3.84f, 37.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_WallJunction_W_2", parent, new Vector3(-14.62f, y + 0.78f, 37.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallJunction_E_2", parent, new Vector3(14.62f, y + 0.78f, 37.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_L_3", parent, new Vector3(-3.2f, y + 3.84f, 43.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_R_3", parent, new Vector3(3.2f, y + 3.84f, 43.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_WallJunction_W_3", parent, new Vector3(-14.62f, y + 0.78f, 43.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallJunction_E_3", parent, new Vector3(14.62f, y + 0.78f, 43.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_L_4", parent, new Vector3(-3.2f, y + 3.84f, 49.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone02_Atrium_Sprinkler_R_4", parent, new Vector3(3.2f, y + 3.84f, 49.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_WallJunction_W_4", parent, new Vector3(-14.62f, y + 0.78f, 49.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallJunction_E_4", parent, new Vector3(14.62f, y + 0.78f, 49.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_FloorJoint_X_0", parent, new Vector3(-13.0f, y + 0.008f, 37.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingStrut_S_0", parent, new Vector3(-13.0f, y + 3.74f, 25.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingStrut_N_0", parent, new Vector3(-13.0f, y + 3.74f, 48.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallBracket_S_0", parent, new Vector3(-13.0f, y + 2.85f, 23.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_WallBracket_N_0", parent, new Vector3(-13.0f, y + 2.85f, 50.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_FloorJoint_X_1", parent, new Vector3(-4.33f, y + 0.008f, 37.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingStrut_S_1", parent, new Vector3(-4.33f, y + 3.74f, 25.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingStrut_N_1", parent, new Vector3(-4.33f, y + 3.74f, 48.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallBracket_S_1", parent, new Vector3(-4.33f, y + 2.85f, 23.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_WallBracket_N_1", parent, new Vector3(-4.33f, y + 2.85f, 50.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_FloorJoint_X_2", parent, new Vector3(4.33f, y + 0.008f, 37.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingStrut_S_2", parent, new Vector3(4.33f, y + 3.74f, 25.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingStrut_N_2", parent, new Vector3(4.33f, y + 3.74f, 48.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallBracket_S_2", parent, new Vector3(4.33f, y + 2.85f, 23.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_WallBracket_N_2", parent, new Vector3(4.33f, y + 2.85f, 50.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_FloorJoint_X_3", parent, new Vector3(13.0f, y + 0.008f, 37.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingStrut_S_3", parent, new Vector3(13.0f, y + 3.74f, 25.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_CeilingStrut_N_3", parent, new Vector3(13.0f, y + 3.74f, 48.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_WallBracket_S_3", parent, new Vector3(13.0f, y + 2.85f, 23.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_WallBracket_N_3", parent, new Vector3(13.0f, y + 2.85f, 50.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_LowerPipe_W_0", parent, new Vector3(-14.65f, y + 0.55f, 24.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_LowerPipe_E_0", parent, new Vector3(14.65f, y + 0.55f, 24.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_HighPipe_W_0", parent, new Vector3(-14.65f, y + 3.38f, 24.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_HighPipe_E_0", parent, new Vector3(14.65f, y + 3.38f, 24.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_LowerPipe_W_1", parent, new Vector3(-14.65f, y + 0.55f, 30.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_LowerPipe_E_1", parent, new Vector3(14.65f, y + 0.55f, 30.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_HighPipe_W_1", parent, new Vector3(-14.65f, y + 3.38f, 30.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_HighPipe_E_1", parent, new Vector3(14.65f, y + 3.38f, 30.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_LowerPipe_W_2", parent, new Vector3(-14.65f, y + 0.55f, 37.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_LowerPipe_E_2", parent, new Vector3(14.65f, y + 0.55f, 37.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_HighPipe_W_2", parent, new Vector3(-14.65f, y + 3.38f, 37.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_HighPipe_E_2", parent, new Vector3(14.65f, y + 3.38f, 37.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_LowerPipe_W_3", parent, new Vector3(-14.65f, y + 0.55f, 43.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_LowerPipe_E_3", parent, new Vector3(14.65f, y + 0.55f, 43.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_HighPipe_W_3", parent, new Vector3(-14.65f, y + 3.38f, 43.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_HighPipe_E_3", parent, new Vector3(14.65f, y + 3.38f, 43.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_LowerPipe_W_4", parent, new Vector3(-14.65f, y + 0.55f, 49.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_LowerPipe_E_4", parent, new Vector3(14.65f, y + 0.55f, 49.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone02_Atrium_HighPipe_W_4", parent, new Vector3(-14.65f, y + 3.38f, 49.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_HighPipe_E_4", parent, new Vector3(14.65f, y + 3.38f, 49.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone02_Atrium_DuctCollar_C_0", parent, new Vector3(0.0f, y + 3.76f, 24.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_W_0", parent, new Vector3(-14.62f, y + 1.58f, 24.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_E_0", parent, new Vector3(14.62f, y + 1.58f, 24.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_W_0", parent, new Vector3(-14.35f, y + 0.01f, 24.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_E_0", parent, new Vector3(14.35f, y + 0.01f, 24.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_DuctCollar_C_1", parent, new Vector3(0.0f, y + 3.76f, 30.9f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_W_1", parent, new Vector3(-14.62f, y + 1.58f, 30.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_E_1", parent, new Vector3(14.62f, y + 1.58f, 30.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_W_1", parent, new Vector3(-14.35f, y + 0.01f, 30.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_E_1", parent, new Vector3(14.35f, y + 0.01f, 30.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_DuctCollar_C_2", parent, new Vector3(0.0f, y + 3.76f, 37.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_W_2", parent, new Vector3(-14.62f, y + 1.58f, 37.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_E_2", parent, new Vector3(14.62f, y + 1.58f, 37.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_W_2", parent, new Vector3(-14.35f, y + 0.01f, 37.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_E_2", parent, new Vector3(14.35f, y + 0.01f, 37.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_DuctCollar_C_3", parent, new Vector3(0.0f, y + 3.76f, 43.1f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_W_3", parent, new Vector3(-14.62f, y + 1.58f, 43.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_E_3", parent, new Vector3(14.62f, y + 1.58f, 43.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_W_3", parent, new Vector3(-14.35f, y + 0.01f, 43.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_E_3", parent, new Vector3(14.35f, y + 0.01f, 43.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_DuctCollar_C_4", parent, new Vector3(0.0f, y + 3.76f, 49.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_W_4", parent, new Vector3(-14.62f, y + 1.58f, 49.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_TelemetryNode_E_4", parent, new Vector3(14.62f, y + 1.58f, 49.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_W_4", parent, new Vector3(-14.35f, y + 0.01f, 49.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_FloorBoltPlate_E_4", parent, new Vector3(14.35f, y + 0.01f, 49.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone02_Atrium_InspHatch_L_0", parent, new Vector3(-5.2f, y + 3.88f, 24.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_InspHatch_R_0", parent, new Vector3(5.2f, y + 3.88f, 24.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_RailClamp_W_0", parent, new Vector3(-14.64f, y + 2.15f, 24.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_RailClamp_E_0", parent, new Vector3(14.64f, y + 2.15f, 24.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_InspHatch_L_1", parent, new Vector3(-5.2f, y + 3.88f, 30.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_InspHatch_R_1", parent, new Vector3(5.2f, y + 3.88f, 30.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_RailClamp_W_1", parent, new Vector3(-14.64f, y + 2.15f, 30.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_RailClamp_E_1", parent, new Vector3(14.64f, y + 2.15f, 30.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_InspHatch_L_2", parent, new Vector3(-5.2f, y + 3.88f, 37.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_InspHatch_R_2", parent, new Vector3(5.2f, y + 3.88f, 37.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_RailClamp_W_2", parent, new Vector3(-14.64f, y + 2.15f, 37.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_RailClamp_E_2", parent, new Vector3(14.64f, y + 2.15f, 37.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_InspHatch_L_3", parent, new Vector3(-5.2f, y + 3.88f, 43.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_InspHatch_R_3", parent, new Vector3(5.2f, y + 3.88f, 43.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_RailClamp_W_3", parent, new Vector3(-14.64f, y + 2.15f, 43.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_RailClamp_E_3", parent, new Vector3(14.64f, y + 2.15f, 43.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_InspHatch_L_4", parent, new Vector3(-5.2f, y + 3.88f, 49.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_InspHatch_R_4", parent, new Vector3(5.2f, y + 3.88f, 49.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone02_Atrium_RailClamp_W_4", parent, new Vector3(-14.64f, y + 2.15f, 49.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone02_Atrium_RailClamp_E_4", parent, new Vector3(14.64f, y + 2.15f, 49.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone03_Calibration(Transform parent)
    {
        float y = MainY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone03_Calibration
        BuildFireExtinguisherCabinet("Zone03_Calibration_FireCab_W_0", parent, new Vector3(-46.45f, y + 1.45f, 19.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone03_Calibration_FireCab_E_0", parent, new Vector3(-17.55f, y + 1.45f, 19.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone03_Calibration_FireCab_W_1", parent, new Vector3(-46.45f, y + 1.45f, 30.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone03_Calibration_FireCab_E_1", parent, new Vector3(-17.55f, y + 1.45f, 30.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone03_Calibration_FireCab_W_2", parent, new Vector3(-46.45f, y + 1.45f, 40.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone03_Calibration_FireCab_E_2", parent, new Vector3(-17.55f, y + 1.45f, 40.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone03_Calibration_CCTV_0", parent, new Vector3(-46.55f, y + 3.35f, 17.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone03_Calibration_CCTV_1", parent, new Vector3(-17.45f, y + 3.35f, 17.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone03_Calibration_CCTV_2", parent, new Vector3(-46.55f, y + 3.35f, 42.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone03_Calibration_CCTV_3", parent, new Vector3(-17.45f, y + 3.35f, 42.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone03_Calibration_CCTV_4", parent, new Vector3(-35.5f, y + 3.35f, 17.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone03_Calibration_CCTV_5", parent, new Vector3(-28.5f, y + 3.35f, 42.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone03_Calibration_Intercom_W_0", parent, new Vector3(-46.58f, y + 1.52f, 20.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone03_Calibration_Intercom_E_0", parent, new Vector3(-17.42f, y + 1.52f, 20.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone03_Calibration_Clock_W_0", parent, new Vector3(-46.6f, y + 2.85f, 20.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone03_Calibration_Clock_E_0", parent, new Vector3(-17.4f, y + 2.85f, 20.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone03_Calibration_Intercom_W_1", parent, new Vector3(-46.58f, y + 1.52f, 26.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone03_Calibration_Intercom_E_1", parent, new Vector3(-17.42f, y + 1.52f, 26.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone03_Calibration_Intercom_W_2", parent, new Vector3(-46.58f, y + 1.52f, 33.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone03_Calibration_Intercom_E_2", parent, new Vector3(-17.42f, y + 1.52f, 33.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone03_Calibration_Clock_W_2", parent, new Vector3(-46.6f, y + 2.85f, 33.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone03_Calibration_Clock_E_2", parent, new Vector3(-17.4f, y + 2.85f, 33.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone03_Calibration_Intercom_W_3", parent, new Vector3(-46.58f, y + 1.52f, 40.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone03_Calibration_Intercom_E_3", parent, new Vector3(-17.42f, y + 1.52f, 40.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_W_0", parent, new Vector3(-46.55f, y, 19.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_E_0", parent, new Vector3(-17.45f, y, 19.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_W_1", parent, new Vector3(-46.55f, y, 26.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_E_1", parent, new Vector3(-17.45f, y, 26.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_W_2", parent, new Vector3(-46.55f, y, 33.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_E_2", parent, new Vector3(-17.45f, y, 33.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_W_3", parent, new Vector3(-46.55f, y, 41.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_E_3", parent, new Vector3(-17.45f, y, 41.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_W_4", parent, new Vector3(-46.55f, y, 48.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_E_4", parent, new Vector3(-17.45f, y, 48.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_W_5", parent, new Vector3(-46.55f, y, 55.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone03_Calibration_Radiator_E_5", parent, new Vector3(-17.45f, y, 55.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_W_0", parent, new Vector3(-46.58f, y + 3.15f, 19.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_E_0", parent, new Vector3(-17.42f, y + 3.15f, 19.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_W_0", parent, new Vector3(-45.75f, y + 3.45f, 19.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_E_0", parent, new Vector3(-18.25f, y + 3.45f, 19.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_W_1", parent, new Vector3(-46.58f, y + 3.15f, 26.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_E_1", parent, new Vector3(-17.42f, y + 3.15f, 26.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_W_1", parent, new Vector3(-45.75f, y + 3.45f, 26.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_E_1", parent, new Vector3(-18.25f, y + 3.45f, 26.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_W_2", parent, new Vector3(-46.58f, y + 3.15f, 33.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_E_2", parent, new Vector3(-17.42f, y + 3.15f, 33.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_W_2", parent, new Vector3(-45.75f, y + 3.45f, 33.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_E_2", parent, new Vector3(-18.25f, y + 3.45f, 33.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_W_3", parent, new Vector3(-46.58f, y + 3.15f, 40.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_E_3", parent, new Vector3(-17.42f, y + 3.15f, 40.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_W_3", parent, new Vector3(-45.75f, y + 3.45f, 40.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_E_3", parent, new Vector3(-18.25f, y + 3.45f, 40.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_W_4", parent, new Vector3(-46.58f, y + 3.15f, 48.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_E_4", parent, new Vector3(-17.42f, y + 3.15f, 48.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_W_4", parent, new Vector3(-45.75f, y + 3.45f, 48.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_E_4", parent, new Vector3(-18.25f, y + 3.45f, 48.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_W_5", parent, new Vector3(-46.58f, y + 3.15f, 55.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone03_Calibration_Vent_E_5", parent, new Vector3(-17.42f, y + 3.15f, 55.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_W_5", parent, new Vector3(-45.75f, y + 3.45f, 55.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone03_Calibration_PA_E_5", parent, new Vector3(-18.25f, y + 3.45f, 55.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone03_Calibration_ValveManifold_S_0", parent, new Vector3(-44.0f, y, 17.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone03_Calibration_ValveManifold_N_0", parent, new Vector3(-44.0f, y, 42.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone03_Calibration_FuseBox_S_0", parent, new Vector3(-42.8f, y + 1.65f, 17.45f), Quaternion.identity);
        BuildWallFuseBox("Zone03_Calibration_FuseBox_N_0", parent, new Vector3(-45.2f, y + 1.65f, 42.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone03_Calibration_ValveManifold_S_1", parent, new Vector3(-36.0f, y, 17.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone03_Calibration_ValveManifold_N_1", parent, new Vector3(-36.0f, y, 42.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone03_Calibration_FuseBox_S_1", parent, new Vector3(-34.8f, y + 1.65f, 17.45f), Quaternion.identity);
        BuildWallFuseBox("Zone03_Calibration_FuseBox_N_1", parent, new Vector3(-37.2f, y + 1.65f, 42.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone03_Calibration_ValveManifold_S_2", parent, new Vector3(-28.0f, y, 17.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone03_Calibration_ValveManifold_N_2", parent, new Vector3(-28.0f, y, 42.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone03_Calibration_FuseBox_S_2", parent, new Vector3(-26.8f, y + 1.65f, 17.45f), Quaternion.identity);
        BuildWallFuseBox("Zone03_Calibration_FuseBox_N_2", parent, new Vector3(-29.2f, y + 1.65f, 42.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone03_Calibration_ValveManifold_S_3", parent, new Vector3(-20.0f, y, 17.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone03_Calibration_ValveManifold_N_3", parent, new Vector3(-20.0f, y, 42.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone03_Calibration_FuseBox_S_3", parent, new Vector3(-18.8f, y + 1.65f, 17.45f), Quaternion.identity);
        BuildWallFuseBox("Zone03_Calibration_FuseBox_N_3", parent, new Vector3(-21.2f, y + 1.65f, 42.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_L_0", parent, new Vector3(-36.2f, y + 3.48f, 20.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_R_0", parent, new Vector3(-27.8f, y + 3.48f, 20.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_L_1", parent, new Vector3(-36.2f, y + 3.48f, 25.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_R_1", parent, new Vector3(-27.8f, y + 3.48f, 25.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_L_2", parent, new Vector3(-36.2f, y + 3.48f, 30.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_R_2", parent, new Vector3(-27.8f, y + 3.48f, 30.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_L_3", parent, new Vector3(-36.2f, y + 3.48f, 35.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_R_3", parent, new Vector3(-27.8f, y + 3.48f, 35.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_L_4", parent, new Vector3(-36.2f, y + 3.48f, 40.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone03_Calibration_Baffle_R_4", parent, new Vector3(-27.8f, y + 3.48f, 40.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone03_Calibration_Crates_SW", parent, new Vector3(-45.4f, y, 18.6f), 14f);
        BuildCableSpoolAndMat("Zone03_Calibration_Spool_SE", parent, new Vector3(-18.6f, y, 18.6f), -28f);
        BuildHazmatDrumCluster("Zone03_Calibration_Drums_NW", parent, new Vector3(-45.4f, y, 41.4f));
        BuildPatchPanelRack("Zone03_Calibration_PatchRack_NE", parent, new Vector3(-18.6f, y, 41.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone03_Calibration_CheckStation_S_0", parent, new Vector3(-44.6f, y, 18.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone03_Calibration_Bollard_S_0", parent, new Vector3(-45.45f, y, 17.95f));
        BuildWallWarningBollard("Zone03_Calibration_Bollard_N_0", parent, new Vector3(-43.75f, y, 42.05f));
        BuildClipboardCheckStation("Zone03_Calibration_CheckStation_S_1", parent, new Vector3(-36.2f, y, 18.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone03_Calibration_Bollard_S_1", parent, new Vector3(-37.05f, y, 17.95f));
        BuildWallWarningBollard("Zone03_Calibration_Bollard_N_1", parent, new Vector3(-35.35f, y, 42.05f));
        BuildClipboardCheckStation("Zone03_Calibration_CheckStation_S_2", parent, new Vector3(-27.8f, y, 18.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone03_Calibration_Bollard_S_2", parent, new Vector3(-28.65f, y, 17.95f));
        BuildWallWarningBollard("Zone03_Calibration_Bollard_N_2", parent, new Vector3(-26.95f, y, 42.05f));
        BuildClipboardCheckStation("Zone03_Calibration_CheckStation_S_3", parent, new Vector3(-19.4f, y, 18.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone03_Calibration_Bollard_S_3", parent, new Vector3(-20.25f, y, 17.95f));
        BuildWallWarningBollard("Zone03_Calibration_Bollard_N_3", parent, new Vector3(-18.55f, y, 42.05f));
        CreateAcousticBox("Zone03_Calibration_Pilaster_W_0", parent, new Vector3(-46.75f, y + 2.0f, 18.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateAcousticBox("Zone03_Calibration_Pilaster_E_0", parent, new Vector3(-17.25f, y + 2.0f, 18.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_W_0", parent, new Vector3(-46.62f, y + 0.32f, 18.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_E_0", parent, new Vector3(-17.38f, y + 0.32f, 18.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone03_Calibration_Pilaster_W_1", parent, new Vector3(-46.75f, y + 2.0f, 24.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateAcousticBox("Zone03_Calibration_Pilaster_E_1", parent, new Vector3(-17.25f, y + 2.0f, 24.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_W_1", parent, new Vector3(-46.62f, y + 0.32f, 24.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_E_1", parent, new Vector3(-17.38f, y + 0.32f, 24.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone03_Calibration_Pilaster_W_2", parent, new Vector3(-46.75f, y + 2.0f, 30.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateAcousticBox("Zone03_Calibration_Pilaster_E_2", parent, new Vector3(-17.25f, y + 2.0f, 30.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_W_2", parent, new Vector3(-46.62f, y + 0.32f, 30.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_E_2", parent, new Vector3(-17.38f, y + 0.32f, 30.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone03_Calibration_Pilaster_W_3", parent, new Vector3(-46.75f, y + 2.0f, 35.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateAcousticBox("Zone03_Calibration_Pilaster_E_3", parent, new Vector3(-17.25f, y + 2.0f, 35.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_W_3", parent, new Vector3(-46.62f, y + 0.32f, 35.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_E_3", parent, new Vector3(-17.38f, y + 0.32f, 35.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone03_Calibration_Pilaster_W_4", parent, new Vector3(-46.75f, y + 2.0f, 41.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateAcousticBox("Zone03_Calibration_Pilaster_E_4", parent, new Vector3(-17.25f, y + 2.0f, 41.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Glass);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_W_4", parent, new Vector3(-46.62f, y + 0.32f, 41.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_ConduitTrim_E_4", parent, new Vector3(-17.38f, y + 0.32f, 41.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingRib_0", parent, new Vector3(-32.0f, y + 3.82f, 18.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_L_0", parent, new Vector3(-40.06f, y + 3.68f, 18.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_R_0", parent, new Vector3(-23.94f, y + 3.68f, 18.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CeilingRib_1", parent, new Vector3(-32.0f, y + 3.82f, 24.25f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_L_1", parent, new Vector3(-40.06f, y + 3.68f, 24.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_R_1", parent, new Vector3(-23.94f, y + 3.68f, 24.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CeilingRib_2", parent, new Vector3(-32.0f, y + 3.82f, 30.0f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_L_2", parent, new Vector3(-40.06f, y + 3.68f, 30.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_R_2", parent, new Vector3(-23.94f, y + 3.68f, 30.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CeilingRib_3", parent, new Vector3(-32.0f, y + 3.82f, 35.75f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_L_3", parent, new Vector3(-40.06f, y + 3.68f, 35.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_R_3", parent, new Vector3(-23.94f, y + 3.68f, 35.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CeilingRib_4", parent, new Vector3(-32.0f, y + 3.82f, 41.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_L_4", parent, new Vector3(-40.06f, y + 3.68f, 41.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CableTrayHanger_R_4", parent, new Vector3(-23.94f, y + 3.68f, 41.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_FloorSafetyLine_S_0", parent, new Vector3(-44.2f, y + 0.014f, 18.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_FloorSafetyLine_N_0", parent, new Vector3(-44.2f, y + 0.014f, 41.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_WallDataPlaque_S_0", parent, new Vector3(-44.2f, y + 2.25f, 17.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_WallDataPlaque_N_0", parent, new Vector3(-44.2f, y + 2.25f, 42.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_FloorSafetyLine_S_1", parent, new Vector3(-36.07f, y + 0.014f, 18.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_FloorSafetyLine_N_1", parent, new Vector3(-36.07f, y + 0.014f, 41.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_WallDataPlaque_S_1", parent, new Vector3(-36.07f, y + 2.25f, 17.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_WallDataPlaque_N_1", parent, new Vector3(-36.07f, y + 2.25f, 42.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_FloorSafetyLine_S_2", parent, new Vector3(-27.93f, y + 0.014f, 18.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_FloorSafetyLine_N_2", parent, new Vector3(-27.93f, y + 0.014f, 41.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_WallDataPlaque_S_2", parent, new Vector3(-27.93f, y + 2.25f, 17.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_WallDataPlaque_N_2", parent, new Vector3(-27.93f, y + 2.25f, 42.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_FloorSafetyLine_S_3", parent, new Vector3(-19.8f, y + 0.014f, 18.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_FloorSafetyLine_N_3", parent, new Vector3(-19.8f, y + 0.014f, 41.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_WallDataPlaque_S_3", parent, new Vector3(-19.8f, y + 2.25f, 17.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_WallDataPlaque_N_3", parent, new Vector3(-19.8f, y + 2.25f, 42.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_AcousticSensor_W_0", parent, new Vector3(-46.58f, y + 2.45f, 19.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_AcousticSensorLed_W_0", parent, new Vector3(-46.49f, y + 2.48f, 19.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone03_Calibration_AcousticSensor_E_0", parent, new Vector3(-17.42f, y + 2.45f, 19.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_AcousticSensorLed_E_0", parent, new Vector3(-17.51f, y + 2.48f, 19.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone03_Calibration_AcousticSensor_W_1", parent, new Vector3(-46.58f, y + 2.45f, 26.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_AcousticSensorLed_W_1", parent, new Vector3(-46.49f, y + 2.48f, 26.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone03_Calibration_AcousticSensor_E_1", parent, new Vector3(-17.42f, y + 2.45f, 26.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_AcousticSensorLed_E_1", parent, new Vector3(-17.51f, y + 2.48f, 26.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone03_Calibration_AcousticSensor_W_2", parent, new Vector3(-46.58f, y + 2.45f, 33.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_AcousticSensorLed_W_2", parent, new Vector3(-46.49f, y + 2.48f, 33.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone03_Calibration_AcousticSensor_E_2", parent, new Vector3(-17.42f, y + 2.45f, 33.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_AcousticSensorLed_E_2", parent, new Vector3(-17.51f, y + 2.48f, 33.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone03_Calibration_AcousticSensor_W_3", parent, new Vector3(-46.58f, y + 2.45f, 40.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_AcousticSensorLed_W_3", parent, new Vector3(-46.49f, y + 2.48f, 40.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone03_Calibration_AcousticSensor_E_3", parent, new Vector3(-17.42f, y + 2.45f, 40.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_AcousticSensorLed_E_3", parent, new Vector3(-17.51f, y + 2.48f, 40.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone03_Calibration_DrainStrip_L_0", parent, new Vector3(-34.6f, y + 0.012f, 19.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_DrainStrip_R_0", parent, new Vector3(-29.4f, y + 0.012f, 19.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallCableClamp_W_0", parent, new Vector3(-46.64f, y + 1.15f, 19.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_WallCableClamp_E_0", parent, new Vector3(-17.36f, y + 1.15f, 19.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_DrainStrip_L_1", parent, new Vector3(-34.6f, y + 0.012f, 26.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_DrainStrip_R_1", parent, new Vector3(-29.4f, y + 0.012f, 26.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallCableClamp_W_1", parent, new Vector3(-46.64f, y + 1.15f, 26.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_WallCableClamp_E_1", parent, new Vector3(-17.36f, y + 1.15f, 26.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_DrainStrip_L_2", parent, new Vector3(-34.6f, y + 0.012f, 33.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_DrainStrip_R_2", parent, new Vector3(-29.4f, y + 0.012f, 33.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallCableClamp_W_2", parent, new Vector3(-46.64f, y + 1.15f, 33.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_WallCableClamp_E_2", parent, new Vector3(-17.36f, y + 1.15f, 33.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_DrainStrip_L_3", parent, new Vector3(-34.6f, y + 0.012f, 41.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_DrainStrip_R_3", parent, new Vector3(-29.4f, y + 0.012f, 41.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallCableClamp_W_3", parent, new Vector3(-46.64f, y + 1.15f, 41.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_WallCableClamp_E_3", parent, new Vector3(-17.36f, y + 1.15f, 41.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_Baseboard_S_0", parent, new Vector3(-44.8f, y + 0.12f, 17.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_Baseboard_N_0", parent, new Vector3(-44.8f, y + 0.12f, 42.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingPipe_S_0", parent, new Vector3(-44.8f, y + 3.55f, 18.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_CeilingPipe_N_0", parent, new Vector3(-44.8f, y + 3.55f, 41.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_Baseboard_S_1", parent, new Vector3(-36.27f, y + 0.12f, 17.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_Baseboard_N_1", parent, new Vector3(-36.27f, y + 0.12f, 42.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingPipe_S_1", parent, new Vector3(-36.27f, y + 3.55f, 18.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_CeilingPipe_N_1", parent, new Vector3(-36.27f, y + 3.55f, 41.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_Baseboard_S_2", parent, new Vector3(-27.73f, y + 0.12f, 17.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_Baseboard_N_2", parent, new Vector3(-27.73f, y + 0.12f, 42.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingPipe_S_2", parent, new Vector3(-27.73f, y + 3.55f, 18.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_CeilingPipe_N_2", parent, new Vector3(-27.73f, y + 3.55f, 41.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_Baseboard_S_3", parent, new Vector3(-19.2f, y + 0.12f, 17.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_Baseboard_N_3", parent, new Vector3(-19.2f, y + 0.12f, 42.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingPipe_S_3", parent, new Vector3(-19.2f, y + 3.55f, 18.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_CeilingPipe_N_3", parent, new Vector3(-19.2f, y + 3.55f, 41.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone03_Calibration_AcousticPanel_W_0", parent, new Vector3(-46.64f, y + 1.85f, 19.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Glass);
        CreateAcousticBox("Zone03_Calibration_AcousticPanel_E_0", parent, new Vector3(-17.36f, y + 1.85f, 19.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Glass);
        CreateDecorBox("Zone03_Calibration_StrobeMount_W_0", parent, new Vector3(-46.6f, y + 3.05f, 20.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone03_Calibration_StrobeMount_E_0", parent, new Vector3(-17.4f, y + 3.05f, 18.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone03_Calibration_AcousticPanel_W_1", parent, new Vector3(-46.64f, y + 1.85f, 26.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Glass);
        CreateAcousticBox("Zone03_Calibration_AcousticPanel_E_1", parent, new Vector3(-17.36f, y + 1.85f, 26.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Glass);
        CreateDecorBox("Zone03_Calibration_StrobeMount_W_1", parent, new Vector3(-46.6f, y + 3.05f, 27.18f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone03_Calibration_StrobeMount_E_1", parent, new Vector3(-17.4f, y + 3.05f, 25.88f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone03_Calibration_AcousticPanel_W_2", parent, new Vector3(-46.64f, y + 1.85f, 33.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Glass);
        CreateAcousticBox("Zone03_Calibration_AcousticPanel_E_2", parent, new Vector3(-17.36f, y + 1.85f, 33.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Glass);
        CreateDecorBox("Zone03_Calibration_StrobeMount_W_2", parent, new Vector3(-46.6f, y + 3.05f, 34.12f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone03_Calibration_StrobeMount_E_2", parent, new Vector3(-17.4f, y + 3.05f, 32.82f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone03_Calibration_AcousticPanel_W_3", parent, new Vector3(-46.64f, y + 1.85f, 40.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Glass);
        CreateAcousticBox("Zone03_Calibration_AcousticPanel_E_3", parent, new Vector3(-17.36f, y + 1.85f, 40.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Glass);
        CreateDecorBox("Zone03_Calibration_StrobeMount_W_3", parent, new Vector3(-46.6f, y + 3.05f, 41.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone03_Calibration_StrobeMount_E_3", parent, new Vector3(-17.4f, y + 3.05f, 39.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone03_Calibration_CeilingVentBox_S_0", parent, new Vector3(-44.4f, y + 3.86f, 26.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CeilingVentBox_N_0", parent, new Vector3(-44.4f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_VerticalConduit_S_0", parent, new Vector3(-44.4f, y + 1.95f, 17.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_VerticalConduit_N_0", parent, new Vector3(-44.4f, y + 1.95f, 42.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_CeilingVentBox_S_1", parent, new Vector3(-36.13f, y + 3.86f, 26.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CeilingVentBox_N_1", parent, new Vector3(-36.13f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_VerticalConduit_S_1", parent, new Vector3(-36.13f, y + 1.95f, 17.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_VerticalConduit_N_1", parent, new Vector3(-36.13f, y + 1.95f, 42.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_CeilingVentBox_S_2", parent, new Vector3(-27.87f, y + 3.86f, 26.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CeilingVentBox_N_2", parent, new Vector3(-27.87f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_VerticalConduit_S_2", parent, new Vector3(-27.87f, y + 1.95f, 17.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_VerticalConduit_N_2", parent, new Vector3(-27.87f, y + 1.95f, 42.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_CeilingVentBox_S_3", parent, new Vector3(-19.6f, y + 3.86f, 26.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_CeilingVentBox_N_3", parent, new Vector3(-19.6f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_VerticalConduit_S_3", parent, new Vector3(-19.6f, y + 1.95f, 17.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_VerticalConduit_N_3", parent, new Vector3(-19.6f, y + 1.95f, 42.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_UpperTrim_W_0", parent, new Vector3(-46.66f, y + 3.65f, 18.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_UpperTrim_E_0", parent, new Vector3(-17.34f, y + 3.65f, 18.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_MidStripe_W_0", parent, new Vector3(-46.67f, y + 1.32f, 18.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_MidStripe_E_0", parent, new Vector3(-17.33f, y + 1.32f, 18.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_UpperTrim_W_1", parent, new Vector3(-46.66f, y + 3.65f, 24.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_UpperTrim_E_1", parent, new Vector3(-17.34f, y + 3.65f, 24.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_MidStripe_W_1", parent, new Vector3(-46.67f, y + 1.32f, 24.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_MidStripe_E_1", parent, new Vector3(-17.33f, y + 1.32f, 24.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_UpperTrim_W_2", parent, new Vector3(-46.66f, y + 3.65f, 30.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_UpperTrim_E_2", parent, new Vector3(-17.34f, y + 3.65f, 30.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_MidStripe_W_2", parent, new Vector3(-46.67f, y + 1.32f, 30.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_MidStripe_E_2", parent, new Vector3(-17.33f, y + 1.32f, 30.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_UpperTrim_W_3", parent, new Vector3(-46.66f, y + 3.65f, 35.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_UpperTrim_E_3", parent, new Vector3(-17.34f, y + 3.65f, 35.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_MidStripe_W_3", parent, new Vector3(-46.67f, y + 1.32f, 35.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_MidStripe_E_3", parent, new Vector3(-17.33f, y + 1.32f, 35.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_UpperTrim_W_4", parent, new Vector3(-46.66f, y + 3.65f, 41.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_UpperTrim_E_4", parent, new Vector3(-17.34f, y + 3.65f, 41.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone03_Calibration_MidStripe_W_4", parent, new Vector3(-46.67f, y + 1.32f, 41.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone03_Calibration_MidStripe_E_4", parent, new Vector3(-17.33f, y + 1.32f, 41.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_L_0", parent, new Vector3(-35.2f, y + 3.84f, 18.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_R_0", parent, new Vector3(-28.8f, y + 3.84f, 18.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_WallJunction_W_0", parent, new Vector3(-46.62f, y + 0.78f, 18.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallJunction_E_0", parent, new Vector3(-17.38f, y + 0.78f, 18.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_L_1", parent, new Vector3(-35.2f, y + 3.84f, 24.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_R_1", parent, new Vector3(-28.8f, y + 3.84f, 24.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_WallJunction_W_1", parent, new Vector3(-46.62f, y + 0.78f, 24.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallJunction_E_1", parent, new Vector3(-17.38f, y + 0.78f, 24.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_L_2", parent, new Vector3(-35.2f, y + 3.84f, 30.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_R_2", parent, new Vector3(-28.8f, y + 3.84f, 30.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_WallJunction_W_2", parent, new Vector3(-46.62f, y + 0.78f, 30.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallJunction_E_2", parent, new Vector3(-17.38f, y + 0.78f, 30.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_L_3", parent, new Vector3(-35.2f, y + 3.84f, 35.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_R_3", parent, new Vector3(-28.8f, y + 3.84f, 35.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_WallJunction_W_3", parent, new Vector3(-46.62f, y + 0.78f, 35.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallJunction_E_3", parent, new Vector3(-17.38f, y + 0.78f, 35.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_L_4", parent, new Vector3(-35.2f, y + 3.84f, 41.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone03_Calibration_Sprinkler_R_4", parent, new Vector3(-28.8f, y + 3.84f, 41.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_WallJunction_W_4", parent, new Vector3(-46.62f, y + 0.78f, 41.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallJunction_E_4", parent, new Vector3(-17.38f, y + 0.78f, 41.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_FloorJoint_X_0", parent, new Vector3(-45.0f, y + 0.008f, 30.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingStrut_S_0", parent, new Vector3(-45.0f, y + 3.74f, 19.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingStrut_N_0", parent, new Vector3(-45.0f, y + 3.74f, 40.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallBracket_S_0", parent, new Vector3(-45.0f, y + 2.85f, 17.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_WallBracket_N_0", parent, new Vector3(-45.0f, y + 2.85f, 42.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_FloorJoint_X_1", parent, new Vector3(-36.33f, y + 0.008f, 30.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingStrut_S_1", parent, new Vector3(-36.33f, y + 3.74f, 19.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingStrut_N_1", parent, new Vector3(-36.33f, y + 3.74f, 40.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallBracket_S_1", parent, new Vector3(-36.33f, y + 2.85f, 17.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_WallBracket_N_1", parent, new Vector3(-36.33f, y + 2.85f, 42.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_FloorJoint_X_2", parent, new Vector3(-27.67f, y + 0.008f, 30.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingStrut_S_2", parent, new Vector3(-27.67f, y + 3.74f, 19.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingStrut_N_2", parent, new Vector3(-27.67f, y + 3.74f, 40.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallBracket_S_2", parent, new Vector3(-27.67f, y + 2.85f, 17.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_WallBracket_N_2", parent, new Vector3(-27.67f, y + 2.85f, 42.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_FloorJoint_X_3", parent, new Vector3(-19.0f, y + 0.008f, 30.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingStrut_S_3", parent, new Vector3(-19.0f, y + 3.74f, 19.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_CeilingStrut_N_3", parent, new Vector3(-19.0f, y + 3.74f, 40.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_WallBracket_S_3", parent, new Vector3(-19.0f, y + 2.85f, 17.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_WallBracket_N_3", parent, new Vector3(-19.0f, y + 2.85f, 42.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_LowerPipe_W_0", parent, new Vector3(-46.65f, y + 0.55f, 18.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_LowerPipe_E_0", parent, new Vector3(-17.35f, y + 0.55f, 18.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_HighPipe_W_0", parent, new Vector3(-46.65f, y + 3.38f, 18.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_HighPipe_E_0", parent, new Vector3(-17.35f, y + 3.38f, 18.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_LowerPipe_W_1", parent, new Vector3(-46.65f, y + 0.55f, 24.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_LowerPipe_E_1", parent, new Vector3(-17.35f, y + 0.55f, 24.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_HighPipe_W_1", parent, new Vector3(-46.65f, y + 3.38f, 24.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_HighPipe_E_1", parent, new Vector3(-17.35f, y + 3.38f, 24.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_LowerPipe_W_2", parent, new Vector3(-46.65f, y + 0.55f, 30.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_LowerPipe_E_2", parent, new Vector3(-17.35f, y + 0.55f, 30.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_HighPipe_W_2", parent, new Vector3(-46.65f, y + 3.38f, 30.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_HighPipe_E_2", parent, new Vector3(-17.35f, y + 3.38f, 30.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_LowerPipe_W_3", parent, new Vector3(-46.65f, y + 0.55f, 35.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_LowerPipe_E_3", parent, new Vector3(-17.35f, y + 0.55f, 35.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_HighPipe_W_3", parent, new Vector3(-46.65f, y + 3.38f, 35.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_HighPipe_E_3", parent, new Vector3(-17.35f, y + 3.38f, 35.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_LowerPipe_W_4", parent, new Vector3(-46.65f, y + 0.55f, 41.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_LowerPipe_E_4", parent, new Vector3(-17.35f, y + 0.55f, 41.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone03_Calibration_HighPipe_W_4", parent, new Vector3(-46.65f, y + 3.38f, 41.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_HighPipe_E_4", parent, new Vector3(-17.35f, y + 3.38f, 41.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone03_Calibration_DuctCollar_C_0", parent, new Vector3(-32.0f, y + 3.76f, 18.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_W_0", parent, new Vector3(-46.62f, y + 1.58f, 18.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_E_0", parent, new Vector3(-17.38f, y + 1.58f, 18.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_W_0", parent, new Vector3(-46.35f, y + 0.01f, 18.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_E_0", parent, new Vector3(-17.65f, y + 0.01f, 18.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_DuctCollar_C_1", parent, new Vector3(-32.0f, y + 3.76f, 24.4f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_W_1", parent, new Vector3(-46.62f, y + 1.58f, 24.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_E_1", parent, new Vector3(-17.38f, y + 1.58f, 24.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_W_1", parent, new Vector3(-46.35f, y + 0.01f, 24.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_E_1", parent, new Vector3(-17.65f, y + 0.01f, 24.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_DuctCollar_C_2", parent, new Vector3(-32.0f, y + 3.76f, 30.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_W_2", parent, new Vector3(-46.62f, y + 1.58f, 30.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_E_2", parent, new Vector3(-17.38f, y + 1.58f, 30.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_W_2", parent, new Vector3(-46.35f, y + 0.01f, 30.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_E_2", parent, new Vector3(-17.65f, y + 0.01f, 30.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_DuctCollar_C_3", parent, new Vector3(-32.0f, y + 3.76f, 35.6f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_W_3", parent, new Vector3(-46.62f, y + 1.58f, 35.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_E_3", parent, new Vector3(-17.38f, y + 1.58f, 35.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_W_3", parent, new Vector3(-46.35f, y + 0.01f, 35.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_E_3", parent, new Vector3(-17.65f, y + 0.01f, 35.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_DuctCollar_C_4", parent, new Vector3(-32.0f, y + 3.76f, 41.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_W_4", parent, new Vector3(-46.62f, y + 1.58f, 41.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_TelemetryNode_E_4", parent, new Vector3(-17.38f, y + 1.58f, 41.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_W_4", parent, new Vector3(-46.35f, y + 0.01f, 41.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_FloorBoltPlate_E_4", parent, new Vector3(-17.65f, y + 0.01f, 41.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone03_Calibration_InspHatch_L_0", parent, new Vector3(-37.2f, y + 3.88f, 18.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_InspHatch_R_0", parent, new Vector3(-26.8f, y + 3.88f, 18.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_RailClamp_W_0", parent, new Vector3(-46.64f, y + 2.15f, 18.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_RailClamp_E_0", parent, new Vector3(-17.36f, y + 2.15f, 18.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_InspHatch_L_1", parent, new Vector3(-37.2f, y + 3.88f, 24.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_InspHatch_R_1", parent, new Vector3(-26.8f, y + 3.88f, 24.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_RailClamp_W_1", parent, new Vector3(-46.64f, y + 2.15f, 24.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_RailClamp_E_1", parent, new Vector3(-17.36f, y + 2.15f, 24.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_InspHatch_L_2", parent, new Vector3(-37.2f, y + 3.88f, 30.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_InspHatch_R_2", parent, new Vector3(-26.8f, y + 3.88f, 30.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_RailClamp_W_2", parent, new Vector3(-46.64f, y + 2.15f, 30.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_RailClamp_E_2", parent, new Vector3(-17.36f, y + 2.15f, 30.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_InspHatch_L_3", parent, new Vector3(-37.2f, y + 3.88f, 35.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_InspHatch_R_3", parent, new Vector3(-26.8f, y + 3.88f, 35.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_RailClamp_W_3", parent, new Vector3(-46.64f, y + 2.15f, 35.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_RailClamp_E_3", parent, new Vector3(-17.36f, y + 2.15f, 35.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_InspHatch_L_4", parent, new Vector3(-37.2f, y + 3.88f, 41.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_InspHatch_R_4", parent, new Vector3(-26.8f, y + 3.88f, 41.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone03_Calibration_RailClamp_W_4", parent, new Vector3(-46.64f, y + 2.15f, 41.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone03_Calibration_RailClamp_E_4", parent, new Vector3(-17.36f, y + 2.15f, 41.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone04_Acoustics(Transform parent)
    {
        float y = MainY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone04_Acoustics
        BuildFireExtinguisherCabinet("Zone04_Acoustics_FireCab_W_0", parent, new Vector3(-46.45f, y + 1.45f, 47.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone04_Acoustics_FireCab_E_0", parent, new Vector3(-17.55f, y + 1.45f, 47.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone04_Acoustics_FireCab_W_1", parent, new Vector3(-46.45f, y + 1.45f, 59.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone04_Acoustics_FireCab_E_1", parent, new Vector3(-17.55f, y + 1.45f, 59.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone04_Acoustics_FireCab_W_2", parent, new Vector3(-46.45f, y + 1.45f, 70.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone04_Acoustics_FireCab_E_2", parent, new Vector3(-17.55f, y + 1.45f, 70.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone04_Acoustics_CCTV_0", parent, new Vector3(-46.55f, y + 3.35f, 45.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone04_Acoustics_CCTV_1", parent, new Vector3(-17.45f, y + 3.35f, 45.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone04_Acoustics_CCTV_2", parent, new Vector3(-46.55f, y + 3.35f, 72.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone04_Acoustics_CCTV_3", parent, new Vector3(-17.45f, y + 3.35f, 72.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone04_Acoustics_CCTV_4", parent, new Vector3(-35.5f, y + 3.35f, 45.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone04_Acoustics_CCTV_5", parent, new Vector3(-28.5f, y + 3.35f, 72.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone04_Acoustics_Intercom_W_0", parent, new Vector3(-46.58f, y + 1.52f, 48.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone04_Acoustics_Intercom_E_0", parent, new Vector3(-17.42f, y + 1.52f, 48.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone04_Acoustics_Clock_W_0", parent, new Vector3(-46.6f, y + 2.85f, 48.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone04_Acoustics_Clock_E_0", parent, new Vector3(-17.4f, y + 2.85f, 48.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone04_Acoustics_Intercom_W_1", parent, new Vector3(-46.58f, y + 1.52f, 55.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone04_Acoustics_Intercom_E_1", parent, new Vector3(-17.42f, y + 1.52f, 55.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone04_Acoustics_Intercom_W_2", parent, new Vector3(-46.58f, y + 1.52f, 62.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone04_Acoustics_Intercom_E_2", parent, new Vector3(-17.42f, y + 1.52f, 62.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone04_Acoustics_Clock_W_2", parent, new Vector3(-46.6f, y + 2.85f, 62.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone04_Acoustics_Clock_E_2", parent, new Vector3(-17.4f, y + 2.85f, 62.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone04_Acoustics_Intercom_W_3", parent, new Vector3(-46.58f, y + 1.52f, 70.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone04_Acoustics_Intercom_E_3", parent, new Vector3(-17.42f, y + 1.52f, 70.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_W_0", parent, new Vector3(-46.55f, y, 47.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_E_0", parent, new Vector3(-17.45f, y, 47.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_W_1", parent, new Vector3(-46.55f, y, 55.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_E_1", parent, new Vector3(-17.45f, y, 55.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_W_2", parent, new Vector3(-46.55f, y, 63.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_E_2", parent, new Vector3(-17.45f, y, 63.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_W_3", parent, new Vector3(-46.55f, y, 71.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_E_3", parent, new Vector3(-17.45f, y, 71.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_W_4", parent, new Vector3(-46.55f, y, 79.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_E_4", parent, new Vector3(-17.45f, y, 79.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_W_5", parent, new Vector3(-46.55f, y, 87.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone04_Acoustics_Radiator_E_5", parent, new Vector3(-17.45f, y, 87.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_W_0", parent, new Vector3(-46.58f, y + 3.15f, 47.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_E_0", parent, new Vector3(-17.42f, y + 3.15f, 47.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_W_0", parent, new Vector3(-45.75f, y + 3.45f, 47.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_E_0", parent, new Vector3(-18.25f, y + 3.45f, 47.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_W_1", parent, new Vector3(-46.58f, y + 3.15f, 55.07f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_E_1", parent, new Vector3(-17.42f, y + 3.15f, 55.07f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_W_1", parent, new Vector3(-45.75f, y + 3.45f, 55.07f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_E_1", parent, new Vector3(-18.25f, y + 3.45f, 55.07f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_W_2", parent, new Vector3(-46.58f, y + 3.15f, 62.93f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_E_2", parent, new Vector3(-17.42f, y + 3.15f, 62.93f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_W_2", parent, new Vector3(-45.75f, y + 3.45f, 62.93f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_E_2", parent, new Vector3(-18.25f, y + 3.45f, 62.93f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_W_3", parent, new Vector3(-46.58f, y + 3.15f, 70.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_E_3", parent, new Vector3(-17.42f, y + 3.15f, 70.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_W_3", parent, new Vector3(-45.75f, y + 3.45f, 70.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_E_3", parent, new Vector3(-18.25f, y + 3.45f, 70.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_W_4", parent, new Vector3(-46.58f, y + 3.15f, 78.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_E_4", parent, new Vector3(-17.42f, y + 3.15f, 78.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_W_4", parent, new Vector3(-45.75f, y + 3.45f, 78.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_E_4", parent, new Vector3(-18.25f, y + 3.45f, 78.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_W_5", parent, new Vector3(-46.58f, y + 3.15f, 86.53f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone04_Acoustics_Vent_E_5", parent, new Vector3(-17.42f, y + 3.15f, 86.53f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_W_5", parent, new Vector3(-45.75f, y + 3.45f, 86.53f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone04_Acoustics_PA_E_5", parent, new Vector3(-18.25f, y + 3.45f, 86.53f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone04_Acoustics_ValveManifold_S_0", parent, new Vector3(-44.0f, y, 45.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone04_Acoustics_ValveManifold_N_0", parent, new Vector3(-44.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone04_Acoustics_FuseBox_S_0", parent, new Vector3(-42.8f, y + 1.65f, 45.45f), Quaternion.identity);
        BuildWallFuseBox("Zone04_Acoustics_FuseBox_N_0", parent, new Vector3(-45.2f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone04_Acoustics_ValveManifold_S_1", parent, new Vector3(-36.0f, y, 45.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone04_Acoustics_ValveManifold_N_1", parent, new Vector3(-36.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone04_Acoustics_FuseBox_S_1", parent, new Vector3(-34.8f, y + 1.65f, 45.45f), Quaternion.identity);
        BuildWallFuseBox("Zone04_Acoustics_FuseBox_N_1", parent, new Vector3(-37.2f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone04_Acoustics_ValveManifold_S_2", parent, new Vector3(-28.0f, y, 45.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone04_Acoustics_ValveManifold_N_2", parent, new Vector3(-28.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone04_Acoustics_FuseBox_S_2", parent, new Vector3(-26.8f, y + 1.65f, 45.45f), Quaternion.identity);
        BuildWallFuseBox("Zone04_Acoustics_FuseBox_N_2", parent, new Vector3(-29.2f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone04_Acoustics_ValveManifold_S_3", parent, new Vector3(-20.0f, y, 45.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone04_Acoustics_ValveManifold_N_3", parent, new Vector3(-20.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone04_Acoustics_FuseBox_S_3", parent, new Vector3(-18.8f, y + 1.65f, 45.45f), Quaternion.identity);
        BuildWallFuseBox("Zone04_Acoustics_FuseBox_N_3", parent, new Vector3(-21.2f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_L_0", parent, new Vector3(-36.2f, y + 3.48f, 48.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_R_0", parent, new Vector3(-27.8f, y + 3.48f, 48.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_L_1", parent, new Vector3(-36.2f, y + 3.48f, 53.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_R_1", parent, new Vector3(-27.8f, y + 3.48f, 53.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_L_2", parent, new Vector3(-36.2f, y + 3.48f, 59.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_R_2", parent, new Vector3(-27.8f, y + 3.48f, 59.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_L_3", parent, new Vector3(-36.2f, y + 3.48f, 64.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_R_3", parent, new Vector3(-27.8f, y + 3.48f, 64.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_L_4", parent, new Vector3(-36.2f, y + 3.48f, 70.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone04_Acoustics_Baffle_R_4", parent, new Vector3(-27.8f, y + 3.48f, 70.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone04_Acoustics_Crates_SW", parent, new Vector3(-45.4f, y, 46.6f), 14f);
        BuildCableSpoolAndMat("Zone04_Acoustics_Spool_SE", parent, new Vector3(-18.6f, y, 46.6f), -28f);
        BuildHazmatDrumCluster("Zone04_Acoustics_Drums_NW", parent, new Vector3(-45.4f, y, 71.4f));
        BuildPatchPanelRack("Zone04_Acoustics_PatchRack_NE", parent, new Vector3(-18.6f, y, 71.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone04_Acoustics_CheckStation_S_0", parent, new Vector3(-44.6f, y, 46.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone04_Acoustics_Bollard_S_0", parent, new Vector3(-45.45f, y, 45.95f));
        BuildWallWarningBollard("Zone04_Acoustics_Bollard_N_0", parent, new Vector3(-43.75f, y, 72.05f));
        BuildClipboardCheckStation("Zone04_Acoustics_CheckStation_S_1", parent, new Vector3(-36.2f, y, 46.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone04_Acoustics_Bollard_S_1", parent, new Vector3(-37.05f, y, 45.95f));
        BuildWallWarningBollard("Zone04_Acoustics_Bollard_N_1", parent, new Vector3(-35.35f, y, 72.05f));
        BuildClipboardCheckStation("Zone04_Acoustics_CheckStation_S_2", parent, new Vector3(-27.8f, y, 46.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone04_Acoustics_Bollard_S_2", parent, new Vector3(-28.65f, y, 45.95f));
        BuildWallWarningBollard("Zone04_Acoustics_Bollard_N_2", parent, new Vector3(-26.95f, y, 72.05f));
        BuildClipboardCheckStation("Zone04_Acoustics_CheckStation_S_3", parent, new Vector3(-19.4f, y, 46.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone04_Acoustics_Bollard_S_3", parent, new Vector3(-20.25f, y, 45.95f));
        BuildWallWarningBollard("Zone04_Acoustics_Bollard_N_3", parent, new Vector3(-18.55f, y, 72.05f));
        CreateAcousticBox("Zone04_Acoustics_Pilaster_W_0", parent, new Vector3(-46.75f, y + 2.0f, 46.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Zone04_Acoustics_Pilaster_E_0", parent, new Vector3(-17.25f, y + 2.0f, 46.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_W_0", parent, new Vector3(-46.62f, y + 0.32f, 46.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_E_0", parent, new Vector3(-17.38f, y + 0.32f, 46.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone04_Acoustics_Pilaster_W_1", parent, new Vector3(-46.75f, y + 2.0f, 52.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Zone04_Acoustics_Pilaster_E_1", parent, new Vector3(-17.25f, y + 2.0f, 52.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_W_1", parent, new Vector3(-46.62f, y + 0.32f, 52.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_E_1", parent, new Vector3(-17.38f, y + 0.32f, 52.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone04_Acoustics_Pilaster_W_2", parent, new Vector3(-46.75f, y + 2.0f, 59.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Zone04_Acoustics_Pilaster_E_2", parent, new Vector3(-17.25f, y + 2.0f, 59.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_W_2", parent, new Vector3(-46.62f, y + 0.32f, 59.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_E_2", parent, new Vector3(-17.38f, y + 0.32f, 59.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone04_Acoustics_Pilaster_W_3", parent, new Vector3(-46.75f, y + 2.0f, 65.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Zone04_Acoustics_Pilaster_E_3", parent, new Vector3(-17.25f, y + 2.0f, 65.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_W_3", parent, new Vector3(-46.62f, y + 0.32f, 65.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_E_3", parent, new Vector3(-17.38f, y + 0.32f, 65.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone04_Acoustics_Pilaster_W_4", parent, new Vector3(-46.75f, y + 2.0f, 71.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Zone04_Acoustics_Pilaster_E_4", parent, new Vector3(-17.25f, y + 2.0f, 71.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.AcousticFoam);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_W_4", parent, new Vector3(-46.62f, y + 0.32f, 71.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_ConduitTrim_E_4", parent, new Vector3(-17.38f, y + 0.32f, 71.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingRib_0", parent, new Vector3(-32.0f, y + 3.82f, 46.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_L_0", parent, new Vector3(-40.06f, y + 3.68f, 46.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_R_0", parent, new Vector3(-23.94f, y + 3.68f, 46.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CeilingRib_1", parent, new Vector3(-32.0f, y + 3.82f, 52.75f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_L_1", parent, new Vector3(-40.06f, y + 3.68f, 52.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_R_1", parent, new Vector3(-23.94f, y + 3.68f, 52.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CeilingRib_2", parent, new Vector3(-32.0f, y + 3.82f, 59.0f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_L_2", parent, new Vector3(-40.06f, y + 3.68f, 59.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_R_2", parent, new Vector3(-23.94f, y + 3.68f, 59.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CeilingRib_3", parent, new Vector3(-32.0f, y + 3.82f, 65.25f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_L_3", parent, new Vector3(-40.06f, y + 3.68f, 65.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_R_3", parent, new Vector3(-23.94f, y + 3.68f, 65.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CeilingRib_4", parent, new Vector3(-32.0f, y + 3.82f, 71.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_L_4", parent, new Vector3(-40.06f, y + 3.68f, 71.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CableTrayHanger_R_4", parent, new Vector3(-23.94f, y + 3.68f, 71.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_FloorSafetyLine_S_0", parent, new Vector3(-44.2f, y + 0.014f, 46.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_FloorSafetyLine_N_0", parent, new Vector3(-44.2f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_WallDataPlaque_S_0", parent, new Vector3(-44.2f, y + 2.25f, 45.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_WallDataPlaque_N_0", parent, new Vector3(-44.2f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_FloorSafetyLine_S_1", parent, new Vector3(-36.07f, y + 0.014f, 46.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_FloorSafetyLine_N_1", parent, new Vector3(-36.07f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_WallDataPlaque_S_1", parent, new Vector3(-36.07f, y + 2.25f, 45.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_WallDataPlaque_N_1", parent, new Vector3(-36.07f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_FloorSafetyLine_S_2", parent, new Vector3(-27.93f, y + 0.014f, 46.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_FloorSafetyLine_N_2", parent, new Vector3(-27.93f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_WallDataPlaque_S_2", parent, new Vector3(-27.93f, y + 2.25f, 45.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_WallDataPlaque_N_2", parent, new Vector3(-27.93f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_FloorSafetyLine_S_3", parent, new Vector3(-19.8f, y + 0.014f, 46.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_FloorSafetyLine_N_3", parent, new Vector3(-19.8f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_WallDataPlaque_S_3", parent, new Vector3(-19.8f, y + 2.25f, 45.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_WallDataPlaque_N_3", parent, new Vector3(-19.8f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_AcousticSensor_W_0", parent, new Vector3(-46.58f, y + 2.45f, 47.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_AcousticSensorLed_W_0", parent, new Vector3(-46.49f, y + 2.48f, 47.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone04_Acoustics_AcousticSensor_E_0", parent, new Vector3(-17.42f, y + 2.45f, 47.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_AcousticSensorLed_E_0", parent, new Vector3(-17.51f, y + 2.48f, 47.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone04_Acoustics_AcousticSensor_W_1", parent, new Vector3(-46.58f, y + 2.45f, 55.13f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_AcousticSensorLed_W_1", parent, new Vector3(-46.49f, y + 2.48f, 55.13f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone04_Acoustics_AcousticSensor_E_1", parent, new Vector3(-17.42f, y + 2.45f, 55.13f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_AcousticSensorLed_E_1", parent, new Vector3(-17.51f, y + 2.48f, 55.13f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone04_Acoustics_AcousticSensor_W_2", parent, new Vector3(-46.58f, y + 2.45f, 62.87f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_AcousticSensorLed_W_2", parent, new Vector3(-46.49f, y + 2.48f, 62.87f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone04_Acoustics_AcousticSensor_E_2", parent, new Vector3(-17.42f, y + 2.45f, 62.87f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_AcousticSensorLed_E_2", parent, new Vector3(-17.51f, y + 2.48f, 62.87f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone04_Acoustics_AcousticSensor_W_3", parent, new Vector3(-46.58f, y + 2.45f, 70.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_AcousticSensorLed_W_3", parent, new Vector3(-46.49f, y + 2.48f, 70.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone04_Acoustics_AcousticSensor_E_3", parent, new Vector3(-17.42f, y + 2.45f, 70.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_AcousticSensorLed_E_3", parent, new Vector3(-17.51f, y + 2.48f, 70.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone04_Acoustics_DrainStrip_L_0", parent, new Vector3(-34.6f, y + 0.012f, 47.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_DrainStrip_R_0", parent, new Vector3(-29.4f, y + 0.012f, 47.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallCableClamp_W_0", parent, new Vector3(-46.64f, y + 1.15f, 47.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_WallCableClamp_E_0", parent, new Vector3(-17.36f, y + 1.15f, 47.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_DrainStrip_L_1", parent, new Vector3(-34.6f, y + 0.012f, 55.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_DrainStrip_R_1", parent, new Vector3(-29.4f, y + 0.012f, 55.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallCableClamp_W_1", parent, new Vector3(-46.64f, y + 1.15f, 55.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_WallCableClamp_E_1", parent, new Vector3(-17.36f, y + 1.15f, 55.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_DrainStrip_L_2", parent, new Vector3(-34.6f, y + 0.012f, 63.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_DrainStrip_R_2", parent, new Vector3(-29.4f, y + 0.012f, 63.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallCableClamp_W_2", parent, new Vector3(-46.64f, y + 1.15f, 63.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_WallCableClamp_E_2", parent, new Vector3(-17.36f, y + 1.15f, 63.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_DrainStrip_L_3", parent, new Vector3(-34.6f, y + 0.012f, 71.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_DrainStrip_R_3", parent, new Vector3(-29.4f, y + 0.012f, 71.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallCableClamp_W_3", parent, new Vector3(-46.64f, y + 1.15f, 71.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_WallCableClamp_E_3", parent, new Vector3(-17.36f, y + 1.15f, 71.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_Baseboard_S_0", parent, new Vector3(-44.8f, y + 0.12f, 45.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_Baseboard_N_0", parent, new Vector3(-44.8f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingPipe_S_0", parent, new Vector3(-44.8f, y + 3.55f, 46.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_CeilingPipe_N_0", parent, new Vector3(-44.8f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_Baseboard_S_1", parent, new Vector3(-36.27f, y + 0.12f, 45.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_Baseboard_N_1", parent, new Vector3(-36.27f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingPipe_S_1", parent, new Vector3(-36.27f, y + 3.55f, 46.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_CeilingPipe_N_1", parent, new Vector3(-36.27f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_Baseboard_S_2", parent, new Vector3(-27.73f, y + 0.12f, 45.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_Baseboard_N_2", parent, new Vector3(-27.73f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingPipe_S_2", parent, new Vector3(-27.73f, y + 3.55f, 46.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_CeilingPipe_N_2", parent, new Vector3(-27.73f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_Baseboard_S_3", parent, new Vector3(-19.2f, y + 0.12f, 45.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_Baseboard_N_3", parent, new Vector3(-19.2f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingPipe_S_3", parent, new Vector3(-19.2f, y + 3.55f, 46.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_CeilingPipe_N_3", parent, new Vector3(-19.2f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone04_Acoustics_AcousticPanel_W_0", parent, new Vector3(-46.64f, y + 1.85f, 47.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Zone04_Acoustics_AcousticPanel_E_0", parent, new Vector3(-17.36f, y + 1.85f, 47.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.AcousticFoam);
        CreateDecorBox("Zone04_Acoustics_StrobeMount_W_0", parent, new Vector3(-46.6f, y + 3.05f, 48.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone04_Acoustics_StrobeMount_E_0", parent, new Vector3(-17.4f, y + 3.05f, 46.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone04_Acoustics_AcousticPanel_W_1", parent, new Vector3(-46.64f, y + 1.85f, 55.2f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Zone04_Acoustics_AcousticPanel_E_1", parent, new Vector3(-17.36f, y + 1.85f, 55.2f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.AcousticFoam);
        CreateDecorBox("Zone04_Acoustics_StrobeMount_W_1", parent, new Vector3(-46.6f, y + 3.05f, 55.85f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone04_Acoustics_StrobeMount_E_1", parent, new Vector3(-17.4f, y + 3.05f, 54.55f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone04_Acoustics_AcousticPanel_W_2", parent, new Vector3(-46.64f, y + 1.85f, 62.8f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Zone04_Acoustics_AcousticPanel_E_2", parent, new Vector3(-17.36f, y + 1.85f, 62.8f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.AcousticFoam);
        CreateDecorBox("Zone04_Acoustics_StrobeMount_W_2", parent, new Vector3(-46.6f, y + 3.05f, 63.45f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone04_Acoustics_StrobeMount_E_2", parent, new Vector3(-17.4f, y + 3.05f, 62.15f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone04_Acoustics_AcousticPanel_W_3", parent, new Vector3(-46.64f, y + 1.85f, 70.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.AcousticFoam);
        CreateAcousticBox("Zone04_Acoustics_AcousticPanel_E_3", parent, new Vector3(-17.36f, y + 1.85f, 70.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.AcousticFoam);
        CreateDecorBox("Zone04_Acoustics_StrobeMount_W_3", parent, new Vector3(-46.6f, y + 3.05f, 71.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone04_Acoustics_StrobeMount_E_3", parent, new Vector3(-17.4f, y + 3.05f, 69.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone04_Acoustics_CeilingVentBox_S_0", parent, new Vector3(-44.4f, y + 3.86f, 55.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CeilingVentBox_N_0", parent, new Vector3(-44.4f, y + 3.86f, 62.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_VerticalConduit_S_0", parent, new Vector3(-44.4f, y + 1.95f, 45.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_VerticalConduit_N_0", parent, new Vector3(-44.4f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_CeilingVentBox_S_1", parent, new Vector3(-36.13f, y + 3.86f, 55.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CeilingVentBox_N_1", parent, new Vector3(-36.13f, y + 3.86f, 62.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_VerticalConduit_S_1", parent, new Vector3(-36.13f, y + 1.95f, 45.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_VerticalConduit_N_1", parent, new Vector3(-36.13f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_CeilingVentBox_S_2", parent, new Vector3(-27.87f, y + 3.86f, 55.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CeilingVentBox_N_2", parent, new Vector3(-27.87f, y + 3.86f, 62.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_VerticalConduit_S_2", parent, new Vector3(-27.87f, y + 1.95f, 45.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_VerticalConduit_N_2", parent, new Vector3(-27.87f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_CeilingVentBox_S_3", parent, new Vector3(-19.6f, y + 3.86f, 55.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_CeilingVentBox_N_3", parent, new Vector3(-19.6f, y + 3.86f, 62.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_VerticalConduit_S_3", parent, new Vector3(-19.6f, y + 1.95f, 45.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_VerticalConduit_N_3", parent, new Vector3(-19.6f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_W_0", parent, new Vector3(-46.66f, y + 3.65f, 46.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_E_0", parent, new Vector3(-17.34f, y + 3.65f, 46.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_MidStripe_W_0", parent, new Vector3(-46.67f, y + 1.32f, 46.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_MidStripe_E_0", parent, new Vector3(-17.33f, y + 1.32f, 46.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_W_1", parent, new Vector3(-46.66f, y + 3.65f, 52.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_E_1", parent, new Vector3(-17.34f, y + 3.65f, 52.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_MidStripe_W_1", parent, new Vector3(-46.67f, y + 1.32f, 52.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_MidStripe_E_1", parent, new Vector3(-17.33f, y + 1.32f, 52.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_W_2", parent, new Vector3(-46.66f, y + 3.65f, 59.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_E_2", parent, new Vector3(-17.34f, y + 3.65f, 59.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_MidStripe_W_2", parent, new Vector3(-46.67f, y + 1.32f, 59.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_MidStripe_E_2", parent, new Vector3(-17.33f, y + 1.32f, 59.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_W_3", parent, new Vector3(-46.66f, y + 3.65f, 65.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_E_3", parent, new Vector3(-17.34f, y + 3.65f, 65.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_MidStripe_W_3", parent, new Vector3(-46.67f, y + 1.32f, 65.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_MidStripe_E_3", parent, new Vector3(-17.33f, y + 1.32f, 65.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_W_4", parent, new Vector3(-46.66f, y + 3.65f, 71.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_UpperTrim_E_4", parent, new Vector3(-17.34f, y + 3.65f, 71.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone04_Acoustics_MidStripe_W_4", parent, new Vector3(-46.67f, y + 1.32f, 71.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone04_Acoustics_MidStripe_E_4", parent, new Vector3(-17.33f, y + 1.32f, 71.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_L_0", parent, new Vector3(-35.2f, y + 3.84f, 46.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_R_0", parent, new Vector3(-28.8f, y + 3.84f, 46.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_WallJunction_W_0", parent, new Vector3(-46.62f, y + 0.78f, 46.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallJunction_E_0", parent, new Vector3(-17.38f, y + 0.78f, 46.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_L_1", parent, new Vector3(-35.2f, y + 3.84f, 52.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_R_1", parent, new Vector3(-28.8f, y + 3.84f, 52.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_WallJunction_W_1", parent, new Vector3(-46.62f, y + 0.78f, 52.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallJunction_E_1", parent, new Vector3(-17.38f, y + 0.78f, 52.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_L_2", parent, new Vector3(-35.2f, y + 3.84f, 59.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_R_2", parent, new Vector3(-28.8f, y + 3.84f, 59.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_WallJunction_W_2", parent, new Vector3(-46.62f, y + 0.78f, 59.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallJunction_E_2", parent, new Vector3(-17.38f, y + 0.78f, 59.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_L_3", parent, new Vector3(-35.2f, y + 3.84f, 65.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_R_3", parent, new Vector3(-28.8f, y + 3.84f, 65.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_WallJunction_W_3", parent, new Vector3(-46.62f, y + 0.78f, 65.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallJunction_E_3", parent, new Vector3(-17.38f, y + 0.78f, 65.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_L_4", parent, new Vector3(-35.2f, y + 3.84f, 71.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone04_Acoustics_Sprinkler_R_4", parent, new Vector3(-28.8f, y + 3.84f, 71.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_WallJunction_W_4", parent, new Vector3(-46.62f, y + 0.78f, 71.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallJunction_E_4", parent, new Vector3(-17.38f, y + 0.78f, 71.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_FloorJoint_X_0", parent, new Vector3(-45.0f, y + 0.008f, 59.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingStrut_S_0", parent, new Vector3(-45.0f, y + 3.74f, 47.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingStrut_N_0", parent, new Vector3(-45.0f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallBracket_S_0", parent, new Vector3(-45.0f, y + 2.85f, 45.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_WallBracket_N_0", parent, new Vector3(-45.0f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_FloorJoint_X_1", parent, new Vector3(-36.33f, y + 0.008f, 59.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingStrut_S_1", parent, new Vector3(-36.33f, y + 3.74f, 47.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingStrut_N_1", parent, new Vector3(-36.33f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallBracket_S_1", parent, new Vector3(-36.33f, y + 2.85f, 45.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_WallBracket_N_1", parent, new Vector3(-36.33f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_FloorJoint_X_2", parent, new Vector3(-27.67f, y + 0.008f, 59.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingStrut_S_2", parent, new Vector3(-27.67f, y + 3.74f, 47.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingStrut_N_2", parent, new Vector3(-27.67f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallBracket_S_2", parent, new Vector3(-27.67f, y + 2.85f, 45.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_WallBracket_N_2", parent, new Vector3(-27.67f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_FloorJoint_X_3", parent, new Vector3(-19.0f, y + 0.008f, 59.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingStrut_S_3", parent, new Vector3(-19.0f, y + 3.74f, 47.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_CeilingStrut_N_3", parent, new Vector3(-19.0f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_WallBracket_S_3", parent, new Vector3(-19.0f, y + 2.85f, 45.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_WallBracket_N_3", parent, new Vector3(-19.0f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_W_0", parent, new Vector3(-46.65f, y + 0.55f, 46.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_E_0", parent, new Vector3(-17.35f, y + 0.55f, 46.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_HighPipe_W_0", parent, new Vector3(-46.65f, y + 3.38f, 46.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_HighPipe_E_0", parent, new Vector3(-17.35f, y + 3.38f, 46.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_W_1", parent, new Vector3(-46.65f, y + 0.55f, 52.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_E_1", parent, new Vector3(-17.35f, y + 0.55f, 52.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_HighPipe_W_1", parent, new Vector3(-46.65f, y + 3.38f, 52.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_HighPipe_E_1", parent, new Vector3(-17.35f, y + 3.38f, 52.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_W_2", parent, new Vector3(-46.65f, y + 0.55f, 59.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_E_2", parent, new Vector3(-17.35f, y + 0.55f, 59.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_HighPipe_W_2", parent, new Vector3(-46.65f, y + 3.38f, 59.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_HighPipe_E_2", parent, new Vector3(-17.35f, y + 3.38f, 59.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_W_3", parent, new Vector3(-46.65f, y + 0.55f, 65.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_E_3", parent, new Vector3(-17.35f, y + 0.55f, 65.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_HighPipe_W_3", parent, new Vector3(-46.65f, y + 3.38f, 65.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_HighPipe_E_3", parent, new Vector3(-17.35f, y + 3.38f, 65.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_W_4", parent, new Vector3(-46.65f, y + 0.55f, 71.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_LowerPipe_E_4", parent, new Vector3(-17.35f, y + 0.55f, 71.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone04_Acoustics_HighPipe_W_4", parent, new Vector3(-46.65f, y + 3.38f, 71.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_HighPipe_E_4", parent, new Vector3(-17.35f, y + 3.38f, 71.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone04_Acoustics_DuctCollar_C_0", parent, new Vector3(-32.0f, y + 3.76f, 46.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_W_0", parent, new Vector3(-46.62f, y + 1.58f, 46.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_E_0", parent, new Vector3(-17.38f, y + 1.58f, 46.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_W_0", parent, new Vector3(-46.35f, y + 0.01f, 46.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_E_0", parent, new Vector3(-17.65f, y + 0.01f, 46.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_DuctCollar_C_1", parent, new Vector3(-32.0f, y + 3.76f, 52.9f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_W_1", parent, new Vector3(-46.62f, y + 1.58f, 52.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_E_1", parent, new Vector3(-17.38f, y + 1.58f, 52.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_W_1", parent, new Vector3(-46.35f, y + 0.01f, 52.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_E_1", parent, new Vector3(-17.65f, y + 0.01f, 52.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_DuctCollar_C_2", parent, new Vector3(-32.0f, y + 3.76f, 59.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_W_2", parent, new Vector3(-46.62f, y + 1.58f, 59.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_E_2", parent, new Vector3(-17.38f, y + 1.58f, 59.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_W_2", parent, new Vector3(-46.35f, y + 0.01f, 59.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_E_2", parent, new Vector3(-17.65f, y + 0.01f, 59.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_DuctCollar_C_3", parent, new Vector3(-32.0f, y + 3.76f, 65.1f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_W_3", parent, new Vector3(-46.62f, y + 1.58f, 65.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_E_3", parent, new Vector3(-17.38f, y + 1.58f, 65.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_W_3", parent, new Vector3(-46.35f, y + 0.01f, 65.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_E_3", parent, new Vector3(-17.65f, y + 0.01f, 65.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_DuctCollar_C_4", parent, new Vector3(-32.0f, y + 3.76f, 71.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_W_4", parent, new Vector3(-46.62f, y + 1.58f, 71.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_TelemetryNode_E_4", parent, new Vector3(-17.38f, y + 1.58f, 71.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_W_4", parent, new Vector3(-46.35f, y + 0.01f, 71.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_FloorBoltPlate_E_4", parent, new Vector3(-17.65f, y + 0.01f, 71.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone04_Acoustics_InspHatch_L_0", parent, new Vector3(-37.2f, y + 3.88f, 46.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_InspHatch_R_0", parent, new Vector3(-26.8f, y + 3.88f, 46.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_RailClamp_W_0", parent, new Vector3(-46.64f, y + 2.15f, 46.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_RailClamp_E_0", parent, new Vector3(-17.36f, y + 2.15f, 46.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_InspHatch_L_1", parent, new Vector3(-37.2f, y + 3.88f, 52.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_InspHatch_R_1", parent, new Vector3(-26.8f, y + 3.88f, 52.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_RailClamp_W_1", parent, new Vector3(-46.64f, y + 2.15f, 52.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_RailClamp_E_1", parent, new Vector3(-17.36f, y + 2.15f, 52.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_InspHatch_L_2", parent, new Vector3(-37.2f, y + 3.88f, 59.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_InspHatch_R_2", parent, new Vector3(-26.8f, y + 3.88f, 59.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_RailClamp_W_2", parent, new Vector3(-46.64f, y + 2.15f, 59.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_RailClamp_E_2", parent, new Vector3(-17.36f, y + 2.15f, 59.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_InspHatch_L_3", parent, new Vector3(-37.2f, y + 3.88f, 65.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_InspHatch_R_3", parent, new Vector3(-26.8f, y + 3.88f, 65.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_RailClamp_W_3", parent, new Vector3(-46.64f, y + 2.15f, 65.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_RailClamp_E_3", parent, new Vector3(-17.36f, y + 2.15f, 65.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_InspHatch_L_4", parent, new Vector3(-37.2f, y + 3.88f, 71.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_InspHatch_R_4", parent, new Vector3(-26.8f, y + 3.88f, 71.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone04_Acoustics_RailClamp_W_4", parent, new Vector3(-46.64f, y + 2.15f, 71.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone04_Acoustics_RailClamp_E_4", parent, new Vector3(-17.36f, y + 2.15f, 71.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone05_Administration(Transform parent)
    {
        float y = MainY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone05_Administration
        BuildFireExtinguisherCabinet("Zone05_Administration_FireCab_W_0", parent, new Vector3(17.55f, y + 1.45f, 19.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone05_Administration_FireCab_E_0", parent, new Vector3(46.45f, y + 1.45f, 19.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone05_Administration_FireCab_W_1", parent, new Vector3(17.55f, y + 1.45f, 30.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone05_Administration_FireCab_E_1", parent, new Vector3(46.45f, y + 1.45f, 30.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone05_Administration_FireCab_W_2", parent, new Vector3(17.55f, y + 1.45f, 40.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone05_Administration_FireCab_E_2", parent, new Vector3(46.45f, y + 1.45f, 40.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone05_Administration_CCTV_0", parent, new Vector3(17.45f, y + 3.35f, 17.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone05_Administration_CCTV_1", parent, new Vector3(46.55f, y + 3.35f, 17.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone05_Administration_CCTV_2", parent, new Vector3(17.45f, y + 3.35f, 42.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone05_Administration_CCTV_3", parent, new Vector3(46.55f, y + 3.35f, 42.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone05_Administration_CCTV_4", parent, new Vector3(28.5f, y + 3.35f, 17.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone05_Administration_CCTV_5", parent, new Vector3(35.5f, y + 3.35f, 42.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone05_Administration_Intercom_W_0", parent, new Vector3(17.42f, y + 1.52f, 20.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone05_Administration_Intercom_E_0", parent, new Vector3(46.58f, y + 1.52f, 20.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone05_Administration_Clock_W_0", parent, new Vector3(17.4f, y + 2.85f, 20.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone05_Administration_Clock_E_0", parent, new Vector3(46.6f, y + 2.85f, 20.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone05_Administration_Intercom_W_1", parent, new Vector3(17.42f, y + 1.52f, 26.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone05_Administration_Intercom_E_1", parent, new Vector3(46.58f, y + 1.52f, 26.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone05_Administration_Intercom_W_2", parent, new Vector3(17.42f, y + 1.52f, 33.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone05_Administration_Intercom_E_2", parent, new Vector3(46.58f, y + 1.52f, 33.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone05_Administration_Clock_W_2", parent, new Vector3(17.4f, y + 2.85f, 33.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone05_Administration_Clock_E_2", parent, new Vector3(46.6f, y + 2.85f, 33.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone05_Administration_Intercom_W_3", parent, new Vector3(17.42f, y + 1.52f, 40.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone05_Administration_Intercom_E_3", parent, new Vector3(46.58f, y + 1.52f, 40.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_W_0", parent, new Vector3(17.45f, y, 19.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_E_0", parent, new Vector3(46.55f, y, 19.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_W_1", parent, new Vector3(17.45f, y, 26.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_E_1", parent, new Vector3(46.55f, y, 26.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_W_2", parent, new Vector3(17.45f, y, 33.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_E_2", parent, new Vector3(46.55f, y, 33.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_W_3", parent, new Vector3(17.45f, y, 41.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_E_3", parent, new Vector3(46.55f, y, 41.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_W_4", parent, new Vector3(17.45f, y, 48.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_E_4", parent, new Vector3(46.55f, y, 48.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_W_5", parent, new Vector3(17.45f, y, 55.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone05_Administration_Radiator_E_5", parent, new Vector3(46.55f, y, 55.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_W_0", parent, new Vector3(17.42f, y + 3.15f, 19.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_E_0", parent, new Vector3(46.58f, y + 3.15f, 19.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_W_0", parent, new Vector3(18.25f, y + 3.45f, 19.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_E_0", parent, new Vector3(45.75f, y + 3.45f, 19.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_W_1", parent, new Vector3(17.42f, y + 3.15f, 26.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_E_1", parent, new Vector3(46.58f, y + 3.15f, 26.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_W_1", parent, new Vector3(18.25f, y + 3.45f, 26.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_E_1", parent, new Vector3(45.75f, y + 3.45f, 26.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_W_2", parent, new Vector3(17.42f, y + 3.15f, 33.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_E_2", parent, new Vector3(46.58f, y + 3.15f, 33.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_W_2", parent, new Vector3(18.25f, y + 3.45f, 33.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_E_2", parent, new Vector3(45.75f, y + 3.45f, 33.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_W_3", parent, new Vector3(17.42f, y + 3.15f, 40.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_E_3", parent, new Vector3(46.58f, y + 3.15f, 40.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_W_3", parent, new Vector3(18.25f, y + 3.45f, 40.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_E_3", parent, new Vector3(45.75f, y + 3.45f, 40.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_W_4", parent, new Vector3(17.42f, y + 3.15f, 48.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_E_4", parent, new Vector3(46.58f, y + 3.15f, 48.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_W_4", parent, new Vector3(18.25f, y + 3.45f, 48.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_E_4", parent, new Vector3(45.75f, y + 3.45f, 48.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_W_5", parent, new Vector3(17.42f, y + 3.15f, 55.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone05_Administration_Vent_E_5", parent, new Vector3(46.58f, y + 3.15f, 55.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_W_5", parent, new Vector3(18.25f, y + 3.45f, 55.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone05_Administration_PA_E_5", parent, new Vector3(45.75f, y + 3.45f, 55.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone05_Administration_ValveManifold_S_0", parent, new Vector3(20.0f, y, 17.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone05_Administration_ValveManifold_N_0", parent, new Vector3(20.0f, y, 42.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone05_Administration_FuseBox_S_0", parent, new Vector3(21.2f, y + 1.65f, 17.45f), Quaternion.identity);
        BuildWallFuseBox("Zone05_Administration_FuseBox_N_0", parent, new Vector3(18.8f, y + 1.65f, 42.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone05_Administration_ValveManifold_S_1", parent, new Vector3(28.0f, y, 17.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone05_Administration_ValveManifold_N_1", parent, new Vector3(28.0f, y, 42.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone05_Administration_FuseBox_S_1", parent, new Vector3(29.2f, y + 1.65f, 17.45f), Quaternion.identity);
        BuildWallFuseBox("Zone05_Administration_FuseBox_N_1", parent, new Vector3(26.8f, y + 1.65f, 42.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone05_Administration_ValveManifold_S_2", parent, new Vector3(36.0f, y, 17.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone05_Administration_ValveManifold_N_2", parent, new Vector3(36.0f, y, 42.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone05_Administration_FuseBox_S_2", parent, new Vector3(37.2f, y + 1.65f, 17.45f), Quaternion.identity);
        BuildWallFuseBox("Zone05_Administration_FuseBox_N_2", parent, new Vector3(34.8f, y + 1.65f, 42.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone05_Administration_ValveManifold_S_3", parent, new Vector3(44.0f, y, 17.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone05_Administration_ValveManifold_N_3", parent, new Vector3(44.0f, y, 42.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone05_Administration_FuseBox_S_3", parent, new Vector3(45.2f, y + 1.65f, 17.45f), Quaternion.identity);
        BuildWallFuseBox("Zone05_Administration_FuseBox_N_3", parent, new Vector3(42.8f, y + 1.65f, 42.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_L_0", parent, new Vector3(27.8f, y + 3.48f, 20.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_R_0", parent, new Vector3(36.2f, y + 3.48f, 20.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_L_1", parent, new Vector3(27.8f, y + 3.48f, 25.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_R_1", parent, new Vector3(36.2f, y + 3.48f, 25.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_L_2", parent, new Vector3(27.8f, y + 3.48f, 30.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_R_2", parent, new Vector3(36.2f, y + 3.48f, 30.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_L_3", parent, new Vector3(27.8f, y + 3.48f, 35.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_R_3", parent, new Vector3(36.2f, y + 3.48f, 35.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_L_4", parent, new Vector3(27.8f, y + 3.48f, 40.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone05_Administration_Baffle_R_4", parent, new Vector3(36.2f, y + 3.48f, 40.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone05_Administration_Crates_SW", parent, new Vector3(18.6f, y, 18.6f), 14f);
        BuildCableSpoolAndMat("Zone05_Administration_Spool_SE", parent, new Vector3(45.4f, y, 18.6f), -28f);
        BuildHazmatDrumCluster("Zone05_Administration_Drums_NW", parent, new Vector3(18.6f, y, 41.4f));
        BuildPatchPanelRack("Zone05_Administration_PatchRack_NE", parent, new Vector3(45.4f, y, 41.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone05_Administration_CheckStation_S_0", parent, new Vector3(19.4f, y, 18.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone05_Administration_Bollard_S_0", parent, new Vector3(18.55f, y, 17.95f));
        BuildWallWarningBollard("Zone05_Administration_Bollard_N_0", parent, new Vector3(20.25f, y, 42.05f));
        BuildClipboardCheckStation("Zone05_Administration_CheckStation_S_1", parent, new Vector3(27.8f, y, 18.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone05_Administration_Bollard_S_1", parent, new Vector3(26.95f, y, 17.95f));
        BuildWallWarningBollard("Zone05_Administration_Bollard_N_1", parent, new Vector3(28.65f, y, 42.05f));
        BuildClipboardCheckStation("Zone05_Administration_CheckStation_S_2", parent, new Vector3(36.2f, y, 18.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone05_Administration_Bollard_S_2", parent, new Vector3(35.35f, y, 17.95f));
        BuildWallWarningBollard("Zone05_Administration_Bollard_N_2", parent, new Vector3(37.05f, y, 42.05f));
        BuildClipboardCheckStation("Zone05_Administration_CheckStation_S_3", parent, new Vector3(44.6f, y, 18.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone05_Administration_Bollard_S_3", parent, new Vector3(43.75f, y, 17.95f));
        BuildWallWarningBollard("Zone05_Administration_Bollard_N_3", parent, new Vector3(45.45f, y, 42.05f));
        CreateAcousticBox("Zone05_Administration_Pilaster_W_0", parent, new Vector3(17.25f, y + 2.0f, 18.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateAcousticBox("Zone05_Administration_Pilaster_E_0", parent, new Vector3(46.75f, y + 2.0f, 18.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateDecorBox("Zone05_Administration_ConduitTrim_W_0", parent, new Vector3(17.38f, y + 0.32f, 18.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_ConduitTrim_E_0", parent, new Vector3(46.62f, y + 0.32f, 18.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone05_Administration_Pilaster_W_1", parent, new Vector3(17.25f, y + 2.0f, 24.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateAcousticBox("Zone05_Administration_Pilaster_E_1", parent, new Vector3(46.75f, y + 2.0f, 24.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateDecorBox("Zone05_Administration_ConduitTrim_W_1", parent, new Vector3(17.38f, y + 0.32f, 24.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_ConduitTrim_E_1", parent, new Vector3(46.62f, y + 0.32f, 24.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone05_Administration_Pilaster_W_2", parent, new Vector3(17.25f, y + 2.0f, 30.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateAcousticBox("Zone05_Administration_Pilaster_E_2", parent, new Vector3(46.75f, y + 2.0f, 30.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateDecorBox("Zone05_Administration_ConduitTrim_W_2", parent, new Vector3(17.38f, y + 0.32f, 30.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_ConduitTrim_E_2", parent, new Vector3(46.62f, y + 0.32f, 30.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone05_Administration_Pilaster_W_3", parent, new Vector3(17.25f, y + 2.0f, 35.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateAcousticBox("Zone05_Administration_Pilaster_E_3", parent, new Vector3(46.75f, y + 2.0f, 35.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateDecorBox("Zone05_Administration_ConduitTrim_W_3", parent, new Vector3(17.38f, y + 0.32f, 35.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_ConduitTrim_E_3", parent, new Vector3(46.62f, y + 0.32f, 35.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone05_Administration_Pilaster_W_4", parent, new Vector3(17.25f, y + 2.0f, 41.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateAcousticBox("Zone05_Administration_Pilaster_E_4", parent, new Vector3(46.75f, y + 2.0f, 41.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Wood);
        CreateDecorBox("Zone05_Administration_ConduitTrim_W_4", parent, new Vector3(17.38f, y + 0.32f, 41.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_ConduitTrim_E_4", parent, new Vector3(46.62f, y + 0.32f, 41.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingRib_0", parent, new Vector3(32.0f, y + 3.82f, 18.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_L_0", parent, new Vector3(23.94f, y + 3.68f, 18.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_R_0", parent, new Vector3(40.06f, y + 3.68f, 18.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CeilingRib_1", parent, new Vector3(32.0f, y + 3.82f, 24.25f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_L_1", parent, new Vector3(23.94f, y + 3.68f, 24.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_R_1", parent, new Vector3(40.06f, y + 3.68f, 24.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CeilingRib_2", parent, new Vector3(32.0f, y + 3.82f, 30.0f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_L_2", parent, new Vector3(23.94f, y + 3.68f, 30.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_R_2", parent, new Vector3(40.06f, y + 3.68f, 30.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CeilingRib_3", parent, new Vector3(32.0f, y + 3.82f, 35.75f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_L_3", parent, new Vector3(23.94f, y + 3.68f, 35.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_R_3", parent, new Vector3(40.06f, y + 3.68f, 35.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CeilingRib_4", parent, new Vector3(32.0f, y + 3.82f, 41.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_L_4", parent, new Vector3(23.94f, y + 3.68f, 41.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CableTrayHanger_R_4", parent, new Vector3(40.06f, y + 3.68f, 41.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_FloorSafetyLine_S_0", parent, new Vector3(19.8f, y + 0.014f, 18.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_FloorSafetyLine_N_0", parent, new Vector3(19.8f, y + 0.014f, 41.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_WallDataPlaque_S_0", parent, new Vector3(19.8f, y + 2.25f, 17.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_WallDataPlaque_N_0", parent, new Vector3(19.8f, y + 2.25f, 42.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_FloorSafetyLine_S_1", parent, new Vector3(27.93f, y + 0.014f, 18.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_FloorSafetyLine_N_1", parent, new Vector3(27.93f, y + 0.014f, 41.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_WallDataPlaque_S_1", parent, new Vector3(27.93f, y + 2.25f, 17.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_WallDataPlaque_N_1", parent, new Vector3(27.93f, y + 2.25f, 42.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_FloorSafetyLine_S_2", parent, new Vector3(36.07f, y + 0.014f, 18.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_FloorSafetyLine_N_2", parent, new Vector3(36.07f, y + 0.014f, 41.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_WallDataPlaque_S_2", parent, new Vector3(36.07f, y + 2.25f, 17.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_WallDataPlaque_N_2", parent, new Vector3(36.07f, y + 2.25f, 42.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_FloorSafetyLine_S_3", parent, new Vector3(44.2f, y + 0.014f, 18.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_FloorSafetyLine_N_3", parent, new Vector3(44.2f, y + 0.014f, 41.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_WallDataPlaque_S_3", parent, new Vector3(44.2f, y + 2.25f, 17.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_WallDataPlaque_N_3", parent, new Vector3(44.2f, y + 2.25f, 42.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_AcousticSensor_W_0", parent, new Vector3(17.42f, y + 2.45f, 19.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_AcousticSensorLed_W_0", parent, new Vector3(17.51f, y + 2.48f, 19.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone05_Administration_AcousticSensor_E_0", parent, new Vector3(46.58f, y + 2.45f, 19.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_AcousticSensorLed_E_0", parent, new Vector3(46.49f, y + 2.48f, 19.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone05_Administration_AcousticSensor_W_1", parent, new Vector3(17.42f, y + 2.45f, 26.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_AcousticSensorLed_W_1", parent, new Vector3(17.51f, y + 2.48f, 26.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone05_Administration_AcousticSensor_E_1", parent, new Vector3(46.58f, y + 2.45f, 26.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_AcousticSensorLed_E_1", parent, new Vector3(46.49f, y + 2.48f, 26.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone05_Administration_AcousticSensor_W_2", parent, new Vector3(17.42f, y + 2.45f, 33.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_AcousticSensorLed_W_2", parent, new Vector3(17.51f, y + 2.48f, 33.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone05_Administration_AcousticSensor_E_2", parent, new Vector3(46.58f, y + 2.45f, 33.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_AcousticSensorLed_E_2", parent, new Vector3(46.49f, y + 2.48f, 33.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone05_Administration_AcousticSensor_W_3", parent, new Vector3(17.42f, y + 2.45f, 40.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_AcousticSensorLed_W_3", parent, new Vector3(17.51f, y + 2.48f, 40.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone05_Administration_AcousticSensor_E_3", parent, new Vector3(46.58f, y + 2.45f, 40.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_AcousticSensorLed_E_3", parent, new Vector3(46.49f, y + 2.48f, 40.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone05_Administration_DrainStrip_L_0", parent, new Vector3(29.4f, y + 0.012f, 19.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_DrainStrip_R_0", parent, new Vector3(34.6f, y + 0.012f, 19.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallCableClamp_W_0", parent, new Vector3(17.36f, y + 1.15f, 19.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_WallCableClamp_E_0", parent, new Vector3(46.64f, y + 1.15f, 19.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_DrainStrip_L_1", parent, new Vector3(29.4f, y + 0.012f, 26.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_DrainStrip_R_1", parent, new Vector3(34.6f, y + 0.012f, 26.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallCableClamp_W_1", parent, new Vector3(17.36f, y + 1.15f, 26.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_WallCableClamp_E_1", parent, new Vector3(46.64f, y + 1.15f, 26.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_DrainStrip_L_2", parent, new Vector3(29.4f, y + 0.012f, 33.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_DrainStrip_R_2", parent, new Vector3(34.6f, y + 0.012f, 33.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallCableClamp_W_2", parent, new Vector3(17.36f, y + 1.15f, 33.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_WallCableClamp_E_2", parent, new Vector3(46.64f, y + 1.15f, 33.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_DrainStrip_L_3", parent, new Vector3(29.4f, y + 0.012f, 41.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_DrainStrip_R_3", parent, new Vector3(34.6f, y + 0.012f, 41.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallCableClamp_W_3", parent, new Vector3(17.36f, y + 1.15f, 41.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_WallCableClamp_E_3", parent, new Vector3(46.64f, y + 1.15f, 41.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_Baseboard_S_0", parent, new Vector3(19.2f, y + 0.12f, 17.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_Baseboard_N_0", parent, new Vector3(19.2f, y + 0.12f, 42.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingPipe_S_0", parent, new Vector3(19.2f, y + 3.55f, 18.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_CeilingPipe_N_0", parent, new Vector3(19.2f, y + 3.55f, 41.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_Baseboard_S_1", parent, new Vector3(27.73f, y + 0.12f, 17.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_Baseboard_N_1", parent, new Vector3(27.73f, y + 0.12f, 42.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingPipe_S_1", parent, new Vector3(27.73f, y + 3.55f, 18.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_CeilingPipe_N_1", parent, new Vector3(27.73f, y + 3.55f, 41.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_Baseboard_S_2", parent, new Vector3(36.27f, y + 0.12f, 17.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_Baseboard_N_2", parent, new Vector3(36.27f, y + 0.12f, 42.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingPipe_S_2", parent, new Vector3(36.27f, y + 3.55f, 18.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_CeilingPipe_N_2", parent, new Vector3(36.27f, y + 3.55f, 41.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_Baseboard_S_3", parent, new Vector3(44.8f, y + 0.12f, 17.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_Baseboard_N_3", parent, new Vector3(44.8f, y + 0.12f, 42.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingPipe_S_3", parent, new Vector3(44.8f, y + 3.55f, 18.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_CeilingPipe_N_3", parent, new Vector3(44.8f, y + 3.55f, 41.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone05_Administration_AcousticPanel_W_0", parent, new Vector3(17.36f, y + 1.85f, 19.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Wood);
        CreateAcousticBox("Zone05_Administration_AcousticPanel_E_0", parent, new Vector3(46.64f, y + 1.85f, 19.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Wood);
        CreateDecorBox("Zone05_Administration_StrobeMount_W_0", parent, new Vector3(17.4f, y + 3.05f, 20.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone05_Administration_StrobeMount_E_0", parent, new Vector3(46.6f, y + 3.05f, 18.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone05_Administration_AcousticPanel_W_1", parent, new Vector3(17.36f, y + 1.85f, 26.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Wood);
        CreateAcousticBox("Zone05_Administration_AcousticPanel_E_1", parent, new Vector3(46.64f, y + 1.85f, 26.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Wood);
        CreateDecorBox("Zone05_Administration_StrobeMount_W_1", parent, new Vector3(17.4f, y + 3.05f, 27.18f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone05_Administration_StrobeMount_E_1", parent, new Vector3(46.6f, y + 3.05f, 25.88f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone05_Administration_AcousticPanel_W_2", parent, new Vector3(17.36f, y + 1.85f, 33.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Wood);
        CreateAcousticBox("Zone05_Administration_AcousticPanel_E_2", parent, new Vector3(46.64f, y + 1.85f, 33.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Wood);
        CreateDecorBox("Zone05_Administration_StrobeMount_W_2", parent, new Vector3(17.4f, y + 3.05f, 34.12f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone05_Administration_StrobeMount_E_2", parent, new Vector3(46.6f, y + 3.05f, 32.82f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone05_Administration_AcousticPanel_W_3", parent, new Vector3(17.36f, y + 1.85f, 40.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Wood);
        CreateAcousticBox("Zone05_Administration_AcousticPanel_E_3", parent, new Vector3(46.64f, y + 1.85f, 40.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Wood);
        CreateDecorBox("Zone05_Administration_StrobeMount_W_3", parent, new Vector3(17.4f, y + 3.05f, 41.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone05_Administration_StrobeMount_E_3", parent, new Vector3(46.6f, y + 3.05f, 39.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone05_Administration_CeilingVentBox_S_0", parent, new Vector3(19.6f, y + 3.86f, 26.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CeilingVentBox_N_0", parent, new Vector3(19.6f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_VerticalConduit_S_0", parent, new Vector3(19.6f, y + 1.95f, 17.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_VerticalConduit_N_0", parent, new Vector3(19.6f, y + 1.95f, 42.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_CeilingVentBox_S_1", parent, new Vector3(27.87f, y + 3.86f, 26.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CeilingVentBox_N_1", parent, new Vector3(27.87f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_VerticalConduit_S_1", parent, new Vector3(27.87f, y + 1.95f, 17.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_VerticalConduit_N_1", parent, new Vector3(27.87f, y + 1.95f, 42.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_CeilingVentBox_S_2", parent, new Vector3(36.13f, y + 3.86f, 26.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CeilingVentBox_N_2", parent, new Vector3(36.13f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_VerticalConduit_S_2", parent, new Vector3(36.13f, y + 1.95f, 17.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_VerticalConduit_N_2", parent, new Vector3(36.13f, y + 1.95f, 42.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_CeilingVentBox_S_3", parent, new Vector3(44.4f, y + 3.86f, 26.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_CeilingVentBox_N_3", parent, new Vector3(44.4f, y + 3.86f, 33.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_VerticalConduit_S_3", parent, new Vector3(44.4f, y + 1.95f, 17.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_VerticalConduit_N_3", parent, new Vector3(44.4f, y + 1.95f, 42.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_UpperTrim_W_0", parent, new Vector3(17.34f, y + 3.65f, 18.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_UpperTrim_E_0", parent, new Vector3(46.66f, y + 3.65f, 18.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_MidStripe_W_0", parent, new Vector3(17.33f, y + 1.32f, 18.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_MidStripe_E_0", parent, new Vector3(46.67f, y + 1.32f, 18.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_UpperTrim_W_1", parent, new Vector3(17.34f, y + 3.65f, 24.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_UpperTrim_E_1", parent, new Vector3(46.66f, y + 3.65f, 24.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_MidStripe_W_1", parent, new Vector3(17.33f, y + 1.32f, 24.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_MidStripe_E_1", parent, new Vector3(46.67f, y + 1.32f, 24.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_UpperTrim_W_2", parent, new Vector3(17.34f, y + 3.65f, 30.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_UpperTrim_E_2", parent, new Vector3(46.66f, y + 3.65f, 30.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_MidStripe_W_2", parent, new Vector3(17.33f, y + 1.32f, 30.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_MidStripe_E_2", parent, new Vector3(46.67f, y + 1.32f, 30.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_UpperTrim_W_3", parent, new Vector3(17.34f, y + 3.65f, 35.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_UpperTrim_E_3", parent, new Vector3(46.66f, y + 3.65f, 35.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_MidStripe_W_3", parent, new Vector3(17.33f, y + 1.32f, 35.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_MidStripe_E_3", parent, new Vector3(46.67f, y + 1.32f, 35.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_UpperTrim_W_4", parent, new Vector3(17.34f, y + 3.65f, 41.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_UpperTrim_E_4", parent, new Vector3(46.66f, y + 3.65f, 41.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone05_Administration_MidStripe_W_4", parent, new Vector3(17.33f, y + 1.32f, 41.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone05_Administration_MidStripe_E_4", parent, new Vector3(46.67f, y + 1.32f, 41.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_L_0", parent, new Vector3(28.8f, y + 3.84f, 18.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_R_0", parent, new Vector3(35.2f, y + 3.84f, 18.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_WallJunction_W_0", parent, new Vector3(17.38f, y + 0.78f, 18.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallJunction_E_0", parent, new Vector3(46.62f, y + 0.78f, 18.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_L_1", parent, new Vector3(28.8f, y + 3.84f, 24.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_R_1", parent, new Vector3(35.2f, y + 3.84f, 24.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_WallJunction_W_1", parent, new Vector3(17.38f, y + 0.78f, 24.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallJunction_E_1", parent, new Vector3(46.62f, y + 0.78f, 24.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_L_2", parent, new Vector3(28.8f, y + 3.84f, 30.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_R_2", parent, new Vector3(35.2f, y + 3.84f, 30.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_WallJunction_W_2", parent, new Vector3(17.38f, y + 0.78f, 30.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallJunction_E_2", parent, new Vector3(46.62f, y + 0.78f, 30.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_L_3", parent, new Vector3(28.8f, y + 3.84f, 35.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_R_3", parent, new Vector3(35.2f, y + 3.84f, 35.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_WallJunction_W_3", parent, new Vector3(17.38f, y + 0.78f, 35.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallJunction_E_3", parent, new Vector3(46.62f, y + 0.78f, 35.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_L_4", parent, new Vector3(28.8f, y + 3.84f, 41.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone05_Administration_Sprinkler_R_4", parent, new Vector3(35.2f, y + 3.84f, 41.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_WallJunction_W_4", parent, new Vector3(17.38f, y + 0.78f, 41.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallJunction_E_4", parent, new Vector3(46.62f, y + 0.78f, 41.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_FloorJoint_X_0", parent, new Vector3(19.0f, y + 0.008f, 30.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingStrut_S_0", parent, new Vector3(19.0f, y + 3.74f, 19.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingStrut_N_0", parent, new Vector3(19.0f, y + 3.74f, 40.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallBracket_S_0", parent, new Vector3(19.0f, y + 2.85f, 17.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_WallBracket_N_0", parent, new Vector3(19.0f, y + 2.85f, 42.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_FloorJoint_X_1", parent, new Vector3(27.67f, y + 0.008f, 30.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingStrut_S_1", parent, new Vector3(27.67f, y + 3.74f, 19.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingStrut_N_1", parent, new Vector3(27.67f, y + 3.74f, 40.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallBracket_S_1", parent, new Vector3(27.67f, y + 2.85f, 17.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_WallBracket_N_1", parent, new Vector3(27.67f, y + 2.85f, 42.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_FloorJoint_X_2", parent, new Vector3(36.33f, y + 0.008f, 30.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingStrut_S_2", parent, new Vector3(36.33f, y + 3.74f, 19.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingStrut_N_2", parent, new Vector3(36.33f, y + 3.74f, 40.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallBracket_S_2", parent, new Vector3(36.33f, y + 2.85f, 17.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_WallBracket_N_2", parent, new Vector3(36.33f, y + 2.85f, 42.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_FloorJoint_X_3", parent, new Vector3(45.0f, y + 0.008f, 30.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingStrut_S_3", parent, new Vector3(45.0f, y + 3.74f, 19.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_CeilingStrut_N_3", parent, new Vector3(45.0f, y + 3.74f, 40.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_WallBracket_S_3", parent, new Vector3(45.0f, y + 2.85f, 17.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_WallBracket_N_3", parent, new Vector3(45.0f, y + 2.85f, 42.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_LowerPipe_W_0", parent, new Vector3(17.35f, y + 0.55f, 18.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_LowerPipe_E_0", parent, new Vector3(46.65f, y + 0.55f, 18.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_HighPipe_W_0", parent, new Vector3(17.35f, y + 3.38f, 18.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_HighPipe_E_0", parent, new Vector3(46.65f, y + 3.38f, 18.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_LowerPipe_W_1", parent, new Vector3(17.35f, y + 0.55f, 24.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_LowerPipe_E_1", parent, new Vector3(46.65f, y + 0.55f, 24.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_HighPipe_W_1", parent, new Vector3(17.35f, y + 3.38f, 24.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_HighPipe_E_1", parent, new Vector3(46.65f, y + 3.38f, 24.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_LowerPipe_W_2", parent, new Vector3(17.35f, y + 0.55f, 30.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_LowerPipe_E_2", parent, new Vector3(46.65f, y + 0.55f, 30.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_HighPipe_W_2", parent, new Vector3(17.35f, y + 3.38f, 30.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_HighPipe_E_2", parent, new Vector3(46.65f, y + 3.38f, 30.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_LowerPipe_W_3", parent, new Vector3(17.35f, y + 0.55f, 35.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_LowerPipe_E_3", parent, new Vector3(46.65f, y + 0.55f, 35.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_HighPipe_W_3", parent, new Vector3(17.35f, y + 3.38f, 35.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_HighPipe_E_3", parent, new Vector3(46.65f, y + 3.38f, 35.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_LowerPipe_W_4", parent, new Vector3(17.35f, y + 0.55f, 41.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_LowerPipe_E_4", parent, new Vector3(46.65f, y + 0.55f, 41.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone05_Administration_HighPipe_W_4", parent, new Vector3(17.35f, y + 3.38f, 41.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_HighPipe_E_4", parent, new Vector3(46.65f, y + 3.38f, 41.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone05_Administration_DuctCollar_C_0", parent, new Vector3(32.0f, y + 3.76f, 18.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_TelemetryNode_W_0", parent, new Vector3(17.38f, y + 1.58f, 18.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_TelemetryNode_E_0", parent, new Vector3(46.62f, y + 1.58f, 18.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_W_0", parent, new Vector3(17.65f, y + 0.01f, 18.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_E_0", parent, new Vector3(46.35f, y + 0.01f, 18.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_DuctCollar_C_1", parent, new Vector3(32.0f, y + 3.76f, 24.4f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_TelemetryNode_W_1", parent, new Vector3(17.38f, y + 1.58f, 24.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_TelemetryNode_E_1", parent, new Vector3(46.62f, y + 1.58f, 24.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_W_1", parent, new Vector3(17.65f, y + 0.01f, 24.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_E_1", parent, new Vector3(46.35f, y + 0.01f, 24.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_DuctCollar_C_2", parent, new Vector3(32.0f, y + 3.76f, 30.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_TelemetryNode_W_2", parent, new Vector3(17.38f, y + 1.58f, 30.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_TelemetryNode_E_2", parent, new Vector3(46.62f, y + 1.58f, 30.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_W_2", parent, new Vector3(17.65f, y + 0.01f, 30.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_E_2", parent, new Vector3(46.35f, y + 0.01f, 30.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_DuctCollar_C_3", parent, new Vector3(32.0f, y + 3.76f, 35.6f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_TelemetryNode_W_3", parent, new Vector3(17.38f, y + 1.58f, 35.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_TelemetryNode_E_3", parent, new Vector3(46.62f, y + 1.58f, 35.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_W_3", parent, new Vector3(17.65f, y + 0.01f, 35.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_E_3", parent, new Vector3(46.35f, y + 0.01f, 35.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_DuctCollar_C_4", parent, new Vector3(32.0f, y + 3.76f, 41.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_TelemetryNode_W_4", parent, new Vector3(17.38f, y + 1.58f, 41.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_TelemetryNode_E_4", parent, new Vector3(46.62f, y + 1.58f, 41.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_W_4", parent, new Vector3(17.65f, y + 0.01f, 41.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_FloorBoltPlate_E_4", parent, new Vector3(46.35f, y + 0.01f, 41.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone05_Administration_InspHatch_L_0", parent, new Vector3(26.8f, y + 3.88f, 18.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_InspHatch_R_0", parent, new Vector3(37.2f, y + 3.88f, 18.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_RailClamp_W_0", parent, new Vector3(17.36f, y + 2.15f, 18.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_RailClamp_E_0", parent, new Vector3(46.64f, y + 2.15f, 18.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_InspHatch_L_1", parent, new Vector3(26.8f, y + 3.88f, 24.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_InspHatch_R_1", parent, new Vector3(37.2f, y + 3.88f, 24.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_RailClamp_W_1", parent, new Vector3(17.36f, y + 2.15f, 24.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_RailClamp_E_1", parent, new Vector3(46.64f, y + 2.15f, 24.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_InspHatch_L_2", parent, new Vector3(26.8f, y + 3.88f, 30.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_InspHatch_R_2", parent, new Vector3(37.2f, y + 3.88f, 30.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_RailClamp_W_2", parent, new Vector3(17.36f, y + 2.15f, 30.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_RailClamp_E_2", parent, new Vector3(46.64f, y + 2.15f, 30.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_InspHatch_L_3", parent, new Vector3(26.8f, y + 3.88f, 35.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_InspHatch_R_3", parent, new Vector3(37.2f, y + 3.88f, 35.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_RailClamp_W_3", parent, new Vector3(17.36f, y + 2.15f, 35.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_RailClamp_E_3", parent, new Vector3(46.64f, y + 2.15f, 35.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_InspHatch_L_4", parent, new Vector3(26.8f, y + 3.88f, 41.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_InspHatch_R_4", parent, new Vector3(37.2f, y + 3.88f, 41.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone05_Administration_RailClamp_W_4", parent, new Vector3(17.36f, y + 2.15f, 41.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone05_Administration_RailClamp_E_4", parent, new Vector3(46.64f, y + 2.15f, 41.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone06_MedicalBay(Transform parent)
    {
        float y = MainY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone06_MedicalBay
        BuildFireExtinguisherCabinet("Zone06_MedicalBay_FireCab_W_0", parent, new Vector3(17.55f, y + 1.45f, 47.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone06_MedicalBay_FireCab_E_0", parent, new Vector3(46.45f, y + 1.45f, 47.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone06_MedicalBay_FireCab_W_1", parent, new Vector3(17.55f, y + 1.45f, 59.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone06_MedicalBay_FireCab_E_1", parent, new Vector3(46.45f, y + 1.45f, 59.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone06_MedicalBay_FireCab_W_2", parent, new Vector3(17.55f, y + 1.45f, 70.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone06_MedicalBay_FireCab_E_2", parent, new Vector3(46.45f, y + 1.45f, 70.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone06_MedicalBay_CCTV_0", parent, new Vector3(17.45f, y + 3.35f, 45.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone06_MedicalBay_CCTV_1", parent, new Vector3(46.55f, y + 3.35f, 45.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone06_MedicalBay_CCTV_2", parent, new Vector3(17.45f, y + 3.35f, 72.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone06_MedicalBay_CCTV_3", parent, new Vector3(46.55f, y + 3.35f, 72.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone06_MedicalBay_CCTV_4", parent, new Vector3(28.5f, y + 3.35f, 45.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone06_MedicalBay_CCTV_5", parent, new Vector3(35.5f, y + 3.35f, 72.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone06_MedicalBay_Intercom_W_0", parent, new Vector3(17.42f, y + 1.52f, 48.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone06_MedicalBay_Intercom_E_0", parent, new Vector3(46.58f, y + 1.52f, 48.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone06_MedicalBay_Clock_W_0", parent, new Vector3(17.4f, y + 2.85f, 48.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone06_MedicalBay_Clock_E_0", parent, new Vector3(46.6f, y + 2.85f, 48.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone06_MedicalBay_Intercom_W_1", parent, new Vector3(17.42f, y + 1.52f, 55.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone06_MedicalBay_Intercom_E_1", parent, new Vector3(46.58f, y + 1.52f, 55.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone06_MedicalBay_Intercom_W_2", parent, new Vector3(17.42f, y + 1.52f, 62.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone06_MedicalBay_Intercom_E_2", parent, new Vector3(46.58f, y + 1.52f, 62.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone06_MedicalBay_Clock_W_2", parent, new Vector3(17.4f, y + 2.85f, 62.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone06_MedicalBay_Clock_E_2", parent, new Vector3(46.6f, y + 2.85f, 62.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone06_MedicalBay_Intercom_W_3", parent, new Vector3(17.42f, y + 1.52f, 70.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone06_MedicalBay_Intercom_E_3", parent, new Vector3(46.58f, y + 1.52f, 70.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_W_0", parent, new Vector3(17.45f, y, 47.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_E_0", parent, new Vector3(46.55f, y, 47.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_W_1", parent, new Vector3(17.45f, y, 55.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_E_1", parent, new Vector3(46.55f, y, 55.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_W_2", parent, new Vector3(17.45f, y, 63.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_E_2", parent, new Vector3(46.55f, y, 63.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_W_3", parent, new Vector3(17.45f, y, 71.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_E_3", parent, new Vector3(46.55f, y, 71.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_W_4", parent, new Vector3(17.45f, y, 79.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_E_4", parent, new Vector3(46.55f, y, 79.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_W_5", parent, new Vector3(17.45f, y, 87.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone06_MedicalBay_Radiator_E_5", parent, new Vector3(46.55f, y, 87.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_W_0", parent, new Vector3(17.42f, y + 3.15f, 47.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_E_0", parent, new Vector3(46.58f, y + 3.15f, 47.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_W_0", parent, new Vector3(18.25f, y + 3.45f, 47.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_E_0", parent, new Vector3(45.75f, y + 3.45f, 47.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_W_1", parent, new Vector3(17.42f, y + 3.15f, 55.07f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_E_1", parent, new Vector3(46.58f, y + 3.15f, 55.07f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_W_1", parent, new Vector3(18.25f, y + 3.45f, 55.07f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_E_1", parent, new Vector3(45.75f, y + 3.45f, 55.07f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_W_2", parent, new Vector3(17.42f, y + 3.15f, 62.93f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_E_2", parent, new Vector3(46.58f, y + 3.15f, 62.93f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_W_2", parent, new Vector3(18.25f, y + 3.45f, 62.93f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_E_2", parent, new Vector3(45.75f, y + 3.45f, 62.93f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_W_3", parent, new Vector3(17.42f, y + 3.15f, 70.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_E_3", parent, new Vector3(46.58f, y + 3.15f, 70.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_W_3", parent, new Vector3(18.25f, y + 3.45f, 70.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_E_3", parent, new Vector3(45.75f, y + 3.45f, 70.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_W_4", parent, new Vector3(17.42f, y + 3.15f, 78.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_E_4", parent, new Vector3(46.58f, y + 3.15f, 78.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_W_4", parent, new Vector3(18.25f, y + 3.45f, 78.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_E_4", parent, new Vector3(45.75f, y + 3.45f, 78.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_W_5", parent, new Vector3(17.42f, y + 3.15f, 86.53f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone06_MedicalBay_Vent_E_5", parent, new Vector3(46.58f, y + 3.15f, 86.53f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_W_5", parent, new Vector3(18.25f, y + 3.45f, 86.53f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone06_MedicalBay_PA_E_5", parent, new Vector3(45.75f, y + 3.45f, 86.53f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone06_MedicalBay_ValveManifold_S_0", parent, new Vector3(20.0f, y, 45.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone06_MedicalBay_ValveManifold_N_0", parent, new Vector3(20.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone06_MedicalBay_FuseBox_S_0", parent, new Vector3(21.2f, y + 1.65f, 45.45f), Quaternion.identity);
        BuildWallFuseBox("Zone06_MedicalBay_FuseBox_N_0", parent, new Vector3(18.8f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone06_MedicalBay_ValveManifold_S_1", parent, new Vector3(28.0f, y, 45.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone06_MedicalBay_ValveManifold_N_1", parent, new Vector3(28.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone06_MedicalBay_FuseBox_S_1", parent, new Vector3(29.2f, y + 1.65f, 45.45f), Quaternion.identity);
        BuildWallFuseBox("Zone06_MedicalBay_FuseBox_N_1", parent, new Vector3(26.8f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone06_MedicalBay_ValveManifold_S_2", parent, new Vector3(36.0f, y, 45.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone06_MedicalBay_ValveManifold_N_2", parent, new Vector3(36.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone06_MedicalBay_FuseBox_S_2", parent, new Vector3(37.2f, y + 1.65f, 45.45f), Quaternion.identity);
        BuildWallFuseBox("Zone06_MedicalBay_FuseBox_N_2", parent, new Vector3(34.8f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone06_MedicalBay_ValveManifold_S_3", parent, new Vector3(44.0f, y, 45.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone06_MedicalBay_ValveManifold_N_3", parent, new Vector3(44.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone06_MedicalBay_FuseBox_S_3", parent, new Vector3(45.2f, y + 1.65f, 45.45f), Quaternion.identity);
        BuildWallFuseBox("Zone06_MedicalBay_FuseBox_N_3", parent, new Vector3(42.8f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_L_0", parent, new Vector3(27.8f, y + 3.48f, 48.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_R_0", parent, new Vector3(36.2f, y + 3.48f, 48.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_L_1", parent, new Vector3(27.8f, y + 3.48f, 53.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_R_1", parent, new Vector3(36.2f, y + 3.48f, 53.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_L_2", parent, new Vector3(27.8f, y + 3.48f, 59.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_R_2", parent, new Vector3(36.2f, y + 3.48f, 59.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_L_3", parent, new Vector3(27.8f, y + 3.48f, 64.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_R_3", parent, new Vector3(36.2f, y + 3.48f, 64.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_L_4", parent, new Vector3(27.8f, y + 3.48f, 70.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone06_MedicalBay_Baffle_R_4", parent, new Vector3(36.2f, y + 3.48f, 70.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone06_MedicalBay_Crates_SW", parent, new Vector3(18.6f, y, 46.6f), 14f);
        BuildCableSpoolAndMat("Zone06_MedicalBay_Spool_SE", parent, new Vector3(45.4f, y, 46.6f), -28f);
        BuildHazmatDrumCluster("Zone06_MedicalBay_Drums_NW", parent, new Vector3(18.6f, y, 71.4f));
        BuildPatchPanelRack("Zone06_MedicalBay_PatchRack_NE", parent, new Vector3(45.4f, y, 71.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone06_MedicalBay_CheckStation_S_0", parent, new Vector3(19.4f, y, 46.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone06_MedicalBay_Bollard_S_0", parent, new Vector3(18.55f, y, 45.95f));
        BuildWallWarningBollard("Zone06_MedicalBay_Bollard_N_0", parent, new Vector3(20.25f, y, 72.05f));
        BuildClipboardCheckStation("Zone06_MedicalBay_CheckStation_S_1", parent, new Vector3(27.8f, y, 46.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone06_MedicalBay_Bollard_S_1", parent, new Vector3(26.95f, y, 45.95f));
        BuildWallWarningBollard("Zone06_MedicalBay_Bollard_N_1", parent, new Vector3(28.65f, y, 72.05f));
        BuildClipboardCheckStation("Zone06_MedicalBay_CheckStation_S_2", parent, new Vector3(36.2f, y, 46.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone06_MedicalBay_Bollard_S_2", parent, new Vector3(35.35f, y, 45.95f));
        BuildWallWarningBollard("Zone06_MedicalBay_Bollard_N_2", parent, new Vector3(37.05f, y, 72.05f));
        BuildClipboardCheckStation("Zone06_MedicalBay_CheckStation_S_3", parent, new Vector3(44.6f, y, 46.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone06_MedicalBay_Bollard_S_3", parent, new Vector3(43.75f, y, 45.95f));
        BuildWallWarningBollard("Zone06_MedicalBay_Bollard_N_3", parent, new Vector3(45.45f, y, 72.05f));
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_W_0", parent, new Vector3(17.25f, y + 2.0f, 46.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_E_0", parent, new Vector3(46.75f, y + 2.0f, 46.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_W_0", parent, new Vector3(17.38f, y + 0.32f, 46.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_E_0", parent, new Vector3(46.62f, y + 0.32f, 46.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_W_1", parent, new Vector3(17.25f, y + 2.0f, 52.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_E_1", parent, new Vector3(46.75f, y + 2.0f, 52.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_W_1", parent, new Vector3(17.38f, y + 0.32f, 52.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_E_1", parent, new Vector3(46.62f, y + 0.32f, 52.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_W_2", parent, new Vector3(17.25f, y + 2.0f, 59.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_E_2", parent, new Vector3(46.75f, y + 2.0f, 59.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_W_2", parent, new Vector3(17.38f, y + 0.32f, 59.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_E_2", parent, new Vector3(46.62f, y + 0.32f, 59.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_W_3", parent, new Vector3(17.25f, y + 2.0f, 65.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_E_3", parent, new Vector3(46.75f, y + 2.0f, 65.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_W_3", parent, new Vector3(17.38f, y + 0.32f, 65.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_E_3", parent, new Vector3(46.62f, y + 0.32f, 65.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_W_4", parent, new Vector3(17.25f, y + 2.0f, 71.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone06_MedicalBay_Pilaster_E_4", parent, new Vector3(46.75f, y + 2.0f, 71.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Tile);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_W_4", parent, new Vector3(17.38f, y + 0.32f, 71.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_ConduitTrim_E_4", parent, new Vector3(46.62f, y + 0.32f, 71.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingRib_0", parent, new Vector3(32.0f, y + 3.82f, 46.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_L_0", parent, new Vector3(23.94f, y + 3.68f, 46.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_R_0", parent, new Vector3(40.06f, y + 3.68f, 46.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CeilingRib_1", parent, new Vector3(32.0f, y + 3.82f, 52.75f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_L_1", parent, new Vector3(23.94f, y + 3.68f, 52.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_R_1", parent, new Vector3(40.06f, y + 3.68f, 52.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CeilingRib_2", parent, new Vector3(32.0f, y + 3.82f, 59.0f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_L_2", parent, new Vector3(23.94f, y + 3.68f, 59.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_R_2", parent, new Vector3(40.06f, y + 3.68f, 59.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CeilingRib_3", parent, new Vector3(32.0f, y + 3.82f, 65.25f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_L_3", parent, new Vector3(23.94f, y + 3.68f, 65.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_R_3", parent, new Vector3(40.06f, y + 3.68f, 65.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CeilingRib_4", parent, new Vector3(32.0f, y + 3.82f, 71.5f), new Vector3(28.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_L_4", parent, new Vector3(23.94f, y + 3.68f, 71.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CableTrayHanger_R_4", parent, new Vector3(40.06f, y + 3.68f, 71.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_FloorSafetyLine_S_0", parent, new Vector3(19.8f, y + 0.014f, 46.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_FloorSafetyLine_N_0", parent, new Vector3(19.8f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_WallDataPlaque_S_0", parent, new Vector3(19.8f, y + 2.25f, 45.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_WallDataPlaque_N_0", parent, new Vector3(19.8f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_FloorSafetyLine_S_1", parent, new Vector3(27.93f, y + 0.014f, 46.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_FloorSafetyLine_N_1", parent, new Vector3(27.93f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_WallDataPlaque_S_1", parent, new Vector3(27.93f, y + 2.25f, 45.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_WallDataPlaque_N_1", parent, new Vector3(27.93f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_FloorSafetyLine_S_2", parent, new Vector3(36.07f, y + 0.014f, 46.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_FloorSafetyLine_N_2", parent, new Vector3(36.07f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_WallDataPlaque_S_2", parent, new Vector3(36.07f, y + 2.25f, 45.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_WallDataPlaque_N_2", parent, new Vector3(36.07f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_FloorSafetyLine_S_3", parent, new Vector3(44.2f, y + 0.014f, 46.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_FloorSafetyLine_N_3", parent, new Vector3(44.2f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_WallDataPlaque_S_3", parent, new Vector3(44.2f, y + 2.25f, 45.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_WallDataPlaque_N_3", parent, new Vector3(44.2f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensor_W_0", parent, new Vector3(17.42f, y + 2.45f, 47.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensorLed_W_0", parent, new Vector3(17.51f, y + 2.48f, 47.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensor_E_0", parent, new Vector3(46.58f, y + 2.45f, 47.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensorLed_E_0", parent, new Vector3(46.49f, y + 2.48f, 47.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensor_W_1", parent, new Vector3(17.42f, y + 2.45f, 55.13f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensorLed_W_1", parent, new Vector3(17.51f, y + 2.48f, 55.13f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensor_E_1", parent, new Vector3(46.58f, y + 2.45f, 55.13f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensorLed_E_1", parent, new Vector3(46.49f, y + 2.48f, 55.13f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensor_W_2", parent, new Vector3(17.42f, y + 2.45f, 62.87f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensorLed_W_2", parent, new Vector3(17.51f, y + 2.48f, 62.87f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensor_E_2", parent, new Vector3(46.58f, y + 2.45f, 62.87f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensorLed_E_2", parent, new Vector3(46.49f, y + 2.48f, 62.87f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensor_W_3", parent, new Vector3(17.42f, y + 2.45f, 70.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensorLed_W_3", parent, new Vector3(17.51f, y + 2.48f, 70.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensor_E_3", parent, new Vector3(46.58f, y + 2.45f, 70.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_AcousticSensorLed_E_3", parent, new Vector3(46.49f, y + 2.48f, 70.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone06_MedicalBay_DrainStrip_L_0", parent, new Vector3(29.4f, y + 0.012f, 47.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_DrainStrip_R_0", parent, new Vector3(34.6f, y + 0.012f, 47.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallCableClamp_W_0", parent, new Vector3(17.36f, y + 1.15f, 47.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_WallCableClamp_E_0", parent, new Vector3(46.64f, y + 1.15f, 47.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_DrainStrip_L_1", parent, new Vector3(29.4f, y + 0.012f, 55.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_DrainStrip_R_1", parent, new Vector3(34.6f, y + 0.012f, 55.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallCableClamp_W_1", parent, new Vector3(17.36f, y + 1.15f, 55.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_WallCableClamp_E_1", parent, new Vector3(46.64f, y + 1.15f, 55.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_DrainStrip_L_2", parent, new Vector3(29.4f, y + 0.012f, 63.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_DrainStrip_R_2", parent, new Vector3(34.6f, y + 0.012f, 63.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallCableClamp_W_2", parent, new Vector3(17.36f, y + 1.15f, 63.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_WallCableClamp_E_2", parent, new Vector3(46.64f, y + 1.15f, 63.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_DrainStrip_L_3", parent, new Vector3(29.4f, y + 0.012f, 71.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_DrainStrip_R_3", parent, new Vector3(34.6f, y + 0.012f, 71.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallCableClamp_W_3", parent, new Vector3(17.36f, y + 1.15f, 71.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_WallCableClamp_E_3", parent, new Vector3(46.64f, y + 1.15f, 71.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_Baseboard_S_0", parent, new Vector3(19.2f, y + 0.12f, 45.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_Baseboard_N_0", parent, new Vector3(19.2f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingPipe_S_0", parent, new Vector3(19.2f, y + 3.55f, 46.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_CeilingPipe_N_0", parent, new Vector3(19.2f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_Baseboard_S_1", parent, new Vector3(27.73f, y + 0.12f, 45.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_Baseboard_N_1", parent, new Vector3(27.73f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingPipe_S_1", parent, new Vector3(27.73f, y + 3.55f, 46.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_CeilingPipe_N_1", parent, new Vector3(27.73f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_Baseboard_S_2", parent, new Vector3(36.27f, y + 0.12f, 45.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_Baseboard_N_2", parent, new Vector3(36.27f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingPipe_S_2", parent, new Vector3(36.27f, y + 3.55f, 46.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_CeilingPipe_N_2", parent, new Vector3(36.27f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_Baseboard_S_3", parent, new Vector3(44.8f, y + 0.12f, 45.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_Baseboard_N_3", parent, new Vector3(44.8f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingPipe_S_3", parent, new Vector3(44.8f, y + 3.55f, 46.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_CeilingPipe_N_3", parent, new Vector3(44.8f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone06_MedicalBay_AcousticPanel_W_0", parent, new Vector3(17.36f, y + 1.85f, 47.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone06_MedicalBay_AcousticPanel_E_0", parent, new Vector3(46.64f, y + 1.85f, 47.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateDecorBox("Zone06_MedicalBay_StrobeMount_W_0", parent, new Vector3(17.4f, y + 3.05f, 48.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone06_MedicalBay_StrobeMount_E_0", parent, new Vector3(46.6f, y + 3.05f, 46.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone06_MedicalBay_AcousticPanel_W_1", parent, new Vector3(17.36f, y + 1.85f, 55.2f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone06_MedicalBay_AcousticPanel_E_1", parent, new Vector3(46.64f, y + 1.85f, 55.2f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateDecorBox("Zone06_MedicalBay_StrobeMount_W_1", parent, new Vector3(17.4f, y + 3.05f, 55.85f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone06_MedicalBay_StrobeMount_E_1", parent, new Vector3(46.6f, y + 3.05f, 54.55f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone06_MedicalBay_AcousticPanel_W_2", parent, new Vector3(17.36f, y + 1.85f, 62.8f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone06_MedicalBay_AcousticPanel_E_2", parent, new Vector3(46.64f, y + 1.85f, 62.8f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateDecorBox("Zone06_MedicalBay_StrobeMount_W_2", parent, new Vector3(17.4f, y + 3.05f, 63.45f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone06_MedicalBay_StrobeMount_E_2", parent, new Vector3(46.6f, y + 3.05f, 62.15f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone06_MedicalBay_AcousticPanel_W_3", parent, new Vector3(17.36f, y + 1.85f, 70.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateAcousticBox("Zone06_MedicalBay_AcousticPanel_E_3", parent, new Vector3(46.64f, y + 1.85f, 70.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Tile);
        CreateDecorBox("Zone06_MedicalBay_StrobeMount_W_3", parent, new Vector3(17.4f, y + 3.05f, 71.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone06_MedicalBay_StrobeMount_E_3", parent, new Vector3(46.6f, y + 3.05f, 69.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone06_MedicalBay_CeilingVentBox_S_0", parent, new Vector3(19.6f, y + 3.86f, 55.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CeilingVentBox_N_0", parent, new Vector3(19.6f, y + 3.86f, 62.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_VerticalConduit_S_0", parent, new Vector3(19.6f, y + 1.95f, 45.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_VerticalConduit_N_0", parent, new Vector3(19.6f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_CeilingVentBox_S_1", parent, new Vector3(27.87f, y + 3.86f, 55.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CeilingVentBox_N_1", parent, new Vector3(27.87f, y + 3.86f, 62.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_VerticalConduit_S_1", parent, new Vector3(27.87f, y + 1.95f, 45.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_VerticalConduit_N_1", parent, new Vector3(27.87f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_CeilingVentBox_S_2", parent, new Vector3(36.13f, y + 3.86f, 55.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CeilingVentBox_N_2", parent, new Vector3(36.13f, y + 3.86f, 62.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_VerticalConduit_S_2", parent, new Vector3(36.13f, y + 1.95f, 45.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_VerticalConduit_N_2", parent, new Vector3(36.13f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_CeilingVentBox_S_3", parent, new Vector3(44.4f, y + 3.86f, 55.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_CeilingVentBox_N_3", parent, new Vector3(44.4f, y + 3.86f, 62.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_VerticalConduit_S_3", parent, new Vector3(44.4f, y + 1.95f, 45.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_VerticalConduit_N_3", parent, new Vector3(44.4f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_W_0", parent, new Vector3(17.34f, y + 3.65f, 46.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_E_0", parent, new Vector3(46.66f, y + 3.65f, 46.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_W_0", parent, new Vector3(17.33f, y + 1.32f, 46.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_E_0", parent, new Vector3(46.67f, y + 1.32f, 46.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_W_1", parent, new Vector3(17.34f, y + 3.65f, 52.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_E_1", parent, new Vector3(46.66f, y + 3.65f, 52.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_W_1", parent, new Vector3(17.33f, y + 1.32f, 52.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_E_1", parent, new Vector3(46.67f, y + 1.32f, 52.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_W_2", parent, new Vector3(17.34f, y + 3.65f, 59.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_E_2", parent, new Vector3(46.66f, y + 3.65f, 59.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_W_2", parent, new Vector3(17.33f, y + 1.32f, 59.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_E_2", parent, new Vector3(46.67f, y + 1.32f, 59.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_W_3", parent, new Vector3(17.34f, y + 3.65f, 65.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_E_3", parent, new Vector3(46.66f, y + 3.65f, 65.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_W_3", parent, new Vector3(17.33f, y + 1.32f, 65.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_E_3", parent, new Vector3(46.67f, y + 1.32f, 65.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_W_4", parent, new Vector3(17.34f, y + 3.65f, 71.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_UpperTrim_E_4", parent, new Vector3(46.66f, y + 3.65f, 71.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_W_4", parent, new Vector3(17.33f, y + 1.32f, 71.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone06_MedicalBay_MidStripe_E_4", parent, new Vector3(46.67f, y + 1.32f, 71.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_L_0", parent, new Vector3(28.8f, y + 3.84f, 46.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_R_0", parent, new Vector3(35.2f, y + 3.84f, 46.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_W_0", parent, new Vector3(17.38f, y + 0.78f, 46.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_E_0", parent, new Vector3(46.62f, y + 0.78f, 46.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_L_1", parent, new Vector3(28.8f, y + 3.84f, 52.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_R_1", parent, new Vector3(35.2f, y + 3.84f, 52.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_W_1", parent, new Vector3(17.38f, y + 0.78f, 52.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_E_1", parent, new Vector3(46.62f, y + 0.78f, 52.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_L_2", parent, new Vector3(28.8f, y + 3.84f, 59.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_R_2", parent, new Vector3(35.2f, y + 3.84f, 59.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_W_2", parent, new Vector3(17.38f, y + 0.78f, 59.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_E_2", parent, new Vector3(46.62f, y + 0.78f, 59.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_L_3", parent, new Vector3(28.8f, y + 3.84f, 65.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_R_3", parent, new Vector3(35.2f, y + 3.84f, 65.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_W_3", parent, new Vector3(17.38f, y + 0.78f, 65.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_E_3", parent, new Vector3(46.62f, y + 0.78f, 65.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_L_4", parent, new Vector3(28.8f, y + 3.84f, 71.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone06_MedicalBay_Sprinkler_R_4", parent, new Vector3(35.2f, y + 3.84f, 71.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_W_4", parent, new Vector3(17.38f, y + 0.78f, 71.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallJunction_E_4", parent, new Vector3(46.62f, y + 0.78f, 71.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_FloorJoint_X_0", parent, new Vector3(19.0f, y + 0.008f, 59.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingStrut_S_0", parent, new Vector3(19.0f, y + 3.74f, 47.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingStrut_N_0", parent, new Vector3(19.0f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallBracket_S_0", parent, new Vector3(19.0f, y + 2.85f, 45.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_WallBracket_N_0", parent, new Vector3(19.0f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_FloorJoint_X_1", parent, new Vector3(27.67f, y + 0.008f, 59.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingStrut_S_1", parent, new Vector3(27.67f, y + 3.74f, 47.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingStrut_N_1", parent, new Vector3(27.67f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallBracket_S_1", parent, new Vector3(27.67f, y + 2.85f, 45.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_WallBracket_N_1", parent, new Vector3(27.67f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_FloorJoint_X_2", parent, new Vector3(36.33f, y + 0.008f, 59.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingStrut_S_2", parent, new Vector3(36.33f, y + 3.74f, 47.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingStrut_N_2", parent, new Vector3(36.33f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallBracket_S_2", parent, new Vector3(36.33f, y + 2.85f, 45.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_WallBracket_N_2", parent, new Vector3(36.33f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_FloorJoint_X_3", parent, new Vector3(45.0f, y + 0.008f, 59.0f), new Vector3(0.05f, 0.01f, 25.0f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingStrut_S_3", parent, new Vector3(45.0f, y + 3.74f, 47.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_CeilingStrut_N_3", parent, new Vector3(45.0f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_WallBracket_S_3", parent, new Vector3(45.0f, y + 2.85f, 45.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_WallBracket_N_3", parent, new Vector3(45.0f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_W_0", parent, new Vector3(17.35f, y + 0.55f, 46.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_E_0", parent, new Vector3(46.65f, y + 0.55f, 46.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_W_0", parent, new Vector3(17.35f, y + 3.38f, 46.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_E_0", parent, new Vector3(46.65f, y + 3.38f, 46.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_W_1", parent, new Vector3(17.35f, y + 0.55f, 52.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_E_1", parent, new Vector3(46.65f, y + 0.55f, 52.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_W_1", parent, new Vector3(17.35f, y + 3.38f, 52.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_E_1", parent, new Vector3(46.65f, y + 3.38f, 52.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_W_2", parent, new Vector3(17.35f, y + 0.55f, 59.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_E_2", parent, new Vector3(46.65f, y + 0.55f, 59.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_W_2", parent, new Vector3(17.35f, y + 3.38f, 59.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_E_2", parent, new Vector3(46.65f, y + 3.38f, 59.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_W_3", parent, new Vector3(17.35f, y + 0.55f, 65.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_E_3", parent, new Vector3(46.65f, y + 0.55f, 65.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_W_3", parent, new Vector3(17.35f, y + 3.38f, 65.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_E_3", parent, new Vector3(46.65f, y + 3.38f, 65.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_W_4", parent, new Vector3(17.35f, y + 0.55f, 71.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_LowerPipe_E_4", parent, new Vector3(46.65f, y + 0.55f, 71.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_W_4", parent, new Vector3(17.35f, y + 3.38f, 71.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_HighPipe_E_4", parent, new Vector3(46.65f, y + 3.38f, 71.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone06_MedicalBay_DuctCollar_C_0", parent, new Vector3(32.0f, y + 3.76f, 46.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_W_0", parent, new Vector3(17.38f, y + 1.58f, 46.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_E_0", parent, new Vector3(46.62f, y + 1.58f, 46.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_W_0", parent, new Vector3(17.65f, y + 0.01f, 46.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_E_0", parent, new Vector3(46.35f, y + 0.01f, 46.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_DuctCollar_C_1", parent, new Vector3(32.0f, y + 3.76f, 52.9f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_W_1", parent, new Vector3(17.38f, y + 1.58f, 52.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_E_1", parent, new Vector3(46.62f, y + 1.58f, 52.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_W_1", parent, new Vector3(17.65f, y + 0.01f, 52.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_E_1", parent, new Vector3(46.35f, y + 0.01f, 52.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_DuctCollar_C_2", parent, new Vector3(32.0f, y + 3.76f, 59.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_W_2", parent, new Vector3(17.38f, y + 1.58f, 59.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_E_2", parent, new Vector3(46.62f, y + 1.58f, 59.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_W_2", parent, new Vector3(17.65f, y + 0.01f, 59.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_E_2", parent, new Vector3(46.35f, y + 0.01f, 59.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_DuctCollar_C_3", parent, new Vector3(32.0f, y + 3.76f, 65.1f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_W_3", parent, new Vector3(17.38f, y + 1.58f, 65.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_E_3", parent, new Vector3(46.62f, y + 1.58f, 65.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_W_3", parent, new Vector3(17.65f, y + 0.01f, 65.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_E_3", parent, new Vector3(46.35f, y + 0.01f, 65.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_DuctCollar_C_4", parent, new Vector3(32.0f, y + 3.76f, 71.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_W_4", parent, new Vector3(17.38f, y + 1.58f, 71.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_TelemetryNode_E_4", parent, new Vector3(46.62f, y + 1.58f, 71.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_W_4", parent, new Vector3(17.65f, y + 0.01f, 71.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_FloorBoltPlate_E_4", parent, new Vector3(46.35f, y + 0.01f, 71.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_L_0", parent, new Vector3(26.8f, y + 3.88f, 46.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_R_0", parent, new Vector3(37.2f, y + 3.88f, 46.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_W_0", parent, new Vector3(17.36f, y + 2.15f, 46.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_E_0", parent, new Vector3(46.64f, y + 2.15f, 46.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_L_1", parent, new Vector3(26.8f, y + 3.88f, 52.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_R_1", parent, new Vector3(37.2f, y + 3.88f, 52.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_W_1", parent, new Vector3(17.36f, y + 2.15f, 52.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_E_1", parent, new Vector3(46.64f, y + 2.15f, 52.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_L_2", parent, new Vector3(26.8f, y + 3.88f, 59.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_R_2", parent, new Vector3(37.2f, y + 3.88f, 59.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_W_2", parent, new Vector3(17.36f, y + 2.15f, 59.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_E_2", parent, new Vector3(46.64f, y + 2.15f, 59.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_L_3", parent, new Vector3(26.8f, y + 3.88f, 65.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_R_3", parent, new Vector3(37.2f, y + 3.88f, 65.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_W_3", parent, new Vector3(17.36f, y + 2.15f, 65.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_E_3", parent, new Vector3(46.64f, y + 2.15f, 65.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_L_4", parent, new Vector3(26.8f, y + 3.88f, 71.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_InspHatch_R_4", parent, new Vector3(37.2f, y + 3.88f, 71.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_W_4", parent, new Vector3(17.36f, y + 2.15f, 71.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone06_MedicalBay_RailClamp_E_4", parent, new Vector3(46.64f, y + 2.15f, 71.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone07_Security(Transform parent)
    {
        float y = MainY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone07_Security
        BuildFireExtinguisherCabinet("Zone07_Security_FireCab_W_0", parent, new Vector3(-12.45f, y + 1.45f, 55.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone07_Security_FireCab_E_0", parent, new Vector3(12.45f, y + 1.45f, 55.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone07_Security_FireCab_W_1", parent, new Vector3(-12.45f, y + 1.45f, 63.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone07_Security_FireCab_E_1", parent, new Vector3(12.45f, y + 1.45f, 63.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone07_Security_FireCab_W_2", parent, new Vector3(-12.45f, y + 1.45f, 70.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone07_Security_FireCab_E_2", parent, new Vector3(12.45f, y + 1.45f, 70.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone07_Security_CCTV_0", parent, new Vector3(-12.55f, y + 3.35f, 53.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone07_Security_CCTV_1", parent, new Vector3(12.55f, y + 3.35f, 53.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone07_Security_CCTV_2", parent, new Vector3(-12.55f, y + 3.35f, 72.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone07_Security_CCTV_3", parent, new Vector3(12.55f, y + 3.35f, 72.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone07_Security_CCTV_4", parent, new Vector3(-3.5f, y + 3.35f, 53.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone07_Security_CCTV_5", parent, new Vector3(3.5f, y + 3.35f, 72.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone07_Security_Intercom_W_0", parent, new Vector3(-12.58f, y + 1.52f, 56.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone07_Security_Intercom_E_0", parent, new Vector3(12.58f, y + 1.52f, 56.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone07_Security_Clock_W_0", parent, new Vector3(-12.6f, y + 2.85f, 56.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone07_Security_Clock_E_0", parent, new Vector3(12.6f, y + 2.85f, 56.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone07_Security_Intercom_W_1", parent, new Vector3(-12.58f, y + 1.52f, 60.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone07_Security_Intercom_E_1", parent, new Vector3(12.58f, y + 1.52f, 60.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone07_Security_Intercom_W_2", parent, new Vector3(-12.58f, y + 1.52f, 65.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone07_Security_Intercom_E_2", parent, new Vector3(12.58f, y + 1.52f, 65.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone07_Security_Clock_W_2", parent, new Vector3(-12.6f, y + 2.85f, 65.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone07_Security_Clock_E_2", parent, new Vector3(12.6f, y + 2.85f, 65.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone07_Security_Intercom_W_3", parent, new Vector3(-12.58f, y + 1.52f, 70.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone07_Security_Intercom_E_3", parent, new Vector3(12.58f, y + 1.52f, 70.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_W_0", parent, new Vector3(-12.55f, y, 55.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_E_0", parent, new Vector3(12.55f, y, 55.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_W_1", parent, new Vector3(-12.55f, y, 60.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_E_1", parent, new Vector3(12.55f, y, 60.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_W_2", parent, new Vector3(-12.55f, y, 65.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_E_2", parent, new Vector3(12.55f, y, 65.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_W_3", parent, new Vector3(-12.55f, y, 71.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_E_3", parent, new Vector3(12.55f, y, 71.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_W_4", parent, new Vector3(-12.55f, y, 76.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_E_4", parent, new Vector3(12.55f, y, 76.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_W_5", parent, new Vector3(-12.55f, y, 81.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone07_Security_Radiator_E_5", parent, new Vector3(12.55f, y, 81.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_W_0", parent, new Vector3(-12.58f, y + 3.15f, 55.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_E_0", parent, new Vector3(12.58f, y + 3.15f, 55.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_W_0", parent, new Vector3(-11.75f, y + 3.45f, 55.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_E_0", parent, new Vector3(11.75f, y + 3.45f, 55.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_W_1", parent, new Vector3(-12.58f, y + 3.15f, 60.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_E_1", parent, new Vector3(12.58f, y + 3.15f, 60.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_W_1", parent, new Vector3(-11.75f, y + 3.45f, 60.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_E_1", parent, new Vector3(11.75f, y + 3.45f, 60.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_W_2", parent, new Vector3(-12.58f, y + 3.15f, 65.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_E_2", parent, new Vector3(12.58f, y + 3.15f, 65.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_W_2", parent, new Vector3(-11.75f, y + 3.45f, 65.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_E_2", parent, new Vector3(11.75f, y + 3.45f, 65.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_W_3", parent, new Vector3(-12.58f, y + 3.15f, 70.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_E_3", parent, new Vector3(12.58f, y + 3.15f, 70.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_W_3", parent, new Vector3(-11.75f, y + 3.45f, 70.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_E_3", parent, new Vector3(11.75f, y + 3.45f, 70.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_W_4", parent, new Vector3(-12.58f, y + 3.15f, 76.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_E_4", parent, new Vector3(12.58f, y + 3.15f, 76.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_W_4", parent, new Vector3(-11.75f, y + 3.45f, 76.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_E_4", parent, new Vector3(11.75f, y + 3.45f, 76.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_W_5", parent, new Vector3(-12.58f, y + 3.15f, 81.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone07_Security_Vent_E_5", parent, new Vector3(12.58f, y + 3.15f, 81.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_W_5", parent, new Vector3(-11.75f, y + 3.45f, 81.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone07_Security_PA_E_5", parent, new Vector3(11.75f, y + 3.45f, 81.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone07_Security_ValveManifold_S_0", parent, new Vector3(-10.0f, y, 53.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone07_Security_ValveManifold_N_0", parent, new Vector3(-10.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone07_Security_FuseBox_S_0", parent, new Vector3(-8.8f, y + 1.65f, 53.45f), Quaternion.identity);
        BuildWallFuseBox("Zone07_Security_FuseBox_N_0", parent, new Vector3(-11.2f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone07_Security_ValveManifold_S_1", parent, new Vector3(-3.33f, y, 53.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone07_Security_ValveManifold_N_1", parent, new Vector3(-3.33f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone07_Security_FuseBox_S_1", parent, new Vector3(-2.13f, y + 1.65f, 53.45f), Quaternion.identity);
        BuildWallFuseBox("Zone07_Security_FuseBox_N_1", parent, new Vector3(-4.53f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone07_Security_ValveManifold_S_2", parent, new Vector3(3.33f, y, 53.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone07_Security_ValveManifold_N_2", parent, new Vector3(3.33f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone07_Security_FuseBox_S_2", parent, new Vector3(4.53f, y + 1.65f, 53.45f), Quaternion.identity);
        BuildWallFuseBox("Zone07_Security_FuseBox_N_2", parent, new Vector3(2.13f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone07_Security_ValveManifold_S_3", parent, new Vector3(10.0f, y, 53.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone07_Security_ValveManifold_N_3", parent, new Vector3(10.0f, y, 72.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone07_Security_FuseBox_S_3", parent, new Vector3(11.2f, y + 1.65f, 53.45f), Quaternion.identity);
        BuildWallFuseBox("Zone07_Security_FuseBox_N_3", parent, new Vector3(8.8f, y + 1.65f, 72.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_L_0", parent, new Vector3(-4.2f, y + 3.48f, 56.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_R_0", parent, new Vector3(4.2f, y + 3.48f, 56.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_L_1", parent, new Vector3(-4.2f, y + 3.48f, 59.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_R_1", parent, new Vector3(4.2f, y + 3.48f, 59.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_L_2", parent, new Vector3(-4.2f, y + 3.48f, 63.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_R_2", parent, new Vector3(4.2f, y + 3.48f, 63.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_L_3", parent, new Vector3(-4.2f, y + 3.48f, 66.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_R_3", parent, new Vector3(4.2f, y + 3.48f, 66.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_L_4", parent, new Vector3(-4.2f, y + 3.48f, 70.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone07_Security_Baffle_R_4", parent, new Vector3(4.2f, y + 3.48f, 70.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone07_Security_Crates_SW", parent, new Vector3(-11.4f, y, 54.6f), 14f);
        BuildCableSpoolAndMat("Zone07_Security_Spool_SE", parent, new Vector3(11.4f, y, 54.6f), -28f);
        BuildHazmatDrumCluster("Zone07_Security_Drums_NW", parent, new Vector3(-11.4f, y, 71.4f));
        BuildPatchPanelRack("Zone07_Security_PatchRack_NE", parent, new Vector3(11.4f, y, 71.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone07_Security_CheckStation_S_0", parent, new Vector3(-10.6f, y, 54.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone07_Security_Bollard_S_0", parent, new Vector3(-11.45f, y, 53.95f));
        BuildWallWarningBollard("Zone07_Security_Bollard_N_0", parent, new Vector3(-9.75f, y, 72.05f));
        BuildClipboardCheckStation("Zone07_Security_CheckStation_S_1", parent, new Vector3(-3.53f, y, 54.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone07_Security_Bollard_S_1", parent, new Vector3(-4.38f, y, 53.95f));
        BuildWallWarningBollard("Zone07_Security_Bollard_N_1", parent, new Vector3(-2.68f, y, 72.05f));
        BuildClipboardCheckStation("Zone07_Security_CheckStation_S_2", parent, new Vector3(3.53f, y, 54.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone07_Security_Bollard_S_2", parent, new Vector3(2.68f, y, 53.95f));
        BuildWallWarningBollard("Zone07_Security_Bollard_N_2", parent, new Vector3(4.38f, y, 72.05f));
        BuildClipboardCheckStation("Zone07_Security_CheckStation_S_3", parent, new Vector3(10.6f, y, 54.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone07_Security_Bollard_S_3", parent, new Vector3(9.75f, y, 53.95f));
        BuildWallWarningBollard("Zone07_Security_Bollard_N_3", parent, new Vector3(11.45f, y, 72.05f));
        CreateAcousticBox("Zone07_Security_Pilaster_W_0", parent, new Vector3(-12.75f, y + 2.0f, 54.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone07_Security_Pilaster_E_0", parent, new Vector3(12.75f, y + 2.0f, 54.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone07_Security_ConduitTrim_W_0", parent, new Vector3(-12.62f, y + 0.32f, 54.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone07_Security_ConduitTrim_E_0", parent, new Vector3(12.62f, y + 0.32f, 54.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone07_Security_Pilaster_W_1", parent, new Vector3(-12.75f, y + 2.0f, 58.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone07_Security_Pilaster_E_1", parent, new Vector3(12.75f, y + 2.0f, 58.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone07_Security_ConduitTrim_W_1", parent, new Vector3(-12.62f, y + 0.32f, 58.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone07_Security_ConduitTrim_E_1", parent, new Vector3(12.62f, y + 0.32f, 58.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone07_Security_Pilaster_W_2", parent, new Vector3(-12.75f, y + 2.0f, 63.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone07_Security_Pilaster_E_2", parent, new Vector3(12.75f, y + 2.0f, 63.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone07_Security_ConduitTrim_W_2", parent, new Vector3(-12.62f, y + 0.32f, 63.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone07_Security_ConduitTrim_E_2", parent, new Vector3(12.62f, y + 0.32f, 63.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone07_Security_Pilaster_W_3", parent, new Vector3(-12.75f, y + 2.0f, 67.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone07_Security_Pilaster_E_3", parent, new Vector3(12.75f, y + 2.0f, 67.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone07_Security_ConduitTrim_W_3", parent, new Vector3(-12.62f, y + 0.32f, 67.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone07_Security_ConduitTrim_E_3", parent, new Vector3(12.62f, y + 0.32f, 67.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone07_Security_Pilaster_W_4", parent, new Vector3(-12.75f, y + 2.0f, 71.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone07_Security_Pilaster_E_4", parent, new Vector3(12.75f, y + 2.0f, 71.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone07_Security_ConduitTrim_W_4", parent, new Vector3(-12.62f, y + 0.32f, 71.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone07_Security_ConduitTrim_E_4", parent, new Vector3(12.62f, y + 0.32f, 71.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingRib_0", parent, new Vector3(0.0f, y + 3.82f, 54.5f), new Vector3(24.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CableTrayHanger_L_0", parent, new Vector3(-6.94f, y + 3.68f, 54.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CableTrayHanger_R_0", parent, new Vector3(6.94f, y + 3.68f, 54.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CeilingRib_1", parent, new Vector3(0.0f, y + 3.82f, 58.75f), new Vector3(24.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CableTrayHanger_L_1", parent, new Vector3(-6.94f, y + 3.68f, 58.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CableTrayHanger_R_1", parent, new Vector3(6.94f, y + 3.68f, 58.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CeilingRib_2", parent, new Vector3(0.0f, y + 3.82f, 63.0f), new Vector3(24.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CableTrayHanger_L_2", parent, new Vector3(-6.94f, y + 3.68f, 63.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CableTrayHanger_R_2", parent, new Vector3(6.94f, y + 3.68f, 63.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CeilingRib_3", parent, new Vector3(0.0f, y + 3.82f, 67.25f), new Vector3(24.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CableTrayHanger_L_3", parent, new Vector3(-6.94f, y + 3.68f, 67.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CableTrayHanger_R_3", parent, new Vector3(6.94f, y + 3.68f, 67.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CeilingRib_4", parent, new Vector3(0.0f, y + 3.82f, 71.5f), new Vector3(24.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CableTrayHanger_L_4", parent, new Vector3(-6.94f, y + 3.68f, 71.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CableTrayHanger_R_4", parent, new Vector3(6.94f, y + 3.68f, 71.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_FloorSafetyLine_S_0", parent, new Vector3(-10.2f, y + 0.014f, 54.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_FloorSafetyLine_N_0", parent, new Vector3(-10.2f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_WallDataPlaque_S_0", parent, new Vector3(-10.2f, y + 2.25f, 53.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_WallDataPlaque_N_0", parent, new Vector3(-10.2f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_FloorSafetyLine_S_1", parent, new Vector3(-3.4f, y + 0.014f, 54.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_FloorSafetyLine_N_1", parent, new Vector3(-3.4f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_WallDataPlaque_S_1", parent, new Vector3(-3.4f, y + 2.25f, 53.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_WallDataPlaque_N_1", parent, new Vector3(-3.4f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_FloorSafetyLine_S_2", parent, new Vector3(3.4f, y + 0.014f, 54.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_FloorSafetyLine_N_2", parent, new Vector3(3.4f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_WallDataPlaque_S_2", parent, new Vector3(3.4f, y + 2.25f, 53.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_WallDataPlaque_N_2", parent, new Vector3(3.4f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_FloorSafetyLine_S_3", parent, new Vector3(10.2f, y + 0.014f, 54.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_FloorSafetyLine_N_3", parent, new Vector3(10.2f, y + 0.014f, 71.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_WallDataPlaque_S_3", parent, new Vector3(10.2f, y + 2.25f, 53.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_WallDataPlaque_N_3", parent, new Vector3(10.2f, y + 2.25f, 72.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_AcousticSensor_W_0", parent, new Vector3(-12.58f, y + 2.45f, 55.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone07_Security_AcousticSensorLed_W_0", parent, new Vector3(-12.49f, y + 2.48f, 55.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone07_Security_AcousticSensor_E_0", parent, new Vector3(12.58f, y + 2.45f, 55.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone07_Security_AcousticSensorLed_E_0", parent, new Vector3(12.49f, y + 2.48f, 55.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone07_Security_AcousticSensor_W_1", parent, new Vector3(-12.58f, y + 2.45f, 60.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone07_Security_AcousticSensorLed_W_1", parent, new Vector3(-12.49f, y + 2.48f, 60.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone07_Security_AcousticSensor_E_1", parent, new Vector3(12.58f, y + 2.45f, 60.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone07_Security_AcousticSensorLed_E_1", parent, new Vector3(12.49f, y + 2.48f, 60.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone07_Security_AcousticSensor_W_2", parent, new Vector3(-12.58f, y + 2.45f, 65.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone07_Security_AcousticSensorLed_W_2", parent, new Vector3(-12.49f, y + 2.48f, 65.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone07_Security_AcousticSensor_E_2", parent, new Vector3(12.58f, y + 2.45f, 65.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone07_Security_AcousticSensorLed_E_2", parent, new Vector3(12.49f, y + 2.48f, 65.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone07_Security_AcousticSensor_W_3", parent, new Vector3(-12.58f, y + 2.45f, 70.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone07_Security_AcousticSensorLed_W_3", parent, new Vector3(-12.49f, y + 2.48f, 70.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone07_Security_AcousticSensor_E_3", parent, new Vector3(12.58f, y + 2.45f, 70.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone07_Security_AcousticSensorLed_E_3", parent, new Vector3(12.49f, y + 2.48f, 70.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone07_Security_DrainStrip_L_0", parent, new Vector3(-2.6f, y + 0.012f, 55.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone07_Security_DrainStrip_R_0", parent, new Vector3(2.6f, y + 0.012f, 55.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallCableClamp_W_0", parent, new Vector3(-12.64f, y + 1.15f, 55.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_WallCableClamp_E_0", parent, new Vector3(12.64f, y + 1.15f, 55.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_DrainStrip_L_1", parent, new Vector3(-2.6f, y + 0.012f, 60.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone07_Security_DrainStrip_R_1", parent, new Vector3(2.6f, y + 0.012f, 60.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallCableClamp_W_1", parent, new Vector3(-12.64f, y + 1.15f, 60.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_WallCableClamp_E_1", parent, new Vector3(12.64f, y + 1.15f, 60.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_DrainStrip_L_2", parent, new Vector3(-2.6f, y + 0.012f, 65.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone07_Security_DrainStrip_R_2", parent, new Vector3(2.6f, y + 0.012f, 65.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallCableClamp_W_2", parent, new Vector3(-12.64f, y + 1.15f, 65.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_WallCableClamp_E_2", parent, new Vector3(12.64f, y + 1.15f, 65.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_DrainStrip_L_3", parent, new Vector3(-2.6f, y + 0.012f, 71.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone07_Security_DrainStrip_R_3", parent, new Vector3(2.6f, y + 0.012f, 71.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallCableClamp_W_3", parent, new Vector3(-12.64f, y + 1.15f, 71.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_WallCableClamp_E_3", parent, new Vector3(12.64f, y + 1.15f, 71.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_Baseboard_S_0", parent, new Vector3(-10.8f, y + 0.12f, 53.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone07_Security_Baseboard_N_0", parent, new Vector3(-10.8f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingPipe_S_0", parent, new Vector3(-10.8f, y + 3.55f, 54.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_CeilingPipe_N_0", parent, new Vector3(-10.8f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_Baseboard_S_1", parent, new Vector3(-3.6f, y + 0.12f, 53.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone07_Security_Baseboard_N_1", parent, new Vector3(-3.6f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingPipe_S_1", parent, new Vector3(-3.6f, y + 3.55f, 54.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_CeilingPipe_N_1", parent, new Vector3(-3.6f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_Baseboard_S_2", parent, new Vector3(3.6f, y + 0.12f, 53.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone07_Security_Baseboard_N_2", parent, new Vector3(3.6f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingPipe_S_2", parent, new Vector3(3.6f, y + 3.55f, 54.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_CeilingPipe_N_2", parent, new Vector3(3.6f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_Baseboard_S_3", parent, new Vector3(10.8f, y + 0.12f, 53.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone07_Security_Baseboard_N_3", parent, new Vector3(10.8f, y + 0.12f, 72.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingPipe_S_3", parent, new Vector3(10.8f, y + 3.55f, 54.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_CeilingPipe_N_3", parent, new Vector3(10.8f, y + 3.55f, 71.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone07_Security_AcousticPanel_W_0", parent, new Vector3(-12.64f, y + 1.85f, 55.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone07_Security_AcousticPanel_E_0", parent, new Vector3(12.64f, y + 1.85f, 55.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateDecorBox("Zone07_Security_StrobeMount_W_0", parent, new Vector3(-12.6f, y + 3.05f, 56.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone07_Security_StrobeMount_E_0", parent, new Vector3(12.6f, y + 3.05f, 54.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone07_Security_AcousticPanel_W_1", parent, new Vector3(-12.64f, y + 1.85f, 60.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone07_Security_AcousticPanel_E_1", parent, new Vector3(12.64f, y + 1.85f, 60.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateDecorBox("Zone07_Security_StrobeMount_W_1", parent, new Vector3(-12.6f, y + 3.05f, 61.18f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone07_Security_StrobeMount_E_1", parent, new Vector3(12.6f, y + 3.05f, 59.88f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone07_Security_AcousticPanel_W_2", parent, new Vector3(-12.64f, y + 1.85f, 65.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone07_Security_AcousticPanel_E_2", parent, new Vector3(12.64f, y + 1.85f, 65.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateDecorBox("Zone07_Security_StrobeMount_W_2", parent, new Vector3(-12.6f, y + 3.05f, 66.12f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone07_Security_StrobeMount_E_2", parent, new Vector3(12.6f, y + 3.05f, 64.82f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone07_Security_AcousticPanel_W_3", parent, new Vector3(-12.64f, y + 1.85f, 70.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone07_Security_AcousticPanel_E_3", parent, new Vector3(12.64f, y + 1.85f, 70.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateDecorBox("Zone07_Security_StrobeMount_W_3", parent, new Vector3(-12.6f, y + 3.05f, 71.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone07_Security_StrobeMount_E_3", parent, new Vector3(12.6f, y + 3.05f, 69.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone07_Security_CeilingVentBox_S_0", parent, new Vector3(-10.4f, y + 3.86f, 59.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CeilingVentBox_N_0", parent, new Vector3(-10.4f, y + 3.86f, 66.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_VerticalConduit_S_0", parent, new Vector3(-10.4f, y + 1.95f, 53.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_VerticalConduit_N_0", parent, new Vector3(-10.4f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_CeilingVentBox_S_1", parent, new Vector3(-3.47f, y + 3.86f, 59.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CeilingVentBox_N_1", parent, new Vector3(-3.47f, y + 3.86f, 66.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_VerticalConduit_S_1", parent, new Vector3(-3.47f, y + 1.95f, 53.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_VerticalConduit_N_1", parent, new Vector3(-3.47f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_CeilingVentBox_S_2", parent, new Vector3(3.47f, y + 3.86f, 59.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CeilingVentBox_N_2", parent, new Vector3(3.47f, y + 3.86f, 66.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_VerticalConduit_S_2", parent, new Vector3(3.47f, y + 1.95f, 53.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_VerticalConduit_N_2", parent, new Vector3(3.47f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_CeilingVentBox_S_3", parent, new Vector3(10.4f, y + 3.86f, 59.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_CeilingVentBox_N_3", parent, new Vector3(10.4f, y + 3.86f, 66.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_VerticalConduit_S_3", parent, new Vector3(10.4f, y + 1.95f, 53.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_VerticalConduit_N_3", parent, new Vector3(10.4f, y + 1.95f, 72.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_UpperTrim_W_0", parent, new Vector3(-12.66f, y + 3.65f, 54.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_UpperTrim_E_0", parent, new Vector3(12.66f, y + 3.65f, 54.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_MidStripe_W_0", parent, new Vector3(-12.67f, y + 1.32f, 54.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_MidStripe_E_0", parent, new Vector3(12.67f, y + 1.32f, 54.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_UpperTrim_W_1", parent, new Vector3(-12.66f, y + 3.65f, 58.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_UpperTrim_E_1", parent, new Vector3(12.66f, y + 3.65f, 58.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_MidStripe_W_1", parent, new Vector3(-12.67f, y + 1.32f, 58.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_MidStripe_E_1", parent, new Vector3(12.67f, y + 1.32f, 58.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_UpperTrim_W_2", parent, new Vector3(-12.66f, y + 3.65f, 63.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_UpperTrim_E_2", parent, new Vector3(12.66f, y + 3.65f, 63.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_MidStripe_W_2", parent, new Vector3(-12.67f, y + 1.32f, 63.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_MidStripe_E_2", parent, new Vector3(12.67f, y + 1.32f, 63.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_UpperTrim_W_3", parent, new Vector3(-12.66f, y + 3.65f, 67.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_UpperTrim_E_3", parent, new Vector3(12.66f, y + 3.65f, 67.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_MidStripe_W_3", parent, new Vector3(-12.67f, y + 1.32f, 67.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_MidStripe_E_3", parent, new Vector3(12.67f, y + 1.32f, 67.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_UpperTrim_W_4", parent, new Vector3(-12.66f, y + 3.65f, 71.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_UpperTrim_E_4", parent, new Vector3(12.66f, y + 3.65f, 71.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone07_Security_MidStripe_W_4", parent, new Vector3(-12.67f, y + 1.32f, 71.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone07_Security_MidStripe_E_4", parent, new Vector3(12.67f, y + 1.32f, 71.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone07_Security_Sprinkler_L_0", parent, new Vector3(-3.2f, y + 3.84f, 54.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone07_Security_Sprinkler_R_0", parent, new Vector3(3.2f, y + 3.84f, 54.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_WallJunction_W_0", parent, new Vector3(-12.62f, y + 0.78f, 54.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallJunction_E_0", parent, new Vector3(12.62f, y + 0.78f, 54.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone07_Security_Sprinkler_L_1", parent, new Vector3(-3.2f, y + 3.84f, 58.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone07_Security_Sprinkler_R_1", parent, new Vector3(3.2f, y + 3.84f, 58.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_WallJunction_W_1", parent, new Vector3(-12.62f, y + 0.78f, 58.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallJunction_E_1", parent, new Vector3(12.62f, y + 0.78f, 58.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone07_Security_Sprinkler_L_2", parent, new Vector3(-3.2f, y + 3.84f, 63.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone07_Security_Sprinkler_R_2", parent, new Vector3(3.2f, y + 3.84f, 63.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_WallJunction_W_2", parent, new Vector3(-12.62f, y + 0.78f, 63.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallJunction_E_2", parent, new Vector3(12.62f, y + 0.78f, 63.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone07_Security_Sprinkler_L_3", parent, new Vector3(-3.2f, y + 3.84f, 67.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone07_Security_Sprinkler_R_3", parent, new Vector3(3.2f, y + 3.84f, 67.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_WallJunction_W_3", parent, new Vector3(-12.62f, y + 0.78f, 67.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallJunction_E_3", parent, new Vector3(12.62f, y + 0.78f, 67.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone07_Security_Sprinkler_L_4", parent, new Vector3(-3.2f, y + 3.84f, 71.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone07_Security_Sprinkler_R_4", parent, new Vector3(3.2f, y + 3.84f, 71.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_WallJunction_W_4", parent, new Vector3(-12.62f, y + 0.78f, 71.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallJunction_E_4", parent, new Vector3(12.62f, y + 0.78f, 71.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone07_Security_FloorJoint_X_0", parent, new Vector3(-11.0f, y + 0.008f, 63.0f), new Vector3(0.05f, 0.01f, 17.0f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingStrut_S_0", parent, new Vector3(-11.0f, y + 3.74f, 55.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingStrut_N_0", parent, new Vector3(-11.0f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallBracket_S_0", parent, new Vector3(-11.0f, y + 2.85f, 53.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_WallBracket_N_0", parent, new Vector3(-11.0f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_FloorJoint_X_1", parent, new Vector3(-3.67f, y + 0.008f, 63.0f), new Vector3(0.05f, 0.01f, 17.0f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingStrut_S_1", parent, new Vector3(-3.67f, y + 3.74f, 55.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingStrut_N_1", parent, new Vector3(-3.67f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallBracket_S_1", parent, new Vector3(-3.67f, y + 2.85f, 53.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_WallBracket_N_1", parent, new Vector3(-3.67f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_FloorJoint_X_2", parent, new Vector3(3.67f, y + 0.008f, 63.0f), new Vector3(0.05f, 0.01f, 17.0f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingStrut_S_2", parent, new Vector3(3.67f, y + 3.74f, 55.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingStrut_N_2", parent, new Vector3(3.67f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallBracket_S_2", parent, new Vector3(3.67f, y + 2.85f, 53.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_WallBracket_N_2", parent, new Vector3(3.67f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_FloorJoint_X_3", parent, new Vector3(11.0f, y + 0.008f, 63.0f), new Vector3(0.05f, 0.01f, 17.0f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingStrut_S_3", parent, new Vector3(11.0f, y + 3.74f, 55.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone07_Security_CeilingStrut_N_3", parent, new Vector3(11.0f, y + 3.74f, 70.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone07_Security_WallBracket_S_3", parent, new Vector3(11.0f, y + 2.85f, 53.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_WallBracket_N_3", parent, new Vector3(11.0f, y + 2.85f, 72.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_LowerPipe_W_0", parent, new Vector3(-12.65f, y + 0.55f, 54.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_LowerPipe_E_0", parent, new Vector3(12.65f, y + 0.55f, 54.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_HighPipe_W_0", parent, new Vector3(-12.65f, y + 3.38f, 54.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_HighPipe_E_0", parent, new Vector3(12.65f, y + 3.38f, 54.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_LowerPipe_W_1", parent, new Vector3(-12.65f, y + 0.55f, 58.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_LowerPipe_E_1", parent, new Vector3(12.65f, y + 0.55f, 58.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_HighPipe_W_1", parent, new Vector3(-12.65f, y + 3.38f, 58.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_HighPipe_E_1", parent, new Vector3(12.65f, y + 3.38f, 58.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_LowerPipe_W_2", parent, new Vector3(-12.65f, y + 0.55f, 63.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_LowerPipe_E_2", parent, new Vector3(12.65f, y + 0.55f, 63.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_HighPipe_W_2", parent, new Vector3(-12.65f, y + 3.38f, 63.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_HighPipe_E_2", parent, new Vector3(12.65f, y + 3.38f, 63.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_LowerPipe_W_3", parent, new Vector3(-12.65f, y + 0.55f, 67.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_LowerPipe_E_3", parent, new Vector3(12.65f, y + 0.55f, 67.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_HighPipe_W_3", parent, new Vector3(-12.65f, y + 3.38f, 67.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_HighPipe_E_3", parent, new Vector3(12.65f, y + 3.38f, 67.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_LowerPipe_W_4", parent, new Vector3(-12.65f, y + 0.55f, 71.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_LowerPipe_E_4", parent, new Vector3(12.65f, y + 0.55f, 71.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone07_Security_HighPipe_W_4", parent, new Vector3(-12.65f, y + 3.38f, 71.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_HighPipe_E_4", parent, new Vector3(12.65f, y + 3.38f, 71.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone07_Security_DuctCollar_C_0", parent, new Vector3(0.0f, y + 3.76f, 54.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone07_Security_TelemetryNode_W_0", parent, new Vector3(-12.62f, y + 1.58f, 54.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_TelemetryNode_E_0", parent, new Vector3(12.62f, y + 1.58f, 54.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_W_0", parent, new Vector3(-12.35f, y + 0.01f, 54.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_E_0", parent, new Vector3(12.35f, y + 0.01f, 54.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_DuctCollar_C_1", parent, new Vector3(0.0f, y + 3.76f, 58.9f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone07_Security_TelemetryNode_W_1", parent, new Vector3(-12.62f, y + 1.58f, 58.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_TelemetryNode_E_1", parent, new Vector3(12.62f, y + 1.58f, 58.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_W_1", parent, new Vector3(-12.35f, y + 0.01f, 58.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_E_1", parent, new Vector3(12.35f, y + 0.01f, 58.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_DuctCollar_C_2", parent, new Vector3(0.0f, y + 3.76f, 63.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone07_Security_TelemetryNode_W_2", parent, new Vector3(-12.62f, y + 1.58f, 63.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_TelemetryNode_E_2", parent, new Vector3(12.62f, y + 1.58f, 63.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_W_2", parent, new Vector3(-12.35f, y + 0.01f, 63.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_E_2", parent, new Vector3(12.35f, y + 0.01f, 63.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_DuctCollar_C_3", parent, new Vector3(0.0f, y + 3.76f, 67.1f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone07_Security_TelemetryNode_W_3", parent, new Vector3(-12.62f, y + 1.58f, 67.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_TelemetryNode_E_3", parent, new Vector3(12.62f, y + 1.58f, 67.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_W_3", parent, new Vector3(-12.35f, y + 0.01f, 67.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_E_3", parent, new Vector3(12.35f, y + 0.01f, 67.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_DuctCollar_C_4", parent, new Vector3(0.0f, y + 3.76f, 71.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone07_Security_TelemetryNode_W_4", parent, new Vector3(-12.62f, y + 1.58f, 71.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_TelemetryNode_E_4", parent, new Vector3(12.62f, y + 1.58f, 71.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_W_4", parent, new Vector3(-12.35f, y + 0.01f, 71.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_FloorBoltPlate_E_4", parent, new Vector3(12.35f, y + 0.01f, 71.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone07_Security_InspHatch_L_0", parent, new Vector3(-5.2f, y + 3.88f, 54.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_InspHatch_R_0", parent, new Vector3(5.2f, y + 3.88f, 54.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_RailClamp_W_0", parent, new Vector3(-12.64f, y + 2.15f, 54.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_RailClamp_E_0", parent, new Vector3(12.64f, y + 2.15f, 54.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_InspHatch_L_1", parent, new Vector3(-5.2f, y + 3.88f, 58.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_InspHatch_R_1", parent, new Vector3(5.2f, y + 3.88f, 58.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_RailClamp_W_1", parent, new Vector3(-12.64f, y + 2.15f, 58.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_RailClamp_E_1", parent, new Vector3(12.64f, y + 2.15f, 58.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_InspHatch_L_2", parent, new Vector3(-5.2f, y + 3.88f, 63.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_InspHatch_R_2", parent, new Vector3(5.2f, y + 3.88f, 63.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_RailClamp_W_2", parent, new Vector3(-12.64f, y + 2.15f, 63.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_RailClamp_E_2", parent, new Vector3(12.64f, y + 2.15f, 63.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_InspHatch_L_3", parent, new Vector3(-5.2f, y + 3.88f, 67.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_InspHatch_R_3", parent, new Vector3(5.2f, y + 3.88f, 67.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_RailClamp_W_3", parent, new Vector3(-12.64f, y + 2.15f, 67.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_RailClamp_E_3", parent, new Vector3(12.64f, y + 2.15f, 67.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_InspHatch_L_4", parent, new Vector3(-5.2f, y + 3.88f, 71.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_InspHatch_R_4", parent, new Vector3(5.2f, y + 3.88f, 71.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone07_Security_RailClamp_W_4", parent, new Vector3(-12.64f, y + 2.15f, 71.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone07_Security_RailClamp_E_4", parent, new Vector3(12.64f, y + 2.15f, 71.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone08_ServerHall(Transform parent)
    {
        float y = MainY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone08_ServerHall
        BuildFireExtinguisherCabinet("Zone08_ServerHall_FireCab_W_0", parent, new Vector3(-46.45f, y + 1.45f, 77.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone08_ServerHall_FireCab_E_0", parent, new Vector3(-15.55f, y + 1.45f, 77.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone08_ServerHall_FireCab_W_1", parent, new Vector3(-46.45f, y + 1.45f, 91.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone08_ServerHall_FireCab_E_1", parent, new Vector3(-15.55f, y + 1.45f, 91.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone08_ServerHall_FireCab_W_2", parent, new Vector3(-46.45f, y + 1.45f, 104.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone08_ServerHall_FireCab_E_2", parent, new Vector3(-15.55f, y + 1.45f, 104.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone08_ServerHall_CCTV_0", parent, new Vector3(-46.55f, y + 3.35f, 75.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone08_ServerHall_CCTV_1", parent, new Vector3(-15.45f, y + 3.35f, 75.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone08_ServerHall_CCTV_2", parent, new Vector3(-46.55f, y + 3.35f, 106.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone08_ServerHall_CCTV_3", parent, new Vector3(-15.45f, y + 3.35f, 106.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone08_ServerHall_CCTV_4", parent, new Vector3(-34.5f, y + 3.35f, 75.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone08_ServerHall_CCTV_5", parent, new Vector3(-27.5f, y + 3.35f, 106.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone08_ServerHall_Intercom_W_0", parent, new Vector3(-46.58f, y + 1.52f, 78.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone08_ServerHall_Intercom_E_0", parent, new Vector3(-15.42f, y + 1.52f, 78.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone08_ServerHall_Clock_W_0", parent, new Vector3(-46.6f, y + 2.85f, 78.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone08_ServerHall_Clock_E_0", parent, new Vector3(-15.4f, y + 2.85f, 78.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone08_ServerHall_Intercom_W_1", parent, new Vector3(-46.58f, y + 1.52f, 86.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone08_ServerHall_Intercom_E_1", parent, new Vector3(-15.42f, y + 1.52f, 86.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone08_ServerHall_Intercom_W_2", parent, new Vector3(-46.58f, y + 1.52f, 95.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone08_ServerHall_Intercom_E_2", parent, new Vector3(-15.42f, y + 1.52f, 95.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone08_ServerHall_Clock_W_2", parent, new Vector3(-46.6f, y + 2.85f, 95.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone08_ServerHall_Clock_E_2", parent, new Vector3(-15.4f, y + 2.85f, 95.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone08_ServerHall_Intercom_W_3", parent, new Vector3(-46.58f, y + 1.52f, 104.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone08_ServerHall_Intercom_E_3", parent, new Vector3(-15.42f, y + 1.52f, 104.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_W_0", parent, new Vector3(-46.55f, y, 77.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_E_0", parent, new Vector3(-15.45f, y, 77.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_W_1", parent, new Vector3(-46.55f, y, 86.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_E_1", parent, new Vector3(-15.45f, y, 86.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_W_2", parent, new Vector3(-46.55f, y, 95.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_E_2", parent, new Vector3(-15.45f, y, 95.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_W_3", parent, new Vector3(-46.55f, y, 105.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_E_3", parent, new Vector3(-15.45f, y, 105.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_W_4", parent, new Vector3(-46.55f, y, 114.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_E_4", parent, new Vector3(-15.45f, y, 114.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_W_5", parent, new Vector3(-46.55f, y, 123.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone08_ServerHall_Radiator_E_5", parent, new Vector3(-15.45f, y, 123.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_W_0", parent, new Vector3(-46.58f, y + 3.15f, 77.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_E_0", parent, new Vector3(-15.42f, y + 3.15f, 77.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_W_0", parent, new Vector3(-45.75f, y + 3.45f, 77.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_E_0", parent, new Vector3(-16.25f, y + 3.45f, 77.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_W_1", parent, new Vector3(-46.58f, y + 3.15f, 86.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_E_1", parent, new Vector3(-15.42f, y + 3.15f, 86.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_W_1", parent, new Vector3(-45.75f, y + 3.45f, 86.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_E_1", parent, new Vector3(-16.25f, y + 3.45f, 86.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_W_2", parent, new Vector3(-46.58f, y + 3.15f, 95.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_E_2", parent, new Vector3(-15.42f, y + 3.15f, 95.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_W_2", parent, new Vector3(-45.75f, y + 3.45f, 95.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_E_2", parent, new Vector3(-16.25f, y + 3.45f, 95.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_W_3", parent, new Vector3(-46.58f, y + 3.15f, 104.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_E_3", parent, new Vector3(-15.42f, y + 3.15f, 104.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_W_3", parent, new Vector3(-45.75f, y + 3.45f, 104.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_E_3", parent, new Vector3(-16.25f, y + 3.45f, 104.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_W_4", parent, new Vector3(-46.58f, y + 3.15f, 114.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_E_4", parent, new Vector3(-15.42f, y + 3.15f, 114.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_W_4", parent, new Vector3(-45.75f, y + 3.45f, 114.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_E_4", parent, new Vector3(-16.25f, y + 3.45f, 114.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_W_5", parent, new Vector3(-46.58f, y + 3.15f, 123.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone08_ServerHall_Vent_E_5", parent, new Vector3(-15.42f, y + 3.15f, 123.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_W_5", parent, new Vector3(-45.75f, y + 3.45f, 123.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone08_ServerHall_PA_E_5", parent, new Vector3(-16.25f, y + 3.45f, 123.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone08_ServerHall_ValveManifold_S_0", parent, new Vector3(-44.0f, y, 75.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone08_ServerHall_ValveManifold_N_0", parent, new Vector3(-44.0f, y, 106.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone08_ServerHall_FuseBox_S_0", parent, new Vector3(-42.8f, y + 1.65f, 75.45f), Quaternion.identity);
        BuildWallFuseBox("Zone08_ServerHall_FuseBox_N_0", parent, new Vector3(-45.2f, y + 1.65f, 106.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone08_ServerHall_ValveManifold_S_1", parent, new Vector3(-35.33f, y, 75.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone08_ServerHall_ValveManifold_N_1", parent, new Vector3(-35.33f, y, 106.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone08_ServerHall_FuseBox_S_1", parent, new Vector3(-34.13f, y + 1.65f, 75.45f), Quaternion.identity);
        BuildWallFuseBox("Zone08_ServerHall_FuseBox_N_1", parent, new Vector3(-36.53f, y + 1.65f, 106.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone08_ServerHall_ValveManifold_S_2", parent, new Vector3(-26.67f, y, 75.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone08_ServerHall_ValveManifold_N_2", parent, new Vector3(-26.67f, y, 106.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone08_ServerHall_FuseBox_S_2", parent, new Vector3(-25.47f, y + 1.65f, 75.45f), Quaternion.identity);
        BuildWallFuseBox("Zone08_ServerHall_FuseBox_N_2", parent, new Vector3(-27.87f, y + 1.65f, 106.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone08_ServerHall_ValveManifold_S_3", parent, new Vector3(-18.0f, y, 75.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone08_ServerHall_ValveManifold_N_3", parent, new Vector3(-18.0f, y, 106.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone08_ServerHall_FuseBox_S_3", parent, new Vector3(-16.8f, y + 1.65f, 75.45f), Quaternion.identity);
        BuildWallFuseBox("Zone08_ServerHall_FuseBox_N_3", parent, new Vector3(-19.2f, y + 1.65f, 106.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_L_0", parent, new Vector3(-35.2f, y + 3.48f, 78.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_R_0", parent, new Vector3(-26.8f, y + 3.48f, 78.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_L_1", parent, new Vector3(-35.2f, y + 3.48f, 84.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_R_1", parent, new Vector3(-26.8f, y + 3.48f, 84.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_L_2", parent, new Vector3(-35.2f, y + 3.48f, 91.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_R_2", parent, new Vector3(-26.8f, y + 3.48f, 91.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_L_3", parent, new Vector3(-35.2f, y + 3.48f, 97.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_R_3", parent, new Vector3(-26.8f, y + 3.48f, 97.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_L_4", parent, new Vector3(-35.2f, y + 3.48f, 104.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone08_ServerHall_Baffle_R_4", parent, new Vector3(-26.8f, y + 3.48f, 104.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone08_ServerHall_Crates_SW", parent, new Vector3(-45.4f, y, 76.6f), 14f);
        BuildCableSpoolAndMat("Zone08_ServerHall_Spool_SE", parent, new Vector3(-16.6f, y, 76.6f), -28f);
        BuildHazmatDrumCluster("Zone08_ServerHall_Drums_NW", parent, new Vector3(-45.4f, y, 105.4f));
        BuildPatchPanelRack("Zone08_ServerHall_PatchRack_NE", parent, new Vector3(-16.6f, y, 105.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone08_ServerHall_CheckStation_S_0", parent, new Vector3(-44.6f, y, 76.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone08_ServerHall_Bollard_S_0", parent, new Vector3(-45.45f, y, 75.95f));
        BuildWallWarningBollard("Zone08_ServerHall_Bollard_N_0", parent, new Vector3(-43.75f, y, 106.05f));
        BuildClipboardCheckStation("Zone08_ServerHall_CheckStation_S_1", parent, new Vector3(-35.53f, y, 76.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone08_ServerHall_Bollard_S_1", parent, new Vector3(-36.38f, y, 75.95f));
        BuildWallWarningBollard("Zone08_ServerHall_Bollard_N_1", parent, new Vector3(-34.68f, y, 106.05f));
        BuildClipboardCheckStation("Zone08_ServerHall_CheckStation_S_2", parent, new Vector3(-26.47f, y, 76.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone08_ServerHall_Bollard_S_2", parent, new Vector3(-27.32f, y, 75.95f));
        BuildWallWarningBollard("Zone08_ServerHall_Bollard_N_2", parent, new Vector3(-25.62f, y, 106.05f));
        BuildClipboardCheckStation("Zone08_ServerHall_CheckStation_S_3", parent, new Vector3(-17.4f, y, 76.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone08_ServerHall_Bollard_S_3", parent, new Vector3(-18.25f, y, 75.95f));
        BuildWallWarningBollard("Zone08_ServerHall_Bollard_N_3", parent, new Vector3(-16.55f, y, 106.05f));
        CreateAcousticBox("Zone08_ServerHall_Pilaster_W_0", parent, new Vector3(-46.75f, y + 2.0f, 76.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone08_ServerHall_Pilaster_E_0", parent, new Vector3(-15.25f, y + 2.0f, 76.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_W_0", parent, new Vector3(-46.62f, y + 0.32f, 76.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_E_0", parent, new Vector3(-15.38f, y + 0.32f, 76.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone08_ServerHall_Pilaster_W_1", parent, new Vector3(-46.75f, y + 2.0f, 83.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone08_ServerHall_Pilaster_E_1", parent, new Vector3(-15.25f, y + 2.0f, 83.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_W_1", parent, new Vector3(-46.62f, y + 0.32f, 83.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_E_1", parent, new Vector3(-15.38f, y + 0.32f, 83.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone08_ServerHall_Pilaster_W_2", parent, new Vector3(-46.75f, y + 2.0f, 91.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone08_ServerHall_Pilaster_E_2", parent, new Vector3(-15.25f, y + 2.0f, 91.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_W_2", parent, new Vector3(-46.62f, y + 0.32f, 91.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_E_2", parent, new Vector3(-15.38f, y + 0.32f, 91.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone08_ServerHall_Pilaster_W_3", parent, new Vector3(-46.75f, y + 2.0f, 98.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone08_ServerHall_Pilaster_E_3", parent, new Vector3(-15.25f, y + 2.0f, 98.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_W_3", parent, new Vector3(-46.62f, y + 0.32f, 98.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_E_3", parent, new Vector3(-15.38f, y + 0.32f, 98.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone08_ServerHall_Pilaster_W_4", parent, new Vector3(-46.75f, y + 2.0f, 105.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone08_ServerHall_Pilaster_E_4", parent, new Vector3(-15.25f, y + 2.0f, 105.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Metal);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_W_4", parent, new Vector3(-46.62f, y + 0.32f, 105.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_ConduitTrim_E_4", parent, new Vector3(-15.38f, y + 0.32f, 105.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingRib_0", parent, new Vector3(-31.0f, y + 3.82f, 76.5f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_L_0", parent, new Vector3(-39.62f, y + 3.68f, 76.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_R_0", parent, new Vector3(-22.38f, y + 3.68f, 76.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CeilingRib_1", parent, new Vector3(-31.0f, y + 3.82f, 83.75f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_L_1", parent, new Vector3(-39.62f, y + 3.68f, 83.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_R_1", parent, new Vector3(-22.38f, y + 3.68f, 83.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CeilingRib_2", parent, new Vector3(-31.0f, y + 3.82f, 91.0f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_L_2", parent, new Vector3(-39.62f, y + 3.68f, 91.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_R_2", parent, new Vector3(-22.38f, y + 3.68f, 91.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CeilingRib_3", parent, new Vector3(-31.0f, y + 3.82f, 98.25f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_L_3", parent, new Vector3(-39.62f, y + 3.68f, 98.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_R_3", parent, new Vector3(-22.38f, y + 3.68f, 98.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CeilingRib_4", parent, new Vector3(-31.0f, y + 3.82f, 105.5f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_L_4", parent, new Vector3(-39.62f, y + 3.68f, 105.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CableTrayHanger_R_4", parent, new Vector3(-22.38f, y + 3.68f, 105.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_FloorSafetyLine_S_0", parent, new Vector3(-44.2f, y + 0.014f, 76.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_FloorSafetyLine_N_0", parent, new Vector3(-44.2f, y + 0.014f, 105.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_WallDataPlaque_S_0", parent, new Vector3(-44.2f, y + 2.25f, 75.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_WallDataPlaque_N_0", parent, new Vector3(-44.2f, y + 2.25f, 106.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_FloorSafetyLine_S_1", parent, new Vector3(-35.4f, y + 0.014f, 76.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_FloorSafetyLine_N_1", parent, new Vector3(-35.4f, y + 0.014f, 105.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_WallDataPlaque_S_1", parent, new Vector3(-35.4f, y + 2.25f, 75.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_WallDataPlaque_N_1", parent, new Vector3(-35.4f, y + 2.25f, 106.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_FloorSafetyLine_S_2", parent, new Vector3(-26.6f, y + 0.014f, 76.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_FloorSafetyLine_N_2", parent, new Vector3(-26.6f, y + 0.014f, 105.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_WallDataPlaque_S_2", parent, new Vector3(-26.6f, y + 2.25f, 75.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_WallDataPlaque_N_2", parent, new Vector3(-26.6f, y + 2.25f, 106.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_FloorSafetyLine_S_3", parent, new Vector3(-17.8f, y + 0.014f, 76.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_FloorSafetyLine_N_3", parent, new Vector3(-17.8f, y + 0.014f, 105.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_WallDataPlaque_S_3", parent, new Vector3(-17.8f, y + 2.25f, 75.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_WallDataPlaque_N_3", parent, new Vector3(-17.8f, y + 2.25f, 106.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_AcousticSensor_W_0", parent, new Vector3(-46.58f, y + 2.45f, 77.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_AcousticSensorLed_W_0", parent, new Vector3(-46.49f, y + 2.48f, 77.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone08_ServerHall_AcousticSensor_E_0", parent, new Vector3(-15.42f, y + 2.45f, 77.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_AcousticSensorLed_E_0", parent, new Vector3(-15.51f, y + 2.48f, 77.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone08_ServerHall_AcousticSensor_W_1", parent, new Vector3(-46.58f, y + 2.45f, 86.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_AcousticSensorLed_W_1", parent, new Vector3(-46.49f, y + 2.48f, 86.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone08_ServerHall_AcousticSensor_E_1", parent, new Vector3(-15.42f, y + 2.45f, 86.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_AcousticSensorLed_E_1", parent, new Vector3(-15.51f, y + 2.48f, 86.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone08_ServerHall_AcousticSensor_W_2", parent, new Vector3(-46.58f, y + 2.45f, 95.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_AcousticSensorLed_W_2", parent, new Vector3(-46.49f, y + 2.48f, 95.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone08_ServerHall_AcousticSensor_E_2", parent, new Vector3(-15.42f, y + 2.45f, 95.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_AcousticSensorLed_E_2", parent, new Vector3(-15.51f, y + 2.48f, 95.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone08_ServerHall_AcousticSensor_W_3", parent, new Vector3(-46.58f, y + 2.45f, 104.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_AcousticSensorLed_W_3", parent, new Vector3(-46.49f, y + 2.48f, 104.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone08_ServerHall_AcousticSensor_E_3", parent, new Vector3(-15.42f, y + 2.45f, 104.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_AcousticSensorLed_E_3", parent, new Vector3(-15.51f, y + 2.48f, 104.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone08_ServerHall_DrainStrip_L_0", parent, new Vector3(-33.6f, y + 0.012f, 77.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_DrainStrip_R_0", parent, new Vector3(-28.4f, y + 0.012f, 77.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallCableClamp_W_0", parent, new Vector3(-46.64f, y + 1.15f, 77.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_WallCableClamp_E_0", parent, new Vector3(-15.36f, y + 1.15f, 77.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_DrainStrip_L_1", parent, new Vector3(-33.6f, y + 0.012f, 86.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_DrainStrip_R_1", parent, new Vector3(-28.4f, y + 0.012f, 86.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallCableClamp_W_1", parent, new Vector3(-46.64f, y + 1.15f, 86.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_WallCableClamp_E_1", parent, new Vector3(-15.36f, y + 1.15f, 86.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_DrainStrip_L_2", parent, new Vector3(-33.6f, y + 0.012f, 95.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_DrainStrip_R_2", parent, new Vector3(-28.4f, y + 0.012f, 95.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallCableClamp_W_2", parent, new Vector3(-46.64f, y + 1.15f, 95.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_WallCableClamp_E_2", parent, new Vector3(-15.36f, y + 1.15f, 95.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_DrainStrip_L_3", parent, new Vector3(-33.6f, y + 0.012f, 105.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_DrainStrip_R_3", parent, new Vector3(-28.4f, y + 0.012f, 105.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallCableClamp_W_3", parent, new Vector3(-46.64f, y + 1.15f, 105.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_WallCableClamp_E_3", parent, new Vector3(-15.36f, y + 1.15f, 105.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_Baseboard_S_0", parent, new Vector3(-44.8f, y + 0.12f, 75.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_Baseboard_N_0", parent, new Vector3(-44.8f, y + 0.12f, 106.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingPipe_S_0", parent, new Vector3(-44.8f, y + 3.55f, 76.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_CeilingPipe_N_0", parent, new Vector3(-44.8f, y + 3.55f, 105.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_Baseboard_S_1", parent, new Vector3(-35.6f, y + 0.12f, 75.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_Baseboard_N_1", parent, new Vector3(-35.6f, y + 0.12f, 106.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingPipe_S_1", parent, new Vector3(-35.6f, y + 3.55f, 76.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_CeilingPipe_N_1", parent, new Vector3(-35.6f, y + 3.55f, 105.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_Baseboard_S_2", parent, new Vector3(-26.4f, y + 0.12f, 75.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_Baseboard_N_2", parent, new Vector3(-26.4f, y + 0.12f, 106.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingPipe_S_2", parent, new Vector3(-26.4f, y + 3.55f, 76.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_CeilingPipe_N_2", parent, new Vector3(-26.4f, y + 3.55f, 105.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_Baseboard_S_3", parent, new Vector3(-17.2f, y + 0.12f, 75.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_Baseboard_N_3", parent, new Vector3(-17.2f, y + 0.12f, 106.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingPipe_S_3", parent, new Vector3(-17.2f, y + 3.55f, 76.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_CeilingPipe_N_3", parent, new Vector3(-17.2f, y + 3.55f, 105.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone08_ServerHall_AcousticPanel_W_0", parent, new Vector3(-46.64f, y + 1.85f, 77.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone08_ServerHall_AcousticPanel_E_0", parent, new Vector3(-15.36f, y + 1.85f, 77.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateDecorBox("Zone08_ServerHall_StrobeMount_W_0", parent, new Vector3(-46.6f, y + 3.05f, 78.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone08_ServerHall_StrobeMount_E_0", parent, new Vector3(-15.4f, y + 3.05f, 76.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone08_ServerHall_AcousticPanel_W_1", parent, new Vector3(-46.64f, y + 1.85f, 86.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone08_ServerHall_AcousticPanel_E_1", parent, new Vector3(-15.36f, y + 1.85f, 86.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateDecorBox("Zone08_ServerHall_StrobeMount_W_1", parent, new Vector3(-46.6f, y + 3.05f, 87.18f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone08_ServerHall_StrobeMount_E_1", parent, new Vector3(-15.4f, y + 3.05f, 85.88f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone08_ServerHall_AcousticPanel_W_2", parent, new Vector3(-46.64f, y + 1.85f, 95.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone08_ServerHall_AcousticPanel_E_2", parent, new Vector3(-15.36f, y + 1.85f, 95.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateDecorBox("Zone08_ServerHall_StrobeMount_W_2", parent, new Vector3(-46.6f, y + 3.05f, 96.12f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone08_ServerHall_StrobeMount_E_2", parent, new Vector3(-15.4f, y + 3.05f, 94.82f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone08_ServerHall_AcousticPanel_W_3", parent, new Vector3(-46.64f, y + 1.85f, 104.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateAcousticBox("Zone08_ServerHall_AcousticPanel_E_3", parent, new Vector3(-15.36f, y + 1.85f, 104.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Metal);
        CreateDecorBox("Zone08_ServerHall_StrobeMount_W_3", parent, new Vector3(-46.6f, y + 3.05f, 105.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone08_ServerHall_StrobeMount_E_3", parent, new Vector3(-15.4f, y + 3.05f, 103.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone08_ServerHall_CeilingVentBox_S_0", parent, new Vector3(-44.4f, y + 3.86f, 87.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CeilingVentBox_N_0", parent, new Vector3(-44.4f, y + 3.86f, 94.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_VerticalConduit_S_0", parent, new Vector3(-44.4f, y + 1.95f, 75.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_VerticalConduit_N_0", parent, new Vector3(-44.4f, y + 1.95f, 106.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_CeilingVentBox_S_1", parent, new Vector3(-35.47f, y + 3.86f, 87.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CeilingVentBox_N_1", parent, new Vector3(-35.47f, y + 3.86f, 94.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_VerticalConduit_S_1", parent, new Vector3(-35.47f, y + 1.95f, 75.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_VerticalConduit_N_1", parent, new Vector3(-35.47f, y + 1.95f, 106.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_CeilingVentBox_S_2", parent, new Vector3(-26.53f, y + 3.86f, 87.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CeilingVentBox_N_2", parent, new Vector3(-26.53f, y + 3.86f, 94.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_VerticalConduit_S_2", parent, new Vector3(-26.53f, y + 1.95f, 75.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_VerticalConduit_N_2", parent, new Vector3(-26.53f, y + 1.95f, 106.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_CeilingVentBox_S_3", parent, new Vector3(-17.6f, y + 3.86f, 87.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_CeilingVentBox_N_3", parent, new Vector3(-17.6f, y + 3.86f, 94.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_VerticalConduit_S_3", parent, new Vector3(-17.6f, y + 1.95f, 75.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_VerticalConduit_N_3", parent, new Vector3(-17.6f, y + 1.95f, 106.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_W_0", parent, new Vector3(-46.66f, y + 3.65f, 76.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_E_0", parent, new Vector3(-15.34f, y + 3.65f, 76.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_MidStripe_W_0", parent, new Vector3(-46.67f, y + 1.32f, 76.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_MidStripe_E_0", parent, new Vector3(-15.33f, y + 1.32f, 76.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_W_1", parent, new Vector3(-46.66f, y + 3.65f, 83.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_E_1", parent, new Vector3(-15.34f, y + 3.65f, 83.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_MidStripe_W_1", parent, new Vector3(-46.67f, y + 1.32f, 83.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_MidStripe_E_1", parent, new Vector3(-15.33f, y + 1.32f, 83.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_W_2", parent, new Vector3(-46.66f, y + 3.65f, 91.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_E_2", parent, new Vector3(-15.34f, y + 3.65f, 91.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_MidStripe_W_2", parent, new Vector3(-46.67f, y + 1.32f, 91.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_MidStripe_E_2", parent, new Vector3(-15.33f, y + 1.32f, 91.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_W_3", parent, new Vector3(-46.66f, y + 3.65f, 98.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_E_3", parent, new Vector3(-15.34f, y + 3.65f, 98.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_MidStripe_W_3", parent, new Vector3(-46.67f, y + 1.32f, 98.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_MidStripe_E_3", parent, new Vector3(-15.33f, y + 1.32f, 98.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_W_4", parent, new Vector3(-46.66f, y + 3.65f, 105.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_UpperTrim_E_4", parent, new Vector3(-15.34f, y + 3.65f, 105.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone08_ServerHall_MidStripe_W_4", parent, new Vector3(-46.67f, y + 1.32f, 105.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone08_ServerHall_MidStripe_E_4", parent, new Vector3(-15.33f, y + 1.32f, 105.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_L_0", parent, new Vector3(-34.2f, y + 3.84f, 76.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_R_0", parent, new Vector3(-27.8f, y + 3.84f, 76.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_WallJunction_W_0", parent, new Vector3(-46.62f, y + 0.78f, 76.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallJunction_E_0", parent, new Vector3(-15.38f, y + 0.78f, 76.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_L_1", parent, new Vector3(-34.2f, y + 3.84f, 83.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_R_1", parent, new Vector3(-27.8f, y + 3.84f, 83.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_WallJunction_W_1", parent, new Vector3(-46.62f, y + 0.78f, 83.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallJunction_E_1", parent, new Vector3(-15.38f, y + 0.78f, 83.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_L_2", parent, new Vector3(-34.2f, y + 3.84f, 91.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_R_2", parent, new Vector3(-27.8f, y + 3.84f, 91.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_WallJunction_W_2", parent, new Vector3(-46.62f, y + 0.78f, 91.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallJunction_E_2", parent, new Vector3(-15.38f, y + 0.78f, 91.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_L_3", parent, new Vector3(-34.2f, y + 3.84f, 98.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_R_3", parent, new Vector3(-27.8f, y + 3.84f, 98.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_WallJunction_W_3", parent, new Vector3(-46.62f, y + 0.78f, 98.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallJunction_E_3", parent, new Vector3(-15.38f, y + 0.78f, 98.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_L_4", parent, new Vector3(-34.2f, y + 3.84f, 105.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone08_ServerHall_Sprinkler_R_4", parent, new Vector3(-27.8f, y + 3.84f, 105.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_WallJunction_W_4", parent, new Vector3(-46.62f, y + 0.78f, 105.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallJunction_E_4", parent, new Vector3(-15.38f, y + 0.78f, 105.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_FloorJoint_X_0", parent, new Vector3(-45.0f, y + 0.008f, 91.0f), new Vector3(0.05f, 0.01f, 29.0f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingStrut_S_0", parent, new Vector3(-45.0f, y + 3.74f, 77.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingStrut_N_0", parent, new Vector3(-45.0f, y + 3.74f, 104.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallBracket_S_0", parent, new Vector3(-45.0f, y + 2.85f, 75.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_WallBracket_N_0", parent, new Vector3(-45.0f, y + 2.85f, 106.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_FloorJoint_X_1", parent, new Vector3(-35.67f, y + 0.008f, 91.0f), new Vector3(0.05f, 0.01f, 29.0f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingStrut_S_1", parent, new Vector3(-35.67f, y + 3.74f, 77.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingStrut_N_1", parent, new Vector3(-35.67f, y + 3.74f, 104.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallBracket_S_1", parent, new Vector3(-35.67f, y + 2.85f, 75.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_WallBracket_N_1", parent, new Vector3(-35.67f, y + 2.85f, 106.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_FloorJoint_X_2", parent, new Vector3(-26.33f, y + 0.008f, 91.0f), new Vector3(0.05f, 0.01f, 29.0f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingStrut_S_2", parent, new Vector3(-26.33f, y + 3.74f, 77.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingStrut_N_2", parent, new Vector3(-26.33f, y + 3.74f, 104.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallBracket_S_2", parent, new Vector3(-26.33f, y + 2.85f, 75.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_WallBracket_N_2", parent, new Vector3(-26.33f, y + 2.85f, 106.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_FloorJoint_X_3", parent, new Vector3(-17.0f, y + 0.008f, 91.0f), new Vector3(0.05f, 0.01f, 29.0f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingStrut_S_3", parent, new Vector3(-17.0f, y + 3.74f, 77.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_CeilingStrut_N_3", parent, new Vector3(-17.0f, y + 3.74f, 104.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_WallBracket_S_3", parent, new Vector3(-17.0f, y + 2.85f, 75.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_WallBracket_N_3", parent, new Vector3(-17.0f, y + 2.85f, 106.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_W_0", parent, new Vector3(-46.65f, y + 0.55f, 76.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_E_0", parent, new Vector3(-15.35f, y + 0.55f, 76.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_HighPipe_W_0", parent, new Vector3(-46.65f, y + 3.38f, 76.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_HighPipe_E_0", parent, new Vector3(-15.35f, y + 3.38f, 76.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_W_1", parent, new Vector3(-46.65f, y + 0.55f, 83.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_E_1", parent, new Vector3(-15.35f, y + 0.55f, 83.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_HighPipe_W_1", parent, new Vector3(-46.65f, y + 3.38f, 83.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_HighPipe_E_1", parent, new Vector3(-15.35f, y + 3.38f, 83.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_W_2", parent, new Vector3(-46.65f, y + 0.55f, 91.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_E_2", parent, new Vector3(-15.35f, y + 0.55f, 91.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_HighPipe_W_2", parent, new Vector3(-46.65f, y + 3.38f, 91.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_HighPipe_E_2", parent, new Vector3(-15.35f, y + 3.38f, 91.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_W_3", parent, new Vector3(-46.65f, y + 0.55f, 98.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_E_3", parent, new Vector3(-15.35f, y + 0.55f, 98.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_HighPipe_W_3", parent, new Vector3(-46.65f, y + 3.38f, 98.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_HighPipe_E_3", parent, new Vector3(-15.35f, y + 3.38f, 98.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_W_4", parent, new Vector3(-46.65f, y + 0.55f, 105.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_LowerPipe_E_4", parent, new Vector3(-15.35f, y + 0.55f, 105.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone08_ServerHall_HighPipe_W_4", parent, new Vector3(-46.65f, y + 3.38f, 105.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_HighPipe_E_4", parent, new Vector3(-15.35f, y + 3.38f, 105.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone08_ServerHall_DuctCollar_C_0", parent, new Vector3(-31.0f, y + 3.76f, 76.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_W_0", parent, new Vector3(-46.62f, y + 1.58f, 76.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_E_0", parent, new Vector3(-15.38f, y + 1.58f, 76.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_W_0", parent, new Vector3(-46.35f, y + 0.01f, 76.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_E_0", parent, new Vector3(-15.65f, y + 0.01f, 76.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_DuctCollar_C_1", parent, new Vector3(-31.0f, y + 3.76f, 83.9f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_W_1", parent, new Vector3(-46.62f, y + 1.58f, 83.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_E_1", parent, new Vector3(-15.38f, y + 1.58f, 83.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_W_1", parent, new Vector3(-46.35f, y + 0.01f, 83.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_E_1", parent, new Vector3(-15.65f, y + 0.01f, 83.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_DuctCollar_C_2", parent, new Vector3(-31.0f, y + 3.76f, 91.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_W_2", parent, new Vector3(-46.62f, y + 1.58f, 91.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_E_2", parent, new Vector3(-15.38f, y + 1.58f, 91.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_W_2", parent, new Vector3(-46.35f, y + 0.01f, 91.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_E_2", parent, new Vector3(-15.65f, y + 0.01f, 91.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_DuctCollar_C_3", parent, new Vector3(-31.0f, y + 3.76f, 98.1f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_W_3", parent, new Vector3(-46.62f, y + 1.58f, 98.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_E_3", parent, new Vector3(-15.38f, y + 1.58f, 98.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_W_3", parent, new Vector3(-46.35f, y + 0.01f, 98.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_E_3", parent, new Vector3(-15.65f, y + 0.01f, 98.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_DuctCollar_C_4", parent, new Vector3(-31.0f, y + 3.76f, 105.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_W_4", parent, new Vector3(-46.62f, y + 1.58f, 105.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_TelemetryNode_E_4", parent, new Vector3(-15.38f, y + 1.58f, 105.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_W_4", parent, new Vector3(-46.35f, y + 0.01f, 105.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_FloorBoltPlate_E_4", parent, new Vector3(-15.65f, y + 0.01f, 105.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone08_ServerHall_InspHatch_L_0", parent, new Vector3(-36.2f, y + 3.88f, 76.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_InspHatch_R_0", parent, new Vector3(-25.8f, y + 3.88f, 76.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_RailClamp_W_0", parent, new Vector3(-46.64f, y + 2.15f, 76.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_RailClamp_E_0", parent, new Vector3(-15.36f, y + 2.15f, 76.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_InspHatch_L_1", parent, new Vector3(-36.2f, y + 3.88f, 83.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_InspHatch_R_1", parent, new Vector3(-25.8f, y + 3.88f, 83.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_RailClamp_W_1", parent, new Vector3(-46.64f, y + 2.15f, 83.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_RailClamp_E_1", parent, new Vector3(-15.36f, y + 2.15f, 83.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_InspHatch_L_2", parent, new Vector3(-36.2f, y + 3.88f, 91.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_InspHatch_R_2", parent, new Vector3(-25.8f, y + 3.88f, 91.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_RailClamp_W_2", parent, new Vector3(-46.64f, y + 2.15f, 91.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_RailClamp_E_2", parent, new Vector3(-15.36f, y + 2.15f, 91.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_InspHatch_L_3", parent, new Vector3(-36.2f, y + 3.88f, 98.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_InspHatch_R_3", parent, new Vector3(-25.8f, y + 3.88f, 98.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_RailClamp_W_3", parent, new Vector3(-46.64f, y + 2.15f, 98.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_RailClamp_E_3", parent, new Vector3(-15.36f, y + 2.15f, 98.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_InspHatch_L_4", parent, new Vector3(-36.2f, y + 3.88f, 105.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_InspHatch_R_4", parent, new Vector3(-25.8f, y + 3.88f, 105.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone08_ServerHall_RailClamp_W_4", parent, new Vector3(-46.64f, y + 2.15f, 105.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone08_ServerHall_RailClamp_E_4", parent, new Vector3(-15.36f, y + 2.15f, 105.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone09_Reactor(Transform parent)
    {
        float y = SubY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone09_Reactor
        BuildFireExtinguisherCabinet("Zone09_Reactor_FireCab_W_0", parent, new Vector3(15.55f, y + 1.45f, 77.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone09_Reactor_FireCab_E_0", parent, new Vector3(46.45f, y + 1.45f, 77.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone09_Reactor_FireCab_W_1", parent, new Vector3(15.55f, y + 1.45f, 91.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone09_Reactor_FireCab_E_1", parent, new Vector3(46.45f, y + 1.45f, 91.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone09_Reactor_FireCab_W_2", parent, new Vector3(15.55f, y + 1.45f, 104.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone09_Reactor_FireCab_E_2", parent, new Vector3(46.45f, y + 1.45f, 104.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone09_Reactor_CCTV_0", parent, new Vector3(15.45f, y + 3.35f, 75.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone09_Reactor_CCTV_1", parent, new Vector3(46.55f, y + 3.35f, 75.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone09_Reactor_CCTV_2", parent, new Vector3(15.45f, y + 3.35f, 106.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone09_Reactor_CCTV_3", parent, new Vector3(46.55f, y + 3.35f, 106.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone09_Reactor_CCTV_4", parent, new Vector3(27.5f, y + 3.35f, 75.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone09_Reactor_CCTV_5", parent, new Vector3(34.5f, y + 3.35f, 106.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone09_Reactor_Intercom_W_0", parent, new Vector3(15.42f, y + 1.52f, 78.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone09_Reactor_Intercom_E_0", parent, new Vector3(46.58f, y + 1.52f, 78.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone09_Reactor_Clock_W_0", parent, new Vector3(15.4f, y + 2.85f, 78.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone09_Reactor_Clock_E_0", parent, new Vector3(46.6f, y + 2.85f, 78.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone09_Reactor_Intercom_W_1", parent, new Vector3(15.42f, y + 1.52f, 86.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone09_Reactor_Intercom_E_1", parent, new Vector3(46.58f, y + 1.52f, 86.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone09_Reactor_Intercom_W_2", parent, new Vector3(15.42f, y + 1.52f, 95.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone09_Reactor_Intercom_E_2", parent, new Vector3(46.58f, y + 1.52f, 95.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone09_Reactor_Clock_W_2", parent, new Vector3(15.4f, y + 2.85f, 95.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone09_Reactor_Clock_E_2", parent, new Vector3(46.6f, y + 2.85f, 95.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone09_Reactor_Intercom_W_3", parent, new Vector3(15.42f, y + 1.52f, 104.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone09_Reactor_Intercom_E_3", parent, new Vector3(46.58f, y + 1.52f, 104.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_W_0", parent, new Vector3(15.45f, y, 77.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_E_0", parent, new Vector3(46.55f, y, 77.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_W_1", parent, new Vector3(15.45f, y, 86.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_E_1", parent, new Vector3(46.55f, y, 86.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_W_2", parent, new Vector3(15.45f, y, 95.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_E_2", parent, new Vector3(46.55f, y, 95.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_W_3", parent, new Vector3(15.45f, y, 105.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_E_3", parent, new Vector3(46.55f, y, 105.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_W_4", parent, new Vector3(15.45f, y, 114.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_E_4", parent, new Vector3(46.55f, y, 114.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_W_5", parent, new Vector3(15.45f, y, 123.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone09_Reactor_Radiator_E_5", parent, new Vector3(46.55f, y, 123.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_W_0", parent, new Vector3(15.42f, y + 3.15f, 77.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_E_0", parent, new Vector3(46.58f, y + 3.15f, 77.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_W_0", parent, new Vector3(16.25f, y + 3.45f, 77.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_E_0", parent, new Vector3(45.75f, y + 3.45f, 77.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_W_1", parent, new Vector3(15.42f, y + 3.15f, 86.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_E_1", parent, new Vector3(46.58f, y + 3.15f, 86.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_W_1", parent, new Vector3(16.25f, y + 3.45f, 86.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_E_1", parent, new Vector3(45.75f, y + 3.45f, 86.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_W_2", parent, new Vector3(15.42f, y + 3.15f, 95.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_E_2", parent, new Vector3(46.58f, y + 3.15f, 95.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_W_2", parent, new Vector3(16.25f, y + 3.45f, 95.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_E_2", parent, new Vector3(45.75f, y + 3.45f, 95.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_W_3", parent, new Vector3(15.42f, y + 3.15f, 104.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_E_3", parent, new Vector3(46.58f, y + 3.15f, 104.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_W_3", parent, new Vector3(16.25f, y + 3.45f, 104.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_E_3", parent, new Vector3(45.75f, y + 3.45f, 104.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_W_4", parent, new Vector3(15.42f, y + 3.15f, 114.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_E_4", parent, new Vector3(46.58f, y + 3.15f, 114.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_W_4", parent, new Vector3(16.25f, y + 3.45f, 114.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_E_4", parent, new Vector3(45.75f, y + 3.45f, 114.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_W_5", parent, new Vector3(15.42f, y + 3.15f, 123.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone09_Reactor_Vent_E_5", parent, new Vector3(46.58f, y + 3.15f, 123.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_W_5", parent, new Vector3(16.25f, y + 3.45f, 123.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone09_Reactor_PA_E_5", parent, new Vector3(45.75f, y + 3.45f, 123.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone09_Reactor_ValveManifold_S_0", parent, new Vector3(18.0f, y, 75.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone09_Reactor_ValveManifold_N_0", parent, new Vector3(18.0f, y, 106.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone09_Reactor_FuseBox_S_0", parent, new Vector3(19.2f, y + 1.65f, 75.45f), Quaternion.identity);
        BuildWallFuseBox("Zone09_Reactor_FuseBox_N_0", parent, new Vector3(16.8f, y + 1.65f, 106.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone09_Reactor_ValveManifold_S_1", parent, new Vector3(26.67f, y, 75.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone09_Reactor_ValveManifold_N_1", parent, new Vector3(26.67f, y, 106.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone09_Reactor_FuseBox_S_1", parent, new Vector3(27.87f, y + 1.65f, 75.45f), Quaternion.identity);
        BuildWallFuseBox("Zone09_Reactor_FuseBox_N_1", parent, new Vector3(25.47f, y + 1.65f, 106.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone09_Reactor_ValveManifold_S_2", parent, new Vector3(35.33f, y, 75.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone09_Reactor_ValveManifold_N_2", parent, new Vector3(35.33f, y, 106.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone09_Reactor_FuseBox_S_2", parent, new Vector3(36.53f, y + 1.65f, 75.45f), Quaternion.identity);
        BuildWallFuseBox("Zone09_Reactor_FuseBox_N_2", parent, new Vector3(34.13f, y + 1.65f, 106.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone09_Reactor_ValveManifold_S_3", parent, new Vector3(44.0f, y, 75.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone09_Reactor_ValveManifold_N_3", parent, new Vector3(44.0f, y, 106.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone09_Reactor_FuseBox_S_3", parent, new Vector3(45.2f, y + 1.65f, 75.45f), Quaternion.identity);
        BuildWallFuseBox("Zone09_Reactor_FuseBox_N_3", parent, new Vector3(42.8f, y + 1.65f, 106.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_L_0", parent, new Vector3(26.8f, y + 3.48f, 78.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_R_0", parent, new Vector3(35.2f, y + 3.48f, 78.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_L_1", parent, new Vector3(26.8f, y + 3.48f, 84.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_R_1", parent, new Vector3(35.2f, y + 3.48f, 84.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_L_2", parent, new Vector3(26.8f, y + 3.48f, 91.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_R_2", parent, new Vector3(35.2f, y + 3.48f, 91.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_L_3", parent, new Vector3(26.8f, y + 3.48f, 97.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_R_3", parent, new Vector3(35.2f, y + 3.48f, 97.5f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_L_4", parent, new Vector3(26.8f, y + 3.48f, 104.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone09_Reactor_Baffle_R_4", parent, new Vector3(35.2f, y + 3.48f, 104.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone09_Reactor_Crates_SW", parent, new Vector3(16.6f, y, 76.6f), 14f);
        BuildCableSpoolAndMat("Zone09_Reactor_Spool_SE", parent, new Vector3(45.4f, y, 76.6f), -28f);
        BuildHazmatDrumCluster("Zone09_Reactor_Drums_NW", parent, new Vector3(16.6f, y, 105.4f));
        BuildPatchPanelRack("Zone09_Reactor_PatchRack_NE", parent, new Vector3(45.4f, y, 105.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone09_Reactor_CheckStation_S_0", parent, new Vector3(17.4f, y, 76.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone09_Reactor_Bollard_S_0", parent, new Vector3(16.55f, y, 75.95f));
        BuildWallWarningBollard("Zone09_Reactor_Bollard_N_0", parent, new Vector3(18.25f, y, 106.05f));
        BuildClipboardCheckStation("Zone09_Reactor_CheckStation_S_1", parent, new Vector3(26.47f, y, 76.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone09_Reactor_Bollard_S_1", parent, new Vector3(25.62f, y, 75.95f));
        BuildWallWarningBollard("Zone09_Reactor_Bollard_N_1", parent, new Vector3(27.32f, y, 106.05f));
        BuildClipboardCheckStation("Zone09_Reactor_CheckStation_S_2", parent, new Vector3(35.53f, y, 76.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone09_Reactor_Bollard_S_2", parent, new Vector3(34.68f, y, 75.95f));
        BuildWallWarningBollard("Zone09_Reactor_Bollard_N_2", parent, new Vector3(36.38f, y, 106.05f));
        BuildClipboardCheckStation("Zone09_Reactor_CheckStation_S_3", parent, new Vector3(44.6f, y, 76.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone09_Reactor_Bollard_S_3", parent, new Vector3(43.75f, y, 75.95f));
        BuildWallWarningBollard("Zone09_Reactor_Bollard_N_3", parent, new Vector3(45.45f, y, 106.05f));
        CreateAcousticBox("Zone09_Reactor_Pilaster_W_0", parent, new Vector3(15.25f, y + 2.0f, 76.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone09_Reactor_Pilaster_E_0", parent, new Vector3(46.75f, y + 2.0f, 76.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_W_0", parent, new Vector3(15.38f, y + 0.32f, 76.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_E_0", parent, new Vector3(46.62f, y + 0.32f, 76.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone09_Reactor_Pilaster_W_1", parent, new Vector3(15.25f, y + 2.0f, 83.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone09_Reactor_Pilaster_E_1", parent, new Vector3(46.75f, y + 2.0f, 83.6f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_W_1", parent, new Vector3(15.38f, y + 0.32f, 83.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_E_1", parent, new Vector3(46.62f, y + 0.32f, 83.6f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone09_Reactor_Pilaster_W_2", parent, new Vector3(15.25f, y + 2.0f, 91.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone09_Reactor_Pilaster_E_2", parent, new Vector3(46.75f, y + 2.0f, 91.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_W_2", parent, new Vector3(15.38f, y + 0.32f, 91.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_E_2", parent, new Vector3(46.62f, y + 0.32f, 91.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone09_Reactor_Pilaster_W_3", parent, new Vector3(15.25f, y + 2.0f, 98.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone09_Reactor_Pilaster_E_3", parent, new Vector3(46.75f, y + 2.0f, 98.4f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_W_3", parent, new Vector3(15.38f, y + 0.32f, 98.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_E_3", parent, new Vector3(46.62f, y + 0.32f, 98.4f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone09_Reactor_Pilaster_W_4", parent, new Vector3(15.25f, y + 2.0f, 105.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone09_Reactor_Pilaster_E_4", parent, new Vector3(46.75f, y + 2.0f, 105.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_W_4", parent, new Vector3(15.38f, y + 0.32f, 105.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_ConduitTrim_E_4", parent, new Vector3(46.62f, y + 0.32f, 105.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingRib_0", parent, new Vector3(31.0f, y + 3.82f, 76.5f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_L_0", parent, new Vector3(22.38f, y + 3.68f, 76.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_R_0", parent, new Vector3(39.62f, y + 3.68f, 76.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CeilingRib_1", parent, new Vector3(31.0f, y + 3.82f, 83.75f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_L_1", parent, new Vector3(22.38f, y + 3.68f, 83.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_R_1", parent, new Vector3(39.62f, y + 3.68f, 83.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CeilingRib_2", parent, new Vector3(31.0f, y + 3.82f, 91.0f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_L_2", parent, new Vector3(22.38f, y + 3.68f, 91.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_R_2", parent, new Vector3(39.62f, y + 3.68f, 91.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CeilingRib_3", parent, new Vector3(31.0f, y + 3.82f, 98.25f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_L_3", parent, new Vector3(22.38f, y + 3.68f, 98.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_R_3", parent, new Vector3(39.62f, y + 3.68f, 98.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CeilingRib_4", parent, new Vector3(31.0f, y + 3.82f, 105.5f), new Vector3(30.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_L_4", parent, new Vector3(22.38f, y + 3.68f, 105.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CableTrayHanger_R_4", parent, new Vector3(39.62f, y + 3.68f, 105.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_FloorSafetyLine_S_0", parent, new Vector3(17.8f, y + 0.014f, 76.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_FloorSafetyLine_N_0", parent, new Vector3(17.8f, y + 0.014f, 105.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_WallDataPlaque_S_0", parent, new Vector3(17.8f, y + 2.25f, 75.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_WallDataPlaque_N_0", parent, new Vector3(17.8f, y + 2.25f, 106.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_FloorSafetyLine_S_1", parent, new Vector3(26.6f, y + 0.014f, 76.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_FloorSafetyLine_N_1", parent, new Vector3(26.6f, y + 0.014f, 105.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_WallDataPlaque_S_1", parent, new Vector3(26.6f, y + 2.25f, 75.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_WallDataPlaque_N_1", parent, new Vector3(26.6f, y + 2.25f, 106.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_FloorSafetyLine_S_2", parent, new Vector3(35.4f, y + 0.014f, 76.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_FloorSafetyLine_N_2", parent, new Vector3(35.4f, y + 0.014f, 105.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_WallDataPlaque_S_2", parent, new Vector3(35.4f, y + 2.25f, 75.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_WallDataPlaque_N_2", parent, new Vector3(35.4f, y + 2.25f, 106.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_FloorSafetyLine_S_3", parent, new Vector3(44.2f, y + 0.014f, 76.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_FloorSafetyLine_N_3", parent, new Vector3(44.2f, y + 0.014f, 105.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_WallDataPlaque_S_3", parent, new Vector3(44.2f, y + 2.25f, 75.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_WallDataPlaque_N_3", parent, new Vector3(44.2f, y + 2.25f, 106.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_AcousticSensor_W_0", parent, new Vector3(15.42f, y + 2.45f, 77.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_AcousticSensorLed_W_0", parent, new Vector3(15.51f, y + 2.48f, 77.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone09_Reactor_AcousticSensor_E_0", parent, new Vector3(46.58f, y + 2.45f, 77.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_AcousticSensorLed_E_0", parent, new Vector3(46.49f, y + 2.48f, 77.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone09_Reactor_AcousticSensor_W_1", parent, new Vector3(15.42f, y + 2.45f, 86.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_AcousticSensorLed_W_1", parent, new Vector3(15.51f, y + 2.48f, 86.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone09_Reactor_AcousticSensor_E_1", parent, new Vector3(46.58f, y + 2.45f, 86.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_AcousticSensorLed_E_1", parent, new Vector3(46.49f, y + 2.48f, 86.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone09_Reactor_AcousticSensor_W_2", parent, new Vector3(15.42f, y + 2.45f, 95.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_AcousticSensorLed_W_2", parent, new Vector3(15.51f, y + 2.48f, 95.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone09_Reactor_AcousticSensor_E_2", parent, new Vector3(46.58f, y + 2.45f, 95.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_AcousticSensorLed_E_2", parent, new Vector3(46.49f, y + 2.48f, 95.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone09_Reactor_AcousticSensor_W_3", parent, new Vector3(15.42f, y + 2.45f, 104.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_AcousticSensorLed_W_3", parent, new Vector3(15.51f, y + 2.48f, 104.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone09_Reactor_AcousticSensor_E_3", parent, new Vector3(46.58f, y + 2.45f, 104.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_AcousticSensorLed_E_3", parent, new Vector3(46.49f, y + 2.48f, 104.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone09_Reactor_DrainStrip_L_0", parent, new Vector3(28.4f, y + 0.012f, 77.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_DrainStrip_R_0", parent, new Vector3(33.6f, y + 0.012f, 77.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallCableClamp_W_0", parent, new Vector3(15.36f, y + 1.15f, 77.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_WallCableClamp_E_0", parent, new Vector3(46.64f, y + 1.15f, 77.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_DrainStrip_L_1", parent, new Vector3(28.4f, y + 0.012f, 86.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_DrainStrip_R_1", parent, new Vector3(33.6f, y + 0.012f, 86.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallCableClamp_W_1", parent, new Vector3(15.36f, y + 1.15f, 86.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_WallCableClamp_E_1", parent, new Vector3(46.64f, y + 1.15f, 86.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_DrainStrip_L_2", parent, new Vector3(28.4f, y + 0.012f, 95.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_DrainStrip_R_2", parent, new Vector3(33.6f, y + 0.012f, 95.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallCableClamp_W_2", parent, new Vector3(15.36f, y + 1.15f, 95.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_WallCableClamp_E_2", parent, new Vector3(46.64f, y + 1.15f, 95.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_DrainStrip_L_3", parent, new Vector3(28.4f, y + 0.012f, 105.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_DrainStrip_R_3", parent, new Vector3(33.6f, y + 0.012f, 105.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallCableClamp_W_3", parent, new Vector3(15.36f, y + 1.15f, 105.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_WallCableClamp_E_3", parent, new Vector3(46.64f, y + 1.15f, 105.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_Baseboard_S_0", parent, new Vector3(17.2f, y + 0.12f, 75.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_Baseboard_N_0", parent, new Vector3(17.2f, y + 0.12f, 106.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingPipe_S_0", parent, new Vector3(17.2f, y + 3.55f, 76.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_CeilingPipe_N_0", parent, new Vector3(17.2f, y + 3.55f, 105.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_Baseboard_S_1", parent, new Vector3(26.4f, y + 0.12f, 75.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_Baseboard_N_1", parent, new Vector3(26.4f, y + 0.12f, 106.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingPipe_S_1", parent, new Vector3(26.4f, y + 3.55f, 76.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_CeilingPipe_N_1", parent, new Vector3(26.4f, y + 3.55f, 105.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_Baseboard_S_2", parent, new Vector3(35.6f, y + 0.12f, 75.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_Baseboard_N_2", parent, new Vector3(35.6f, y + 0.12f, 106.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingPipe_S_2", parent, new Vector3(35.6f, y + 3.55f, 76.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_CeilingPipe_N_2", parent, new Vector3(35.6f, y + 3.55f, 105.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_Baseboard_S_3", parent, new Vector3(44.8f, y + 0.12f, 75.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_Baseboard_N_3", parent, new Vector3(44.8f, y + 0.12f, 106.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingPipe_S_3", parent, new Vector3(44.8f, y + 3.55f, 76.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_CeilingPipe_N_3", parent, new Vector3(44.8f, y + 3.55f, 105.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone09_Reactor_AcousticPanel_W_0", parent, new Vector3(15.36f, y + 1.85f, 77.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone09_Reactor_AcousticPanel_E_0", parent, new Vector3(46.64f, y + 1.85f, 77.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone09_Reactor_StrobeMount_W_0", parent, new Vector3(15.4f, y + 3.05f, 78.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone09_Reactor_StrobeMount_E_0", parent, new Vector3(46.6f, y + 3.05f, 76.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone09_Reactor_AcousticPanel_W_1", parent, new Vector3(15.36f, y + 1.85f, 86.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone09_Reactor_AcousticPanel_E_1", parent, new Vector3(46.64f, y + 1.85f, 86.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone09_Reactor_StrobeMount_W_1", parent, new Vector3(15.4f, y + 3.05f, 87.18f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone09_Reactor_StrobeMount_E_1", parent, new Vector3(46.6f, y + 3.05f, 85.88f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone09_Reactor_AcousticPanel_W_2", parent, new Vector3(15.36f, y + 1.85f, 95.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone09_Reactor_AcousticPanel_E_2", parent, new Vector3(46.64f, y + 1.85f, 95.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone09_Reactor_StrobeMount_W_2", parent, new Vector3(15.4f, y + 3.05f, 96.12f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone09_Reactor_StrobeMount_E_2", parent, new Vector3(46.6f, y + 3.05f, 94.82f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone09_Reactor_AcousticPanel_W_3", parent, new Vector3(15.36f, y + 1.85f, 104.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone09_Reactor_AcousticPanel_E_3", parent, new Vector3(46.64f, y + 1.85f, 104.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone09_Reactor_StrobeMount_W_3", parent, new Vector3(15.4f, y + 3.05f, 105.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone09_Reactor_StrobeMount_E_3", parent, new Vector3(46.6f, y + 3.05f, 103.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone09_Reactor_CeilingVentBox_S_0", parent, new Vector3(17.6f, y + 3.86f, 87.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CeilingVentBox_N_0", parent, new Vector3(17.6f, y + 3.86f, 94.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_VerticalConduit_S_0", parent, new Vector3(17.6f, y + 1.95f, 75.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_VerticalConduit_N_0", parent, new Vector3(17.6f, y + 1.95f, 106.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_CeilingVentBox_S_1", parent, new Vector3(26.53f, y + 3.86f, 87.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CeilingVentBox_N_1", parent, new Vector3(26.53f, y + 3.86f, 94.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_VerticalConduit_S_1", parent, new Vector3(26.53f, y + 1.95f, 75.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_VerticalConduit_N_1", parent, new Vector3(26.53f, y + 1.95f, 106.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_CeilingVentBox_S_2", parent, new Vector3(35.47f, y + 3.86f, 87.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CeilingVentBox_N_2", parent, new Vector3(35.47f, y + 3.86f, 94.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_VerticalConduit_S_2", parent, new Vector3(35.47f, y + 1.95f, 75.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_VerticalConduit_N_2", parent, new Vector3(35.47f, y + 1.95f, 106.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_CeilingVentBox_S_3", parent, new Vector3(44.4f, y + 3.86f, 87.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_CeilingVentBox_N_3", parent, new Vector3(44.4f, y + 3.86f, 94.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_VerticalConduit_S_3", parent, new Vector3(44.4f, y + 1.95f, 75.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_VerticalConduit_N_3", parent, new Vector3(44.4f, y + 1.95f, 106.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_UpperTrim_W_0", parent, new Vector3(15.34f, y + 3.65f, 76.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_UpperTrim_E_0", parent, new Vector3(46.66f, y + 3.65f, 76.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_MidStripe_W_0", parent, new Vector3(15.33f, y + 1.32f, 76.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_MidStripe_E_0", parent, new Vector3(46.67f, y + 1.32f, 76.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_UpperTrim_W_1", parent, new Vector3(15.34f, y + 3.65f, 83.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_UpperTrim_E_1", parent, new Vector3(46.66f, y + 3.65f, 83.7f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_MidStripe_W_1", parent, new Vector3(15.33f, y + 1.32f, 83.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_MidStripe_E_1", parent, new Vector3(46.67f, y + 1.32f, 83.7f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_UpperTrim_W_2", parent, new Vector3(15.34f, y + 3.65f, 91.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_UpperTrim_E_2", parent, new Vector3(46.66f, y + 3.65f, 91.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_MidStripe_W_2", parent, new Vector3(15.33f, y + 1.32f, 91.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_MidStripe_E_2", parent, new Vector3(46.67f, y + 1.32f, 91.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_UpperTrim_W_3", parent, new Vector3(15.34f, y + 3.65f, 98.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_UpperTrim_E_3", parent, new Vector3(46.66f, y + 3.65f, 98.3f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_MidStripe_W_3", parent, new Vector3(15.33f, y + 1.32f, 98.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_MidStripe_E_3", parent, new Vector3(46.67f, y + 1.32f, 98.3f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_UpperTrim_W_4", parent, new Vector3(15.34f, y + 3.65f, 105.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_UpperTrim_E_4", parent, new Vector3(46.66f, y + 3.65f, 105.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone09_Reactor_MidStripe_W_4", parent, new Vector3(15.33f, y + 1.32f, 105.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone09_Reactor_MidStripe_E_4", parent, new Vector3(46.67f, y + 1.32f, 105.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_L_0", parent, new Vector3(27.8f, y + 3.84f, 76.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_R_0", parent, new Vector3(34.2f, y + 3.84f, 76.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_WallJunction_W_0", parent, new Vector3(15.38f, y + 0.78f, 76.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallJunction_E_0", parent, new Vector3(46.62f, y + 0.78f, 76.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_L_1", parent, new Vector3(27.8f, y + 3.84f, 83.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_R_1", parent, new Vector3(34.2f, y + 3.84f, 83.8f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_WallJunction_W_1", parent, new Vector3(15.38f, y + 0.78f, 83.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallJunction_E_1", parent, new Vector3(46.62f, y + 0.78f, 83.8f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_L_2", parent, new Vector3(27.8f, y + 3.84f, 91.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_R_2", parent, new Vector3(34.2f, y + 3.84f, 91.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_WallJunction_W_2", parent, new Vector3(15.38f, y + 0.78f, 91.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallJunction_E_2", parent, new Vector3(46.62f, y + 0.78f, 91.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_L_3", parent, new Vector3(27.8f, y + 3.84f, 98.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_R_3", parent, new Vector3(34.2f, y + 3.84f, 98.2f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_WallJunction_W_3", parent, new Vector3(15.38f, y + 0.78f, 98.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallJunction_E_3", parent, new Vector3(46.62f, y + 0.78f, 98.2f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_L_4", parent, new Vector3(27.8f, y + 3.84f, 105.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone09_Reactor_Sprinkler_R_4", parent, new Vector3(34.2f, y + 3.84f, 105.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_WallJunction_W_4", parent, new Vector3(15.38f, y + 0.78f, 105.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallJunction_E_4", parent, new Vector3(46.62f, y + 0.78f, 105.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_FloorJoint_X_0", parent, new Vector3(17.0f, y + 0.008f, 91.0f), new Vector3(0.05f, 0.01f, 29.0f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingStrut_S_0", parent, new Vector3(17.0f, y + 3.74f, 77.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingStrut_N_0", parent, new Vector3(17.0f, y + 3.74f, 104.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallBracket_S_0", parent, new Vector3(17.0f, y + 2.85f, 75.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_WallBracket_N_0", parent, new Vector3(17.0f, y + 2.85f, 106.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_FloorJoint_X_1", parent, new Vector3(26.33f, y + 0.008f, 91.0f), new Vector3(0.05f, 0.01f, 29.0f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingStrut_S_1", parent, new Vector3(26.33f, y + 3.74f, 77.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingStrut_N_1", parent, new Vector3(26.33f, y + 3.74f, 104.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallBracket_S_1", parent, new Vector3(26.33f, y + 2.85f, 75.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_WallBracket_N_1", parent, new Vector3(26.33f, y + 2.85f, 106.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_FloorJoint_X_2", parent, new Vector3(35.67f, y + 0.008f, 91.0f), new Vector3(0.05f, 0.01f, 29.0f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingStrut_S_2", parent, new Vector3(35.67f, y + 3.74f, 77.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingStrut_N_2", parent, new Vector3(35.67f, y + 3.74f, 104.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallBracket_S_2", parent, new Vector3(35.67f, y + 2.85f, 75.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_WallBracket_N_2", parent, new Vector3(35.67f, y + 2.85f, 106.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_FloorJoint_X_3", parent, new Vector3(45.0f, y + 0.008f, 91.0f), new Vector3(0.05f, 0.01f, 29.0f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingStrut_S_3", parent, new Vector3(45.0f, y + 3.74f, 77.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_CeilingStrut_N_3", parent, new Vector3(45.0f, y + 3.74f, 104.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_WallBracket_S_3", parent, new Vector3(45.0f, y + 2.85f, 75.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_WallBracket_N_3", parent, new Vector3(45.0f, y + 2.85f, 106.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_LowerPipe_W_0", parent, new Vector3(15.35f, y + 0.55f, 76.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_LowerPipe_E_0", parent, new Vector3(46.65f, y + 0.55f, 76.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_HighPipe_W_0", parent, new Vector3(15.35f, y + 3.38f, 76.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_HighPipe_E_0", parent, new Vector3(46.65f, y + 3.38f, 76.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_LowerPipe_W_1", parent, new Vector3(15.35f, y + 0.55f, 83.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_LowerPipe_E_1", parent, new Vector3(46.65f, y + 0.55f, 83.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_HighPipe_W_1", parent, new Vector3(15.35f, y + 3.38f, 83.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_HighPipe_E_1", parent, new Vector3(46.65f, y + 3.38f, 83.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_LowerPipe_W_2", parent, new Vector3(15.35f, y + 0.55f, 91.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_LowerPipe_E_2", parent, new Vector3(46.65f, y + 0.55f, 91.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_HighPipe_W_2", parent, new Vector3(15.35f, y + 3.38f, 91.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_HighPipe_E_2", parent, new Vector3(46.65f, y + 3.38f, 91.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_LowerPipe_W_3", parent, new Vector3(15.35f, y + 0.55f, 98.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_LowerPipe_E_3", parent, new Vector3(46.65f, y + 0.55f, 98.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_HighPipe_W_3", parent, new Vector3(15.35f, y + 3.38f, 98.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_HighPipe_E_3", parent, new Vector3(46.65f, y + 3.38f, 98.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_LowerPipe_W_4", parent, new Vector3(15.35f, y + 0.55f, 105.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_LowerPipe_E_4", parent, new Vector3(46.65f, y + 0.55f, 105.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone09_Reactor_HighPipe_W_4", parent, new Vector3(15.35f, y + 3.38f, 105.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_HighPipe_E_4", parent, new Vector3(46.65f, y + 3.38f, 105.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone09_Reactor_DuctCollar_C_0", parent, new Vector3(31.0f, y + 3.76f, 76.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_W_0", parent, new Vector3(15.38f, y + 1.58f, 76.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_E_0", parent, new Vector3(46.62f, y + 1.58f, 76.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_W_0", parent, new Vector3(15.65f, y + 0.01f, 76.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_E_0", parent, new Vector3(46.35f, y + 0.01f, 76.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_DuctCollar_C_1", parent, new Vector3(31.0f, y + 3.76f, 83.9f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_W_1", parent, new Vector3(15.38f, y + 1.58f, 83.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_E_1", parent, new Vector3(46.62f, y + 1.58f, 83.9f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_W_1", parent, new Vector3(15.65f, y + 0.01f, 83.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_E_1", parent, new Vector3(46.35f, y + 0.01f, 83.9f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_DuctCollar_C_2", parent, new Vector3(31.0f, y + 3.76f, 91.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_W_2", parent, new Vector3(15.38f, y + 1.58f, 91.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_E_2", parent, new Vector3(46.62f, y + 1.58f, 91.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_W_2", parent, new Vector3(15.65f, y + 0.01f, 91.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_E_2", parent, new Vector3(46.35f, y + 0.01f, 91.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_DuctCollar_C_3", parent, new Vector3(31.0f, y + 3.76f, 98.1f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_W_3", parent, new Vector3(15.38f, y + 1.58f, 98.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_E_3", parent, new Vector3(46.62f, y + 1.58f, 98.1f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_W_3", parent, new Vector3(15.65f, y + 0.01f, 98.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_E_3", parent, new Vector3(46.35f, y + 0.01f, 98.1f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_DuctCollar_C_4", parent, new Vector3(31.0f, y + 3.76f, 105.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_W_4", parent, new Vector3(15.38f, y + 1.58f, 105.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_TelemetryNode_E_4", parent, new Vector3(46.62f, y + 1.58f, 105.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_W_4", parent, new Vector3(15.65f, y + 0.01f, 105.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_FloorBoltPlate_E_4", parent, new Vector3(46.35f, y + 0.01f, 105.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone09_Reactor_InspHatch_L_0", parent, new Vector3(25.8f, y + 3.88f, 76.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_InspHatch_R_0", parent, new Vector3(36.2f, y + 3.88f, 76.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_RailClamp_W_0", parent, new Vector3(15.36f, y + 2.15f, 76.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_RailClamp_E_0", parent, new Vector3(46.64f, y + 2.15f, 76.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_InspHatch_L_1", parent, new Vector3(25.8f, y + 3.88f, 83.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_InspHatch_R_1", parent, new Vector3(36.2f, y + 3.88f, 83.85f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_RailClamp_W_1", parent, new Vector3(15.36f, y + 2.15f, 83.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_RailClamp_E_1", parent, new Vector3(46.64f, y + 2.15f, 83.85f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_InspHatch_L_2", parent, new Vector3(25.8f, y + 3.88f, 91.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_InspHatch_R_2", parent, new Vector3(36.2f, y + 3.88f, 91.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_RailClamp_W_2", parent, new Vector3(15.36f, y + 2.15f, 91.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_RailClamp_E_2", parent, new Vector3(46.64f, y + 2.15f, 91.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_InspHatch_L_3", parent, new Vector3(25.8f, y + 3.88f, 98.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_InspHatch_R_3", parent, new Vector3(36.2f, y + 3.88f, 98.15f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_RailClamp_W_3", parent, new Vector3(15.36f, y + 2.15f, 98.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_RailClamp_E_3", parent, new Vector3(46.64f, y + 2.15f, 98.15f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_InspHatch_L_4", parent, new Vector3(25.8f, y + 3.88f, 105.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_InspHatch_R_4", parent, new Vector3(36.2f, y + 3.88f, 105.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone09_Reactor_RailClamp_W_4", parent, new Vector3(15.36f, y + 2.15f, 105.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone09_Reactor_RailClamp_E_4", parent, new Vector3(46.64f, y + 2.15f, 105.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone10_CentralCore(Transform parent)
    {
        float y = MainY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone10_CentralCore
        BuildFireExtinguisherCabinet("Zone10_CentralCore_FireCab_W_0", parent, new Vector3(-18.45f, y + 1.45f, 111.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone10_CentralCore_FireCab_E_0", parent, new Vector3(18.45f, y + 1.45f, 111.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone10_CentralCore_FireCab_W_1", parent, new Vector3(-18.45f, y + 1.45f, 122.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone10_CentralCore_FireCab_E_1", parent, new Vector3(18.45f, y + 1.45f, 122.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone10_CentralCore_FireCab_W_2", parent, new Vector3(-18.45f, y + 1.45f, 132.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone10_CentralCore_FireCab_E_2", parent, new Vector3(18.45f, y + 1.45f, 132.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone10_CentralCore_CCTV_0", parent, new Vector3(-18.55f, y + 3.35f, 109.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone10_CentralCore_CCTV_1", parent, new Vector3(18.55f, y + 3.35f, 109.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone10_CentralCore_CCTV_2", parent, new Vector3(-18.55f, y + 3.35f, 134.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone10_CentralCore_CCTV_3", parent, new Vector3(18.55f, y + 3.35f, 134.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone10_CentralCore_CCTV_4", parent, new Vector3(-3.5f, y + 3.35f, 109.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone10_CentralCore_CCTV_5", parent, new Vector3(3.5f, y + 3.35f, 134.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone10_CentralCore_Intercom_W_0", parent, new Vector3(-18.58f, y + 1.52f, 112.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone10_CentralCore_Intercom_E_0", parent, new Vector3(18.58f, y + 1.52f, 112.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone10_CentralCore_Clock_W_0", parent, new Vector3(-18.6f, y + 2.85f, 112.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone10_CentralCore_Clock_E_0", parent, new Vector3(18.6f, y + 2.85f, 112.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone10_CentralCore_Intercom_W_1", parent, new Vector3(-18.58f, y + 1.52f, 118.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone10_CentralCore_Intercom_E_1", parent, new Vector3(18.58f, y + 1.52f, 118.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone10_CentralCore_Intercom_W_2", parent, new Vector3(-18.58f, y + 1.52f, 125.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone10_CentralCore_Intercom_E_2", parent, new Vector3(18.58f, y + 1.52f, 125.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone10_CentralCore_Clock_W_2", parent, new Vector3(-18.6f, y + 2.85f, 125.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone10_CentralCore_Clock_E_2", parent, new Vector3(18.6f, y + 2.85f, 125.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone10_CentralCore_Intercom_W_3", parent, new Vector3(-18.58f, y + 1.52f, 132.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone10_CentralCore_Intercom_E_3", parent, new Vector3(18.58f, y + 1.52f, 132.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_W_0", parent, new Vector3(-18.55f, y, 111.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_E_0", parent, new Vector3(18.55f, y, 111.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_W_1", parent, new Vector3(-18.55f, y, 118.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_E_1", parent, new Vector3(18.55f, y, 118.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_W_2", parent, new Vector3(-18.55f, y, 125.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_E_2", parent, new Vector3(18.55f, y, 125.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_W_3", parent, new Vector3(-18.55f, y, 133.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_E_3", parent, new Vector3(18.55f, y, 133.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_W_4", parent, new Vector3(-18.55f, y, 140.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_E_4", parent, new Vector3(18.55f, y, 140.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_W_5", parent, new Vector3(-18.55f, y, 147.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone10_CentralCore_Radiator_E_5", parent, new Vector3(18.55f, y, 147.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_W_0", parent, new Vector3(-18.58f, y + 3.15f, 111.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_E_0", parent, new Vector3(18.58f, y + 3.15f, 111.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_W_0", parent, new Vector3(-17.75f, y + 3.45f, 111.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_E_0", parent, new Vector3(17.75f, y + 3.45f, 111.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_W_1", parent, new Vector3(-18.58f, y + 3.15f, 118.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_E_1", parent, new Vector3(18.58f, y + 3.15f, 118.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_W_1", parent, new Vector3(-17.75f, y + 3.45f, 118.4f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_E_1", parent, new Vector3(17.75f, y + 3.45f, 118.4f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_W_2", parent, new Vector3(-18.58f, y + 3.15f, 125.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_E_2", parent, new Vector3(18.58f, y + 3.15f, 125.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_W_2", parent, new Vector3(-17.75f, y + 3.45f, 125.6f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_E_2", parent, new Vector3(17.75f, y + 3.45f, 125.6f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_W_3", parent, new Vector3(-18.58f, y + 3.15f, 132.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_E_3", parent, new Vector3(18.58f, y + 3.15f, 132.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_W_3", parent, new Vector3(-17.75f, y + 3.45f, 132.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_E_3", parent, new Vector3(17.75f, y + 3.45f, 132.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_W_4", parent, new Vector3(-18.58f, y + 3.15f, 140.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_E_4", parent, new Vector3(18.58f, y + 3.15f, 140.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_W_4", parent, new Vector3(-17.75f, y + 3.45f, 140.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_E_4", parent, new Vector3(17.75f, y + 3.45f, 140.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_W_5", parent, new Vector3(-18.58f, y + 3.15f, 147.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone10_CentralCore_Vent_E_5", parent, new Vector3(18.58f, y + 3.15f, 147.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_W_5", parent, new Vector3(-17.75f, y + 3.45f, 147.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone10_CentralCore_PA_E_5", parent, new Vector3(17.75f, y + 3.45f, 147.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone10_CentralCore_ValveManifold_S_0", parent, new Vector3(-16.0f, y, 109.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone10_CentralCore_ValveManifold_N_0", parent, new Vector3(-16.0f, y, 134.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone10_CentralCore_FuseBox_S_0", parent, new Vector3(-14.8f, y + 1.65f, 109.45f), Quaternion.identity);
        BuildWallFuseBox("Zone10_CentralCore_FuseBox_N_0", parent, new Vector3(-17.2f, y + 1.65f, 134.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone10_CentralCore_ValveManifold_S_1", parent, new Vector3(-5.33f, y, 109.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone10_CentralCore_ValveManifold_N_1", parent, new Vector3(-5.33f, y, 134.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone10_CentralCore_FuseBox_S_1", parent, new Vector3(-4.13f, y + 1.65f, 109.45f), Quaternion.identity);
        BuildWallFuseBox("Zone10_CentralCore_FuseBox_N_1", parent, new Vector3(-6.53f, y + 1.65f, 134.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone10_CentralCore_ValveManifold_S_2", parent, new Vector3(5.33f, y, 109.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone10_CentralCore_ValveManifold_N_2", parent, new Vector3(5.33f, y, 134.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone10_CentralCore_FuseBox_S_2", parent, new Vector3(6.53f, y + 1.65f, 109.45f), Quaternion.identity);
        BuildWallFuseBox("Zone10_CentralCore_FuseBox_N_2", parent, new Vector3(4.13f, y + 1.65f, 134.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone10_CentralCore_ValveManifold_S_3", parent, new Vector3(16.0f, y, 109.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone10_CentralCore_ValveManifold_N_3", parent, new Vector3(16.0f, y, 134.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone10_CentralCore_FuseBox_S_3", parent, new Vector3(17.2f, y + 1.65f, 109.45f), Quaternion.identity);
        BuildWallFuseBox("Zone10_CentralCore_FuseBox_N_3", parent, new Vector3(14.8f, y + 1.65f, 134.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_L_0", parent, new Vector3(-4.2f, y + 3.48f, 112.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_R_0", parent, new Vector3(4.2f, y + 3.48f, 112.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_L_1", parent, new Vector3(-4.2f, y + 3.48f, 117.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_R_1", parent, new Vector3(4.2f, y + 3.48f, 117.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_L_2", parent, new Vector3(-4.2f, y + 3.48f, 122.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_R_2", parent, new Vector3(4.2f, y + 3.48f, 122.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_L_3", parent, new Vector3(-4.2f, y + 3.48f, 127.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_R_3", parent, new Vector3(4.2f, y + 3.48f, 127.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_L_4", parent, new Vector3(-4.2f, y + 3.48f, 132.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone10_CentralCore_Baffle_R_4", parent, new Vector3(4.2f, y + 3.48f, 132.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone10_CentralCore_Crates_SW", parent, new Vector3(-17.4f, y, 110.6f), 14f);
        BuildCableSpoolAndMat("Zone10_CentralCore_Spool_SE", parent, new Vector3(17.4f, y, 110.6f), -28f);
        BuildHazmatDrumCluster("Zone10_CentralCore_Drums_NW", parent, new Vector3(-17.4f, y, 133.4f));
        BuildPatchPanelRack("Zone10_CentralCore_PatchRack_NE", parent, new Vector3(17.4f, y, 133.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone10_CentralCore_CheckStation_S_0", parent, new Vector3(-16.6f, y, 110.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone10_CentralCore_Bollard_S_0", parent, new Vector3(-17.45f, y, 109.95f));
        BuildWallWarningBollard("Zone10_CentralCore_Bollard_N_0", parent, new Vector3(-15.75f, y, 134.05f));
        BuildClipboardCheckStation("Zone10_CentralCore_CheckStation_S_1", parent, new Vector3(-5.53f, y, 110.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone10_CentralCore_Bollard_S_1", parent, new Vector3(-6.38f, y, 109.95f));
        BuildWallWarningBollard("Zone10_CentralCore_Bollard_N_1", parent, new Vector3(-4.68f, y, 134.05f));
        BuildClipboardCheckStation("Zone10_CentralCore_CheckStation_S_2", parent, new Vector3(5.53f, y, 110.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone10_CentralCore_Bollard_S_2", parent, new Vector3(4.68f, y, 109.95f));
        BuildWallWarningBollard("Zone10_CentralCore_Bollard_N_2", parent, new Vector3(6.38f, y, 134.05f));
        BuildClipboardCheckStation("Zone10_CentralCore_CheckStation_S_3", parent, new Vector3(16.6f, y, 110.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone10_CentralCore_Bollard_S_3", parent, new Vector3(15.75f, y, 109.95f));
        BuildWallWarningBollard("Zone10_CentralCore_Bollard_N_3", parent, new Vector3(17.45f, y, 134.05f));
        CreateAcousticBox("Zone10_CentralCore_Pilaster_W_0", parent, new Vector3(-18.75f, y + 2.0f, 110.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone10_CentralCore_Pilaster_E_0", parent, new Vector3(18.75f, y + 2.0f, 110.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_W_0", parent, new Vector3(-18.62f, y + 0.32f, 110.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_E_0", parent, new Vector3(18.62f, y + 0.32f, 110.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone10_CentralCore_Pilaster_W_1", parent, new Vector3(-18.75f, y + 2.0f, 116.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone10_CentralCore_Pilaster_E_1", parent, new Vector3(18.75f, y + 2.0f, 116.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_W_1", parent, new Vector3(-18.62f, y + 0.32f, 116.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_E_1", parent, new Vector3(18.62f, y + 0.32f, 116.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone10_CentralCore_Pilaster_W_2", parent, new Vector3(-18.75f, y + 2.0f, 122.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone10_CentralCore_Pilaster_E_2", parent, new Vector3(18.75f, y + 2.0f, 122.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_W_2", parent, new Vector3(-18.62f, y + 0.32f, 122.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_E_2", parent, new Vector3(18.62f, y + 0.32f, 122.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone10_CentralCore_Pilaster_W_3", parent, new Vector3(-18.75f, y + 2.0f, 127.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone10_CentralCore_Pilaster_E_3", parent, new Vector3(18.75f, y + 2.0f, 127.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_W_3", parent, new Vector3(-18.62f, y + 0.32f, 127.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_E_3", parent, new Vector3(18.62f, y + 0.32f, 127.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone10_CentralCore_Pilaster_W_4", parent, new Vector3(-18.75f, y + 2.0f, 133.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone10_CentralCore_Pilaster_E_4", parent, new Vector3(18.75f, y + 2.0f, 133.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_W_4", parent, new Vector3(-18.62f, y + 0.32f, 133.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_ConduitTrim_E_4", parent, new Vector3(18.62f, y + 0.32f, 133.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingRib_0", parent, new Vector3(0.0f, y + 3.82f, 110.5f), new Vector3(36.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_L_0", parent, new Vector3(-10.3f, y + 3.68f, 110.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_R_0", parent, new Vector3(10.3f, y + 3.68f, 110.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CeilingRib_1", parent, new Vector3(0.0f, y + 3.82f, 116.25f), new Vector3(36.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_L_1", parent, new Vector3(-10.3f, y + 3.68f, 116.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_R_1", parent, new Vector3(10.3f, y + 3.68f, 116.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CeilingRib_2", parent, new Vector3(0.0f, y + 3.82f, 122.0f), new Vector3(36.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_L_2", parent, new Vector3(-10.3f, y + 3.68f, 122.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_R_2", parent, new Vector3(10.3f, y + 3.68f, 122.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CeilingRib_3", parent, new Vector3(0.0f, y + 3.82f, 127.75f), new Vector3(36.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_L_3", parent, new Vector3(-10.3f, y + 3.68f, 127.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_R_3", parent, new Vector3(10.3f, y + 3.68f, 127.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CeilingRib_4", parent, new Vector3(0.0f, y + 3.82f, 133.5f), new Vector3(36.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_L_4", parent, new Vector3(-10.3f, y + 3.68f, 133.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CableTrayHanger_R_4", parent, new Vector3(10.3f, y + 3.68f, 133.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_FloorSafetyLine_S_0", parent, new Vector3(-16.2f, y + 0.014f, 110.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_FloorSafetyLine_N_0", parent, new Vector3(-16.2f, y + 0.014f, 133.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_WallDataPlaque_S_0", parent, new Vector3(-16.2f, y + 2.25f, 109.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_WallDataPlaque_N_0", parent, new Vector3(-16.2f, y + 2.25f, 134.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_FloorSafetyLine_S_1", parent, new Vector3(-5.4f, y + 0.014f, 110.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_FloorSafetyLine_N_1", parent, new Vector3(-5.4f, y + 0.014f, 133.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_WallDataPlaque_S_1", parent, new Vector3(-5.4f, y + 2.25f, 109.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_WallDataPlaque_N_1", parent, new Vector3(-5.4f, y + 2.25f, 134.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_FloorSafetyLine_S_2", parent, new Vector3(5.4f, y + 0.014f, 110.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_FloorSafetyLine_N_2", parent, new Vector3(5.4f, y + 0.014f, 133.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_WallDataPlaque_S_2", parent, new Vector3(5.4f, y + 2.25f, 109.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_WallDataPlaque_N_2", parent, new Vector3(5.4f, y + 2.25f, 134.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_FloorSafetyLine_S_3", parent, new Vector3(16.2f, y + 0.014f, 110.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_FloorSafetyLine_N_3", parent, new Vector3(16.2f, y + 0.014f, 133.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_WallDataPlaque_S_3", parent, new Vector3(16.2f, y + 2.25f, 109.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_WallDataPlaque_N_3", parent, new Vector3(16.2f, y + 2.25f, 134.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_AcousticSensor_W_0", parent, new Vector3(-18.58f, y + 2.45f, 111.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_AcousticSensorLed_W_0", parent, new Vector3(-18.49f, y + 2.48f, 111.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone10_CentralCore_AcousticSensor_E_0", parent, new Vector3(18.58f, y + 2.45f, 111.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_AcousticSensorLed_E_0", parent, new Vector3(18.49f, y + 2.48f, 111.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone10_CentralCore_AcousticSensor_W_1", parent, new Vector3(-18.58f, y + 2.45f, 118.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_AcousticSensorLed_W_1", parent, new Vector3(-18.49f, y + 2.48f, 118.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone10_CentralCore_AcousticSensor_E_1", parent, new Vector3(18.58f, y + 2.45f, 118.47f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_AcousticSensorLed_E_1", parent, new Vector3(18.49f, y + 2.48f, 118.47f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone10_CentralCore_AcousticSensor_W_2", parent, new Vector3(-18.58f, y + 2.45f, 125.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_AcousticSensorLed_W_2", parent, new Vector3(-18.49f, y + 2.48f, 125.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone10_CentralCore_AcousticSensor_E_2", parent, new Vector3(18.58f, y + 2.45f, 125.53f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_AcousticSensorLed_E_2", parent, new Vector3(18.49f, y + 2.48f, 125.53f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone10_CentralCore_AcousticSensor_W_3", parent, new Vector3(-18.58f, y + 2.45f, 132.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_AcousticSensorLed_W_3", parent, new Vector3(-18.49f, y + 2.48f, 132.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone10_CentralCore_AcousticSensor_E_3", parent, new Vector3(18.58f, y + 2.45f, 132.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_AcousticSensorLed_E_3", parent, new Vector3(18.49f, y + 2.48f, 132.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone10_CentralCore_DrainStrip_L_0", parent, new Vector3(-2.6f, y + 0.012f, 111.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_DrainStrip_R_0", parent, new Vector3(2.6f, y + 0.012f, 111.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallCableClamp_W_0", parent, new Vector3(-18.64f, y + 1.15f, 111.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_WallCableClamp_E_0", parent, new Vector3(18.64f, y + 1.15f, 111.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_DrainStrip_L_1", parent, new Vector3(-2.6f, y + 0.012f, 118.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_DrainStrip_R_1", parent, new Vector3(2.6f, y + 0.012f, 118.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallCableClamp_W_1", parent, new Vector3(-18.64f, y + 1.15f, 118.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_WallCableClamp_E_1", parent, new Vector3(18.64f, y + 1.15f, 118.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_DrainStrip_L_2", parent, new Vector3(-2.6f, y + 0.012f, 125.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_DrainStrip_R_2", parent, new Vector3(2.6f, y + 0.012f, 125.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallCableClamp_W_2", parent, new Vector3(-18.64f, y + 1.15f, 125.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_WallCableClamp_E_2", parent, new Vector3(18.64f, y + 1.15f, 125.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_DrainStrip_L_3", parent, new Vector3(-2.6f, y + 0.012f, 133.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_DrainStrip_R_3", parent, new Vector3(2.6f, y + 0.012f, 133.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallCableClamp_W_3", parent, new Vector3(-18.64f, y + 1.15f, 133.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_WallCableClamp_E_3", parent, new Vector3(18.64f, y + 1.15f, 133.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_Baseboard_S_0", parent, new Vector3(-16.8f, y + 0.12f, 109.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_Baseboard_N_0", parent, new Vector3(-16.8f, y + 0.12f, 134.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingPipe_S_0", parent, new Vector3(-16.8f, y + 3.55f, 110.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_CeilingPipe_N_0", parent, new Vector3(-16.8f, y + 3.55f, 133.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_Baseboard_S_1", parent, new Vector3(-5.6f, y + 0.12f, 109.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_Baseboard_N_1", parent, new Vector3(-5.6f, y + 0.12f, 134.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingPipe_S_1", parent, new Vector3(-5.6f, y + 3.55f, 110.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_CeilingPipe_N_1", parent, new Vector3(-5.6f, y + 3.55f, 133.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_Baseboard_S_2", parent, new Vector3(5.6f, y + 0.12f, 109.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_Baseboard_N_2", parent, new Vector3(5.6f, y + 0.12f, 134.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingPipe_S_2", parent, new Vector3(5.6f, y + 3.55f, 110.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_CeilingPipe_N_2", parent, new Vector3(5.6f, y + 3.55f, 133.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_Baseboard_S_3", parent, new Vector3(16.8f, y + 0.12f, 109.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_Baseboard_N_3", parent, new Vector3(16.8f, y + 0.12f, 134.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingPipe_S_3", parent, new Vector3(16.8f, y + 3.55f, 110.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_CeilingPipe_N_3", parent, new Vector3(16.8f, y + 3.55f, 133.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone10_CentralCore_AcousticPanel_W_0", parent, new Vector3(-18.64f, y + 1.85f, 111.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone10_CentralCore_AcousticPanel_E_0", parent, new Vector3(18.64f, y + 1.85f, 111.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone10_CentralCore_StrobeMount_W_0", parent, new Vector3(-18.6f, y + 3.05f, 112.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone10_CentralCore_StrobeMount_E_0", parent, new Vector3(18.6f, y + 3.05f, 110.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone10_CentralCore_AcousticPanel_W_1", parent, new Vector3(-18.64f, y + 1.85f, 118.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone10_CentralCore_AcousticPanel_E_1", parent, new Vector3(18.64f, y + 1.85f, 118.53f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone10_CentralCore_StrobeMount_W_1", parent, new Vector3(-18.6f, y + 3.05f, 119.18f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone10_CentralCore_StrobeMount_E_1", parent, new Vector3(18.6f, y + 3.05f, 117.88f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone10_CentralCore_AcousticPanel_W_2", parent, new Vector3(-18.64f, y + 1.85f, 125.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone10_CentralCore_AcousticPanel_E_2", parent, new Vector3(18.64f, y + 1.85f, 125.47f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone10_CentralCore_StrobeMount_W_2", parent, new Vector3(-18.6f, y + 3.05f, 126.12f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone10_CentralCore_StrobeMount_E_2", parent, new Vector3(18.6f, y + 3.05f, 124.82f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone10_CentralCore_AcousticPanel_W_3", parent, new Vector3(-18.64f, y + 1.85f, 132.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone10_CentralCore_AcousticPanel_E_3", parent, new Vector3(18.64f, y + 1.85f, 132.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone10_CentralCore_StrobeMount_W_3", parent, new Vector3(-18.6f, y + 3.05f, 133.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone10_CentralCore_StrobeMount_E_3", parent, new Vector3(18.6f, y + 3.05f, 131.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone10_CentralCore_CeilingVentBox_S_0", parent, new Vector3(-16.4f, y + 3.86f, 118.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CeilingVentBox_N_0", parent, new Vector3(-16.4f, y + 3.86f, 125.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_VerticalConduit_S_0", parent, new Vector3(-16.4f, y + 1.95f, 109.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_VerticalConduit_N_0", parent, new Vector3(-16.4f, y + 1.95f, 134.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_CeilingVentBox_S_1", parent, new Vector3(-5.47f, y + 3.86f, 118.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CeilingVentBox_N_1", parent, new Vector3(-5.47f, y + 3.86f, 125.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_VerticalConduit_S_1", parent, new Vector3(-5.47f, y + 1.95f, 109.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_VerticalConduit_N_1", parent, new Vector3(-5.47f, y + 1.95f, 134.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_CeilingVentBox_S_2", parent, new Vector3(5.47f, y + 3.86f, 118.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CeilingVentBox_N_2", parent, new Vector3(5.47f, y + 3.86f, 125.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_VerticalConduit_S_2", parent, new Vector3(5.47f, y + 1.95f, 109.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_VerticalConduit_N_2", parent, new Vector3(5.47f, y + 1.95f, 134.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_CeilingVentBox_S_3", parent, new Vector3(16.4f, y + 3.86f, 118.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_CeilingVentBox_N_3", parent, new Vector3(16.4f, y + 3.86f, 125.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_VerticalConduit_S_3", parent, new Vector3(16.4f, y + 1.95f, 109.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_VerticalConduit_N_3", parent, new Vector3(16.4f, y + 1.95f, 134.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_W_0", parent, new Vector3(-18.66f, y + 3.65f, 110.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_E_0", parent, new Vector3(18.66f, y + 3.65f, 110.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_MidStripe_W_0", parent, new Vector3(-18.67f, y + 1.32f, 110.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_MidStripe_E_0", parent, new Vector3(18.67f, y + 1.32f, 110.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_W_1", parent, new Vector3(-18.66f, y + 3.65f, 116.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_E_1", parent, new Vector3(18.66f, y + 3.65f, 116.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_MidStripe_W_1", parent, new Vector3(-18.67f, y + 1.32f, 116.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_MidStripe_E_1", parent, new Vector3(18.67f, y + 1.32f, 116.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_W_2", parent, new Vector3(-18.66f, y + 3.65f, 122.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_E_2", parent, new Vector3(18.66f, y + 3.65f, 122.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_MidStripe_W_2", parent, new Vector3(-18.67f, y + 1.32f, 122.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_MidStripe_E_2", parent, new Vector3(18.67f, y + 1.32f, 122.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_W_3", parent, new Vector3(-18.66f, y + 3.65f, 127.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_E_3", parent, new Vector3(18.66f, y + 3.65f, 127.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_MidStripe_W_3", parent, new Vector3(-18.67f, y + 1.32f, 127.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_MidStripe_E_3", parent, new Vector3(18.67f, y + 1.32f, 127.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_W_4", parent, new Vector3(-18.66f, y + 3.65f, 133.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_UpperTrim_E_4", parent, new Vector3(18.66f, y + 3.65f, 133.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone10_CentralCore_MidStripe_W_4", parent, new Vector3(-18.67f, y + 1.32f, 133.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone10_CentralCore_MidStripe_E_4", parent, new Vector3(18.67f, y + 1.32f, 133.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_L_0", parent, new Vector3(-3.2f, y + 3.84f, 110.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_R_0", parent, new Vector3(3.2f, y + 3.84f, 110.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_WallJunction_W_0", parent, new Vector3(-18.62f, y + 0.78f, 110.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallJunction_E_0", parent, new Vector3(18.62f, y + 0.78f, 110.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_L_1", parent, new Vector3(-3.2f, y + 3.84f, 116.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_R_1", parent, new Vector3(3.2f, y + 3.84f, 116.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_WallJunction_W_1", parent, new Vector3(-18.62f, y + 0.78f, 116.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallJunction_E_1", parent, new Vector3(18.62f, y + 0.78f, 116.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_L_2", parent, new Vector3(-3.2f, y + 3.84f, 122.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_R_2", parent, new Vector3(3.2f, y + 3.84f, 122.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_WallJunction_W_2", parent, new Vector3(-18.62f, y + 0.78f, 122.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallJunction_E_2", parent, new Vector3(18.62f, y + 0.78f, 122.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_L_3", parent, new Vector3(-3.2f, y + 3.84f, 127.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_R_3", parent, new Vector3(3.2f, y + 3.84f, 127.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_WallJunction_W_3", parent, new Vector3(-18.62f, y + 0.78f, 127.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallJunction_E_3", parent, new Vector3(18.62f, y + 0.78f, 127.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_L_4", parent, new Vector3(-3.2f, y + 3.84f, 133.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone10_CentralCore_Sprinkler_R_4", parent, new Vector3(3.2f, y + 3.84f, 133.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_WallJunction_W_4", parent, new Vector3(-18.62f, y + 0.78f, 133.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallJunction_E_4", parent, new Vector3(18.62f, y + 0.78f, 133.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_FloorJoint_X_0", parent, new Vector3(-17.0f, y + 0.008f, 122.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingStrut_S_0", parent, new Vector3(-17.0f, y + 3.74f, 111.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingStrut_N_0", parent, new Vector3(-17.0f, y + 3.74f, 132.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallBracket_S_0", parent, new Vector3(-17.0f, y + 2.85f, 109.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_WallBracket_N_0", parent, new Vector3(-17.0f, y + 2.85f, 134.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_FloorJoint_X_1", parent, new Vector3(-5.67f, y + 0.008f, 122.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingStrut_S_1", parent, new Vector3(-5.67f, y + 3.74f, 111.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingStrut_N_1", parent, new Vector3(-5.67f, y + 3.74f, 132.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallBracket_S_1", parent, new Vector3(-5.67f, y + 2.85f, 109.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_WallBracket_N_1", parent, new Vector3(-5.67f, y + 2.85f, 134.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_FloorJoint_X_2", parent, new Vector3(5.67f, y + 0.008f, 122.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingStrut_S_2", parent, new Vector3(5.67f, y + 3.74f, 111.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingStrut_N_2", parent, new Vector3(5.67f, y + 3.74f, 132.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallBracket_S_2", parent, new Vector3(5.67f, y + 2.85f, 109.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_WallBracket_N_2", parent, new Vector3(5.67f, y + 2.85f, 134.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_FloorJoint_X_3", parent, new Vector3(17.0f, y + 0.008f, 122.0f), new Vector3(0.05f, 0.01f, 23.0f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingStrut_S_3", parent, new Vector3(17.0f, y + 3.74f, 111.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_CeilingStrut_N_3", parent, new Vector3(17.0f, y + 3.74f, 132.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_WallBracket_S_3", parent, new Vector3(17.0f, y + 2.85f, 109.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_WallBracket_N_3", parent, new Vector3(17.0f, y + 2.85f, 134.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_W_0", parent, new Vector3(-18.65f, y + 0.55f, 110.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_E_0", parent, new Vector3(18.65f, y + 0.55f, 110.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_HighPipe_W_0", parent, new Vector3(-18.65f, y + 3.38f, 110.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_HighPipe_E_0", parent, new Vector3(18.65f, y + 3.38f, 110.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_W_1", parent, new Vector3(-18.65f, y + 0.55f, 116.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_E_1", parent, new Vector3(18.65f, y + 0.55f, 116.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_HighPipe_W_1", parent, new Vector3(-18.65f, y + 3.38f, 116.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_HighPipe_E_1", parent, new Vector3(18.65f, y + 3.38f, 116.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_W_2", parent, new Vector3(-18.65f, y + 0.55f, 122.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_E_2", parent, new Vector3(18.65f, y + 0.55f, 122.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_HighPipe_W_2", parent, new Vector3(-18.65f, y + 3.38f, 122.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_HighPipe_E_2", parent, new Vector3(18.65f, y + 3.38f, 122.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_W_3", parent, new Vector3(-18.65f, y + 0.55f, 127.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_E_3", parent, new Vector3(18.65f, y + 0.55f, 127.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_HighPipe_W_3", parent, new Vector3(-18.65f, y + 3.38f, 127.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_HighPipe_E_3", parent, new Vector3(18.65f, y + 3.38f, 127.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_W_4", parent, new Vector3(-18.65f, y + 0.55f, 133.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_LowerPipe_E_4", parent, new Vector3(18.65f, y + 0.55f, 133.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone10_CentralCore_HighPipe_W_4", parent, new Vector3(-18.65f, y + 3.38f, 133.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_HighPipe_E_4", parent, new Vector3(18.65f, y + 3.38f, 133.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone10_CentralCore_DuctCollar_C_0", parent, new Vector3(0.0f, y + 3.76f, 110.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_W_0", parent, new Vector3(-18.62f, y + 1.58f, 110.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_E_0", parent, new Vector3(18.62f, y + 1.58f, 110.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_W_0", parent, new Vector3(-18.35f, y + 0.01f, 110.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_E_0", parent, new Vector3(18.35f, y + 0.01f, 110.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_DuctCollar_C_1", parent, new Vector3(0.0f, y + 3.76f, 116.4f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_W_1", parent, new Vector3(-18.62f, y + 1.58f, 116.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_E_1", parent, new Vector3(18.62f, y + 1.58f, 116.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_W_1", parent, new Vector3(-18.35f, y + 0.01f, 116.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_E_1", parent, new Vector3(18.35f, y + 0.01f, 116.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_DuctCollar_C_2", parent, new Vector3(0.0f, y + 3.76f, 122.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_W_2", parent, new Vector3(-18.62f, y + 1.58f, 122.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_E_2", parent, new Vector3(18.62f, y + 1.58f, 122.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_W_2", parent, new Vector3(-18.35f, y + 0.01f, 122.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_E_2", parent, new Vector3(18.35f, y + 0.01f, 122.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_DuctCollar_C_3", parent, new Vector3(0.0f, y + 3.76f, 127.6f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_W_3", parent, new Vector3(-18.62f, y + 1.58f, 127.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_E_3", parent, new Vector3(18.62f, y + 1.58f, 127.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_W_3", parent, new Vector3(-18.35f, y + 0.01f, 127.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_E_3", parent, new Vector3(18.35f, y + 0.01f, 127.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_DuctCollar_C_4", parent, new Vector3(0.0f, y + 3.76f, 133.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_W_4", parent, new Vector3(-18.62f, y + 1.58f, 133.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_TelemetryNode_E_4", parent, new Vector3(18.62f, y + 1.58f, 133.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_W_4", parent, new Vector3(-18.35f, y + 0.01f, 133.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_FloorBoltPlate_E_4", parent, new Vector3(18.35f, y + 0.01f, 133.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone10_CentralCore_InspHatch_L_0", parent, new Vector3(-5.2f, y + 3.88f, 110.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_InspHatch_R_0", parent, new Vector3(5.2f, y + 3.88f, 110.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_RailClamp_W_0", parent, new Vector3(-18.64f, y + 2.15f, 110.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_RailClamp_E_0", parent, new Vector3(18.64f, y + 2.15f, 110.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_InspHatch_L_1", parent, new Vector3(-5.2f, y + 3.88f, 116.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_InspHatch_R_1", parent, new Vector3(5.2f, y + 3.88f, 116.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_RailClamp_W_1", parent, new Vector3(-18.64f, y + 2.15f, 116.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_RailClamp_E_1", parent, new Vector3(18.64f, y + 2.15f, 116.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_InspHatch_L_2", parent, new Vector3(-5.2f, y + 3.88f, 122.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_InspHatch_R_2", parent, new Vector3(5.2f, y + 3.88f, 122.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_RailClamp_W_2", parent, new Vector3(-18.64f, y + 2.15f, 122.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_RailClamp_E_2", parent, new Vector3(18.64f, y + 2.15f, 122.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_InspHatch_L_3", parent, new Vector3(-5.2f, y + 3.88f, 127.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_InspHatch_R_3", parent, new Vector3(5.2f, y + 3.88f, 127.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_RailClamp_W_3", parent, new Vector3(-18.64f, y + 2.15f, 127.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_RailClamp_E_3", parent, new Vector3(18.64f, y + 2.15f, 127.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_InspHatch_L_4", parent, new Vector3(-5.2f, y + 3.88f, 133.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_InspHatch_R_4", parent, new Vector3(5.2f, y + 3.88f, 133.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone10_CentralCore_RailClamp_W_4", parent, new Vector3(-18.64f, y + 2.15f, 133.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone10_CentralCore_RailClamp_E_4", parent, new Vector3(18.64f, y + 2.15f, 133.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }

    private static void BuildDetails_Zone11_SubTunnels(Transform parent)
    {
        float y = SubY;
        // Автоматично розміщені інженерні вузли, щитки, камери, вентиляція та реквізит для Zone11_SubTunnels
        BuildFireExtinguisherCabinet("Zone11_SubTunnels_FireCab_W_0", parent, new Vector3(-30.45f, y + 1.45f, 39.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone11_SubTunnels_FireCab_E_0", parent, new Vector3(30.45f, y + 1.45f, 39.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone11_SubTunnels_FireCab_W_1", parent, new Vector3(-30.45f, y + 1.45f, 64.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone11_SubTunnels_FireCab_E_1", parent, new Vector3(30.45f, y + 1.45f, 64.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildFireExtinguisherCabinet("Zone11_SubTunnels_FireCab_W_2", parent, new Vector3(-30.45f, y + 1.45f, 88.5f), Quaternion.Euler(0f, 90f, 0f));
        BuildFireExtinguisherCabinet("Zone11_SubTunnels_FireCab_E_2", parent, new Vector3(30.45f, y + 1.45f, 88.5f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallCctvCamera("Zone11_SubTunnels_CCTV_0", parent, new Vector3(-30.55f, y + 3.35f, 37.8f), Quaternion.Euler(0f, 45.0f, 0f));
        BuildWallCctvCamera("Zone11_SubTunnels_CCTV_1", parent, new Vector3(30.55f, y + 3.35f, 37.8f), Quaternion.Euler(0f, -45.0f, 0f));
        BuildWallCctvCamera("Zone11_SubTunnels_CCTV_2", parent, new Vector3(-30.55f, y + 3.35f, 90.2f), Quaternion.Euler(0f, 135.0f, 0f));
        BuildWallCctvCamera("Zone11_SubTunnels_CCTV_3", parent, new Vector3(30.55f, y + 3.35f, 90.2f), Quaternion.Euler(0f, -135.0f, 0f));
        BuildWallCctvCamera("Zone11_SubTunnels_CCTV_4", parent, new Vector3(-3.5f, y + 3.35f, 37.6f), Quaternion.Euler(0f, 15.0f, 0f));
        BuildWallCctvCamera("Zone11_SubTunnels_CCTV_5", parent, new Vector3(3.5f, y + 3.35f, 90.4f), Quaternion.Euler(0f, 165.0f, 0f));
        BuildEmergencyIntercomWallUnit("Zone11_SubTunnels_Intercom_W_0", parent, new Vector3(-30.58f, y + 1.52f, 40.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone11_SubTunnels_Intercom_E_0", parent, new Vector3(30.58f, y + 1.52f, 40.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone11_SubTunnels_Clock_W_0", parent, new Vector3(-30.6f, y + 2.85f, 40.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone11_SubTunnels_Clock_E_0", parent, new Vector3(30.6f, y + 2.85f, 40.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone11_SubTunnels_Intercom_W_1", parent, new Vector3(-30.58f, y + 1.52f, 56.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone11_SubTunnels_Intercom_E_1", parent, new Vector3(30.58f, y + 1.52f, 56.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone11_SubTunnels_Intercom_W_2", parent, new Vector3(-30.58f, y + 1.52f, 72.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone11_SubTunnels_Intercom_E_2", parent, new Vector3(30.58f, y + 1.52f, 72.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallClockSoviet("Zone11_SubTunnels_Clock_W_2", parent, new Vector3(-30.6f, y + 2.85f, 72.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallClockSoviet("Zone11_SubTunnels_Clock_E_2", parent, new Vector3(30.6f, y + 2.85f, 72.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone11_SubTunnels_Intercom_W_3", parent, new Vector3(-30.58f, y + 1.52f, 88.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildEmergencyIntercomWallUnit("Zone11_SubTunnels_Intercom_E_3", parent, new Vector3(30.58f, y + 1.52f, 88.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_W_0", parent, new Vector3(-30.55f, y, 39.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_E_0", parent, new Vector3(30.55f, y, 39.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_W_1", parent, new Vector3(-30.55f, y, 55.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_E_1", parent, new Vector3(30.55f, y, 55.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_W_2", parent, new Vector3(-30.55f, y, 72.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_E_2", parent, new Vector3(30.55f, y, 72.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_W_3", parent, new Vector3(-30.55f, y, 89.0f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_E_3", parent, new Vector3(30.55f, y, 89.0f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_W_4", parent, new Vector3(-30.55f, y, 105.67f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_E_4", parent, new Vector3(30.55f, y, 105.67f), Quaternion.Euler(0f, -90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_W_5", parent, new Vector3(-30.55f, y, 122.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildWallRadiator("Zone11_SubTunnels_Radiator_E_5", parent, new Vector3(30.55f, y, 122.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_W_0", parent, new Vector3(-30.58f, y + 3.15f, 39.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_E_0", parent, new Vector3(30.58f, y + 3.15f, 39.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_W_0", parent, new Vector3(-29.75f, y + 3.45f, 39.2f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_E_0", parent, new Vector3(29.75f, y + 3.45f, 39.2f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_W_1", parent, new Vector3(-30.58f, y + 3.15f, 55.73f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_E_1", parent, new Vector3(30.58f, y + 3.15f, 55.73f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_W_1", parent, new Vector3(-29.75f, y + 3.45f, 55.73f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_E_1", parent, new Vector3(29.75f, y + 3.45f, 55.73f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_W_2", parent, new Vector3(-30.58f, y + 3.15f, 72.27f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_E_2", parent, new Vector3(30.58f, y + 3.15f, 72.27f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_W_2", parent, new Vector3(-29.75f, y + 3.45f, 72.27f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_E_2", parent, new Vector3(29.75f, y + 3.45f, 72.27f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_W_3", parent, new Vector3(-30.58f, y + 3.15f, 88.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_E_3", parent, new Vector3(30.58f, y + 3.15f, 88.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_W_3", parent, new Vector3(-29.75f, y + 3.45f, 88.8f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_E_3", parent, new Vector3(29.75f, y + 3.45f, 88.8f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_W_4", parent, new Vector3(-30.58f, y + 3.15f, 105.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_E_4", parent, new Vector3(30.58f, y + 3.15f, 105.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_W_4", parent, new Vector3(-29.75f, y + 3.45f, 105.33f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_E_4", parent, new Vector3(29.75f, y + 3.45f, 105.33f), Quaternion.Euler(0f, -90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_W_5", parent, new Vector3(-30.58f, y + 3.15f, 121.87f), Quaternion.Euler(0f, 90f, 0f));
        BuildVentilationGrille("Zone11_SubTunnels_Vent_E_5", parent, new Vector3(30.58f, y + 3.15f, 121.87f), Quaternion.Euler(0f, -90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_W_5", parent, new Vector3(-29.75f, y + 3.45f, 121.87f), Quaternion.Euler(0f, 90f, 0f));
        BuildCeilingPublicAddressSpeaker("Zone11_SubTunnels_PA_E_5", parent, new Vector3(29.75f, y + 3.45f, 121.87f), Quaternion.Euler(0f, -90f, 0f));
        BuildPipeValveManifold("Zone11_SubTunnels_ValveManifold_S_0", parent, new Vector3(-28.0f, y, 37.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone11_SubTunnels_ValveManifold_N_0", parent, new Vector3(-28.0f, y, 90.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone11_SubTunnels_FuseBox_S_0", parent, new Vector3(-26.8f, y + 1.65f, 37.45f), Quaternion.identity);
        BuildWallFuseBox("Zone11_SubTunnels_FuseBox_N_0", parent, new Vector3(-29.2f, y + 1.65f, 90.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone11_SubTunnels_ValveManifold_S_1", parent, new Vector3(-9.33f, y, 37.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone11_SubTunnels_ValveManifold_N_1", parent, new Vector3(-9.33f, y, 90.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone11_SubTunnels_FuseBox_S_1", parent, new Vector3(-8.13f, y + 1.65f, 37.45f), Quaternion.identity);
        BuildWallFuseBox("Zone11_SubTunnels_FuseBox_N_1", parent, new Vector3(-10.53f, y + 1.65f, 90.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone11_SubTunnels_ValveManifold_S_2", parent, new Vector3(9.33f, y, 37.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone11_SubTunnels_ValveManifold_N_2", parent, new Vector3(9.33f, y, 90.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone11_SubTunnels_FuseBox_S_2", parent, new Vector3(10.53f, y + 1.65f, 37.45f), Quaternion.identity);
        BuildWallFuseBox("Zone11_SubTunnels_FuseBox_N_2", parent, new Vector3(8.13f, y + 1.65f, 90.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildPipeValveManifold("Zone11_SubTunnels_ValveManifold_S_3", parent, new Vector3(28.0f, y, 37.55f), Quaternion.identity);
        BuildPipeValveManifold("Zone11_SubTunnels_ValveManifold_N_3", parent, new Vector3(28.0f, y, 90.45f), Quaternion.Euler(0f, 180f, 0f));
        BuildWallFuseBox("Zone11_SubTunnels_FuseBox_S_3", parent, new Vector3(29.2f, y + 1.65f, 37.45f), Quaternion.identity);
        BuildWallFuseBox("Zone11_SubTunnels_FuseBox_N_3", parent, new Vector3(26.8f, y + 1.65f, 90.55f), Quaternion.Euler(0f, 180f, 0f));
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_L_0", parent, new Vector3(-4.2f, y + 3.48f, 40.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_R_0", parent, new Vector3(4.2f, y + 3.48f, 40.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_L_1", parent, new Vector3(-4.2f, y + 3.48f, 52.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_R_1", parent, new Vector3(4.2f, y + 3.48f, 52.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_L_2", parent, new Vector3(-4.2f, y + 3.48f, 64.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_R_2", parent, new Vector3(4.2f, y + 3.48f, 64.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_L_3", parent, new Vector3(-4.2f, y + 3.48f, 76.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_R_3", parent, new Vector3(4.2f, y + 3.48f, 76.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_L_4", parent, new Vector3(-4.2f, y + 3.48f, 88.0f), Quaternion.identity);
        BuildAcousticCeilingBaffle("Zone11_SubTunnels_Baffle_R_4", parent, new Vector3(4.2f, y + 3.48f, 88.0f), Quaternion.identity);
        BuildIndustrialCrateStack("Zone11_SubTunnels_Crates_SW", parent, new Vector3(-29.4f, y, 38.6f), 14f);
        BuildCableSpoolAndMat("Zone11_SubTunnels_Spool_SE", parent, new Vector3(29.4f, y, 38.6f), -28f);
        BuildHazmatDrumCluster("Zone11_SubTunnels_Drums_NW", parent, new Vector3(-29.4f, y, 89.4f));
        BuildPatchPanelRack("Zone11_SubTunnels_PatchRack_NE", parent, new Vector3(29.4f, y, 89.4f), Quaternion.Euler(0f, -135f, 0f));
        BuildClipboardCheckStation("Zone11_SubTunnels_CheckStation_S_0", parent, new Vector3(-28.6f, y, 38.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone11_SubTunnels_Bollard_S_0", parent, new Vector3(-29.45f, y, 37.95f));
        BuildWallWarningBollard("Zone11_SubTunnels_Bollard_N_0", parent, new Vector3(-27.75f, y, 90.05f));
        BuildClipboardCheckStation("Zone11_SubTunnels_CheckStation_S_1", parent, new Vector3(-9.53f, y, 38.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone11_SubTunnels_Bollard_S_1", parent, new Vector3(-10.38f, y, 37.95f));
        BuildWallWarningBollard("Zone11_SubTunnels_Bollard_N_1", parent, new Vector3(-8.68f, y, 90.05f));
        BuildClipboardCheckStation("Zone11_SubTunnels_CheckStation_S_2", parent, new Vector3(9.53f, y, 38.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone11_SubTunnels_Bollard_S_2", parent, new Vector3(8.68f, y, 37.95f));
        BuildWallWarningBollard("Zone11_SubTunnels_Bollard_N_2", parent, new Vector3(10.38f, y, 90.05f));
        BuildClipboardCheckStation("Zone11_SubTunnels_CheckStation_S_3", parent, new Vector3(28.6f, y, 38.35f), Quaternion.identity);
        BuildWallWarningBollard("Zone11_SubTunnels_Bollard_S_3", parent, new Vector3(27.75f, y, 37.95f));
        BuildWallWarningBollard("Zone11_SubTunnels_Bollard_N_3", parent, new Vector3(29.45f, y, 90.05f));
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_W_0", parent, new Vector3(-30.75f, y + 2.0f, 38.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_E_0", parent, new Vector3(30.75f, y + 2.0f, 38.2f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_W_0", parent, new Vector3(-30.62f, y + 0.32f, 38.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_E_0", parent, new Vector3(30.62f, y + 0.32f, 38.2f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_W_1", parent, new Vector3(-30.75f, y + 2.0f, 51.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_E_1", parent, new Vector3(30.75f, y + 2.0f, 51.1f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_W_1", parent, new Vector3(-30.62f, y + 0.32f, 51.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_E_1", parent, new Vector3(30.62f, y + 0.32f, 51.1f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_W_2", parent, new Vector3(-30.75f, y + 2.0f, 64.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_E_2", parent, new Vector3(30.75f, y + 2.0f, 64.0f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_W_2", parent, new Vector3(-30.62f, y + 0.32f, 64.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_E_2", parent, new Vector3(30.62f, y + 0.32f, 64.0f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_W_3", parent, new Vector3(-30.75f, y + 2.0f, 76.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_E_3", parent, new Vector3(30.75f, y + 2.0f, 76.9f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_W_3", parent, new Vector3(-30.62f, y + 0.32f, 76.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_E_3", parent, new Vector3(30.62f, y + 0.32f, 76.9f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_W_4", parent, new Vector3(-30.75f, y + 2.0f, 89.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone11_SubTunnels_Pilaster_E_4", parent, new Vector3(30.75f, y + 2.0f, 89.8f), new Vector3(0.28f, 3.95f, 0.36f), _matConcreteDark, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_W_4", parent, new Vector3(-30.62f, y + 0.32f, 89.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_ConduitTrim_E_4", parent, new Vector3(30.62f, y + 0.32f, 89.8f), new Vector3(0.08f, 0.14f, 1.45f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingRib_0", parent, new Vector3(0.0f, y + 3.82f, 38.5f), new Vector3(60.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_L_0", parent, new Vector3(-17.02f, y + 3.68f, 38.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_R_0", parent, new Vector3(17.02f, y + 3.68f, 38.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CeilingRib_1", parent, new Vector3(0.0f, y + 3.82f, 51.25f), new Vector3(60.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_L_1", parent, new Vector3(-17.02f, y + 3.68f, 51.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_R_1", parent, new Vector3(17.02f, y + 3.68f, 51.25f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CeilingRib_2", parent, new Vector3(0.0f, y + 3.82f, 64.0f), new Vector3(60.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_L_2", parent, new Vector3(-17.02f, y + 3.68f, 64.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_R_2", parent, new Vector3(17.02f, y + 3.68f, 64.0f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CeilingRib_3", parent, new Vector3(0.0f, y + 3.82f, 76.75f), new Vector3(60.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_L_3", parent, new Vector3(-17.02f, y + 3.68f, 76.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_R_3", parent, new Vector3(17.02f, y + 3.68f, 76.75f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CeilingRib_4", parent, new Vector3(0.0f, y + 3.82f, 89.5f), new Vector3(60.8f, 0.16f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_L_4", parent, new Vector3(-17.02f, y + 3.68f, 89.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CableTrayHanger_R_4", parent, new Vector3(17.02f, y + 3.68f, 89.5f), new Vector3(0.65f, 0.08f, 0.16f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_FloorSafetyLine_S_0", parent, new Vector3(-28.2f, y + 0.014f, 38.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_FloorSafetyLine_N_0", parent, new Vector3(-28.2f, y + 0.014f, 89.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_WallDataPlaque_S_0", parent, new Vector3(-28.2f, y + 2.25f, 37.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_WallDataPlaque_N_0", parent, new Vector3(-28.2f, y + 2.25f, 90.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_FloorSafetyLine_S_1", parent, new Vector3(-9.4f, y + 0.014f, 38.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_FloorSafetyLine_N_1", parent, new Vector3(-9.4f, y + 0.014f, 89.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_WallDataPlaque_S_1", parent, new Vector3(-9.4f, y + 2.25f, 37.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_WallDataPlaque_N_1", parent, new Vector3(-9.4f, y + 2.25f, 90.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_FloorSafetyLine_S_2", parent, new Vector3(9.4f, y + 0.014f, 38.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_FloorSafetyLine_N_2", parent, new Vector3(9.4f, y + 0.014f, 89.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_WallDataPlaque_S_2", parent, new Vector3(9.4f, y + 2.25f, 37.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_WallDataPlaque_N_2", parent, new Vector3(9.4f, y + 2.25f, 90.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_FloorSafetyLine_S_3", parent, new Vector3(28.2f, y + 0.014f, 38.05f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_FloorSafetyLine_N_3", parent, new Vector3(28.2f, y + 0.014f, 89.95f), new Vector3(1.65f, 0.015f, 0.16f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_WallDataPlaque_S_3", parent, new Vector3(28.2f, y + 2.25f, 37.4f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_WallDataPlaque_N_3", parent, new Vector3(28.2f, y + 2.25f, 90.6f), new Vector3(0.95f, 0.42f, 0.05f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensor_W_0", parent, new Vector3(-30.58f, y + 2.45f, 39.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensorLed_W_0", parent, new Vector3(-30.49f, y + 2.48f, 39.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensor_E_0", parent, new Vector3(30.58f, y + 2.45f, 39.4f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensorLed_E_0", parent, new Vector3(30.49f, y + 2.48f, 39.4f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensor_W_1", parent, new Vector3(-30.58f, y + 2.45f, 55.8f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensorLed_W_1", parent, new Vector3(-30.49f, y + 2.48f, 55.8f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensor_E_1", parent, new Vector3(30.58f, y + 2.45f, 55.8f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensorLed_E_1", parent, new Vector3(30.49f, y + 2.48f, 55.8f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensor_W_2", parent, new Vector3(-30.58f, y + 2.45f, 72.2f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensorLed_W_2", parent, new Vector3(-30.49f, y + 2.48f, 72.2f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensor_E_2", parent, new Vector3(30.58f, y + 2.45f, 72.2f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensorLed_E_2", parent, new Vector3(30.49f, y + 2.48f, 72.2f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensor_W_3", parent, new Vector3(-30.58f, y + 2.45f, 88.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensorLed_W_3", parent, new Vector3(-30.49f, y + 2.48f, 88.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtGreen);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensor_E_3", parent, new Vector3(30.58f, y + 2.45f, 88.6f), new Vector3(0.16f, 0.24f, 0.16f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_AcousticSensorLed_E_3", parent, new Vector3(30.49f, y + 2.48f, 88.6f), new Vector3(0.03f, 0.06f, 0.06f), _matCrtAmber);
        CreateDecorBox("Zone11_SubTunnels_DrainStrip_L_0", parent, new Vector3(-2.6f, y + 0.012f, 39.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_DrainStrip_R_0", parent, new Vector3(2.6f, y + 0.012f, 39.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallCableClamp_W_0", parent, new Vector3(-30.64f, y + 1.15f, 39.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_WallCableClamp_E_0", parent, new Vector3(30.64f, y + 1.15f, 39.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_DrainStrip_L_1", parent, new Vector3(-2.6f, y + 0.012f, 55.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_DrainStrip_R_1", parent, new Vector3(2.6f, y + 0.012f, 55.67f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallCableClamp_W_1", parent, new Vector3(-30.64f, y + 1.15f, 55.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_WallCableClamp_E_1", parent, new Vector3(30.64f, y + 1.15f, 55.67f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_DrainStrip_L_2", parent, new Vector3(-2.6f, y + 0.012f, 72.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_DrainStrip_R_2", parent, new Vector3(2.6f, y + 0.012f, 72.33f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallCableClamp_W_2", parent, new Vector3(-30.64f, y + 1.15f, 72.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_WallCableClamp_E_2", parent, new Vector3(30.64f, y + 1.15f, 72.33f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_DrainStrip_L_3", parent, new Vector3(-2.6f, y + 0.012f, 89.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_DrainStrip_R_3", parent, new Vector3(2.6f, y + 0.012f, 89.0f), new Vector3(0.32f, 0.015f, 1.85f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallCableClamp_W_3", parent, new Vector3(-30.64f, y + 1.15f, 89.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_WallCableClamp_E_3", parent, new Vector3(30.64f, y + 1.15f, 89.0f), new Vector3(0.08f, 0.10f, 1.15f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_Baseboard_S_0", parent, new Vector3(-28.8f, y + 0.12f, 37.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_Baseboard_N_0", parent, new Vector3(-28.8f, y + 0.12f, 90.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingPipe_S_0", parent, new Vector3(-28.8f, y + 3.55f, 38.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_CeilingPipe_N_0", parent, new Vector3(-28.8f, y + 3.55f, 89.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_Baseboard_S_1", parent, new Vector3(-9.6f, y + 0.12f, 37.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_Baseboard_N_1", parent, new Vector3(-9.6f, y + 0.12f, 90.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingPipe_S_1", parent, new Vector3(-9.6f, y + 3.55f, 38.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_CeilingPipe_N_1", parent, new Vector3(-9.6f, y + 3.55f, 89.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_Baseboard_S_2", parent, new Vector3(9.6f, y + 0.12f, 37.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_Baseboard_N_2", parent, new Vector3(9.6f, y + 0.12f, 90.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingPipe_S_2", parent, new Vector3(9.6f, y + 3.55f, 38.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_CeilingPipe_N_2", parent, new Vector3(9.6f, y + 3.55f, 89.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_Baseboard_S_3", parent, new Vector3(28.8f, y + 0.12f, 37.34f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_Baseboard_N_3", parent, new Vector3(28.8f, y + 0.12f, 90.66f), new Vector3(2.2f, 0.24f, 0.08f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingPipe_S_3", parent, new Vector3(28.8f, y + 3.55f, 38.8f), new Vector3(2.4f, 0.12f, 0.12f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_CeilingPipe_N_3", parent, new Vector3(28.8f, y + 3.55f, 89.2f), new Vector3(2.4f, 0.12f, 0.12f), _matRustedSteel);
        CreateAcousticBox("Zone11_SubTunnels_AcousticPanel_W_0", parent, new Vector3(-30.64f, y + 1.85f, 39.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone11_SubTunnels_AcousticPanel_E_0", parent, new Vector3(30.64f, y + 1.85f, 39.6f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone11_SubTunnels_StrobeMount_W_0", parent, new Vector3(-30.6f, y + 3.05f, 40.25f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone11_SubTunnels_StrobeMount_E_0", parent, new Vector3(30.6f, y + 3.05f, 38.95f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone11_SubTunnels_AcousticPanel_W_1", parent, new Vector3(-30.64f, y + 1.85f, 55.87f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone11_SubTunnels_AcousticPanel_E_1", parent, new Vector3(30.64f, y + 1.85f, 55.87f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone11_SubTunnels_StrobeMount_W_1", parent, new Vector3(-30.6f, y + 3.05f, 56.52f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone11_SubTunnels_StrobeMount_E_1", parent, new Vector3(30.6f, y + 3.05f, 55.22f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone11_SubTunnels_AcousticPanel_W_2", parent, new Vector3(-30.64f, y + 1.85f, 72.13f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone11_SubTunnels_AcousticPanel_E_2", parent, new Vector3(30.64f, y + 1.85f, 72.13f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone11_SubTunnels_StrobeMount_W_2", parent, new Vector3(-30.6f, y + 3.05f, 72.78f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone11_SubTunnels_StrobeMount_E_2", parent, new Vector3(30.6f, y + 3.05f, 71.48f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateAcousticBox("Zone11_SubTunnels_AcousticPanel_W_3", parent, new Vector3(-30.64f, y + 1.85f, 88.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateAcousticBox("Zone11_SubTunnels_AcousticPanel_E_3", parent, new Vector3(30.64f, y + 1.85f, 88.4f), new Vector3(0.09f, 1.15f, 0.85f), _matAcousticFoam, AcousticMaterialType.Concrete);
        CreateDecorBox("Zone11_SubTunnels_StrobeMount_W_3", parent, new Vector3(-30.6f, y + 3.05f, 89.05f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone11_SubTunnels_StrobeMount_E_3", parent, new Vector3(30.6f, y + 3.05f, 87.75f), new Vector3(0.14f, 0.18f, 0.14f), _matLampAmber);
        CreateDecorBox("Zone11_SubTunnels_CeilingVentBox_S_0", parent, new Vector3(-28.4f, y + 3.86f, 60.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CeilingVentBox_N_0", parent, new Vector3(-28.4f, y + 3.86f, 67.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_VerticalConduit_S_0", parent, new Vector3(-28.4f, y + 1.95f, 37.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_VerticalConduit_N_0", parent, new Vector3(-28.4f, y + 1.95f, 90.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_CeilingVentBox_S_1", parent, new Vector3(-9.47f, y + 3.86f, 60.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CeilingVentBox_N_1", parent, new Vector3(-9.47f, y + 3.86f, 67.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_VerticalConduit_S_1", parent, new Vector3(-9.47f, y + 1.95f, 37.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_VerticalConduit_N_1", parent, new Vector3(-9.47f, y + 1.95f, 90.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_CeilingVentBox_S_2", parent, new Vector3(9.47f, y + 3.86f, 60.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CeilingVentBox_N_2", parent, new Vector3(9.47f, y + 3.86f, 67.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_VerticalConduit_S_2", parent, new Vector3(9.47f, y + 1.95f, 37.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_VerticalConduit_N_2", parent, new Vector3(9.47f, y + 1.95f, 90.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_CeilingVentBox_S_3", parent, new Vector3(28.4f, y + 3.86f, 60.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_CeilingVentBox_N_3", parent, new Vector3(28.4f, y + 3.86f, 67.5f), new Vector3(0.75f, 0.12f, 0.75f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_VerticalConduit_S_3", parent, new Vector3(28.4f, y + 1.95f, 37.36f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_VerticalConduit_N_3", parent, new Vector3(28.4f, y + 1.95f, 90.64f), new Vector3(0.08f, 3.85f, 0.08f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_W_0", parent, new Vector3(-30.66f, y + 3.65f, 38.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_E_0", parent, new Vector3(30.66f, y + 3.65f, 38.4f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_W_0", parent, new Vector3(-30.67f, y + 1.32f, 38.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_E_0", parent, new Vector3(30.67f, y + 1.32f, 38.4f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_W_1", parent, new Vector3(-30.66f, y + 3.65f, 51.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_E_1", parent, new Vector3(30.66f, y + 3.65f, 51.2f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_W_1", parent, new Vector3(-30.67f, y + 1.32f, 51.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_E_1", parent, new Vector3(30.67f, y + 1.32f, 51.2f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_W_2", parent, new Vector3(-30.66f, y + 3.65f, 64.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_E_2", parent, new Vector3(30.66f, y + 3.65f, 64.0f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_W_2", parent, new Vector3(-30.67f, y + 1.32f, 64.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_E_2", parent, new Vector3(30.67f, y + 1.32f, 64.0f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_W_3", parent, new Vector3(-30.66f, y + 3.65f, 76.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_E_3", parent, new Vector3(30.66f, y + 3.65f, 76.8f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_W_3", parent, new Vector3(-30.67f, y + 1.32f, 76.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_E_3", parent, new Vector3(30.67f, y + 1.32f, 76.8f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_W_4", parent, new Vector3(-30.66f, y + 3.65f, 89.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_UpperTrim_E_4", parent, new Vector3(30.66f, y + 3.65f, 89.6f), new Vector3(0.10f, 0.18f, 1.55f), _matConcreteDark);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_W_4", parent, new Vector3(-30.67f, y + 1.32f, 89.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorBox("Zone11_SubTunnels_MidStripe_E_4", parent, new Vector3(30.67f, y + 1.32f, 89.6f), new Vector3(0.04f, 0.08f, 1.50f), _matHazardStripes);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_L_0", parent, new Vector3(-3.2f, y + 3.84f, 38.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_R_0", parent, new Vector3(3.2f, y + 3.84f, 38.6f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_W_0", parent, new Vector3(-30.62f, y + 0.78f, 38.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_E_0", parent, new Vector3(30.62f, y + 0.78f, 38.6f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_L_1", parent, new Vector3(-3.2f, y + 3.84f, 51.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_R_1", parent, new Vector3(3.2f, y + 3.84f, 51.3f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_W_1", parent, new Vector3(-30.62f, y + 0.78f, 51.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_E_1", parent, new Vector3(30.62f, y + 0.78f, 51.3f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_L_2", parent, new Vector3(-3.2f, y + 3.84f, 64.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_R_2", parent, new Vector3(3.2f, y + 3.84f, 64.0f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_W_2", parent, new Vector3(-30.62f, y + 0.78f, 64.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_E_2", parent, new Vector3(30.62f, y + 0.78f, 64.0f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_L_3", parent, new Vector3(-3.2f, y + 3.84f, 76.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_R_3", parent, new Vector3(3.2f, y + 3.84f, 76.7f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_W_3", parent, new Vector3(-30.62f, y + 0.78f, 76.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_E_3", parent, new Vector3(30.62f, y + 0.78f, 76.7f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_L_4", parent, new Vector3(-3.2f, y + 3.84f, 89.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorCylinder("Zone11_SubTunnels_Sprinkler_R_4", parent, new Vector3(3.2f, y + 3.84f, 89.4f), new Vector3(0.10f, 0.08f, 0.10f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_W_4", parent, new Vector3(-30.62f, y + 0.78f, 89.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallJunction_E_4", parent, new Vector3(30.62f, y + 0.78f, 89.4f), new Vector3(0.14f, 0.22f, 0.22f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_FloorJoint_X_0", parent, new Vector3(-29.0f, y + 0.008f, 64.0f), new Vector3(0.05f, 0.01f, 51.0f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingStrut_S_0", parent, new Vector3(-29.0f, y + 3.74f, 39.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingStrut_N_0", parent, new Vector3(-29.0f, y + 3.74f, 88.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallBracket_S_0", parent, new Vector3(-29.0f, y + 2.85f, 37.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_WallBracket_N_0", parent, new Vector3(-29.0f, y + 2.85f, 90.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_FloorJoint_X_1", parent, new Vector3(-9.67f, y + 0.008f, 64.0f), new Vector3(0.05f, 0.01f, 51.0f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingStrut_S_1", parent, new Vector3(-9.67f, y + 3.74f, 39.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingStrut_N_1", parent, new Vector3(-9.67f, y + 3.74f, 88.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallBracket_S_1", parent, new Vector3(-9.67f, y + 2.85f, 37.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_WallBracket_N_1", parent, new Vector3(-9.67f, y + 2.85f, 90.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_FloorJoint_X_2", parent, new Vector3(9.67f, y + 0.008f, 64.0f), new Vector3(0.05f, 0.01f, 51.0f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingStrut_S_2", parent, new Vector3(9.67f, y + 3.74f, 39.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingStrut_N_2", parent, new Vector3(9.67f, y + 3.74f, 88.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallBracket_S_2", parent, new Vector3(9.67f, y + 2.85f, 37.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_WallBracket_N_2", parent, new Vector3(9.67f, y + 2.85f, 90.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_FloorJoint_X_3", parent, new Vector3(29.0f, y + 0.008f, 64.0f), new Vector3(0.05f, 0.01f, 51.0f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingStrut_S_3", parent, new Vector3(29.0f, y + 3.74f, 39.2f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_CeilingStrut_N_3", parent, new Vector3(29.0f, y + 3.74f, 88.8f), new Vector3(0.12f, 0.22f, 1.25f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_WallBracket_S_3", parent, new Vector3(29.0f, y + 2.85f, 37.38f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_WallBracket_N_3", parent, new Vector3(29.0f, y + 2.85f, 90.62f), new Vector3(0.28f, 0.14f, 0.18f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_W_0", parent, new Vector3(-30.65f, y + 0.55f, 38.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_E_0", parent, new Vector3(30.65f, y + 0.55f, 38.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_W_0", parent, new Vector3(-30.65f, y + 3.38f, 38.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_E_0", parent, new Vector3(30.65f, y + 3.38f, 38.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_W_1", parent, new Vector3(-30.65f, y + 0.55f, 51.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_E_1", parent, new Vector3(30.65f, y + 0.55f, 51.25f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_W_1", parent, new Vector3(-30.65f, y + 3.38f, 51.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_E_1", parent, new Vector3(30.65f, y + 3.38f, 51.25f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_W_2", parent, new Vector3(-30.65f, y + 0.55f, 64.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_E_2", parent, new Vector3(30.65f, y + 0.55f, 64.0f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_W_2", parent, new Vector3(-30.65f, y + 3.38f, 64.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_E_2", parent, new Vector3(30.65f, y + 3.38f, 64.0f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_W_3", parent, new Vector3(-30.65f, y + 0.55f, 76.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_E_3", parent, new Vector3(30.65f, y + 0.55f, 76.75f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_W_3", parent, new Vector3(-30.65f, y + 3.38f, 76.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_E_3", parent, new Vector3(30.65f, y + 3.38f, 76.75f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_W_4", parent, new Vector3(-30.65f, y + 0.55f, 89.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_LowerPipe_E_4", parent, new Vector3(30.65f, y + 0.55f, 89.5f), new Vector3(0.08f, 0.08f, 1.40f), _matRustedSteel);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_W_4", parent, new Vector3(-30.65f, y + 3.38f, 89.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_HighPipe_E_4", parent, new Vector3(30.65f, y + 3.38f, 89.5f), new Vector3(0.10f, 0.10f, 1.40f), _matCopperPipe);
        CreateDecorBox("Zone11_SubTunnels_DuctCollar_C_0", parent, new Vector3(0.0f, y + 3.76f, 38.8f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_W_0", parent, new Vector3(-30.62f, y + 1.58f, 38.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_E_0", parent, new Vector3(30.62f, y + 1.58f, 38.8f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_W_0", parent, new Vector3(-30.35f, y + 0.01f, 38.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_E_0", parent, new Vector3(30.35f, y + 0.01f, 38.8f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_DuctCollar_C_1", parent, new Vector3(0.0f, y + 3.76f, 51.4f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_W_1", parent, new Vector3(-30.62f, y + 1.58f, 51.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_E_1", parent, new Vector3(30.62f, y + 1.58f, 51.4f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_W_1", parent, new Vector3(-30.35f, y + 0.01f, 51.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_E_1", parent, new Vector3(30.35f, y + 0.01f, 51.4f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_DuctCollar_C_2", parent, new Vector3(0.0f, y + 3.76f, 64.0f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_W_2", parent, new Vector3(-30.62f, y + 1.58f, 64.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_E_2", parent, new Vector3(30.62f, y + 1.58f, 64.0f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_W_2", parent, new Vector3(-30.35f, y + 0.01f, 64.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_E_2", parent, new Vector3(30.35f, y + 0.01f, 64.0f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_DuctCollar_C_3", parent, new Vector3(0.0f, y + 3.76f, 76.6f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_W_3", parent, new Vector3(-30.62f, y + 1.58f, 76.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_E_3", parent, new Vector3(30.62f, y + 1.58f, 76.6f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_W_3", parent, new Vector3(-30.35f, y + 0.01f, 76.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_E_3", parent, new Vector3(30.35f, y + 0.01f, 76.6f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_DuctCollar_C_4", parent, new Vector3(0.0f, y + 3.76f, 89.2f), new Vector3(1.12f, 0.18f, 0.14f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_W_4", parent, new Vector3(-30.62f, y + 1.58f, 89.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_TelemetryNode_E_4", parent, new Vector3(30.62f, y + 1.58f, 89.2f), new Vector3(0.12f, 0.28f, 0.20f), _matRetroBeige);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_W_4", parent, new Vector3(-30.35f, y + 0.01f, 89.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_FloorBoltPlate_E_4", parent, new Vector3(30.35f, y + 0.01f, 89.2f), new Vector3(0.35f, 0.015f, 0.35f), _matDarkIron);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_L_0", parent, new Vector3(-5.2f, y + 3.88f, 38.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_R_0", parent, new Vector3(5.2f, y + 3.88f, 38.7f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_W_0", parent, new Vector3(-30.64f, y + 2.15f, 38.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_E_0", parent, new Vector3(30.64f, y + 2.15f, 38.7f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_L_1", parent, new Vector3(-5.2f, y + 3.88f, 51.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_R_1", parent, new Vector3(5.2f, y + 3.88f, 51.35f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_W_1", parent, new Vector3(-30.64f, y + 2.15f, 51.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_E_1", parent, new Vector3(30.64f, y + 2.15f, 51.35f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_L_2", parent, new Vector3(-5.2f, y + 3.88f, 64.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_R_2", parent, new Vector3(5.2f, y + 3.88f, 64.0f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_W_2", parent, new Vector3(-30.64f, y + 2.15f, 64.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_E_2", parent, new Vector3(30.64f, y + 2.15f, 64.0f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_L_3", parent, new Vector3(-5.2f, y + 3.88f, 76.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_R_3", parent, new Vector3(5.2f, y + 3.88f, 76.65f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_W_3", parent, new Vector3(-30.64f, y + 2.15f, 76.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_E_3", parent, new Vector3(30.64f, y + 2.15f, 76.65f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_L_4", parent, new Vector3(-5.2f, y + 3.88f, 89.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_InspHatch_R_4", parent, new Vector3(5.2f, y + 3.88f, 89.3f), new Vector3(0.85f, 0.06f, 0.85f), _matMetalBulkhead);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_W_4", parent, new Vector3(-30.64f, y + 2.15f, 89.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
        CreateDecorBox("Zone11_SubTunnels_RailClamp_E_4", parent, new Vector3(30.64f, y + 2.15f, 89.3f), new Vector3(0.08f, 0.14f, 0.24f), _matBrassTrim);
    }


    // =============================================================================================
    // ІНІЦІАЛІЗАЦІЯ ПРОЦЕДУРНИХ МАТЕРІАЛІВ ТА ТЕКСТУР URP
    // =============================================================================================
    private static void InitializeMaterials()
    {
        Texture2D concreteTex = GenerateConcreteTexture(256, 256);
        Texture2D tileTex = GenerateTileTexture(256, 256, 32);
        Texture2D checkerTex = GenerateCheckerTexture(256, 256);
        Texture2D woodTex = GenerateWoodTexture(256, 256);
        Texture2D metalTex = GenerateNoiseTexture(128, 128, 0.18f, 0.30f);
        Texture2D hazardTex = GenerateHazardTexture(256, 256);
        Texture2D foamTex = GenerateAcousticFoamTexture(256, 256);

        _matConcreteWall = CreateTexturedMat("UG_ConcreteWall", concreteTex, new Color(0.42f, 0.44f, 0.42f), 0.18f, new Vector2(4f, 2f));
        _matConcreteDark = CreateTexturedMat("UG_ConcreteDark", concreteTex, new Color(0.24f, 0.26f, 0.26f), 0.22f, new Vector2(4f, 2f));
        _matConcreteDamaged = CreateTexturedMat("UG_ConcreteDamaged", concreteTex, new Color(0.32f, 0.34f, 0.31f), 0.14f, new Vector2(3f, 2f));
        _matConcreteFloor = CreateTexturedMat("UG_ConcreteFloor", concreteTex, new Color(0.28f, 0.30f, 0.29f), 0.36f, new Vector2(10f, 10f));
        _matLabTileFloor = CreateTexturedMat("UG_LabTileFloor", tileTex, new Color(0.24f, 0.27f, 0.26f), 0.48f, new Vector2(12f, 12f));
        _matCeramicTileWall = CreateTexturedMat("UG_CeramicTileWall", tileTex, new Color(0.70f, 0.78f, 0.76f), 0.62f, new Vector2(6f, 3f));
        _matCheckerTile = CreateTexturedMat("UG_CheckerTile", checkerTex, new Color(0.62f, 0.66f, 0.64f), 0.54f, new Vector2(10f, 10f));

        _matWoodOak = CreateTexturedMat("UG_WoodOak", woodTex, new Color(0.48f, 0.32f, 0.19f), 0.26f, new Vector2(3f, 2f));
        _matWoodDark = CreateTexturedMat("UG_WoodDark", woodTex, new Color(0.28f, 0.18f, 0.11f), 0.32f, new Vector2(3f, 2f));
        _matWoodPlywood = CreateTexturedMat("UG_WoodPlywood", woodTex, new Color(0.58f, 0.45f, 0.29f), 0.18f, new Vector2(2f, 2f));

        _matMetalBulkhead = CreateTexturedMat("UG_MetalBulkhead", metalTex, new Color(0.30f, 0.35f, 0.37f), 0.52f, new Vector2(3f, 2f));
        _matRustedSteel = CreateTexturedMat("UG_RustedSteel", metalTex, new Color(0.34f, 0.24f, 0.18f), 0.36f, new Vector2(3f, 3f));
        _matDarkIron = CreateTexturedMat("UG_DarkIron", metalTex, new Color(0.15f, 0.17f, 0.19f), 0.58f, new Vector2(2f, 2f));
        _matCopperPipe = CreateTexturedMat("UG_CopperPipe", metalTex, new Color(0.58f, 0.36f, 0.22f), 0.70f, new Vector2(2f, 1f));
        _matBrassTrim = CreateMat("UG_BrassTrim", new Color(0.76f, 0.60f, 0.24f), 0.78f);
        _matHazardStripes = CreateTexturedMat("UG_HazardStripes", hazardTex, Color.white, 0.28f, new Vector2(4f, 1f));

        _matAcousticFoam = CreateTexturedMat("UG_AcousticFoam", foamTex, new Color(0.22f, 0.23f, 0.26f), 0.04f, new Vector2(6f, 6f));
        _matAcousticFoamDark = CreateTexturedMat("UG_AcousticFoamDark", foamTex, new Color(0.13f, 0.14f, 0.17f), 0.03f, new Vector2(6f, 6f));

        _matGlassClear = CreateGlassMat("UG_GlassClear", new Color(0.56f, 0.78f, 0.76f, 0.24f));
        _matGlassFrosted = CreateGlassMat("UG_GlassFrosted", new Color(0.60f, 0.85f, 0.94f, 0.42f));

        _matRetroBeige = CreateMat("UG_RetroBeige", new Color(0.62f, 0.60f, 0.52f), 0.32f);
        _matMedicalWhite = CreateMat("UG_MedicalWhite", new Color(0.84f, 0.87f, 0.86f), 0.48f);
        _matCarpetSoviet = CreateMat("UG_CarpetSoviet", new Color(0.46f, 0.14f, 0.13f), 0.08f);
        _matFabricCurtain = CreateMat("UG_FabricCurtain", new Color(0.64f, 0.76f, 0.72f), 0.06f);
        _matLeatherDark = CreateMat("UG_LeatherDark", new Color(0.16f, 0.13f, 0.12f), 0.42f);
        _matWaterPuddle = CreateMat("UG_WaterPuddle", new Color(0.08f, 0.12f, 0.14f), 0.96f);
        _matCeilingGrid = CreateMat("UG_CeilingGrid", new Color(0.26f, 0.28f, 0.27f), 0.12f);
        _matWallLowerGreen = CreateMat("UG_WallLowerGreen", new Color(0.20f, 0.31f, 0.27f), 0.32f);
        _matWallUpperCream = CreateMat("UG_WallUpperCream", new Color(0.62f, 0.63f, 0.57f), 0.18f);

        _matBinderRed = CreateMat("UG_BinderRed", new Color(0.62f, 0.18f, 0.16f), 0.25f);
        _matBinderBlue = CreateMat("UG_BinderBlue", new Color(0.18f, 0.32f, 0.56f), 0.25f);
        _matBinderGreen = CreateMat("UG_BinderGreen", new Color(0.20f, 0.44f, 0.26f), 0.25f);
        _matBinderYellow = CreateMat("UG_BinderYellow", new Color(0.76f, 0.62f, 0.20f), 0.25f);

        _matCrtGreen = CreateEmissiveMat("UG_CRT_Green", new Color(0.32f, 0.78f, 0.42f), new Color(0.30f, 0.82f, 0.42f) * 1.7f);
        _matCrtAmber = CreateEmissiveMat("UG_CRT_Amber", new Color(0.92f, 0.60f, 0.14f), new Color(0.95f, 0.62f, 0.14f) * 1.8f);
        _matCrtBlue = CreateEmissiveMat("UG_CRT_Blue", new Color(0.25f, 0.70f, 0.96f), new Color(0.25f, 0.72f, 1.0f) * 1.8f);
        _matCrtRed = CreateEmissiveMat("UG_CRT_Red", new Color(0.92f, 0.22f, 0.16f), new Color(1.0f, 0.22f, 0.14f) * 1.9f);
        _matCrtStatic = CreateEmissiveMat("UG_CRT_Static", new Color(0.76f, 0.82f, 0.80f), new Color(0.65f, 0.74f, 0.72f) * 1.6f);

        _matLampCool = CreateEmissiveMat("UG_LampCool", Color.white, new Color(0.80f, 0.95f, 1.0f) * 2.6f);
        _matLampWarm = CreateEmissiveMat("UG_LampWarm", new Color(1f, 0.86f, 0.58f), new Color(1f, 0.82f, 0.48f) * 2.5f);
        _matLampAmber = CreateEmissiveMat("UG_LampAmber", new Color(1f, 0.66f, 0.18f), new Color(1f, 0.62f, 0.14f) * 3.0f);
        _matLampRed = CreateEmissiveMat("UG_LampRed", Color.red, new Color(1f, 0.16f, 0.12f) * 3.2f);
        _matLampCyan = CreateEmissiveMat("UG_LampCyan", new Color(0.35f, 0.88f, 1f), new Color(0.32f, 0.88f, 1f) * 2.8f);
    }

    private static Texture2D GenerateConcreteTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = 0.66f + (Mathf.PerlinNoise(x * 0.06f, y * 0.06f) - 0.5f) * 0.28f;
                if ((x % 64) < 2 || (y % 64) < 2) v *= 0.62f;
                tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateTileTexture(int w, int h, int cell)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool grout = (x % cell < 2) || (y % cell < 2);
                float v = grout ? 0.38f : 0.82f + (Mathf.PerlinNoise(x * 0.1f, y * 0.1f) - 0.5f) * 0.10f;
                tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateCheckerTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool dark = ((x / 32) + (y / 32)) % 2 == 0;
                bool seam = (x % 32 < 2) || (y % 32 < 2);
                float v = seam ? 0.22f : (dark ? 0.46f : 0.80f);
                tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateWoodTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = (y % 32 < 2) ? 0.38f : 0.62f + Mathf.PerlinNoise(x * 0.03f, y * 0.24f) * 0.28f;
                tex.SetPixel(x, y, new Color(v, v * 0.90f, v * 0.80f, 1f));
            }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateHazardTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool yellow = ((x + y) / 24) % 2 == 0;
                tex.SetPixel(x, y, yellow ? new Color(0.90f, 0.70f, 0.12f) : new Color(0.11f, 0.11f, 0.11f));
            }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateAcousticFoamTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs((x % 32) - 16) / 16f;
                float dy = Mathf.Abs((y % 32) - 16) / 16f;
                float pyramid = 1.0f - Mathf.Max(dx, dy);
                float v = 0.25f + pyramid * 0.55f;
                tex.SetPixel(x, y, new Color(v, v, v * 1.04f, 1f));
            }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateNoiseTexture(int w, int h, float minV, float maxV)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = Mathf.Lerp(minV, maxV, Mathf.PerlinNoise(x * 0.14f, y * 0.14f));
                tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        tex.Apply();
        return tex;
    }

    private static Material CreateMat(string name, Color col, float smoothness)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(sh) { name = name, color = col };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
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