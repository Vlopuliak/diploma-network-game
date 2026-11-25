#if UNITY_EDITOR
using TheCopy.Shared;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Автоматичний установник Пазлу №1 у Зоні 4 (Акустична лабораторія):
///   - Осцилограф «С1-87» на пульті звукорежисерської (-28.8, -3.55, 65.5)
///   - 4 мережеві генератори тонів у 4 кабінах матеріалів (Бетон / Дерево / Метал / Поролон)
///   - Гермошлюз входу до Серверної Зони 8 (-27.5, -2.9, 74.0)
/// </summary>
public static class AcousticPuzzleSetupEditor
{
    private const float MainY = -4.5f;

    [MenuItem("The Copy/11. Встановити ПАЗЛ №1 (Акустичний Осцилограф + 4 Кабіни + Шлюз Серверної)")]
    public static void SetupAcousticResonancePuzzle()
    {
        GameObject old = GameObject.Find("Zone4_AcousticResonance_PuzzleSystem");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
        }

        GameObject root = new GameObject("Zone4_AcousticResonance_PuzzleSystem");
        Undo.RegisterCreatedObjectUndo(root, "Setup Zone 4 Acoustic Puzzle");

        // 1. Гермошлюз між Зоною 4 (Акустика) та Зоною 8 (Серверна) на X = -27.5, Z = 74.0
        GameObject gateObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gateObj.name = "Zone4_To_ServerHall_SecurityGate";
        gateObj.transform.SetParent(root.transform);
        gateObj.transform.position = new Vector3(-27.5f, MainY + 1.65f, 74.0f);
        gateObj.transform.localScale = new Vector3(5.2f, 3.3f, 0.45f);
        gateObj.GetComponent<Renderer>().sharedMaterial = CreateMat("Mat_AcousticGate", new Color(0.22f, 0.27f, 0.28f));
        gateObj.AddComponent<NetworkObject>();
        NetworkAirlockDoor gateDoor = gateObj.AddComponent<NetworkAirlockDoor>();

        // 2. Головний ЕПТ-осцилограф «С1-87» на пульті у Звукорежисерській (-28.8, -3.5, 65.5)
        GameObject scopeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scopeObj.name = "Acoustic_Oscilloscope_S1_87";
        scopeObj.transform.SetParent(root.transform);
        scopeObj.transform.position = new Vector3(-28.4f, MainY + 1.15f, 65.5f);
        scopeObj.transform.localScale = new Vector3(0.68f, 0.56f, 0.85f);
        scopeObj.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMat("Mat_OscilloscopeGreen", new Color(0.25f, 0.85f, 0.42f));
        scopeObj.AddComponent<NetworkObject>();
        NetworkAcousticOscilloscope oscilloscope = scopeObj.AddComponent<NetworkAcousticOscilloscope>();

        // 3. Чотири акустичні генератори всередині 4 кабін матеріалів Зони 4
        NetworkAcousticEmitterNode[] emitters = new NetworkAcousticEmitterNode[4];
        emitters[0] = CreateEmitterNode(root.transform, "AcousticEmitter_Booth1_Concrete", new Vector3(-45.5f, MainY + 1.45f, 49.6f), "Кабіна 1 (Бетон)", AcousticMaterialType.Concrete, 1.5f, 0.30f, oscilloscope);
        emitters[1] = CreateEmitterNode(root.transform, "AcousticEmitter_Booth2_Wood", new Vector3(-41.5f, MainY + 1.45f, 49.6f), "Кабіна 2 (Дерево)", AcousticMaterialType.Wood, 3.5f, 0.62f, oscilloscope);
        emitters[2] = CreateEmitterNode(root.transform, "AcousticEmitter_Booth3_Metal", new Vector3(-37.5f, MainY + 1.45f, 49.6f), "Кабіна 3 (Метал)", AcousticMaterialType.Metal, 2.5f, 0.42f, oscilloscope);
        emitters[3] = CreateEmitterNode(root.transform, "AcousticEmitter_Booth4_Foam", new Vector3(-33.5f, MainY + 1.45f, 49.6f), "Кабіна 4 (Поролон)", AcousticMaterialType.AcousticFoam, 1.0f, 0.14f, oscilloscope);

        // 4. Прив'язуємо всі посилання до NetworkAcousticOscilloscope через SerializedObject
        SerializedObject soScope = new SerializedObject(oscilloscope);
        soScope.FindProperty("targetSecurityGate").objectReferenceValue = gateDoor;

        SerializedProperty emittersProp = soScope.FindProperty("boothEmitters");
        emittersProp.arraySize = emitters.Length;
        for (int i = 0; i < emitters.Length; i++)
        {
            emittersProp.GetArrayElementAtIndex(i).objectReferenceValue = emitters[i];
        }
        soScope.ApplyModifiedProperties();

        Selection.activeGameObject = root;
        Debug.Log("[AcousticPuzzleSetupEditor] Пазл №1 (Осцилограф С1-87 + 4 акустичні кабіни + Шлюз Серверної) успішно встановлено!");
    }

    private static NetworkAcousticEmitterNode CreateEmitterNode(
        Transform parent,
        string objName,
        Vector3 pos,
        string title,
        AcousticMaterialType matType,
        float freq,
        float amp,
        NetworkAcousticOscilloscope masterScope)
    {
        GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nodeObj.name = objName;
        nodeObj.transform.SetParent(parent);
        nodeObj.transform.position = pos;
        nodeObj.transform.localScale = new Vector3(0.45f, 0.35f, 0.35f);
        nodeObj.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMat("Mat_" + objName, new Color(0.85f, 0.55f, 0.15f));
        nodeObj.AddComponent<NetworkObject>();

        NetworkAcousticEmitterNode emitter = nodeObj.AddComponent<NetworkAcousticEmitterNode>();
        SerializedObject so = new SerializedObject(emitter);
        so.FindProperty("boothTitle").stringValue = title;
        so.FindProperty("boothMaterial").enumValueIndex = (int)matType;
        so.FindProperty("harmonicFrequency").floatValue = freq;
        so.FindProperty("effectiveAmplitude").floatValue = amp;
        so.FindProperty("masterOscilloscope").objectReferenceValue = masterScope;
        so.FindProperty("statusLampRenderer").objectReferenceValue = nodeObj.GetComponent<Renderer>();
        so.ApplyModifiedProperties();

        return emitter;
    }

    private static Material CreateMat(string name, Color col)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(sh) { name = name, color = col };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        return m;
    }

    private static Material CreateEmissiveMat(string name, Color col)
    {
        Material m = CreateMat(name, col);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col * 1.4f);
        return m;
    }
}
#endif