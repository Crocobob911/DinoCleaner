using System;
using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 실제 저장/재화 시스템이 생기기 전까지 쓰는 임시 구현.
    /// 돈·업그레이드 단계·튜토리얼 완료 여부·스테이지 클리어 여부는 SaveSystem(save.json), 설정값은 SettingsSystem(settings.json)에 저장됩니다.
    /// </summary>
    public class TemporaryGameServices : IGameProgress, IGameSettings
    {
        private readonly Wallet wallet = new();
        // 업그레이드 정의는 Resources/UpgradeCatalog 에셋에서 읽음. upgrades와 같은 인덱스로 짝지어짐
        private readonly List<UpgradeData> _upgradeData = new();
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

        /// <summary>날기 튜토리얼 완료 여부를 조회하거나 저장합니다.</summary>
        public bool IsFlyTutorialCompleted
        {
            get => SaveSystem.Data.flyTutorialCompleted;
            set
            {
                if (SaveSystem.Data.flyTutorialCompleted == value) return;
                SaveSystem.Data.flyTutorialCompleted = value;
                SaveSystem.Save();
            }
        }

        /// <summary>요정 공룡을 구매했고 날기 튜토리얼이 미완료인지 조회합니다.</summary>
        public bool ShouldShowFlyTutorial =>
            SaveSystem.Data.GetUpgradeLevel("fairyDino", 0) >= 1 && !SaveSystem.Data.flyTutorialCompleted;

        // 설정값: 바꾸면 메모리에 바로 반영하고 저장 예약. 실제 저장은 설정 창이 닫힐 때(UI_Settings)·게임 종료 시
        public int BgmVolume
        {
            get => SettingsSystem.Data.bgmVolume;
            set => SetSetting(ref SettingsSystem.Data.bgmVolume, value);
        }

        public int SfxVolume
        {
            get => SettingsSystem.Data.sfxVolume;
            set => SetSetting(ref SettingsSystem.Data.sfxVolume, value);
        }

        public int MouseSensitivity
        {
            get => SettingsSystem.Data.mouseSensitivity;
            set => SetSetting(ref SettingsSystem.Data.mouseSensitivity, value);
        }

        public event Action Changed;

        public TemporaryGameServices()
        {
            wallet.Changed += () => Changed?.Invoke();

            UpgradeCatalog catalog = UpgradeCatalog.Load();
            if (catalog == null)
            {
                Debug.LogWarning($"[TemporaryGameServices] Resources/{UpgradeCatalog.ResourcesPath} 에셋이 없어 상점 업그레이드가 비어 있습니다. 메뉴 DinoCleaner/Upgrade/상점 업그레이드 에셋 생성을 실행하세요.");
                return;
            }

            SaveData save = SaveSystem.Data;
            foreach (UpgradeData data in catalog.Upgrades)
            {
                if (data == null) continue;
                int level = data.ClampLevel(save.GetUpgradeLevel(data.Id, data.StartLevel));
                _upgradeData.Add(data);
                upgrades.Add(data.ToInfo(level));
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
            int index = _upgradeData.FindIndex(d => d.Id == upgradeId);
            if (index < 0) return false;

            UpgradeData def = _upgradeData[index];
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

        private static void SetSetting(ref int field, int value)
        {
            value = SettingsSystem.Clamp(value);
            if (field == value) return;
            field = value;
            SettingsSystem.MarkDirty();
        }

        public bool IsStageCleared(int stageId) => SaveSystem.Data.IsStageCleared(stageId);

        public void MarkStageCleared(int stageId)
        {
            if (stageId <= 0)
            {
                Debug.LogWarning($"[TemporaryGameServices] stageId가 {stageId}라 클리어를 기록하지 않습니다. StageDefinition의 Stage Id를 지정하세요.");
                return;
            }
            if (!SaveSystem.Data.MarkStageCleared(stageId)) return; // 이미 클리어한 스테이지

            SaveSystem.Save();
            Changed?.Invoke();
        }

        public bool TryGetStageProgress(int stageId, out float progress) => StageSlots.TryGetProgress(stageId, out progress);

        public void SaveCleaningProgress()
        {
            Debug.Log("[TemporaryGameServices] 저장 시스템 연결 전이라 실제로 저장하지 않습니다.");
        }
    }
}
