Shader "Oren/CrackGlowURP_Toon_Tiling_ToneMapped_BlinkOutline"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (0.18,0.5,0.9,1)

        _UseTriplanar("Use Triplanar Base", Float) = 1
        _TriScale("Triplanar Scale", Range(0.1,10)) = 2
        _TriSharp("Triplanar Sharpness", Range(1,16)) = 6
        _MinAlbedo("Min Albedo", Range(0,1)) = 0.05

        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0,2)) = 1

        _ToonSteps("Toon Steps", Range(1,8)) = 4
        _SpecSteps("Spec Steps", Range(1,8)) = 2
        _ToonEdgeBoost("Edge Boost", Range(0,1)) = 0.15
        _UseRamp("Use Ramp Texture", Float) = 0
        _ToonRamp("Toon Ramp (1D)", 2D) = "gray" {}
        _Specular("Specular Level", Range(0,1)) = 0.04
        _Smoothness("Smoothness", Range(0,1)) = 0.85

        _CrackMask("Crack Mask (white=crack)", 2D) = "black" {}
        _NoiseTex("Noise", 2D) = "gray" {}
        _NoiseScale("Noise Scale", Range(0.1,10)) = 2
        _NoiseSpeed("Noise Speed XY", Vector) = (0.5,0,0,0)
        _NoiseNormalStrength("Noise Normal Strength", Range(0,2)) = 0.6

        _SweepAmount("Sweep Amount", Range(0,1)) = 1
        _SweepDir("Sweep Dir XY (UV)", Vector) = (1,0,0,0)
        _SweepWidth("Sweep Width", Range(0.01,1)) = 0.15
        _SweepSpeed("Sweep Speed", Range(-10,10)) = 1.0
        _SweepOffset("Sweep Offset", Range(0,1)) = 0.0
        _SweepSoftness("Sweep Softness", Range(0.0,0.5)) = 0.12   // חדש, רכות המעבר

        [HDR]_GlowColor("Glow Color (HDR)", Color) = (0.2,0.9,1.5,1)
        _GlowIntensity("Glow Intensity", Range(0,10)) = 2.0
        _BlinkSpeed("Blink Speed", Range(0,10)) = 3
        _BlinkAmount("Blink Amount", Range(0,1)) = 0.6
        _CrackHardness("Crack Hardness", Range(0,5)) = 2
        _CrackFeather("Crack Feather", Range(0,0.2)) = 0.04
        _EmissionMul("Emission Mul", Range(0,3)) = 1

        _UseToneMap("Use ToneMap", Float) = 1
        _ToneStrength("Tone Strength", Range(0,1)) = 1

        [HDR]_OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width (world)", Range(0,0.1)) = 0.003

        _OutlineBlinkSpeed("Outline Blink Speed", Range(0,20)) = 6
        _OutlineIntensityMin("Outline Intensity Min", Range(0,3)) = 0.9
        _OutlineIntensityMax("Outline Intensity Max", Range(0,5)) = 1.5
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "UniversalMaterialType"="Lit" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);   SAMPLER(sampler_BumpMap);
            TEXTURE2D(_CrackMask); SAMPLER(sampler_CrackMask);
            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_ToonRamp);  SAMPLER(sampler_ToonRamp);
            float4 _NoiseTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor;
                float  _UseTriplanar, _TriScale, _TriSharp, _MinAlbedo;
                float4 _BumpMap_ST; float _BumpScale;
                float  _ToonSteps, _SpecSteps, _ToonEdgeBoost, _UseRamp, _Specular, _Smoothness;
                float4 _CrackMask_ST, _NoiseTex_ST;
                float  _NoiseScale; float4 _NoiseSpeed; float _NoiseNormalStrength;
                float  _SweepAmount; float4 _SweepDir; float _SweepWidth, _SweepSpeed, _SweepOffset, _SweepSoftness;
                float4 _GlowColor; float _GlowIntensity, _BlinkSpeed, _BlinkAmount, _CrackHardness, _CrackFeather, _EmissionMul;
                float  _UseToneMap, _ToneStrength;
                float4 _OutlineColor; float _OutlineWidth;
                float  _OutlineBlinkSpeed, _OutlineIntensityMin, _OutlineIntensityMax;
            CBUFFER_END

            struct Attributes{ float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionHCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float4 tangentWS:TEXCOORD2; float2 uv:TEXCOORD3; float3 viewDirWS:TEXCOORD4; float4 shadowCoord:TEXCOORD5; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes IN){
                Varyings O; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN,O); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(O);
                O.uv = TRANSFORM_TEX(IN.uv,_BaseMap);
                float3 ws=TransformObjectToWorld(IN.positionOS.xyz);
                O.positionWS=ws; O.positionHCS=TransformWorldToHClip(ws);
                O.normalWS=TransformObjectToWorldNormal(IN.normalOS);
                O.tangentWS=float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
                O.viewDirWS=GetWorldSpaceViewDir(ws);
                O.shadowCoord=TransformWorldToShadowCoord(ws);
                return O;
            }

            float3 TriplanarRGB(TEXTURE2D_PARAM(tex,samp), float3 wsPos, float3 wsNorm, float scale){
                float3 w=normalize(abs(wsNorm)); w=pow(w,_TriSharp.xxx); w/=max(1e-5,w.x+w.y+w.z);
                float3 sx=SAMPLE_TEXTURE2D(tex,samp,wsPos.zy*scale).rgb;
                float3 sy=SAMPLE_TEXTURE2D(tex,samp,wsPos.xz*scale).rgb;
                float3 sz=SAMPLE_TEXTURE2D(tex,samp,wsPos.xy*scale).rgb;
                return sx*w.x + sy*w.y + sz*w.z;
            }

            float3 GetWSNormal(float2 uvBump, float3 normalWS, float4 tangentWS){
                float3 t=normalize(tangentWS.xyz), n=normalize(normalWS), b=normalize(cross(n,t)*tangentWS.w);
                float3 nTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uvBump),_BumpScale);
                return normalize(mul(nTS,float3x3(t,b,n)));
            }

            float3 NoiseNormalWS(float2 uv, float3 n0, float4 tangentWS)
            {
                float2 texel = _NoiseTex_TexelSize.xy;
                float2 scale = _NoiseTex_ST.xy * _NoiseScale;
                float2 uvN   = uv * scale + _NoiseTex_ST.zw;

                float h  = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvN).r;
                float hx = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvN + float2(texel.x,0)).r - h;
                float hy = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvN + float2(0,texel.y)).r - h;

                float3 T = normalize(tangentWS.xyz);
                float3 B = normalize(cross(n0,T) * tangentWS.w);
                float3 nTS = normalize(float3(hx * _NoiseNormalStrength, hy * _NoiseNormalStrength, 1.0));
                return normalize(mul(nTS, float3x3(T,B,n0)));
            }

            void CrackCompute(float2 uv, out float crackCore, out float crackSoft, out float noiseGate)
            {
                float m = SAMPLE_TEXTURE2D(_CrackMask, sampler_CrackMask, uv*_CrackMask_ST.xy + _CrackMask_ST.zw).r;
                crackCore = saturate(pow(m, _CrackHardness + 1.0));
                crackSoft = smoothstep(0.0, max(1e-5,_CrackFeather), m);

                float2 uvn = uv * (_NoiseTex_ST.xy * _NoiseScale) + _NoiseTex_ST.zw + _Time.y * _NoiseSpeed.xy;
                float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvn).r;
                noiseGate = smoothstep(0.4, 0.6, n);
            }

            // Sweep רציף ללא קפיצה
            float DirectionalSweep(float2 uv)
            {
                float2 dir = normalize(_SweepDir.xy + 1e-6);
                float t = dot(uv, dir);                       // מיקום לאורך הכיוון
                float phase = _SweepOffset + _Time.y * _SweepSpeed;

                // מרחק מחזורי מהמרכז, עטוף לרצועה [0,0.5] באופן רציף
                float a = abs(frac(t - phase) - 0.5);         // 0 באמצע הרצועה, 0.5 בקצוות

                float w = saturate(_SweepWidth);              // חצי-רוחב הרצועה, 0..1
                w = max(w, 1e-4);

                // מסיכה ליניארית בתוך הרוחב
                float band = saturate(1.0 - a / w);

                // ריכוך קצוות, רציף בלופ
                float s = saturate(_SweepSoftness);           // 0=חד, 0.5=רך
                // ממפה band דרך עקומה חלקה, ללא כיבוי פתאומי
                band = smoothstep(0.0, s, band);

                return band;
            }

            float3 ToonLighting(float3 albedo, float3 N, float3 V, float3 posWS, float4 shadowCoord)
            {
                float3 col=0;
                Light Lm=GetMainLight(shadowCoord);
                float3 L=Lm.direction;
                float ndl=saturate(dot(N,L));
                float lambert;

                if (_UseRamp > 0.5) lambert = SAMPLE_TEXTURE2D(_ToonRamp, sampler_ToonRamp, float2(ndl,0.5)).r;
                else { float s=max(1.0,_ToonSteps); lambert=floor(ndl*s)/s; lambert=saturate(lambert + _ToonEdgeBoost*(1.0 - saturate(dot(N,V)))); }

                float3 diff = albedo * Lm.color * lambert * Lm.shadowAttenuation;

                float3 H=normalize(L+V);
                float ndh=saturate(dot(N,H));
                float spec = pow(ndh, lerp(16,256,_Smoothness));
                float ss=max(1.0,_SpecSteps); spec=floor(spec*ss)/ss;
                float3 F0=_Specular.xxx;
                float3 specCol = spec * (F0 + (1.0-F0)*pow(1.0 - saturate(dot(H,V)),5.0)) * Lm.color * Lm.shadowAttenuation;

                col += diff + specCol;

                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                uint lc=GetAdditionalLightsCount();
                for(uint i=0;i<lc;i++){
                    Light l=GetAdditionalLight(i,posWS);
                    float ndl2=saturate(dot(N,l.direction));
                    float lam2 = (_UseRamp>0.5) ? SAMPLE_TEXTURE2D(_ToonRamp, sampler_ToonRamp, float2(ndl2,0.5)).r
                                                : floor(ndl2*max(1.0,_ToonSteps))/max(1.0,_ToonSteps);
                    float atten = l.distanceAttenuation * l.shadowAttenuation;
                    col += albedo * l.color * lam2 * atten;
                }
                #endif

                return col;
            }

            half4 frag(Varyings IN):SV_Target
            {
                float3 baseRGB = (_UseTriplanar>0.5)
                    ? TriplanarRGB(TEXTURE2D_ARGS(_BaseMap,sampler_BaseMap), IN.positionWS, IN.normalWS, _TriScale)
                    : SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                float3 albedo = max(_MinAlbedo.xxx, baseRGB) * _BaseColor.rgb;

                float2 uvB = IN.uv * _BumpMap_ST.xy + _BumpMap_ST.zw;
                float3 N0  = normalize(IN.normalWS);
                float3 N   = GetWSNormal(uvB, N0, IN.tangentWS);

                if (_NoiseNormalStrength>0.001){
                    float cCore, cSoft, nGate; CrackCompute(IN.uv, cCore, cSoft, nGate);
                    float3 Nn = NoiseNormalWS(IN.uv, N0, IN.tangentWS);
                    N = normalize(lerp(N, Nn, cSoft));
                }

                float3 V = normalize(IN.viewDirWS);
                float3 litCol = ToonLighting(albedo, N, V, IN.positionWS, IN.shadowCoord);

                float crackCore, crackSoft, noiseGate; CrackCompute(IN.uv, crackCore, crackSoft, noiseGate);
                float blink = lerp(1.0, 0.5+0.5*sin(_Time.y*_BlinkSpeed), _BlinkAmount);

                // משתמש בפונקציה הרציפה החדשה
                float sweep = DirectionalSweep(IN.uv);
                float gate = noiseGate * lerp(1.0, sweep, _SweepAmount);

                float glow = (crackCore*1.2 + crackSoft*0.5) * gate * blink * _GlowIntensity * _EmissionMul;
                float3 emission = _GlowColor.rgb * glow;

                float3 finalCol = litCol + emission;

                if (_UseToneMap > 0.5){
                    float3 tm = finalCol / (1.0 + finalCol);
                    finalCol = lerp(finalCol, tm, _ToneStrength);
                }

                return half4(finalCol, 1);
            }
            ENDHLSL
        }

        // Outline עם הבהוב בעוצמה בלבד, ללא שינוי
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
                float  _OutlineBlinkSpeed, _OutlineIntensityMin, _OutlineIntensityMax;
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

                float k = 0.5 + 0.5 * sin(_Time.y * _OutlineBlinkSpeed);
                o.inten = lerp(_OutlineIntensityMin, _OutlineIntensityMax, k);
                return o;
            }

            half4 frag(V i):SV_Target
            {
                return half4(_OutlineColor.rgb * i.inten, _OutlineColor.a);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
