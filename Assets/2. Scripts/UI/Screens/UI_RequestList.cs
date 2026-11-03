using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 청소할 공룡을 고르는 의뢰 리스트. 카드는 전달받은 스테이지 수만큼 만듭니다.
    /// </summary>
    public class UI_RequestList : MonoBehaviour, UI_IDataReceiver<StageDefinition[]>
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private UI_RequestCard cardTemplate;

        private bool isBuilt;

        private void Awake()
        {
            backButton.onClick.AddListener(OnClickBack);
            settingsButton.onClick.AddListener(() => UILoader.Instance.ShowUI(UIName.Settings, SettingsMode.Menu));
        }

        public void ReceiveData(StageDefinition[] stages)
        {
            if (isBuilt) return;
            isBuilt = true;

            foreach (StageDefinition stage in stages)
            {
                if (stage == null) continue;

                UI_RequestCard card = Instantiate(cardTemplate, cardTemplate.transform.parent);
                card.gameObject.SetActive(true);
                card.Bind(stage, OnClickCard);
            }
        }

        private void OnClickBack()
        {
            UILoader.Instance.HideUI(UIName.RequestList);
            UILoader.Instance.ShowUI(UIName.Lobby);
        }

        private void OnClickCard(StageDefinition stage)
        {
            UILoader.Instance.HideUI(UIName.RequestList);
            UILoader.Instance.ShowUI(UIName.RequestDetail, stage);
        }
    }
}
