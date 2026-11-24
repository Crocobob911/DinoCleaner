using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Incidents
{
    // 돌발 상황 공통 틀: 정해진 시간이 지나면 끝난다. 사건마다 상속해서 OnBegin / OnEnd만 채운다.
    public abstract class StageIncidentBase : MonoBehaviour, IStageIncident
    {
        [Tooltip("이 시간(초)이 지나면 자동으로 끝남. 0이면 Finish()를 부를 때까지 계속")]
        [SerializeField] protected float duration = 10f;

        protected IncidentContext Context { get; private set; }
        public bool IsFinished { get; private set; }

        float _endTime;

        public void Begin(IncidentContext context)
        {
            Context = context;
            _endTime = duration > 0f ? Time.time + duration : float.MaxValue;
            OnBegin();
        }

        protected virtual void Update()
        {
            if (!IsFinished && Context != null && Time.time >= _endTime) Finish();
        }

        // 시간 전에 끝낼 때 (예: 플레이어가 보호단체를 쫓아냄)
        protected void Finish() => IsFinished = true;

        public void End() => OnEnd();

        protected abstract void OnBegin();
        protected virtual void OnEnd() { }
    }
}
