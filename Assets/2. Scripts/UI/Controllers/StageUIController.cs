using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoCleaner.UI
{
    /// <summary>
    /// Stage 씬의 UI 입력과 스테이지 이벤트를 맡습니다.
    /// UI 프리팹은 비동기로 로드되어 시작 시점의 이벤트를 놓칠 수 있으므로, 씬에 있는 이 컴포넌트가 먼저 받아 전달합니다.
    /// </summary>
    public class StageUIController : MonoBehaviour
    {
        private ICleanProgress progress;
        private PlayerHudState hudState = PlayerHudState.Default;

        private void OnEnable()
        {
            EventBus<DinoSpawnedEvent>.Subscribe(OnDinoSpawned);
            EventBus<StageStateChangedEvent>.Subscribe(OnStageStateChanged);
            EventBus<StageFinishedEvent>.Subscribe(OnStageFinished);
            EventBus<PlayerHudStateChangedEvent>.Subscribe(OnHudStateChanged);
        }

        private void OnDisable()
        {
            EventBus<DinoSpawnedEvent>.Unsubscribe(OnDinoSpawned);
            EventBus<StageStateChangedEvent>.Unsubscribe(OnStageStateChanged);
            EventBus<StageFinishedEvent>.Unsubscribe(OnStageFinished);
            EventBus<PlayerHudStateChangedEvent>.Unsubscribe(OnHudStateChanged);
        }

        private void OnDinoSpawned(DinoSpawnedEvent e)
        {
            progress = e.Progress;
            UILoader.Instance.ShowUI(UIName.Hud, (progress, hudState));
        }

        private void OnStageStateChanged(StageStateChangedEvent e)
        {
            if (e.To == StageState.Washing && !GameServices.Progress.IsTutorialCompleted)
            {
                UILoader.Instance.ShowUI(UIName.Tutorial);
            }
        }

        private void OnStageFinished(StageFinishedEvent e)
        {
            GameServices.Progress.AddMoney(e.Result.Reward);

            UILoader loader = UILoader.Instance;
            if (loader.IsShowing(UIName.Tutorial)) loader.HideUI(UIName.Tutorial);
            if (loader.IsShowing(UIName.ProgressDetail)) loader.HideUI(UIName.ProgressDetail);
            if (loader.IsShowing(UIName.ConfirmPopup)) loader.HideUI(UIName.ConfirmPopup);
            if (loader.IsShowing(UIName.Settings)) loader.HideUI(UIName.Settings);
            loader.ShowUI(UIName.Settlement, e.Result);
        }

        private void OnHudStateChanged(PlayerHudStateChangedEvent e) => hudState = e.State;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

#if UNITY_EDITOR
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                EventBus<WashFinishRequestedEvent>.Publish(new WashFinishRequestedEvent());
            }
#endif

            UILoader loader = UILoader.Instance;
            if (loader.IsShowing(UIName.Settlement)) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                UI_Settings.ToggleByEscape(SettingsMode.InGame);
            }
            else if (keyboard.tabKey.wasPressedThisFrame && progress != null
                     && !loader.IsShowing(UIName.Settings) && !loader.IsShowing(UIName.ConfirmPopup))
            {
                if (loader.IsShowing(UIName.ProgressDetail)) loader.HideUI(UIName.ProgressDetail);
                else loader.ShowUI(UIName.ProgressDetail, progress);
            }
        }
    }
}
