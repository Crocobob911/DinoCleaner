using System;
using UnityEngine;

namespace DinoWash.Cleaning
{
    // 오물 마스크용 RenderTexture을 하나 생성하고 관리하는 class
    // R=진흙 G=이끼/화산재 B=배설물 A=비누 (1 = 더러움)
    internal class DirtMask : IDisposable
    {
        public RenderTexture Texture { get; }

        public DirtMask(int resolution)
        {
            Texture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "DirtMask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            Texture.Create();
        }

        // 마스크 전체 칠해버림
        public void Fill(Color color)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = Texture;
            GL.Clear(false, true, color);
            RenderTexture.active = previous;
        }

        // RenderTexture는 gpu 메모리를 사용하므로 Dispose를 통해 해제 필수
        public void Dispose()
        {
            if (Texture == null) return;
            Texture.Release();
            UnityEngine.Object.Destroy(Texture);
        }
    }
}