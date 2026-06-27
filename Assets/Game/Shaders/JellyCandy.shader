Shader "Jelly/Candy"
{
    // Unlit "fake-lit" candy look: màu rực (không bị môi trường làm xám), bóng top-sáng/bottom-tối
    // + highlight specular. Render được trên SkinnedMeshRenderer (pass SRPDefaultUnlit dưới URP).
    Properties
    {
        _BaseColor      ("Base Color", Color)        = (1,1,1,1)
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.42
        _Brightness     ("Brightness", Range(0.5,2))    = 1.18
        _SpecPower      ("Spec Power", Range(1,128))     = 36
        _SpecStrength   ("Spec Strength", Range(0,2))    = 0.85
        _RimStrength    ("Rim Strength", Range(0,1))     = 0.12
        _Saturation     ("Saturation", Range(1,2))       = 1.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 pos  : SV_POSITION;
                float3 wN   : TEXCOORD0;
                float3 vDir : TEXCOORD1;
            };

            fixed4 _BaseColor;
            float  _ShadowStrength, _Brightness, _SpecPower, _SpecStrength, _RimStrength, _Saturation;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wN  = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vDir = _WorldSpaceCameraPos - wp;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 N = normalize(i.wN);
                float3 V = normalize(i.vDir);
                float3 L = normalize(float3(-0.25, 0.92, -0.45)); // ánh sáng từ trên xuống

                // Half-lambert: top sáng, bottom tối nhẹ → khối đọc 3D mà màu vẫn rực.
                float ndl   = dot(N, L) * 0.5 + 0.5;
                float shade = lerp(1.0 - _ShadowStrength, 1.0, ndl);

                // Specular highlight (Blinn-Phong) — vệt bóng trắng trên đỉnh khối.
                float3 H    = normalize(L + V);
                float  spec = pow(saturate(dot(N, H)), _SpecPower) * _SpecStrength;

                // Rim sáng nhẹ ở mép cho cảm giác kẹo dẻo.
                float  rim  = pow(1.0 - saturate(dot(N, V)), 3.0) * _RimStrength;

                // Tăng saturation cho màu rực giống bản gốc (TCP2).
                float3 baseCol = _BaseColor.rgb;
                float  lum = dot(baseCol, float3(0.299, 0.587, 0.114));
                baseCol = lerp(lum.xxx, baseCol, _Saturation);

                float3 col = baseCol * shade * _Brightness + spec + rim;
                return fixed4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
