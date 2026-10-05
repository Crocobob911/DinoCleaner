using UnityEngine;
using UnityEngine.InputSystem; // 새로운 Input System 사용

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float crouchSpeed = 2.5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public Transform cameraTransform; // Main Camera를 할당해주세요
    public float mouseSensitivity = 20f;
    private float xRotation = 0f;

    // Components & Input
    private CharacterController controller;
    private PlayerControls inputActions; // 아까 Generate 한 C# 클래스
    
    // State variables
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        // Input System 초기화
        inputActions = new PlayerControls();
        
        // 점프 입력
        inputActions.Player.Jump.performed += ctx => Jump();
        // 웅크리기 입력
        inputActions.Player.Crouch.performed += ctx => ToggleCrouch();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        Cursor.lockState = CursorLockMode.Locked; // 마우스 커서 숨기기 및 고정
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

        // 2. 상태 업데이트
        CheckGrounded();
        
        // 3. 실행
        HandleLook();
        HandleMovement();
        ApplyGravity();
    }

    private void HandleLook()
    {
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

    private void HandleMovement()
    {
        float currentSpeed = isCrouching ? crouchSpeed : walkSpeed;

        // 카메라가 보는 방향을 기준으로 이동 벡터 계산
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    private void Jump()
    {
        if (isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void ToggleCrouch()
    {
       
        isCrouching = !isCrouching;

        // Player Controller 조정
        if(isCrouching)
        {
            controller.height = 1f;
            controller.center = new Vector3(0, -0.5f, 0);
        }
        else
        {
            controller.height = 2f;
            controller.center = new Vector3(0, 0f, 0);
        }

        // 카메라 위치 조정
        if (cameraTransform != null)
        {
            float targetCameraHeight = isCrouching ? 0.1f : 0.6f;
            // x, z를 0으로 해도 되지만, 확장성을 위해 기존 x, z값을 유지하도록 설정
            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, targetCameraHeight, cameraTransform.localPosition.z);
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

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}