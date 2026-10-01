using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Cleaning
{
    public class DebugStrokeGun : MonoBehaviour
    {
        public bool isFiring = true;
        [SerializeField] float range = 20f;
        [SerializeField] float radius = 0.3f;
        [SerializeField] float strength = 1f;
        [SerializeField] CleanerFlags flags = CleanerFlags.Water;

        // Update is called once per frame
        void Update()
        {
            if (!isFiring) return;

            Ray ray = new Ray(transform.position, transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, range)) return;

            Debug.DrawLine(ray.origin, hit.point, Color.red);

            ICleanable hitTarget = hit.collider.GetComponent<ICleanable>();
            if (hitTarget == null) return;

            hitTarget.ApplyStroke(new CleanStroke
            {
                Position = hit.point,
                Normal = hit.normal,
                Radius = radius,
                Strength = strength,
                Flags = flags,
                DeltaTime = Time.deltaTime,
            });

        }
    }
}