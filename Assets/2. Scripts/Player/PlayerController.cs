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
        
        [Header("시야(감도) 세팅")]
        public Transform cameraTransform; // Main Camera를 할당해주세요
        public float mouseSensitivity = 20f;
        private float xRotation = 0f;

        [Header("자세별 물리 세팅")]
        public StanceSetting standSetting = new StanceSetting { height = 1f, centerY = 0.5f, cameraHeight = 0.7f };
        public StanceSetting crouchSetting = new StanceSetting { height = 0.5f, centerY = -1f, cameraHeight = 0.1f };
        public StanceSetting proneSetting = new StanceSetting { height = 0.2f, centerY = -1.25f, cameraHeight = -0.4f };
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

        [Header("애니메이션 세팅")]
        public Animator playerAnimator;
        
        // 장비류 세팅
        PlayerEquipment equipment;
        private bool isFiring = false;

        [Header("점프/비행 세팅")]
        private bool wasGrounded = true;
        private float prevElevationInput = 0f;
        private bool suppressJumpUntilRelease = false;
        private float airTime = 0f; // 공중에 떠 있는 시간을 누적할 변수
        private float doubleJumpCooldown = 0.15f; // 첫 점프 후 요정 변신까지의 최소 대기 시간
        private bool isFairyEquipped = false;
        // 카메라 잠금 선택
        private bool isCameraLocked = false;

        PlayerInteraction interaction;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            equipment = GetComponent<PlayerEquipment>();
            interaction = GetComponent<PlayerInteraction>();

            if (playerAnimator == null) playerAnimator = GetComponentInChildren<Animator>();
            
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
            float rawElevationInput = inputActions.Player.FlyElevation.ReadValue<float>();
            bool isJumpPressedThisFrame = rawElevationInput > 0 && prevElevationInput <= 0;

            prevElevationInput = rawElevationInput;
            if(rawElevationInput <= 0f) suppressJumpUntilRelease = false;
            // 2. 상태 업데이트
            CheckGrounded();

            if (isJumpPressedThisFrame && currentStance != PlayerStance.Standing)
            {
                currentStance = PlayerStance.Standing;
                targetStanceSetting = standSetting;
                
                // 일어서기만 하고 실제 점프(위로 솟구침)는 하지 않도록 이번 프레임 점프 입력을 0으로 덮어씀
                suppressJumpUntilRelease = true;
                isJumpPressedThisFrame = false;
            }

            float flyElevationInput = suppressJumpUntilRelease ? 0f : rawElevationInput;
            
            // 3. 실행
            HandleLook();

            if(isFiring)
            {
                equipment.UseCurrentTool();
            }
            HandleStanceTransition();

            if (equipment.CurrentMovement == null)
            {
                Debug.LogWarning("[PlayerController] PlayerEquipment에 movement 장비가 할당되지 않았습니다. 기본 이동 장비를 확인하세요.");
                return;
            }

            if (controller.isGrounded)
            {
                airTime = 0f; // 땅에 닿아있으면 0으로 초기화
            }
            else
            {
                airTime += Time.deltaTime; // 공중에 있으면 시간 계속 누적
            }

            
            
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
            Vector3 frameMovement = equipment.CurrentMovement.CalculateMovement(
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

            if (controller.isGrounded && !wasGrounded)
            {
                equipment.EquipDefaultShoes();
                isFairyEquipped = false;
            }

            if (!controller.isGrounded && airTime > doubleJumpCooldown && isJumpPressedThisFrame)
            {
                equipment.EquipFairyDino();
                isFairyEquipped = true;
            }

            if (playerAnimator != null)
            {
                // WASD 입력 크기를 0 ~ 1 사이 값으로 변환해 Speed에 전달 (0이면 Idle, 1이면 Walk 재생)
                float currentSpeed = Mathf.Clamp01(moveInput.magnitude);
                playerAnimator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime); // 0.1초 동안 부드럽게 값 변경
                
                // 현재 자세(Standing=0, Crouching=1, Prone=2)를 정수(int)로 변환해 전달
                playerAnimator.SetInteger("Stance", (int)currentStance);
                
                // 바닥 착지 여부 전달
                playerAnimator.SetBool("IsGrounded", controller.isGrounded);
            }


            wasGrounded = controller.isGrounded;
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
            if (isFairyEquipped) return; // 요정 공룡 상태에서는 자세 전환 불가
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
