// Shared by every Weather FX Free shader. The
// patterns work in world pixels (WeatherFX.PixelsPerUnit to a unit) with y down:
// WeatherFX gives the rect's size and top-left in them, and the quad's UV runs from its top left.

#include "UnityCG.cginc"

float _Intensity, _Speed, _Seed, _PixelSize, _ColorSteps;
float4 _RectSize, _Origin;  // xy
float _WfxTime;             // global, set by WeatherFX

#define WFX_TAU 6.2831853
#define RECT_SIZE _RectSize.xy
#define WORLD_ORIGIN _Origin.xy
#define TIME _WfxTime

struct wfx_appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct wfx_v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 screen : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

wfx_v2f wfx_vert(wfx_appdata v)
{
    wfx_v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    o.screen = ComputeScreenPos(o.pos);
    return o;
}

// Integer hash: a float hash of large coordinates rounds differently at each call site on some
// GPUs, which tears the noise into steps.
float wfx_hash_i(int2 i)
{
    uint2 q = asuint(i) * uint2(1597334673u, 3812015801u);
    uint n = (q.x ^ q.y) * 1597334673u;
    return float(n) * (1.0 / 4294967295.0);
}

// Two independent 0..1 values for a lattice cell.
float2 wfx_hash2_i(int2 i)
{
    return float2(wfx_hash_i(i), wfx_hash_i(i + int2(7919, 104729)));
}

float wfx_noise(float2 p)
{
    float2 fl = floor(p);
    int2 i = int2(fl);
    float2 f = p - fl;
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(wfx_hash_i(i), wfx_hash_i(i + int2(1, 0)), u.x),
                lerp(wfx_hash_i(i + int2(0, 1)), wfx_hash_i(i + int2(1, 1)), u.x), u.y);
}

// 0..1, spread to cover the range.
float wfx_fbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++)
    {
        v += a * wfx_noise(p);
        p = p * 2.03 + 17.0;
        a *= 0.5;
    }
    return saturate((v - 0.15) / 0.7);
}

// GLSL's mod: the result takes b's sign (HLSL's fmod takes a's).
float wfx_mod(float a, float b) { return a - b * floor(a / b); }

// UV snapped to the centre of its _PixelSize block, the blocks aligned to the world.
float2 wfx_uv(float2 uv)
{
    if (_PixelSize < 1.0)
        return uv;
    float2 p = WORLD_ORIGIN + uv * RECT_SIZE;
    p = (floor(p / _PixelSize) + 0.5) * _PixelSize;
    return (p - WORLD_ORIGIN) / RECT_SIZE;
}

// World position of a (snapped) UV, in pixels.
float2 wfx_pos(float2 uv)
{
    return WORLD_ORIGIN + uv * RECT_SIZE;
}

// One pixel, or one _PixelSize block: keeps thin lines visible.
float wfx_px()
{
    return max(_PixelSize, 1.0);
}

// Colour steps, counted in gamma space, whatever the project's colour space.
float3 wfx_steps(float3 rgb)
{
    if (_ColorSteps < 1.0)
        return rgb;
#ifdef UNITY_COLORSPACE_GAMMA
    return floor(rgb * _ColorSteps + 0.5) / _ColorSteps;
#else
    return GammaToLinearSpace(floor(LinearToGammaSpace(rgb) * _ColorSteps + 0.5) / _ColorSteps);
#endif
}

// Colour steps, and in pixel mode alpha in eight flat bands: crisp pixel-art edges, and a thin
// veil (fog, a flash) stays a veil instead of a dither pattern.
float4 wfx_finish(float4 c)
{
    c.a = saturate(c.a * _Intensity);
    c.rgb = wfx_steps(c.rgb);
    if (_PixelSize >= 1.0)
        c.a = floor(c.a * 8.0 + 0.5) / 8.0;
    return c;
}

// Screen UV per rect pixel, from derivatives: holds under camera zoom and in a flipped render target.
float2 wfx_screen_per_px(wfx_v2f i)
{
    float2 suv = i.screen.xy / i.screen.w;
    return float2(ddx(suv.x) / ddx(i.uv.x), ddy(suv.y) / ddy(i.uv.y)) / RECT_SIZE;
}
