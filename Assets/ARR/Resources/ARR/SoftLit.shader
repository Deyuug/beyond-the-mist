// Simple wrap-lit surface (boat, islands, lighthouses) using the global ARR sun.
Shader "ARR/SoftLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Emission ("Emission", Color) = (0,0,0,0)
        _Rim ("Rim", Float) = 0.25
        _Flat ("Flat shading", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float4 _Color, _Emission; float _Rim, _Flat;
            float4 _ArrSunDir, _ArrSunColor, _ArrHorizon, _ArrZenith;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 wpos : TEXCOORD1; UNITY_FOG_COORDS(2) };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 fn = normalize(cross(ddy(i.wpos), ddx(i.wpos)));
                if (dot(fn, n) < 0) fn = -fn;
                n = normalize(lerp(n, fn, _Flat));
                float3 l = normalize(_ArrSunDir.xyz);
                float wrap = saturate(dot(n, l) * 0.5 + 0.5);
                float3 amb = lerp(_ArrHorizon.rgb * 0.55, _ArrZenith.rgb * 0.7, n.y * 0.5 + 0.5);
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);
                float rim = pow(1 - saturate(dot(n, v)), 3) * _Rim;
                float3 albedo = _Color.rgb;
                float3 col = albedo * (amb * 1.6 + _ArrSunColor.rgb * wrap * 0.55) + _ArrHorizon.rgb * rim * 1.5 + _Emission.rgb;
                fixed4 c = fixed4(col, 1);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
