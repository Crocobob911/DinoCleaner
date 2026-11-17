using DG.Tweening;
using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 청소할 공룡을 고르는 의뢰 리스트. 카드는 전달받은 스테이지 수만큼 만듭니다.
    /// 좌우 화살표 버튼을 누르면 카드 한 장만큼 스크롤합니다.
    /// </summary>
    public class UI_RequestList : MonoBehaviour, UI_IDataReceiver<StageDefinition[]>
    {
        private const float ScrollDuration = 0.25f;
        private const float EdgeEpsilon = 0.001f;

        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private UI_RequestCard cardTemplate;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        private bool isBuilt;

        private void Awake()
        {
            backButton.onClick.AddListener(OnClickBack);
            settingsButton.onClick.AddListener(() => UILoader.Instance.ShowUI(UIName.Settings, SettingsMode.Menu));
            prevButton.onClick.AddListener(() => ScrollByCard(-1));
            nextButton.onClick.AddListener(() => ScrollByCard(1));
        }

        private void OnDisable()
        {
            scrollRect.DOKill();
        }

        // 카드 수와 스크롤 위치에 따라 끝에 닿은 쪽 화살표를 비활성으로 표시합니다.
        private void LateUpdate()
        {
            float position = scrollRect.horizontalNormalizedPosition;
            bool canScroll = GetScrollableWidth() > 0f;
            prevButton.interactable = canScroll && position > EdgeEpsilon;
            nextButton.interactable = canScroll && position < 1f - EdgeEpsilon;
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

        private void ScrollByCard(int direction)
        {
            float scrollable = GetScrollableWidth();
            if (scrollable <= 0f) return;

            var layout = scrollRect.content.GetComponent<HorizontalLayoutGroup>();
            float spacing = layout != null ? layout.spacing : 0f;
            float cardStep = ((RectTransform)cardTemplate.transform).rect.width + spacing;
            float target = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + direction * cardStep / scrollable);

            scrollRect.DOKill();
            scrollRect.StopMovement();
            DOTween.To(() => scrollRect.horizontalNormalizedPosition, value => scrollRect.horizontalNormalizedPosition = value,
                    target, ScrollDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(scrollRect);
        }

        private float GetScrollableWidth()
        {
            return scrollRect.content.rect.width - scrollRect.viewport.rect.width;
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
