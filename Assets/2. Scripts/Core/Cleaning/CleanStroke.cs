using UnityEngine;

namespace DinoCleaner.Core
{
    public enum CleanerFlags { None = 0, Water = 1, Pressure = 2, Bubble = 3 }
    
    // û���ϴ� ����(1������ ����)
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