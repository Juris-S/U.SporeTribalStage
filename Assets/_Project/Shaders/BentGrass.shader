Shader "Project/BentGrass"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _RootTint ("Root Tint", Color) = (0.5, 0.68, 0.4, 1)
        _RootFade ("Root Fade Height (m)", Range(0.05, 1)) = 0.35
        _GrassHeight ("Blade Height (m)", Range(0.1, 2)) = 0.7
        _NormalUp ("Normal Blend To Up", Range(0, 1)) = 0.7
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _WindStrength ("Wind Bend (m at normal wind)", Range(0, 0.5)) = 0.14
        _PushRadius ("Push Radius (m)", Range(0.2, 4)) = 1.4
        _PushStrength ("Push Strength (m)", Range(0, 1.5)) = 0.55
        _Wobble ("Wobble (m)", Range(0, 0.3)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WorldBend.hlsl"
            #include "Wind.hlsl"

            #define MAX_PUSHERS 64

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _RootTint;
                half _RootFade;
                half _GrassHeight;
                half _NormalUp;
                half _Smoothness;
                half _WindStrength;
                half _PushRadius;
                half _PushStrength;
                half _Wobble;
            CBUFFER_END

            float4 _GrassPushers[MAX_PUSHERS];
            float _GrassPusherCount;
            float _GrassFade;

            TEXTURE2D(_GroundFootprints);
            SAMPLER(sampler_GroundFootprints);
            float4 _GroundFootprintRect;

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half height : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = TransformObjectToWorld(IN.positionOS.xyz);

                float h = saturate(pos.y / max(_GrassHeight, 0.01));
                float stiff = h * h;
                OUT.height = h;

                float2 footUV = (pos.xz - _GroundFootprintRect.xy) / max(_GroundFootprintRect.z, 0.001);
                float worn = smoothstep(0.05, 0.4, SAMPLE_TEXTURE2D_LOD(_GroundFootprints, sampler_GroundFootprints, footUV, 0).r);
                float sink = max(_GrassFade, worn);
                pos.y = pos.y * (1.0 - sink) - sink * 0.15;

                pos += WindOffset(pos, _WindStrength * stiff);
                pos.xz += sin(_Time.y * 4.0 + pos.x * 2.1 + pos.z * 1.7) * 0.02 * stiff * _GlobalWindStrength;

                int count = (int)_GrassPusherCount;
                for (int i = 0; i < count; i++)
                {
                    float4 p = _GrassPushers[i];
                    float2 d = pos.xz - p.xz;
                    float radius = _PushRadius * p.w;
                    float dist = length(d);
                    float f = saturate(1.0 - dist / radius);
                    f = f * f * (3.0 - 2.0 * f);
                    float2 dir = d / max(dist, 0.001);
                    float2 side = float2(-dir.y, dir.x);

                    pos.xz += dir * f * _PushStrength * stiff;
                    pos.xz += side * sin(_Time.y * 11.0 + dist * 5.0) * f * _Wobble * stiff;
                    pos.y -= f * _PushStrength * 0.45 * stiff;
                }

                OUT.positionWS = WorldBend(pos);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                float3 n = TransformObjectToWorldNormal(IN.normalOS);
                OUT.normalWS = normalize(lerp(n, float3(0, 1, 0), _NormalUp));
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                half rootT = saturate(IN.height * _GrassHeight / max(_RootFade, 0.01));
                albedo.rgb *= lerp(_RootTint.rgb, half3(1, 1, 1), rootT);

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
                surf.albedo = albedo.rgb;
                surf.smoothness = _Smoothness;
                surf.occlusion = lerp(0.6, 1.0, rootT);
                surf.alpha = 1;
                return UniversalFragmentPBR(inputData, surf);
            }
            ENDHLSL
        }
    }
}
