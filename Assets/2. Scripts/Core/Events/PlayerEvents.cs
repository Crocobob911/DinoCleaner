using UnityEngine;
using DinoCleaner.Data;

namespace DinoCleaner.Core
{
    // 1. 플레이어 자세(Stance) 변경 이벤트
    // 용도: UI에 현재 자세(서기/앉기/엎드리기) 아이콘을 표시하거나, 발소리 크기/종류를 바꿀 때
    public readonly struct PlayerStanceChangedEvent : IEvent
    {
        // TODO: 자세 바뀔 때 자세 값 넘겨주기 해야 함.
    }

    // 2. 플레이어 장비/도구(Equipment) 변경 이벤트
    // 용도: 우측 하단 UI에 현재 들고 있는 무기(물총/사다리 등) 이름이나 아이콘을 갱신할 때
    public readonly struct PlayerEquipmentChangedEvent : IEvent
    {

        // 생성자 1: 일반 도구 장착 시 (예: 사다리 빌더)
        public PlayerEquipmentChangedEvent(Object a)
        {
            throw new System.NotImplementedException("일반 도구 장착 이벤트는 아직 구현되지 않았습니다.");
            // TODO: 할 것
        }

        // 생성자 2: 청소 도구 장착 및 상태(모드) 변경 시
        public PlayerEquipmentChangedEvent(Object a, Object b)
        {
            throw new System.NotImplementedException("일반 도구 장착 이벤트는 아직 구현되지 않았습니다.");
            // TODO: 할 것
        }
    }

    // 3. 카메라(마우스 커서) 고정 모드 전환 이벤트
    // 용도: UI에서 마우스 커서가 나왔을 때 크로스헤어를 숨기거나, 화면에 블러 효과를 줄 때
    public readonly struct PlayerCameraModeChangedEvent : IEvent
    {
        public readonly bool IsCameraLocked; // true: 고정 모드(커서 자유), false: FPS 모드

        public PlayerCameraModeChangedEvent(bool isCameraLocked)
        {
            IsCameraLocked = isCameraLocked;
        }
    }
}