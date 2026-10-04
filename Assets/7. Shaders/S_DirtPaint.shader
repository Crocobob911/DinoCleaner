// 메시를 UV 공간으로 펼쳐서 오물 마스크를 지우는 셰이더.
// 카메라가 아니라 DirtPainter가 직접 호출한다.
Shader "Hidden/DinoWash/S_DirtPaint"
{
    SubShader
    {
        // 앞뒷면, 깊이 상관없이 전부 그린다
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _SourceMask;     // 칠하기 직전 마스크의 복사본
            float4 _DW_BrushPos;       // xyz = 브러시 중심(월드 좌표), w = 반경
            float  _DW_BrushAmount;    // 이번 스트로크에 지울 양

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

            v2f vert(appdata v)
            {
                v2f o;

                // ① 화면 위치 대신 UV 위치에 그린다 (메시를 전개도로 펼침)
                //    UV(0~1)를 GPU 화면 좌표(-1~1)로 변환
                o.pos = float4(v.uv * 2 - 1, 0.5, 1);
                #if UNITY_UV_STARTS_AT_TOP
                o.pos.y = -o.pos.y;   // DirectX 계열은 위아래가 반대라서 뒤집어줌
                #endif

                // ② 원래 월드 위치는 따로 기억해둔다
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            // 마스크의 픽셀 하나하나마다 실행된다
            float4 frag(v2f i) : SV_Target
            {
                float4 mask = tex2D(_SourceMask, i.uv);

                // 이 픽셀이 브러시 중심에서 얼마나 먼가
                float dist = distance(i.worldPos, _DW_BrushPos.xyz);

                // 중심은 1, 반경 끝은 0으로 부드럽게 줄어드는 값
                float falloff = 1 - smoothstep(0, _DW_BrushPos.w, dist);

                // 진흙(R)만 줄인다. saturate = 0~1 범위로 자르기
                mask.r = saturate(mask.r - _DW_BrushAmount * falloff);
                return mask;
            }
            ENDCG
        }
    }
}