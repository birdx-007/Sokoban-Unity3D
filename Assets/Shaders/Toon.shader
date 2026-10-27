Shader "BoxPushing/Toon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _RampBands ("Ramp Bands", Range(1, 8)) = 3
        _RampSmoothness ("Ramp Smoothness", Range(0, 1)) = 0.65
        _ShadowTint ("Shadow Tint", Color) = (0.58, 0.52, 0.44, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 0.6
        [Toggle] _FlatShade ("Flat Shade", Float) = 0

        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionStrength ("Emission Strength", Range(0, 3)) = 0
        _Pulse ("Glow Pulse", Range(0, 2)) = 0

        _GridLineColor ("Grid Line Color", Color) = (0,0,0,0)
        _GridCellSize ("Grid Cell Size", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf ToonLighting fullforwardshadows addshadow
        #pragma target 3.0

        half4 _Color;
        half _RampBands;
        half _RampSmoothness;
        half4 _ShadowTint;
        half _AmbientStrength;
        half _FlatShade;
        half4 _EmissionColor;
        half _EmissionStrength;
        half _Pulse;
        half4 _GridLineColor;
        half _GridCellSize;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        half ToonRamp(half ndl)
        {
            if (_FlatShade > 0.5)
                return 1.0;

            half x = saturate(ndl);
            half bands = max(2.0, _RampBands);
            half levels = bands - 1.0;
            half scaled = x * levels;
            half cell = floor(scaled);
            half frac = scaled - cell;
            half halfSmooth = saturate(_RampSmoothness) * 0.5;
            half smoothFrac = smoothstep(0.5 - halfSmooth, 0.5 + halfSmooth, frac);
            return saturate((cell + smoothFrac) / levels);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
            o.Emission = _EmissionColor.rgb * (_EmissionStrength + _Pulse);

            // Optional fine grid lines on upward-facing surfaces (floor tops).
            if (_GridLineColor.a > 0.0001)
            {
                float2 uv = IN.worldPos.xz / max(0.0001, _GridCellSize);
                float2 grid = abs(frac(uv - 0.5) - 0.5) / max(fwidth(uv), 0.0001);
                float lineMask = 1.0 - min(1.0, min(grid.x, grid.y));
                float up = saturate(IN.worldNormal.y * 20.0);
                o.Albedo = lerp(o.Albedo, _GridLineColor.rgb, saturate(lineMask * up * _GridLineColor.a));
            }
        }

        half4 LightingToonLighting (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            if (_FlatShade > 0.5)
            {
                half4 flat;
                flat.rgb = s.Albedo + s.Emission;
                flat.a = s.Alpha;
                return flat;
            }

            half ndl = dot(s.Normal, lightDir) * 0.5 + 0.5;
            half ramp = ToonRamp(ndl);

            half3 ambient = ShadeSH9(half4(s.Normal, 1.0)) * _AmbientStrength;
            half3 shadowed = s.Albedo * _ShadowTint.rgb;
            half3 lit = s.Albedo * _LightColor0.rgb;
            half3 direct = lerp(shadowed, lit, ramp) * atten;

            half4 c;
            c.rgb = ambient * s.Albedo + direct + s.Emission;
            c.a = s.Alpha;
            return c;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
