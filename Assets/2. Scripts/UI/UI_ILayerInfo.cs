using System;

namespace DinoCleaner.UI
{
    public enum EUILayer
    {
        FullScreen,
        Popup,
        Top
    }

    /// <summary>
    /// UI 프리팹 루트에 붙여 어느 Canvas 아래에 생성될지 지정합니다.
    /// 구현하지 않으면 FullScreen 레이어에 생성됩니다.
    /// </summary>
    public interface UI_ILayerInfo
    {
        EUILayer TargetLayer { get; }
    }

    /// <summary>
    /// 열기/닫기 연출이 있는 UI. UILoader가 Show 시 OpenAction, Hide 시 CloseAction을 호출합니다.
    /// </summary>
    public interface UI_Popup
    {
        void OpenAction();

        /// <summary>
        /// 닫기 연출이 끝나면 반드시 onAnimationComplete를 호출해야 합니다. (UILoader가 이 시점에 비활성화)
        /// </summary>
        void CloseAction(Action onAnimationComplete);
    }
}
