namespace DinoCleaner.Stage
{
    // 모든 State의 공통 틀: 들어올 때(Enter) / 매 프레임(Tick) / 나갈 때(Exit)
    internal abstract class StageStateBase
    {
        protected readonly StageController Controller;
        protected StageStateBase(StageController controller) => Controller = controller;

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }
    }


}