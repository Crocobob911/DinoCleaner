using System;
using UnityEngine;
using DinoCleaner.Core;

namespace DinoCleaner.Equipment
{
    public class DeployableTool : Tool
    {
        [Header("Layers & Materials")]
        public LayerMask groundLayer;
        public LayerMask obstacleLayer;
        public float deployRange = 5f;
        public Material validMaterial;
        public Material invalidMaterial;
        [Header("Position Offset")]
        public float heightOffset = 0f;

        private GameObject deployedPrefab;
        private GameObject currentHologram;
        private HologramCollisionChecker collisionChecker;
        private MeshRenderer[] hologramRenderers;

        public event Action OnDeployed;

        private float currentYRotation = 0f; // R키 누를 때마다 누적될 회전값
        private bool isPointingGround = false;
        private Vector3 targetPosition;

        public void InitializeDeployData(DeployItemData data)
        {
            this.deployedPrefab = data.deployedPrefab;
            
            // 기존에 띄워져 있던 홀로그램이 있다면 삭제
            if (currentHologram != null) Destroy(currentHologram);

            // 전달받은 새로운 홀로그램 생성
            if (this.deployedPrefab != null)
            {
                Debug.Log("[DeployableTool] 홀로그램 생성");
                currentHologram = CreateHologramFromPrefab(this.deployedPrefab);
                currentHologram.SetActive(false);
            }
        }

        private GameObject CreateHologramFromPrefab(GameObject original)
        {
            // 1. 복제
            GameObject holo = Instantiate(original);
            holo.name = original.name + "_Hologram";

            // 2. 상호작용 기능 무력화 (홀로그램을 주우면 안 되니까 IInteractable 삭제)
            var interactables = holo.GetComponentsInChildren<MonoBehaviour>();
            foreach (var script in interactables)
            {
                if (script is IInteractable) Destroy(script);
            }

            // 3. 모든 콜라이더를 트리거(물체 통과)로 변경
            var colliders = holo.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.isTrigger = true; 
            }

            // 4. 충돌 감지용 Rigidbody 세팅 (중력 끄기)
            Rigidbody rb = holo.GetComponent<Rigidbody>();
            if (rb == null) rb = holo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // 5. 충돌 체크 스크립트 달아주고 레이어 세팅
            collisionChecker = holo.AddComponent<HologramCollisionChecker>();
            collisionChecker.obstacleLayer = this.obstacleLayer; 

            // 6. 렌더러 캐싱 (업데이트 문에서 Material을 덮어씌울 용도)
            hologramRenderers = holo.GetComponentsInChildren<MeshRenderer>();

            // 7. 레이어 변경 (설치 레이저가 홀로그램을 뚫고 바닥을 볼 수 있게 Ignore Raycast로)
            ChangeLayerRecursive(holo, LayerMask.NameToLayer("Ignore Raycast"));

            return holo;
        }

        private void ChangeLayerRecursive(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                ChangeLayerRecursive(child.gameObject, newLayer);
            }
        }


        private void OnDisable()
        {
            // 다른 무기로 바꾸면 홀로그램 끄기
            if (currentHologram != null) currentHologram.SetActive(false);
        }

        public void RotateHologram()
        {
            currentYRotation = (currentYRotation + 45f) % 360f;
        }

        private void Update()
        {
            if (currentHologram == null || collisionChecker == null) return;
            if (Camera.main == null) {
                Debug.LogWarning("[DeployableTool] 메인 카메라가 없습니다. 홀로그램 위치 업데이트를 건너뜁니다.");
                return;
            }
            Transform cam = Camera.main.transform;
            Ray ray = new Ray(cam.position, cam.forward);

            // 1. 카메라 정면으로 레이저를 쏴서 '바닥(groundLayer)'에 맞았을 때만
            if (Physics.Raycast(ray, out RaycastHit hit, deployRange, groundLayer))
            {
                currentHologram.SetActive(true);
                isPointingGround = true;
                targetPosition = hit.point + new Vector3(0, heightOffset, 0); // 정확히 바닥에 닿은 좌표

                // 좌표 반영 및 Y축 회전 적용
                Quaternion rotation = Quaternion.Euler(0, currentYRotation, 0);
                currentHologram.transform.SetPositionAndRotation(targetPosition, rotation);

                // 2. 충돌 체크(HologramCollisionChecker) 결과에 따라 색상 변경
                Material targetMat = collisionChecker.IsValid ? validMaterial : invalidMaterial;
                foreach (var r in hologramRenderers)
                {
                    if (r.material != targetMat) r.material = targetMat;
                }
            }
            else
            {
                currentHologram.SetActive(false);
                isPointingGround = false;
            }
        }

        public override void UseTool(Transform originTransform)
        {
            // 좌클릭 시 설치 조건이 맞으면 실제 프리팹 생성
            if (isPointingGround && currentHologram.activeSelf)
            {
                Instantiate(deployedPrefab, targetPosition, Quaternion.Euler(0, currentYRotation, 0));
                Destroy(currentHologram);
                OnDeployed?.Invoke();
            }
        }
    }
}