Shader "Oren/CRT_StaticNoise_Aligned"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _FlipX ("Flip X", Float) = 0
        _FlipY ("Flip Y", Float) = 0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.5
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.7
        _Distortion ("Distortion", Range(0, 0.1)) = 0.02
        _Speed ("Speed", Range(0, 10)) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FlipX;
            float _FlipY;
            float _NoiseStrength;
            float _ScanlineIntensity;
            float _Distortion;
            float _Speed;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float2 uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                // שליטה דרך משתנים ולא קשיח
                if (_FlipX > 0.5) uv.x = 1 - uv.x;
                if (_FlipY > 0.5) uv.y = 1 - uv.y;

                o.uv = uv;
                return o;
            }

            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                uv.x += sin(uv.y * 50.0 + _Time.y * _Speed) * _Distortion;

                fixed4 col = tex2D(_MainTex, uv);

                float noise = random(uv * _Time.y * 100.0) * 2.0 - 1.0;
                col.rgb += noise * _NoiseStrength;

                float scan = sin(uv.y * 800.0) * 0.5 + 0.5;
                col.rgb *= lerp(1.0, scan, _ScanlineIntensity);

                float flicker = sin(_Time.y * 60.0) * 0.1 + 0.9;
                col.rgb *= flicker;

                return saturate(col);
            }
            ENDCG
        }
    }
}
