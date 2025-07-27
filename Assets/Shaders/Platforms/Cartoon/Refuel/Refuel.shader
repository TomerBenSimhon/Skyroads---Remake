Shader "URP/CartoonRefuelPlatform_Base"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        _TopGradientStart ("Top Gradient Start", Color) = (1, 0, 1, 1)
        _TopGradientEnd ("Top Gradient End", Color) = (0, 0, 1, 1)
        _TopGradientLerp ("Top Gradient Lerp", Range(0,1)) = 0.5
        _TopGradientDirection ("Top Gradient Direction", Vector) = (0, 1, 0, 0)

        _TopEdgeColor ("Top Edge Color", Color) = (1, 1, 1, 1)
        _TopEdgeBlendWidth ("Top Edge Blend Width", Range(0.0, 0.5)) = 0.1

        _SideGradientStart ("Side Gradient Start", Color) = (0.7, 0.2, 1.0, 1)
        _SideGradientEnd ("Side Gradient End", Color) = (0.1, 0.0, 0.2, 1)
        _SideGradientMin ("Side Gradient World Min", Float) = -0.5
        _SideGradientMax ("Side Gradient World Max", Float) = 0.5

        _CircleTex ("Circle Texture", 2D) = "white" {}
        _CircleColor1 ("Circle Color 1", Color) = (1, 1, 1, 1)
        _CircleColor2 ("Circle Color 2", Color) = (1, 0.5, 1, 1)
        _CircleCount ("Number of Circles", Float) = 10
        _CircleSizeMin ("Min Circle Size", Float) = 0.05
        _CircleSizeMax ("Max Circle Size", Float) = 0.2
        _CircleLifeMin ("Min Circle Life", Float) = 0.5
        _CircleLifeMax ("Max Circle Life", Float) = 2.0
        _CircleMoveSpeedMin ("Min Move Speed", Float) = 0.1
        _CircleMoveSpeedMax ("Max Move Speed", Float) = 0.5

        _LightningTex ("Lightning Texture", 2D) = "white" {}
        [HDR]_LightningGlowColor ("Glow HDR Color", Color) = (3, 3, 6, 1)
        _LightningIntensityOff ("Lightning Intensity When Off", Float) = 0.2
        _LightningIntensityOn ("Lightning Intensity When On", Float) = 1.0
        _LightningTransitionTime ("Lightning Transition Duration", Float) = 1.0
        _IsPlayerOn ("Is Player On Platform", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_CIRCLE_COUNT 200

            TEXTURE2D(_CircleTex);
            SAMPLER(sampler_CircleTex);
            TEXTURE2D(_LightningTex);
            SAMPLER(sampler_LightningTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float4 _TopGradientStart;
                float4 _TopGradientEnd;
                float _TopGradientLerp;
                float4 _TopGradientDirection;

                float4 _TopEdgeColor;
                float _TopEdgeBlendWidth;

                float4 _SideGradientStart;
                float4 _SideGradientEnd;
                float _SideGradientMin;
                float _SideGradientMax;

                float4 _CircleColor1;
                float4 _CircleColor2;
                float _CircleCount;
                float _CircleSizeMin;
                float _CircleSizeMax;
                float _CircleLifeMin;
                float _CircleLifeMax;
                float _CircleMoveSpeedMin;
                float _CircleMoveSpeedMax;

                float4 _LightningGlowColor;
                float _LightningIntensityOff;
                float _LightningIntensityOn;
                float _LightningTransitionTime;
                float _IsPlayerOn;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS).xyz;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float3 normal = normalize(IN.normalWS);
                float isTopFace = step(0.9, normal.y);

                float edgeX = min(uv.x, 1.0 - uv.x);
                float edgeY = min(uv.y, 1.0 - uv.y);
                float edgeDist = min(edgeX, edgeY);
                float edgeMask = smoothstep(0.0, _TopEdgeBlendWidth, edgeDist);
                edgeMask = 1.0 - edgeMask;

                float2 dir = normalize(_TopGradientDirection.xy);
                float grad = dot(uv - 0.5, dir) + _TopGradientLerp - 0.5;
                grad = saturate(grad + 0.5);
                float4 topGradient = lerp(_TopGradientStart, _TopGradientEnd, grad);
                float4 topColor = lerp(topGradient, _TopEdgeColor, edgeMask);

                float posForGrad = IN.positionWS.y;
                float sideHeight = saturate((posForGrad - _SideGradientMin) / (_SideGradientMax - _SideGradientMin));
                float4 sideColor = lerp(_SideGradientStart, _SideGradientEnd, sideHeight);

                float4 finalColor = lerp(sideColor, topColor, isTopFace);

                float4 circleLayer = float4(0, 0, 0, 0);
                for (int i = 0; i < MAX_CIRCLE_COUNT; i++)
                {
                    if (i >= (int)_CircleCount) break;

                    float seed = i * 37.41;
                    float2 randPos = float2(
                        frac(sin(seed * 12.9898) * 43758.5453),
                        frac(cos(seed * 78.233) * 12345.6789)
                    );

                    float randLife = lerp(_CircleLifeMin, _CircleLifeMax, frac(sin(seed * 0.33) * 17.123));
                    float randSize = lerp(_CircleSizeMin, _CircleSizeMax, frac(sin(seed * 1.77) * 97.231));
                    float randSpeed = lerp(_CircleMoveSpeedMin, _CircleMoveSpeedMax, frac(cos(seed * 5.44) * 32.87));
                    float2 randColorSelector = float2(frac(sin(seed * 3.7) * 51.2), 0);
                    float4 color = lerp(_CircleColor1, _CircleColor2, step(0.5, randColorSelector.x));

                    float offsetTime = frac((_Time.y + seed * 0.13) / randLife);
                    float scale = sin(offsetTime * 3.14159);

                    float2 offset = float2(
                        sin(_Time.y * randSpeed + seed) * 0.05,
                        cos(_Time.y * randSpeed + seed * 2.0) * 0.05
                    );

                    float2 localUV = (uv + offset - randPos) / randSize;
                    localUV /= scale + 0.0001;

                    if (abs(localUV.x) <= 1 && abs(localUV.y) <= 1)
                    {
                        float4 sample = SAMPLE_TEXTURE2D(_CircleTex, sampler_CircleTex, 0.5 + localUV * 0.5);
                        float3 rgb = sample.rgb * sample.a;
                        float4 glow = float4(rgb, 1) * color * scale;
                        circleLayer += glow;
                    }
                }

                finalColor += circleLayer;

                float4 lightningLayer = float4(0, 0, 0, 0);
                if (isTopFace > 0.5)
                {
                    float4 lightningTex = SAMPLE_TEXTURE2D(_LightningTex, sampler_LightningTex, uv);
                    float lightningMask = lightningTex.a;
                    float intensity = lerp(_LightningIntensityOff, _LightningIntensityOn, _IsPlayerOn);
                    lightningLayer.rgb = lightningTex.rgb * intensity * _LightningGlowColor.rgb;
                    lightningLayer.a = 1.0;
                }

                finalColor += lightningLayer;
                return finalColor;
            }
            ENDHLSL
        }
    }
}
