using UnityEngine;

namespace DinoCleaner.Core
{
    // 돌발 상황 프리팹에 붙는 컴포넌트가 구현한다. 스테이지는 이 인터페이스만 안다.
    public interface IStageIncident
    {
        void Begin(IncidentContext context);   // 생성 직후 한 번
        bool IsFinished { get; }               // 스스로 끝났으면 true (시간 종료, 플레이어가 해결 등)
        void End();                            // 정리. 세차가 끝나서 강제 종료될 때도 불림
    }

    // 사건이 스테이지에서 건드릴 수 있는 것들 (필요해질 때 하나씩 추가)
    public sealed class IncidentContext
    {
        public GameObject Dino { get; }
        public ICleanProgress Progress { get; }
        public Transform WashPoint { get; }

        public IncidentContext(GameObject dino, ICleanProgress progress, Transform washPoint)
        {
            Dino = dino;
            Progress = progress;
            WashPoint = washPoint;
        }
    }
}
