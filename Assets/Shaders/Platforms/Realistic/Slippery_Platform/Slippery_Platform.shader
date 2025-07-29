Shader "URP/SlipperyPlatform_CartoonIce_DualNormal"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _OverlayTex ("Overlay Texture", 2D) = "white" {}
        _MainColor ("Main Color", Color) = (0.6, 0.9, 1.0, 1)
        _ShadowColor ("Shadow Color", Color) = (0.2, 0.4, 0.6, 1)
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _SpecularPower ("Specular Power", Float) = 32.0

        _RimColor ("Rim Color", Color) = (0.7, 0.9, 1.0, 1)
        _RimPower ("Rim Power", Float) = 3.0

        _GradientTop ("Gradient Top Color", Color) = (0.8, 1.0, 1.0, 1)
        _GradientBottom ("Gradient Bottom Color", Color) = (0.3, 0.5, 0.6, 1)
        _GradientMinY ("Gradient Min Y", Float) = 0.0
        _GradientMaxY ("Gradient Max Y", Float) = 1.0

        _MainTexTint ("Main Texture Tint", Color) = (1, 1, 1, 1)
        _OverlayTexTint ("Overlay Texture Tint", Color) = (1, 1, 1, 1)

        _GlowFlashColor ("Glow Flash Color", Color) = (2, 2, 2, 1)
        _GlowFlashDuration ("Glow Flash Duration", Float) = 3.0
        _GlowFlashSize ("Glow Flash Size", Float) = 0.15

        _ViewAngleGradientColor ("View Angle Gradient Color", Color) = (1, 1, 1, 1)
        _ViewAngleGradientStrength ("View Angle Gradient Strength", Float) = 0.3
        _NoiseStrength ("Noise Strength", Float) = 0.1
        _ShadowStrength ("Edge Shadow Strength", Float) = 0.3

        _ReflectionTex ("Reflection Cubemap", CUBE) = "" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5
        _ReflectionTint ("Reflection Tint", Color) = (1, 1, 1, 1)
        _ReflectionSmoothness ("Reflection Sharpness", Range(0.1, 1)) = 0.7

        _DynamicReflectionTex ("Dynamic Reflection", 2D) = "black" {}

        _NormalMap1 ("Normal Map 1 (Base)", 2D) = "bump" {}
        _NormalMap2 ("Normal Map 2 (Detail)", 2D) = "bump" {}
        _NormalMap2_Tiling ("Detail Tiling", Float) = 3.0
        _NormalMap2_Strength ("Detail Strength", Range(0, 1)) = 0.4

        _AOMap ("AO Map", 2D) = "white" {}
        _AOMap2 ("AO Map 2 (Detail)", 2D) = "white" {}
        _AOMap2_Tiling ("AO2 Tiling", Float) = 3.0
        _AOMap2_Strength ("AO2 Strength", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 objectNormal : TEXCOORD3;
                float3x3 TBN : TEXCOORD4;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_OverlayTex); SAMPLER(sampler_OverlayTex);
            TEXTURE2D(_NormalMap1); SAMPLER(sampler_NormalMap1);
            TEXTURE2D(_NormalMap2); SAMPLER(sampler_NormalMap2);
            TEXTURE2D(_AOMap); SAMPLER(sampler_AOMap);
            TEXTURE2D(_AOMap2); SAMPLER(sampler_AOMap2);
            TEXTURE2D(_DynamicReflectionTex); SAMPLER(sampler_DynamicReflectionTex);
            TEXTURECUBE(_ReflectionTex); SAMPLER(sampler_ReflectionTex);

            float4 _MainColor, _ShadowColor, _HighlightColor;
            float _SpecularPower;
            float4 _RimColor;
            float _RimPower;

            float4 _GradientTop, _GradientBottom;
            float _GradientMinY, _GradientMaxY;

            float4 _MainTexTint, _OverlayTexTint;
            float4 _GlowFlashColor;
            float _GlowFlashDuration, _GlowFlashSize;

            float4 _ViewAngleGradientColor;
            float _ViewAngleGradientStrength;
            float _NoiseStrength;
            float _ShadowStrength;

            float _ReflectionStrength;
            float4 _ReflectionTint;
            float _ReflectionSmoothness;

            float _NormalMap2_Tiling;
            float _NormalMap2_Strength;
            float _AOMap2_Tiling;
            float _AOMap2_Strength;

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 worldNormal = TransformObjectToWorldNormal(v.normalOS);
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);

                float3 up = float3(0, 1, 0);
                float3 tangent = normalize(cross(up, worldNormal));
                float3 bitangent = cross(worldNormal, tangent);
                o.TBN = float3x3(tangent, bitangent, worldNormal);

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.worldPos = worldPos;
                o.worldNormal = worldNormal;
                o.uv = v.uv;
                o.objectNormal = v.normalOS;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 finalColor = float3(0, 0, 0);

                bool isTopFace = i.objectNormal.y > 0.9;

                if (isTopFace)
                {
                    float2 uv1 = i.uv;
                    float2 uv2 = i.uv * _NormalMap2_Tiling;

                    float3 normalTS1 = SAMPLE_TEXTURE2D(_NormalMap1, sampler_NormalMap1, uv1).xyz * 2.0 - 1.0;
                    float3 normalTS2 = SAMPLE_TEXTURE2D(_NormalMap2, sampler_NormalMap2, uv2).xyz * 2.0 - 1.0;
                    normalTS2.xy *= _NormalMap2_Strength;
                    normalTS2.z = sqrt(saturate(1.0 - dot(normalTS2.xy, normalTS2.xy)));
                    float3 combinedTS = normalize(normalTS1 + normalTS2);

                    float3 N = normalize(mul(combinedTS, i.TBN));

                    float ao1 = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, i.uv).r;
                    float ao2 = SAMPLE_TEXTURE2D(_AOMap2, sampler_AOMap2, i.uv * _AOMap2_Tiling).r;
                    float aoCombined = lerp(1.0, ao2, _AOMap2_Strength) * ao1;

                    float3 L = normalize(_MainLightPosition.xyz);
                    float NdotL = dot(N, L);
                    float3 lightIntensity = NdotL > 0.5 ? _MainColor.rgb : _ShadowColor.rgb;
                    lightIntensity = lerp(lightIntensity, _HighlightColor.rgb, pow(saturate(NdotL), _SpecularPower)) * aoCombined;

                    float rim = pow(1.0 - saturate(dot(N, viewDir)), _RimPower);

                    float heightT = saturate((i.worldPos.y - _GradientMinY) / max(0.0001, (_GradientMaxY - _GradientMinY)));
                    float3 gradientColor = lerp(_GradientBottom.rgb, _GradientTop.rgb, heightT);

                    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _MainTexTint;
                    float4 overlayColor = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, i.uv) * _OverlayTexTint;

                    float2 noiseUV = i.uv;
                    float noise = frac(sin(dot(noiseUV, float2(12.9898, 78.233))) * 43758.5453);
                    texColor.rgb *= lerp(1.0 - _NoiseStrength, 1.0 + _NoiseStrength, noise);

                    float edgeFalloff = smoothstep(0.4, 0.0, abs(i.uv.x - 0.5) + abs(i.uv.y - 0.5));
                    texColor.rgb *= lerp(1.0, 1.0 - _ShadowStrength, edgeFalloff);

                    float viewDot = dot(normalize(i.worldNormal), viewDir);
                    float viewAngleEffect = pow(1.0 - saturate(viewDot), 2.0);
                    float3 viewGradient = _ViewAngleGradientColor.rgb * viewAngleEffect * _ViewAngleGradientStrength;

                    finalColor = gradientColor * lightIntensity * texColor.rgb;
                    finalColor += overlayColor.rgb * overlayColor.a;
                    finalColor += rim * _RimColor.rgb * _RimColor.a * aoCombined;
                    finalColor += viewGradient;

                    float2 reflectUV = i.uv;
                    reflectUV.y = 1.0 - reflectUV.y;
                    float3 dynReflection = SAMPLE_TEXTURE2D(_DynamicReflectionTex, sampler_DynamicReflectionTex, reflectUV).rgb;
                    dynReflection *= _ReflectionTint.rgb;
                    finalColor = lerp(finalColor, dynReflection, _ReflectionStrength);

                    float time = _Time.y;
                    float flashT = frac(time / _GlowFlashDuration);
                    float2 center = float2(0.5, 0.5);
                    float dist = distance(i.uv, center);
                    float flash = smoothstep(_GlowFlashSize, 0.0, abs(dist - flashT));
                    finalColor += flash * _GlowFlashColor.rgb;
                }
                else
                {
                    float3 baseLight = lerp(_ShadowColor.rgb, _MainColor.rgb, saturate(dot(i.worldNormal, viewDir)));
                    float3 gradientColor = lerp(_GradientBottom.rgb, _GradientTop.rgb, saturate((i.worldPos.y - _GradientMinY) / max(0.0001, (_GradientMaxY - _GradientMinY))));
                    finalColor = gradientColor * baseLight;
                }

                return float4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
