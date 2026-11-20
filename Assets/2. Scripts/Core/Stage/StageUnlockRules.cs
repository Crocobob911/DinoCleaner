namespace DinoCleaner.Core
{
    // 공룡 스테이지 해금 규칙. 판정은 stageId로만 합니다.
    public static class StageUnlockRules
    {
        /// <summary>
        /// stageId 스테이지가 열려 있는지 반환합니다. 2는 1 클리어, 3은 2 클리어 후 열리고 나머지는 항상 열립니다.
        /// progress가 null이면 모두 열림으로 처리합니다.
        /// </summary>
        public static bool IsUnlocked(int stageId, IGameProgress progress)
        {
            if (progress == null) return true;

            switch (stageId)
            {
                case 2: return progress.IsStageCleared(1);
                case 3: return progress.IsStageCleared(2);
                default: return true;
            }
        }
    }
}
