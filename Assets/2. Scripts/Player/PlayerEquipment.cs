using UnityEngine;
using DinoCleaner.Equipment;
namespace DinoCleaner.Player
{
    public class PlayerEquipment : MonoBehaviour
    {
        [Header("장비")]
        [SerializeField] private Tool currentTool;
        [SerializeField] private Transform firePoint;

        public void UseCurrentTool()
        {
            if (currentTool != null && firePoint != null)
            {
                currentTool.UseTool(firePoint);
            }
            else
            {
                Debug.LogWarning("[PlayerEquipment] 도구가 없거나 firepoint가 설정되지 않았습니다.");
            }
        }
    }
}
