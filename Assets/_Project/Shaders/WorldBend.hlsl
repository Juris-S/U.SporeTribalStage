#ifndef PROJECT_WORLD_BEND_INCLUDED
#define PROJECT_WORLD_BEND_INCLUDED

float _WorldBendAmount;
float4 _WorldBendPivot;

float3 WorldBend(float3 positionWS)
{
    float2 d = positionWS.xz - _WorldBendPivot.xz;
    positionWS.y -= dot(d, d) * _WorldBendAmount;
    return positionWS;
}

#endif
