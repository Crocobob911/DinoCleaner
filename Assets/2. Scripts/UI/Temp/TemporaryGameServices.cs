using System;
using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 실제 저장/재화 시스템이 생기기 전까지 쓰는 임시 구현. 값은 Play 중에만 유지됩니다.
    /// </summary>
    public class TemporaryGameServices : IGameProgress, IGameSettings
    {
        private const int StartMoney = 1000;

        private readonly List<UpgradeInfo> upgrades = new()
        {
            new UpgradeInfo("pressure", "수압", "수압이 더 강해져서 오물이 더 빨리 씻깁니다.", 1, 3, 100),
            new UpgradeInfo("nozzleWidth", "노즐 너비", "노즐이 넓어져서 한 번에 더 넓게 씻을 수 있습니다.", 1, 3, 100),
            new UpgradeInfo("foamWidth", "거품 너비", "거품을 한 번에 더 넓게 뿌릴 수 있습니다.", 1, 3, 100),
            new UpgradeInfo("fairyDino", "요정 공룡", "요정 공룡의 도움으로 높은 곳까지 날아갈 수 있습니다.", 0, 1, 500),
        };

        public int Money { get; private set; } = StartMoney;
        public IReadOnlyList<UpgradeInfo> Upgrades => upgrades;
        public bool IsTutorialCompleted { get; set; }
        public int BgmVolume { get; set; } = 50;
        public int SfxVolume { get; set; } = 65;
        public int MouseSensitivity { get; set; } = 40;

        public event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterIfMissing()
        {
            if (GameServices.Progress != null) return;
            var services = new TemporaryGameServices();
            GameServices.Register(services, services);
        }

        public void AddMoney(int amount)
        {
            Money += amount;
            Changed?.Invoke();
        }

        public bool TryUpgrade(string upgradeId)
        {
            int index = upgrades.FindIndex(u => u.Id == upgradeId);
            if (index < 0) return false;

            UpgradeInfo current = upgrades[index];
            if (current.IsMaxLevel || Money < current.NextCost) return false;

            Money -= current.NextCost;
            upgrades[index] = new UpgradeInfo(current.Id, current.DisplayName, current.Description,
                current.Level + 1, current.MaxLevel, current.NextCost + 100);
            Changed?.Invoke();
            return true;
        }

        public void SaveCleaningProgress()
        {
            Debug.Log("[TemporaryGameServices] 저장 시스템 연결 전이라 실제로 저장하지 않습니다.");
        }
    }
}
