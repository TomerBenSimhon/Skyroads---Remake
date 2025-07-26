Shader "URP/RefuelPlatform_LightningFullCycle"
{
    Properties
    {
        _MainColor ("Base Color", Color) = (0.3, 0.0, 0.5, 1)
        _GradientColor1 ("Gradient Color 1", Color) = (0.0, 1.0, 1.0, 1)
        _GradientYStart1 ("Gradient 1 Y Start", Float) = 0.5
        _GradientColor2 ("Gradient Color 2", Color) = (0.0, 0.8, 1.0, 1)
        _GradientYStart2 ("Gradient 2 Y Start", Float) = 0.3
        _GradientColor3 ("Gradient Color 3", Color) = (0.0, 0.6, 1.0, 1)
        _GradientYStart3 ("Gradient 3 Y Start", Float) = 0.1
        _GradientColor4 ("Gradient Color 4", Color) = (0.0, 0.4, 1.0, 1)
        _GradientYStart4 ("Gradient 4 Y Start", Float) = -0.1

        _CircleColor ("Circle Color", Color) = (1, 0, 0, 1)
        _CircleCount ("Circle Count", Float) = 10
        _CircleSize ("Circle Size", Float) = 0.2
        _CircleSpeed ("Circle Speed", Float) = 1.0
        _CircleLifetime ("Circle Lifetime", Float) = 2.0

        [NoScaleOffset]_LightningTex ("Lightning Texture", 2D) = "black" {}
        [HDR]_LightningColor ("Lightning Color", Color) = (1, 1, 1, 1)
        _LightningFillDuration ("Lightning Fill Duration", Float) = 1.0
        _LightningHoldDuration ("Lightning Hold Duration", Float) = 1.0
        _LightningFadeDuration ("Lightning Fade Duration", Float) = 1.0

        _DistortedTex ("Distorted Texture", 2D) = "white" {}
        _DistortTexColor ("Distorted Texture Color", Color) = (0, 0, 0, 1)
        _DistortSpeed ("Distortion Speed", Float) = 1.0
        _DistortIntensity ("Distortion Intensity", Float) = 0.05

        [NoScaleOffset]_BaseOverlayTex ("Base Overlay Texture", 2D) = "white" {}
        _BaseOverlayColor ("Base Overlay Color", Color) = (0, 0, 0, 0)
        [HDR]_BaseOverlayGlowColor ("Base Overlay Glow Color", Color) = (1, 1, 1, 1)
        _BaseOverlayFillDuration ("Base Overlay Fill Duration", Float) = 1.0
        _BaseOverlayHoldDuration ("Base Overlay Hold Duration", Float) = 1.0
        _BaseOverlayFadeDuration ("Base Overlay Fade Duration", Float) = 1.0

        [HDR]_RimColor ("Rim Light Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Light Power", Float) = 3.0

        _ViewAngleGradientColor("View Gradient Color", Color) = (1, 1, 1, 1)
        _ViewAngleGradientStrength("View Gradient Strength", Float) = 2.0
        _NoiseStrength("Noise Strength", Float) = 0.05
        _ShadowStrength("Edge Shadow Strength", Float) = 0.3

        // === Toy Style Additions START ===
        [HDR]_ToySpecularColor("Toy Specular Color", Color) = (1, 1, 1, 1)
        _ToySpecularPower("Toy Specular Sharpness", Float) = 128.0
        _ToySpecularIntensity("Toy Specular Intensity", Float) = 1.0
        _ToyOutlineWidth("Toy Outline Width", Float) = 0.5
        _ToyRimPower("Toy Rim Power", Float) = 2.0
        [HDR]_ToyRimColor("Toy Rim Color", Color) = (1, 1, 1, 1)
        // === Toy Style Additions END ===
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
                float3 worldPos    : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            float4 _MainColor;
            float4 _GradientColor1, _GradientColor2, _GradientColor3, _GradientColor4;
            float _GradientYStart1, _GradientYStart2, _GradientYStart3, _GradientYStart4;

            float4 _CircleColor;
            float _CircleCount;
            float _CircleSize;
            float _CircleSpeed;
            float _CircleLifetime;

            TEXTURE2D(_LightningTex); SAMPLER(sampler_LightningTex);
            float4 _LightningColor;
            float _LightningFillDuration;
            float _LightningHoldDuration;
            float _LightningFadeDuration;

            TEXTURE2D(_DistortedTex); SAMPLER(sampler_DistortedTex);
            float4 _DistortTexColor;
            float _DistortSpeed;
            float _DistortIntensity;

            TEXTURE2D(_BaseOverlayTex); SAMPLER(sampler_BaseOverlayTex);
            float4 _BaseOverlayColor;
            float4 _BaseOverlayGlowColor;
            float _BaseOverlayFillDuration;
            float _BaseOverlayHoldDuration;
            float _BaseOverlayFadeDuration;

            float4 _RimColor;
            float _RimPower;

            float4 _ViewAngleGradientColor;
            float _ViewAngleGradientStrength;
            float _NoiseStrength;
            float _ShadowStrength;


            // === Toy Style Additions START ===
            float4 _ToySpecularColor;
            float _ToySpecularPower;
            float _ToySpecularIntensity;
            float _ToyOutlineWidth;
            float _ToyRimPower;
            float4 _ToyRimColor;
            // === Toy Style Additions END ===

            float4 ApplyGradient(float3 worldPos)
            {
                float4 color = _MainColor;

                float height1 = saturate(1.0 - (worldPos.y - _GradientYStart1));
                float height2 = saturate(1.0 - (worldPos.y - _GradientYStart2));
                float height3 = saturate(1.0 - (worldPos.y - _GradientYStart3));
                float height4 = saturate(1.0 - (worldPos.y - _GradientYStart4));

                color = lerp(color, _GradientColor1, height1);
                color = lerp(color, _GradientColor2, height2);
                color = lerp(color, _GradientColor3, height3);
                color = lerp(color, _GradientColor4, height4);

                return color;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.objectPos = v.positionOS.xyz;
                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.worldNormal = TransformObjectToWorldNormal(v.normalOS);
                o.positionHCS = TransformWorldToHClip(o.worldPos);
                return o;
            }
                        float4 frag(Varyings i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float4 baseColor = ApplyGradient(i.worldPos);

                if (normal.y > 0.9)
                {
                    float2 uv = i.objectPos.xz * 0.5 + 0.5;

                    float overlayCycleTime = _BaseOverlayFillDuration + _BaseOverlayHoldDuration + _BaseOverlayFadeDuration;
                    float overlayT = fmod(_Time.y, overlayCycleTime);

                    float overlayMask = 0.0;
                    if (overlayT < _BaseOverlayFillDuration)
                    {
                        float overlayPhase = overlayT / _BaseOverlayFillDuration;
                        overlayMask = smoothstep(0.0, 1.0, overlayPhase - uv.y);
                    }
                    else if (overlayT < _BaseOverlayFillDuration + _BaseOverlayHoldDuration)
                    {
                        overlayMask = 1.0;
                    }
                    else
                    {
                        float fadeT = (overlayT - _BaseOverlayFillDuration - _BaseOverlayHoldDuration) / _BaseOverlayFadeDuration;
                        overlayMask = 1.0 - saturate(fadeT);
                    }

                    float4 baseOverlayTex = SAMPLE_TEXTURE2D(_BaseOverlayTex, sampler_BaseOverlayTex, uv);
                    float4 overlayColor = lerp(_BaseOverlayColor, _BaseOverlayGlowColor, overlayMask);
                    baseColor.rgb += baseOverlayTex.rgb * overlayColor.rgb * baseOverlayTex.a;

                    float circleSum = 0;
                    [unroll(20)]
                    for (int c = 0; c < 20; c++)
                    {
                        if (c >= (int)_CircleCount) break;

                        float seed = dot(float2(c, c * 1.37), float2(12.9898, 78.233));
                        float birthOffset = frac(sin(seed) * 43758.5453);
                        float t = fmod(_Time.y + birthOffset * _CircleLifetime, _CircleLifetime);
                        float lifeProgress = t / _CircleLifetime;

                        float scale = sin(lifeProgress * 3.14159);

                        float2 posOffset = float2(
                            frac(sin(seed + t * _CircleSpeed) * 12345.6789),
                            frac(cos(seed + t * _CircleSpeed) * 98765.4321)
                        );
                        posOffset = posOffset * 0.8 + 0.1;

                        float2 motion = float2(sin(seed + t), cos(seed + t)) * 0.1;
                        float2 pos = posOffset + motion * lifeProgress;

                        float2 deform = float2(1.0 + 0.2 * sin(t + seed), 1.0 + 0.2 * cos(t + seed));
                        float2 delta = (uv - pos) * deform;
                        float dist = length(delta);
                        float circle = smoothstep(_CircleSize * scale, _CircleSize * scale * 0.8, dist);

                        circleSum += circle;
                    }
                    circleSum = saturate(circleSum);
                    baseColor.rgb = lerp(baseColor.rgb, _CircleColor.rgb, circleSum);
                    baseColor.a = lerp(baseColor.a, _CircleColor.a, circleSum);

                    float cycleTime = _LightningFillDuration + _LightningHoldDuration + _LightningFadeDuration;
                    float t = fmod(_Time.y, cycleTime);

                    float4 lightningTex = SAMPLE_TEXTURE2D(_LightningTex, sampler_LightningTex, uv);
                    float lightningMask = 0.0;

                    if (t < _LightningFillDuration)
                    {
                        float phase = t / _LightningFillDuration;
                        float fillMask = smoothstep(0.0, 1.0, phase - uv.y);
                        lightningMask = lightningTex.r * fillMask;
                    }
                    else if (t < _LightningFillDuration + _LightningHoldDuration)
                    {
                        lightningMask = lightningTex.r;
                    }
                    else
                    {
                        float fadeT = (t - _LightningFillDuration - _LightningHoldDuration) / _LightningFadeDuration;
                        float fadeOut = 1.0 - saturate(fadeT);
                        lightningMask = lightningTex.r * fadeOut;
                    }

                    if (lightningMask > 0.01)
                    {
                        baseColor.rgb = lerp(baseColor.rgb, _LightningColor.rgb, lightningMask);
                        baseColor.a = lerp(baseColor.a, _LightningColor.a, lightningMask);
                    }

                    float2 distortUV = uv + sin(float2(uv.y, uv.x) * 20 + _Time.y * _DistortSpeed) * _DistortIntensity;
                    float4 distortTex = SAMPLE_TEXTURE2D(_DistortedTex, sampler_DistortedTex, distortUV);
                    baseColor.rgb += distortTex.rgb * _DistortTexColor.rgb * distortTex.a;

                    float edgeFalloff = smoothstep(0.4, 0.0, abs(uv.x - 0.5) + abs(uv.y - 0.5));
                    baseColor.rgb *= lerp(1.0, 1.0 - _ShadowStrength, edgeFalloff);

                    float2 noiseUV = i.objectPos.xz * 0.5 + 0.5;
                    float noise = frac(sin(dot(noiseUV, float2(12.9898, 78.233))) * 43758.5453);
                    baseColor.rgb *= lerp(1.0, 1.0 + _NoiseStrength, noise);
                }
                                // Rim Light
                float rim = 1.0 - saturate(dot(normal, viewDir));
                rim = pow(rim, _RimPower);
                baseColor.rgb += rim * _RimColor.rgb * _RimColor.a;

                // View Angle Gradient
                float viewDot = dot(normal, viewDir);
                float viewFactor = pow(1.0 - saturate(viewDot), _ViewAngleGradientStrength);
                baseColor.rgb *= lerp(1.0, _ViewAngleGradientColor.rgb, viewFactor);

                // === Toy Style Additions START ===
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float3 halfDir = normalize(lightDir + viewDir);
                float toySpec = pow(saturate(dot(normal, halfDir)), _ToySpecularPower);
                baseColor.rgb += toySpec * _ToySpecularColor.rgb * _ToySpecularIntensity;

                float outlineMask = pow(1.0 - saturate(dot(normal, viewDir)), 2.0);
                baseColor.rgb = lerp(baseColor.rgb, float3(0,0,0), outlineMask * _ToyOutlineWidth);

                float toyRim = pow(1.0 - saturate(dot(normal, viewDir)), _ToyRimPower);
                baseColor.rgb += toyRim * _ToyRimColor.rgb * _ToyRimColor.a;
                // === Toy Style Additions END ===

                return baseColor;
            }
            ENDHLSL
        }
    }
}

