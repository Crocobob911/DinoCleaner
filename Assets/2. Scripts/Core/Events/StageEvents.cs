using UnityEngine;

namespace DinoCleaner.Core
{
    // 스테이지 상태가 바뀜 (UI 화면 전환, BGM)
    public readonly struct StageStateChangedEvent : IEvent
    {
        public readonly StageState From;
        public readonly StageState To;
        public StageStateChangedEvent(StageState from, StageState to) { From = from; To = to; }
    }

    // 공룡이 생성됨. UI는 여기서 진행도 확인 위해 ICleanProgress 받아감
    public readonly struct DinoArrivedEvent : IEvent
    {
        public readonly GameObject Dino;
        public readonly ICleanProgress Progress;
        public DinoArrivedEvent(GameObject dino, ICleanProgress progress) { Dino = dino; Progress = progress; }
    }

    // 판정이 끝나고 결과가 나옴(클리어 화면)
    public readonly struct StageFinishedEvent : IEvent
    {
        public readonly StageResult Result;
        public StageFinishedEvent(StageResult result) { Result = result; }
    }

    //=== 요청 (UI -> 스테이지) ===
    public readonly struct WashFinishRequestedEvent : IEvent { }   // "세차 완료" 버튼
}