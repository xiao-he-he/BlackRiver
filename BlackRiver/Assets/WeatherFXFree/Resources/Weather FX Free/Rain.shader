// Angled rain in three depths, splashing where it meets the ground line.
Shader "Weather FX Free/Rain"
{
    Properties
    {
        _Color ("Color", Color) = (0.74, 0.82, 0.96, 0.7)
        _Density ("Density", Range(0, 1)) = 0.5
        _Wind ("Wind", Range(-1, 1)) = 0.25                // slant; + blows to the right
        _DropLength ("Drop Length", Range(4, 80)) = 26     // pixels, nearest layer
        _FallSpeed ("Fall Speed", Range(0.2, 3)) = 1
        _Ground ("Ground", Range(0, 1)) = 1                // splash line, fraction of the height; 1 = none
        _Splashes ("Splashes", Range(0, 1)) = 0.8
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
            float _Density, _Wind, _DropLength, _FallSpeed, _Ground, _Splashes;

            // Streak alpha of one layer; depth 1 is nearest.
            float streaks(float2 p, float depth, float time, int layer)
            {
                float slant = _Wind * 0.6;
                float w = 10.0 + 14.0 * depth;
                float len = _DropLength * (0.4 + 0.6 * depth);
                float h = len * 2.2 + 30.0;
                float2 q = p + float2(float(layer) * 137.0 + _Seed * 97.0, 0.0);
                q.x -= q.y * slant;  // skewed: drops fall straight down in q
                float col = floor(q.x / w);
                q.y -= time * (520.0 + 780.0 * depth) * _FallSpeed;
                q.y += wfx_hash_i(int2(int(col), layer * 7 + 3)) * h;
                int2 cell = int2(int(col), int(floor(q.y / h))) + int2(0, layer * 100003);
                float2 r = wfx_hash2_i(cell);
                if (wfx_hash_i(cell + int2(911, 0)) > _Density * (1.3 - 0.3 * depth))
                    return 0.0;
                float2 f = q - float2(col * w, floor(q.y / h) * h);
                float x0 = (0.2 + 0.6 * r.x) * w;
                float y0 = r.y * (h - len);
                float along = (f.y - y0) / len;
                float body = step(0.0, along) * step(along, 1.0) * pow(saturate(along), 1.3);
                float half_w = (0.35 + 0.45 * depth) * wfx_px() * (1.0 + abs(slant) * 0.3);
                float aa = _PixelSize >= 1.0 ? 0.01 : 0.9;
                float across = 1.0 - smoothstep(half_w, half_w + aa, abs(f.x - x0));
                return body * across * (0.35 + 0.65 * depth);
            }

            // A crown and two droplets popping up from the ground line.
            float splash(float2 local, float2 p, float gy, float time)
            {
                float cw = 26.0;
                float cellx = floor(p.x / cw);
                float rate = 1.6 * _FallSpeed;
                float a = 0.0;
                for (int k = 0; k < 2; k++)
                {
                    float ph = wfx_hash_i(int2(int(cellx), 17 + k));
                    float cyc = floor(time * rate + ph);
                    int2 id = int2(int(cellx), int(cyc) * 2 + k);
                    if (wfx_hash_i(id + int2(0, 5003)) > _Density * 1.2)
                        continue;
                    float age = frac(time * rate + ph);
                    float cx = (cellx + 0.2 + 0.6 * wfx_hash_i(id)) * cw - WORLD_ORIGIN.x;
                    float2 d = local - float2(cx, gy);
                    float r = 1.5 + age * 7.0;
                    float e = length(d / float2(r, r * 0.45)) - 1.0;
                    float ring = (1.0 - smoothstep(0.0, 1.6 / r, abs(e))) * step(d.y, 0.5);
                    float drops = 0.0;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float2 dp = float2(float(s) * (2.0 + age * 10.0), -age * (1.0 - age) * 26.0);
                        drops += 1.0 - smoothstep(0.6, 1.5, length(d - dp) / wfx_px());
                    }
                    a = max(a, (ring * 0.8 + drops) * pow(1.0 - age, 1.5));
                }
                return saturate(a);
            }

            float4 frag(wfx_v2f i) : SV_Target
            {
                float2 uv = wfx_uv(i.uv);
                float2 local = uv * RECT_SIZE;
                float time = wfx_mod(TIME, 300.0) * _Speed;
                float gy = _Ground * RECT_SIZE.y;
                float a = 0.0;
                for (int l = 0; l < 3; l++)
                {
                    float depth = 0.35 + 0.325 * float(l);
                    float2 p = local + WORLD_ORIGIN * depth;
                    float s = streaks(p, depth, time, l);
                    a = max(a, s * step(local.y, gy + 1.0));
                }
                float3 rgb = _Color.rgb;
                if (_Ground < 1.0)
                {
                    float sp = splash(local, wfx_pos(uv), gy, time) * _Splashes;
                    rgb = lerp(rgb, lerp(_Color.rgb, float3(1.0, 1.0, 1.0), 0.35), step(a, sp));
                    a = max(a, sp);
                }
                return wfx_finish(float4(rgb, _Color.a * a));
            }
            ENDCG
        }
    }
}
