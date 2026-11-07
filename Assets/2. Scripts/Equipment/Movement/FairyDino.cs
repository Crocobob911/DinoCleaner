using UnityEngine;

namespace DinoCleaner.Equipment
{
    public class FairyDino : MovementEquipment
    {
        public override Vector3 CalculateMovement(
            Transform playerTransform, 
            Vector2 moveInput, 
            float elevationInput, 
            bool isGrounded, 
            float deltaTime,
            Stance stance,
            bool isSprinting
        )
        {
            return new Vector3(0,0,0);
        }
    }
}
