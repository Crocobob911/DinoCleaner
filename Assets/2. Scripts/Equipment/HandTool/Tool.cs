using UnityEngine;

namespace DinoCleaner.Equipment
{
    // 물 / 거품 타입
    public enum LiquidType { Water, Foam }
    
    // 쏘는 방향 (가로 / 세로)
    public enum NozzleDirection { Horizontal, Vertical }
    public abstract class Tool : MonoBehaviour
    {
        // originTransform: 기존 플레이어의 위치
        public abstract void UseTool(Transform originTransform);   
    }
}
