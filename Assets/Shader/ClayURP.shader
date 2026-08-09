Shader "Custom/ClayURP"
{
    Properties
    {
        [Header(Base)]
        _BaseColor ("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)

        // Palette color slots are multipliers. Leave white for auto colors from Base.
        _ShadowColor ("Shadow Color Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimColor ("Rim Color Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _HighlightTint ("Highlight Tint Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _ShadowTint ("Shadow Tint Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _SubsurfaceColor ("Subsurface Color Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _SheenColor ("Clay Sheen Color Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _PaletteWarmth ("Palette Warmth", Range(0.0, 1.0)) = 0.55

        _MainTex ("Albedo Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0.0, 2.0)) = 0.8

        _Roughness ("Roughness", Range(0.0, 1.0)) = 0.88
        _SpecularStrength ("Specular Strength", Range(0.0, 1.0)) = 0.08
        // 1 = fully kill gloss on floors/ceilings, 0 = no extra attenuation
        _HorizontalMatte ("Horizontal Matte", Range(0.0, 1.0)) = 0.85

        _RimPower ("Rim Power", Range(0.1, 8.0)) = 2.8
        _RimStrength ("Rim Strength", Range(0.0, 1.0)) = 0.22
        _WrapAmount ("Wrap Amount", Range(0.0, 1.0)) = 0.45
        _WrapContrast ("Wrap Contrast", Range(0.5, 4.0)) = 1.8
        _ShadowSoftness ("Shadow Softness", Range(0.0, 1.0)) = 0.28
        _BandingStrength ("Banding Strength", Range(0.0, 1.0)) = 0.2
        _BandSteps ("Band Steps", Range(2.0, 8.0)) = 4.0
        _SubsurfaceStrength ("Subsurface Strength", Range(0.0, 1.0)) = 0.22
        _SheenStrength ("Clay Sheen Strength", Range(0.0, 1.0)) = 0.16
        _SheenWidth ("Clay Sheen Width", Range(0.1, 1.0)) = 0.45

        _ClayVariationTex ("Clay Variation Noise", 2D) = "gray" {}
        _ClayVariationScale ("Variation Scale", Range(0.5, 20.0)) = 6.0
        _ClayVariationStrength ("Variation Strength", Range(0.0, 0.3)) = 0.06
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
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
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
                float4 _ShadowColor;
                float4 _RimColor;
                float4 _HighlightTint;
                float4 _ShadowTint;
                float4 _SheenColor;
                float4 _MainTex_ST;
                float4 _ClayVariationTex_ST;
                float _NormalStrength;
                float _Roughness;
                float _SpecularStrength;
                float _HorizontalMatte;
                float _RimPower;
                float _RimStrength;
                float _WrapAmount;
                float _WrapContrast;
                float _ShadowSoftness;
                float _BandingStrength;
                float _BandSteps;
                float4 _SubsurfaceColor;
                float _SubsurfaceStrength;
                float _SheenStrength;
                float _SheenWidth;
                float _ClayVariationScale;
                float _ClayVariationStrength;
                float _PaletteWarmth;
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

            struct ClayPalette
            {
                float3 highlightTint;
                float3 shadowTint;
                float3 shadowColor;
                float3 rimColor;
                float3 subsurfaceColor;
                float3 sheenColor;
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

            float3 AdjustSaturation(float3 c, float satMul)
            {
                float l = Luma(c);
                return saturate(lerp(l.xxx, c, satMul));
            }

            // Keep hue/chroma from base, but assign a fixed target brightness.
            // (Prevents albedo * derivedColor from double-darkening.)
            float3 Chroma(float3 c)
            {
                float l = max(Luma(c), 1e-4);
                return c / l;
            }

            float3 WithLuma(float3 chroma, float targetLuma)
            {
                return saturate(chroma * targetLuma);
            }

            // Build a consistent clay palette from BaseColor * Albedo.
            // Uses chroma only + fixed luminances (similar brightness to the old manual tints).
            ClayPalette BuildClayPalette(float3 base)
            {
                float3 safeBase = max(base, float3(0.04, 0.04, 0.04));
                float warmth = saturate(_PaletteWarmth);

                float3 baseChroma = Chroma(safeBase);
                // Soften very saturated textures so lighting tints stay clay-like.
                baseChroma = AdjustSaturation(baseChroma, 0.85);

                float3 warmChroma = Chroma(float3(1.00, 0.86, 0.75));
                float3 shadowChroma = Chroma(float3(0.76, 0.57, 0.46));
                float3 deepShadowChroma = Chroma(float3(0.55, 0.38, 0.30));

                float3 litChroma = Chroma(lerp(baseChroma, warmChroma, 0.35 * warmth));
                float3 midShadowChroma = Chroma(lerp(baseChroma, shadowChroma, 0.40 * warmth));
                float3 deepChroma = Chroma(lerp(baseChroma, deepShadowChroma, 0.45 * warmth));

                ClayPalette p;
                // Target luminances ≈ original manual palette brightness.
                p.highlightTint = WithLuma(litChroma, 1.05);
                p.shadowTint = WithLuma(midShadowChroma, 0.68);
                p.shadowColor = WithLuma(deepChroma, 0.48);
                p.rimColor = WithLuma(litChroma, 1.10);
                p.subsurfaceColor = WithLuma(Chroma(lerp(baseChroma, warmChroma, 0.55 * warmth)), 0.95);
                p.sheenColor = WithLuma(litChroma, 1.00);

                // Optional artistic multipliers (leave white for pure auto look).
                p.highlightTint *= _HighlightTint.rgb;
                p.shadowTint *= _ShadowTint.rgb;
                p.shadowColor *= _ShadowColor.rgb;
                p.rimColor *= _RimColor.rgb;
                p.subsurfaceColor *= _SubsurfaceColor.rgb;
                p.sheenColor *= _SheenColor.rgb;

                p.highlightTint = saturate(p.highlightTint);
                p.shadowTint = saturate(p.shadowTint);
                p.shadowColor = saturate(p.shadowColor);
                p.rimColor = saturate(p.rimColor);
                p.subsurfaceColor = saturate(p.subsurfaceColor);
                p.sheenColor = saturate(p.sheenColor);
                return p;
            }

            float3 BlendLight(
                float3 normalWS,
                float3 viewDirWS,
                float3 albedo,
                ClayPalette palette,
                float3 lightDir,
                float3 lightColor,
                float atten)
            {
                float ndl = saturate(dot(normalWS, lightDir));
                float wrap = saturate((ndl + _WrapAmount) / (1.0 + _WrapAmount));
                float wrapDiffuse = pow(wrap, _WrapContrast);
                float smoothDiffuse = smoothstep(_ShadowSoftness, 1.0, wrapDiffuse);
                float steps = max(2.0, _BandSteps);
                float stepped = floor(smoothDiffuse * (steps - 1.0) + 0.5) / (steps - 1.0);
                float stylizedDiffuse = lerp(smoothDiffuse, stepped, _BandingStrength);

                // Floors/ceilings face up/down: kill broad glossy bands.
                float upFacing = abs(normalWS.y);
                float horizontalMask = upFacing * upFacing;
                float glossScale = 1.0 - saturate(_HorizontalMatte) * horizontalMask;

                // Soften lit tint on horizontal surfaces so floors don't look oily.
                float3 softHighlight = lerp(palette.highlightTint, AdjustSaturation(palette.highlightTint, 0.35) * 0.92, horizontalMask);
                float3 warmTint = lerp(palette.shadowTint, softHighlight, stylizedDiffuse);
                float diffuseLift = lerp(1.08, 1.02, horizontalMask);
                float3 diffuse = lerp(palette.shadowColor * albedo, albedo * warmTint, stylizedDiffuse) * lightColor * diffuseLift;

                float3 halfDir = SafeNormalize(lightDir + viewDirWS);
                float ndh = saturate(dot(normalWS, halfDir));
                float rough = saturate(_Roughness);
                // High roughness => much weaker, tighter specular (clay/matte).
                float specPow = lerp(12.0, 96.0, 1.0 - rough);
                float specIntensity = _SpecularStrength * pow(max(1.0 - rough, 0.0), 1.75) * glossScale;
                float spec = pow(ndh, specPow) * specIntensity * stylizedDiffuse;
                float3 specular = spec * lightColor;

                // Fake SSS: back-lit areas get a warm soft glow.
                float backScatter = saturate(dot(-lightDir, normalWS));
                float edgeMask = 1.0 - saturate(dot(normalWS, viewDirWS));
                backScatter = pow(backScatter, 1.6) * pow(edgeMask, 0.7) * _SubsurfaceStrength;
                // Almost no SSS on floors.
                backScatter *= lerp(1.0, 0.15, horizontalMask);
                float3 subsurface = backScatter * palette.subsurfaceColor * albedo * lightColor;

                // Tighter sheen, strongly reduced on horizontal surfaces.
                float sheenExp = lerp(12.0, 4.0, _SheenWidth);
                float sheenMask = pow(edgeMask, sheenExp) * stylizedDiffuse;
                float3 sheen = sheenMask * _SheenStrength * glossScale * palette.sheenColor * lightColor;

                return (diffuse + specular + subsurface + sheen) * atten;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));

                float2 variationUV = (IN.positionWS.xz * _ClayVariationScale) * _ClayVariationTex_ST.xy + _ClayVariationTex_ST.zw;
                float variation = SAMPLE_TEXTURE2D(_ClayVariationTex, sampler_ClayVariationTex, variationUV).r;
                float3 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;

                float3 baseAlbedo = albedoTex * _BaseColor.rgb;
                float clayTint = (variation - 0.5) * 2.0 * _ClayVariationStrength;
                float3 albedo = saturate(baseAlbedo + clayTint.xxx);

                // Palette follows BaseColor * Albedo so every material stays consistent.
                ClayPalette palette = BuildClayPalette(albedo);

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv), _NormalStrength);
                float tangentLen = length(IN.tangentWS.xyz);
                float3 normalWS;
                if (tangentLen > 0.0001)
                {
                    float3 tangentWS = normalize(IN.tangentWS.xyz);
                    float3 bitangentWS = normalize(cross(IN.normalWS, tangentWS) * IN.tangentWS.w);
                    float3x3 tbn = float3x3(tangentWS, bitangentWS, normalize(IN.normalWS));
                    normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tbn));
                }
                else
                {
                    // Imported meshes without tangents still receive lighting.
                    normalWS = NormalizeNormalPerPixel(IN.normalWS);
                }

                Light mainLight = GetMainLight();
                float mainShadowAtten = MainLightRealtimeShadow(TransformWorldToShadowCoord(IN.positionWS));
                float mainAtten = mainLight.distanceAttenuation * mainShadowAtten;

                float3 color = BlendLight(normalWS, viewDirWS, albedo, palette, mainLight.direction, mainLight.color, mainAtten);
                float lightVisibility = dot(mainLight.color, float3(0.299, 0.587, 0.114)) * mainAtten;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightsCount = GetAdditionalLightsCount();
                half4 shadowMask = half4(1.0, 1.0, 1.0, 1.0);
                LIGHT_LOOP_BEGIN(lightsCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, IN.positionWS, shadowMask);
                    float additionalAtten = additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
                    color += BlendLight(
                        normalWS,
                        viewDirWS,
                        albedo,
                        palette,
                        additionalLight.direction,
                        additionalLight.color,
                        additionalAtten
                    );
                    lightVisibility += dot(additionalLight.color, float3(0.299, 0.587, 0.114)) * additionalAtten;
                LIGHT_LOOP_END
                #endif

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                color += albedo * IN.vertexLighting;
                lightVisibility += dot(IN.vertexLighting, float3(0.299, 0.587, 0.114));
                #endif

                float rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower) * _RimStrength;
                color += rim * palette.rimColor * saturate(lightVisibility);

                color = MixFog(color, IN.fogFactor);
                return float4(color, _BaseColor.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    FallBack Off
}
