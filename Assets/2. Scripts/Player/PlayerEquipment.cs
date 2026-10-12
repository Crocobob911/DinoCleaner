using UnityEngine;
using DinoCleaner.Equipment;
using DinoCleaner.Core;
namespace DinoCleaner.Player
{
    public class PlayerEquipment : MonoBehaviour, IEquipper
    {
        [Header("장비")]
        public Tool waterGun;
        public Tool deployTool;
        [SerializeField] private Transform firePoint;

        private Tool currentTool;

        public bool hasDeployItem = false;

        private void Start()
        {
            if (waterGun != null) waterGun.gameObject.SetActive(true);
            if (deployTool != null) deployTool.gameObject.SetActive(false);
            currentTool = waterGun;
            if (deployTool is DeployableTool dt)
            {
                dt.OnDeployed += ConsumeDeployItem;
            }
        }

        public bool EquipDeployTool(DeployItemData itemData)
        {
            if (hasDeployItem)
            {
                Debug.Log("[PlayerEquipment] 이미 설치용 아이템을 가지고 있어서 더 주울 수 없습니다.");
                return false; 
            }
            if (deployTool is DeployableTool dt)
            {
                dt.InitializeDeployData(itemData);
            }
            if(waterGun != null) waterGun.gameObject.SetActive(false);
            if(deployTool != null) deployTool.gameObject.SetActive(true);

            currentTool = deployTool;
            hasDeployItem = true;
            Debug.Log("[PlayerEquipment] 빌드 모드로 전환");
            return true;
        }


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

        private void ConsumeDeployItem()
        {
            hasDeployItem = false;
            if (deployTool != null) deployTool.gameObject.SetActive(false);
            if (waterGun != null) waterGun.gameObject.SetActive(true);
            currentTool = waterGun;
            Debug.Log("[PlayerEquipment] 설치용 아이템 사용 완료, 일반 모드로 전환");
        }

    }
}
