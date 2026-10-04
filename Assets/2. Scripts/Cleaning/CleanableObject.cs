using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Cleaning
{
    public class CleanableObject : MonoBehaviour, ICleanable
    {
        [SerializeField] int maskResolution = 1024;

        [Tooltip("셰이더 그래프에서 마스크를 받는 텍스처 프로퍼티의 Reference 이름")]
        [SerializeField] string maskProperty = "_DirtMask";

        [Tooltip("Shaders/DirtPaint.shader를 연결")]
        [SerializeField] Shader paintShader;

        Renderer[] _renderers;
        DirtMask _mask;
        DirtPainter _painter;

        void Awake()
        {
            if (paintShader == null)
            {
                Debug.LogError($"[{name}]에 PaintShader연결 필요.", this);
                enabled = false;
                return;
            }

            _mask = new DirtMask(maskResolution);
            _mask.Fill(new Color(1, 0, 0, 0));   // 처음엔 진흙으로 칠함

            _renderers = GetComponentsInChildren<Renderer>();
            _painter = new DirtPainter(_mask, _renderers, paintShader);

            BindMaskToRenderers();
        }

        // 이 오브젝트의 렌더러들에게 "너의 _DirtMask는 이 텍스처야"라고 알려준다
        void BindMaskToRenderers()
        {
            var block = new MaterialPropertyBlock();
            int id = Shader.PropertyToID(maskProperty);

            foreach (var r in _renderers)
            {
                r.GetPropertyBlock(block);
                block.SetTexture(id, _mask.Texture);
                r.SetPropertyBlock(block);
            }
        }

        public void ApplyStroke(CleanStroke stroke)
        {
            _painter?.Paint(stroke);
        }

        // Debug용(Play 중 컴포넌트에서 실행가능)
        [ContextMenu("Test/표면 더럽히기")]
        void TestFillDirty() => _mask?.Fill(new Color(1, 0, 0, 0));

        [ContextMenu("Test/표면 Clean")]
        void TestClear() => _mask?.Fill(Color.clear);

        void OnDestroy()
        {
            _painter?.Dispose();
            _mask?.Dispose();
        }
    }
}