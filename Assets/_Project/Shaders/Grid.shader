Shader "Project/Grid"
{
    Properties
    {
        [Header(Ground)]
        _BaseColor ("Base Color", Color) = (0.06, 0.06, 0.07, 1)
        _CellSize ("Cell Size (world units)", Float) = 1

        [Header(Lines)]
        [Toggle] _ShowLines ("Show Lines", Float) = 1
        _LineColor ("Line Color", Color) = (0.12, 0.12, 0.14, 1)
        _LineWidth ("Line Width (world units)", Range(0.001, 0.2)) = 0.02

        [Header(Major Lines)]
        [Toggle] _ShowMajor ("Show Major Lines", Float) = 1
        _MajorEvery ("Major Line Every N Cells", Float) = 10
        _MajorBoost ("Major Line Brightness", Range(1, 4)) = 1.8

        [Header(Dots)]
        [Toggle] _ShowDots ("Show Intersection Dots", Float) = 1
        _DotColor ("Dot Color", Color) = (0.2, 0.2, 0.23, 1)
        _DotRadius ("Dot Radius (world units)", Range(0.005, 0.3)) = 0.05

        [Header(Dirt Noise)]
        [Toggle] _ShowNoise ("Show Dirt Noise", Float) = 0
        _DirtColor ("Dirt Color", Color) = (0.03, 0.025, 0.02, 1)
        _NoiseScale ("Noise Scale (features per unit)", Range(0.02, 4)) = 0.35
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.6

        [Header(Surface)]
        [Toggle(_LIT)] _Lit ("Lit (off = flat unlit)", Float) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Grid"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _LIT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _CellSize;
                float _ShowLines;
                half4 _LineColor;
                float _LineWidth;
                float _ShowMajor;
                float _MajorEvery;
                float _MajorBoost;
                float _ShowDots;
                half4 _DotColor;
                float _DotRadius;
                float _ShowNoise;
                half4 _DirtColor;
                float _NoiseScale;
                float _NoiseStrength;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            // 1 on a grid line, 0 between, antialiased by screen-space derivative.
            float GridLine(float2 coord, float cell, float width)
            {
                float2 c = coord / cell;
                float2 w = fwidth(c);
                float2 halfW = max(width / cell * 0.5, w * 0.5);
                float2 d = abs(frac(c + 0.5) - 0.5);
                float2 l = 1 - smoothstep(halfW - w * 0.5, halfW + w * 0.5, d);
                // fade out when lines are sub-pixel to avoid moire at distance
                l *= saturate(1 - (w - 0.25) * 2);
                return max(l.x, l.y);
            }

            // 1 at each grid intersection, antialiased, fades when sub-pixel.
            float GridDots(float2 coord, float cell, float radius)
            {
                float2 c = coord / cell;
                float aa = max(length(fwidth(c)), 1e-5);
                float r = radius / cell;
                float d = length(frac(c + 0.5) - 0.5);
                float dotMask = 1 - smoothstep(r - aa, r + aa, d);
                return dotMask * saturate(r / aa) * saturate(1 - (aa - 0.25) * 2);
            }

            // World-space value noise: no UVs, no tiling, so it is seamless by construction.
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3 - 2 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0;
                float amp = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += ValueNoise(p) * amp;
                    p = p * 2.03 + 17.0;
                    amp *= 0.5;
                }
                return v;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 xz = IN.positionWS.xz;
                half3 col = _BaseColor.rgb;

                if (_ShowLines > 0.5)
                    col = lerp(col, _LineColor.rgb, GridLine(xz, _CellSize, _LineWidth));

                if (_ShowMajor > 0.5)
                {
                    float major = GridLine(xz, _CellSize * max(_MajorEvery, 1), _LineWidth * 1.5);
                    col = lerp(col, saturate(_LineColor.rgb * _MajorBoost), major);
                }

                if (_ShowDots > 0.5)
                    col = lerp(col, _DotColor.rgb, GridDots(xz, _CellSize, _DotRadius));

                if (_ShowNoise > 0.5)
                {
                    float n = Fbm(xz * _NoiseScale);
                    col = lerp(col, _DirtColor.rgb, smoothstep(0.35, 0.75, n) * _NoiseStrength);
                }

            #if defined(_LIT)
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.positionCS = IN.positionCS;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surf = (SurfaceData)0;
                surf.albedo = col;
                surf.metallic = _Metallic;
                surf.smoothness = _Smoothness;
                surf.occlusion = 1;
                surf.alpha = 1;
                return UniversalFragmentPBR(inputData, surf);
            #else
                return half4(col, 1);
            #endif
            }
            ENDHLSL
        }
    }

    // Supplies ShadowCaster / DepthOnly passes.
    Fallback "Universal Render Pipeline/Unlit"
}
