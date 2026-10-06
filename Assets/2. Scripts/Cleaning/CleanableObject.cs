using DinoCleaner.Core;
using DinoWash.Core;
using System;
using UnityEngine;

namespace DinoCleaner.Cleaning
{
    public class CleanableObject : MonoBehaviour, ICleanable, ICleanProgress
    {
        [SerializeField] int maskResolution = 1024;

        [Tooltip("셰이더 그래프에서 마스크를 받는 텍스처 프로퍼티의 Reference 이름")]
        [SerializeField] string maskProperty = "_DirtMask";

        [Tooltip("Shaders/DirtPaint.shader를 연결")]
        [SerializeField] Shader paintShader;

        [Header("진행도 측정")]
        [Tooltip("진행도 계산용 압축 해상도")]
        [SerializeField] int progressResolution = 256;
        [Tooltip("진행도 갱신 간격(s)")]
        [SerializeField] float progressInterval = 0.25f;

        Renderer[] _renderers;
        DirtMask _mask;
        DirtPainter _painter;
        CleanProgressTracker _tracker;

        bool _maskChanged;        // 마지막 측정 이후 마스크가 바뀌었는지
        float _nextMeasureTime;   // 다음 측정 가능 시각

        // === ICleanProgress ===
        public bool IsReady => _tracker != null && _tracker.IsReady;
        public float TotalProgress => _tracker != null ? _tracker.Progress : 0f;
        public event Action<ICleanProgress> Changed;

        void Awake()
        {
            if (paintShader == null)
            {
                Debug.LogError($"[{name}]에 PaintShader연결 필요.", this);
                enabled = false;
                return;
            }

            _renderers = GetComponentsInChildren<Renderer>();

            _mask = new DirtMask(maskResolution);
            
            // DirtPainter 초기화
            _painter = new DirtPainter(_mask, _renderers, paintShader);
            _painter.Init();
            // ProgressTracker 초기화, event설정
            _tracker = new CleanProgressTracker(progressResolution);
            _tracker.Updated += () => Changed?.Invoke(this);

            BindMaskToRenderers();
            _tracker.Request(_mask.Texture); // 첫 진행도 요청
        }

        void Update()
        {
            // 주기적으로 진행도 요청(변화X/계산중인 경우 제외)
            if (!_maskChanged || _tracker.IsPending || Time.time < _nextMeasureTime) return;
            _tracker.Request(_mask.Texture);
            _maskChanged = false;
            _nextMeasureTime = Time.time + progressInterval;
        }


        // 이 오브젝트의 렌더러들에게 셰이더(SG_DirtyLit)의_DirtMask의 프로퍼티가 뭘 가리킬지 연결해준다.
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
            if (_painter == null) return;
            _painter.Paint(stroke);
            _maskChanged = true;
        }

        //============================디버그용 기능============================
        // Debug용(Play 중 컴포넌트에서 실행가능)
        [ContextMenu("Test/표면 더럽히기")]
        void TestFillDirty()
        {
            _painter?.Init();
            _maskChanged = true;
        }

        [ContextMenu("Test/표면 Clean")]
        void TestClear()
        {
            _mask?.Fill(Color.clear);
            _maskChanged = true;
        }

        void OnDestroy()
        {
            _tracker?.Dispose();
            _painter?.Dispose();
            _mask?.Dispose();
        }
    }
}