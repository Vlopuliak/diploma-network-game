#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Виправляє помилку білду "The file 'level0' is corrupted! [Position out of bounds!]".
/// Виносить усі процедурно згенеровані Texture2D та Material зі сцени у реальні файли
/// в папці Assets/GeneratedAssets/ (PNG + MAT), а також прибирає Missing Scripts.
/// </summary>
public static class BuildSceneFixer
{
    private const string RootFolder = "Assets/GeneratedAssets";
    private const string TexFolder = "Assets/GeneratedAssets/Textures";
    private const string MatFolder = "Assets/GeneratedAssets/Materials";

    [MenuItem("The Copy/13. ПІДГОТУВАТИ СЦЕНУ ДО БІЛДУ (Зберегти матеріали як асети)")]
    public static void FixSceneForBuild()
    {
        EnsureFolder("Assets", "GeneratedAssets");
        EnsureFolder(RootFolder, "Textures");
        EnsureFolder(RootFolder, "Materials");

        Dictionary<Texture2D, Texture2D> savedTextures = new Dictionary<Texture2D, Texture2D>();
        Dictionary<Material, Material> savedMaterials = new Dictionary<Material, Material>();

        int texturesSaved = 0;
        int materialsSaved = 0;
        int renderersUpdated = 0;
        int missingScriptsRemoved = 0;

        // 1. Прибираємо биті/порожні Missing Scripts (наприклад, на AcousticZone_*), щоб не було попереджень при білді
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in allObjects)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            missingScriptsRemoved += removed;
        }

        // 2. Збираємо всі Renderer у сцені й виносимо незбережені процедурні текстури та матеріали на диск
        Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        try
        {
            for (int rIdx = 0; rIdx < allRenderers.Length; rIdx++)
            {
                Renderer r = allRenderers[rIdx];
                if (r == null) continue;

                if (rIdx % 200 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "Підготовка сцени до білду",
                        $"Обробка об'єктів: {rIdx} / {allRenderers.Length}",
                        rIdx / (float)Mathf.Max(1, allRenderers.Length)
                    );
                }

                Material[] mats = r.sharedMaterials;
                bool changed = false;

                for (int mIdx = 0; mIdx < mats.Length; mIdx++)
                {
                    Material mat = mats[mIdx];
                    if (mat == null) continue;

                    // Пропускаємо матеріали, які вже є реальними файлами в Assets або вбудованими асетами
                    if (AssetDatabase.Contains(mat)) continue;

                    if (!savedMaterials.TryGetValue(mat, out Material assetMat))
                    {
                        // Спочатку зберігаємо процедурну текстуру, якщо вона є в матеріалі
                        Texture2D mainTex = mat.mainTexture as Texture2D;
                        Texture2D baseTex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") as Texture2D : null;
                        Texture2D procTex = baseTex != null ? baseTex : mainTex;

                        Texture2D savedTex = null;
                        if (procTex != null && !AssetDatabase.Contains(procTex))
                        {
                            if (!savedTextures.TryGetValue(procTex, out savedTex))
                            {
                                savedTex = SaveProceduralTextureToPng(procTex, texturesSaved);
                                if (savedTex != null)
                                {
                                    savedTextures[procTex] = savedTex;
                                    texturesSaved++;
                                }
                            }
                        }

                        // Створюємо копію матеріалу й зберігаємо як .mat файл
                        assetMat = new Material(mat);
                        if (savedTex != null)
                        {
                            Vector2 scale = mat.mainTextureScale;
                            assetMat.mainTexture = savedTex;
                            assetMat.mainTextureScale = scale;
                            if (assetMat.HasProperty("_BaseMap"))
                            {
                                assetMat.SetTexture("_BaseMap", savedTex);
                                assetMat.SetTextureScale("_BaseMap", scale);
                            }
                        }

                        string safeMatName = SanitizeFileName(string.IsNullOrEmpty(mat.name) ? $"ProcMat_{materialsSaved}" : mat.name);
                        string matPath = AssetDatabase.GenerateUniqueAssetPath($"{MatFolder}/{safeMatName}.mat");
                        AssetDatabase.CreateAsset(assetMat, matPath);

                        savedMaterials[mat] = assetMat;
                        materialsSaved++;
                    }

                    mats[mIdx] = assetMat;
                    changed = true;
                }

                if (changed)
                {
                    Undo.RecordObject(r, "Assign Persistent Material Asset");
                    r.sharedMaterials = mats;
                    renderersUpdated++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 3. Очищаємо процедурний skybox у RenderSettings (його на старті створить SunsetAtmosphereController)
        if (RenderSettings.skybox != null && !AssetDatabase.Contains(RenderSettings.skybox))
        {
            RenderSettings.skybox = null;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            $"[BuildSceneFixer] ГОТОВО! Збережено текстур: {texturesSaved}, матеріалів: {materialsSaved}, " +
            $"оновлено рендерерів: {renderersUpdated}, видалено Missing Scripts: {missingScriptsRemoved}. " +
            "Тепер натисни Ctrl+S і збери білд у нову порожню папку!"
        );
    }

    private static Texture2D SaveProceduralTextureToPng(Texture2D sourceTex, int index)
    {
        try
        {
            Texture2D readable = sourceTex;
            if (!sourceTex.isReadable)
            {
                RenderTexture rt = RenderTexture.GetTemporary(sourceTex.width, sourceTex.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(sourceTex, rt);
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                readable = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, sourceTex.width, sourceTex.height), 0, 0);
                readable.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }

            byte[] png = readable.EncodeToPNG();
            if (readable != sourceTex) Object.DestroyImmediate(readable);
            if (png == null || png.Length == 0) return null;

            string safeName = SanitizeFileName(string.IsNullOrEmpty(sourceTex.name) ? $"ProcTex_{index}" : sourceTex.name);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{TexFolder}/{safeName}_{index}.png");
            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
        catch
        {
            return null;
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static string SanitizeFileName(string raw)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            raw = raw.Replace(c, '_');
        }
        return raw.Replace(' ', '_').Replace('.', '_');
    }
}
#endif