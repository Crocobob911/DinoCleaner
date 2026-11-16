namespace DinoCleaner.Core
{
    // 상점에 표시할 업그레이드 하나의 현재 상태
    public readonly struct UpgradeInfo
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int Level;
        public readonly int MaxLevel;
        public readonly int NextCost;

        public bool IsMaxLevel => Level >= MaxLevel;

        public UpgradeInfo(string id, string displayName, string description, int level, int maxLevel, int nextCost)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Level = level;
            MaxLevel = maxLevel;
            NextCost = nextCost;
        }
    }
}
