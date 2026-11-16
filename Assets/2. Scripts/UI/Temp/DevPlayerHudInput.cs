using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 플레이어가 없는 Stage 씬에서 HUD를 확인하기 위한 임시 입력.
    /// Player 모듈이 PlayerHudStateChangedEvent를 발행하게 되면 씬에서 제거합니다.
    /// </summary>
    public class DevPlayerHudInput : MonoBehaviour
    {
        private PlayerHudState state = PlayerHudState.Default;
        private bool isModalOpen;

        private void OnEnable() => EventBus<UIModalChangedEvent>.Subscribe(OnModalChanged);
        private void OnDisable() => EventBus<UIModalChangedEvent>.Unsubscribe(OnModalChanged);
        private void OnModalChanged(UIModalChangedEvent e) => isModalOpen = e.IsOpen;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null || isModalOpen) return;

            PlayerPosture posture = state.Posture;
            CleaningToolType tool = state.Tool;
            int nozzle = state.NozzleLevel;
            AimShape shape = state.Shape;
            bool aimFree = state.IsAimFree;

            if (keyboard.leftCtrlKey.wasPressedThisFrame) posture = (PlayerPosture)(((int)posture + 1) % 4);
            if (keyboard.digit1Key.wasPressedThisFrame) tool = CleaningToolType.Water;
            if (keyboard.digit2Key.wasPressedThisFrame) tool = CleaningToolType.Foam;
            if (keyboard.rKey.wasPressedThisFrame) shape = (AimShape)(((int)shape + 1) % 3);
            if (keyboard.cKey.wasPressedThisFrame) aimFree = !aimFree;

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f) nozzle = Mathf.Min(nozzle + 1, PlayerHudState.MaxNozzleLevel);
            if (scroll < 0f) nozzle = Mathf.Max(nozzle - 1, PlayerHudState.MinNozzleLevel);

            if (posture == state.Posture && tool == state.Tool && nozzle == state.NozzleLevel
                && shape == state.Shape && aimFree == state.IsAimFree) return;

            state = new PlayerHudState(posture, tool, nozzle, shape, aimFree);
            EventBus<PlayerHudStateChangedEvent>.Publish(new PlayerHudStateChangedEvent(state));
        }
    }
}
