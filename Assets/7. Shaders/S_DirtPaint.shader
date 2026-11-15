// 메시를 UV 공간으로 펼쳐서 오물 마스크를 지우는 셰이더.
// 카메라가 아니라 DirtPainter가 직접 호출한다.
Shader "Hidden/DinoWash/S_DirtPaint"
{
    // CGINCLUDE 안의 코드는 아래 모든 Pass에 자동으로 붙는다 (공용 코드)
    CGINCLUDE
    #include "UnityCG.cginc"

    // === 여러 Pass가 같이 쓰는 값 ===
    sampler2D _SourceMask;    // 현재 마스크의 복사본 (읽기용)
    float4 _DW_BrushPos;      // xyz = 브러시 중심(월드), w = 반경
    float  _DW_BrushAmount;   // 이번 프레임에 지울 양 (Strength × dt)
    float  _DW_BrushHardness; // 0 = 가운데만 세게, 0.85 = 반경 85%까지 꽉 차게
    float  _DW_CleanSnap;     // 이 값 아래로 내려가면 0으로 snap (덜 씻긴 자국 방지)

    // 이 지점이 브러시 안에서 얼마나 세게 영향받는지 (1 = 최대, 0 = 밖)
    float BrushFalloff(float3 worldPos)
    {
        float r = _DW_BrushPos.w;
        float dist = distance(worldPos, _DW_BrushPos.xyz);
        return 1 - smoothstep(r * _DW_BrushHardness, r, dist);
    }

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

    // 메시를 UV 위치로 펼친다 (모든 Pass 공용)
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

        // Pass 0: Paint - 브러시 영역의 진흙 지우기
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragPaint

            float4 fragPaint(v2f i) : SV_Target
            {
                float4 mask = tex2D(_SourceMask, i.uv);
                float falloff = BrushFalloff(i.worldPos);

                mask.r = saturate(mask.r - _DW_BrushAmount * falloff);

                if (falloff > 0 && mask.r < _DW_CleanSnap)
                    mask.r = 0;    // 덜 씻긴 자국 방지

                return mask;
            }
            ENDCG
        }

        // Pass 1: Init - 메시가 덮는 곳만 진흙으로 채우기
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

        // Pass 2: AddDirt - 물총과 반대로 채널 값을 올린다
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragSetDirt

            float4 _DW_DirtType;   // 어떤 오물인지 (진흙 = (1,0,0), 이끼 = (0,1,0), 배설물 = (0,0,1))

            float4 fragSetDirt(v2f i) : SV_Target
            {
                float4 mask = tex2D(_SourceMask, i.uv);
                float dist = distance(i.worldPos, _DW_BrushPos.xyz);

                if (dist > _DW_BrushPos.w) return mask;        // 원 밖은 그대로
                return float4(_DW_DirtType.rgb, mask.a);       // (A는 비누용으로 보존)
            }
            ENDCG
        }
    }
}