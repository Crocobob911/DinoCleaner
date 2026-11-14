using UnityEngine;

namespace DinoCleaner.Equipment
{
    [CreateAssetMenu(fileName = "NewShoesData", menuName = "Equipment/Movement/Shoes Data")]
    public class ShoesData : MovementData
    {
        public float walkSpeed = 5f;
        public float crouchSpeed = 2.5f;
        public float jumpHeight = 1.5f;
        public float gravity = -9.81f;
        public float sprintScale = 1.5f;
        
    }
}
