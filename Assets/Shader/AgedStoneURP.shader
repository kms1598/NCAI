Shader "Custom/AgedStoneURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.62, 0.62, 0.60, 1.0)
        _AgedTint ("Aged Tint", Color) = (0.51, 0.50, 0.48, 1.0)
        _EdgeColor ("Edge Wear Color", Color) = (0.73, 0.72, 0.69, 1.0)
        _DirtColor ("Dirt Color", Color) = (0.20, 0.19, 0.16, 1.0)
        _RustColor ("Rust/Patina Color", Color) = (0.33, 0.40, 0.29, 1.0)

        _MainTex ("Albedo Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0.0, 2.0)) = 1.2
        _GrungeTex ("Grunge Noise", 2D) = "gray" {}
        _GrungeScale ("Grunge Scale", Range(0.5, 20.0)) = 3.7

        _WearAmount ("Wear Amount", Range(0.0, 1.0)) = 0.32
        _WearContrast ("Wear Contrast", Range(0.5, 4.0)) = 1.5
        _DirtAmount ("Dirt Amount", Range(0.0, 1.0)) = 0.46
        _DirtContrast ("Dirt Contrast", Range(0.5, 4.0)) = 1.8
        _RustAmount ("Rust/Patina Amount", Range(0.0, 1.0)) = 0.27

        _Roughness ("Roughness", Range(0.0, 1.0)) = 0.9
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

        UsePass "Custom/AgedObjectURP/ForwardLit"
    }

    FallBack Off
}
