// Renders the GPU stardust buffer as soft camera-facing points (6 vertices per particle, no mesh).
Shader "ARR/Stardust"
{
    Properties
    {
        _MainTex ("Dot", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct Particle
            {
                float3 pos; float life; float3 vel; float maxLife;
                float3 home; float seed; float kind; float offset; float size; float pad;
            };
            StructuredBuffer<Particle> _Particles;
            sampler2D _MainTex;
            float _Brightness;
            float4 _BoatScreen;     // xy = boat position in NDC, z = aspect
            float _MinPixels;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };

            static const float2 corners[6] = { float2(-1,-1), float2(1,-1), float2(1,1), float2(-1,-1), float2(1,1), float2(-1,1) };

            float3 palette(float s)
            {
                // cold white, pale blue, lavender, and a little pink
                if (s < 0.38) return float3(0.86, 0.92, 1.0);
                if (s < 0.70) return float3(0.58, 0.76, 1.0);
                if (s < 0.95) return float3(0.76, 0.66, 1.0);
                return float3(1.0, 0.62, 0.86);
            }

            v2f vert (uint vid : SV_VertexID)
            {
                v2f o;
                uint id = vid / 6;
                float2 c = corners[vid % 6];
                Particle P = _Particles[id];

                float t = saturate(P.life / max(P.maxLife, 1e-3));
                float fade = smoothstep(0.0, 0.18, t) * smoothstep(1.0, 0.82, t);   // fade in / out, never pop
                if (P.life <= 0) fade = 0;

                float3 viewC = mul(UNITY_MATRIX_V, float4(P.pos, 1)).xyz;
                float depth = max(-viewC.z, 0.05);
                float size = P.size;
                if (P.kind > 1.5) size *= 0.8 + 0.4 * sin(_Time.y * (1.5 + P.seed * 3) + P.seed * 40);  // twinkle
                // keep distant dust at least ~1.5 px wide, trading size for brightness (no shimmer)
                float px = size * _ScreenParams.y * UNITY_MATRIX_P[1][1] * 0.5 / depth;
                float boost = 1;
                if (px < _MinPixels) { boost = (px / _MinPixels) * (px / _MinPixels); size *= _MinPixels / px; }
                // ...and never larger than a few pixels: starlight, not bokeh
                float maxPx = P.kind > 1.5 ? 9.0 : (P.kind > 0.5 ? 6.0 : 4.5);
                if (px > maxPx) size *= maxPx / px;

                // moving air stretches the dust into soft streaks along the flow
                float2 vv = mul((float3x3)UNITY_MATRIX_V, P.vel).xy;
                float sp = length(vv);
                float2 dir = sp > 1e-3 ? vv / sp : float2(0, 1);
                float2 perp = float2(-dir.y, dir.x);
                float stretch = P.kind < 0.5 ? 1 + min(sp * 0.7, 6.0) : (P.kind < 1.5 ? 1 + min(sp * 0.45, 3.5) : 1 + min(sp * 0.4, 3.0));
                float3 viewP = viewC + float3((perp * c.x + dir * c.y * stretch) * size, 0);
                o.pos = mul(UNITY_MATRIX_P, float4(viewP, 1));
                o.uv = c * 0.5 + 0.5;

                // low visual obstruction: thin out around the boat on screen, and right in front of the camera
                float4 cc = mul(UNITY_MATRIX_P, float4(viewC, 1));
                float2 ndc = cc.xy / max(cc.w, 1e-3);
                float2 dd = (ndc - _BoatScreen.xy) * float2(_BoatScreen.z, 1);
                float clear = smoothstep(0.06, 0.32, length(dd));
                bool mist = P.kind > 0.5 && P.kind < 1.5;
                float nearFade = mist ? smoothstep(4.0, 11.0, depth) : smoothstep(5.0, 16.0, depth);

                float a;
                if (P.kind < 0.5) a = 0.42;
                else if (mist) a = 0.24;
                else a = 0.8;
                // air that is moving catches a little more light, so the breath's current is readable
                a *= (1 + saturate(sp * 0.25) * 0.8) / sqrt(stretch);
                a *= fade * boost * nearFade * lerp(mist ? 0.6 : 0.35, 1.0, clear) * _Brightness;
                float3 col = mist ? lerp(float3(0.70, 0.72, 1.0), float3(0.86, 0.80, 1.0), P.seed) : palette(P.seed);
                o.color = float4(col, a);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0));
                return col;
            }
            ENDCG
        }
    }
}
