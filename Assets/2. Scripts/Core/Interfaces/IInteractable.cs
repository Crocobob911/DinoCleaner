using UnityEngine;

namespace DinoCleaner.Core
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);

        string GetInteractionPrompt();
    }
}
