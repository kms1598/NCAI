Shader "Custom/AgedWoodURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.60, 0.47, 0.33, 1.0)
        _AgedTint ("Aged Tint", Color) = (0.48, 0.39, 0.30, 1.0)
        _EdgeColor ("Edge Wear Color", Color) = (0.75, 0.62, 0.45, 1.0)
        _DirtColor ("Dirt Color", Color) = (0.19, 0.16, 0.13, 1.0)
        _RustColor ("Rust/Patina Color", Color) = (0.34, 0.24, 0.16, 1.0)

        _MainTex ("Albedo Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0.0, 2.0)) = 1.05
        _GrungeTex ("Grunge Noise", 2D) = "gray" {}
        _GrungeScale ("Grunge Scale", Range(0.5, 20.0)) = 5.0

        _WearAmount ("Wear Amount", Range(0.0, 1.0)) = 0.38
        _WearContrast ("Wear Contrast", Range(0.5, 4.0)) = 1.6
        _DirtAmount ("Dirt Amount", Range(0.0, 1.0)) = 0.42
        _DirtContrast ("Dirt Contrast", Range(0.5, 4.0)) = 1.7
        _RustAmount ("Rust/Patina Amount", Range(0.0, 1.0)) = 0.18

        _Roughness ("Roughness", Range(0.0, 1.0)) = 0.86
        _SpecularStrength ("Specular Strength", Range(0.0, 1.0)) = 0.06
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        UsePass "Custom/AgedObjectURP/ForwardLit"
    }

    FallBack Off
}
