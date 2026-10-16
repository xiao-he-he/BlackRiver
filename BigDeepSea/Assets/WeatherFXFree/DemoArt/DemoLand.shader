// The demo's landscape: its texture as it is (its Tiling and Offset cut the landscape out of the
// power-of-two texture it is padded to), unlit, in any render pipeline.
Shader "Weather FX Free/Demo Land"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return float4(tex2D(_MainTex, i.uv).rgb, 1.0);
            }
            ENDCG
        }
    }
}
