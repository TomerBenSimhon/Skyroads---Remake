Shader "Custom/VideoBoost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorBoost ("Color Boost", Float) = 1.0
        _Saturation ("Saturation", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _ColorBoost;
            float _Saturation;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                // הגברת בהירות (color boost)
                col.rgb *= _ColorBoost;

                // הגברת סאטורציה
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(gray.xxx, col.rgb, _Saturation);

                return col;
            }
            ENDCG
        }
    }
}
