using UnityEngine;
using DinoCleaner.Equipment;

namespace DinoCleaner.Player
{

    [System.Serializable]
    public struct StanceSetting
    {
        public float height;
        public float centerY; // 보통 X, Z는 0으로 고정이니 Y값만 뺍니다.
        public float cameraHeight;
    }
    public enum PlayerStance { Standing, Crouching, Prone }
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerEquipment))]
    [RequireComponent(typeof(PlayerInteraction))]
    public class PlayerController : MonoBehaviour
    {
        
        [Header("현재 장착된 이동 장비")]
        public MovementEquipment currentMovement;

        [Header("시야(감도) 세팅")]
        public Transform cameraTransform; // Main Camera를 할당해주세요
        public float mouseSensitivity = 20f;
        private float xRotation = 0f;

        [Header("자세별 물리 세팅")]
        public StanceSetting standSetting = new StanceSetting { height = 2f, centerY = 0f, cameraHeight = 0.6f };
        public StanceSetting crouchSetting = new StanceSetting { height = 1f, centerY = -0.5f, cameraHeight = 0.1f };
        public StanceSetting proneSetting = new StanceSetting { height = 0.5f, centerY = -0.75f, cameraHeight = -0.4f };
        public float stanceTransitionSpeed = 10f;
        private StanceSetting targetStanceSetting;

        // Components & Input
        private CharacterController controller;
        private PlayerControls inputActions; // 아까 Generate 한 C# 클래스
        
        // State variables
        private Vector2 moveInput;
        private Vector2 lookInput;
        private Vector3 velocity;
        private bool isGrounded;
        private PlayerStance currentStance = PlayerStance.Standing;
        private float defaultSlopeLimit;
        private bool isSprinting = false;

        
        // 장비류 세팅
        PlayerEquipment equipment;
        private bool isFiring = false;

        // 카메라 잠금 선택
        private bool isCameraLocked = false;

        PlayerInteraction interaction;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            equipment = GetComponent<PlayerEquipment>();
            interaction = GetComponent<PlayerInteraction>();
            
            defaultSlopeLimit = controller.slopeLimit;

            // Input System 초기화
            inputActions = new PlayerControls();
            
            // 웅크리기 입력
            inputActions.Player.Crouch.performed += ctx => ToggleStance();
            inputActions.Player.Fire.started += ctx => isFiring = true;  // 마우스 누름
            inputActions.Player.Fire.canceled += ctx => isFiring = false; // 마우스 뗌
            // 달리기 입력
            inputActions.Player.Sprint.started += ctx => isSprinting = true;
            inputActions.Player.Sprint.canceled += ctx => isSprinting = false;
            // 상호작용 입력
            inputActions.Player.Interact.performed += ctx => interaction.TryInteract();
            // c 키로 카메라 잠금 입력
            inputActions.Player.CameraLock.started += ctx => ToggleCameraLock();
            // r키로 회전 등록
            inputActions.Player.Rotate.started += ctx =>
            {
                // 사다리 모드일 때만 R키가 작동하도록
                if (equipment.hasDeployItem && equipment.deployTool is DeployableTool tool)
                {
                    tool.RotateHologram();
                } else
                {
                    // 청소 도구 회전 구현 TODO
                }
            };
            currentStance = PlayerStance.Standing;
            targetStanceSetting = standSetting;
        }

        private void OnEnable()
        {
            inputActions.Enable();
            Cursor.lockState = CursorLockMode.Locked; // 마우스 커서 숨기기 및 고정
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            inputActions.Disable();
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            // 1. 매 프레임 입력값 읽기
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            lookInput = inputActions.Player.Look.ReadValue<Vector2>();
            float flyElevationInput = inputActions.Player.FlyElevation.ReadValue<float>();
            // 2. 상태 업데이트
            CheckGrounded();
            
            // 3. 실행
            HandleLook();
            if (currentMovement)
            {
                Stance st;
                switch(currentStance)
                {
                    case PlayerStance.Standing:
                        st = Stance.Standing;
                        break;
                    case PlayerStance.Crouching:
                        st = Stance.Crouching;
                        break;
                    case PlayerStance.Prone:
                        st = Stance.Prone;
                        break;
                    default:
                        st = Stance.Standing;
                        break;
                }
                Vector3 frameMovement = currentMovement.CalculateMovement(
                transform, 
                moveInput, 
                flyElevationInput, 
                controller.isGrounded, 
                Time.deltaTime,
                st,
                isSprinting
            );

            // 5. 캐릭터 컨트롤러로 실제 이동
            controller.Move(frameMovement);
            }

            if(isFiring)
            {
                equipment.UseCurrentTool();
            }
            HandleStanceTransition();
        }

        private void HandleLook()
        {
            if (isCameraLocked || cameraTransform == null) return;
            if (cameraTransform == null) return;

            // 마우스 상하 이동 (X축 회전) - 위아래로 고개 끄덕이기
            float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 고개가 뒤로 꺾이지 않도록 제한
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // 마우스 좌우 이동 (Y축 회전) - 몸통 전체가 회전
            float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up * mouseX);
        }

        private void ToggleStance()
        {
            // 다음 상태로 즉시 변경
            currentStance = (PlayerStance)(((int)currentStance + 1) % 3);

            // 결정된 다음 상태를 목표치로 세팅
            switch (currentStance)
            {
                case PlayerStance.Standing: targetStanceSetting = standSetting; break;
                case PlayerStance.Crouching: targetStanceSetting = crouchSetting; break;
                case PlayerStance.Prone: targetStanceSetting = proneSetting; break;
            }
        }

        private void HandleStanceTransition()
        {
            if (Mathf.Abs(controller.height - targetStanceSetting.height) < 0.001f) return;

            controller.height = Mathf.Lerp(controller.height, targetStanceSetting.height, Time.deltaTime * stanceTransitionSpeed);
            controller.center = new Vector3(0, Mathf.Lerp(controller.center.y, targetStanceSetting.centerY, Time.deltaTime * stanceTransitionSpeed), 0);

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = new Vector3(
                    cameraTransform.localPosition.x,
                    Mathf.Lerp(cameraTransform.localPosition.y, targetStanceSetting.cameraHeight, Time.deltaTime * stanceTransitionSpeed),
                    cameraTransform.localPosition.z
                );
            }
        }

        private void ToggleCameraLock()
        {
            isCameraLocked = !isCameraLocked;

            if (isCameraLocked)
            {
                // 카메라 회전 중지, 마우스 커서 화면에 표시 및 자유 이동
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // 다시 FPS 모드로 복귀 (커서 중앙 고정 및 숨김)
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        private void CheckGrounded()
        {
            // 이번에는 CharacterController 내장 기능인 isGrounded를 우선 사용해봅니다.
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; 
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 플레이어가 "Ladder" 태그를 가진 투명 박스 안에 들어가면 한계를 80으로 늘림
            if (other.CompareTag("Ladder"))
            {
                controller.slopeLimit = 85f;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            // 사다리 영역에서 빠져나가면 다시 원래 값(45)으로 원상복구
            if (other.CompareTag("Ladder"))
            {
                controller.slopeLimit = defaultSlopeLimit;
            }
        }
    }
}
