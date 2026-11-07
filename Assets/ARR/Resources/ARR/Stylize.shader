// Picture-book finish for the child's view (image effect on the child camera):
//  - split-tone grade: shadows pushed toward deep navy, highlights toward warm gold
//  - printed grain: fine stipple that changes ~12 times a second, strongest in the mid-tones
//  - soft vignette
Shader "ARR/Stylize"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Grain, _Vignette, _GrainTime;
            sampler2D _BlurTex;
            float _Haze;            // 0 clear .. 1 thick haze (lowered by each breath)
            float4 _VeilColor;
            float4 _ShadowTint, _HighTint;

            // stable hash on small integer coordinates (no float-precision streaks)
            float hash(float2 p) { p = fmod(p, 289.0); return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            fixed4 frag (v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                // mist haze over the whole view: softened and veiled until a breath clears the air
                float3 blurred = tex2D(_BlurTex, i.uv).rgb;
                c = lerp(c, blurred, saturate(_Haze * 0.95));
                c = lerp(c, _VeilColor.rgb, saturate(_Haze * 0.3));
                float l = dot(c, float3(0.299, 0.587, 0.114));

                // split toning
                c += _ShadowTint.rgb * pow(1 - l, 3) * _ShadowTint.a;
                c += _HighTint.rgb * pow(l, 2) * _HighTint.a;
                // gentle contrast and saturation, like flat printed colour
                c = saturate((c - 0.5) * 1.08 + 0.5);
                float l2 = dot(c, float3(0.299, 0.587, 0.114));
                c = lerp(l2.xxx, c, 1.12);

                // printed grain (pixel-sized stipple; animated at a slow, papery rate)
                float2 px = floor(i.uv * _MainTex_TexelSize.zw);
                float g = hash(px + float2(fmod(_GrainTime * 37.0, 97.0), fmod(_GrainTime * 53.0, 89.0))) - 0.5;
                float mid = 1.0 - abs(l2 * 2.0 - 1.0);
                c += g * _Grain * (0.35 + 0.65 * mid);

                // vignette
                float2 d = i.uv - 0.5;
                c *= 1.0 - _Vignette * smoothstep(0.25, 0.85, dot(d, d) * 2.2);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
