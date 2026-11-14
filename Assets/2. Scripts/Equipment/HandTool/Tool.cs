using UnityEngine;

namespace DinoCleaner.Equipment
{
    public abstract class Tool : MonoBehaviour
    {
        public abstract void UseTool(Transform originTransform);   
    }
}
