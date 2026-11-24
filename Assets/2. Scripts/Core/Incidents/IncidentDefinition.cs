using UnityEngine;

namespace DinoCleaner.Core
{
    // 돌발 상황 하나의 정보. 사건마다 에셋을 하나씩 만든다 (취객, 보호단체, 몸부림 ...)
    [CreateAssetMenu(menuName = "DinoCleaner/Incident Definition", fileName = "Incident_")]
    public class IncidentDefinition : ScriptableObject
    {
        [Tooltip("UI 경고에 보일 이름")]
        public string displayName = "Incident";
        [Tooltip("시작할 때 화면에 띄울 경고 문구")]
        [TextArea(2, 4)] public string warning;
        [Tooltip("IStageIncident 컴포넌트가 붙은 프리팹")]
        public GameObject prefab;

        [Header("발생 조건")]
        [Tooltip("뽑힐 확률 가중치 (다른 사건 대비 상대값)")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("세차 시작 후 이 시간(초)이 지나야 발생 가능")]
        [Min(0f)] public float earliestTime = 10f;
        [Tooltip("한 스테이지에서 최대 발생 횟수. 0이면 무제한")]
        [Min(0)] public int maxPerStage = 1;
    }
}
