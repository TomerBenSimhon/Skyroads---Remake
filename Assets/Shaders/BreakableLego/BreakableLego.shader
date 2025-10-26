Shader "Oren/LegoBreak_Glow_PBR_URP_Triplanar_CrackGlow_OutlineFixed_Final_NoRedefine"
{
    Properties
    {
        // אל תגדיר מחדש את _BaseMap – הוא קיים כבר ב-URP
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _MetallicGlossMap ("Metallic Map (R=Metallic, A=Smoothness)", 2D) = "black" {}
        _CrackMask ("Crack Mask (White = Glow)", 2D) = "white" {}
        _CrackIntensity ("Crack Intensity", Range(0,5)) = 1

        _NormalStrength ("Normal Strength", Range(0,2)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0.5
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        _WorldTexTiling ("World Texture Tiling", Vector) = (0.05,0.05,0.05,0)
        _WorldTexOffset ("World Texture Offset", Vector) = (0,0,0,0)
        _TriSharpness ("Triplanar Sharpness", Range(1,16)) = 6

        [HDR]_GlowColor ("Glow Color (HDR)", Color) = (2, 0.4, 0.1, 1)
        _GlowIntensity ("Glow Intensity", Range(0,10)) = 2
        _GlowPulseSpeed ("Glow Pulse Speed", Range(0,10)) = 2

        [HDR]_OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineWidth ("Outline Width (world)", Range(0,0.1)) = 0.005
        _OutlineBlinkSpeed ("Outline Blink Speed", Range(0,20)) = 4
        _OutlineIntensityMin ("Outline Intensity Min", Range(0,3)) = 0.8
        _OutlineIntensityMax ("Outline Intensity Max", Range(0,5)) = 1.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; };
            struct Varyings   { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float4 tangentWS:TEXCOORD2; };

            TEXTURE2D(_NormalMap);         SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MetallicGlossMap);  SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_CrackMask);         SAMPLER(sampler_CrackMask);

            CBUFFER_START(UnityPerMaterial)
                float _NormalStrength;
                float _Metallic;
                float _Smoothness;
                float _CrackIntensity;
                float4 _WorldTexTiling;
                float4 _WorldTexOffset;
                float  _TriSharpness;
                float4 _GlowColor;
                float  _GlowIntensity;
                float  _GlowPulseSpeed;
            CBUFFER_END

            // ---------- Triplanar ----------
            float3 TriplanarRGB(TEXTURE2D_PARAM(tex,samp), float3 wp, float3 wn)
            {
                float3 w = normalize(abs(wn));
                w = pow(w, _TriSharpness.xxx);
                w /= max(1e-5, w.x+w.y+w.z);
                float3 p = wp * _WorldTexTiling.xyz + _WorldTexOffset.xyz;
                float3 X = SAMPLE_TEXTURE2D(tex,samp,p.zy).rgb;
                float3 Y = SAMPLE_TEXTURE2D(tex,samp,p.xz).rgb;
                float3 Z = SAMPLE_TEXTURE2D(tex,samp,p.xy).rgb;
                return X*w.x + Y*w.y + Z*w.z;
            }

            float4 TriplanarRGBA(TEXTURE2D_PARAM(tex,samp), float3 wp, float3 wn)
            {
                float3 w = normalize(abs(wn));
                w = pow(w,_TriSharpness.xxx);
                w /= max(1e-5,w.x+w.y+w.z);
                float3 p = wp*_WorldTexTiling.xyz + _WorldTexOffset.xyz;
                float4 X=SAMPLE_TEXTURE2D(tex,samp,p.zy);
                float4 Y=SAMPLE_TEXTURE2D(tex,samp,p.xz);
                float4 Z=SAMPLE_TEXTURE2D(tex,samp,p.xy);
                return X*w.x + Y*w.y + Z*w.z;
            }

            float3 TriplanarNormalWS(TEXTURE2D_PARAM(ntex,nsamp),float3 wp,float3 wn,float s)
            {
                float3 w=normalize(abs(wn));
                w=pow(w,_TriSharpness.xxx);
                w/=max(1e-5,w.x+w.y+w.z);
                float3 p=wp*_WorldTexTiling.xyz + _WorldTexOffset.xyz;
                float3 nX=UnpackNormalScale(SAMPLE_TEXTURE2D(ntex,nsamp,p.zy),s);
                float3 nY=UnpackNormalScale(SAMPLE_TEXTURE2D(ntex,nsamp,p.xz),s);
                float3 nZ=UnpackNormalScale(SAMPLE_TEXTURE2D(ntex,nsamp,p.xy),s);
                float3x3 TBN_X=float3x3(float3(0,1,0),float3(0,0,1),float3(1,0,0));
                float3x3 TBN_Y=float3x3(float3(1,0,0),float3(0,0,1),float3(0,1,0));
                float3x3 TBN_Z=float3x3(float3(1,0,0),float3(0,1,0),float3(0,0,1));
                float3 NX=normalize(mul(nX,TBN_X));
                float3 NY=normalize(mul(nY,TBN_Y));
                float3 NZ=normalize(mul(nZ,TBN_Z));
                return normalize(NX*w.x + NY*w.y + NZ*w.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS  = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                SurfaceData surfaceData; ZERO_INITIALIZE(SurfaceData, surfaceData);

                // Albedo (משתמש ב־_BaseMap מה־URP)
                surfaceData.albedo = TriplanarRGB(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), IN.positionWS, IN.normalWS);

                // Normal map
                float3 Nw = TriplanarNormalWS(TEXTURE2D_ARGS(_NormalMap, sampler_NormalMap), IN.positionWS, IN.normalWS, _NormalStrength);
                float3x3 T2W = CreateTangentToWorld(IN.normalWS, IN.tangentWS.xyz, IN.tangentWS.w);
                float3x3 W2T = transpose(T2W);
                surfaceData.normalTS = normalize(mul(Nw, W2T));

                // Metallic + Smoothness
                float4 mgl = TriplanarRGBA(TEXTURE2D_ARGS(_MetallicGlossMap, sampler_MetallicGlossMap), IN.positionWS, IN.normalWS);
                surfaceData.metallic   = lerp(_Metallic, mgl.r, 0.5);
                surfaceData.smoothness = lerp(_Smoothness, mgl.a, 0.5);
                surfaceData.occlusion  = 1.0;

                // Crack emission
                float crack = TriplanarRGB(TEXTURE2D_ARGS(_CrackMask, sampler_CrackMask), IN.positionWS, IN.normalWS).r;
                float pulse = 0.5 + 0.5 * sin(_Time.y * _GlowPulseSpeed);
                surfaceData.emission = _GlowColor.rgb * (_GlowIntensity * pulse * crack * _CrackIntensity);

                // Lighting
                InputData inputData; ZERO_INITIALIZE(InputData, inputData);
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalize(mul(surfaceData.normalTS, T2W));
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.bakedGI = SampleSH(inputData.normalWS);

                half4 col = UniversalFragmentPBR(inputData, surfaceData);
                col.a = 1;
                return col;
            }
            ENDHLSL
        }

        // -------- OUTLINE PASS --------
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineBlinkSpeed;
                float  _OutlineIntensityMin;
                float  _OutlineIntensityMax;
            CBUFFER_END

            struct A { float4 pos:POSITION; float3 normal:NORMAL; };
            struct V { float4 pos:SV_POSITION; float inten:TEXCOORD0; };

            V vert(A IN)
            {
                V o;
                float3 ws = TransformObjectToWorld(IN.pos.xyz);
                float3 n  = TransformObjectToWorldNormal(IN.normal);
                ws += n * _OutlineWidth;
                o.pos = TransformWorldToHClip(ws);
                float blink = 0.5 + 0.5 * sin(_Time.y * _OutlineBlinkSpeed);
                o.inten = lerp(_OutlineIntensityMin, _OutlineIntensityMax, blink);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                return half4(_OutlineColor.rgb * i.inten, _OutlineColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
