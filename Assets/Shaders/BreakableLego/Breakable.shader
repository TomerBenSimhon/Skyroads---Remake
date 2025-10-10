Shader "Oren/BreakableCrackGlow_v6"
{
    Properties
    {
        _AlbedoTex("Albedo", 2D) = "white" {}
        _AlbedoColor("Albedo Color", Color) = (1,1,1,1)

        _NrmTex("Base Normal", 2D) = "bump" {}
        _NormalScale("Base Normal Scale", Range(0,2)) = 1

        _CrackTex("Crack Mask (white lines)", 2D) = "black" {}
        _CrackNoise("Crack Noise", 2D) = "gray" {}
        _CrackNormalTex("Crack Normal", 2D) = "bump" {}
        _CrackNormalScale("Crack Normal Scale", Range(0,2)) = 1

        _CrackScroll("Crack Noise Scroll XY", Vector) = (0,0.1,0,0)
        _CrackDilatePx("Crack Thickness (px)", Range(0,8)) = 3
        _CrackSoftEdge("Crack Soft Edge", Range(0.001,0.2)) = 0.06

        [HDR]_CrackColor("Crack Emissive Color", Color) = (0.2,0.9,1.5,1)
        _CrackInner("Crack Inner Intensity", Range(0,20)) = 8
        _CrackOuter("Crack Outer Intensity", Range(0,10)) = 2
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
        _DissolveEdge("Dissolve Edge Width", Range(0,0.2)) = 0.04
        [HDR]_DissolveColor("Dissolve Edge Color", Color) = (0.3,0.9,1.3,1)

        _Smoothness("Specular Smoothness", Range(0,1)) = 0.55
        _SpecularStrength("Specular Strength", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Textures + ST
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
            float  _CrackDilatePx, _CrackSoftEdge;
            float  _CrackInner, _CrackOuter;
            float  _CrackFlashSpeed, _CrackFlashIntensity, _CrackFlashMin;

            float  _RimPower, _RimIntensity, _RimFlashSpeed, _RimFlashIntensity, _RimFlashMin;

            float  _DissolveAmount, _DissolveEdge;
            float  _Smoothness, _SpecularStrength;

            // Auto from Unity: _CrackTex_TexelSize (x=1/w, y=1/h, z=w, w=h)
            float4 _CrackTex_TexelSize;

            struct appdata {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct v2f {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv          : TEXCOORD4;
            };

            float2 TransformUV(float2 uv, float4 st) { return uv * st.xy + st.zw; }

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

            float3 SampleBaseNormalWS(v2f i)
            {
                float2 uv = TransformUV(i.uv, _NrmTex_ST);
                float3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NrmTex, sampler_NrmTex, uv), _NormalScale);
                float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                return normalize(mul(nTS, TBN));
            }

            // mask + dilation (מרחיב קווים בפיקסלים)
            float CrackMaskDilated(float2 uv)
            {
                float2 uvC = TransformUV(uv, _CrackTex_ST);
                float2 uvN = TransformUV(uv, _CrackNoise_ST) + _CrackScroll.xy * _Time.y;

                float baseCrack = SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC).r;
                float noise = SAMPLE_TEXTURE2D(_CrackNoise, sampler_CrackNoise, uvN).r;
                float m = saturate(baseCrack * 0.9 + noise * 0.2);

                // dilation by max of neighbor taps
                float2 texel = _CrackTex_TexelSize.xy * _CrackDilatePx;
                float v = m;
                v = max(v, SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC + float2( texel.x, 0)).r);
                v = max(v, SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC + float2(-texel.x, 0)).r);
                v = max(v, SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC + float2(0,  texel.y)).r);
                v = max(v, SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC + float2(0, -texel.y)).r);
                v = max(v, SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC + texel).r);
                v = max(v, SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC - texel).r);
                v = max(v, SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC + float2(texel.x,-texel.y)).r);
                v = max(v, SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uvC + float2(-texel.x,texel.y)).r);

                // soft edge
                return smoothstep(1.0 - _CrackSoftEdge, 1.0, v);
            }

            // crack normal blended רק היכן שיש מסכה
            float3 CrackNormalWS(v2f i)
            {
                float2 uv = TransformUV(i.uv, _CrackNormalTex_ST);
                float3 crackTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_CrackNormalTex, sampler_CrackNormalTex, uv), _CrackNormalScale);
                float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                return normalize(mul(crackTS, TBN));
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

                float3 albedo = (SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, TransformUV(i.uv, _AlbedoTex_ST)) * _AlbedoColor).rgb;

                // normals
                float3 Nbase = SampleBaseNormalWS(i);
                float cracksMask = CrackMaskDilated(i.uv);
                float3 Ncrack = CrackNormalWS(i);
                float3 N = normalize(lerp(Nbase, Ncrack, cracksMask));

                float3 V = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));

                // lighting (Directional main)
                float3 color = SampleSH(N) * albedo;
                float3 Ldir = normalize(_MainLightPosition.xyz);
                float3 Lcol = _MainLightColor.rgb;
                float NdotL = saturate(dot(N, Ldir));
                float3 diffuse = albedo * Lcol * NdotL;
                float3 H = SafeNormalize(Ldir + V);
                float  spec = pow(saturate(dot(N, H)), lerp(8.0, 128.0, _Smoothness)) * _SpecularStrength;
                float3 specCol = Lcol * spec;

                // emissive: core + halo + flicker
                float core  = smoothstep(0.85, 1.0, cracksMask);
                float halo  = saturate(smoothstep(0.2, 0.85, cracksMask) - core);
                float crackFlash = lerp(_CrackFlashMin, _CrackFlashIntensity, 0.5 + 0.5 * sin(_Time.y * _CrackFlashSpeed * 6.2831853));
                float3 crackEmiss = _CrackColor.rgb * crackFlash * (core * _CrackInner + halo * _CrackOuter);

                // rim emissive + flicker
                float rimBase = pow(saturate(1.0 - dot(N, V)), _RimPower);
                float rimFlash = lerp(_RimFlashMin, _RimFlashIntensity, 0.5 + 0.5 * sin(_Time.y * _RimFlashSpeed * 6.2831853));
                float3 rimCol = _RimColor.rgb * rimBase * _RimIntensity * rimFlash;

                float3 dissolveEmiss = _DissolveColor.rgb * edgeF;

                float3 outCol = color + diffuse + specCol + crackEmiss + rimCol + dissolveEmiss;
                return half4(outCol, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
