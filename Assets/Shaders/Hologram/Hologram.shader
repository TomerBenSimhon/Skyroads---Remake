Shader "Oren/Hologram_OutlineNoise_6Colors_WithNoiseMap"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NoiseMap("Noise Texture (optional)", 2D) = "gray" {}

        _UseNoiseMap("Use Noise Map (0=Off, 1=On)", Range(0,1)) = 0

        _EdgeColor1("Edge Color 1", Color) = (0.2, 0.8, 1.0, 1)
        _EdgeColor2("Edge Color 2", Color) = (1.0, 0.2, 1.0, 1)
        _EdgeColor3("Edge Color 3", Color) = (0.2, 1.0, 0.5, 1)
        _EdgeColor4("Edge Color 4", Color) = (1.0, 0.8, 0.2, 1)
        _EdgeColor5("Edge Color 5", Color) = (1.0, 0.3, 0.3, 1)
        _EdgeColor6("Edge Color 6", Color) = (0.3, 0.3, 1.0, 1)

        _OutlineIntensity("Outline Intensity", Range(0, 5)) = 2.0
        _FresnelPower("Fresnel Power", Range(0.1, 10)) = 3.0
        _NoiseScale("Noise Scale", Range(0.1, 10)) = 3.0
        _NoiseSpeed("Noise Speed", Range(0.1, 10)) = 2.0
        _ColorShiftSpeed("Color Shift Speed", Range(0.1, 10)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldTangent : TEXCOORD2;
                float3 worldBinormal : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            float _UseNoiseMap;
            float _OutlineIntensity, _FresnelPower, _NoiseScale, _NoiseSpeed, _ColorShiftSpeed;
            float4 _EdgeColor1, _EdgeColor2, _EdgeColor3, _EdgeColor4, _EdgeColor5, _EdgeColor6;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.worldNormal = normalize(TransformObjectToWorldNormal(v.normalOS));
                o.worldTangent = normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                o.worldBinormal = cross(o.worldNormal, o.worldTangent) * v.tangentOS.w;
                o.uv = v.uv;
                return o;
            }

            float hash(float n) { return frac(sin(n) * 43758.5453); }

            float noise(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float n = p.x + p.y * 57.0 + 113.0 * p.z;
                return lerp(
                    lerp(
                        lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                        lerp(hash(n + 57.0), hash(n + 58.0), f.x),
                        f.y),
                    lerp(
                        lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                        lerp(hash(n + 170.0), hash(n + 171.0), f.x),
                        f.y),
                    f.z);
            }

            float3 getMultiColor(float t)
            {
                float segment = frac(t) * 6.0;
                if (segment < 1.0)
                    return lerp(_EdgeColor1.rgb, _EdgeColor2.rgb, segment);
                else if (segment < 2.0)
                    return lerp(_EdgeColor2.rgb, _EdgeColor3.rgb, segment - 1.0);
                else if (segment < 3.0)
                    return lerp(_EdgeColor3.rgb, _EdgeColor4.rgb, segment - 2.0);
                else if (segment < 4.0)
                    return lerp(_EdgeColor4.rgb, _EdgeColor5.rgb, segment - 3.0);
                else if (segment < 5.0)
                    return lerp(_EdgeColor5.rgb, _EdgeColor6.rgb, segment - 4.0);
                else
                    return lerp(_EdgeColor6.rgb, _EdgeColor1.rgb, segment - 5.0);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));
                float3x3 TBN = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);
                float3 normalWS = normalize(mul(normalTS, TBN));

                float3 viewDir = normalize(GetCameraPositionWS() - i.worldPos);
                float fresnel = pow(1.0 - saturate(dot(viewDir, normalWS)), _FresnelPower);

                float t = _Time.y * _NoiseSpeed;
                float n = noise(i.worldPos * _NoiseScale + t);

                // אם מוגדרת טקסטורת נויז, נוסיף אותה לערך
                if (_UseNoiseMap > 0.5)
                {
                    float2 uvScroll = i.uv + float2(t * 0.1, t * 0.05);
                    float texNoise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uvScroll).r;
                    n = lerp(n, texNoise, 0.8);
                }

                float3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                float3 colorShift = getMultiColor(_Time.y * _ColorShiftSpeed + n);

                float3 outline = colorShift * fresnel * (1 + n) * _OutlineIntensity;
                float3 finalColor = baseColor + outline;

                return float4(finalColor, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
