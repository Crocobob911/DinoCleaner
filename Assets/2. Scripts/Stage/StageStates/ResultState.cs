using DinoCleaner.Core;

namespace DinoCleaner.Stage
{
    // 클리어 화면 상태. 결과를 알리기만 하고, "퇴근" 버튼은 UI가 직접 GameSession.GoHome()을 부른다.
    internal class ResultState : StageStateBase
    {
        public ResultState(StageController controller) : base(controller) { }

        public override void Enter()
            => EventBus<StageFinishedEvent>.Publish(new StageFinishedEvent(Controller.LastResult));
    }
}