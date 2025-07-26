Shader "URP/HighlightSweepOverlay_DirectionalControlled"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color (HDR)", Color) = (10, 5, 1, 1)

        _GlowDir ("Glow Direction (X,Y)", Vector) = (1, 1, 0, 0)

        _GlowFrequency ("Glow Frequency", Range(0.1, 10.0)) = 1.0
        _GlowWidth ("Glow Width", Range(0.001, 1.0)) = 0.2
        _GlowSpeed ("Glow Speed", Float) = 1.0
        _Alpha ("Alpha", Range(0, 1)) = 1.0
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

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.uv = IN.uv;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float time = _Time.y;
                float2 dir = normalize(_GlowDir.xy);

                // הקרנה של UV לאורך הכיוון
                float projection = dot(IN.uv, dir);

                // הגל: סינוס עם שליטה על תדירות (מרחק בין הפסים)
                float wave = 0.5 + 0.5 * sin((projection - time * _GlowSpeed) * _GlowFrequency * 6.2831);

                // טשטוש סביב מרכז הגל
                float glow = smoothstep(0.5 - _GlowWidth, 0.5 + _GlowWidth, wave);

                return float4(_GlowColor.rgb, glow * _Alpha);
            }
            ENDHLSL
        }
    }
}
