using UnityEngine;

namespace DinoCleaner.Incidents
{
    // 시스템 테스트용: 공룡을 좌우로 흔든다 (몸부림 미리보기)
    public class TestIncident : StageIncidentBase
    {
        [SerializeField] float shakeAmount = 0.05f;
        Vector3 _origin;

        protected override void OnBegin()
        {
            _origin = Context.Dino.transform.position;
            Debug.Log($"[Incident] 테스트 사건 시작 ({duration}초)");
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished || Context == null || Context.Dino == null) return;
            Context.Dino.transform.position = _origin + Vector3.right * (Mathf.Sin(Time.time * 20f) * shakeAmount);
        }

        protected override void OnEnd()
        {
            if (Context != null && Context.Dino != null) Context.Dino.transform.position = _origin;
            Debug.Log("[Incident] 테스트 사건 끝");
        }
    }
}
