using System.Collections.Generic;
using UnityEngine;

namespace DinoCleaner.Core
{
    // 상점에 나올 업그레이드 목록. 배열 순서가 상점 표시 순서
    // Assets/Resources/UpgradeCatalog.asset 에 두어야 런타임에 Resources.Load로 찾을 수 있음
    [CreateAssetMenu(menuName = "DinoCleaner/Upgrade Catalog", fileName = "UpgradeCatalog")]
    public class UpgradeCatalog : ScriptableObject
    {
        public const string ResourcesPath = "UpgradeCatalog";

        [SerializeField] private UpgradeData[] _upgrades;

        public IReadOnlyList<UpgradeData> Upgrades => _upgrades ?? System.Array.Empty<UpgradeData>();

        /// <summary>Resources 폴더의 카탈로그를 불러옵니다. 없으면 null입니다.</summary>
        public static UpgradeCatalog Load() => Resources.Load<UpgradeCatalog>(ResourcesPath);
    }
}
