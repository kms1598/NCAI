Shader "Custom/LittleNightmareClayURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.62, 0.58, 0.52, 1.0)
        _ShadowColor ("Shadow Color", Color) = (0.11, 0.12, 0.14, 1.0)
        _HighlightColor ("Highlight Color", Color) = (0.78, 0.72, 0.62, 1.0)
        _GloomColor ("Gloom Tint", Color) = (0.07, 0.08, 0.10, 1.0)

        _MainTex ("Albedo Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0.0, 2.0)) = 1.1

        _GrimeTex ("Grime Noise", 2D) = "gray" {}
        _GrimeScale ("Grime Scale", Range(0.5, 20.0)) = 6.5
        _GrimeStrength ("Grime Strength", Range(0.0, 1.0)) = 0.42

        _WrapAmount ("Wrap Amount", Range(0.0, 1.0)) = 0.26
        _ShadowHardness ("Shadow Hardness", Range(0.5, 4.0)) = 2.2
        _Desaturate ("Desaturate", Range(0.0, 1.0)) = 0.45
        _GloomStrength ("Gloom Strength", Range(0.0, 1.0)) = 0.38
        _EdgeDarken ("Edge Darken", Range(0.0, 1.0)) = 0.30

        _Roughness ("Roughness", Range(0.0, 1.0)) = 0.92
        _SpecularStrength ("Specular Strength", Range(0.0, 1.0)) = 0.05
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
            TEXTURE2D(_GrimeTex);
            SAMPLER(sampler_GrimeTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _HighlightColor;
                float4 _GloomColor;
                float4 _MainTex_ST;
                float4 _GrimeTex_ST;
                float _NormalStrength;
                float _GrimeScale;
                float _GrimeStrength;
                float _WrapAmount;
                float _ShadowHardness;
                float _Desaturate;
                float _GloomStrength;
                float _EdgeDarken;
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

            float3 ShadeOneLight(float3 normalWS, float3 viewDirWS, float3 albedo, float3 lightDir, float3 lightColor, float atten)
            {
                float ndl = saturate(dot(normalWS, lightDir));
                float wrap = saturate((ndl + _WrapAmount) / (1.0 + _WrapAmount));
                float lit = pow(wrap, _ShadowHardness);

                float3 diffuseRamp = lerp(_ShadowColor.rgb, _HighlightColor.rgb, lit);
                float3 diffuse = albedo * diffuseRamp * lightColor;

                float3 halfDir = SafeNormalize(lightDir + viewDirWS);
                float ndh = saturate(dot(normalWS, halfDir));
                float specPow = lerp(8.0, 56.0, 1.0 - _Roughness);
                float spec = pow(ndh, specPow) * _SpecularStrength * lit;

                return (diffuse + spec.xxx * lightColor) * atten;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));

                float3 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;
                float3 albedo = albedoTex * _BaseColor.rgb;

                float2 grimeUV = (IN.positionWS.xz * _GrimeScale) * _GrimeTex_ST.xy + _GrimeTex_ST.zw;
                float grimeA = SAMPLE_TEXTURE2D(_GrimeTex, sampler_GrimeTex, grimeUV).r;
                float grimeB = SAMPLE_TEXTURE2D(_GrimeTex, sampler_GrimeTex, IN.uv * 2.0).g;
                float grime = saturate(grimeA * 0.6 + grimeB * 0.4);
                float grimeMask = pow(grime, 1.3) * _GrimeStrength;
                albedo = lerp(albedo, albedo * 0.65, grimeMask);

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv), _NormalStrength);
                float3 bitangentWS = cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w;
                float3x3 tbn = float3x3(IN.tangentWS.xyz, bitangentWS, IN.normalWS);
                float3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tbn));

                Light mainLight = GetMainLight();
                float mainShadowAtten = MainLightRealtimeShadow(TransformWorldToShadowCoord(IN.positionWS));
                float mainAtten = mainLight.distanceAttenuation * mainShadowAtten;
                float3 color = ShadeOneLight(normalWS, viewDirWS, albedo, mainLight.direction, mainLight.color, mainAtten);

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightsCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightsCount; i++)
                {
                    Light additionalLight = GetAdditionalLight(i, IN.positionWS);
                    color += ShadeOneLight(
                        normalWS,
                        viewDirWS,
                        albedo,
                        additionalLight.direction,
                        additionalLight.color,
                        additionalLight.distanceAttenuation * additionalLight.shadowAttenuation
                    );
                }
                #endif

                // Desaturate for bleak mood.
                float gray = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(color, gray.xxx, _Desaturate);

                // Global gloom tint and edge darkening.
                float edge = 1.0 - saturate(dot(normalWS, viewDirWS));
                color = lerp(color, color * _GloomColor.rgb, _GloomStrength);
                color *= lerp(1.0, 1.0 - edge * 0.6, _EdgeDarken);

                color = MixFog(color, IN.fogFactor);
                return float4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
