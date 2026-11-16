using UnityEngine;
using DinoCleaner.Core;
using DinoCleaner.Data;
namespace DinoCleaner.Equipment
{
    [CreateAssetMenu(fileName = "NewCleaningTool", menuName = "DinoCleaner/Equipment/Cleaning Tool")]
    public class CleaningToolData : ToolData
    {
        [Header("Base Specs (1단계 기본 스펙)")]
        public float BaseDistance = 5f;  // 1단계 사거리
        public float BaseWidth = 0.5f;   // 1단계 넓이
        public float BaseStrength = 1f;  // 1단계 세척력

        [Header("Tool Characteristics (무기 특성)")]
        public CastShape Shape = CastShape.Capsule; // 물은 Capsule, 거품은 Sphere
        public bool IsRotatable = true;             // 가로/세로 전환 기능 유무
        public bool IsPressureAffected = true;      // 수압 스탯의 영향을 받는지 여부
        
        public CleanerFlags Flags = CleanerFlags.Water;
    }
}
