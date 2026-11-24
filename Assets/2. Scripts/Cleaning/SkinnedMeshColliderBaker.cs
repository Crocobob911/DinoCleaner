using UnityEngine;

namespace DinoCleaner.Cleaning
{
    // 스킨드 메시(공룡)의 지금 자세 그대로 MeshCollider를 만들어 준다. 물총 Raycast가 맞도록
    // [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class SkinnedMeshColliderBaker : MonoBehaviour
    {
        [Tooltip("0 = 시작할 때 한 번만. 공룡이 애니메이션으로 움직이면 0.1~0.2초")]
        [SerializeField] float rebakeInterval = 0f;

        SkinnedMeshRenderer _skinned;
        MeshCollider _collider;
        Mesh _baked;
        float _nextBake;

        void Awake()
        {
            _skinned = GetComponentInChildren<SkinnedMeshRenderer>();
            _collider = GetComponent<MeshCollider>();
            if (_collider == null) _collider = gameObject.AddComponent<MeshCollider>();

            _baked = new Mesh { name = $"{name}_ColliderMesh" };
            Bake();
        }

        void Update()
        {
            if (rebakeInterval <= 0f || Time.time < _nextBake) return;
            Bake();
        }

        void Bake()
        {
            _skinned.BakeMesh(_baked, true);        // 지금 자세 그대로. 스케일은 Transform이 적용하므로 굽지 않음
            _collider.sharedMesh = null;        // 같은 메시를 다시 넣으면 갱신이 안 돼서 한 번 비움
            _collider.sharedMesh = _baked;
            _nextBake = Time.time + rebakeInterval;
        }

        void OnDestroy()
        {
            if (_baked != null) Destroy(_baked);
        }
    }
}
