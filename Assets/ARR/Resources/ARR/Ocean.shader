// Stylised soft ocean: Gerstner waves driven by global _ArrWaves (mirrored on the CPU in Waves.cs),
// fresnel sky reflection, breath ripple ring and a warm "companion" light pool.
Shader "ARR/Ocean"
{
    Properties
    {
        _DeepColor ("Deep", Color) = (0.05, 0.28, 0.34, 1)
        _ShallowColor ("Shallow", Color) = (0.17, 0.53, 0.56, 1)
        _FoamColor ("Foam", Color) = (0.85, 0.95, 0.95, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float4 _DeepColor, _ShallowColor, _FoamColor;
            float4 _ArrWaves[4];      // xy = direction, z = steepness, w = wavelength
            float _ArrWaveAmp;
            float _ArrTime;
            float4 _ArrSunDir;        // xyz
            float4 _ArrSunColor;
            float4 _ArrHorizon;
            float4 _ArrZenith;
            float4 _ArrRipple;        // xyz centre, w = age (s)
            float _ArrRippleStrength;
            float4 _ArrGlowPos;       // xyz, w intensity
            float4 _ArrGlowColor;
            float4 _ArrBeamOrigin;    // xyz, w = intensity
            float4 _ArrBeamDir;       // xz direction, w = length
            float4 _ArrTreasure;      // the surprise the light is on: xyz, w = glow intensity
            float4 _ArrTreasureColor;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 wpos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float height : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            void Gerstner(float4 w, float3 p, inout float3 disp, inout float3 tangent, inout float3 binormal)
            {
                float k = 6.28318 / w.w;
                float c = sqrt(9.8 / k);
                float2 d = normalize(w.xy);
                float s = w.z * _ArrWaveAmp;
                float f = k * (dot(d, p.xz) - c * _ArrTime);
                float a = s / k;
                float cf = cos(f), sf = sin(f);
                tangent += float3(-d.x * d.x * s * sf, d.x * s * cf, -d.x * d.y * s * sf);
                binormal += float3(-d.x * d.y * s * sf, d.y * s * cf, -d.y * d.y * s * sf);
                disp += float3(d.x * a * cf, a * sf, d.y * a * cf);
            }

            v2f vert (appdata v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 disp = 0;
                float3 t = float3(1, 0, 0);
                float3 b = float3(0, 0, 1);
                Gerstner(_ArrWaves[0], wp, disp, t, b);
                Gerstner(_ArrWaves[1], wp, disp, t, b);
                Gerstner(_ArrWaves[2], wp, disp, t, b);
                Gerstner(_ArrWaves[3], wp, disp, t, b);
                wp += disp;
                o.wpos = wp;
                o.normal = normalize(cross(b, t));
                o.height = disp.y;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                // low-poly facets
                float3 fn = normalize(cross(ddy(i.wpos), ddx(i.wpos)));
                if (fn.y < 0) fn = -fn;
                n = normalize(lerp(n, fn, 0.75));
                // fine procedural ripples
                float2 q = i.wpos.xz;
                float r1 = sin(q.x * 0.9 + _ArrTime * 1.7) * cos(q.y * 0.7 - _ArrTime * 1.3);
                float r2 = sin(q.x * 0.37 - q.y * 0.53 + _ArrTime * 0.9);
                n = normalize(n + float3(r1 * 0.02, 0, r2 * 0.02));

                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);
                float fres = pow(1 - saturate(dot(n, v)), 4);
                float hmix = saturate(i.height * 0.6 + 0.45);
                float3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, hmix);

                float3 rdir = reflect(-v, n);
                float3 sky = lerp(_ArrHorizon.rgb, _ArrZenith.rgb, saturate(rdir.y * 2));
                col = lerp(col, sky, saturate(fres * 0.85 + 0.08));

                float3 sun = normalize(_ArrSunDir.xyz);
                float spec = pow(saturate(dot(rdir, sun)), 60);
                col += _ArrSunColor.rgb * spec * 0.55;
                col += _ArrSunColor.rgb * pow(saturate(dot(rdir, sun)), 6) * 0.06;
                // moonlit facets catch a little pink
                col += float3(0.82, 0.54, 0.72) * pow(saturate(n.x * 0.5 + 0.5), 6) * 0.05;

                float foam = saturate((i.height - 0.32) * 2.2);
                col = lerp(col, _FoamColor.rgb, foam * 0.35);

                // illustrated sea: a few thin, flowing pale lines drifting with the swell
                float2 q2 = i.wpos.xz;
                float flowLine = sin(q2.x * 0.21 + q2.y * 0.05 + sin(q2.y * 0.045 + _ArrTime * 0.15) * 3.2 + _ArrTime * 0.35);
                float lineMask = smoothstep(0.965, 0.995, flowLine) * smoothstep(0.35, 0.75, sin(q2.y * 0.031 + q2.x * 0.017) * 0.5 + 0.5);
                col += float3(0.55, 0.65, 1.0) * lineMask * 0.22;

                // breath ripple ring around the boat
                float dist = distance(i.wpos.xz, _ArrRipple.xz);
                float radius = 2 + _ArrRipple.w * 5.5;
                float ring = exp(-pow((dist - radius) / 1.6, 2)) * saturate(1 - _ArrRipple.w / 3.2);
                col += float3(0.6, 0.72, 1.0) * ring * _ArrRippleStrength * 0.3;

                // warm companion light pool
                float gd = distance(i.wpos.xz, _ArrGlowPos.xz);
                col += _ArrGlowColor.rgb * exp(-gd * gd / 30) * _ArrGlowPos.w * 0.45;
                // the lighthouse beam paints a golden path on the water
                float3 rel = i.wpos - _ArrBeamOrigin.xyz;
                float along = dot(rel.xz, _ArrBeamDir.xz);
                float across = abs(rel.x * _ArrBeamDir.z - rel.z * _ArrBeamDir.x);
                float width = 3.0 + along * 0.06;
                float path = exp(-pow(across / width, 2)) * smoothstep(8, 30, along) * saturate(1 - along / _ArrBeamDir.w);
                float3 beamCol = float3(1.0, 0.82, 0.49) * path * (0.55 + 0.45 * fres) * _ArrBeamOrigin.w;

                fixed4 c = fixed4(col, 1);
                UNITY_APPLY_FOG(i.fogCoord, c);
                // the golden path is light, not water colour: it stays visible through the mist from far away
                float camDist = distance(_WorldSpaceCameraPos, i.wpos);
                c.rgb += beamCol * exp(-camDist / 420.0);
                // a small patch of glowing sea where the light's surprise is hiding, with soft ripples spreading out;
                // added after the fog like the golden path, so it can be found even in thick mist
                float td = distance(i.wpos.xz, _ArrTreasure.xz);
                float pool = exp(-td * td / 121.0);
                float ripples = 0.75 + 0.25 * sin(td * 0.9 - _ArrTime * 1.8);
                c.rgb += _ArrTreasureColor.rgb * pool * ripples * _ArrTreasure.w * 0.85 * exp(-camDist / 420.0);
                return c;
            }
            ENDCG
        }
    }
}
