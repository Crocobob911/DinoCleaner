using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DinoCleaner.Cleaning
{
    internal class CleanProgressTracker : IDisposable
    {
        RenderTexture _small;   // CPU 전달용으로 축소한 마스크
        long _initialDirt = -1; // 처음 진흙 총량 (-1 = 아직 모름)
        bool _disposed;

        public bool IsPending { get; private set; }   // 요청 보내고 결과 기다리는 중
        public bool IsReady => _initialDirt >= 0;
        public float Progress { get; private set; }
        public event Action Updated;

        public CleanProgressTracker(int resolution)
        {
            _small = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "DirtMaskSmall",
            };
            _small.Create();
        }

        // 측정 요청. 결과는 몇 프레임 뒤 OnReadback으로 옴
        public void Request(RenderTexture mask)
        {
            if (_disposed || IsPending) return;

            Graphics.Blit(mask, _small);   // GPU 안에서 1024 → 256으로 줄여 복사
            IsPending = true;
            AsyncGPUReadback.Request(_small, 0, TextureFormat.RGBA32, OnReadback);  // 진동벨 받기
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

            long dirt = 0;
            for (int i = 0; i < pixels.Length; i++)
                dirt += pixels[i].r;   // 진흙(R)만 더함

            if (!IsReady) _initialDirt = dirt;   // 첫 측정 = 기준값

            Progress = _initialDirt > 0 ? Mathf.Clamp01(1f - (float)dirt / _initialDirt) : 1f;
            Updated?.Invoke();
        }

        public void Dispose()
        {
            _disposed = true;
            if (_small == null) return;
            _small.Release();
            UnityEngine.Object.Destroy(_small);
        }
    }
}
