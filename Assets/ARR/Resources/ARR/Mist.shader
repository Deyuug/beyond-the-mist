// Soft, continuous sea mist: one slice of a layered (sliced) volume. Several slices stacked over the water with
// an exponential height profile approximate height fog with real local shape and movement.
//  - two scrolling tileable noise octaves give slow, drifting density (never particle "smoke")
//  - each breath (BreathFlow puffs) clears a soft tube along its air flow and drags the noise forward/outward,
//    plus a sphere mask around the bow that opens with the breath and closes slowly
//  - local fog volumes: denser mist around unfound surprises, clear pockets around the island and discoveries
//  - the lighthouse beam scatters warm light in the mist it passes through (cheap "volumetric" light)
Shader "ARR/Mist"
{
    Properties
    {
        _Layer ("Layer seed", Float) = 0
        _LayerWeight ("Layer density", Float) = 0.15
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.5
            #include "UnityCG.cginc"

            sampler2D _MistNoise;
            float _Layer, _LayerWeight;
            float _MistDensity;
            float4 _MistColorLow, _MistColorHigh;
            float4 _MistWind;          // xy, zw: two wind directions (m/s)
            float _ArrTime;
            float4 _ArrSunDir;
            float4 _ArrLamp;           // lantern position, w = intensity
            float4 _ArrBeam3D;         // beam direction, w = length
            float4 _ArrBow;            // bow position, w = always-clear radius
            float4 _ArrBowDir;         // boat heading (xz)
            float4 _ArrBeamOrigin;     // lighthouse xz, w = intensity (also drives the golden water path)
            float4 _ArrBeamDir;        // beam xz direction, w = length
            float4 _ArrClearSpots[5];  // xyz, w = clear radius
            float4 _ArrDenseSpots[4];  // xyz, w = extra density
            float4 _ArrTreasure;       // the surprise the light is on: xyz, w = glow intensity
            float4 _ArrTreasureColor;
            float4 _PuffOrigin[20];     // origin, w = centre distance
            float4 _PuffAxis[20];       // axis.x, axis.z, strength, width
            float4 _PuffLength[20];     // length, clear amount

            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; UNITY_FOG_COORDS(1) };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.wpos = mul(unity_ObjectToWorld, vertex).xyz;
                o.pos = UnityObjectToClipPos(vertex);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 p = i.wpos;
                float t = _ArrTime;

                // ---- breath: a soft tube of clearer air along each puff, the mist texture pushed ahead of it
                float clear = 0, maxC = 0;
                float2 disp = 0;
                [loop] for (int k = 0; k < 20; k++)
                {
                    float C = _PuffLength[k].y;
                    if (C < 1e-3) continue;
                    maxC = max(maxC, C);
                    float3 ax = float3(_PuffAxis[k].x, 0, _PuffAxis[k].y);
                    float3 rel = p - _PuffOrigin[k].xyz;
                    float s = dot(rel, ax);
                    float3 radial = rel - ax * s;
                    float rho = length(radial);
                    float w = _PuffAxis[k].w, L = _PuffLength[k].x, sc = _PuffOrigin[k].w;
                    // a jet is narrow at the mouth and widens downstream (half-width grows ~linearly with distance)
                    float wj = min(3.0 + 2.6 * w, 1.1 + 0.36 * max(s, 0.0));
                    // the stream starts just ahead of the bow: nothing is blown backwards
                    float start = smoothstep(2.0, 7.0, s);
                    float tube = exp(-rho * rho / (wj * wj)) * start * (1.0 - smoothstep(sc, sc + L * 1.3, s));
                    // advection: mist in the stream is carried forward by how far the air has travelled past it,
                    // so it stretches into streaks along the flow; it eases back as the stream fades (C -> 0)
                    float carried = clamp(sc - s, 0.0, 10.0) * 0.6;
                    clear = max(clear, C * tube);
                    disp += ax.xz * (tube * carried * C);
                }
                // the stream only ever acts in front of the boat's current bow: a forward breeze, not a ring
                float ahead = smoothstep(1.5, 7.0, dot(p.xz - _ArrBow.xz, _ArrBowDir.xz));
                clear *= ahead;
                disp *= ahead;
                // the mist is a little thinner right around the boat (never cleared): _ArrBow.w = how much thinner
                float db = distance(p.xz, _ArrBow.xz - _ArrBowDir.xz * 2.4);
                float sphere = _ArrBow.w * (1.0 - smoothstep(3.0, 9.0, db));

                // ---- drifting density from two noise octaves (the breath drags the texture)
                float2 uv1 = (p.xz - disp) * 0.021 + _MistWind.xy * t * 0.021 + _Layer * 0.37;
                float2 uv2 = (p.xz - disp * 1.4) * 0.058 + _MistWind.zw * t * 0.058 + _Layer * 0.71;
                float n = tex2D(_MistNoise, uv1).r * 0.65 + tex2D(_MistNoise, uv2).r * 0.35;
                // cloud-sea look: puffy shapes with fairly crisp outlines (soft enough to never look cut out)
                float dens = lerp(smoothstep(0.34, 0.60, n), smoothstep(0.43, 0.53, n), 0.55);
                // rim light: the side of each puff facing the moon catches light
                float2 toMoon = normalize(_ArrSunDir.xz + 1e-4);
                float nOff = tex2D(_MistNoise, uv1 - toMoon * 0.05).r * 0.65 + tex2D(_MistNoise, uv2 - toMoon * 0.14).r * 0.35;
                float rim = saturate((n - nOff) * 7.0) * smoothstep(0.38, 0.5, n);

                // ---- local fog volumes and clear pockets
                [unroll] for (int a = 0; a < 4; a++)
                {
                    float dd = distance(p.xz, _ArrDenseSpots[a].xz);
                    dens *= 1.0 + _ArrDenseSpots[a].w * exp(-dd * dd / 420.0);
                }
                float pockets = 1;
                [unroll] for (int b = 0; b < 5; b++)
                {
                    float r = _ArrClearSpots[b].w;
                    if (r < 0.01) continue;
                    pockets *= smoothstep(r * 0.55, r * 1.25 + 3.0, distance(p.xz, _ArrClearSpots[b].xz));
                }

                // never right in the camera's face, and fade at the edge of the slice
                float camD = distance(p, _WorldSpaceCameraPos);
                float nearFade = smoothstep(3.0, 9.0, camD) * smoothstep(0.6, 2.2, abs(p.y - _WorldSpaceCameraPos.y));
                float farFade = 1.0 - smoothstep(170.0, 240.0, length(p.xz - _WorldSpaceCameraPos.xz));

                float alpha = dens * _LayerWeight * _MistDensity * (1.0 - clear) * (1.0 - sphere) * pockets * nearFade * farFade;

                // ---- colour: moonlit lavender, lit gold where the lighthouse beam passes through
                // slightly posterised tones, like flat illustrated clouds
                float tone = lerp(n, floor(n * 5.0) / 5.0, 0.45);
                float3 col = lerp(_MistColorLow.rgb, _MistColorHigh.rgb, saturate((tone - 0.25) * 1.6));
                col += float3(0.35, 0.40, 0.60) * rim * 0.35;
                float3 v = p - _ArrLamp.xyz;
                float along = dot(v, _ArrBeam3D.xyz);
                float perp = length(v - _ArrBeam3D.xyz * along);
                float width = 1.4 + max(along, 0) * 0.045;
                float beam = exp(-(perp * perp) / (width * width)) * step(0, along) * saturate(1.0 - along / _ArrBeam3D.w);
                float lamp = exp(-length(v) / 20.0);
                float bow = exp(-db / 5.0) * 0.35;
                // the golden light path on the sea glows up through the mist, so the way stays visible
                float2 rel2 = p.xz - _ArrBeamOrigin.xz;
                float pAlong = dot(rel2, _ArrBeamDir.xz);
                float pAcross = abs(rel2.x * _ArrBeamDir.z - rel2.y * _ArrBeamDir.x);
                float pWidth = 3.0 + max(pAlong, 0) * 0.06;
                float path = exp(-pow(pAcross / pWidth, 2)) * smoothstep(8, 30, pAlong) * saturate(1 - pAlong / _ArrBeamDir.w);
                float lit = beam * 0.6 + lamp * 0.5 + bow + path * 0.45;
                col += float3(1.0, 0.80, 0.48) * lit * _ArrLamp.w;
                col += float3(1.0, 0.78, 0.40) * rim * saturate(lit * 2.0) * 0.6;   // golden rims where the light falls
                // the glowing sea patch at the light's surprise lights the mist above it from below
                // Measured along the line of sight (like light scattered in fog), not only right above the patch:
                // the higher slices that the eye crosses on the way there get the halo too, so it isn't hidden.
                float3 toT = _ArrTreasure.xyz + float3(0, 0.5, 0) - _WorldSpaceCameraPos;
                float3 toP = p - _WorldSpaceCameraPos;
                float dT = length(toT), dP = length(toP);
                float ang2 = 2.0 * (1.0 - dot(toT / dT, toP / max(dP, 0.01)));   // ~ squared angle from the patch, as seen
                float sig = clamp(15.0 / dT, 0.09, 0.26);                           // ~15 m across there, 5°–15° on screen
                float tglow = exp(-ang2 / (sig * sig)) * exp(-max(dP - dT, 0) / 25.0) * _ArrTreasure.w;
                alpha *= 1.0 + beam * 0.25 + tglow * 0.3;

                fixed4 c = fixed4(col, saturate(alpha));
                UNITY_APPLY_FOG(i.fogCoord, c);
                // after the fog, and tinted rather than only added (the mist is already pale), so the warm halo
                // above the hidden friend stays clearly visible even in the thickest mist
                // a saturated warm gold (the pale mist would wash a lighter tint out to white), hinting the friend's colour
                float3 tcol = lerp(float3(1.0, 0.70, 0.28), _ArrTreasureColor.rgb, 0.25);
                c.rgb = lerp(c.rgb, tcol * 1.2, saturate(tglow * 0.9)) + tcol * tglow * 0.25;
                return c;
            }
            ENDCG
        }
    }
}
