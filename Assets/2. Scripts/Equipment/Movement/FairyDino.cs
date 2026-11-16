using UnityEngine;

namespace DinoCleaner.Equipment
{
    public class FairyDino : MovementEquipment
    {
        public FairyData stats; // 인스펙터에서 SO 할당

        public override Vector3 CalculateMovement(Transform playerTransform, Vector2 moveInput, float elevationInput, bool isGrounded, float deltaTime, Stance stance, bool isSprinting)
        {
            // WASD 이동 계산
            Vector3 move = playerTransform.right * moveInput.x + playerTransform.forward * moveInput.y;
            Vector3 finalVelocity = move * stats.flySpeed;

            // 상하 비행 (중력 무시, 입력값에 따라 속도 직접 제어)
            finalVelocity.y = elevationInput * stats.flySpeed;

            return finalVelocity * deltaTime;
        }
    }
}
