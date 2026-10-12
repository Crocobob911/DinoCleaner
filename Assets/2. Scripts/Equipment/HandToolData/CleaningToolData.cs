using UnityEngine;
using DinoCleaner.Core;
namespace DinoCleaner.Equipment
{
    [CreateAssetMenu(fileName = "NewCleaningTool", menuName = "DinoCleaner/Equipment/Cleaning Tool")]
    public class CleaningToolData : ToolData
    {
        [Header("Cleaning Specs")]
        public float Range = 20f;
        public float Radius = 0.3f;
        public float Strength = 1f;
        public CleanerFlags Flags = CleanerFlags.Water;
    }
}
