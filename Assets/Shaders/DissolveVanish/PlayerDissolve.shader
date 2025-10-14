Shader "Oren/PlayerURP_PBR_OccludedOutline_Dissolve"
{
    Properties
    {
        // Base PBR
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0,2)) = 1

        _MetallicGlossMap("Metallic(R) Smoothness(A)", 2D) = "black" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5

        _EmissionMap("Emission Map", 2D) = "black" {}
        [HDR]_EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionStrength("Emission Strength", Range(0,10)) = 1

        // Dissolve
        _NoiseMap("Noise", 2D) = "gray" {}
        _NoiseScale("Noise Tiling", Range(0.1, 100)) = 30
        _Dissolve("Dissolve", Range(0,1)) = 0
        _EdgeWidth("Edge Width", Range(0.001,0.2)) = 0.01
        [HDR]_EdgeColor("Edge Color", Color) = (0,1,1,1)

        // Outline
        [HDR]_OutlineColor("Outline Color", Color) = (1,0.6,0,1)
        _OutlineThickness("Outline Thickness (world units)", Range(0,0.1)) = 0.03
        [Toggle(_OUTLINE_ON)] _OutlineEnabled("Enable Outline", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            // Textures + samplers
            TEXTURE2D(_BaseMap);           SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);           SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap);  SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_EmissionMap);       SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_NoiseMap);          SAMPLER(sampler_NoiseMap);

            // Per-material
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _BumpScale;
                float  _Metallic;
                float  _Smoothness;
                float4 _EmissionColor;
                float  _EmissionStrength;

                float4 _BaseMap_ST;
                float4 _BumpMap_ST;
                float4 _MetallicGlossMap_ST;
                float4 _EmissionMap_ST;
                float4 _NoiseMap_ST;

                float  _NoiseScale;
                float  _Dissolve;
                float  _EdgeWidth;
                float4 _EdgeColor;

                float4 _OutlineColor;
                float  _OutlineThickness;
                float  _OutlineEnabled;   // <— toggle (0/1)
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
            };

            float2 ApplyST(float2 uv, float4 st){ return uv*st.xy + st.zw; }

            float3x3 BuildTBN(float3 nWS, float4 tWS)
            {
                float3 t = normalize(tWS.xyz);
                float3 b = normalize(cross(nWS, t) * tWS.w);
                return float3x3(t, b, nWS);
            }

            Varyings VertPBR(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS  = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
                OUT.uv         = IN.uv;
                return OUT;
            }

            // === ForwardLit fragment with Dissolve ===
            float4 FragPBR(Varyings IN) : SV_Target
            {
                // UVs
                float2 uvBase = ApplyST(IN.uv, _BaseMap_ST);
                float2 uvNrm  = ApplyST(IN.uv, _BumpMap_ST);
                float2 uvMS   = ApplyST(IN.uv, _MetallicGlossMap_ST);
                float2 uvEmi  = ApplyST(IN.uv, _EmissionMap_ST);
                float2 uvNoise= ApplyST(IN.uv * _NoiseScale, _NoiseMap_ST);

                // Sample maps
                float3 baseRGB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvBase).rgb * _BaseColor.rgb;

                float4 nTex    = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvNrm);
                float3 nTS     = UnpackNormalScale(nTex, _BumpScale);

                float4 ms      = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uvMS);
                float  metallic= saturate(ms.r + _Metallic);
                float  smooth  = saturate(ms.a + _Smoothness);

                float3 eTex    = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uvEmi).rgb;
                float3 emission= (_EmissionColor.rgb * eTex) * _EmissionStrength;

                // Dissolve mask from noise
                float noise    = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uvNoise).r;
                // Soft alpha: 0..1 across edge width
                float alphaMask = smoothstep(_Dissolve, _Dissolve + _EdgeWidth, noise);
                // Edge band for color
                float edgeBand  = saturate(
                    smoothstep(_Dissolve - _EdgeWidth, _Dissolve, noise) -
                    smoothstep(_Dissolve, _Dissolve + _EdgeWidth, noise)
                );
                // Final base color with edge tint
                float3 colorWithEdge = lerp(baseRGB, _EdgeColor.rgb, edgeBand);

                // World normal
                float3x3 TBN = BuildTBN(normalize(IN.normalWS), IN.tangentWS);
                float3 nWS   = normalize(mul(nTS, TBN));

                // Surface and lighting
                SurfaceData surf; ZERO_INITIALIZE(SurfaceData, surf);
                surf.albedo     = colorWithEdge;
                surf.metallic   = metallic;
                surf.specular   = 0;
                surf.smoothness = smooth;
                surf.normalTS   = float3(0,0,1); // supplying world normal via InputData
                surf.occlusion  = 1;
                surf.emission   = emission;
                surf.alpha      = alphaMask;

                InputData inData; ZERO_INITIALIZE(InputData, inData);
                inData.positionWS      = IN.positionWS;
                inData.normalWS        = nWS;
                inData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                inData.shadowCoord     = TransformWorldToShadowCoord(IN.positionWS);
                inData.bakedGI         = SampleSH(nWS);

                // Alpha clip dissolve
                clip(alphaMask - 0.5);

                return UniversalFragmentPBR(inData, surf);
            }

            // === Outline pass ===
            struct VaryingsOL { float4 positionCS : SV_POSITION; };

            VaryingsOL VertOutline(Attributes IN)
            {
                VaryingsOL OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS   = TransformObjectToWorldNormal(IN.normalOS);
                posWS += nWS * _OutlineThickness;
                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            float4 FragOutline() : SV_Target
            {
                // Toggle: if off, discard (no outline drawn)
                clip(_OutlineEnabled - 0.5);
                return _OutlineColor;
            }
        ENDHLSL

        // Pass: ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
                #pragma vertex   VertPBR
                #pragma fragment FragPBR
                #pragma target 4.5

                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
                #pragma multi_compile _ _ADDITIONAL_LIGHTS
                #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
                #pragma multi_compile _ _SHADOWS_SOFT
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile_fragment _ FOG_EXP2
                #pragma multi_compile_instancing
            ENDHLSL
        }

        // Pass: Outline only when occluded
        Pass
        {
            Name "OccludedOutline"
            Tags{ "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest Greater
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
                #pragma vertex   VertOutline
                #pragma fragment FragOutline
                #pragma target 4.5
                #pragma multi_compile_instancing
                // Optional keyword variant stripping if you want:
                // #pragma shader_feature_local _ _OUTLINE_ON
            ENDHLSL
        }
    }

    FallBack Off
}
