Shader "Universal Render Pipeline/Lit/StylizedLavaFull"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo (RGB)", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)

        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Strength", Range(0,2)) = 1

        _MetallicGlossMap("Metallic (R) Smoothness (A)", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5

        _EmissionMap("Emission Map", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (1,0.5,0.2,1)
        _EmissionStrength("Emission Strength", Range(0,20)) = 4

        _UVScroll("Global UV Scroll XY", Vector) = (0.1, 0, 0, 0)

        _NoiseTex("Noise Map", 2D) = "gray" {}
        _NoiseTiling("Noise Tiling XY", Vector) = (3,3,0,0)
        _NoiseScroll("Noise UV Scroll XY", Vector) = (0.1,0.05,0,0)

        _DeformAmp("Deform Amplitude", Range(0,1)) = 0.1
        [Toggle(_DEFORM_ON)] _DeformEnabled("Vertex Displacement", Float) = 1

        _CrackThreshold("Crack Threshold", Range(0,1)) = 0.55
        _CrackSoftness("Crack Softness", Range(0,0.5)) = 0.08
    }

        SubShader
        {
            Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "RenderType" = "Opaque" }
            LOD 300

            Pass
            {
                Name "ForwardLit"
                Tags { "LightMode" = "UniversalForward" }

                HLSLPROGRAM
                #pragma target 4.5
                #pragma vertex vert
                #pragma fragment frag

                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
                #pragma multi_compile _ _ADDITIONAL_LIGHTS
                #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
                #pragma multi_compile _ _SHADOWS_SOFT
                #pragma multi_compile_instancing
                #pragma shader_feature_local _DEFORM_ON

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normalOS   : NORMAL;
                    float4 tangentOS  : TANGENT;
                    float2 uv         : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float4 positionHCS : SV_Position;
                    float3 positionWS  : TEXCOORD0;
                    float3 normalWS    : TEXCOORD1;
                    float4 tangentWS   : TEXCOORD2;
                    float2 uv          : TEXCOORD3;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                CBUFFER_START(UnityPerMaterial)
                    float4 _BaseColor;
                    float4 _EmissionColor;
                    float  _EmissionStrength;
                    float  _Metallic;
                    float  _Smoothness;

                    float4 _BaseMap_ST;           // x,y = tiling; z,w = offset
                    float4 _NormalMap_ST;
                    float4 _MetallicGlossMap_ST;
                    float4 _EmissionMap_ST;

                    float4 _UVScroll;            // global XY scroll
                    float4 _NoiseTiling;         // XY tiling for noise
                    float4 _NoiseScroll;         // XY scroll for noise

                    float  _DeformAmp;
                    float  _CrackThreshold;
                    float  _CrackSoftness;
                    float  _NormalScale;
                CBUFFER_END

                TEXTURE2D(_BaseMap);           SAMPLER(sampler_BaseMap);
                TEXTURE2D(_NormalMap);         SAMPLER(sampler_NormalMap);
                TEXTURE2D(_MetallicGlossMap);  SAMPLER(sampler_MetallicGlossMap);
                TEXTURE2D(_EmissionMap);       SAMPLER(sampler_EmissionMap);
                TEXTURE2D(_NoiseTex);          SAMPLER(sampler_NoiseTex);

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    UNITY_SETUP_INSTANCE_ID(IN);
                    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                    float3 pos = IN.positionOS.xyz;
                    #if defined(_DEFORM_ON)
                        float2 nuv = IN.uv * _NoiseTiling.xy + _Time.x * _NoiseScroll.xy;
                        float n = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, nuv, 0).r;
                        float h = (n * 2 - 1) * _DeformAmp;
                        pos += normalize(IN.normalOS) * h;
                    #endif

                    float3 wpos = TransformObjectToWorld(pos);
                    OUT.positionHCS = TransformWorldToHClip(wpos);
                    OUT.positionWS = wpos;
                    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                    OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
                    OUT.uv = IN.uv;
                    return OUT;
                }

                float4 frag(Varyings IN) : SV_Target
                {
                    // 1) Build your scroll & tiling + offset UVs
                    float2 scrollUV = _Time.x * _UVScroll.xy;
                    float2 uvAlbedo = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw + scrollUV;
                    float2 uvNorm = IN.uv * _NormalMap_ST.xy + _NormalMap_ST.zw + scrollUV;
                    float2 uvMetal = IN.uv * _MetallicGlossMap_ST.xy + _MetallicGlossMap_ST.zw + scrollUV;
                    float2 uvEmiss = IN.uv * _EmissionMap_ST.xy + _EmissionMap_ST.zw + scrollUV;

                    // 2) Sample PBR maps
                    float4  colTex = SAMPLE_TEXTURE2D(_BaseMap,          sampler_BaseMap,         uvAlbedo) * _BaseColor;
                    float3  normTS = UnpackNormalScale(
                                          SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,     uvNorm),
                                          _NormalScale
                                       );
                    float4  msTex = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uvMetal);
                    float   metal = saturate(msTex.r) * _Metallic;
                    float   smooth = saturate(msTex.a) * _Smoothness;
                    float3  eTex = SAMPLE_TEXTURE2D(_EmissionMap,    sampler_EmissionMap,      uvEmiss).rgb;

                    // 3) Lava crack/emission mask (noise-driven, separate scroll)
                    float2 nuv = IN.uv * _NoiseTiling.xy + _Time.x * _NoiseScroll.xy;
                    float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nuv).r;
                    float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nuv * 0.5 + float2(7.13,-2.47)).r;
                    float pat = saturate(n1 * 0.7 + n2 * 0.3);
                    float cMask = 1 - smoothstep(_CrackThreshold, _CrackThreshold + _CrackSoftness, pat);
                    float lavaMask = 1 - cMask;
                    float3 emission = eTex * _EmissionColor.rgb * _EmissionStrength * lavaMask;

                    // 4) Build TBN & world-space normal
                    float3 T = normalize(IN.tangentWS.xyz);
                    float3 N = normalize(IN.normalWS);
                    float3 B = cross(N, T) * IN.tangentWS.w;
                    float3x3 TBN = float3x3(T, B, N);
                    float3 normalWS = normalize(mul(normTS, TBN));

                    // 5) URP PBR lighting
                    InputData  L;
                    ZERO_INITIALIZE(InputData, L);
                    L.positionWS = IN.positionWS;
                    L.normalWS = normalWS;
                    L.viewDirectionWS = GetWorldSpaceViewDir(IN.positionWS);
                    L.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    L.bakedGI = 0;
                    L.fogCoord = 0;
                    L.vertexLighting = 0;
                    L.normalizedScreenSpaceUV = 0;
                    L.shadowMask = 0;

                    SurfaceData S;
                    ZERO_INITIALIZE(SurfaceData, S);
                    S.albedo = colTex.rgb;
                    S.metallic = metal;
                    S.smoothness = smooth;
                    S.normalTS = normTS;
                    S.occlusion = 1;
                    S.emission = emission;
                    S.alpha = colTex.a;

                    return UniversalFragmentPBR(L, S);
                }
                ENDHLSL
            }
        }
            FallBack Off
}