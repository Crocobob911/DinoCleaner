using UnityEngine;
using DinoCleaner.Equipment;
using DinoCleaner.Core;
using DinoCleaner.Data;
namespace DinoCleaner.Player
{
    public class PlayerEquipment : MonoBehaviour, IEquipper
    {
        [Header("장비")]
        public Tool waterGun;
        public Tool deployTool;
        [SerializeField] private Transform firePoint;
        public Tool currentTool;
        public bool hasDeployItem = false;

        [Header("이동 장비")]
        public MovementEquipment defaultShoes;
        public MovementEquipment fairyDino;

        public bool isFairyUnlocked = false;
        public MovementEquipment CurrentMovement {get; private set;}

        private void Start()
        {
            if (waterGun != null) waterGun.gameObject.SetActive(true);
            if (deployTool != null) deployTool.gameObject.SetActive(false);
            currentTool = waterGun;
            if (deployTool is DeployableTool dt)
            {
                dt.OnDeployed += ConsumeDeployItem;
            }
            CurrentMovement = defaultShoes;
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
            EventBus<CurrentToolChangedEvent>.Publish(new CurrentToolChangedEvent(ToolType.Builder));
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
            EventBus<CurrentToolChangedEvent>.Publish(new CurrentToolChangedEvent(ToolType.CleaningTool));
            Debug.Log("[PlayerEquipment] 설치용 아이템 사용 완료, 일반 모드로 전환");
        }

        public void EquipFairyDino()
        {
            if (isFairyUnlocked && CurrentMovement != fairyDino)
            {
                CurrentMovement = fairyDino;
                EventBus<UsingFairyDinoEvent>.Publish(new UsingFairyDinoEvent(true));
                Debug.Log("[PlayerEquipment] 요정 공룡 장착");
            }
        }

        public void EquipDefaultShoes()
        {
            if (CurrentMovement != defaultShoes)
            {
                CurrentMovement = defaultShoes;
                EventBus<UsingFairyDinoEvent>.Publish(new UsingFairyDinoEvent(false));
                Debug.Log("[PlayerEquipment] 기본 신발 장착");
            }
        }

    }
}
