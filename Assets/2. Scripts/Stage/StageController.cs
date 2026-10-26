using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Stage
{
    // Stage 씬 하나의 흐름: (출근) -> Arriving->Washing->Judging->Result -> (퇴근 버튼) 홈으로
    // 어떤 공룡을 세차할지는 홈에서 고른 GameSession.CurrentStage가 정한다.
    public class StageController : MonoBehaviour
    {
        [Tooltip("홈을 거치지 않고 이 씬에서 바로 Play할 때 쓸 스테이지 (test용)")]
        [SerializeField] StageDefinition testStage;
        [Tooltip("공룡이 처음 나타나는 위치")]
        [SerializeField] Transform spawnPoint;
        [Tooltip("공룡이 멈춰서 세차받는 위치")]
        [SerializeField] Transform washPoint;
        [SerializeField] float arriveDuration = 2f;
        [SerializeField] float judgeDuration = 1f;

        public StageState CurrentState { get; private set; } = StageState.None;

        // === State들이 같이 쓰는 값 ===
        internal StageDefinition Stage { get; private set; }
        internal Transform SpawnPoint => spawnPoint;
        internal Transform WashPoint => washPoint;
        internal float ArriveDuration => arriveDuration;
        internal float JudgeDuration => judgeDuration;

        internal GameObject Dino { get; set; }
        internal ICleanProgress Progress { get; set; }
        internal float WashTime { get; set; }
        internal StageResult LastResult { get; set; }

        Dictionary<StageState, StageStateBase> _states;
        StageStateBase _current;

        void Awake()
        {
            _states = new Dictionary<StageState, StageStateBase>
            {
                { StageState.Arriving, new ArrivingState(this) },
                { StageState.Washing,  new WashingState(this) },
                { StageState.Judging,  new JudgingState(this) },
                { StageState.Result,   new ResultState(this) },
            };
        }

        void Start()
        {
            // 홈에서 고른 스테이지 - 없으면(이 씬에서 바로 Play) 테스트용 스테이지
            Stage = GameSession.CurrentStage != null ? GameSession.CurrentStage : testStage;

            if (Stage == null || Stage.dinoPrefab == null || spawnPoint == null || washPoint == null)
            {
                Debug.LogError("[Stage] 스테이지(공룡 프리팹 포함), Spawn Point, Wash Point를 확인하세요. " + 
                    "이 씬에서 바로 Play한다면 Test Stage를 연결하세요.", this);
                enabled = false;
                return;
            }

            ChangeState(StageState.Arriving);
        }

        void Update() => _current?.Tick(Time.deltaTime);

        // 씬을 떠날 때 현재 상태가 해 둔 이벤트 구독 정리
        void OnDestroy() => _current?.Exit();

        internal void ChangeState(StageState next)
        {
            var prev = CurrentState;

            _current?.Exit();
            CurrentState = next;
            _current = _states[next];

            EventBus<StageStateChangedEvent>.Publish(new StageStateChangedEvent(prev, next));

            _current.Enter();
        }
    }
}