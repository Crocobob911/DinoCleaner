using UnityEngine;

namespace DinoCleaner.Core
{
    [CreateAssetMenu(fileName = "NewDeployItem", menuName = "DinoCleaner/Deploy Item Data")]
    public class DeployItemData : ScriptableObject
    {
        public string itemName;               // 예: "사다리", "조명"
        public GameObject deployedPrefab;     // 실제 설치될 프리팹
    }
}
