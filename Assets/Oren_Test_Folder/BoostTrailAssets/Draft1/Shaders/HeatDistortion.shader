Shader "URP/Boost/HeatDistortion_MaskedV2"
{
    Properties
    {
        _NoiseTex("Noise", 2D) = "gray" {}
        _MaskTex("Edge Mask (use flame_soft)", 2D) = "white" {}
        _Distortion("Distortion Strength", Range(0,0.1)) = 0.02
        _Opacity("Opacity", Range(0,1)) = 0.4
        _Scroll ("Noise Scroll", Vector) = (0.0, 0.2, 0, 0)
        _NoiseTiling ("Noise Tiling", Float) = 2.0
        _DepthFade ("Depth Fade", Range(0.001,2)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+20" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float4 screenPos:TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float _Distortion, _Opacity, _NoiseTiling, _DepthFade;
                float4 _NoiseTex_ST, _MaskTex_ST, _Scroll;
            CBUFFER_END

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_MaskTex);  SAMPLER(sampler_MaskTex);
            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            Varyings vert(Attributes v){
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.screenPos = o.positionHCS;
                return o;
            }

            half4 frag(Varyings i):SV_Target
            {
                // מסכה רכה מה־R גם אם אין אלפא
                float2 uvMask = TRANSFORM_TEX(i.uv, _MaskTex);
                half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask).r;
                mask = smoothstep(0.0, 0.95, mask);

                // נויז דו-אוקטבות חלק
                float2 uvN = TRANSFORM_TEX(i.uv, _NoiseTex) * _NoiseTiling + _Time.yx * _Scroll.xy;
                half2 n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvN).rg * 2 - 1;
                half2 n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvN * 2.13).rg * 2 - 1;
                half2 n = normalize(n1*0.6 + n2*0.4);

                // UV למסך + קלמפ
                float2 uvScreen = (i.screenPos.xy / max(i.screenPos.w, 1e-5)) * 0.5 + 0.5;
                uvScreen = saturate(uvScreen + n * (_Distortion * mask));

                // Depth fade למניעת קצה מרובע על גיאומטריה
                float sceneDepth01 = SampleSceneDepth(uvScreen);
                float sceneLinear = Linear01Depth(sceneDepth01, _ZBufferParams);
                float fragLinear  = Linear01Depth(i.screenPos.z / i.screenPos.w, _ZBufferParams);
                float fade = saturate((sceneLinear - fragLinear) / max(_DepthFade, 1e-4));

                half3 col = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen).rgb;
                half a = saturate(_Opacity * mask * fade);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
