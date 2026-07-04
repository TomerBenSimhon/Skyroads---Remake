
Shader "URP/Boost/TrailCutoutDither"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Color ("Color", Color) = (0.1, 0.9, 1.0, 1.0)
        _Cutoff ("Edge Softness", Range(0,1)) = 0.4
        _Scroll ("UV Scroll", Vector) = (0, -1.5, 0, 0)
        _Tiling ("UV Tiling", Vector) = (1, 1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }
        LOD 100
        ZWrite On
        ZTest LEqual
        Cull Off
        Blend Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float2 screenUV:TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _Scroll;
                float4 _Tiling;
                float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            float bayer4x4(float2 uv)
            {
                const float4x4 M = float4x4(
                    0/16.0,  8/16.0,  2/16.0, 10/16.0,
                    12/16.0, 4/16.0, 14/16.0, 6/16.0,
                    3/16.0, 11/16.0, 1/16.0,  9/16.0,
                    15/16.0,7/16.0, 13/16.0, 5/16.0
                );
                float2 p = floor(frac(uv) * 4.0);
                int x = (int)p.x;
                int y = (int)p.y;
                return M[y][x];
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                float2 uv = TRANSFORM_TEX(v.uv,_MainTex);
                uv = uv * _Tiling.xy + _Scroll.xy * _Time.y;
                o.uv = uv;
                float2 ndc = o.positionHCS.xy / max(o.positionHCS.w, 1e-5);
                o.screenUV = ndc * 0.5 + 0.5;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half a = t.r;
                float b = bayer4x4(i.screenUV * _ScreenParams.xy);
                clip(a - _Cutoff - (b*0.1));
                half3 col = t.rgb * _Color.rgb;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
