using System;
using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 상점의 업그레이드 카드 하나입니다.
    /// </summary>
    public class UI_UpgradeCard : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private UI_LevelDots levelDots;
        [SerializeField] private GameObject selectedOutline;

        public void SetClickListener(Action onClick) => button.onClick.AddListener(() => onClick());

        public void Refresh(UpgradeInfo info, bool isSelected)
        {
            nameText.text = info.DisplayName;
            levelText.text = $"Lv. {info.Level}";
            levelDots.Set(info.Level, info.MaxLevel);
            selectedOutline.SetActive(isSelected);
        }
    }
}
