Shader "Custom/NormalMapDebug"
{
    Properties
    {
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0,2)) = 1
    }

        SubShader
        {
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "RenderType" = "Opaque"
            }
            LOD 100

            Pass
            {
                Name "DebugNormals"
                Tags { "LightMode" = "UniversalForward" }

                HLSLPROGRAM
                #pragma target 4.5
                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
                float _NormalScale;

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv         : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionHCS : SV_Position;
                    float2 uv          : TEXCOORD0;
                };

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    // pass UV straight through
                    OUT.uv = IN.uv;
                    // standard object → clip
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    return OUT;
                }

                float4 frag(Varyings IN) : SV_Target
                {
                    // sample the normal map (rgb) and unwrap from [0,1] → [-1,1]
                    float3 nTS = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv).rgb * 2 - 1;
                    // remap to [0,1] so we can see it
                    float3 debugCol = nTS * 0.5 + 0.5;
                    return float4(debugCol, 1);
                }
                ENDHLSL
            }
        }

            FallBack Off
}