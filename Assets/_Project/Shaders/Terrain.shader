Shader "Project/Terrain"
{
    Properties
    {
        [Header(Meadow)]
        _GrassLight ("Grass Light", Color) = (0.47, 0.74, 0.16, 1)
        _GrassDark ("Grass Dark", Color) = (0.22, 0.5, 0.09, 1)
        _UnderGrass ("Under Grass Clumps", Color) = (0.13, 0.36, 0.06, 1)
        _MeadowScale ("Meadow Gradient Scale", Range(0.005, 0.2)) = 0.035

        [Header(Island Beach)]
        _IslandRadius ("Island Radius (m)", Float) = 95
        _SandWidth ("Beach Sand Width (m)", Float) = 9
        _SeaLevel ("Sea Level (m)", Float) = -0.4

        [Header(Village Ground)]
        [NoScaleOffset] _MaskMap ("Baked Mask (G trail, A grass cover)", 2D) = "black" {}
        _MaskRect ("Mask Rect (origin x, origin z, size)", Vector) = (-75, -75, 150, 0)
        _SandColor ("Packed Sand", Color) = (0.82, 0.71, 0.6, 1)
        _DustColor ("Dusty Edge", Color) = (0.62, 0.62, 0.36, 1)
        _RockColor ("Rock", Color) = (0.36, 0.35, 0.37, 1)
        _CrackColor ("Cracks", Color) = (0.2, 0.17, 0.16, 1)
        _CrackStrength ("Crack Strength", Range(0, 1)) = 0
        _EdgeWarp ("Edge Warp (m)", Range(0, 4)) = 1.4
        _RockAmount ("Rock Amount", Range(0, 1)) = 0.75
        _RockInnerClear ("Rock Inner Clearing", Range(0, 1)) = 0.4
        _RockBreakup ("Rock Ring Breakup", Range(0, 1)) = 0.6
        _RockBreakupScale ("Rock Ring Breakup Scale", Range(0.03, 1)) = 0.12
        _SandOverAmount ("Sand Over Stone Amount", Range(0, 1)) = 0.5
        _SandOverScale ("Sand Over Stone Scale", Range(0.05, 3)) = 0.45
        _SandOverSoftness ("Sand Over Stone Softness", Range(0.02, 0.5)) = 0.2

        [Header(Detail)]
        [NoScaleOffset] _DetailGrass ("Grass Detail", 2D) = "grey" {}
        [NoScaleOffset] _DetailGround ("Dust Detail", 2D) = "grey" {}
        [NoScaleOffset] _DetailSand ("Sand Detail", 2D) = "grey" {}
        [NoScaleOffset] _DetailRock ("Rock Detail", 2D) = "grey" {}
        _DetailTile ("Detail Tile Size (m)", Range(1, 20)) = 5
        _DetailStrength ("Detail Strength", Range(0, 2)) = 1
        _RockDetailStrength ("Rock Detail Strength", Range(0, 4)) = 2.5

        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WorldBend.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GrassLight;
                half4 _GrassDark;
                half4 _UnderGrass;
                float _MeadowScale;
                float _IslandRadius;
                float _SandWidth;
                float _SeaLevel;
                float4 _MaskRect;
                half4 _SandColor;
                half4 _DustColor;
                half4 _RockColor;
                half4 _CrackColor;
                float _CrackStrength;
                float _EdgeWarp;
                float _RockAmount;
                float _RockInnerClear;
                float _RockBreakup;
                float _RockBreakupScale;
                float _SandOverAmount;
                float _SandOverScale;
                float _SandOverSoftness;
                float _DetailTile;
                float _DetailStrength;
                float _RockDetailStrength;
                float _Smoothness;
            CBUFFER_END

            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);
            TEXTURE2D(_DetailGrass);
            TEXTURE2D(_DetailGround);
            TEXTURE2D(_DetailSand);
            TEXTURE2D(_DetailRock);
            SAMPLER(sampler_DetailGrass);

            TEXTURE2D(_GroundFootprints);
            SAMPLER(sampler_GroundFootprints);
            float4 _GroundFootprintRect;

            half Detail(TEXTURE2D_PARAM(tex, smp), float2 uv, float strength)
            {
                half d = SAMPLE_TEXTURE2D(tex, smp, uv).r * 2.0;
                return max(0.0, lerp(1.0, d, strength));
            }

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
                float3 flatWS : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.flatWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = WorldBend(OUT.flatWS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
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

            float CrackDistance(float2 p)
            {
                float2 n = floor(p);
                float2 f = frac(p);
                float2 mg = 0, mr = 0;
                float md = 8;
                for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    float2 g = float2(i, j);
                    float2 r = g + Hash2(n + g) - f;
                    float d = dot(r, r);
                    if (d < md) { md = d; mr = r; mg = g; }
                }

                md = 8;
                for (int y = -2; y <= 2; y++)
                for (int x = -2; x <= 2; x++)
                {
                    float2 g = mg + float2(x, y);
                    float2 r = g + Hash2(n + g) - f;
                    float2 diff = r - mr;
                    if (dot(diff, diff) > 1e-5)
                        md = min(md, dot(0.5 * (mr + r), normalize(diff)));
                }
                return md;
            }

            float Cracks(float2 p, float width)
            {
                float d = CrackDistance(p);
                float aa = fwidth(d);
                return 1 - smoothstep(width - aa, width + aa, d);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 xz = IN.flatWS.xz;

                float2 warp = float2(Fbm(xz * 0.22), Fbm(xz * 0.22 + 9.1)) - 0.5;
                float2 uv = (xz + warp * _EdgeWarp - _MaskRect.xy) / _MaskRect.z;
                half4 m = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
                float trail = m.g;
                float cover = m.a;

                float2 footUV = (xz + warp * _EdgeWarp - _GroundFootprintRect.xy) / max(_GroundFootprintRect.z, 0.001);
                half4 foot = SAMPLE_TEXTURE2D(_GroundFootprints, sampler_GroundFootprints, footUV);
                float village = foot.r;
                float stonePatch = foot.g;

                float meadow = smoothstep(0.3, 0.7, Fbm(xz * _MeadowScale + 50.0));
                half3 col = lerp(_GrassDark.rgb, _GrassLight.rgb, meadow);
                col = lerp(col, _UnderGrass.rgb, smoothstep(0.0, 0.8, cover) * 0.75);

                float2 detailUV = xz / _DetailTile;
                col *= Detail(TEXTURE2D_ARGS(_DetailGrass, sampler_DetailGrass), detailUV, _DetailStrength);
                half3 dustCol = _DustColor.rgb * Detail(TEXTURE2D_ARGS(_DetailGround, sampler_DetailGrass), detailUV, _DetailStrength);

                float trailBreak = smoothstep(0.35, 0.65, Fbm(xz * 0.5 + 3.0));
                col = lerp(col, dustCol, saturate(trail * trailBreak * 1.4) * 0.7);

                float blotch = (Fbm(xz * 0.6 + 21.0) - 0.5) * 0.3;
                float dust = smoothstep(0.05, 0.45, village + blotch);
                col = lerp(col, dustCol, dust * 0.65);

                float sandMask = smoothstep(0.3, 0.7, village + blotch);
                if (sandMask > 0)
                {
                    float fine = Fbm(xz * 2.1);
                    half3 sand = _SandColor.rgb * lerp(0.9, 1.05, fine) * Detail(TEXTURE2D_ARGS(_DetailSand, sampler_DetailGrass), detailUV, _DetailStrength);
                    sand = lerp(sand, _CrackColor.rgb, Cracks(xz * 0.9, 0.025) * 0.28 * _CrackStrength * smoothstep(0.55, 0.9, village));

                    float rockField = stonePatch + (Fbm(xz * 0.3 + 31.0) - 0.5) * 0.5
                                    - (1 - smoothstep(0.55, 0.85, village)) * 0.3;
                    float clearEdge = smoothstep(0.45, 0.95, village + (Fbm(xz * 0.35 + 63.0) - 0.5) * 0.35);
                    rockField -= clearEdge * _RockInnerClear * 1.5;

                    float2 bp = xz * _RockBreakupScale + (warp * 3.0);
                    float breakNoise = Fbm(bp + 41.0);
                    float gate = smoothstep(0.3, 0.6, breakNoise);
                    rockField = lerp(rockField, rockField * gate, _RockBreakup)
                              + (breakNoise - 0.5) * _RockBreakup * 0.6;
                    float threshold = lerp(0.8, 0.35, _RockAmount);
                    float speck = (Fbm(xz * 1.6 + 13.0) - 0.5) * 0.35 + (fine - 0.5) * 0.2;
                    float rock = smoothstep(threshold - 0.18, threshold + 0.12, rockField + speck);
                    float inner = smoothstep(threshold, threshold + 0.3, rockField);

                    half rd = Detail(TEXTURE2D_ARGS(_DetailRock, sampler_DetailGrass), detailUV, _RockDetailStrength) - 1.0;
                    rd = 1.0 + rd * rsqrt(1.0 + rd * rd * 9.0);
                    half3 stone = _RockColor.rgb * lerp(0.88, 1.08, Fbm(xz * 0.8 + 7.0)) * rd;
                    stone = lerp(stone, _CrackColor.rgb * 0.6, Cracks(xz * 0.55 + 4.0, 0.03) * 0.55 * _CrackStrength * inner);
                    stone = lerp(lerp(stone, sand, 0.3), stone * 0.92, inner);

                    float overNoise = Fbm(xz * _SandOverScale + 77.0) * 0.75 + Fbm(xz * _SandOverScale * 3.1 + 5.0) * 0.25;
                    float overThreshold = lerp(1.0, 0.25, _SandOverAmount);
                    float over = smoothstep(overThreshold - _SandOverSoftness, overThreshold + _SandOverSoftness,
                                            overNoise + (1 - inner) * 0.25);

                    half3 ground = lerp(sand, stone, rock);
                    ground = lerp(ground, sand, over * rock);
                    col = lerp(col, ground, sandMask);
                }

                float shoreDistance = length(xz) + (warp.x + warp.y) * _EdgeWarp * 2.0;
                float beach = smoothstep(_IslandRadius - _SandWidth, _IslandRadius - _SandWidth * 0.4, shoreDistance);
                if (beach > 0)
                {
                    half3 beachSand = _SandColor.rgb * lerp(0.92, 1.06, Fbm(xz * 2.1))
                                    * Detail(TEXTURE2D_ARGS(_DetailSand, sampler_DetailGrass), detailUV, _DetailStrength);
                    float wet = 1 - smoothstep(0.0, 0.5, IN.flatWS.y - _SeaLevel);
                    beachSand *= lerp(1.0, 0.72, wet);
                    col = lerp(col, beachSand, beach);
                }

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
                surf.smoothness = _Smoothness;
                surf.occlusion = 1;
                surf.alpha = 1;
                return UniversalFragmentPBR(inputData, surf);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "WorldBendPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "WorldBendPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "WorldBendPasses.hlsl"
            ENDHLSL
        }
    }
}
