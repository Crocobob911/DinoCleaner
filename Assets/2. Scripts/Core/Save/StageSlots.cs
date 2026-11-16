using System.IO;
using UnityEngine;

namespace DinoCleaner.Core
{
    /// <summary>
    /// 공룡별 진행도 슬롯 저장소.
    /// - 가벼운 정보(StageSlot: 전체·부위별 진행도, 청소 시간) → save.json
    /// - 무거운 정보(오물 지도 PNG) → DirtPath(stageId). 파일을 쓰고 읽는 건 사용하는 쪽(스테이지)이 하고, 여기선 경로와 삭제만 맡음
    /// 언제 저장하고 언제 지울지(이어하기 규칙)는 스테이지 쪽에서 정함.
    /// </summary>
    public static class StageSlots
    {
        public static string DirtDirectory => Path.Combine(SaveSystem.SaveDirectory, "Dirt");
        public static string DirtPath(int stageId) => Path.Combine(DirtDirectory, $"dirt_{stageId}.png");

        // 없으면 null
        public static StageSlot Get(int stageId) => SaveSystem.Data.GetStageSlot(stageId);

        public static bool TryGetProgress(int stageId, out float progress)
        {
            StageSlot slot = Get(stageId);
            progress = slot != null ? slot.progress : 0f;
            return slot != null;
        }

        // 슬롯 저장 (같은 stageId가 있으면 교체) → save.json 즉시 저장
        public static void Save(StageSlot slot)
        {
            SaveSystem.Data.SetStageSlot(slot);
            SaveSystem.Save();
        }

        // 슬롯과 오물 지도 PNG를 함께 지움
        public static void Delete(int stageId)
        {
            bool removed = SaveSystem.Data.RemoveStageSlot(stageId);
            string path = DirtPath(stageId);
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[StageSlots] 오물 지도 파일을 지우지 못했습니다: {path}\n{e}");
            }
            if (removed) SaveSystem.Save();
        }

        // 모든 오물 지도 파일 삭제 (새 게임)
        internal static void DeleteAllDirtFiles()
        {
            if (!Directory.Exists(DirtDirectory)) return;
            foreach (string file in Directory.GetFiles(DirtDirectory, "dirt_*.png")) File.Delete(file);
        }
    }
}
