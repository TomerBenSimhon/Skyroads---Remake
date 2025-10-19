Shader "Oren/ShipOnPath_WithBackground_AndLogo_URPFix"
{
    Properties
    {
        // --- Ship Texture ---
        _ShipTex("Ship Texture", 2D) = "white" {}
        _ShipPos("Ship Position", Vector) = (0.5, 0.5, 0, 0)
        _ShipScale("Ship Scale", Range(0.01,1)) = 0.3
        _ShipFlipX("Ship Flip X", Float) = 1
        _ShipFlipY("Ship Flip Y", Float) = 1
        _ShipRotation("Ship Rotation (Degrees)", Range(0,360)) = 0

        // --- Path Texture ---
        _PathTex("Path Texture", 2D) = "white" {}
        _PathPos("Path Position", Vector) = (0.5, 0.5, 0, 0)
        _PathScale("Path Scale", Float) = 1
        _PathFlipX("Path Flip X", Float) = 1
        _PathFlipY("Path Flip Y", Float) = 1

        // --- Background Layer ---
        _BgTex("Background Texture", 2D) = "white" {}
        _BgColor("Background Color", Color) = (0.1, 0.2, 0.3, 1)
        _BgWaveAmp("BG Wave Amplitude", Range(0,0.05)) = 0.015
        _BgWaveFreq("BG Wave Frequency", Range(0,20)) = 5
        _BgWaveSpeed("BG Wave Speed", Range(0,5)) = 1
        _BgNoiseTex("BG Noise Texture", 2D) = "gray" {}
        _BgNoiseAmt("BG Noise Amount", Range(0,0.2)) = 0.05

        // --- Logo Layer ---
        _LogoTex("Logo Texture", 2D) = "white" {}
        _LogoPos("Logo Position", Vector) = (0.5, 0.9, 0, 0)
        _LogoScale("Logo Scale", Float) = 0.15
        _LogoFlipX("Logo Flip X", Float) = 1
        _LogoFlipY("Logo Flip Y", Float) = 1

        // --- Movement ---
        _Move("Ship Move 0-1", Range(0,1)) = 0
        _MoveAxis("Move Axis (0=X, 1=Y)", Float) = 0

        // --- General ---
        _Brightness("Brightness", Range(0,3)) = 1
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

        // זה התיקון – כמו בליקוויד שיידר
        ZWrite Off
        ZTest LEqual
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_ShipTex); SAMPLER(sampler_ShipTex);
            TEXTURE2D(_PathTex); SAMPLER(sampler_PathTex);
            TEXTURE2D(_BgTex); SAMPLER(sampler_BgTex);
            TEXTURE2D(_BgNoiseTex); SAMPLER(sampler_BgNoiseTex);
            TEXTURE2D(_LogoTex); SAMPLER(sampler_LogoTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ShipPos;
                float _ShipScale;
                float _ShipFlipX;
                float _ShipFlipY;
                float _ShipRotation;

                float4 _PathPos;
                float _PathScale;
                float _PathFlipX;
                float _PathFlipY;

                float4 _BgColor;
                float _BgWaveAmp;
                float _BgWaveFreq;
                float _BgWaveSpeed;
                float _BgNoiseAmt;

                float4 _LogoPos;
                float _LogoScale;
                float _LogoFlipX;
                float _LogoFlipY;

                float _Move;
                float _MoveAxis;
                float _Brightness;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float2 RotateUV(float2 uv, float rotationDeg)
            {
                float2 center = float2(0.5, 0.5);
                float rad = radians(rotationDeg);
                float s = sin(rad);
                float c = cos(rad);
                uv -= center;
                uv = mul(uv, float2x2(c, -s, s, c));
                uv += center;
                return uv;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // === BACKGROUND ===
                float time = _Time.y * _BgWaveSpeed;
                float wave = sin(uv.x * _BgWaveFreq + time) * _BgWaveAmp;
                float noise = SAMPLE_TEXTURE2D(_BgNoiseTex, sampler_BgNoiseTex, uv + float2(time * 0.05, 0)).r * 2 - 1;
                noise *= _BgNoiseAmt;
                float2 bgUV = uv + float2(wave + noise, 0);
                half4 bgTexCol = SAMPLE_TEXTURE2D(_BgTex, sampler_BgTex, bgUV);
                half4 bgCol = _BgColor * bgTexCol;

                // === PATH ===
                float2 pathUV = (uv - _PathPos.xy) / _PathScale + 0.5;
                pathUV.x = (pathUV.x - 0.5) * _PathFlipX + 0.5;
                pathUV.y = (pathUV.y - 0.5) * _PathFlipY + 0.5;
                half4 pathCol = SAMPLE_TEXTURE2D(_PathTex, sampler_PathTex, saturate(pathUV));

                // === LOGO ===
                float2 logoUV = (uv - _LogoPos.xy) / _LogoScale + 0.5;
                logoUV.x = (logoUV.x - 0.5) * _LogoFlipX + 0.5;
                logoUV.y = (logoUV.y - 0.5) * _LogoFlipY + 0.5;
                half4 logoCol = SAMPLE_TEXTURE2D(_LogoTex, sampler_LogoTex, saturate(logoUV));

                // === SHIP ===
                float2 shipCenter = _ShipPos.xy;
                float safeRange = 1.0 - _ShipScale;
                float offset = (1.0 - _Move) * safeRange;

                if (_MoveAxis < 0.5)
                    shipCenter.x = offset + _ShipPos.x - 0.5;
                else
                    shipCenter.y = offset + _ShipPos.y - 0.5;

                float2 shipUV = (uv - shipCenter) / _ShipScale + 0.5;
                shipUV = RotateUV(shipUV, _ShipRotation);
                shipUV.x = (shipUV.x - 0.5) * _ShipFlipX + 0.5;
                shipUV.y = (shipUV.y - 0.5) * _ShipFlipY + 0.5;
                half4 shipCol = SAMPLE_TEXTURE2D(_ShipTex, sampler_ShipTex, saturate(shipUV));

                // === COMBINE ===
                half4 combined = lerp(bgCol, pathCol, pathCol.a);
                combined = lerp(combined, logoCol, logoCol.a);
                combined = lerp(combined, shipCol, shipCol.a);
                combined.rgb *= _Brightness;

                return combined;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
