using System.Collections.Generic;
using UnityEngine;

namespace DinoCleaner.Tutorial
{
    // 튜토리얼 단계 순서 목록. Addressables 주소 Address로 불러옴 (Tutorial 그룹)
    [CreateAssetMenu(menuName = "DinoCleaner/Tutorial Sequence", fileName = "TutorialSequence")]
    public class TutorialSequence : ScriptableObject
    {
        public const string Address = "TutorialSequence";

        [Tooltip("위에서부터 순서대로 안내함")]
        [SerializeField] private List<TutorialStepData> _steps = new();

        public IReadOnlyList<TutorialStepData> Steps => _steps;
    }
}
