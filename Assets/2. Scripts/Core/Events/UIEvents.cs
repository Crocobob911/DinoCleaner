// UI와 다른 모듈이 주고받는 IEvent 정의

namespace DinoCleaner.Core
{
    // 모달 UI가 열리거나 전부 닫힘. Player는 이 값이 true인 동안 조작을 막고 커서를 풀어준다.
    public readonly struct UIModalChangedEvent : IEvent
    {
        public readonly bool IsOpen;
        public UIModalChangedEvent(bool isOpen) { IsOpen = isOpen; }
    }

    // 플레이어의 자세, 도구, 노즐, 에임 상태가 바뀜 (Player -> UI)
    public readonly struct PlayerHudStateChangedEvent : IEvent
    {
        public readonly PlayerHudState State;
        public PlayerHudStateChangedEvent(PlayerHudState state) { State = state; }
    }
}
