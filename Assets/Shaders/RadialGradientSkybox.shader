Shader "BoxPushing/RadialGradientSkybox"
{
    Properties
    {
        _CenterColor ("Center Color", Color) = (0.957, 0.941, 0.902, 1)
        _EdgeColor ("Edge Color", Color) = (0.812, 0.863, 0.843, 1)
        _Radius ("Radius", Range(0.2, 3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            half4 _CenterColor;
            half4 _EdgeColor;
            half _Radius;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.screenPos.xy / max(0.0001, i.screenPos.w);
                float2 centered = uv - 0.5;
                float r = length(centered) * 2.0 / max(0.0001, _Radius);
                r = saturate(r);
                return lerp(_CenterColor, _EdgeColor, smoothstep(0.0, 1.0, r));
            }
            ENDCG
        }
    }
    FallBack Off
}
