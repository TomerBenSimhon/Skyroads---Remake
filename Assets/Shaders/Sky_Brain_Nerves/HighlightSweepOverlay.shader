Shader "URP/HighlightSweepOverlay_MovingTexture_Fixed"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _MainTexSpeed ("MainTex Scroll Speed (X,Y)", Vector) = (0.1, 0.1, 0, 0)

        [HDR] _GlowColor ("Glow Color (HDR)", Color) = (10, 5, 1, 1)
        _GlowDir ("Glow Direction (X,Y)", Vector) = (1, 1, 0, 0)
        _GlowFrequency ("Glow Frequency", Range(0.1, 10.0)) = 1.0
        _GlowWidth ("Glow Width", Range(0.001, 1.0)) = 0.2
        _GlowSpeed ("Glow Speed", Float) = 1.0
        _Alpha ("Global Alpha", Range(0, 1)) = 1.0
        _SeamSpeed ("Seam Opening Speed", Range(0.01, 5.0)) = 0.5
        _SeamRange ("Seam Fade Width", Range(0.001, 0.5)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" }
        ZWrite Off
        Blend SrcAlpha One
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _GlowColor;
            float4 _GlowDir;
            float _GlowFrequency;
            float _GlowWidth;
            float _GlowSpeed;
            float _Alpha;
            float _SeamSpeed;
            float _SeamRange;

            float4 _MainTexSpeed;
            float4 _MainTex_ST; // <--- חשוב לטילינג ואופסט
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex); // מכיל טילינג ואופסט
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float time = _Time.y;

                // שימוש ב-UV מתוקן עם טילינג + Scroll
                float2 scrolledUV = frac(IN.uv + _MainTexSpeed.xy * time);

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrolledUV);
                float alphaFromTex = texColor.a;

                // Glow wave
                float2 dir = normalize(_GlowDir.xy);
                float projection = dot(IN.uv, dir);
                float wave = 0.5 + 0.5 * sin((projection - time * _GlowSpeed) * _GlowFrequency * 6.2831);
                float glow = smoothstep(0.5 - _GlowWidth, 0.5 + _GlowWidth, wave);

                // Center fade logic (fade from UV.x = 0.5)
                float distFromCenter = abs(IN.uv.x - 0.5);
                float seamFade = smoothstep(0.0, _SeamRange, distFromCenter);
                float seamOpen = saturate(time * _SeamSpeed);
                float fadeFactor = smoothstep(0.0, 1.0, seamFade * seamOpen);

                // Final result
                float finalAlpha = _Alpha * alphaFromTex * fadeFactor;
                float3 finalColor = texColor.rgb + _GlowColor.rgb * glow * alphaFromTex;

                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
