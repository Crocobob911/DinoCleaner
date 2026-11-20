using DinoCleaner.Core;
using DinoCleaner.Data;
using UnityEngine;

namespace DinoCleaner.UI
{
    /// <summary>
    /// Player가 발행하는 상태 이벤트를 모아 HUD용 PlayerHudState로 바꿔 PlayerHudStateChangedEvent로 알립니다.
    /// Player는 값이 바뀔 때만 이벤트를 보내므로, 씬에 미리 두어 시작부터 모든 변화를 받습니다.
    /// </summary>
    public class PlayerHudBridge : MonoBehaviour
    {
        private PlayerStance stance = PlayerStance.Standing;
        private bool isFlying;
        private CleaningToolType tool = CleaningToolType.Water;
        private int nozzleLevel = PlayerHudState.MinNozzleLevel;
        private AimShape shape = AimShape.HorizontalEllipse;
        private bool isAimFree;
        private bool isCleaningToolEquipped = true;

        private void OnEnable()
        {
            EventBus<PlayerStanceChangedEvent>.Subscribe(OnStanceChanged);
            EventBus<UsingFairyDinoEvent>.Subscribe(OnUsingFairyDino);
            EventBus<CleaningToolStateChangedEvent>.Subscribe(OnCleaningToolStateChanged);
            EventBus<CurrentToolChangedEvent>.Subscribe(OnCurrentToolChanged);
            EventBus<PlayerCameraModeChangedEvent>.Subscribe(OnCameraModeChanged);
        }

        private void OnDisable()
        {
            EventBus<PlayerStanceChangedEvent>.Unsubscribe(OnStanceChanged);
            EventBus<UsingFairyDinoEvent>.Unsubscribe(OnUsingFairyDino);
            EventBus<CleaningToolStateChangedEvent>.Unsubscribe(OnCleaningToolStateChanged);
            EventBus<CurrentToolChangedEvent>.Unsubscribe(OnCurrentToolChanged);
            EventBus<PlayerCameraModeChangedEvent>.Unsubscribe(OnCameraModeChanged);
        }

        private void OnStanceChanged(PlayerStanceChangedEvent e)
        {
            stance = e.NewStance;
            Publish();
        }

        private void OnUsingFairyDino(UsingFairyDinoEvent e)
        {
            isFlying = e.isUsing;
            Publish();
        }

        private void OnCleaningToolStateChanged(CleaningToolStateChangedEvent e)
        {
            bool isFoam = e.flags == CleanerFlags.Bubble;
            tool = isFoam ? CleaningToolType.Foam : CleaningToolType.Water;
            nozzleLevel = Mathf.Clamp(e.CurrentWidth, PlayerHudState.MinNozzleLevel, PlayerHudState.MaxNozzleLevel);

            // 거품은 원형(Sphere)으로 뿌리고, 물은 노즐 방향에 따라 가로 또는 세로로 퍼집니다.
            if (e.castShape == CastShape.Sphere) shape = AimShape.Circle;
            else shape = e.Direction == NozzleDirection.Vertical ? AimShape.VerticalEllipse : AimShape.HorizontalEllipse;
            Publish();
        }

        private void OnCurrentToolChanged(CurrentToolChangedEvent e)
        {
            isCleaningToolEquipped = e.toolType == ToolType.CleaningTool;
            Publish();
        }

        // IsCameraLocked가 true면 카메라가 멈추고 커서가 풀린 상태라, 에임이 마우스를 따라갑니다.
        private void OnCameraModeChanged(PlayerCameraModeChangedEvent e)
        {
            isAimFree = e.IsCameraLocked;
            Publish();
        }

        private void Publish()
        {
            var state = new PlayerHudState(ToPosture(), tool, nozzleLevel, shape, isAimFree, isCleaningToolEquipped);
            EventBus<PlayerHudStateChangedEvent>.Publish(new PlayerHudStateChangedEvent(state));
        }

        private PlayerPosture ToPosture()
        {
            if (isFlying) return PlayerPosture.Fly;
            return stance switch
            {
                PlayerStance.Crouching => PlayerPosture.Crouch,
                PlayerStance.Prone => PlayerPosture.Prone,
                _ => PlayerPosture.Stand,
            };
        }
    }
}
