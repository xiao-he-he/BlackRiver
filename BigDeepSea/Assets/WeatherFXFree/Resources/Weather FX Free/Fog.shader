// Rolling fog: three layers of billowing noise drifting with the wind, thicker near the ground.
Shader "Weather FX Free/Fog"
{
    Properties
    {
        _Color ("Color", Color) = (0.87, 0.9, 0.97, 0.85)
        _Density ("Density", Range(0, 1)) = 0.5
        _Wind ("Wind", Range(-1, 1)) = 0.3
        _FogScale ("Fog Scale", Range(0.25, 4)) = 1    // size of the billows
        _GroundFog ("Ground Fog", Range(0, 1)) = 0.6   // 0 even, 1 pooled at the bottom
        _Intensity ("Intensity", Range(0, 2)) = 1
        _Speed ("Speed", Range(0, 4)) = 1
        _Seed ("Seed", Float) = 0
        [IntRange] _PixelSize ("Pixel Size", Range(0, 16)) = 0
        [IntRange] _ColorSteps ("Color Steps", Range(0, 16)) = 0
        [HideInInspector] _RectSize ("Rect Size", Vector) = (1280, 720, 0, 0)
        [HideInInspector] _Origin ("Origin", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex wfx_vert
            #pragma fragment frag
            #pragma target 3.5
            #include "WeatherCommon.cginc"

            float4 _Color;
            float _Density, _Wind, _FogScale, _GroundFog;

            float4 frag(wfx_v2f i) : SV_Target
            {
                float2 uv = wfx_uv(i.uv);
                float2 local = uv * RECT_SIZE;
                float time = TIME * _Speed;
                float lo = lerp(0.85, -0.25, _Density);
                float clear = 1.0;
                float lit = 0.0;
                for (int l = 0; l < 3; l++)
                {
                    float depth = 0.5 + 0.25 * float(l);
                    float2 p = (local + WORLD_ORIGIN * depth) / (220.0 * _FogScale * (0.6 + 0.4 * depth)) * float2(0.45, 1.0);
                    p += float2(-time * _Wind * 0.06 * (0.4 + depth), time * 0.01) + float(l) * float2(5.3, 9.1) + _Seed;
                    float2 warp = float2(wfx_fbm(p * 0.6 + float2(time * 0.015, 3.0)), wfx_fbm(p * 0.6 + float2(7.0, time * 0.02)));
                    float n = wfx_fbm(p + (warp - 0.5) * float2(0.8, 0.5));
                    float f = smoothstep(lo, lo + 0.35, n);
                    clear *= 1.0 - f * 0.6;
                    lit = max(lit, n * f);
                }
                float a = 1.0 - clear;
                float m = lerp(1.0, smoothstep(-0.1, 1.0, uv.y), _GroundFog);
                float3 rgb = _Color.rgb * (0.88 + 0.18 * lit);
                return wfx_finish(float4(rgb, _Color.a * a * m));
            }
            ENDCG
        }
    }
}
