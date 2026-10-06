using System;
using DG.Tweening;
using DinoCleaner.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI.Test
{
    /// <summary>
    /// UILoader 테스트용 Popup. Popup 레이어 + DOTween 열기/닫기 연출 + string 데이터 수신 확인용.
    /// </summary>
    public class Test_Popup : MonoBehaviour, UI_ILayerInfo, UI_Popup, UI_IDataReceiver<string>
    {
        public EUILayer TargetLayer => EUILayer.Popup;

        [SerializeField] private Text messageText;
        [SerializeField] private Button closeButton;
        [SerializeField] private CanvasGroup bgCanvasGroup;
        [SerializeField] private RectTransform windowRect;
        [SerializeField] private float animDuration = 0.15f;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => UILoader.Instance.HideUI("Test_Popup"));
        }

        public void ReceiveData(string data)
        {
            messageText.text = data;
        }

        public void OpenAction()
        {
            bgCanvasGroup.DOKill();
            windowRect.DOKill();

            bgCanvasGroup.alpha = 0f;
            windowRect.localScale = Vector3.one * 0.6f;

            bgCanvasGroup.DOFade(1f, animDuration).SetEase(Ease.OutQuint);
            windowRect.DOScale(Vector3.one, animDuration).SetEase(Ease.OutQuint);
        }

        public void CloseAction(Action onAnimationComplete)
        {
            bgCanvasGroup.DOKill();
            windowRect.DOKill();

            bgCanvasGroup.DOFade(0f, animDuration).SetEase(Ease.InQuad);
            windowRect.DOScale(Vector3.one * 0.6f, animDuration).SetEase(Ease.InBack)
                .OnComplete(() => onAnimationComplete?.Invoke());
        }
    }
}
