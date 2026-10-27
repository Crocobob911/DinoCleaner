using System.Linq;
using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Stage
{
    // 결과를 계산하고 잠깐 기다렸다가 결과 화면으로
    internal class JudgingState : StageStateBase
    {
        float _elapsed;

        public JudgingState(StageController controller) : base(controller) { }

        public override void Enter()
        {
            var stage = Controller.Stage;
            var progress = Controller.Progress;

            float total = progress != null ? progress.TotalProgress : 0f;
            var regions = progress != null ? progress.Regions.ToArray() : new RegionProgress[0];
            bool success = total >= stage.minSuccessProgress;
            int reward = success ? Mathf.RoundToInt(stage.baseReward * total) : 0;

            Controller.LastResult = new StageResult(success, total, regions, Controller.WashTime, reward);
            _elapsed = 0f;
        }

        public override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed >= Controller.JudgeDuration)
                Controller.ChangeState(StageState.Result);
        }
    }
}