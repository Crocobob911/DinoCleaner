using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DinoCleaner.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DinoCleaner.Cleaning
{
    /// <summary>
    /// [기술 검증용] 공룡의 오물 지도(DirtMask)를 PNG로, 부위별 진행도·완료 기록을 같은 이름의 JSON으로 저장하고 되살립니다.
    /// 거품(알파 채널)은 저장하지 않습니다(2026-11-11 결정). 진흙·이끼·배설물(R·G·B)만 보존.
    /// 정식 이어하기 기능이 아니라, "오물을 있는 그대로 저장할 수 있는가"를 확인하기 위한 코드입니다.
    ///
    /// 부위별 진행도(%)는 오물 지도에서 다시 계산되므로, 저장값은 "저장 당시 기록(비교용)"이고
    /// 실제로 되살리는 것은 "완료된 부위" 여부입니다 (되살리지 않으면 부위 완료 알림이 다시 울림).
    /// </summary>
    public static class DirtMaskSnapshot
    {
        [Serializable]
        public class RegionRecord
        {
            public int id;
            public string name;
            public float progress;
            public bool completed;
        }

        [Serializable]
        public class Info
        {
            public float totalProgress;
            public List<RegionRecord> regions = new();
        }

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

        public static string InfoPath(string pngPath) => Path.ChangeExtension(pngPath, ".json");

        // 저장 당시의 진행도·부위 기록. 없거나 깨졌으면 null
        public static Info ReadInfo(string pngPath)
        {
            string path = InfoPath(pngPath);
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<Info>(File.ReadAllText(path)); }
            catch { return null; }
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

                // 부위별 진행도·완료 기록
                var info = new Info { totalProgress = target.TotalProgress };
                foreach (RegionProgress r in target.Regions)
                {
                    info.regions.Add(new RegionRecord
                    {
                        id = r.RegionId, name = r.DisplayName, progress = r.Progress, completed = r.IsCompleted,
                    });
                }
                string json = JsonUtility.ToJson(info, true);
                File.WriteAllText(InfoPath(path), json);

                watch.Stop();
                return new Result(true, $"저장 완료 ({mask.width}x{mask.height}, 부위 {info.regions.Count}개)",
                    png.LongLength + json.Length, watch.Elapsed.TotalMilliseconds);
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

                // 완료된 부위를 먼저 되살려야, 오물 지도를 덮은 뒤 첫 측정에서 완료 알림이 다시 울리지 않음
                Info info = ReadInfo(path);
                int restored = 0;
                if (info != null)
                {
                    var completed = new HashSet<int>();
                    foreach (RegionRecord r in info.regions)
                    {
                        if (r.completed) completed.Add(r.id);
                    }
                    target.RestoreCompletedRegions(completed);
                    restored = completed.Count;
                }

                // CPU 이미지 → 그래픽카드의 오물 지도에 덮어쓰기
                Graphics.Blit(tex, mask);
                target.MarkMaskChanged(); // 진행도 다시 측정

                watch.Stop();
                string regionNote = info != null ? $", 완료 부위 {restored}개 복원" : ", ⚠ 부위 기록(JSON) 없음";
                return new Result(true, "불러오기 완료" + regionNote + sizeNote, png.LongLength, watch.Elapsed.TotalMilliseconds);
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
