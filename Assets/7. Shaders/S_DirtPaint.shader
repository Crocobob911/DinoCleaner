// 메시를 UV 공간으로 펼쳐서 오물 마스크를 지우는 셰이더.
// 카메라가 아니라 DirtPainter가 직접 호출한다.
Shader "Hidden/DinoWash/S_DirtPaint"
{
    // CGINCLUDE 안의 코드는 아래 모든 Pass에 자동으로 붙는다 (공용 코드)
    CGINCLUDE
    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv     : TEXCOORD0;
    };

    struct v2f
    {
        float4 pos      : SV_POSITION;
        float2 uv       : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
    };

    // 메시를 UV 위치로 펼친다 (두 Pass 공용)
    v2f vert(appdata v)
    {
        v2f o;
        o.pos = float4(v.uv * 2 - 1, 0.5, 1);
        #if UNITY_UV_STARTS_AT_TOP
        o.pos.y = -o.pos.y;
        #endif
        o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.uv = v.uv;
        return o;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Paint — 브러시 주변 진흙 지우기
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragPaint

            sampler2D _SourceMask;
            float4 _DW_BrushPos;
            float  _DW_BrushAmount;

            float4 fragPaint(v2f i) : SV_Target
            {
                float4 mask = tex2D(_SourceMask, i.uv);
                float dist = distance(i.worldPos, _DW_BrushPos.xyz);
                float falloff = 1 - smoothstep(0, _DW_BrushPos.w, dist);
                mask.r = saturate(mask.r - _DW_BrushAmount * falloff);
                return mask;
            }
            ENDCG
        }

        // Pass 1: Init — 메시가 덮는 곳만 진흙으로 채우기
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragInit

            float4 fragInit(v2f i) : SV_Target
            {
                return float4(1, 0, 0, 0);   // R(진흙) = 1, 나머지 0
            }
            ENDCG
        }
    }
}