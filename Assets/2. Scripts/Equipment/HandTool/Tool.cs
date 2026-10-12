using UnityEngine;

namespace DinoCleaner.Equipment
{
    public abstract class Tool : MonoBehaviour
    {
        // originTransform: 기존 플레이어의 위치
        public abstract void UseTool(Transform originTransform);   
    }
}
