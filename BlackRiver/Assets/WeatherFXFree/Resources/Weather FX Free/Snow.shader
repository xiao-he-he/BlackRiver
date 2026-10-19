// Snowflakes in four parallax depths, swaying as they drift down.
Shader "Weather FX Free/Snow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 0.95)
        _Density ("Density", Range(0, 1)) = 0.5
        _Wind ("Wind", Range(-1, 1)) = 0.2
        _FlakeSize ("Flake Size", Range(1, 10)) = 3.5   // pixels, nearest layer
        _FallSpeed ("Fall Speed", Range(0.2, 3)) = 1
        _Drift ("Drift", Range(0, 1)) = 0.5             // side to side sway
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
            float _Density, _Wind, _FlakeSize, _FallSpeed, _Drift;

            // Flake alpha of one layer; depth 1 is nearest.
            float flakes(float2 p, float depth, float time, int layer)
            {
                float cell = 24.0 + 40.0 * depth;
                float fall = (22.0 + 50.0 * depth) * _FallSpeed;
                float2 q = p - float2(_Wind * fall * 1.5, fall) * time + float2(float(layer) * 71.3 + _Seed * 53.0, float(layer) * 37.1);
                float2 id = floor(q / cell);
                float2 f = q - id * cell;
                int2 ii = int2(id) + int2(layer * 10007, 0);
                float2 h = wfx_hash2_i(ii);
                float h3 = wfx_hash_i(ii + int2(31, 17));
                if (h3 > _Density * (1.2 - 0.25 * depth))
                    return 0.0;
                float r = max(_FlakeSize * (0.35 + 0.65 * depth) * (0.7 + 0.5 * h.x) * 0.5, 0.5 * wfx_px());
                float room = cell * 0.5 - r - 1.0;
                float sway = _Drift * room * 0.5;
                float swing = sin(time * (0.8 + 0.8 * h.y) * _FallSpeed + h.y * WFX_TAU);
                float2 c = float2(cell * 0.5 + (h.x - 0.5) * 2.0 * (room - sway) + swing * sway,
                                  cell * (0.5 + (h.y - 0.5) * 0.7));
                float soft = _PixelSize >= 1.0 ? 0.01 : 0.6 + 0.4 * depth;
                float a = 1.0 - smoothstep(r - soft * 0.5, r + soft * 0.5, length(f - c));
                return a * (0.4 + 0.6 * depth);
            }

            float4 frag(wfx_v2f i) : SV_Target
            {
                float2 uv = wfx_uv(i.uv);
                float2 local = uv * RECT_SIZE;
                float time = wfx_mod(TIME, 600.0) * _Speed;
                float a = 0.0;
                for (int l = 0; l < 4; l++)
                {
                    float depth = 0.25 + 0.25 * float(l);
                    a = max(a, flakes(local + WORLD_ORIGIN * depth, depth, time, l));
                }
                return wfx_finish(float4(_Color.rgb, _Color.a * a));
            }
            ENDCG
        }
    }
}
