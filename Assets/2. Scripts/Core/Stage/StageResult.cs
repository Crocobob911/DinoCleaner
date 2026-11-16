using System.Collections.Generic;

namespace DinoCleaner.Core
{
    // 스테이지의 판정 결과
    public sealed class StageResult
    {
        public bool Success { get; }
        public float TotalProgress { get; }
        public IReadOnlyList<RegionProgress> Regions { get; }
        public float WashTime { get; }
        public int Reward { get; }
        public int StageId { get; } // StageDefinition.stageId. 클리어 기록용

        public StageResult(bool success, float totalProgress, IReadOnlyList<RegionProgress> regions,
                           float washTime, int reward, int stageId = 0)
        {
            StageId = stageId;
            Success = success;
            TotalProgress = totalProgress;
            Regions = regions;
            WashTime = washTime;
            Reward = reward;
        }
    }
}