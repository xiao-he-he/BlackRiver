Shader "BigDeepSea/HomeWindowDarkness"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PlayerHole ("Player Hole", Vector) = (-100000, -100000, 0, 1)
        _PlayerDarkness ("Player Darkness", Range(0, 1)) = 0.35
        _FireplaceHole ("Fireplace Hole", Vector) = (-100000, -100000, 0, 1)
        _FireplaceVisibility ("Fireplace Visibility", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _PlayerHole;
            float _PlayerDarkness;
            float4 _FireplaceHole;
            float _FireplaceVisibility;

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            half4 frag(VertexOutput input) : SV_Target
            {
                float2 pixel = input.uv * _ScreenParams.xy;
                float playerGlow = 1.0 - smoothstep(_PlayerHole.z,
                    _PlayerHole.z + _PlayerHole.w, distance(pixel, _PlayerHole.xy));
                float playerAlpha = lerp(1.0, _PlayerDarkness, playerGlow);
                float fireplaceGlow = 1.0 - smoothstep(0.0, max(0.001, _FireplaceHole.z),
                    distance(pixel, _FireplaceHole.xy));
                float fireplaceAlpha = 1.0 - fireplaceGlow * _FireplaceVisibility;
                return half4(0, 0, 0, min(playerAlpha, fireplaceAlpha));
            }
            ENDHLSL
        }
    }
}
