using UnityEngine;
using DinoCleaner.Core;

namespace DinoCleaner.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("인터렉션 세팅")]
        public Transform cameraTransform;
        public float interactRange = 3f;

        public void TryInteract()
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, interactRange))
            {
                IInteractable interactable = hitInfo.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(gameObject);
                } 
            }
        }
    }
}
