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

            float4 _DistanceFogCenter;
            float4 _DistanceFogParams;
            half4 _DistanceFogColor;

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    if (depth <= 0.0) return color;
                #else
                    if (depth >= 1.0) return color;
                #endif

                float3 positionWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float dist = distance(positionWS.xz, _DistanceFogCenter.xz);
                half fog = smoothstep(_DistanceFogParams.x, _DistanceFogParams.y, dist) * _DistanceFogParams.z;
                color.rgb = lerp(color.rgb, _DistanceFogColor.rgb, fog);
                return color;
            }
            ENDHLSL
        }
    }
}
