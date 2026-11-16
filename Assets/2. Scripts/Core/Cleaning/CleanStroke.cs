using UnityEngine;

namespace DinoCleaner.Core
{
    [System.Flags]
    public enum CleanerFlags 
    {
        None = 0,
        Water = 1 << 0,   // 1
        Pressure = 1 << 1,   // 2  강한 물줄기
        Soap = 1 << 2,   // 4  비누
    }
    
    // 청소하는 행위(1프레임 단위)
    public struct CleanStroke
    {
        public Vector3 Position;
        public Vector3 Normal;
        public float Radius;
        public float Strength;
        public CleanerFlags Flags;
        public float DeltaTime;
    }
}