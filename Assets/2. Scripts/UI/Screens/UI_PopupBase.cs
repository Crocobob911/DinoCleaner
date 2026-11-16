using System;
using DG.Tweening;
using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    /// <summary>
    /// Popup 레이어 UI의 공통 열기/닫기 연출과 모달 상태 알림을 맡습니다.
    /// </summary>
    public abstract class UI_PopupBase : MonoBehaviour, UI_ILayerInfo, UI_Popup
    {
        private static int modalCount;

        /// <summary>
        /// 입력을 막는 팝업이 하나라도 열려 있는지 여부입니다.
        /// </summary>
        public static bool IsAnyModalOpen => modalCount > 0;

        public EUILayer TargetLayer => EUILayer.Popup;

        /// <summary>
        /// 열려 있는 동안 배경 씬의 입력을 막을지 여부입니다.
        /// </summary>
        protected virtual bool IsModal => true;

        [SerializeField] private CanvasGroup bgCanvasGroup;
        [SerializeField] private RectTransform windowRect;
        [SerializeField] private float animDuration = 0.15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetModalCount() => modalCount = 0;

        protected virtual void OnEnable()
        {
            if (!IsModal) return;
            modalCount++;
            if (modalCount == 1) EventBus<UIModalChangedEvent>.Publish(new UIModalChangedEvent(true));
        }

        protected virtual void OnDisable()
        {
            bgCanvasGroup.DOKill();
            windowRect.DOKill();

            if (!IsModal) return;
            modalCount--;
            if (modalCount == 0) EventBus<UIModalChangedEvent>.Publish(new UIModalChangedEvent(false));
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
