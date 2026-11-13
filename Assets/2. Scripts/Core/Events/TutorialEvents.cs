// 튜토리얼과 관련된 IEvent 정의. TutorialDataManager가 발행하고 UI가 구독함

namespace DinoCleaner.Core
{
    // 튜토리얼 안내 단계가 바뀜 (Tutorial -> UI). 첫 단계 표시도 이 이벤트로 알림
    public readonly struct TutorialStepChangedEvent : IEvent
    {
        public readonly string Message;
        public readonly int StepNumber;  // 1부터 시작
        public readonly int StepCount;

        public TutorialStepChangedEvent(string message, int stepNumber, int stepCount)
        {
            Message = message;
            StepNumber = stepNumber;
            StepCount = stepCount;
        }
    }

    // 튜토리얼이 끝남 (Tutorial -> UI). 완료, 건너뛰기, 중단 모두 포함. UI는 튜토리얼 창을 닫으면 됨
    public readonly struct TutorialEndedEvent : IEvent { }
}
