Shader "URP/BoostPlatform_GradientStarsArrows_Rim"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.8, 0.2, 1)
        _TopGradientColor ("Top Gradient Color", Color) = (0.4, 1, 0.4, 1)
        _BottomGradientColor ("Bottom Gradient Color", Color) = (0.0, 0.3, 0.0, 1)
        _GradientMinY ("Gradient Min Y", Float) = 0.0
        _GradientMaxY ("Gradient Max Y", Float) = 1.0

        [HDR]_StarColor ("Star Color", Color) = (1, 1, 1, 1)
        _StarSize ("Star Size", Float) = 0.02
        _StarTwinkleSpeed ("Star Twinkle Speed", Float) = 5.0
        _StarDensity ("Star Density", Float) = 50.0
        [NoScaleOffset]_StarTex ("Star Texture", 2D) = "white" {}

        [NoScaleOffset]_ArrowTex ("Arrow Texture", 2D) = "white" {}
        _ArrowColor ("Arrow Color", Color) = (0.7, 1.0, 0.7, 1)
        [HDR]_ArrowGlowColor ("Arrow Glow Color", Color) = (1.0, 1.0, 1.0, 1)
        _ArrowGlowSpeed ("Arrow Glow Speed", Float) = 1.0
        _ArrowGlowDelay ("Arrow Glow Delay", Float) = 0.3
        _ArrowPaddingX ("Arrow Padding X", Float) = 0.1
        _ArrowPaddingY ("Arrow Padding Y", Float) = 0.1

        [HDR]_RimColor ("Rim Light Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Light Power", Float) = 3.0

        _ViewAngleGradientColor ("View Angle Gradient Color", Color) = (0.1, 0.1, 0.1, 1)
        _ViewAngleGradientStrength ("View Angle Gradient Strength", Range(0,1)) = 0.5

        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.3
        _ShadowStrength ("Edge Shadow Strength", Range(0,1)) = 0.3

        [HDR]_SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularPower ("Specular Sharpness", Float) = 64.0
        _SpecularIntensity ("Specular Intensity", Float) = 0.5

        _ReflectionTex ("Reflection Cubemap", CUBE) = "" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.4
        _ReflectionTint ("Reflection Tint", Color) = (1, 1, 1, 1)
        _ReflectionSharpness ("Reflection Sharpness", Range(0.1, 1)) = 0.6

        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineStrength ("Outline Strength", Range(0, 1)) = 0.3
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.2
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
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 objectPos   : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _TopGradientColor;
            float4 _BottomGradientColor;
            float _GradientMinY;
            float _GradientMaxY;

            float4 _StarColor;
            float _StarSize;
            float _StarTwinkleSpeed;
            float _StarDensity;
            TEXTURE2D(_StarTex);
            SAMPLER(sampler_StarTex);

            TEXTURE2D(_ArrowTex);
            SAMPLER(sampler_ArrowTex);
            float4 _ArrowColor;
            float4 _ArrowGlowColor;
            float _ArrowGlowSpeed;
            float _ArrowGlowDelay;
            float _ArrowPaddingX;
            float _ArrowPaddingY;

            float4 _RimColor;
            float _RimPower;

            float4 _ViewAngleGradientColor;
            float _ViewAngleGradientStrength;

            float _NoiseStrength;
            float _ShadowStrength;

            float4 _SpecularColor;
            float _SpecularPower;
            float _SpecularIntensity;

            TEXTURECUBE(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);
            float _ReflectionStrength;
            float4 _ReflectionTint;
            float _ReflectionSharpness;

            float4 _OutlineColor;
            float _OutlineStrength;
            float4 _GlowColor;
            float _GlowStrength;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.objectPos = v.positionOS.xyz;
                o.worldNormal = TransformObjectToWorldNormal(v.normalOS);
                o.positionHCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS.xyz));
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float heightNorm = saturate((i.objectPos.y - _GradientMinY) / max(0.0001, (_GradientMaxY - _GradientMinY)));
                float4 color = lerp(_BottomGradientColor, _TopGradientColor, heightNorm);

                float2 noiseUV = i.objectPos.xz * 0.5 + 0.5;
                float noise = frac(sin(dot(noiseUV, float2(12.9898, 78.233))) * 43758.5453);
                color.rgb *= lerp(1.0 - _NoiseStrength, 1.0 + _NoiseStrength, noise);

                if (normal.y > 0.9)
                {
                    float2 uv = i.objectPos.xz * 0.5 + 0.5;
                    float2 paddedUV = float2(
                        lerp(_ArrowPaddingX, 1 - _ArrowPaddingX, uv.x),
                        lerp(_ArrowPaddingY, 1 - _ArrowPaddingY, uv.y)
                    );

                    float edgeFalloff = smoothstep(0.4, 0.0, abs(paddedUV.x - 0.5) + abs(paddedUV.y - 0.5));
                    color.rgb *= lerp(1.0, 1.0 - _ShadowStrength, edgeFalloff);

                    float starSum = 0;
                    for (int j = 0; j < 100; j++)
                    {
                        if (j >= _StarDensity) break;
                        float2 pos = float2(frac(sin(j * 12.9898) * 43758.5453), frac(cos(j * 78.233) * 12345.678));
                        float d = length(paddedUV - pos);
                        float twinkle = 0.5 + 0.5 * sin(_Time.y * _StarTwinkleSpeed + j);
                        float star = smoothstep(_StarSize * twinkle, 0.0, d);
                        float2 texUV = paddedUV - pos + 0.5;
                        float starTex = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, texUV).r;
                        starSum += star * starTex;
                    }
                    color.rgb += _StarColor.rgb * starSum;

                    for (int idx = 0; idx < 3; idx++)
                    {
                        float fy = (float)idx / 2.0;
                        float2 arrowUV = float2(paddedUV.x, paddedUV.y * 3.0 - fy);
                        float glowT = (_Time.y - idx * _ArrowGlowDelay) * _ArrowGlowSpeed;
                        float glowPhase = 0.5 + 0.5 * sin(glowT * 3.14159265);
                        float4 tex = SAMPLE_TEXTURE2D(_ArrowTex, sampler_ArrowTex, arrowUV);
                        float4 arrow = lerp(_ArrowColor, _ArrowGlowColor, glowPhase);
                        color.rgb = lerp(color.rgb, arrow.rgb, tex.a);
                    }
                }

                float3 viewDir = normalize(_WorldSpaceCameraPos - TransformObjectToWorld(i.objectPos));

                float rim = 1.0 - saturate(dot(normal, viewDir));
                rim = pow(rim, _RimPower);
                color.rgb += rim * _RimColor.rgb * _RimColor.a;

                float viewGradient = 1.0 - saturate(dot(normal, viewDir));
                color.rgb = lerp(color.rgb, _ViewAngleGradientColor.rgb, viewGradient * _ViewAngleGradientStrength);

                float3 lightDir = normalize(_MainLightPosition.xyz);
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), _SpecularPower);
                color.rgb += spec * _SpecularColor.rgb * _SpecularIntensity;

                float3 reflectDir = reflect(-viewDir, normal);
                float3 reflection = SAMPLE_TEXTURECUBE(_ReflectionTex, sampler_ReflectionTex, reflectDir);
                reflection *= _ReflectionTint.rgb;

                reflection = lerp(color.rgb, reflection, _ReflectionSharpness);
                color.rgb = lerp(color.rgb, reflection, _ReflectionStrength);

                float outline = pow(1.0 - saturate(dot(normal, viewDir)), 2.0);
                color.rgb = lerp(color.rgb, _OutlineColor.rgb, outline * _OutlineStrength);

                float glow = pow(1.0 - saturate(dot(normal, viewDir)), 4.0);
                color.rgb += glow * _GlowColor.rgb * _GlowStrength;

                return color;
            }
            ENDHLSL
        }
    }
}
