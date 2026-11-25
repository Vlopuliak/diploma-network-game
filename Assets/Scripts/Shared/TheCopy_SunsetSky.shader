Shader "TheCopy/SunsetSkyURP"
{
    Properties
    {
        [Header(Sky Gradient)]
        _ZenithColor ("Zenith Deep Blue", Color) = (0.10, 0.14, 0.34, 1)
        _UpperSkyColor ("Upper Twilight Crimson", Color) = (0.56, 0.21, 0.36, 1)
        _MidSkyColor ("Mid Amber Coral", Color) = (0.96, 0.40, 0.18, 1)
        _HorizonColor ("Horizon Gold", Color) = (1.0, 0.74, 0.28, 1)
        _GroundColor ("Ground Silhouette Tint", Color) = (0.14, 0.10, 0.11, 1)

        [Header(Sun Settings)]
        _SunDirection ("Sun Direction (To Sun)", Vector) = (-0.36, 0.24, 0.90, 0)
        _SunColor ("Sun Core Color", Color) = (1.0, 0.95, 0.80, 1)
        _SunHaloColor ("Sun Halo Color", Color) = (1.0, 0.52, 0.14, 1)
        _SunDiskSize ("Sun Disk Sharpness", Range(100, 1200)) = 420
        _SunIntensity ("Sun Glow Intensity", Range(0.5, 6.0)) = 2.8

        [Header(Sunset Clouds)]
        _CloudCoverage ("Cloud Coverage", Range(0.0, 1.0)) = 0.56
        _CloudSoftness ("Cloud Softness", Range(0.05, 0.6)) = 0.26
        _CloudSpeed ("Cloud Drift Speed", Range(0.0, 0.1)) = 0.012
        _CloudScale ("Cloud Scale", Range(0.5, 4.0)) = 1.45
        _CloudDarkColor ("Cloud Shadow Color", Color) = (0.20, 0.12, 0.24, 1)
        _CloudLitColor ("Cloud Rim Light Color", Color) = (1.0, 0.68, 0.26, 1)

        [Header(Atmosphere Blend)]
        _Exposure ("Sky Exposure", Range(0.2, 2.5)) = 1.15
        _UndergroundDim ("Underground Dimming", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "SunsetSkybox"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir    : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor;
                float4 _UpperSkyColor;
                float4 _MidSkyColor;
                float4 _HorizonColor;
                float4 _GroundColor;
                float4 _SunDirection;
                float4 _SunColor;
                float4 _SunHaloColor;
                float  _SunDiskSize;
                float  _SunIntensity;
                float  _CloudCoverage;
                float  _CloudSoftness;
                float  _CloudSpeed;
                float  _CloudScale;
                float4 _CloudDarkColor;
                float4 _CloudLitColor;
                float  _Exposure;
                float  _UndergroundDim;
            CBUFFER_END

            // Легкий 2D-шум без текстур для плавних вечірніх хмар
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float FbmClouds(float2 p)
            {
                float total = 0.0;
                float amp = 0.52;
                float2x2 rot = float2x2(0.80, -0.60, 0.60, 0.80);

                for (int i = 0; i < 5; i++)
                {
                    total += ValueNoise(p) * amp;
                    p = mul(rot, p) * 2.07 + float2(1.7, 9.2);
                    amp *= 0.48;
                }
                return total;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.viewDir = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.viewDir);
                float3 sunDir = normalize(_SunDirection.xyz);
                float y = dir.y;

                // 1. Багатошаровий градієнт вечірнього неба
                float3 skyCol;
                if (y < 0.0)
                {
                    float groundT = smoothstep(0.0, -0.25, y);
                    skyCol = lerp(_HorizonColor.rgb * 0.72, _GroundColor.rgb, groundT);
                }
                else if (y < 0.16)
                {
                    skyCol = lerp(_HorizonColor.rgb, _MidSkyColor.rgb, smoothstep(0.0, 0.16, y));
                }
                else if (y < 0.46)
                {
                    skyCol = lerp(_MidSkyColor.rgb, _UpperSkyColor.rgb, smoothstep(0.16, 0.46, y));
                }
                else
                {
                    skyCol = lerp(_UpperSkyColor.rgb, _ZenithColor.rgb, smoothstep(0.46, 0.95, y));
                }

                // 2. Сонячний диск та тепле атмосферне розсіювання (Mie scattering)
                float sunDot = max(0.0, dot(dir, sunDir));
                float horizonMask = smoothstep(-0.08, 0.03, y);

                float wideGlow = pow(sunDot, 3.0) * 0.48;
                float tightCorona = pow(sunDot, 28.0) * 0.95;
                float sunDisk = pow(sunDot, _SunDiskSize) * _SunIntensity * horizonMask;

                skyCol += _SunHaloColor.rgb * wideGlow * smoothstep(-0.15, 0.25, y);
                skyCol += lerp(_SunHaloColor.rgb, _SunColor.rgb, 0.55) * tightCorona * horizonMask;
                skyCol += _SunColor.rgb * sunDisk;

                // 3. Анімовані вечірні хмари з золотистою обводкою від сонця
                if (y > 0.015)
                {
                    float2 cloudUv = (dir.xz / (y + 0.18)) * _CloudScale;
                    cloudUv += float2(_Time.y * _CloudSpeed, _Time.y * _CloudSpeed * 0.35);

                    float baseNoise = FbmClouds(cloudUv);
                    float detailNoise = FbmClouds(cloudUv * 2.3 + float2(4.2, -3.1));
                    float cloudShape = baseNoise * 0.72 + detailNoise * 0.28;

                    float threshold = 1.0 - _CloudCoverage;
                    float density = smoothstep(threshold, threshold + _CloudSoftness, cloudShape);

                    // Плавне згасання хмар біля самого горизонту, щоб не було різкої лінії
                    float horizonFade = smoothstep(0.015, 0.16, y);
                    density *= horizonFade;

                    // Підсвічування хмар із боку сонця
                    float silverLining = pow(sunDot, 2.5) * (1.0 - density * 0.45);
                    float3 darkBase = lerp(_CloudDarkColor.rgb, _MidSkyColor.rgb * 0.65, pow(sunDot, 1.5) * 0.6);
                    float3 cloudColor = lerp(darkBase, _CloudLitColor.rgb, saturate(silverLining * 1.5 + 0.12));

                    // Додатковий гарячий відблиск прямо навколо сонця
                    cloudColor += _SunColor.rgb * pow(sunDot, 16.0) * (1.0 - density * 0.6) * 0.65;

                    skyCol = lerp(skyCol, cloudColor, density * 0.88);
                }

                // 4. Ледве помітні перші вечірні зірки високо в зеніті
                if (y > 0.35)
                {
                    float2 starGrid = floor((dir.xz / (y + 0.1)) * 140.0);
                    float starVal = Hash21(starGrid);
                    float starMask = step(0.9965, starVal) * smoothstep(0.35, 0.85, y) * (1.0 - sunDot);
                    skyCol += float3(0.85, 0.90, 1.0) * starMask * 0.55;
                }

                skyCol *= _Exposure;
                skyCol = lerp(skyCol, float3(0.03, 0.04, 0.05), _UndergroundDim);

                return half4(skyCol, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
