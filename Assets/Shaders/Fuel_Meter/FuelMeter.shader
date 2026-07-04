Shader "TriArts/UI/LiquidFuel_Animated"
{
    Properties
    {
        _MainTex   ("Main Texture", 2D) = "white" {}
        _MainColor ("Fill Color", Color) = (1,0,0,1)
        _EdgeColor ("Edge Color", Color) = (1,0.6,0.6,1)
        _FuelColor ("Fuel Color", Color) = (1,1,1,1)

        _Level     ("Level 0-1", Range(0,1)) = 0.5
        _EdgeWidth ("Edge Width", Range(0,0.1)) = 0.02

        _WaveAmp   ("Wave Amplitude", Range(0,0.1)) = 0.03
        _WaveFreq  ("Wave Frequency", Range(0,20))  = 8
        _WaveSpeed ("Wave Speed", Range(0,10)) = 2

        _NoiseTex  ("Noise Tex", 2D) = "white" {}
        _NoiseAmt  ("Noise Amount", Range(0,0.1)) = 0.02

        _Brightness ("Brightness", Range(0,3)) = 1

        // --- תזוזת הטקסטורה הראשית ---
        _MainWaveAmp   ("MainTex Wave Amplitude", Range(0,0.05)) = 0.02
        _MainWaveFreq  ("MainTex Wave Frequency", Range(0,20)) = 6
        _MainWaveSpeed ("MainTex Wave Speed", Range(0,10)) = 1.5
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderPipeline"="UniversalPipeline" 
        }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainColor;
                float4 _EdgeColor;
                float4 _FuelColor;
                float   _Level;
                float   _EdgeWidth;
                float   _WaveAmp;
                float   _WaveFreq;
                float   _WaveSpeed;
                float   _NoiseAmt;
                float   _Brightness;
                float   _MainWaveAmp;
                float   _MainWaveFreq;
                float   _MainWaveSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // --- תנועה "חיה" לטקסטורה הראשית ---
                float t = _Time.y * _MainWaveSpeed;
                float2 waveOffset;
                waveOffset.x = sin(uv.y * _MainWaveFreq + t) * _MainWaveAmp;
                waveOffset.y = cos(uv.x * _MainWaveFreq + t * 1.2) * _MainWaveAmp;
                uv += waveOffset;

                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // גל רמת דלק (קיים)
                float fuelT = _Time.y * _WaveSpeed;
                float wave = sin(IN.uv.x * _WaveFreq + fuelT) * _WaveAmp;

                // רעש עדין
                float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uv + float2(_Time.y * 0.05, 0)).r;
                n = (n * 2.0 - 1.0) * _NoiseAmt;

                float cut = saturate(_Level + wave + n);

                // מסכת מילוי
                float fillMask = step(IN.uv.y, cut);

                // שוליים רכים
                float edgeMask = 1.0 - smoothstep(cut, cut + _EdgeWidth, IN.uv.y);

                // צבעים עם השפעת FuelColor
                float3 baseCol = _MainColor.rgb * _FuelColor.rgb;
                float3 edgeCol = _EdgeColor.rgb * _FuelColor.rgb;
                float3 col = lerp(baseCol, edgeCol, edgeMask);

                // טקסטורה + בהירות
                col *= texCol.rgb * _Brightness;

                // אלפא
                float a = saturate(max(fillMask, edgeMask)) * texCol.a;

                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
