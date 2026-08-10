Shader "Custom/ClayURP_Wall"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0.0, 2.0)) = 0.65

        // Keep white. These only gently multiply; lighting preserves albedo chroma.
        _HighlightTint ("Highlight Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _ShadowColor ("Shadow Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimColor ("Rim Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _PaletteWarmth ("Palette Warmth", Range(0.0, 1.0)) = 0.25
        _ColorLift ("Lit Color Lift", Range(0.0, 0.5)) = 0.12
        _ShadowLevel ("Shadow Level", Range(0.2, 0.9)) = 0.55

        _Roughness ("Roughness", Range(0.0, 1.0)) = 0.93
        _SpecularStrength ("Specular Strength", Range(0.0, 1.0)) = 0.03

        _RimPower ("Rim Power", Range(0.1, 8.0)) = 3.2
        _RimStrength ("Rim Strength", Range(0.0, 1.0)) = 0.10
        _WrapAmount ("Wrap Amount", Range(0.0, 1.0)) = 0.45
        _WrapContrast ("Wrap Contrast", Range(0.5, 4.0)) = 1.8
        _ShadowSoftness ("Shadow Softness", Range(0.0, 1.0)) = 0.28
        _BandingStrength ("Banding Strength", Range(0.0, 1.0)) = 0.12
        _BandSteps ("Band Steps", Range(2.0, 8.0)) = 4.0
        _SheenStrength ("Clay Sheen Strength", Range(0.0, 1.0)) = 0.04
        _SheenWidth ("Clay Sheen Width", Range(0.1, 1.0)) = 0.40

        _ClayVariationTex ("Clay Variation Noise", 2D) = "gray" {}
        _ClayVariationScale ("Variation Scale", Range(0.5, 20.0)) = 6.0
        _ClayVariationStrength ("Variation Strength", Range(0.0, 0.3)) = 0.05
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_ClayVariationTex);
            SAMPLER(sampler_ClayVariationTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _HighlightTint;
                float4 _ShadowColor;
                float4 _RimColor;
                float4 _MainTex_ST;
                float4 _ClayVariationTex_ST;
                float _NormalStrength;
                float _PaletteWarmth;
                float _ColorLift;
                float _ShadowLevel;
                float _Roughness;
                float _SpecularStrength;
                float _RimPower;
                float _RimStrength;
                float _WrapAmount;
                float _WrapContrast;
                float _ShadowSoftness;
                float _BandingStrength;
                float _BandSteps;
                float _SheenStrength;
                float _SheenWidth;
                float _ClayVariationScale;
                float _ClayVariationStrength;
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
                float3 vertexLighting : TEXCOORD5;
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
                OUT.vertexLighting = VertexLighting(posInputs.positionWS, normInputs.normalWS);
                return OUT;
            }

            float Luma(float3 c)
            {
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            // Brighten/darken while keeping the albedo hue (fixes murky grey wash on dark velvet).
            float3 ScaleLumaPreserveChroma(float3 c, float lumaScale, float addLuma)
            {
                float l = max(Luma(c), 1e-4);
                float3 chroma = c / l;
                float newL = saturate(l * lumaScale + addLuma);
                return saturate(chroma * newL);
            }

            float3 BlendLight(float3 normalWS, float3 viewDirWS, float3 albedo, float3 lightDir, float3 lightColor, float atten)
            {
                float ndl = saturate(dot(normalWS, lightDir));
                float wrap = saturate((ndl + _WrapAmount) / (1.0 + _WrapAmount));
                float wrapDiffuse = pow(wrap, _WrapContrast);
                float smoothDiffuse = smoothstep(_ShadowSoftness, 1.0, wrapDiffuse);
                float steps = max(2.0, _BandSteps);
                float stepped = floor(smoothDiffuse * (steps - 1.0) + 0.5) / (steps - 1.0);
                float stylizedDiffuse = lerp(smoothDiffuse, stepped, _BandingStrength);

                float warmth = saturate(_PaletteWarmth);
                float3 litCol = ScaleLumaPreserveChroma(albedo, 1.25, _ColorLift);
                litCol *= lerp(float3(1.0, 1.0, 1.0), float3(1.03, 0.99, 0.97), warmth * 0.35);
                litCol = saturate(litCol * _HighlightTint.rgb);

                float3 shCol = ScaleLumaPreserveChroma(albedo, _ShadowLevel, 0.0);
                shCol *= lerp(float3(1.0, 1.0, 1.0), float3(0.98, 0.96, 1.01), warmth * 0.25);
                shCol = saturate(shCol * _ShadowColor.rgb);

                float3 diffuse = lerp(shCol, litCol, stylizedDiffuse) * lightColor;

                float3 halfDir = SafeNormalize(lightDir + viewDirWS);
                float ndh = saturate(dot(normalWS, halfDir));
                float rough = saturate(_Roughness);
                float specPow = lerp(14.0, 96.0, 1.0 - rough);
                float specIntensity = _SpecularStrength * pow(max(1.0 - rough, 0.0), 1.75);
                float3 specular = pow(ndh, specPow) * specIntensity * stylizedDiffuse * lightColor;

                float edgeMask = 1.0 - saturate(dot(normalWS, viewDirWS));
                float sheenExp = lerp(12.0, 4.0, _SheenWidth);
                float3 sheen = pow(edgeMask, sheenExp) * stylizedDiffuse * _SheenStrength * litCol * lightColor;

                return (diffuse + specular + sheen) * atten;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                float2 variationUV = (IN.positionWS.xz * _ClayVariationScale) * _ClayVariationTex_ST.xy + _ClayVariationTex_ST.zw;
                float variation = SAMPLE_TEXTURE2D(_ClayVariationTex, sampler_ClayVariationTex, variationUV).r;
                float3 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;
                float3 albedo = saturate(albedoTex * _BaseColor.rgb + ((variation - 0.5) * 2.0 * _ClayVariationStrength).xxx);

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv), _NormalStrength);
                float3 normalWS;
                if (length(IN.tangentWS.xyz) > 0.0001)
                {
                    float3 tangentWS = normalize(IN.tangentWS.xyz);
                    float3 bitangentWS = normalize(cross(IN.normalWS, tangentWS) * IN.tangentWS.w);
                    float3x3 tbn = float3x3(tangentWS, bitangentWS, normalize(IN.normalWS));
                    normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tbn));
                }
                else
                {
                    normalWS = NormalizeNormalPerPixel(IN.normalWS);
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);

                Light mainLight = GetMainLight();
                float mainAtten = mainLight.distanceAttenuation * MainLightRealtimeShadow(inputData.shadowCoord);
                float3 color = BlendLight(normalWS, viewDirWS, albedo, mainLight.direction, mainLight.color, mainAtten);
                float lightVisibility = dot(mainLight.color, float3(0.299, 0.587, 0.114)) * mainAtten;

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightsCount = GetAdditionalLightsCount();
                half4 shadowMask = half4(1, 1, 1, 1);
                LIGHT_LOOP_BEGIN(lightsCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, IN.positionWS, shadowMask);
                    float additionalAtten = additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
                    color += BlendLight(normalWS, viewDirWS, albedo, additionalLight.direction, additionalLight.color, additionalAtten);
                    lightVisibility += dot(additionalLight.color, float3(0.299, 0.587, 0.114)) * additionalAtten;
                LIGHT_LOOP_END
                #endif

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                color += albedo * IN.vertexLighting;
                lightVisibility += dot(IN.vertexLighting, float3(0.299, 0.587, 0.114));
                #endif

                // Rim keeps albedo hue instead of peach wash.
                float3 rimCol = ScaleLumaPreserveChroma(albedo, 1.4, 0.05) * _RimColor.rgb;
                float rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower) * _RimStrength;
                color += rim * rimCol * saturate(lightVisibility);

                color = MixFog(color, IN.fogFactor);
                return float4(color, _BaseColor.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    FallBack Off
}
