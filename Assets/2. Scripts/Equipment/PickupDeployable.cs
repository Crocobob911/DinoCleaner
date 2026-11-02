using UnityEngine;
using DinoCleaner.Core;

namespace DinoCleaner.Equipment
{
    public class PickupDeployable : MonoBehaviour, IInteractable
    {
        public DeployItemData itemData;
        public string GetInteractionPrompt()
        {
            return $"{itemData.itemName} 줍기";
        }

        public void Interact(GameObject interactor)
        {
            
            IEquipper equipper = interactor.GetComponent<IEquipper>();
            if (equipper != null)
            {
                if (equipper.EquipDeployTool(itemData))
                {
                    Destroy(gameObject);
                    Debug.Log($"[PickupDeployable] {itemData.itemName}을(를) 주웠습니다!");
                }
                
            }
            else
            {
                Debug.LogWarning("상호작용하는 객체에 IEquipper 컴포넌트가 없습니다.");
            }
        }
    }
}
