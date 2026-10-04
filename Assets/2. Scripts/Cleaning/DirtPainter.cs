using System;
using UnityEngine;
using UnityEngine.Rendering;
using DinoCleaner.Core;

namespace DinoCleaner.Cleaning
{
    internal class DirtPainter : IDisposable
    {
        static readonly int SourceMaskId = Shader.PropertyToID("_SourceMask");
        static readonly int BrushPosId = Shader.PropertyToID("_DW_BrushPos");
        static readonly int BrushAmountId = Shader.PropertyToID("_DW_BrushAmount");

        readonly DirtMask _mask;
        readonly Renderer[] _renderers;
        readonly Material _material;
        readonly RenderTexture _source;   // 셰이더가 읽을 복사본
        readonly CommandBuffer _cmd;

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

        public void Paint(in CleanStroke stroke)
        {
            _cmd.Clear();

            // 1.현재 마스크를 복사본에 복사
            _cmd.CopyTexture(_mask.Texture, _source);

            // 2.브러시 정보를 셰이더에 전달
            var p = stroke.Position;
            _cmd.SetGlobalVector(BrushPosId, new Vector4(p.x, p.y, p.z, stroke.Radius));
            _cmd.SetGlobalFloat(BrushAmountId, stroke.Strength * stroke.DeltaTime);

            // 3.그릴 곳 = 원본 마스크
            _cmd.SetRenderTarget(_mask.Texture);

            // 4.메시들을 언랩해서 그린다
            foreach (var r in _renderers)
            {
                // 머티리얼 슬롯 하나 = 서브메시 하나
                for (int sub = 0; sub < r.sharedMaterials.Length; sub++)
                    _cmd.DrawRenderer(r, _material, sub, 0);
            }

            // 5. 쌓아둔 명령을 GPU에 한 번에 실행
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
