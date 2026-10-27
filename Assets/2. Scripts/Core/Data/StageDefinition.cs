using UnityEngine;

namespace DinoCleaner.Core
{
    // 스테이지 하나의 정보
    [CreateAssetMenu(menuName = "DinoCleaner/Stage Definition", fileName = "Stage_")]
    public class StageDefinition : ScriptableObject
    {
        [Tooltip("레벨 선택 화면에 보일 이름")]
        public string displayName = "Stage 1";

        [Tooltip("CleanableObject가 붙은 공룡 Prefab")]
        public GameObject dinoPrefab;

        [Header("판정")]
        [Tooltip("전체 완료율이 이 이상이면 세차 종료")]
        [Range(0f, 1f)] public float completeThreshold = 0.95f;
        [Tooltip("끄면 완료율이 넘어도 '세차 완료' 버튼을 눌러야 끝남")]
        public bool autoFinishOnComplete = true;
        [Tooltip("이 이상이어야 클리어")]
        [Range(0f, 1f)] public float minSuccessProgress = 0.7f;
        [Tooltip("제한 시간(초). 0이면 없음")]
        public float timeLimit = 0f;

        [Header("보상")]
        public int baseReward = 100;
    }
}