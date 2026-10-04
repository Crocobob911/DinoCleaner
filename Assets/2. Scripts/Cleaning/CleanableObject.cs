using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Cleaning
{
    public class CleanableObject : MonoBehaviour, ICleanable
    {
        [SerializeField] int maskResolution = 1024;

        [Tooltip("셰이더 그래프에서 마스크를 받는 텍스처 프로퍼티의 Reference 이름")]
        [SerializeField] string maskProperty = "_DirtMask";

        DirtMask _mask;

        void Awake()
        {
            _mask = new DirtMask(maskResolution);
            _mask.Fill(new Color(1, 0, 0, 0));   // 처음엔 진흙으로 칠함
            BindMaskToRenderers();
        }

        // 이 오브젝트의 렌더러들에게 "너의 _DirtMask는 이 텍스처야"라고 알려준다
        void BindMaskToRenderers()
        {
            var block = new MaterialPropertyBlock();
            int id = Shader.PropertyToID(maskProperty);

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(block);
                block.SetTexture(id, _mask.Texture);
                r.SetPropertyBlock(block);
            }
        }

        public void ApplyStroke(CleanStroke stroke)
        {
            // 아직 미구현
        }

        // Debug용(Play 중 컴포넌트)
        [ContextMenu("Test/더럽게 채우기")]
        void TestFillDirty() => _mask?.Fill(new Color(1, 0, 0, 0));

        [ContextMenu("Test/깨끗하게 비우기")]
        void TestClear() => _mask?.Fill(Color.clear);

        void OnDestroy() => _mask?.Dispose();
    }
}