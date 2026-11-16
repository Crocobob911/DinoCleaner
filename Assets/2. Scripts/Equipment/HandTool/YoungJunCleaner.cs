using UnityEngine;
using DinoCleaner.Core;

namespace DinoCleaner.Equipment
{
    public class YoungJunCleaner : Tool
    {
        [Tooltip("청소기 노즐 종류 모음")]
        [SerializeField]
        CleaningToolData[] nozzles;  
        CleaningToolData _currentNozzle;

        void Start()
        {
            if (nozzles == null || nozzles.Length == 0 || nozzles[0] == null)
            {
                Debug.LogError($"[{name}] Nozzles에 노즐 에셋을 연결하세요.", this);
                enabled = false;
                return;
            }
            SwitchNozzle(nozzles[0]);
        }

        public override void UseTool(Transform originTransform)
        {
            if (_currentNozzle == null)
            {
                Debug.LogWarning("[RaycastCleaner] CleaningToolData가 연결되지 않았습니다!");
                return;
            }

            // 레이저 발싸
            Ray ray = new Ray(originTransform.position, originTransform.forward);

            // 레이캐스트 충돌 검사
            if (Physics.Raycast(ray, out RaycastHit hit, _currentNozzle.Range))
            {
                // 테스트용 로그 출력: 닿은 좌표와 오브젝트 이름
                Debug.Log($"[RaycastCleaner] 물발싸!!!!!!!!!!!! 닿은 대상: {hit.collider.name}, 좌표: {hit.point}");

                // 에디터 Scene 창에서 레이캐스트가 맞은 지점까지 선을 그려줍니다
                Debug.DrawLine(ray.origin, hit.point, Color.red, 0.1f);

                // ICleanable 컴포넌트를 찾아서 ApplyStroke 호출
                ICleanable hitTarget = hit.collider.GetComponent<ICleanable>();
                if (hitTarget != null)
                {
                    hitTarget.ApplyStroke(new CleanStroke
                    {
                        Position = hit.point,
                        Normal = hit.normal,
                        Radius = _currentNozzle.Radius,     // CleaningToolData에 세팅된 값 사용
                        Strength = _currentNozzle.Strength,
                        Flags = _currentNozzle.Flags,
                        DeltaTime = Time.deltaTime,
                    });

                    Debug.Log($"[RaycastCleaner] {hit.collider.name}에 ApplyStroke(청소) 적용 완료!");
                }
            }
        }
        void SwitchNozzle(CleaningToolData next)
        {
            if (next == _currentNozzle) return;
            _currentNozzle = next;
            EventBus<NozzleChangedEvent>.Publish(new NozzleChangedEvent(next));   // UI에서 아이콘 바꾸기 등
        }

        void SwitchNozzle(int index)
        {
            if(index < 0 || index >= nozzles.Length)
            {
                index += nozzles.Length;
                index %= nozzles.Length;
            }
            SwitchNozzle(nozzles[index]);
        }
    }
}