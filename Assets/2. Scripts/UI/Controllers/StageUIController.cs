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
        private bool isTutorialActive;
        private bool? isCursorFree;

        private void OnEnable()
        {
            EventBus<DinoSpawnedEvent>.Subscribe(OnDinoSpawned);
            EventBus<StageFinishedEvent>.Subscribe(OnStageFinished);
            EventBus<PlayerHudStateChangedEvent>.Subscribe(OnHudStateChanged);
            EventBus<TutorialStepChangedEvent>.Subscribe(OnTutorialStepChanged);
            EventBus<TutorialEndedEvent>.Subscribe(OnTutorialEnded);
        }

        private void OnDisable()
        {
            EventBus<DinoSpawnedEvent>.Unsubscribe(OnDinoSpawned);
            EventBus<StageFinishedEvent>.Unsubscribe(OnStageFinished);
            EventBus<PlayerHudStateChangedEvent>.Unsubscribe(OnHudStateChanged);
            EventBus<TutorialStepChangedEvent>.Unsubscribe(OnTutorialStepChanged);
            EventBus<TutorialEndedEvent>.Unsubscribe(OnTutorialEnded);
        }

        private void OnDinoSpawned(DinoSpawnedEvent e)
        {
            progress = e.Progress;
            UILoader.Instance.ShowUI(UIName.Hud, (progress, hudState));
        }

        // 튜토리얼 시작 조건과 입력 판정은 TutorialDataManager가 맡고, 여기서는 받은 단계를 창에 전달합니다.
        private void OnTutorialStepChanged(TutorialStepChangedEvent e)
        {
            isTutorialActive = true;
            UILoader.Instance.ShowUI(UIName.Tutorial, e);
        }

        // 창이 아직 로드 중일 수 있으므로 로드가 끝난 뒤에 닫습니다.
        private void OnTutorialEnded(TutorialEndedEvent e)
        {
            isTutorialActive = false;
            UILoader loader = UILoader.Instance;
            loader.LoadUI(UIName.Tutorial, instance =>
            {
                if (instance != null && !isTutorialActive && loader.IsShowing(UIName.Tutorial))
                {
                    loader.HideUI(UIName.Tutorial);
                }
            });
        }

        private void OnStageFinished(StageFinishedEvent e)
        {
            // 실패·중도 포기는 기록하지 않음 (중도 포기는 이 이벤트 자체가 오지 않음)
            if (e.Result.Success) GameServices.Progress.MarkStageCleared(e.Result.StageId);
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
            UpdateCursor();

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

        // 모달 팝업이나 정산 화면이 떠 있으면 버튼을 누를 수 있게 커서를 풉니다.
        // Player는 C키(에임 자유 모드)에서만 커서를 다루므로, 원하는 상태가 바뀔 때만 적용해 Player 설정과 겹치지 않게 합니다.
        // TODO: Player가 UIModalChangedEvent를 받아 커서와 입력을 직접 관리하게 되면 이 처리를 지웁니다.
        private void UpdateCursor()
        {
            UILoader loader = UILoader.Instance;
            bool wantsFree = UI_PopupBase.IsAnyModalOpen || hudState.IsAimFree
                             || (loader != null && loader.IsShowing(UIName.Settlement));
            if (isCursorFree == wantsFree) return;

            isCursorFree = wantsFree;
            Cursor.lockState = wantsFree ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = wantsFree;
        }
    }
}
