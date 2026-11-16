using UnityEngine;

namespace DinoCleaner.Equipment
{
    public enum Stance
    {
        Standing,
        Crouching,
        Prone
    }
    public abstract class MovementEquipment : MonoBehaviour
    {
        
        public abstract Vector3 CalculateMovement(
            Transform playerTransform, 
            Vector2 moveInput, 
            float elevationInput, 
            bool isGrounded, 
            float deltaTime,
            Stance stance,
            bool isSprinting
        );
    }
}