Shader "Custom/AgedObjectURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.72, 0.68, 0.62, 1.0)
        _AgedTint ("Aged Tint", Color) = (0.58, 0.54, 0.49, 1.0)
        _EdgeColor ("Edge Wear Color", Color) = (0.86, 0.82, 0.74, 1.0)
        _DirtColor ("Dirt Color", Color) = (0.22, 0.20, 0.17, 1.0)
        _RustColor ("Rust/Patina Color", Color) = (0.45, 0.28, 0.16, 1.0)

        _MainTex ("Albedo Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0.0, 2.0)) = 1.0
        _GrungeTex ("Grunge Noise", 2D) = "gray" {}
        _GrungeScale ("Grunge Scale", Range(0.5, 20.0)) = 4.5

        _WearAmount ("Wear Amount", Range(0.0, 1.0)) = 0.45
        _WearContrast ("Wear Contrast", Range(0.5, 4.0)) = 1.8
        _DirtAmount ("Dirt Amount", Range(0.0, 1.0)) = 0.35
        _DirtContrast ("Dirt Contrast", Range(0.5, 4.0)) = 1.5
        _RustAmount ("Rust/Patina Amount", Range(0.0, 1.0)) = 0.25

        _Roughness ("Roughness", Range(0.0, 1.0)) = 0.82
        _SpecularStrength ("Specular Strength", Range(0.0, 1.0)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_GrungeTex);
            SAMPLER(sampler_GrungeTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _AgedTint;
                float4 _EdgeColor;
                float4 _DirtColor;
                float4 _RustColor;
                float4 _MainTex_ST;
                float4 _GrungeTex_ST;
                float _NormalStrength;
                float _GrungeScale;
                float _WearAmount;
                float _WearContrast;
                float _DirtAmount;
                float _DirtContrast;
                float _RustAmount;
                float _Roughness;
                float _SpecularStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.tangentWS = float4(normInputs.tangentWS, IN.tangentOS.w);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            float3 BlendLight(float3 normalWS, float3 viewDirWS, float3 albedo, float3 lightDir, float3 lightColor, float atten)
            {
                float ndl = saturate(dot(normalWS, lightDir));
                float3 diffuse = albedo * lightColor * ndl;

                float3 halfDir = SafeNormalize(lightDir + viewDirWS);
                float ndh = saturate(dot(normalWS, halfDir));
                float specPow = lerp(12.0, 96.0, 1.0 - _Roughness);
                float spec = pow(ndh, specPow) * _SpecularStrength;
                float3 specular = spec * lightColor;

                return (diffuse + specular) * atten;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));

                float2 grungeUV = (IN.positionWS.xz * _GrungeScale) * _GrungeTex_ST.xy + _GrungeTex_ST.zw;
                float grungeA = SAMPLE_TEXTURE2D(_GrungeTex, sampler_GrungeTex, grungeUV).r;
                float grungeB = SAMPLE_TEXTURE2D(_GrungeTex, sampler_GrungeTex, IN.uv * 2.3).g;
                float grunge = saturate(grungeA * 0.65 + grungeB * 0.35);

                float3 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;
                float3 baseAlbedo = albedoTex * _BaseColor.rgb;
                float3 agedAlbedo = lerp(baseAlbedo, baseAlbedo * _AgedTint.rgb, 0.65);

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv), _NormalStrength);
                float3 bitangentWS = cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w;
                float3x3 tbn = float3x3(IN.tangentWS.xyz, bitangentWS, IN.normalWS);
                float3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tbn));

                float silhouette = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 2.0);
                float wearRaw = saturate(lerp(grunge, silhouette, 0.45) * (1.0 + _WearAmount));
                float wearMask = pow(wearRaw, _WearContrast) * _WearAmount;

                float cavity = 1.0 - saturate(normalWS.y * 0.5 + 0.5);
                float dirtRaw = saturate(cavity * 0.7 + grunge * 0.6);
                float dirtMask = pow(dirtRaw, _DirtContrast) * _DirtAmount;

                float rustRaw = saturate((grunge - 0.45) * 2.0);
                float rustMask = rustRaw * _RustAmount * (1.0 - wearMask * 0.85);

                float3 agedColor = agedAlbedo;
                agedColor = lerp(agedColor, _EdgeColor.rgb, wearMask);
                agedColor = lerp(agedColor, _DirtColor.rgb, dirtMask);
                agedColor = lerp(agedColor, _RustColor.rgb, rustMask);

                Light mainLight = GetMainLight();
                float mainShadowAtten = MainLightRealtimeShadow(TransformWorldToShadowCoord(IN.positionWS));
                float mainAtten = mainLight.distanceAttenuation * mainShadowAtten;
                float3 color = BlendLight(normalWS, viewDirWS, agedColor, mainLight.direction, mainLight.color, mainAtten);

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightsCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightsCount; i++)
                {
                    Light additionalLight = GetAdditionalLight(i, IN.positionWS);
                    color += BlendLight(
                        normalWS,
                        viewDirWS,
                        agedColor,
                        additionalLight.direction,
                        additionalLight.color,
                        additionalLight.distanceAttenuation * additionalLight.shadowAttenuation
                    );
                }
                #endif

                color = MixFog(color, IN.fogFactor);
                return float4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
