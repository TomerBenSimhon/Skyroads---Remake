Shader "Unlit/Death_Platform"
{
    Properties
    {
        _MainColor("Main Color", Color) = (0.8, 0.1, 0.1, 1)
        _FogColor("Fog Color", Color) = (1, 0.2, 0.2, 1)
        _PatternScale("Pattern Scale", Float) = 10
        _FogSpeed("Fog Speed", Float) = 0.5
        _FogStrength("Fog Strength", Range(0,1)) = 0.5
        _NoiseTex("Noise Texture", 2D) = "white" {}

        [HDR]_RimColor("Rim Light Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Light Power", Float) = 3.0

        _TopGradientColor("Top Gradient Color", Color) = (1, 0.5, 0.5, 1)
        _BottomGradientColor("Bottom Gradient Color", Color) = (0.3, 0, 0, 1)
        _GradientMinY("Gradient Min Y", Float) = 0.0
        _GradientMaxY("Gradient Max Y", Float) = 1.0

        _ViewAngleGradientColor("View Angle Gradient Color", Color) = (0.1, 0.1, 0.1, 1)
        _ViewAngleGradientStrength("View Angle Gradient Strength", Range(0,1)) = 0.5

        _NoiseStrength("Noise Strength", Range(0,1)) = 0.3
        _ShadowStrength("Edge Shadow Strength", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

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
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            sampler2D _NoiseTex;
            float4 _MainColor;
            float4 _FogColor;
            float _PatternScale;
            float _FogSpeed;
            float _FogStrength;

            float4 _RimColor;
            float _RimPower;

            float4 _TopGradientColor;
            float4 _BottomGradientColor;
            float _GradientMinY;
            float _GradientMaxY;

            float4 _ViewAngleGradientColor;
            float _ViewAngleGradientStrength;

            float _NoiseStrength;
            float _ShadowStrength;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldPos = worldPos;
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float circlePattern(float2 uv, float scale)
            {
                uv *= scale;
                float2 gv = frac(uv) - 0.5;
                float d = length(gv);
                return smoothstep(0.45, 0.4, d);
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 projectedUV = IN.worldPos.xz;

                float pattern = circlePattern(projectedUV, _PatternScale);
                float4 baseColor = lerp(_MainColor * 0.8, _MainColor, pattern);

                // Gradient based on world Y
                float heightNorm = saturate((IN.worldPos.y - _GradientMinY) / max(0.0001, (_GradientMaxY - _GradientMinY)));
                float4 gradientColor = lerp(_BottomGradientColor, _TopGradientColor, heightNorm);
                baseColor.rgb *= gradientColor.rgb;

                // Noise on top
                float2 noiseUV = projectedUV * 0.5 + 0.5;
                float noise = frac(sin(dot(noiseUV, float2(12.9898, 78.233))) * 43758.5453);
                baseColor.rgb *= lerp(1.0 - _NoiseStrength, 1.0 + _NoiseStrength, noise);

                // Edge shadow (fake vignette)
                float edgeFalloff = smoothstep(0.4, 0.0, abs(noiseUV.x - 0.5) + abs(noiseUV.y - 0.5));
                baseColor.rgb *= lerp(1.0, 1.0 - _ShadowStrength, edgeFalloff);

                // Fog movement and blend
                float2 fogUV = projectedUV;
                fogUV.y += sin(_Time.y * _FogSpeed) * 0.3;
                fogUV.x += cos(_Time.y * _FogSpeed * 1.3) * 0.2;

                float chaosWave = sin(projectedUV.x * 4.0 + _Time.y * 0.7) * cos(projectedUV.y * 3.0 + _Time.y * 0.5);
                fogUV += chaosWave * 0.15;

                float fogNoise = tex2D(_NoiseTex, fogUV).r;
                float fog = smoothstep(0.4, 1.0, fogNoise);
                float4 fogColor = _FogColor * fog * _FogStrength;

                float4 finalColor = baseColor + fogColor;

                // Rim Light
                float3 normal = normalize(IN.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                float rim = 1.0 - saturate(dot(normal, viewDir));
                rim = pow(rim, _RimPower);
                finalColor.rgb += rim * _RimColor.rgb * _RimColor.a;

                // View Angle Gradient
                float viewGrad = 1.0 - saturate(dot(normal, viewDir));
                finalColor.rgb = lerp(finalColor.rgb, _ViewAngleGradientColor.rgb, viewGrad * _ViewAngleGradientStrength);

                finalColor.a = 1;
                return finalColor;
            }
            ENDHLSL
        }
    }
}
