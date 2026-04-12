Shader "Custom/SlideWater_URP"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _NormalA("Normal A", 2D) = "bump" {}
        _NormalB("Normal B", 2D) = "bump" {}
        _FoamTex("Foam Texture", 2D) = "white" {}

        [MainColor] _WaterColor("Water Color", Color) = (0.05, 0.22, 0.45, 0.72)
        _ShallowColor("Shallow Color", Color) = (0.10, 0.35, 0.60, 0.50)
        _FoamColor("Foam Color", Color) = (0.82, 0.90, 1.00, 1.00)

        _NormalStrength("Normal Strength", Range(0, 3)) = 1.0
        _FlowSpeed1("Flow Speed 1", Vector) = (0.15, 0.0, 0, 0)
        _FlowSpeed2("Flow Speed 2", Vector) = (-0.08, 0.04, 0, 0)

        _FoamSpeed("Foam Speed", Vector) = (0.1, 0.02, 0, 0)
        _FoamAmount("Foam Amount", Range(0, 2)) = 0.12
        _FoamThreshold("Foam Threshold", Range(0, 1)) = 0.72

        _Smoothness("Smoothness", Range(0, 1)) = 0.82
        _SpecularStrength("Specular Strength", Range(0, 3)) = 0.55

        _FresnelPower("Fresnel Power", Range(0.1, 8)) = 4.5
        _FresnelStrength("Fresnel Strength", Range(0, 3)) = 0.45

        _Alpha("Alpha", Range(0, 1)) = 0.82
        _DepthFade("Depth Fade", Range(0.01, 5)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

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
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 viewDirWS   : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                float  fogCoord    : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_NormalA);
            SAMPLER(sampler_NormalA);

            TEXTURE2D(_NormalB);
            SAMPLER(sampler_NormalB);

            TEXTURE2D(_FoamTex);
            SAMPLER(sampler_FoamTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NormalA_ST;
                float4 _NormalB_ST;
                float4 _FoamTex_ST;

                half4 _WaterColor;
                half4 _ShallowColor;
                half4 _FoamColor;

                half _NormalStrength;
                float4 _FlowSpeed1;
                float4 _FlowSpeed2;
                float4 _FoamSpeed;

                half _FoamAmount;
                half _FoamThreshold;

                half _Smoothness;
                half _SpecularStrength;

                half _FresnelPower;
                half _FresnelStrength;

                half _Alpha;
                half _DepthFade;
            CBUFFER_END

            float3 BlendWaterNormals(float3 n1, float3 n2)
            {
                n1 = normalize(n1);
                n2 = normalize(n2);
                return normalize(float3(n1.xy + n2.xy, n1.z * n2.z));
            }

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(v.normalOS, v.tangentOS);

                o.positionHCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS = normalize(normalInputs.normalWS);
                o.tangentWS = normalize(normalInputs.tangentWS);
                o.bitangentWS = normalize(normalInputs.bitangentWS);
                o.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                o.shadowCoord = GetShadowCoord(posInputs);
                o.fogCoord = ComputeFogFactor(posInputs.positionCS.z);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float2 uv1 = i.uv + _FlowSpeed1.xy * _Time.y;
                float2 uv2 = i.uv + _FlowSpeed2.xy * _Time.y;
                float2 foamUV = i.uv + _FoamSpeed.xy * _Time.y;

                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

                float3 nA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, uv1));
                float3 nB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalB, sampler_NormalB, uv2));
                float3 tangentNormal = BlendWaterNormals(nA, nB);
                tangentNormal.xy *= _NormalStrength;
                tangentNormal = normalize(tangentNormal);

                float3x3 tbn = float3x3(
                    normalize(i.tangentWS),
                    normalize(i.bitangentWS),
                    normalize(i.normalWS)
                );

                float3 normalWS = normalize(mul(tangentNormal, tbn));
                float3 viewDirWS = normalize(i.viewDirWS);

                float fresnel = pow(saturate(1.0 - dot(normalWS, viewDirWS)), _FresnelPower);
                fresnel *= _FresnelStrength;

                Light mainLight = GetMainLight(i.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, lightDir));

                float3 halfDir = normalize(lightDir + viewDirWS);
                float spec = pow(saturate(dot(normalWS, halfDir)), lerp(8.0, 128.0, _Smoothness));
                float3 specular = spec * _SpecularStrength * mainLight.color * mainLight.shadowAttenuation;

                float foamTex = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV).r;
                float foamMask = smoothstep(_FoamThreshold - 0.06, _FoamThreshold + 0.06, foamTex);
                foamMask *= _FoamAmount;

                float shallowMask = saturate(fresnel * 0.45 + (1.0 - NdotL) * 0.20);

                float3 deepTint = _WaterColor.rgb * 0.75;
                float3 waterCol = lerp(deepTint, _ShallowColor.rgb, shallowMask);
                waterCol *= baseTex.rgb;

                float3 ambient = SampleSH(normalWS);
                float3 litColor = waterCol * (ambient + mainLight.color * NdotL * mainLight.shadowAttenuation);
                litColor += specular;

                // softer highlight, avoids milky look
                litColor += fresnel * 0.08;

                // foam only where texture says
                litColor = lerp(litColor, _FoamColor.rgb, saturate(foamMask));

                float alpha = _Alpha * _WaterColor.a;
                alpha += fresnel * 0.05;
                alpha = saturate(alpha);

                litColor = MixFog(litColor, i.fogCoord);

                return float4(litColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}