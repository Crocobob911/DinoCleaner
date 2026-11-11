using System;
using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 실제 저장/재화 시스템이 생기기 전까지 쓰는 임시 구현.
    /// 돈·업그레이드 단계·튜토리얼 완료 여부는 SaveSystem으로 로컬에 저장되고, 설정값은 아직 Play 중에만 유지됩니다.
    /// </summary>
    public class TemporaryGameServices : IGameProgress, IGameSettings
    {
        // 업그레이드 정의. 단계만 저장되고 이름·설명·비용은 여기서 계산하므로, 문구나 비용을 바꿔도 세이브와 충돌하지 않음.
        // id는 세이브 파일의 키이므로 한 번 정하면 바꾸지 말 것
        private static readonly UpgradeDefinition[] Definitions =
        {
            new("pressure", "수압", "수압이 더 강해져서 오물이 더 빨리 씻깁니다.", 1, 3, 100),
            new("nozzleWidth", "노즐 너비", "노즐이 넓어져서 한 번에 더 넓게 씻을 수 있습니다.", 1, 3, 100),
            new("foamWidth", "거품 너비", "거품을 한 번에 더 넓게 뿌릴 수 있습니다.", 1, 3, 100),
            new("fairyDino", "요정 공룡", "요정 공룡의 도움으로 높은 곳까지 날아갈 수 있습니다.", 0, 1, 500),
        };

        private readonly Wallet wallet = new();
        private readonly List<UpgradeInfo> upgrades = new();

        public int Money => wallet.Money;
        public IReadOnlyList<UpgradeInfo> Upgrades => upgrades;
        public bool IsTutorialCompleted
        {
            get => SaveSystem.Data.tutorialCompleted;
            set
            {
                if (SaveSystem.Data.tutorialCompleted == value) return;
                SaveSystem.Data.tutorialCompleted = value;
                SaveSystem.Save();
            }
        }
        public int BgmVolume { get; set; } = 50;
        public int SfxVolume { get; set; } = 65;
        public int MouseSensitivity { get; set; } = 40;

        public event Action Changed;

        public TemporaryGameServices()
        {
            wallet.Changed += () => Changed?.Invoke();

            SaveData save = SaveSystem.Data;
            foreach (UpgradeDefinition def in Definitions)
            {
                int level = Mathf.Clamp(save.GetUpgradeLevel(def.Id, def.StartLevel), def.StartLevel, def.MaxLevel);
                upgrades.Add(def.ToInfo(level));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterIfMissing()
        {
            if (GameServices.Progress != null) return;
            var services = new TemporaryGameServices();
            GameServices.Register(services, services);
        }

        public void AddMoney(int amount)
        {
            wallet.Income(amount);
        }

        public bool TryUpgrade(string upgradeId)
        {
            int index = Array.FindIndex(Definitions, d => d.Id == upgradeId);
            if (index < 0) return false;

            UpgradeDefinition def = Definitions[index];
            UpgradeInfo current = upgrades[index];
            int cost = current.NextCost;
            if (current.IsMaxLevel || !wallet.CanAfford(cost)) return false;

            // 업그레이드를 먼저 반영한 뒤 돈을 빼야, wallet.Changed 시점에 UI가 새 단계를 읽고
            // 돈 차감 저장 한 번에 업그레이드 단계도 같이 파일에 기록됨
            int newLevel = current.Level + 1;
            upgrades[index] = def.ToInfo(newLevel);
            SaveSystem.Data.SetUpgradeLevel(def.Id, newLevel);
            wallet.TryConsume(cost);

            if (cost == 0) // 무료면 Wallet이 저장·알림을 하지 않으므로 직접
            {
                SaveSystem.Save();
                Changed?.Invoke();
            }
            return true;
        }

        public void SaveCleaningProgress()
        {
            Debug.Log("[TemporaryGameServices] 저장 시스템 연결 전이라 실제로 저장하지 않습니다.");
        }

        private readonly struct UpgradeDefinition
        {
            private const int CostStepPerLevel = 100;

            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly int StartLevel;
            public readonly int MaxLevel;
            public readonly int BaseCost;

            public UpgradeDefinition(string id, string displayName, string description, int startLevel, int maxLevel, int baseCost)
            {
                Id = id;
                DisplayName = displayName;
                Description = description;
                StartLevel = startLevel;
                MaxLevel = maxLevel;
                BaseCost = baseCost;
            }

            // 기존 규칙과 동일: 시작 단계에서 BaseCost, 한 단계 오를 때마다 100씩 증가
            public UpgradeInfo ToInfo(int level) => new(Id, DisplayName, Description, level, MaxLevel,
                BaseCost + (level - StartLevel) * CostStepPerLevel);
        }
    }
}
