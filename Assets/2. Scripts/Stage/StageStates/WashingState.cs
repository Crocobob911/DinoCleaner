using DinoCleaner.Core;

namespace DinoCleaner.Stage
{
    // 세차 중. 목표 달성 / 시간 초과 / "세차 완료" 버튼 중 하나면 판정으로
    internal class WashingState : StageStateBase
    {
        bool _finishRequested;

        public WashingState(StageController controller) : base(controller) { }

        public override void Enter()
        {
            Controller.WashTime = 0f;
            _finishRequested = false;
            EventBus<WashFinishRequestedEvent>.Subscribe(OnFinishRequested);
        }

        public override void Exit() => EventBus<WashFinishRequestedEvent>.Unsubscribe(OnFinishRequested);

        void OnFinishRequested(WashFinishRequestedEvent _) => _finishRequested = true;

        public override void Tick(float deltaTime)
        {
            Controller.WashTime += deltaTime;

            var stage = Controller.Stage;
            var progress = Controller.Progress;

            bool reachedGoal = stage.autoFinishOnComplete
                               && progress != null && progress.IsReady
                               && progress.TotalProgress >= stage.completeThreshold;
            bool timeOver = stage.timeLimit > 0f && Controller.WashTime >= stage.timeLimit;

            if (_finishRequested || reachedGoal || timeOver)
                Controller.ChangeState(StageState.Judging);
        }
    }
}