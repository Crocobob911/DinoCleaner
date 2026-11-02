using UnityEngine;

namespace DinoCleaner.Core
{
    public readonly struct RegionProgress
    {
        public readonly int RegionId;
        public readonly string DisplayName;
        public readonly float Progress;     // 0~1
        public readonly bool IsCompleted;   // 한 번 완료되면 계속 true

        public RegionProgress(int regionId, string displayName, float progress, bool isCompleted)
        {
            RegionId = regionId;
            DisplayName = displayName;
            Progress = progress;
            IsCompleted = isCompleted;
        }
    }
}