// Gradient sky dome with soft sun glow. Horizon colour equals the fog colour so the sea melts into mist.
Shader "ARR/Sky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        Cull Front ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _ArrSunDir, _ArrSunColor, _ArrHorizon, _ArrZenith;

            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, vertex).xyz;
                o.dir = wp - _WorldSpaceCameraPos;
                o.pos = UnityObjectToClipPos(vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = saturate(d.y);
                float3 col = lerp(_ArrHorizon.rgb, _ArrZenith.rgb, pow(h, 0.55));
                // a faint violet-rose band just above the horizon (picture-book gradient)
                col += float3(0.30, 0.12, 0.34) * exp(-h * 9.0) * 0.35;
                float s = saturate(dot(d, normalize(_ArrSunDir.xyz)));
                // soft moon halo, and a large, pale, stippled moon disc
                col += float3(0.55, 0.6, 0.85) * (pow(s, 10) * 0.12 + pow(s, 160) * 0.22);
                float disc = smoothstep(0.99905, 0.99925, s);
                float3 mp = d * 900;
                float st = frac(sin(dot(floor(mp), float3(12.9898, 78.233, 37.719))) * 43758.5453);
                float stipple = lerp(0.78, 1.0, step(0.45, st));
                col = lerp(col, float3(0.93, 0.92, 0.74) * stipple, disc);
                // stars: hashed cells on the sphere, fading toward the misty horizon
                float3 sp = d * 220;
                float3 cell = floor(sp);
                float hsh = frac(sin(dot(cell, float3(12.9898, 78.233, 37.719))) * 43758.5453);
                float3 fp = frac(sp) - 0.5;
                float star = step(0.985, hsh) * smoothstep(0.18, 0.0, length(fp)) * (0.6 + 0.4 * sin(_Time.y * (1 + hsh * 3) + hsh * 50));
                col += float3(0.85, 0.88, 1.0) * star * smoothstep(0.05, 0.35, h) * 0.9;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
