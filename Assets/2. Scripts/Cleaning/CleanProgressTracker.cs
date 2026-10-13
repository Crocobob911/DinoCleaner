using DinoCleaner.Core;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DinoCleaner.Cleaning
{
    internal class CleanProgressTracker : IDisposable
    {
        static readonly int RegionMapId = Shader.PropertyToID("_RegionMap");

        RenderTexture _small;   // CPU 전달용으로 축소한 마스크
        readonly Material _packMaterial;     // ProgressPack 셰이더
        readonly byte _cleanThreshold;  // 오염이 값 이하면 깨끗한 픽셀
        readonly float _regionCompleteThreshold; 


        readonly RegionDefinition[] _regions;
        readonly int[] _indexById = new int[256];   // 부위 ID(0~255) → _regions 배열 칸 번호 (-1 = 없음)
        readonly int[] _initialDirtyPerRegion;      // 부위별 처음 더러운 픽셀 수
        readonly int[] _dirtyPerRegion;             // 부위별 지금 더러운 픽셀 수
        readonly RegionProgress[] _regionProgress;  // 밖에 보여줄 결과

        int _initialDirtyPixels = -1; // 처음 더러운 부분 총량 (-1 = 아직 모름)
        bool _disposed;

        public bool IsPending { get; private set; }   // 요청 보내고 결과 기다리는 중
        public bool IsReady => _initialDirtyPixels >= 0;
        public float Progress { get; private set; }
        public IReadOnlyList<RegionProgress> Regions => _regionProgress;

        public event Action Updated;

        public CleanProgressTracker(int resolution, float cleanThreshold,
            Shader packShader, Texture regionMap, 
            RegionDefinition[] regions, float regionCompleteThreshold)
        {
            _cleanThreshold = (byte)Mathf.RoundToInt(Mathf.Clamp01(cleanThreshold) * 255f);
            _regionCompleteThreshold = regionCompleteThreshold;

            _small = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "DirtMaskSmall",
                filterMode = FilterMode.Point,
            };
            _small.Create();

            _packMaterial = new Material(packShader);

            //DEBUG
            Debug.Log($"[Cleaning] Pack 셰이더 = {packShader.name}, 사용 가능 = {packShader.isSupported}");

            _packMaterial.SetTexture(RegionMapId, regionMap != null ? regionMap : Texture2D.blackTexture);

            //=== 부위 표 만들기 ===
            _regions = regions ?? Array.Empty<RegionDefinition>();

            for (int i = 0; i < _indexById.Length; i++) _indexById[i] = -1;

            for (int i = 0; i < _regions.Length; i++)
            {
                int id = _regions[i].id;
                if (id < 1 || id > 255)
                {
                    Debug.LogWarning($"[Cleaning] 부위 ID는 1~255여야 함: {_regions[i].displayName} = {id}");
                    continue;
                }
                if (_indexById[id] >= 0)
                {
                    Debug.LogWarning($"[Cleaning] 부위 ID {id}가 중복됨: {_regions[i].displayName}");
                    continue;
                }
                _indexById[id] = i;
            }

            _initialDirtyPerRegion = new int[_regions.Length];
            _dirtyPerRegion = new int[_regions.Length];
            _regionProgress = new RegionProgress[_regions.Length];
           
            for (int i = 0; i < _regions.Length; i++)
                _regionProgress[i] = new RegionProgress(_regions[i].id, _regions[i].displayName, 0f, false);
        }

        // 측정 요청. 결과는 몇 프레임 뒤 OnReadback으로 옴
        public void Request(RenderTexture mask)
        {
            if (_disposed || IsPending) return;

            Graphics.Blit(mask, _small, _packMaterial);   // GPU 안에서 1024 → 256으로 줄여 복사
            IsPending = true;
            AsyncGPUReadback.Request(_small, 0, TextureFormat.RGBA32, OnReadback);  // 콜백 설정하고 실행
        }

        // GPU 데이터가 CPU에 도착하면 실행되는 콜백
        void OnReadback(AsyncGPUReadbackRequest request)
        {
            IsPending = false;
            if (_disposed) return; // 기다리는 동안 오브젝트가 사라졌으면 무시
            if (request.hasError)
            {
                Debug.LogWarning("[Cleaning] 진행도 읽기 실패");
                return;
            }

            // 픽셀 배열. Color32는 각 채널이 0~255인 byte
            NativeArray<Color32> pixels = request.GetData<Color32>();

            Array.Clear(_dirtyPerRegion, 0, _dirtyPerRegion.Length);   // 부위별 더러운 픽셀 수 초기화
            int dirtyPixels = 0;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                if (p.r > _cleanThreshold)   // 진흙(R)이 기준값보다 크면 더러운 픽셀
                {
                    dirtyPixels++;
                    int regionIndex = _indexById[p.g];   // 부위 ID(G) → _regions 배열 칸 번호
                    if (regionIndex >= 0)
                        _dirtyPerRegion[regionIndex]++;
                }
            }

            if (!IsReady)
            {
                _initialDirtyPixels = dirtyPixels;
                Array.Copy(_dirtyPerRegion, _initialDirtyPerRegion, _dirtyPerRegion.Length);

                // === DEBUG: 부위 맵에서 실제로 읽힌 번호 분포 ===
                var histogram = new int[256];
                for (int i = 0; i < pixels.Length; i++) histogram[pixels[i].g]++;
                var sb = new System.Text.StringBuilder("[Cleaning] 읽힌 부위 번호: ");
                for (int id = 0; id < 256; id++)
                    if (histogram[id] > 0) sb.Append($"{id}번={histogram[id]}픽셀  ");
                Debug.Log(sb.ToString());

                Debug.Log($"[Cleaning] 가운데 픽셀 = {pixels[pixels.Length / 2]}");
                // =============================================

                WarnEmptyRegions();
            }

            // DEBUG: 측정할 때마다 출력
            Debug.Log($"[Cleaning] 측정: 더러운 픽셀 {dirtyPixels} / 처음 {_initialDirtyPixels}");

            // 전체 진행도 계산
            Progress = Ratio(dirtyPixels, _initialDirtyPixels);

            // 부위별 진행도 계산
            for (int i = 0; i < _regions.Length; i++)
            {
                float progress = Ratio(_dirtyPerRegion[i], _initialDirtyPerRegion[i]);
                bool wasCompleted = _regionProgress[i].IsCompleted;
                bool completed = wasCompleted || progress >= _regionCompleteThreshold;   // 한 번 완료되면 유지

                _regionProgress[i] = new RegionProgress(_regions[i].id, _regions[i].displayName, progress, completed);
            }

            Updated?.Invoke();
        }

        // 처음 더러운 양 대비 지금 남은 양으로 완료율 계산
        static float Ratio(int dirtyNow, int dirtyAtStart)
        {
            if (dirtyAtStart <= 0) return 1f;
            return Mathf.Clamp01(1f - (float)dirtyNow / dirtyAtStart);
        }

        // 부위 맵에 해당 ID 픽셀이 하나도 없으면 알려줌(ID 오타, 텍스처 설정 실수 찾기용)
        void WarnEmptyRegions()
        {
            for (int i = 0; i < _regions.Length; i++)
            {
                if (_initialDirtyPerRegion[i] == 0)
                    Debug.LogWarning($"[Cleaning] 부위 맵에서 '{_regions[i].displayName}'(ID {_regions[i].id}) 영역을 찾지 못했습니다.");
            }
        }

        public void Dispose()
        {
            _disposed = true;

            if (_small != null)
            {
                _small.Release();
                UnityEngine.Object.Destroy(_small);
            }
            if(_packMaterial != null)
            {
                UnityEngine.Object.Destroy(_packMaterial);
            }


        }
    }
}
