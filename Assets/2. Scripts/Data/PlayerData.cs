using UnityEngine;

namespace DinoCleaner.Data
{
    public enum PlayerStance { Standing, Crouching, Prone }

    [System.Serializable]
    public struct StanceSetting
    {
        public float height;
        public float centerY; // 보통 X, Z는 0으로 고정이니 Y값만 뺍니다.
        public float cameraHeight;
        public Vector3 pivotPosition;
        public Vector3 pivotRotation;
    }
}
