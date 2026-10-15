using DinoCleaner.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace DinoCleaner.Cleaning
{
    public class CleanableObject : MonoBehaviour, ICleanable, ICleanProgress
    {
        [SerializeField] int maskResolution = 1024;
            
        [Tooltip("셰이더 그래프에서 마스크를 받는 텍스처 프로퍼티의 Reference 이름")]
        [SerializeField] string maskProperty = "_DirtMask";

        [Tooltip("Shaders/DirtPaint.shader 연결")]
        [SerializeField] Shader paintShader;
        [Tooltip("Shaders/ProgressTracker.shader 연결")]
        [SerializeField] Shader progressPackShader;

        [Header("부위")]
        [Tooltip("UV 공간 부위 맵. R값(0~255) = 부위 ID")]
        [SerializeField] Texture2D regionMap;
        [SerializeField] RegionDefinition[] regions;
        [Tooltip("부위의 이 비율 이상이 깨끗해지면 그 부위 완료")]
        [SerializeField, Range(0f, 1f)] float regionCompleteThreshold = 0.9f;

        [Header("진행도 측정")]
        [Tooltip("진흙 값이 이 이하로 내려가야 깨끗한 픽셀로 센다")]
        [SerializeField, Range(0f, 1f)] float cleanThreshold = 0.1f;
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
        public IReadOnlyList<RegionProgress> Regions => _tracker != null ? _tracker.Regions : Array.Empty<RegionProgress>();
        public event Action<ICleanProgress> Changed;

        void Awake()
        {
            if (paintShader == null || progressPackShader == null)
            {
                Debug.LogError($"[{name}]에 PaintShader/ProgressPackSHader연결 필요.", this);
                enabled = false;
                return;
            }

            _renderers = GetComponentsInChildren<Renderer>();

            _mask = new DirtMask(maskResolution);
            
            // DirtPainter 초기화
            _painter = new DirtPainter(_mask, _renderers, paintShader);
            _painter.Init();

            ValidateRegionMap();
            // ProgressTracker 초기화, event설정
            _tracker = new CleanProgressTracker(progressResolution, cleanThreshold,
                                                progressPackShader, regionMap,
                                                regions, regionCompleteThreshold);
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

        // === ICleanable ===
        public void ApplyStroke(CleanStroke stroke)
        {
            if (_painter == null) return;

            // DEBUG: 1초에 한 번쯤 스트로크 값 출력
            if (Time.frameCount % 60 == 0)
                Debug.Log($"[{name}] Stroke: 위치 {stroke.Position}, 반경 {stroke.Radius}, 세기 {stroke.Strength}, dt {stroke.DeltaTime:F4}");

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

        // 부위 맵 임포트 설정 실수를 시작할 때 잡아낸다
        void ValidateRegionMap()
        {
            if (regions != null && regions.Length > 0 && regionMap == null)
            {
                Debug.LogWarning($"[{name}] Regions는 있는데 Region Map이 비어 있습니다.", this);
                return;
            }
            if (regionMap == null) return;

            Debug.Log($"[{name}] 부위 맵 '{regionMap.name}' 형식 = {regionMap.graphicsFormat}, " +
              $"필터 = {regionMap.filterMode}, 크기 = {regionMap.width}x{regionMap.height}", regionMap);

            var format = regionMap.graphicsFormat;
            if (GraphicsFormatUtility.IsSRGBFormat(format))
                Debug.LogWarning($"[{name}] 부위 맵 '{regionMap.name}'의 sRGB가 켜져 있습니다. 임포트 설정에서 끄고 Apply 하세요.", regionMap);
            if (GraphicsFormatUtility.IsCompressedFormat(format))
                Debug.LogWarning($"[{name}] 부위 맵 '{regionMap.name}'이 압축돼 있습니다 ({format}). Compression을 None으로 하고 Apply 하세요.", regionMap);
            if (regionMap.filterMode != FilterMode.Point)
                Debug.LogWarning($"[{name}] 부위 맵 '{regionMap.name}'의 Filter Mode가 Point가 아닙니다. 부위 경계에서 번호가 섞일 수 있습니다.", regionMap);
        }
    }
}