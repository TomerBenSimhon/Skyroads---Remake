Shader "Oren/PortalVortex_Local_HDR_URP_DualFlow_Randomized"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,0.2,1,1)
        _EmissionColor("Emission Color (HDR)", Color) = (2,0.4,2,1)
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _DistortTex("Distortion Noise", 2D) = "gray" {}

        _SpinSpeed("Spin Speed", Float) = 2.0
        _FlowSpeed("Inward Flow Speed", Float) = 0.5
        _DistortStrength("Distortion Strength", Range(0,1)) = 0.25

        _FadePower("Center Fade Power", Range(0.1,6)) = 3
        _EdgeSoftness("Edge Softness", Range(0.1,3)) = 1.2
        _GlowIntensity("Glow Intensity", Range(0,20)) = 8
        _Opacity("Opacity", Range(0,1)) = 1.0
        _MaskSoftness("Mask Softness", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            sampler2D _NoiseTex, _DistortTex;
            float4 _BaseColor, _EmissionColor;
            float _SpinSpeed, _FlowSpeed, _DistortStrength;
            float _FadePower, _EdgeSoftness, _GlowIntensity, _Opacity, _MaskSoftness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.positionOS.xz;
                float radius = length(uv);
                float angle = atan2(uv.y, uv.x);

                // תזוזת זמן עם "רעש"
                float timeNoise = (sin(_Time.y * 0.7) + sin(_Time.y * 1.3)) * 0.5;
                angle += (_Time.y + timeNoise * 0.3) * _SpinSpeed;

                // מחזור בסיסי עם אקראיות קטנה
                float baseCycle = radius * 0.15 + (_Time.y + timeNoise * 0.2) * _FlowSpeed;

                // שתי שכבות בהיסט מחזורי
                float inward1 = frac(abs(baseCycle));
                float inward2 = frac(abs(baseCycle + 0.5));

                // fade נפרד לכל שכבה
                float fade1 = (smoothstep(0.0, 0.05, inward1)) * (1.0 - smoothstep(0.95, 1.0, inward1));
                float fade2 = (smoothstep(0.0, 0.05, inward2)) * (1.0 - smoothstep(0.95, 1.0, inward2));

                // רעש דינמי לעיוות UV
                float2 distortUV = uv * 2 + _Time.y * 0.1;
                float2 distortion = (tex2D(_DistortTex, distortUV).rg - 0.5) * _DistortStrength;

                // UV של שכבות
                float2 vortexUV1 = float2(cos(angle), sin(angle)) * inward1 * (1 - radius * 0.3) + 0.5 + distortion;
                float2 vortexUV2 = float2(cos(angle * 1.02), sin(angle * 0.98)) * inward2 * (1 - radius * 0.3) + 0.5 - distortion;

                float noise1 = tex2D(_NoiseTex, vortexUV1).r;
                float noise2 = tex2D(_NoiseTex, vortexUV2).r;

                // fade מרכז וקצוות
                float fadeCenter = saturate(1 - pow(radius * 0.25, _FadePower));
                float edgeFade = 1.0 - smoothstep(0.8, _EdgeSoftness, radius * 0.25);
                float fade = fadeCenter * edgeFade;

                // מסכה עגולה
                float maskRadius = 0.9;
                float mask = smoothstep(maskRadius, maskRadius - _MaskSoftness, 1.0 - radius);

                // שילוב שכבות עם אקראיות טבעית
                float layer = saturate(noise1 * fade1 + noise2 * fade2);
                float3 glow = (_BaseColor.rgb + _EmissionColor.rgb * _GlowIntensity) * layer * fade * mask;

                return float4(glow, layer * fade * _Opacity);
            }
            ENDHLSL
        }
    }
}
