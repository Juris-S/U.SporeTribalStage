#ifndef PROJECT_WIND_INCLUDED
#define PROJECT_WIND_INCLUDED

float4 _GlobalWindDir;
float _GlobalWindStrength;
float _GlobalWindPhase;

float3 WindOffset(float3 positionWS, float weight)
{
    float2 dir = _GlobalWindDir.xy;
    float along = dot(positionWS.xz, dir);
    float gust = 0.5 + 0.5 * sin(along * 0.12 - _GlobalWindPhase);
    gust *= gust;
    float sway = sin(_Time.y * 1.9 + positionWS.x * 0.37 + positionWS.z * 0.23) * 0.3;
    float push = _GlobalWindStrength * (0.45 + 0.75 * gust + sway);
    return float3(dir.x, 0, dir.y) * push * weight;
}

#ifdef _WIND
float3 ApplyFoliageWind(float3 positionWS)
{
    float3 origin = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
    float h = max(positionWS.y - origin.y, 0);
    positionWS += WindOffset(origin, h * h * _WindFlex);

    float flutter = sin(_Time.y * 7.0 + dot(positionWS, float3(1.7, 2.3, 1.1)));
    positionWS += float3(0.6, 0.3, 0.5) * flutter * _WindFlutter * saturate(h * 0.5) * _GlobalWindStrength;
    return positionWS;
}
#endif

#endif
