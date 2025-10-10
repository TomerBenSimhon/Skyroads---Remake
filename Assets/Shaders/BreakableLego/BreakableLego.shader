Shader "Oren/CrackedGlowPBR_Simple_v5"
{
    Properties
    {
        _AlbedoTex("Albedo", 2D) = "white" {}
        _AlbedoColor("Albedo Color", Color) = (1,1,1,1)

        _NrmTex("Base Normal", 2D) = "bump" {}
        _NormalScale("Base Normal Scale", Range(0,2)) = 1

        _CrackTex("Crack Mask (white crack)", 2D) = "black" {}
        _CrackNoise("Crack Noise", 2D) = "gray" {}
        _CrackScroll("Crack Noise Scroll XY", Vector) = (0.0,0.1,0,0)
        _CrackWidth("Crack Width", Range(0.001,0.3)) = 0.04

        _CrackNormalTex("Crack Normal", 2D) = "bump" {}
        _CrackNormalScale("Crack Normal Scale", Range(0,2)) = 1

        [HDR]_CrackColor("Crack Emissive Color", Color) = (0.2,0.9,1.5,1)
        _CrackGlow("Crack Glow Intensity", Range(0,10)) = 3
        _CrackFlashSpeed("Crack Flash Speed", Range(0,10)) = 1
        _CrackFlashIntensity("Crack Flash Intensity", Range(0,2)) = 1
        _CrackFlashMin("Crack Flash Min", Range(0,1)) = 0

        _RimPower("Rim Power", Range(0.1,8)) = 3
        _RimIntensity("Rim Intensity", Range(0,5)) = 0.6
        _RimFlashSpeed("Rim Flash Speed", Range(0,10)) = 1
        _RimFlashIntensity("Rim Flash Intensity", Range(0,2)) = 1
        _RimFlashMin("Rim Flash Min", Range(0,1)) = 0
        [HDR]_RimColor("Rim Color", Color) = (1,1,1,1)

        _DissolveTex("Dissolve Noise", 2D) = "gray" {}
        _DissolveAmount("Dissolve Amount", Range(0,1)) = 0
        _DissolveEdge("Dissolve Edge Width", Range(0.0,0.2)) = 0.04
        [HDR]_DissolveColor("Dissolve Edge Color", Color) = (0.3,0.9,1.3,1)

        _Smoothness("Specular Smoothness", Range(0,1)) = 0.5
        _SpecularStrength("Specular Strength", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250

        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_AlbedoTex);      SAMPLER(sampler_AlbedoTex);
            TEXTURE2D(_NrmTex);         SAMPLER(sampler_NrmTex);
            TEXTURE2D(_CrackTex);       SAMPLER(sampler_CrackTex);
            TEXTURE2D(_CrackNoise);     SAMPLER(sampler_CrackNoise);
            TEXTURE2D(_CrackNormalTex); SAMPLER(sampler_CrackNormalTex);
            TEXTURE2D(_DissolveTex);    SAMPLER(sampler_DissolveTex);

            float4 _AlbedoTex_ST, _NrmTex_ST, _CrackTex_ST, _CrackNoise_ST, _CrackNormalTex_ST, _DissolveTex_ST;
            float4 _AlbedoColor, _CrackColor, _RimColor, _DissolveColor;
            float4 _CrackScroll;

            float  _NormalScale, _CrackNormalScale;
            float  _CrackWidth, _CrackGlow;
            float  _CrackFlashSpeed, _CrackFlashIntensity, _CrackFlashMin;

            float  _RimPower, _RimIntensity;
            float  _RimFlashSpeed, _RimFlashIntensity, _RimFlashMin;

            float  _DissolveAmount, _DissolveEdge;
            float  _Smoothness, _SpecularStrength;

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv          : TEXCOORD4;
            };

            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n   = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionHCS = p.positionCS;
                o.positionWS  = p.positionWS;
                o.normalWS    = normalize(n.normalWS);
                o.tangentWS   = normalize(n.tangentWS);
                o.bitangentWS = normalize(n.bitangentWS);
                o.uv = v.uv;
                return o;
            }

            float2 TransformUV(float2 uv, float4 st) { return uv * st.xy + st.zw; }

            // בסיס: נורמל כללי
            float3 SampleBaseNormalWS(v2f i)
            {
                float2 uv = TransformUV(i.uv, _NrmTex_ST);
                float3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NrmTex, sampler_NrmTex, uv), _NormalScale);
                float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                return normalize(mul(nTS, TBN));
            }

            // מסכת סדק 0..1
            float CrackMask(float2 uv)
            {
                float2 uvC = TransformUV(uv, _CrackTex_ST);
                float2 uvN = TransformUV(uv, _CrackNoise_ST) + _CrackScroll.xy * _Time.y;
                float baseCrack = SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC).r;
                float noise = SAMPLE_TEXTURE2D(_CrackNoise, sampler_CrackNoise, uvN).r;
                float v = saturate(baseCrack * 0.9 + noise * 0.2);
                return smoothstep(1.0 - _CrackWidth, 1.0, v);
            }

            // הזרקת נורמל של הסדקים רק במקום שיש מסכה
            float3 BlendCrackNormalWS(v2f i, float mask)
            {
                float2 uv = TransformUV(i.uv, _CrackNormalTex_ST);
                float3 crackTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_CrackNormalTex, sampler_CrackNormalTex, uv), _CrackNormalScale);
                float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                float3 crackWS = normalize(mul(crackTS, TBN));

                float3 baseN = normalize(i.normalWS); // ישולב עם בסיס בהמשך
                // blend נורמל לפי המסכה: נורמל משוקלל עם רה-נורמליזציה
                float3 blended = normalize(lerp(baseN, crackWS, mask));
                return blended;
            }

            void DissolveEval(float2 uv, out float clipVal, out float edgeF)
            {
                float2 uvD = TransformUV(uv, _DissolveTex_ST);
                float d = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, uvD).r;
                clipVal = d - _DissolveAmount;
                float edge = saturate((_DissolveAmount - d) / max(_DissolveEdge, 1e-5));
                edgeF = smoothstep(0.0, 1.0, edge);
            }

            half4 frag(v2f i) : SV_Target
            {
                float clipVal, edgeF;
                DissolveEval(i.uv, clipVal, edgeF);
                clip(clipVal);

                float2 uvA = TransformUV(i.uv, _AlbedoTex_ST);
                float3 albedo = (SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, uvA) * _AlbedoColor).rgb;

                // מסכה לסדקים
                float cracks = CrackMask(i.uv);

                // נורמל סופי: בסיס + תוספת סדקים
                float3 Nbase = SampleBaseNormalWS(i);
                float3 N = normalize(lerp(Nbase, BlendCrackNormalWS(i, 1.0), cracks));

                float3 V = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
                float3 color = SampleSH(N) * albedo;

                // אור ראשי
                Light mainL = GetMainLight();
                float3 L = normalize(mainL.direction);
                float3 Lc = mainL.color;
                float NdotL = saturate(dot(N, L));
                float3 diffuse = albedo * Lc * NdotL;

                // ספֶקולר
                float3 H = SafeNormalize(L + V);
                float spec = pow(saturate(dot(N, H)), lerp(8.0, 128.0, _Smoothness)) * _SpecularStrength;
                float3 specCol = Lc * spec;

                // הבהוב סדקים
                float crackFlash = lerp(_CrackFlashMin, _CrackFlashIntensity, 0.5 + 0.5 * sin(_Time.y * _CrackFlashSpeed * 6.2831));
                float3 crackEmiss = _CrackColor.rgb * _CrackGlow * crackFlash * cracks;

                // Rim מהבהב
                float rimBase = pow(saturate(1.0 - dot(N, V)), _RimPower);
                float rimFlash = lerp(_RimFlashMin, _RimFlashIntensity, 0.5 + 0.5 * sin(_Time.y * _RimFlashSpeed * 6.2831));
                float3 rimCol = _RimColor.rgb * rimBase * _RimIntensity * rimFlash;

                float3 dissolveEmiss = _DissolveColor.rgb * edgeF;

                color += diffuse + specCol + crackEmiss + rimCol + dissolveEmiss;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
