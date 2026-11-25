#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Комплексний діагност і фіксер помилки "level0 is corrupted! [Position out of bounds!]" у Unity 6.
/// Автоматично очищає DontSave-посилання перед білдом (IPreprocessBuildWithReport),
/// прибирає Missing Scripts у сцені й префабах, та запускає CleanBuildCache.
/// </summary>
public class CleanBuildDiagnostic : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        SanitizeSceneBeforeBuild(false);
    }

    [MenuItem("The Copy/14. ЧИСТИЙ БІЛД У НОВУ ПАПКУ (Фікс level0 corrupted)")]
    public static void RunCleanBuildWithDiagnostics()
    {
        // 1. Повна санітаризація сцени й префабів
        SanitizeSceneBeforeBuild(true);

        // 2. Вибір нової порожньої папки для білду
        string folder = EditorUtility.OpenFolderPanel("Вибери ПОРОЖНЮ папку для чистого білду", "D:\\", "TheCopy_CleanBuild");
        if (string.IsNullOrEmpty(folder)) return;

        string exePath = Path.Combine(folder, "TheCopy.exe");

        // 3. Збираємо активні сцени
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.enabled) scenes.Add(s.path);
        }
        if (scenes.Count == 0)
        {
            scenes.Add(SceneManager.GetActiveScene().path);
        }

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            // CleanBuildCache примусово перезбирає всі бінарні файли level0 з нуля без старого кешу!
            options = BuildOptions.CleanBuildCache
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[CleanBuildDiagnostic] УСПІХ! Чистий білд зібрано в: {exePath} ({report.summary.totalSize / (1024 * 1024)} MB)");
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            Debug.LogError($"[CleanBuildDiagnostic] Помилка білду: {report.summary.result}");
        }
    }

    [MenuItem("The Copy/15. Діагностика сцени на биті посилання (Без білду)")]
    public static void RunDiagnosticsOnly()
    {
        SanitizeSceneBeforeBuild(true);
    }

    private static void SanitizeSceneBeforeBuild(bool saveSceneAfter)
    {
        int missingScene = 0;
        int missingPrefabs = 0;
        int clearedDontSave = 0;
        int fixedNan = 0;

        // 1. Чистимо Missing Scripts у всіх об'єктах сцени
        GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in sceneObjects)
        {
            if (go == null) continue;
            missingScene += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            // Перевіряємо Transform на NaN / Infinity
            Vector3 p = go.transform.localPosition;
            Vector3 s = go.transform.localScale;
            if (!IsFinite(p) || !IsFinite(s))
            {
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                fixedNan++;
                Debug.LogWarning($"[CleanBuildDiagnostic] Виправлено NaN Transform на '{go.name}'");
            }
        }

        // 2. Чистимо Missing Scripts у всіх префабах проєкту
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
            if (count > 0)
            {
                GameObject instance = PrefabUtility.LoadPrefabContents(path);
                foreach (Transform t in instance.GetComponentsInChildren<Transform>(true))
                {
                    missingPrefabs += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        // 3. Прибираємо посилання на DontSave / незбережені об'єкти в RenderSettings, Volume і Renderer
        if (RenderSettings.skybox != null && !AssetDatabase.Contains(RenderSettings.skybox))
        {
            RenderSettings.skybox = null;
            clearedDontSave++;
        }

        if (RenderSettings.customReflectionTexture != null && !AssetDatabase.Contains(RenderSettings.customReflectionTexture))
        {
            RenderSettings.customReflectionTexture = null;
            clearedDontSave++;
        }

        Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Volume v in volumes)
        {
            if (v.sharedProfile != null && !AssetDatabase.Contains(v.sharedProfile))
            {
                v.sharedProfile = null;
                clearedDontSave++;
            }
        }

        MeshFilter[] filters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MeshFilter mf in filters)
        {
            if (mf.sharedMesh != null && !AssetDatabase.Contains(mf.sharedMesh) && !mf.sharedMesh.name.StartsWith("Cube") && !mf.sharedMesh.name.StartsWith("Cylinder"))
            {
                Debug.LogWarning($"[CleanBuildDiagnostic] Незбережений Mesh '{mf.sharedMesh.name}' на об'єкті '{mf.name}'!");
            }
        }

        // 4. Обов'язково додаємо наші кастомні шейдери в Always Included Shaders, щоб URP не вирізав їх при білді
        EnsureAlwaysIncludedShader("TheCopy/SunsetSkyURP");
        EnsureAlwaysIncludedShader("TheCopy/WorldTextURP");
        EnsureAlwaysIncludedShader("Skybox/Procedural");

        if (saveSceneAfter)
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log(
            $"[CleanBuildDiagnostic] Перевірка завершена: видалено Missing Scripts у сцені={missingScene}, у префабах={missingPrefabs}, " +
            $"очищено DontSave={clearedDontSave}, виправлено NaN={fixedNan}."
        );
    }

    private static void EnsureAlwaysIncludedShader(string shaderName)
    {
        Shader sh = Shader.Find(shaderName);
        if (sh == null) return;

        var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        if (graphicsSettingsObj == null) return;

        SerializedObject so = new SerializedObject(graphicsSettingsObj);
        SerializedProperty prop = so.FindProperty("m_AlwaysIncludedShaders");
        if (prop == null) return;

        for (int i = 0; i < prop.arraySize; i++)
        {
            if (prop.GetArrayElementAtIndex(i).objectReferenceValue == sh)
                return;
        }

        int idx = prop.arraySize;
        prop.InsertArrayElementAtIndex(idx);
        prop.GetArrayElementAtIndex(idx).objectReferenceValue = sh;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    private static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
#endif