using UnityEngine;

namespace DinoCleaner.Equipment
{
    public class DefaultShoes : MovementEquipment
    {
        public ShoesData stats;

        private float verticalVelocity;

        public override Vector3 CalculateMovement(Transform playerTransform, Vector2 moveInput, float elevationInput, bool isGrounded, float deltaTime, Stance stance, bool isSprinting)
        {
            if (isGrounded && verticalVelocity < 0) 
                verticalVelocity = -2f;

            if (elevationInput > 0 && isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(stats.jumpHeight * -2f * stats.gravity);
            }

            float currentSpeed;
            switch(stance)
            {
                case Stance.Standing:
                    currentSpeed = stats.walkSpeed;
                    break;
                case Stance.Crouching:
                    currentSpeed = stats.crouchSpeed;
                    break;
                case Stance.Prone:
                    currentSpeed = stats.crouchSpeed * 0.5f; // Prone speed is half of crouch speed
                    break;
                default:
                    currentSpeed = stats.walkSpeed;
                    break;
            }
            if(isSprinting)
            {
                currentSpeed *= stats.sprintScale;
            }

            verticalVelocity += stats.gravity * deltaTime;

            Vector3 move = playerTransform.right * moveInput.x + playerTransform.forward * moveInput.y;
            Vector3 finalVelocity = move * currentSpeed;

            finalVelocity.y = verticalVelocity;


            return finalVelocity * deltaTime;
        }
    }
}
