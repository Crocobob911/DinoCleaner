using System.Collections.Generic;
using DinoCleaner.Core;
using DinoCleaner.Equipment;
using UnityEngine;

namespace DinoCleaner.Player
{
    // 저장된 업그레이드 단계를 장비의 해금 값에 넣는다. 상점에서 구매하면 Progress.Changed로 바로 다시 반영됨
    public class PlayerUpgradeApplier : MonoBehaviour
    {
        [SerializeField] private CleaningTool _cleaningTool;
        [SerializeField] private PlayerEquipment _equipment;

        private IGameProgress _progress;

        private void Awake()
        {
            if (_equipment == null) _equipment = GetComponent<PlayerEquipment>();
            if (_cleaningTool == null) _cleaningTool = GetComponentInChildren<CleaningTool>(true);
        }

        private void Start()
        {
            _progress = GameServices.Progress;
            if (_progress == null)
            {
                Debug.LogWarning("[PlayerUpgradeApplier] GameServices.Progress가 없어 인스펙터 값을 그대로 씁니다.");
                return;
            }

            _progress.Changed += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (_progress != null) _progress.Changed -= Apply;
        }

        /// <summary>현재 업그레이드 단계를 청소 도구와 이동 장비에 반영합니다.</summary>
        public void Apply()
        {
            if (_progress == null) return;

            if (_cleaningTool != null)
            {
                _cleaningTool.pressureLevel = GetLevel(UpgradeIds.Pressure, _cleaningTool.pressureLevel);
                _cleaningTool.maxUnlockedLengthStep = GetLevel(UpgradeIds.NozzleLength, _cleaningTool.maxUnlockedLengthStep);
                _cleaningTool.maxUnlockedBubbleWidthStep = GetLevel(UpgradeIds.FoamWidth, _cleaningTool.maxUnlockedBubbleWidthStep);
            }

            if (_equipment != null)
            {
                int fallback = _equipment.isFairyUnlocked ? 1 : 0;
                _equipment.isFairyUnlocked = GetLevel(UpgradeIds.FairyDino, fallback) >= 1;
            }
        }

        // 카탈로그에 없는 id면 fallback(기존 값)을 그대로 돌려줌
        private int GetLevel(string id, int fallback)
        {
            IReadOnlyList<UpgradeInfo> upgrades = _progress.Upgrades;
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].Id == id) return upgrades[i].Level;
            }
            return fallback;
        }
    }
}
