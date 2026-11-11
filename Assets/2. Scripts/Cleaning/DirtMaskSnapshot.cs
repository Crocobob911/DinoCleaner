using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DinoCleaner.Cleaning
{
    /// <summary>
    /// [기술 검증용] 공룡의 오물 지도(DirtMask)를 PNG로 저장하고 다시 덮어씁니다.
    /// 거품(알파 채널)은 저장하지 않습니다(2026-11-11 결정). 진흙·이끼·배설물(R·G·B)만 보존.
    /// 정식 이어하기 기능이 아니라, "오물을 있는 그대로 저장할 수 있는가"를 확인하기 위한 코드입니다.
    /// </summary>
    public static class DirtMaskSnapshot
    {
        public readonly struct Result
        {
            public readonly bool Ok;
            public readonly string Message;
            public readonly long Bytes;
            public readonly double Milliseconds;

            public Result(bool ok, string message, long bytes = 0, double milliseconds = 0)
            {
                Ok = ok;
                Message = message;
                Bytes = bytes;
                Milliseconds = milliseconds;
            }
        }

        // 저장 시점 결정: 나갈 때 즉시 읽는 방식(동기). 몇 ms 멈출 수 있음
        public static Result Save(CleanableObject target, string path)
        {
            RenderTexture mask = target != null ? target.MaskTexture : null;
            if (mask == null) return new Result(false, "오물 지도가 없습니다. Play 중이고 공룡이 생성된 상태인지 확인하세요.");

            var watch = Stopwatch.StartNew();
            Texture2D tex = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                // 그래픽카드의 오물 지도 → CPU (linear 그대로)
                tex = new Texture2D(mask.width, mask.height, TextureFormat.RGBA32, false, true);
                RenderTexture.active = mask;
                tex.ReadPixels(new Rect(0, 0, mask.width, mask.height), 0, 0);
                RenderTexture.active = previous;

                // 거품(A)은 버림. 0 = 거품 없음
                Color32[] pixels = tex.GetPixels32();
                for (int i = 0; i < pixels.Length; i++) pixels[i].a = 0;
                tex.SetPixels32(pixels);

                byte[] png = tex.EncodeToPNG();
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllBytes(path, png);

                watch.Stop();
                return new Result(true, $"저장 완료 ({mask.width}x{mask.height})", png.LongLength, watch.Elapsed.TotalMilliseconds);
            }
            catch (Exception e)
            {
                return new Result(false, $"저장 실패: {e.Message}");
            }
            finally
            {
                RenderTexture.active = previous;
                if (tex != null) Object.Destroy(tex);
            }
        }

        public static Result Load(CleanableObject target, string path)
        {
            RenderTexture mask = target != null ? target.MaskTexture : null;
            if (mask == null) return new Result(false, "오물 지도가 없습니다. Play 중이고 공룡이 생성된 상태인지 확인하세요.");
            if (!File.Exists(path)) return new Result(false, $"저장된 파일이 없습니다: {path}");

            var watch = Stopwatch.StartNew();
            Texture2D tex = null;
            try
            {
                byte[] png = File.ReadAllBytes(path);
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                if (!tex.LoadImage(png)) return new Result(false, "PNG를 읽지 못했습니다.");

                string sizeNote = tex.width == mask.width && tex.height == mask.height
                    ? ""
                    : $" ⚠ 크기 다름: 파일 {tex.width}x{tex.height} → 지도 {mask.width}x{mask.height}로 늘림";

                // CPU 이미지 → 그래픽카드의 오물 지도에 덮어쓰기
                Graphics.Blit(tex, mask);
                target.MarkMaskChanged(); // 진행도 다시 측정

                watch.Stop();
                return new Result(true, "불러오기 완료" + sizeNote, png.LongLength, watch.Elapsed.TotalMilliseconds);
            }
            catch (Exception e)
            {
                return new Result(false, $"불러오기 실패: {e.Message}");
            }
            finally
            {
                if (tex != null) Object.Destroy(tex);
            }
        }
    }
}
