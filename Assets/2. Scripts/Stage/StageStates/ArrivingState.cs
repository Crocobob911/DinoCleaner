using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Stage
{
    // 공룡을 생성하고 세차 위치까지 이동시킨다
    internal class ArrivingState : StageStateBase
    {
        Vector3 _from, _to;
        float _elapsed;

        public ArrivingState(StageController controller) : base(controller) { }

        public override void Enter()
        {
            _from = Controller.SpawnPoint.position;
            _to = Controller.WashPoint.position;
            _elapsed = 0f;

            var dino = Object.Instantiate(Controller.Stage.dinoPrefab, _from, Controller.WashPoint.rotation);
            var progress = dino.GetComponentInChildren<ICleanProgress>();
            if (progress == null)
                Debug.LogError($"[Stage] {dino.name}에 ICleanProgress(CleanableObject)가 없습니다.", dino);

            Controller.Dino = dino;
            Controller.Progress = progress;

            // 공룡이 생성되었음을 알림
            EventBus<DinoSpawnedEvent>.Publish(new DinoSpawnedEvent(dino, progress));
        }

        public override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            float duration = Controller.ArriveDuration;
            float arriveProgress = duration > 0f ? Mathf.Clamp01(_elapsed / duration) : 1f;
            // 공룡을 세차 위치까지 이동
            Controller.Dino.transform.position = Vector3.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, arriveProgress));
            // 공룡이 세차 위치에 도착하면 Washing 상태로 전환
            if (arriveProgress >= 1f) Controller.ChangeState(StageState.Washing);
        }
    }
}