using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Cleaning
{
    public class DebugCleanable : MonoBehaviour, ICleanable
    {
        int hitCount = 0;
        Vector3 lastHit;
        float lastRadius;

        public void ApplyStroke(CleanStroke stroke)
        {
            lastHit = stroke.Position;
            lastRadius = stroke.Radius;
            hitCount++;

            if (hitCount % 30 == 1) // 너무 길어져서 30프레임마다 한번씩만
                Debug.Log($"[{name}] HIT | COUNT-{hitCount} |  POS-{stroke.Position} | FLAG-{stroke.Flags}");
        }

        void OnDrawGizmos()
        {
            if (hitCount == 0) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastHit, lastRadius);
        }
    }
}