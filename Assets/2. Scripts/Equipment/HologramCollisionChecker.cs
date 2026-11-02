using UnityEngine;

namespace DinoCleaner.Equipment
{
    public class HologramCollisionChecker : MonoBehaviour
    {
        public LayerMask obstacleLayer;
        private int overlapCount = 0;
        public bool IsValid => overlapCount == 0;
        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
            {
                overlapCount++;
                Debug.Log($"[HologramCollisionChecker] 장애물 감지: {other.name}, 현재 겹침 수: {overlapCount}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
            {
                overlapCount--;
            }
        }

        private void OnDisable()
        {
            overlapCount = 0;
        }
    }
}
  