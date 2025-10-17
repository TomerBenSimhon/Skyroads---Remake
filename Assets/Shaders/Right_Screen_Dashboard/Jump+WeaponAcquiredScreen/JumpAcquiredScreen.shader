Shader "Oren/JumpAcquiredScreen_SpriteOnTop_Tiled"
{
    Properties
    {
        // --- Sprite Sheet ---
        _SpriteSheet("Sprite Sheet", 2D) = "white" {}
        _FrameCount("Frame Count", Float) = 8
        _FrameSpeed("Frame Speed", Float) = 12
        _SpriteTiling("Sprite Tiling", Vector) = (1,1,0,0)
        _SpriteOffset("Sprite Offset", Vector) = (0,0,0,0)
        _SpriteScale("Sprite Scale", Float) = 1
        _SpritePos("Sprite Position", Vector) = (0.5,0.5,0,0)

        // --- Logo ---
        _LogoTex("Logo Texture", 2D) = "white" {}
        _LogoPos("Logo Position", Vector) = (0.8, 0.8, 0, 0)
        _LogoScale("Logo Scale", Float) = 0.2
        _LogoFlipX("Flip Logo X", Float) = 1
        _LogoFlipY("Flip Logo Y", Float) = 1

        // --- Text ---
        _TextTex("Jump Text Texture", 2D) = "white" {}
        _TextPos("Text Position", Vector) = (0.5, 0.2, 0, 0)
        _TextScale("Text Scale", Float) = 0.3
        _TextColor("Text Color", Color) = (1,1,1,1)
        _TextFlipX("Flip Text X", Float) = 1
        _TextFlipY("Flip Text Y", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _SpriteSheet;
            sampler2D _LogoTex;
            sampler2D _TextTex;

            float _FrameCount;
            float _FrameSpeed;

            float4 _SpriteTiling;
            float4 _SpriteOffset;
            float _SpriteScale;
            float4 _SpritePos;

            float4 _LogoPos;
            float _LogoScale;
            float _LogoFlipX;
            float _LogoFlipY;

            float4 _TextPos;
            float _TextScale;
            float4 _TextColor;
            float _TextFlipX;
            float _TextFlipY;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // --- טקסט ---
                float2 textUV = (IN.uv - _TextPos.xy) / _TextScale + 0.5;
                textUV.x = (textUV.x - 0.5) * _TextFlipX + 0.5;
                textUV.y = (textUV.y - 0.5) * _TextFlipY + 0.5;
                textUV = saturate(textUV);
                float4 textCol = tex2D(_TextTex, textUV);
                textCol.rgb *= _TextColor.rgb;
                textCol.a *= _TextColor.a;

                // --- לוגו ---
                float2 logoUV = (IN.uv - _LogoPos.xy) / _LogoScale + 0.5;
                logoUV.x = (logoUV.x - 0.5) * _LogoFlipX + 0.5;
                logoUV.y = (logoUV.y - 0.5) * _LogoFlipY + 0.5;
                logoUV = saturate(logoUV);
                float4 logoCol = tex2D(_LogoTex, logoUV);

                // --- בסיס ---
                float4 combinedCol = float4(0,0,0,1);
                combinedCol.rgb = lerp(combinedCol.rgb, textCol.rgb, textCol.a);
                combinedCol.rgb = lerp(combinedCol.rgb, logoCol.rgb, logoCol.a);
                combinedCol.a = max(textCol.a, logoCol.a);

                // --- Sprite Sheet עם Tiling / Offset / Scale / Position ---
                float frame = floor(fmod(_Time.y * _FrameSpeed, _FrameCount));
                float frameWidth = 1.0 / _FrameCount;

                float2 sUV = (IN.uv * _SpriteTiling.xy + _SpriteOffset.xy - _SpritePos.xy) / max(_SpriteScale, 1e-4) + 0.5;
                sUV.x = sUV.x / _FrameCount + frame * frameWidth;
                sUV = frac(sUV); // תמיכה ב־tiling אמיתי
                float4 spriteCol = tex2D(_SpriteSheet, sUV);

                // שילוב סופי – הספרייט מוסיף בהירות מעל הכול
                float4 finalCol = lerp(combinedCol, spriteCol, spriteCol.a * 0.8);

                return finalCol;
            }
            ENDHLSL
        }
    }
}
