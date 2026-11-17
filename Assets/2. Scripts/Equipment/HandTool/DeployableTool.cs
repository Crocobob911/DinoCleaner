using System;
using UnityEngine;
using DinoCleaner.Core;
using UnityEngine.InputSystem;

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
            if (Camera.main == null) return;

            // 💡 [개선됨] 카메라 정면 고정(cam.forward) 대신 PlayerController와 똑같이 
            // 마우스 커서 잠금 여부에 따라 레이 방향을 구하도록 통일성 확보
            Ray ray;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            }
            else
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                ray = Camera.main.ScreenPointToRay(mousePos);
            }

            // 설치는 100f 거리가 아니라 지정된 짧은 사거리(deployRange)와 바닥(groundLayer)만 체크해야 합니다.
            if (Physics.Raycast(ray, out RaycastHit hit, deployRange, groundLayer))
            {
                currentHologram.SetActive(true);
                isPointingGround = true;
                targetPosition = hit.point + new Vector3(0, heightOffset, 0); 

                Quaternion rotation = Quaternion.Euler(0, currentYRotation, 0);
                currentHologram.transform.SetPositionAndRotation(targetPosition, rotation);

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
        public override void UseTool(Transform originTransform, Vector3 targetPoint)
        {
            // 💡 [개선됨] 설치 조건에 collisionChecker.IsValid(빨간색이 아닐 때)를 추가하여
            // 겹친 상태에서 억지로 지어지는 버그를 차단합니다.
            if (isPointingGround && currentHologram.activeSelf && collisionChecker.IsValid)
            {
                Instantiate(deployedPrefab, targetPosition, Quaternion.Euler(0, currentYRotation, 0));
                
                // 홀로그램을 파괴(Destroy)하지 않고 숨김(SetActive) 처리하여 재사용성을 높임
                currentHologram.SetActive(false); 
                OnDeployed?.Invoke();
            }
        }
    }
}