Shader "Project/SelectionRing"
{
    Properties
    {
        [Header(Rim)]
        [HDR] _RimColor ("Rim Color", Color) = (0.95, 0.85, 0.4, 0.8)
        _RimRadius ("Rim Radius", Range(0.1, 0.99)) = 0.95
        _RimWidth ("Rim Width", Range(0.002, 0.2)) = 0.03
        _RimSoftness ("Rim Edge Softness", Range(0, 0.1)) = 0.01

        [Header(Fill)]
        _FillColor ("Fill Color", Color) = (0.95, 0.85, 0.4, 0.12)
        _FillClearRadius ("Clear Centre Radius (fraction of rim)", Range(0, 0.95)) = 0.5
        _FillFalloff ("Fill Falloff Power (higher = sharper ramp to rim)", Range(0.2, 6)) = 1.5

        [Header(Depth Cue)]
        _FarSideOpacity ("Far Side Opacity", Range(0, 1)) = 0.25
        _FarFadeStart ("Fade Start (-1 = camera side, 1 = far side)", Range(-1, 1)) = -0.7
        _FarFadeEnd ("Fade End", Range(-1, 1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -2, -2

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldBend.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                half _RimRadius;
                half _RimWidth;
                half _RimSoftness;
                half4 _FillColor;
                half _FillClearRadius;
                half _FillFalloff;
                half _FarSideOpacity;
                half _FarFadeStart;
                half _FarFadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = WorldBend(TransformObjectToWorld(input.positionOS.xyz));
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float r = length(input.uv - 0.5) * 2.0;

                float halfWidth = _RimWidth * 0.5;
                float rim = 1.0 - smoothstep(max(halfWidth - _RimSoftness, 0.0), halfWidth, abs(r - _RimRadius));

                float fillMask = 1.0 - step(_RimRadius, r);
                float fill = fillMask * pow(smoothstep(_FillClearRadius, 1.0, saturate(r / _RimRadius)), _FillFalloff);

                float rimAlpha = rim * _RimColor.a;
                float fillAlpha = fill * _FillColor.a;
                float alpha = max(rimAlpha, fillAlpha);
                half3 colour = rimAlpha >= fillAlpha ? _RimColor.rgb : _FillColor.rgb;

                float2 centre = float2(unity_ObjectToWorld._m03, unity_ObjectToWorld._m23);
                float2 dir = input.positionWS.xz - centre;
                float2 camForward = centre - _WorldSpaceCameraPos.xz;
                float facing = dot(normalize(dir + 1e-5), normalize(camForward + 1e-5));
                float far = smoothstep(_FarFadeStart, _FarFadeEnd, facing);
                alpha *= lerp(1.0, _FarSideOpacity, far);

                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
