
using UnityEngine;
using DinoCleaner.Core;
using DinoCleaner.Data;
using UnityEngine.InputSystem; // 💡 Enum 참조

namespace DinoCleaner.Equipment
{
    public class CleaningTool : Tool
    {
        [SerializeField] private CleaningToolData toolData;
        [SerializeField] private Transform firePoint; // 💡 청소기 총구 끝단 Transform

        [Header("Player Unlocked Stats (임시 세이브 데이터)")]
        public int maxUnlockedLengthStep = 4;
        public int maxUnlockedWaterWidthStep = 4;  // 물: 처음부터 끝까지(Full) 개방됨
        public int maxUnlockedBubbleWidthStep = 1;
        public int pressureLevel = 3; 

        [Header("Runtime State (유저 조작 상태)")]
        public int currentLengthStep = 1; // 2번 키로 조절할 값
        public int currentWidthStep = 1;  // 휠로 조절할 값
        public NozzleDirection currentDirection = NozzleDirection.Horizontal; // R키 조절 값

        public override void UseTool(Transform originTransform)
        {
            if (toolData == null) return;

            // 1. 발사 스펙 산출 (면적 보존 공식)
            float finalDistance = toolData.BaseDistance * currentLengthStep;
            float finalWidth = toolData.BaseWidth * currentWidthStep / currentLengthStep;
            float finalStrength = toolData.IsPressureAffected 
                ? toolData.BaseStrength * pressureLevel 
                : toolData.BaseStrength;

            Vector3 origin = firePoint != null ? firePoint.position : originTransform.position;

            // -------------------------------------------------------------
            // [Step 1] 카메라 정중앙(크로스헤어)으로 레이를 쏴서 '목표 지점' 찾기
            // -------------------------------------------------------------
            Ray camRay;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                // 1. 고정 모드: 화면 정중앙(카메라 정면)으로 쏜다
                camRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            }
            else
            {
                // 2. 마우스 자유 모드: 현재 마우스 커서가 위치한 화면 좌표에서 쏜다
                Vector2 mousePos = Mouse.current.position.ReadValue();
                camRay = Camera.main.ScreenPointToRay(mousePos);
            }
            Vector3 targetPoint;
            
            // 조준용 레이는 청소기 사거리(finalDistance)보다 훨씬 멀리 쏴야 영점이 정확하게 잡힙니다.
            float aimDistance = 100f; 
            
            if (Physics.Raycast(camRay, out RaycastHit camHit, aimDistance))
            {
                targetPoint = camHit.point; // 크로스헤어가 닿은 실제 벽/오브젝트 좌표
            }
            else
            {
                targetPoint = camRay.GetPoint(aimDistance); // 허공일 경우 아주 먼 곳의 좌표
            }

            // -------------------------------------------------------------
            // [Step 2] 총구(origin)에서 목표 지점(targetPoint)으로 실제 청소기 발사
            // -------------------------------------------------------------
            Vector3 fireDirection = (targetPoint - origin).normalized; // 총구 -> 타겟 방향
            
            RaycastHit hit = default;
            bool isHit = false;

            // 실제 발사체(물/거품) 캐스트
            if (toolData.Shape == CastShape.Sphere)
            {
                isHit = Physics.SphereCast(origin, finalWidth, fireDirection, out hit, finalDistance);
            }
            else if (toolData.Shape == CastShape.Capsule)
            {
                float thickness = 0.1f;
                // 가로/세로 퍼짐 방향 (카메라 회전에 맞춤)
                Vector3 offset = currentDirection == NozzleDirection.Horizontal 
                    ? Camera.main.transform.right * finalWidth 
                    : Camera.main.transform.up * finalWidth;

                Vector3 point1 = origin + offset;
                Vector3 point2 = origin - offset;

                isHit = Physics.CapsuleCast(point1, point2, thickness, fireDirection, out hit, finalDistance);
            }

            // -------------------------------------------------------------
            // [Step 3] 실제 맞은 대상(hit)에게 청소(ApplyStroke) 적용
            // -------------------------------------------------------------
            if (isHit)
            {
                // 💡 디버그: 총구에서 실제 맞은 곳까지 초록색 선 그리기
                Debug.DrawLine(origin, hit.point, Color.green, 1f);

                ICleanable hitTarget = hit.collider.GetComponent<ICleanable>();
                if (hitTarget != null)
                {
                    hitTarget.ApplyStroke(new CleanStroke
                    {
                        Position = hit.point,
                        Normal = hit.normal,
                        Radius = finalWidth,
                        Strength = finalStrength,
                        Flags = toolData.Flags,
                        DeltaTime = Time.deltaTime,
                    });
                }
            }
            else
            {
                // 💡 디버그: 허공에 쐈을 때 총구에서 나가는 궤적을 빨간색으로 표시
                Debug.DrawRay(origin, fireDirection * finalDistance, Color.red, 1f);
            }
        }

        // 💡 1. 노즐 길이 토글 (2번 키: 1 -> 2 -> 3 -> 4 -> 1 순환)
        public void ToggleLengthStep()
        {
            currentLengthStep++;
            if (currentLengthStep > maxUnlockedLengthStep)
            {
                currentLengthStep = 1;
            }
            Debug.Log($"[CleaningTool] 노즐 길이 변경: {currentLengthStep}단계");
            PublishCurrentState();
        }

        // 💡 2. 노즐 너비 조절 (마우스 휠: 최소 1 ~ 최대 해금치 사이로 제한)
        public void ToggleWidthStep(float scrollValue)
        {
            int currentMaxLimit = (toolData.Shape == CastShape.Capsule) 
                ? maxUnlockedWaterWidthStep   // 물(Capsule)일 때의 최대치 (항상 4)
                : maxUnlockedBubbleWidthStep; // 거품(Sphere)일 때의 최대치 (업그레이드 수치)
            if (scrollValue > 0)
                currentWidthStep++; // 휠 업: 넓히기
            else if (scrollValue < 0)
                currentWidthStep--; // 휠 다운: 좁히기

            // Mathf.Clamp로 1단계 아래로 내려가거나 최대치를 뚫지 못하게 가둠
            currentWidthStep = Mathf.Clamp(currentWidthStep, 1, currentMaxLimit);
            Debug.Log($"[CleaningTool] 노즐 너비 변경: {currentWidthStep}단계");
            PublishCurrentState();
        }

        // 💡 3. 가로/세로 방향 전환 (R키)
        public void ToggleDirection()
        {
            // 데이터(SO)에서 회전 불가능한 무기(거품 등)라고 설정되어 있으면 무시
            if (!toolData.IsRotatable)
            {
                Debug.Log("[CleaningTool] 이 무기는 방향을 회전할 수 없습니다.");
                return;
            }

            // 삼항 연산자로 가로 <-> 세로 상태 뒤집기
            currentDirection = currentDirection == NozzleDirection.Horizontal 
                ? NozzleDirection.Vertical 
                : NozzleDirection.Horizontal;
                
            Debug.Log($"[CleaningTool] 노즐 방향 전환: {currentDirection}");
            PublishCurrentState();
        }

        public void ToggleWaterBubble()
        {
            if(toolData.Flags == CleanerFlags.Water)
            {
                toolData.Flags = CleanerFlags.Bubble;
                toolData.Shape = CastShape.Sphere;
                toolData.IsRotatable = false;
                toolData.IsPressureAffected = false;
                currentLengthStep = 1;
                currentWidthStep = 1;
                currentDirection = NozzleDirection.Horizontal; // 거품은 방향 전환 불가
                Debug.Log("[CleaningTool] 청소기 모드 변경: 거품");
            }
            else if(toolData.Flags == CleanerFlags.Bubble)
            {
                toolData.Flags = CleanerFlags.Water;
                toolData.Shape = CastShape.Capsule;
                toolData.IsRotatable = true;
                toolData.IsPressureAffected = true;
                currentLengthStep = 1;
                currentWidthStep = 1;
                currentDirection = NozzleDirection.Horizontal;
                Debug.Log("[CleaningTool] 청소기 모드 변경: 물");
            }
            else
            {
                Debug.LogWarning("[CleaningTool] 알 수 없는 청소기 모드입니다.");
            }
            PublishCurrentState();
        }

        private void PublishCurrentState()
        {
            int currentMaxLimit = (toolData.Shape == CastShape.Capsule) 
                ? maxUnlockedWaterWidthStep 
                : maxUnlockedBubbleWidthStep;

            // 현재 무기의 모든 스펙을 영혼까지 끌어모아서 한 방에 발송!
            EventBus<CleaningToolStateChangedEvent>.Publish(new CleaningToolStateChangedEvent(
                currentLengthStep,
                currentWidthStep,
                currentMaxLimit,
                toolData.Shape,
                toolData.Flags,
                currentDirection
            ));
        }
    }
}

