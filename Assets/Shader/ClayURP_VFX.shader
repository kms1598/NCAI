Shader "Custom/ClayURP_VFX"
{
    Properties
    {
        [HDR] _BaseColor ("Tint (HDR)", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTex ("Sprite / Flipbook", 2D) = "white" {}
        _EmissionStrength ("Emission Strength", Range(0.0, 8.0)) = 1.0

        [Header(Blending)]
        // 출력은 항상 프리멀티플라이드이며 Blend One OneMinusSrcAlpha 하나로 모드를 모두 처리합니다.
        [KeywordEnum(Alpha, Additive, Premultiplied)] _Blend ("Blend Mode", Float) = 1
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Alpha Source)]
        // Luminance: 검은 배경이 알파 대신 밝기로 표현된 텍스처용 (msVFX 계열이 여기 해당)
        [KeywordEnum(Texture, Luminance, Both)] _AlphaSource ("Alpha Source", Float) = 1
        _LumAlphaLow ("Luminance Alpha Low", Range(0.0, 1.0)) = 0.02
        _LumAlphaHigh ("Luminance Alpha High", Range(0.0, 1.0)) = 0.35
        // 밝기를 알파로 뽑으면 색까지 어두워지므로 원래 밝기를 되살립니다.
        _ColorRestore ("Color Restore", Range(0.0, 1.0)) = 1.0

        [Header(Flipbook)]
        // 파티클 시스템의 Texture Sheet Animation을 쓰면 끄십시오 (UV가 이미 계산되어 들어옵니다).
        [Toggle(_FLIPBOOK_ON)] _Flipbook ("Manual Flipbook", Float) = 0
        _FlipbookColumns ("Columns", Float) = 4
        _FlipbookRows ("Rows", Float) = 1
        _FlipbookFPS ("Frames Per Second", Float) = 12
        [Toggle(_FLIPBOOK_BLEND_ON)] _FlipbookBlend ("Blend Between Frames", Float) = 1

        [Header(Clay Lighting)]
        [Toggle(_CLAY_LIGHTING_ON)] _ClayLighting ("Clay Lighting", Float) = 0
        _LightingInfluence ("Lighting Influence", Range(0.0, 1.0)) = 0.45
        _HighlightTint ("Highlight Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _ShadowColor ("Shadow Multiply", Color) = (1.0, 1.0, 1.0, 1.0)
        _PaletteWarmth ("Palette Warmth", Range(0.0, 1.0)) = 0.20
        _ColorLift ("Lit Color Lift", Range(0.0, 0.5)) = 0.10
        _ShadowLevel ("Shadow Level", Range(0.2, 0.9)) = 0.62
        _WrapAmount ("Wrap Amount", Range(0.0, 1.0)) = 0.65
        _WrapContrast ("Wrap Contrast", Range(0.5, 4.0)) = 1.4
        _ShadowSoftness ("Shadow Softness", Range(0.0, 1.0)) = 0.20
        _BandingStrength ("Banding Strength", Range(0.0, 1.0)) = 0.35
        _BandSteps ("Band Steps", Range(2.0, 8.0)) = 3.0

        [Header(Soft Particles)]
        [Toggle(_SOFTPARTICLES_ON)] _SoftParticles ("Soft Particles", Float) = 1
        _SoftParticleFade ("Soft Fade Distance", Range(0.01, 10.0)) = 0.8
        _CameraFadeNear ("Camera Fade Near", Range(0.0, 10.0)) = 0.0
        _CameraFadeFar ("Camera Fade Far", Range(0.0, 10.0)) = 0.5

        [Header(Dissolve)]
        _DissolveTex ("Dissolve Noise", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0.0, 1.0)) = 0.0
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0.001, 0.5)) = 0.08
        [HDR] _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1.0, 0.6, 0.3, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "ForwardVFX"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            // 키워드가 아직 지정되지 않은 머티리얼은 목록의 첫 항목으로 컴파일되므로
            // 기본값(Additive / Luminance)을 앞에 둡니다.
            #pragma shader_feature_local_fragment _BLEND_ADDITIVE _BLEND_ALPHA _BLEND_PREMULTIPLIED
            #pragma shader_feature_local_fragment _ALPHASOURCE_LUMINANCE _ALPHASOURCE_TEXTURE _ALPHASOURCE_BOTH
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SOFTPARTICLES_ON
            #pragma shader_feature_local_fragment _CLAY_LIGHTING_ON
            #pragma shader_feature_local _FLIPBOOK_ON
            #pragma shader_feature_local _FLIPBOOK_BLEND_ON

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DissolveTex);
            SAMPLER(sampler_DissolveTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _HighlightTint;
                float4 _ShadowColor;
                float4 _DissolveEdgeColor;
                float4 _MainTex_ST;
                float4 _DissolveTex_ST;
                float _EmissionStrength;
                float _Blend;
                float _ZWrite;
                float _Cull;
                float _AlphaClip;
                float _Cutoff;
                float _AlphaSource;
                float _LumAlphaLow;
                float _LumAlphaHigh;
                float _ColorRestore;
                float _Flipbook;
                float _FlipbookColumns;
                float _FlipbookRows;
                float _FlipbookFPS;
                float _FlipbookBlend;
                float _ClayLighting;
                float _LightingInfluence;
                float _PaletteWarmth;
                float _ColorLift;
                float _ShadowLevel;
                float _WrapAmount;
                float _WrapContrast;
                float _ShadowSoftness;
                float _BandingStrength;
                float _BandSteps;
                float _SoftParticles;
                float _SoftParticleFade;
                float _CameraFadeNear;
                float _CameraFadeFar;
                float _DissolveAmount;
                float _DissolveEdgeWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float fogFactor : TEXCOORD5;
                float3 vertexLighting : TEXCOORD6;
            };

            float Luma(float3 c)
            {
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            float2 FlipbookUV(float2 uv, float frame, float2 grid)
            {
                float2 cell = 1.0 / grid;
                float index = fmod(floor(frame), grid.x * grid.y);
                float col = fmod(index, grid.x);
                float row = floor(index / grid.x);
                // 아틀라스는 좌상단이 첫 프레임이지만 UV는 좌하단이 원점입니다.
                float2 origin = float2(col * cell.x, 1.0 - cell.y - row * cell.y);
                return uv * cell + origin;
            }

            float4 SampleSprite(float2 uv)
            {
                #if defined(_FLIPBOOK_ON)
                float2 grid = float2(max(_FlipbookColumns, 1.0), max(_FlipbookRows, 1.0));
                float frame = _Time.y * _FlipbookFPS;
                float4 current = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, FlipbookUV(uv, frame, grid));

                #if defined(_FLIPBOOK_BLEND_ON)
                float4 next = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, FlipbookUV(uv, frame + 1.0, grid));
                return lerp(current, next, frac(frame));
                #else
                return current;
                #endif
                #else
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                #endif
            }

            float3 ScaleLumaPreserveChroma(float3 c, float lumaScale, float addLuma)
            {
                float l = max(Luma(c), 1e-4);
                float3 chroma = c / l;
                float newL = saturate(l * lumaScale + addLuma);
                return saturate(chroma * newL);
            }

            float3 BlendLight(float3 normalWS, float3 albedo, float3 lightDir, float3 lightColor, float atten)
            {
                float ndl = saturate(dot(normalWS, lightDir));
                float wrap = saturate((ndl + _WrapAmount) / (1.0 + _WrapAmount));
                float wrapDiffuse = pow(wrap, _WrapContrast);
                float smoothDiffuse = smoothstep(_ShadowSoftness, 1.0, wrapDiffuse);
                float steps = max(2.0, _BandSteps);
                float stepped = floor(smoothDiffuse * (steps - 1.0) + 0.5) / (steps - 1.0);
                float stylizedDiffuse = lerp(smoothDiffuse, stepped, _BandingStrength);

                float warmth = saturate(_PaletteWarmth);
                float3 litCol = ScaleLumaPreserveChroma(albedo, 1.15, _ColorLift);
                litCol *= lerp(float3(1.0, 1.0, 1.0), float3(1.03, 0.99, 0.96), warmth);
                litCol = saturate(litCol * _HighlightTint.rgb);

                float3 shCol = ScaleLumaPreserveChroma(albedo, _ShadowLevel, 0.0);
                shCol = saturate(shCol * _ShadowColor.rgb);

                return lerp(shCol, litCol, stylizedDiffuse) * lightColor * atten;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.screenPos = ComputeScreenPos(posInputs.positionCS);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                OUT.vertexLighting = VertexLighting(posInputs.positionWS, normInputs.normalWS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 texColor = SampleSprite(IN.uv);
                float3 texRGB = texColor.rgb;

                float luma = Luma(texRGB);
                float lumaAlpha = smoothstep(_LumAlphaLow, max(_LumAlphaHigh, _LumAlphaLow + 1e-4), luma);

                #if defined(_ALPHASOURCE_LUMINANCE)
                float texAlpha = lumaAlpha;
                #elif defined(_ALPHASOURCE_BOTH)
                float texAlpha = texColor.a * lumaAlpha;
                #else
                float texAlpha = texColor.a;
                #endif

                #if defined(_ALPHASOURCE_LUMINANCE) || defined(_ALPHASOURCE_BOTH)
                // 밝기를 알파로 옮겼으니 색은 프리멀티플라이를 되돌려야 가장자리가 검게 죽지 않습니다.
                float3 unpremultiplied = saturate(texRGB / max(lumaAlpha, 1e-3));
                texRGB = lerp(texRGB, unpremultiplied, _ColorRestore);
                #endif

                // 파티클 Start Color / Color over Lifetime은 버텍스 컬러로 들어옵니다.
                float3 albedo = texRGB * IN.color.rgb * _BaseColor.rgb;
                float alpha = saturate(texAlpha * IN.color.a * _BaseColor.a);

                float2 dissolveUV = IN.uv * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
                float noise = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
                float edgeWidth = max(_DissolveEdgeWidth, 1e-4);
                // Amount가 0일 때 잠식이 시작되지 않도록 임계값을 뒤로 밀어 둡니다.
                float threshold = _DissolveAmount * (1.0 + edgeWidth) - edgeWidth;
                float dissolveMask = saturate((noise - threshold) / edgeWidth);
                alpha *= dissolveMask;

                #if defined(_SOFTPARTICLES_ON)
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 1e-4);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float particleEyeDepth = IN.screenPos.w;
                alpha *= saturate((sceneEyeDepth - particleEyeDepth) / max(_SoftParticleFade, 1e-4));
                #endif

                // 카메라에 너무 가까워지면 화면을 덮지 않도록 서서히 지웁니다.
                if (_CameraFadeNear < _CameraFadeFar)
                {
                    float viewDist = length(GetCameraPositionWS() - IN.positionWS);
                    alpha *= saturate((viewDist - _CameraFadeNear) / max(_CameraFadeFar - _CameraFadeNear, 1e-4));
                }

                #if defined(_ALPHATEST_ON)
                clip(alpha - _Cutoff);
                #endif

                float3 color = albedo;

                #if defined(_CLAY_LIGHTING_ON)
                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 normalWS = NormalizeNormalPerPixel(IN.normalWS);

                // Forward+ 라이트 루프 매크로가 inputData를 직접 참조합니다.
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);

                Light mainLight = GetMainLight();
                float mainAtten = mainLight.distanceAttenuation * MainLightRealtimeShadow(inputData.shadowCoord);
                float3 lit = BlendLight(normalWS, albedo, mainLight.direction, mainLight.color, mainAtten);

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightsCount = GetAdditionalLightsCount();
                half4 shadowMask = half4(1, 1, 1, 1);
                LIGHT_LOOP_BEGIN(lightsCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, IN.positionWS, shadowMask);
                    float additionalAtten = additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
                    lit += BlendLight(normalWS, albedo, additionalLight.direction, additionalLight.color, additionalAtten);
                LIGHT_LOOP_END
                #endif

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                lit += albedo * IN.vertexLighting;
                #endif

                color = lerp(albedo, lit, saturate(_LightingInfluence));
                #endif

                float edgeGlow = saturate(1.0 - dissolveMask) * step(1e-4, _DissolveAmount);
                color += _DissolveEdgeColor.rgb * edgeGlow;
                color *= _EmissionStrength;

                #if defined(_BLEND_ADDITIVE)
                // 가산 합성은 배경을 가리지 않으므로 안개 색을 더하지 않고 검정으로 수렴시킵니다.
                color = MixFogColor(color, float3(0.0, 0.0, 0.0), IN.fogFactor);
                return float4(color * alpha, 0.0);
                #elif defined(_BLEND_PREMULTIPLIED)
                color = MixFog(color, IN.fogFactor);
                return float4(color, alpha);
                #else
                color = MixFog(color, IN.fogFactor);
                return float4(color * alpha, alpha);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
