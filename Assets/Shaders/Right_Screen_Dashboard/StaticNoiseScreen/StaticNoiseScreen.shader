Shader "Oren/CRT_NoiseAndLogo_Layered_Motion"
{
    Properties
    {
        // --- Noise Layer ---
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.5
        _ScanlineIntensity("Scanline Intensity", Range(0,1)) = 0.6
        _Distortion("Distortion", Range(0,0.1)) = 0.02
        _NoiseSpeed("Noise Speed", Range(0,10)) = 2
        _NoiseTiling("Noise Tiling", Vector) = (1,1,0,0)
        _NoiseOffset("Noise Offset", Vector) = (0,0,0,0)
        _NoiseAlpha("Noise Alpha", Range(0,1)) = 1

        // --- Logo Layer ---
        _LogoTex("Logo Texture", 2D) = "white" {}
        _LogoTiling("Logo Tiling", Vector) = (1,1,0,0)
        _LogoOffset("Logo Offset", Vector) = (0,0,0,0)
        _LogoScale("Logo Scale", Float) = 1
        _LogoAlpha("Logo Alpha", Range(0,1)) = 1
        _FlipLogoX("Flip Logo X", Float) = 0
        _FlipLogoY("Flip Logo Y", Float) = 0

        // --- Logo Motion ---
        _LogoDistortion("Logo Distortion", Range(0,0.1)) = 0.02
        _LogoSpeed("Logo Speed", Range(0,10)) = 2
        _LogoNoiseStrength("Logo Noise Strength", Range(0,1)) = 0.3
        _LogoFlickerStrength("Logo Flicker Strength", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // --- Noise uniforms ---
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _NoiseStrength;
            float _ScanlineIntensity;
            float _Distortion;
            float _NoiseSpeed;
            float4 _NoiseTiling;
            float4 _NoiseOffset;
            float _NoiseAlpha;

            // --- Logo uniforms ---
            sampler2D _LogoTex;
            float4 _LogoTex_ST;
            float4 _LogoTiling;
            float4 _LogoOffset;
            float _LogoScale;
            float _LogoAlpha;
            float _FlipLogoX;
            float _FlipLogoY;

            // --- Logo motion ---
            float _LogoDistortion;
            float _LogoSpeed;
            float _LogoNoiseStrength;
            float _LogoFlickerStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // --- Noise Layer ---
                float2 nUV = i.uv * _NoiseTiling.xy + _NoiseOffset.xy;
                nUV.x += sin(nUV.y * 50.0 + _Time.y * _NoiseSpeed) * _Distortion;

                fixed4 noiseCol = tex2D(_NoiseTex, nUV);
                float noise = random(nUV * _Time.y * 100.0) * 2.0 - 1.0;
                noiseCol.rgb += noise * _NoiseStrength;
                float scan = sin(nUV.y * 800.0) * 0.5 + 0.5;
                noiseCol.rgb *= lerp(1.0, scan, _ScanlineIntensity);
                noiseCol.a *= _NoiseAlpha;

                // --- Logo Layer (with motion) ---
                float2 lUV = (i.uv - 0.5) / _LogoScale + 0.5;
                lUV = lUV * _LogoTiling.xy + _LogoOffset.xy;
                if (_FlipLogoX > 0.5) lUV.x = 1 - lUV.x;
                if (_FlipLogoY > 0.5) lUV.y = 1 - lUV.y;

                lUV.x += sin(lUV.y * 50.0 + _Time.y * _LogoSpeed) * _LogoDistortion;
                fixed4 logoCol = tex2D(_LogoTex, lUV);

                float lNoise = random(lUV * _Time.y * 80.0) * 2.0 - 1.0;
                logoCol.rgb += lNoise * _LogoNoiseStrength;

                float lFlicker = sin(_Time.y * 60.0) * _LogoFlickerStrength + (1 - _LogoFlickerStrength);
                logoCol.rgb *= lFlicker;

                logoCol.a *= _LogoAlpha;

                // Combine both layers
                fixed4 finalCol = lerp(noiseCol, logoCol, logoCol.a);
                finalCol.a = max(noiseCol.a, logoCol.a);
                return saturate(finalCol);
            }
            ENDCG
        }
    }
}
