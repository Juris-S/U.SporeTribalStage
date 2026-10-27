#ifndef PROJECT_WORLD_BEND_PASSES_INCLUDED
#define PROJECT_WORLD_BEND_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "WorldBend.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct BendAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
};

struct BendVaryings
{
    float4 positionCS : SV_POSITION;
    float3 normalWS : TEXCOORD0;
};

BendVaryings ShadowVert(BendAttributes IN)
{
    BendVaryings OUT;
    float3 positionWS = WorldBend(TransformObjectToWorld(IN.positionOS.xyz));
    float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    OUT.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
    OUT.normalWS = normalWS;
    return OUT;
}

half4 ShadowFrag(BendVaryings IN) : SV_Target
{
    return 0;
}

BendVaryings DepthVert(BendAttributes IN)
{
    BendVaryings OUT;
    OUT.positionCS = TransformWorldToHClip(WorldBend(TransformObjectToWorld(IN.positionOS.xyz)));
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    return OUT;
}

half DepthFrag(BendVaryings IN) : SV_Target
{
    return IN.positionCS.z;
}

half4 DepthNormalsFrag(BendVaryings IN) : SV_Target
{
#if defined(_GBUFFER_NORMALS_OCT)
    float3 normalWS = normalize(IN.normalWS);
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
    return half4(PackFloat2To888(remappedOctNormalWS), 0.0);
#else
    return half4(NormalizeNormalPerPixel(IN.normalWS), 0.0);
#endif
}

#endif
