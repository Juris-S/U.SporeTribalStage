Shader "Project/DistanceFog"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "DistanceFog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "WorldBend.hlsl"

            float4 _DistanceFogCenter;
            float4 _DistanceFogParams;
            half4 _DistanceFogColor;
            float4 _IslandSea;

            bool HitSea(float3 origin, float3 dir, out float t)
            {
                float2 rel = origin.xz - _WorldBendPivot.xz;
                float a = _WorldBendAmount * dot(dir.xz, dir.xz);
                float b = dir.y + 2.0 * _WorldBendAmount * dot(rel, dir.xz);
                float c = origin.y - _IslandSea.y + _WorldBendAmount * dot(rel, rel);
                t = 0.0;
                if (c <= 0.0) return false;
                if (a < 1e-8)
                {
                    if (b >= 0.0) return false;
                    t = -c / b;
                    return true;
                }
                float disc = b * b - 4.0 * a * c;
                if (disc < 0.0) return false;
                t = (-b - sqrt(disc)) / (2.0 * a);
                return t > 0.0;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    bool sky = depth <= 0.0;
                #else
                    bool sky = depth >= 1.0;
                #endif

                float3 positionWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                if (_IslandSea.x > 0.0)
                {
                    float3 origin = ComputeWorldSpacePosition(uv, UNITY_NEAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
                    float3 toScene = positionWS - origin;
                    float sceneDistance = sky ? 1e9 : length(toScene);
                    float t;
                    if (HitSea(origin, normalize(toScene), t) && t < sceneDistance)
                    {
                        positionWS = origin + normalize(toScene) * t;
                        sky = false;
                    }
                }
                if (sky) return color;

                float dist = distance(positionWS.xz, _DistanceFogCenter.xz);
                half fog = smoothstep(_DistanceFogParams.x, _DistanceFogParams.y, dist) * _DistanceFogParams.z;
                color.rgb = lerp(color.rgb, _DistanceFogColor.rgb, fog);
                return color;
            }
            ENDHLSL
        }
    }
}
