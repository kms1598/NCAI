Shader "Custom/SpriteGlowURP"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color (HDR)", Color) = (1.0, 0.6, 0.2, 1.0)
        _EmissionStrength ("Emission Strength", Range(0.0, 20.0)) = 3.0
        _OutlineThickness ("Outline Thickness (px)", Range(0.0, 16.0)) = 2.0
        // Outer는 실루엣 바깥, Inner는 실루엣 안쪽에 선을 그립니다.
        [KeywordEnum(Outer, Inner)] _OutlineMode ("Outline Mode", Float) = 0

        [Header(Edge Detection)]
        _AlphaThreshold ("Alpha Threshold", Range(0.0, 1.0)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.1

        [Header(Fill)]
        [Toggle(_OUTLINEONLY_ON)] _OutlineOnly ("Outline Only (본체 숨김)", Float) = 0
        _FillColor ("Fill Tint", Color) = (1.0, 1.0, 1.0, 1.0)

        [Header(Options)]
        [KeywordEnum(Alpha, Additive)] _Blend ("Blend Mode", Float) = 0
        // Sprite Renderer가 스프라이트 텍스처를 직접 넣어 주므로 인스펙터에서는 숨깁니다.
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // 출력은 항상 프리멀티플라이드라 이 블렌드 하나로 알파·가산을 모두 처리합니다.
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float4 _FillColor;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _EmissionStrength;
            float _OutlineThickness;
            float _OutlineMode;
            float _AlphaThreshold;
            float _EdgeSoftness;
            float _OutlineOnly;
            float _Blend;
        CBUFFER_END

        // 22.5도 간격 16방향. 둥근 실루엣에서도 선 굵기가 고르게 나옵니다.
        static const float2 kDirections[16] =
        {
            float2( 1.0000,  0.0000), float2( 0.9239,  0.3827),
            float2( 0.7071,  0.7071), float2( 0.3827,  0.9239),
            float2( 0.0000,  1.0000), float2(-0.3827,  0.9239),
            float2(-0.7071,  0.7071), float2(-0.9239,  0.3827),
            float2(-1.0000,  0.0000), float2(-0.9239, -0.3827),
            float2(-0.7071, -0.7071), float2(-0.3827, -0.9239),
            float2( 0.0000, -1.0000), float2( 0.3827, -0.9239),
            float2( 0.7071, -0.7071), float2( 0.9239, -0.3827)
        };

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 color : TEXCOORD0;
            float2 uv : TEXCOORD1;
        };

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            // Sprite Renderer의 Color는 버텍스 컬러로 들어옵니다.
            OUT.color = IN.color;
            return OUT;
        }

        /// 알파를 임계값 기준으로 정리해 0~1 실루엣 값으로 만듭니다.
        float SampleShape(float2 uv)
        {
            float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            return saturate((alpha - _AlphaThreshold) / max(_EdgeSoftness, 1e-4));
        }

        float4 frag(Varyings IN) : SV_Target
        {
            float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            float shape = SampleShape(IN.uv);

            float dilated = shape;
            float eroded = shape;
            float2 radius = _OutlineThickness * _MainTex_TexelSize.xy;

            [unroll]
            for (int i = 0; i < 16; i++)
            {
                // 굵은 선에서 안쪽이 비지 않도록 방향마다 반지름을 두 번 훑습니다.
                float2 offset = kDirections[i] * radius;
                float halfTap = SampleShape(IN.uv + offset * 0.5);
                float fullTap = SampleShape(IN.uv + offset);

                dilated = max(dilated, max(halfTap, fullTap));
                eroded = min(eroded, min(halfTap, fullTap));
            }

            #if defined(_OUTLINEMODE_INNER)
            float outline = saturate(shape - eroded);
            #else
            float outline = saturate(dilated - shape);
            #endif

            float3 fillRGB = tex.rgb * _FillColor.rgb * IN.color.rgb;
            float fillAlpha = tex.a * _FillColor.a * IN.color.a;

            #if defined(_OUTLINEONLY_ON)
            fillAlpha = 0.0;
            #endif

            float3 glowRGB = _OutlineColor.rgb * _EmissionStrength;
            float glowAlpha = outline * _OutlineColor.a * IN.color.a;

            // 외곽선을 본체 위에 올리는 소스 오버 합성입니다.
            float3 premultiplied = glowRGB * glowAlpha + fillRGB * fillAlpha * (1.0 - glowAlpha);
            float alpha = glowAlpha + fillAlpha * (1.0 - glowAlpha);

            #if defined(_BLEND_ADDITIVE)
            return float4(premultiplied, 0.0);
            #else
            return float4(premultiplied, alpha);
            #endif
        }
        ENDHLSL

        Pass
        {
            Name "SpriteOutlineGlowForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local_fragment _OUTLINEMODE_OUTER _OUTLINEMODE_INNER
            #pragma shader_feature_local_fragment _OUTLINEONLY_ON
            #pragma shader_feature_local_fragment _BLEND_ALPHA _BLEND_ADDITIVE
            ENDHLSL
        }

        // 2D Renderer를 쓰는 경우를 위한 패스입니다. 둘 중 하나만 실행됩니다.
        Pass
        {
            Name "SpriteOutlineGlow2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local_fragment _OUTLINEMODE_OUTER _OUTLINEMODE_INNER
            #pragma shader_feature_local_fragment _OUTLINEONLY_ON
            #pragma shader_feature_local_fragment _BLEND_ALPHA _BLEND_ADDITIVE
            ENDHLSL
        }
    }

    FallBack Off
}
