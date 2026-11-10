using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 업그레이드 상점. 카드는 IGameProgress.Upgrades 개수만큼 만들어 항목 추가에 대응합니다.
    /// </summary>
    public class UI_Shop : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Text moneyText;
        [SerializeField] private UI_UpgradeCard cardTemplate;

        [Header("Detail")]
        [SerializeField] private Text detailNameText;
        [SerializeField] private Text detailLevelText;
        [SerializeField] private UI_LevelDots detailDots;
        [SerializeField] private Text detailDescriptionText;
        [SerializeField] private Text detailCostText;
        [SerializeField] private Button upgradeButton;

        private readonly List<UI_UpgradeCard> cards = new();
        private int selectedIndex;

        private void Awake()
        {
            backButton.onClick.AddListener(OnClickBack);
            upgradeButton.onClick.AddListener(OnClickUpgrade);

            IReadOnlyList<UpgradeInfo> upgrades = GameServices.Progress.Upgrades;
            for (int i = 0; i < upgrades.Count; i++)
            {
                int index = i;
                UI_UpgradeCard card = Instantiate(cardTemplate, cardTemplate.transform.parent);
                card.gameObject.SetActive(true);
                card.SetClickListener(() => Select(index));
                cards.Add(card);
            }
        }

        private void OnEnable()
        {
            GameServices.Progress.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameServices.Progress.Changed -= Refresh;
        }

        private void Select(int index)
        {
            selectedIndex = index;
            Refresh();
        }

        private void Refresh()
        {
            IGameProgress progress = GameServices.Progress;
            moneyText.text = $"보유 돈 : {progress.Money}";

            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].Refresh(progress.Upgrades[i], i == selectedIndex);
            }

            UpgradeInfo selected = progress.Upgrades[selectedIndex];
            detailNameText.text = selected.DisplayName;
            detailLevelText.text = $"Lv. {selected.Level}";
            detailDots.Set(selected.Level, selected.MaxLevel);
            detailDescriptionText.text = selected.Description;
            detailCostText.text = selected.IsMaxLevel ? "최대 단계" : $"비용 : {selected.NextCost}";
            upgradeButton.interactable = !selected.IsMaxLevel && progress.Money >= selected.NextCost;
        }

        private void OnClickUpgrade()
        {
            UpgradeInfo selected = GameServices.Progress.Upgrades[selectedIndex];
            string message = $"정말로 업그레이드 하시겠습니까?\n{selected.DisplayName} Lv. {selected.Level + 1}  비용 {selected.NextCost}";
            UILoader.Instance.ShowUI(UIName.ConfirmPopup, new ConfirmPopupData(message,
                () => GameServices.Progress.TryUpgrade(selected.Id)));
        }

        private void OnClickBack()
        {
            UILoader.Instance.HideUI(UIName.Shop);
            UILoader.Instance.ShowUI(UIName.Lobby);
        }
    }
}
