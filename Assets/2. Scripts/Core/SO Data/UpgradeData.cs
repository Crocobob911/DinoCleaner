using UnityEngine;

namespace DinoCleaner.Core
{
    // 상점 업그레이드 하나의 정의. 세이브에는 id와 단계만 저장되므로 이름·설명·비용은 바꿔도 세이브와 충돌하지 않음
    [CreateAssetMenu(menuName = "DinoCleaner/Upgrade Data", fileName = "Upgrade_")]
    public class UpgradeData : ScriptableObject
    {
        [Tooltip("세이브 파일의 키. 한 번 정하면 바꾸지 말 것")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Tooltip("처음부터 가진 단계. 기본 제공이면 1, 구매해야 생기면 0")]
        [SerializeField, Min(0)] private int _startLevel = 1;
        [SerializeField, Min(0)] private int _maxLevel = 3;

        [Tooltip("_costs[i] = (시작 단계 + i)에서 한 단계 올릴 때의 비용. 길이는 최대 단계 - 시작 단계")]
        [SerializeField] private int[] _costs = { 100, 200 };

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int StartLevel => _startLevel;
        public int MaxLevel => _maxLevel;

        /// <summary>현재 단계에서 다음 단계로 올릴 때의 비용을 반환합니다. 최대 단계면 0입니다.</summary>
        public int GetNextCost(int level)
        {
            int index = level - _startLevel;
            if (level >= _maxLevel || index < 0 || _costs == null || index >= _costs.Length) return 0;
            return _costs[index];
        }

        /// <summary>저장된 단계를 이 업그레이드의 시작~최대 범위로 제한해 반환합니다.</summary>
        public int ClampLevel(int level) => Mathf.Clamp(level, _startLevel, _maxLevel);

        /// <summary>상점 표시용 UpgradeInfo를 만듭니다.</summary>
        public UpgradeInfo ToInfo(int level) =>
            new(_id, _displayName, _description, level, _maxLevel, GetNextCost(level));

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_maxLevel < _startLevel) _maxLevel = _startLevel;

            int expected = _maxLevel - _startLevel;
            if (_costs == null || _costs.Length != expected)
            {
                Debug.LogWarning($"[UpgradeData] {name}: 비용 개수({_costs?.Length ?? 0})가 최대 단계 - 시작 단계({expected})와 다릅니다.", this);
            }
        }
#endif
    }
}
