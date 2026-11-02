// 진행도 측정용: 마스크를 줄이면서 한 픽셀에 (R = 오물량, G = 부위 번호)를 담는다.
Shader "Hidden/DinoWash/ProgressPack"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MaskTex;     // 오물 마스크 (직접 넣어주는 것으로 변경)
            sampler2D _RegionMap;   // 부위 맵

            float4 frag(v2f_img i) : SV_Target
            {
                float4 mask = tex2D(_MaskTex, i.uv);
                float dirt = max(mask.r, max(mask.g, mask.b));   // 비누(A)는 오물이 아님
                float region = tex2D(_RegionMap, i.uv).r;
                return float4(dirt, region, 0, 1);
            }
            ENDCG
        }
    }
}