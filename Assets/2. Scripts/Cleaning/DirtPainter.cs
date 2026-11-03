using System;
using UnityEngine;
using UnityEngine.Rendering;
using DinoCleaner.Core;
using UnityEngine.UIElements;

namespace DinoCleaner.Cleaning
{
    internal class DirtPainter : IDisposable
    {
        public float BrushHardness { get; set; } = 0.85f;
        public float CleanSnap { get; set; } = 0.2f;

        static readonly int SourceMaskId = Shader.PropertyToID("_SourceMask");
        static readonly int BrushPosId = Shader.PropertyToID("_DW_BrushPos");
        static readonly int BrushAmountId = Shader.PropertyToID("_DW_BrushAmount");
        static readonly int BrushHardnessId = Shader.PropertyToID("_DW_BrushHardness");
        static readonly int CleanSnapId = Shader.PropertyToID("_DW_CleanSnap");
        static readonly int DirtTypeId = Shader.PropertyToID("_DW_DirtType");

        readonly DirtMask _mask;
        readonly Renderer[] _renderers;
        readonly Material _material;
        readonly RenderTexture _source;   // 셰이더가 읽을 복사본
        readonly CommandBuffer _cmd;

        // Pass 번호 정의 - S_DirtPaint.shader에서 Pass 순서와 일치해야 함
        const int PaintPass = 0; // 브러시 주변 진흙 지우는 Pass
        const int InitPass = 1; // 메시 영역만 진흙으로 칠하는 Pass
        const int SetDirtPass = 2; // 메시 영역만 진흙으로 칠하는 Pass

        public DirtPainter(DirtMask mask, Renderer[] renderers, Shader paintShader)
        {
            _mask = mask;
            _renderers = renderers;

            _material = new Material(paintShader);

            // 원본과 크기, 형식이 똑같은 복사본 텍스처
            _source = new RenderTexture(mask.Texture.descriptor) { name = "DirtMaskSource" };
            _source.Create();
            _material.SetTexture(SourceMaskId, _source);

            _cmd = new CommandBuffer { name = "S_DirtPaint" };
        }

        public void Init()
        {
            _cmd.Clear();
            _cmd.SetRenderTarget(_mask.Texture);
            _cmd.ClearRenderTarget(false, true, Color.clear);  // 전부 0(깨끗)
            DrawRenderers(InitPass);                           // 메시 영역만 진흙
            Graphics.ExecuteCommandBuffer(_cmd);
        }

        // renderer마다 그리는 명령 전달
        void DrawRenderers(int pass)
        {
            foreach (var r in _renderers)
            {
                for (int sub = 0; sub < r.sharedMaterials.Length; sub++)
                    _cmd.DrawRenderer(r, _material, sub, pass);
            }
        }

        public void Paint(CleanStroke stroke)
        {
            _cmd.Clear();

            // 1.현재 마스크를 복사본에 복사
            _cmd.CopyTexture(_mask.Texture, _source);

            // 2.브러시 정보를 셰이더에 전달
            var p = stroke.Position;
            _cmd.SetGlobalVector(BrushPosId, new Vector4(p.x, p.y, p.z, stroke.Radius));
            _cmd.SetGlobalFloat(BrushAmountId, stroke.Strength * stroke.DeltaTime);
            _cmd.SetGlobalFloat(BrushHardnessId, Mathf.Clamp(BrushHardness, 0f, 0.95f));   // 1이면 smoothstep이 0으로 나눔
            _cmd.SetGlobalFloat(CleanSnapId, CleanSnap);

            // 3.그릴 곳 = 원본 마스크
            _cmd.SetRenderTarget(_mask.Texture);

            // 4. shader의 PaintPass부분 실행명령
            DrawRenderers(PaintPass);
            
            // 5. 쌓아둔 명령을 GPU에 한 번에 실행
            Graphics.ExecuteCommandBuffer(_cmd);
        }

        // 지정한 원 안을 오물 한 종류로 덮어쓴다.
        // dirtType = 어떤 오물인지 (R 진흙, G 이끼·화산재, B 배설물 중 하나만 1)
        public void SetDirt(Vector3 pos, float radius, Vector4 dirtType)
        {
            _cmd.Clear();

            // 1. 마스크 복사본에 복사
            _cmd.CopyTexture(_mask.Texture, _source);

            // 2. 브러시 정보 전달
            _cmd.SetGlobalVector(BrushPosId, new Vector4(pos.x, pos.y, pos.z, radius));
            _cmd.SetGlobalVector(DirtTypeId, dirtType);
            _cmd.SetRenderTarget(_mask.Texture);

            // 3. Pass3 실행
            _cmd.SetRenderTarget(_mask.Texture);
            DrawRenderers(SetDirtPass);
            Graphics.ExecuteCommandBuffer(_cmd);
        }

        public void Dispose()
        {
            _cmd?.Release();
            if (_source != null)
            {
                _source.Release();
                UnityEngine.Object.Destroy(_source);
            }
            if (_material != null) UnityEngine.Object.Destroy(_material);
        }
    }
}
