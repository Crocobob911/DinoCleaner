
using UnityEngine;
using DinoCleaner.Core;
using DinoCleaner.Data;
using UnityEngine.InputSystem; // 💡 Enum 참조

namespace DinoCleaner.Equipment
{
    public class CleaningTool : Tool
    {
        [SerializeField] private CleaningToolData toolData;
        [SerializeField] private Transform firePoint; 

        // 💡 [추가됨] 하이브리드 시각 효과용 변수
        [Header("Visual Effects")]
        [SerializeField] private Transform beamPrefab;       // 원기둥 모델 프리팹
        [SerializeField] private ParticleSystem splashPrefab; // 부딪힌 곳 파티클 프리팹

        private Transform currentBeam;
        private ParticleSystem currentSplash;

        [Header("Player Unlocked Stats (임시 세이브 데이터)")]
        public int maxUnlockedLengthStep = 4;
        public int maxUnlockedWaterWidthStep = 4;  
        public int maxUnlockedBubbleWidthStep = 1;
        public int pressureLevel = 3; 

        [Header("Runtime State (유저 조작 상태)")]
        public int currentLengthStep = 1; 
        public int currentWidthStep = 1;  
        public NozzleDirection currentDirection = NozzleDirection.Horizontal; 

        private void Start()
        {
            if (beamPrefab != null)
            {
                // 부모(transform)를 지정하지 않고 월드 최상단에 독립적으로 생성합니다.
                currentBeam = Instantiate(beamPrefab); 
                currentBeam.gameObject.SetActive(false);
            }
            if (splashPrefab != null)
            {
                // 파티클 역시 월드에 독립적으로 생성하여 총의 움직임에 끌려가지 않게 합니다.
                currentSplash = Instantiate(splashPrefab);
                currentSplash.Stop();
            }
        }

        public override void UseTool(Transform originTransform, Vector3 targetPoint)
        {
            if (toolData == null) return;

            float finalDistance = toolData.BaseDistance * currentLengthStep;
            float finalWidth = toolData.BaseWidth * currentWidthStep / currentLengthStep;
            float finalStrength = toolData.IsPressureAffected 
                ? toolData.BaseStrength * pressureLevel 
                : toolData.BaseStrength;

            Vector3 origin = firePoint != null ? firePoint.position : originTransform.position;

            Vector3 fireDirection = (targetPoint - origin).normalized; 
                    
            RaycastHit hit = default;
            bool isHit = false;

            // 실제 발사체(물/거품) 캐스트 (기존과 동일)
            if (toolData.Shape == CastShape.Sphere)
            {
                isHit = Physics.SphereCast(origin, finalWidth, fireDirection, out hit, finalDistance);
            }
            else if (toolData.Shape == CastShape.Capsule)
            {
                float thickness = 0.1f;
                Vector3 offset = currentDirection == NozzleDirection.Horizontal 
                    ? Camera.main.transform.right * finalWidth 
                    : Camera.main.transform.up * finalWidth;

                Vector3 point1 = origin + offset;
                Vector3 point2 = origin - offset;

                isHit = Physics.CapsuleCast(point1, point2, thickness, fireDirection, out hit, finalDistance);
            }

            // 시각 효과 업데이트 호출
            Vector3 actualEndPos = isHit ? hit.point : origin + (fireDirection * finalDistance);
            HandleVisualEffects(origin, actualEndPos, fireDirection, finalWidth, isHit);

            // 실제 오물 청소 로직 적용
            if (isHit)
            {
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
        }

        // 💡 [추가됨] 마우스 클릭을 뗐을 때 PlayerController에서 호출할 함수
        public void StopTool()
        {
            if (currentBeam != null) currentBeam.gameObject.SetActive(false); 
            if (currentSplash != null) 
            {
                var emission = currentSplash.emission;
                emission.enabled = false; 
            }
        }

        // 💡 [추가됨] Mesh(원기둥) 늘리기 및 파티클 위치 이동 전용 로직
        private void HandleVisualEffects(Vector3 startPos, Vector3 endPos, Vector3 direction, float currentWidth, bool isHit)
        {
            float distance = Vector3.Distance(startPos, endPos);

            // 1. 물줄기(Beam) 처리
            if (currentBeam != null)
            {
                currentBeam.gameObject.SetActive(true);
                currentBeam.position = startPos + (direction * (distance * 0.5f));
                
                // 🚨 매 프레임 도는 Rotate 대신, 타겟을 향하는 명확한 '절대 회전값'을 부여합니다.
                // LookRotation으로 타겟을 바라보게 한 뒤, 실린더 형태에 맞게 90도 눕혀줍니다.
                currentBeam.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);

                if (toolData.Shape == CastShape.Capsule)
                {
                    if (currentDirection == NozzleDirection.Horizontal)
                        currentBeam.localScale = new Vector3(currentWidth, distance * 0.5f, 0.1f); 
                    else
                        currentBeam.localScale = new Vector3(0.1f, distance * 0.5f, currentWidth);
                }
                else 
                {
                    currentBeam.localScale = new Vector3(currentWidth, distance * 0.5f, currentWidth); 
                }
            }

            // 2. 물보라(Splash) 처리
            if (currentSplash != null)
            {
                var emission = currentSplash.emission;

                if (isHit)
                {
                    emission.enabled = true;
                    if (!currentSplash.isPlaying) currentSplash.Play();

                    // 부모가 없으므로 월드 고정점(hit.point)에 완벽하게 머무릅니다.
                    currentSplash.transform.position = endPos; 
                    currentSplash.transform.LookAt(startPos); 
                }
                else
                {
                    emission.enabled = false;
                }
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

