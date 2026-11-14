using UnityEngine;
using DinoCleaner.Data;

namespace DinoCleaner.Core
{
    // 1. 플레이어 자세(Stance) 변경 이벤트
    // 용도: UI에 현재 자세(서기/앉기/엎드리기) 아이콘을 표시하거나, 발소리 크기/종류를 바꿀 때
    public readonly struct PlayerStanceChangedEvent : IEvent
    {
        public readonly PlayerStance NewStance;

        public PlayerStanceChangedEvent(PlayerStance newStance)
        {
            NewStance = newStance;
        }
    }


    // 2. 카메라(마우스 커서) 고정 모드 전환 이벤트
    // 용도: UI에서 마우스 커서가 나왔을 때 크로스헤어를 숨기거나, 화면에 블러 효과를 줄 때
    public readonly struct PlayerCameraModeChangedEvent : IEvent
    {
        public readonly bool IsCameraLocked; // true: 고정 모드(커서 자유), false: FPS 모드

        public PlayerCameraModeChangedEvent(bool isCameraLocked)
        {
            IsCameraLocked = isCameraLocked;
        }
    }

    // 3. 카메라 시점 변환 이벤트(혹시나 해서)
    // 용도 미정
    public readonly struct PlayerCameraPerspectiveChangedEvent : IEvent
    {
        public readonly bool IsFirstPerson; // true: 1인칭, false: 3인칭

        public PlayerCameraPerspectiveChangedEvent(bool isFirstPerson)
        {
            IsFirstPerson = isFirstPerson;
        }
    }
}