using System;
using System.Collections.Generic;

namespace DinoCleaner.Core
{
    // 돈, 업그레이드, 저장처럼 스테이지를 넘어 유지되는 진행 정보. For UI & Stage
    public interface IGameProgress
    {
        int Money { get; }
        IReadOnlyList<UpgradeInfo> Upgrades { get; }
        bool IsTutorialCompleted { get; set; }

        // 돈이나 업그레이드가 바뀔 때마다 호출
        event Action Changed;

        void AddMoney(int amount);

        // 돈이 부족하거나 최대 단계면 false
        bool TryUpgrade(string upgradeId);

        // 공룡(스테이지)별 클리어 여부. stageId는 StageDefinition.stageId
        bool IsStageCleared(int stageId);

        // 성공 판정이 났을 때 호출. 처음 클리어면 저장하고 Changed 호출
        void MarkStageCleared(int stageId);

        void SaveCleaningProgress();
    }
}
