using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class SunsetAtmosphereController : MonoBehaviour
{
    [Header("1. Вечірнє Сонце (Golden Hour попереду над комплексом)")]
    [SerializeField] private Light sunLight;
    [SerializeField] private Color sunsetSunColor = new Color(1.0f, 0.62f, 0.28f);
    [SerializeField] private float sunsetSunIntensity = 2.8f;

    [Tooltip("X = 14° (низьке вечірнє сонце, освітлює двір), Y = 158° (попереду над комплексом)")]
    [SerializeField] private Vector3 sunsetSunEulerAngles = new Vector3(14f, 158f, 0f);

    [Header("2. Кольори вечірнього неба та хмар")]
    [SerializeField] private Color zenithDeepBlue = new Color(0.10f, 0.14f, 0.34f);
    [SerializeField] private Color upperCrimson = new Color(0.56f, 0.21f, 0.36f);
    [SerializeField] private Color midAmberPink = new Color(0.96f, 0.40f, 0.18f);
    [SerializeField] private Color horizonGold = new Color(1.0f, 0.74f, 0.28f);
    [Range(0f, 1f)]
    [SerializeField] private float cloudCoverage = 0.56f;
    [Range(0.2f, 2.5f)]
    [SerializeField] private float skyExposure = 1.15f;

    [Header("3. Сонячні промені (Screen-Space God Rays)")]
    [SerializeField] private bool enableGodRays = true;
    [Range(0f, 0.5f)]
    [SerializeField] private float maxRayBrightness = 0.22f;

    [Header("4. Туман та перехід у темний підземний бункер")]
    [SerializeField] private Color surfaceFogColor = new Color(0.68f, 0.42f, 0.26f);
    [Tooltip("Менша щільність на поверхні, щоб було видно комплекс і ліс без замилення")]
    [Range(0f, 0.02f)]
    [SerializeField] private float surfaceFogDensity = 0.0032f;

    [SerializeField] private Color undergroundFogColor = new Color(0.06f, 0.09f, 0.10f);
    [Range(0f, 0.05f)]
    [SerializeField] private float undergroundFogDensity = 0.024f;
    [SerializeField] private float undergroundThresholdY = -2.2f;

    private Material _skyboxMat;
    private bool _usesCustomSunsetShader;
    private Texture2D _godRayTexture;
    private Volume _atmosphereVolume;
    private WhiteBalance _whiteBalance;
    private ColorAdjustments _colorAdjustments;
    private Camera _cachedPlayerCam;

    private float _undergroundBlend = 0f;
    private float _sunOcclusionFactor = 1f;

    private void OnEnable()
    {
        CleanupLegacySkyDome();
        FindAndForceEnableSunLight();
        SetupSunsetSkyboxMaterial();

        if (Application.isPlaying)
        {
            GenerateRadialGodRayTexture(512);
            SetupAtmospherePostProcessVolume();
        }

        _undergroundBlend = 0f;
        ApplyAtmosphereBlend(0f);
        DynamicGI.UpdateEnvironment();
    }

    private void OnValidate()
    {
        if (!enabled) return;
        FindAndForceEnableSunLight();
        SetupSunsetSkyboxMaterial();
        ApplyAtmosphereBlend(_undergroundBlend);
    }

    [ContextMenu("Застосувати закат у сцені зараз")]
    public void ForceRefreshSunsetInScene()
    {
        CleanupLegacySkyDome();
        FindAndForceEnableSunLight();
        SetupSunsetSkyboxMaterial();
        ApplyAtmosphereBlend(0f);
        DynamicGI.UpdateEnvironment();
    }

    private void CleanupLegacySkyDome()
    {
        GameObject oldDome = GameObject.Find("Sunset_Panoramic_SkyDome");
        if (oldDome != null)
        {
            if (Application.isPlaying)
                Destroy(oldDome);
            else
                DestroyImmediate(oldDome);
        }
    }

    private void FindAndForceEnableSunLight()
    {
        if (sunLight == null)
        {
            Light[] allLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Light l in allLights)
            {
                if (l.type == LightType.Directional)
                {
                    sunLight = l;
                    break;
                }
            }
        }

        if (sunLight == null)
        {
            GameObject sunObj = new GameObject("Sunset_Directional_Sun");
            sunLight = sunObj.AddComponent<Light>();
        }

        sunLight.gameObject.SetActive(true);
        sunLight.enabled = true;
        sunLight.type = LightType.Directional;
        sunLight.transform.rotation = Quaternion.Euler(sunsetSunEulerAngles);
        sunLight.color = sunsetSunColor;
        sunLight.intensity = sunsetSunIntensity;
        sunLight.shadows = LightShadows.Soft;
        sunLight.shadowStrength = 0.78f;

        RenderSettings.sun = sunLight;
    }

    private void SetupSunsetSkyboxMaterial()
    {
        Shader customShader = Shader.Find("TheCopy/SunsetSkyURP");

        if (customShader != null)
        {
            if (_skyboxMat == null || _skyboxMat.shader != customShader)
            {
                _skyboxMat = new Material(customShader)
                {
                    name = "Mat_TheCopy_SunsetSkyURP",
                    hideFlags = HideFlags.DontSave
                };
            }

            _usesCustomSunsetShader = true;
            UpdateCustomSkyboxProperties(0f);
            RenderSettings.skybox = _skyboxMat;
            return;
        }

        Shader procSky = Shader.Find("Skybox/Procedural");
        if (procSky != null)
        {
            if (_skyboxMat == null || _skyboxMat.shader != procSky)
            {
                _skyboxMat = new Material(procSky)
                {
                    name = "Mat_URP_ProceduralSunset_Fallback",
                    hideFlags = HideFlags.DontSave
                };
            }

            _usesCustomSunsetShader = false;
            _skyboxMat.SetFloat("_SunDisk", 2f);
            _skyboxMat.SetFloat("_SunSize", 0.055f);
            _skyboxMat.SetFloat("_SunSizeConvergence", 6f);
            _skyboxMat.SetFloat("_AtmosphereThickness", 2.1f);
            _skyboxMat.SetColor("_SkyTint", new Color(0.85f, 0.36f, 0.24f));
            _skyboxMat.SetColor("_GroundColor", new Color(0.22f, 0.14f, 0.12f));
            _skyboxMat.SetFloat("_Exposure", 1.35f);

            RenderSettings.skybox = _skyboxMat;
        }
    }

    private void UpdateCustomSkyboxProperties(float undergroundT)
    {
        if (!_usesCustomSunsetShader || _skyboxMat == null) return;

        Vector3 sunDir = -(Quaternion.Euler(sunsetSunEulerAngles) * Vector3.forward).normalized;

        _skyboxMat.SetColor("_ZenithColor", zenithDeepBlue);
        _skyboxMat.SetColor("_UpperSkyColor", upperCrimson);
        _skyboxMat.SetColor("_MidSkyColor", midAmberPink);
        _skyboxMat.SetColor("_HorizonColor", horizonGold);
        _skyboxMat.SetVector("_SunDirection", new Vector4(sunDir.x, sunDir.y, sunDir.z, 0f));
        _skyboxMat.SetFloat("_CloudCoverage", cloudCoverage);
        _skyboxMat.SetFloat("_Exposure", skyExposure);
        _skyboxMat.SetFloat("_UndergroundDim", undergroundT);
    }

    /// <summary>
    /// Знаходить активну камеру локального гравця навіть якщо вона має тег Untagged.
    /// </summary>
    private Camera GetActivePlayerCamera()
    {
        if (_cachedPlayerCam != null && _cachedPlayerCam.isActiveAndEnabled)
        {
            return _cachedPlayerCam;
        }

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            _cachedPlayerCam = Camera.main;
            return _cachedPlayerCam;
        }

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsConnectedClient &&
            NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            Camera playerCam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>(false);
            if (playerCam != null && playerCam.isActiveAndEnabled)
            {
                _cachedPlayerCam = playerCam;
                return _cachedPlayerCam;
            }
        }

        foreach (Camera c in Camera.allCameras)
        {
            if (c != null && c.isActiveAndEnabled && c.cameraType == CameraType.Game)
            {
                _cachedPlayerCam = c;
                return _cachedPlayerCam;
            }
        }

        return null;
    }

    private void ConfigureCameraForSunset(Camera cam)
    {
        if (cam == null) return;

        // Проставляємо тег MainCamera, щоб RetroAuditorHud, VoiceNoiseEmitter і God Rays бачили камеру
        if (!cam.CompareTag("MainCamera"))
        {
            cam.tag = "MainCamera";
        }

        // Прибираємо чорний SolidColor (залишений після тесту ліхтариків) і вмикаємо Skybox
        if (cam.clearFlags != CameraClearFlags.Skybox)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
        }

        if (cam.farClipPlane < 1000f)
        {
            cam.farClipPlane = 1200f;
        }

        UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData != null && !camData.renderPostProcessing)
        {
            camData.renderPostProcessing = true;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (RenderSettings.skybox != _skyboxMat && _skyboxMat != null)
            {
                RenderSettings.skybox = _skyboxMat;
            }
            UpdateCustomSkyboxProperties(0f);
            return;
        }

        Camera cam = GetActivePlayerCamera();
        ConfigureCameraForSunset(cam);

        Transform target = GetTrackedPlayerOrCamera(cam);
        float targetUnderground = (target != null && target.position.y < undergroundThresholdY) ? 1f : 0f;
        _undergroundBlend = Mathf.MoveTowards(_undergroundBlend, targetUnderground, 1.5f * Time.deltaTime);

        ApplyAtmosphereBlend(_undergroundBlend);
        UpdateSunOcclusion(cam);
    }

    private Transform GetTrackedPlayerOrCamera(Camera cam)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsConnectedClient &&
            NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            return NetworkManager.Singleton.LocalClient.PlayerObject.transform;
        }

        return cam != null ? cam.transform : null;
    }

    private void ApplyAtmosphereBlend(float t)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Color.Lerp(surfaceFogColor, undergroundFogColor, t);
        RenderSettings.fogDensity = Mathf.Lerp(surfaceFogDensity, undergroundFogDensity, t);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.58f, 0.34f, 0.42f), new Color(0.11f, 0.14f, 0.15f), t);
        RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.86f, 0.50f, 0.28f), new Color(0.08f, 0.11f, 0.12f), t);
        RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.26f, 0.18f, 0.15f), new Color(0.04f, 0.06f, 0.06f), t);

        RenderSettings.reflectionIntensity = Mathf.Lerp(0.65f, 0.12f, t);

        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(sunsetSunEulerAngles);
            sunLight.intensity = Mathf.Lerp(sunsetSunIntensity, 0.08f, t);
            sunLight.color = Color.Lerp(sunsetSunColor, new Color(0.40f, 0.55f, 0.68f), t);
        }

        UpdateCustomSkyboxProperties(t);

        if (_whiteBalance != null)
        {
            _whiteBalance.temperature.Override(Mathf.Lerp(26f, -20f, t));
            _whiteBalance.tint.Override(Mathf.Lerp(8f, -4f, t));
        }

        if (_colorAdjustments != null)
        {
            _colorAdjustments.colorFilter.Override(Color.Lerp(new Color(1.0f, 0.94f, 0.86f), new Color(0.84f, 0.93f, 0.96f), t));
            _colorAdjustments.saturation.Override(Mathf.Lerp(4f, -24f, t));
            _colorAdjustments.contrast.Override(Mathf.Lerp(14f, 24f, t));
            _colorAdjustments.postExposure.Override(Mathf.Lerp(0.25f, 0.0f, t));
        }
    }

    private void UpdateSunOcclusion(Camera cam)
    {
        if (cam == null || sunLight == null || _undergroundBlend > 0.85f)
        {
            _sunOcclusionFactor = 0f;
            return;
        }

        Vector3 sunDir = -sunLight.transform.forward;
        Vector3 origin = cam.transform.position + cam.transform.forward * 0.4f;

        Vector3 right = cam.transform.right * 0.65f;
        Vector3 up = cam.transform.up * 0.65f;
        Vector3[] offsets = { Vector3.zero, right, -right, up, -up };

        int clearRays = 0;
        for (int i = 0; i < offsets.Length; i++)
        {
            if (!Physics.Raycast(origin + offsets[i], sunDir, 180f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                clearRays++;
            }
        }

        float targetVis = clearRays / (float)offsets.Length;
        _sunOcclusionFactor = Mathf.Lerp(_sunOcclusionFactor, targetVis, 6f * Time.deltaTime);
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        if (!enableGodRays || _godRayTexture == null || Event.current.type != EventType.Repaint) return;
        if (_undergroundBlend > 0.85f || _sunOcclusionFactor <= 0.01f) return;

        Camera cam = GetActivePlayerCamera();
        if (cam == null || !cam.enabled || sunLight == null) return;

        Vector3 sunDir = -sunLight.transform.forward;
        float lookDot = Vector3.Dot(cam.transform.forward, sunDir);
        if (lookDot <= 0.05f) return;

        Vector3 sunWorldFar = cam.transform.position + sunDir * 800f;
        Vector3 screenPos = cam.WorldToScreenPoint(sunWorldFar);
        if (screenPos.z <= 0f) return;

        float guiX = screenPos.x;
        float guiY = Screen.height - screenPos.y;

        float angleFade = Mathf.SmoothStep(0.05f, 0.95f, lookDot);
        float surfaceFade = 1f - _undergroundBlend;
        float alpha = maxRayBrightness * angleFade * _sunOcclusionFactor * surfaceFade;

        if (alpha <= 0.002f) return;

        float size = Mathf.Max(Screen.width, Screen.height) * 1.35f;
        Rect rayRect = new Rect(guiX - size * 0.5f, guiY - size * 0.5f, size, size);

        Color prev = GUI.color;
        GUI.color = new Color(1.0f, 0.74f, 0.30f, alpha);
        GUI.DrawTexture(rayRect, _godRayTexture, ScaleMode.StretchToFill, true);
        GUI.color = prev;
    }

    private void SetupAtmospherePostProcessVolume()
    {
        if (_atmosphereVolume != null) return;

        Transform existing = transform.Find("Sunset_Atmosphere_Volume");
        GameObject volObj = existing != null ? existing.gameObject : new GameObject("Sunset_Atmosphere_Volume");
        volObj.transform.SetParent(transform);

        _atmosphereVolume = volObj.GetComponent<Volume>();
        if (_atmosphereVolume == null) _atmosphereVolume = volObj.AddComponent<Volume>();

        _atmosphereVolume.isGlobal = true;
        _atmosphereVolume.priority = 25f;
        _atmosphereVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        _whiteBalance = _atmosphereVolume.profile.Add<WhiteBalance>(true);
        _whiteBalance.active = true;

        _colorAdjustments = _atmosphereVolume.profile.Add<ColorAdjustments>(true);
        _colorAdjustments.active = true;

        Bloom bloom = _atmosphereVolume.profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(0.92f);
        bloom.intensity.Override(1.15f);
        bloom.scatter.Override(0.72f);
    }

    private void GenerateRadialGodRayTexture(int size)
    {
        if (_godRayTexture != null) return;

        _godRayTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _godRayTexture.wrapMode = TextureWrapMode.Clamp;
        _godRayTexture.filterMode = FilterMode.Bilinear;

        float half = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist >= 1f)
                {
                    _godRayTexture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float angle = Mathf.Atan2(dy, dx);
                float ray1 = Mathf.PerlinNoise(Mathf.Cos(angle) * 3.5f + 5f, Mathf.Sin(angle) * 3.5f + 5f);
                float ray2 = Mathf.PerlinNoise(Mathf.Cos(angle * 2f) * 6.0f + 12f, Mathf.Sin(angle * 2f) * 6.0f + 12f);
                float rayPattern = Mathf.Pow(Mathf.Clamp01(ray1 * 0.65f + ray2 * 0.45f), 1.6f);

                float radialFalloff = Mathf.Pow(1f - dist, 2.4f);
                float centerCorona = Mathf.Pow(Mathf.Max(0f, 1f - dist * 2.4f), 2.0f) * 0.75f;

                float alpha = Mathf.Clamp01((rayPattern * 0.85f + 0.15f) * radialFalloff + centerCorona);
                _godRayTexture.SetPixel(x, y, new Color(1f, 0.92f, 0.72f, alpha));
            }
        }

        _godRayTexture.Apply();
    }
}