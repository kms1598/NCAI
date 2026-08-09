Shader "Custom/AgedMetalURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.60, 0.61, 0.62, 1.0)
        _AgedTint ("Aged Tint", Color) = (0.49, 0.50, 0.52, 1.0)
        _EdgeColor ("Edge Wear Color", Color) = (0.80, 0.82, 0.84, 1.0)
        _DirtColor ("Dirt Color", Color) = (0.20, 0.19, 0.17, 1.0)
        _RustColor ("Rust/Patina Color", Color) = (0.49, 0.28, 0.14, 1.0)

        _MainTex ("Albedo Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0.0, 2.0)) = 0.95
        _GrungeTex ("Grunge Noise", 2D) = "gray" {}
        _GrungeScale ("Grunge Scale", Range(0.5, 20.0)) = 4.2

        _WearAmount ("Wear Amount", Range(0.0, 1.0)) = 0.56
        _WearContrast ("Wear Contrast", Range(0.5, 4.0)) = 2.0
        _DirtAmount ("Dirt Amount", Range(0.0, 1.0)) = 0.30
        _DirtContrast ("Dirt Contrast", Range(0.5, 4.0)) = 1.35
        _RustAmount ("Rust/Patina Amount", Range(0.0, 1.0)) = 0.44

        _Roughness ("Roughness", Range(0.0, 1.0)) = 0.62
        _SpecularStrength ("Specular Strength", Range(0.0, 1.0)) = 0.22
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
