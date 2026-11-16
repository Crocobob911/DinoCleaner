using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DinoCleaner.Tutorial
{
    // Stage 씬에 하나 둔다. 세차가 시작되면 조건에 맞는 기본 또는 날기 튜토리얼을 UI에 알린다.
    // 단계 목록은 Addressables의 TutorialSequence와 FlyTutorialSequence에서 불러온다.
    // UI는 이벤트(TutorialStepChangedEvent, TutorialEndedEvent)만 받아 표시하고, 입력 판정은 여기서 한다.
    public class TutorialDataManager : MonoBehaviour
    {
        private enum TutorialKind
        {
            Basic,
            Fly,
        }

        [Tooltip("튜토리얼을 진행할 스테이지의 StageDefinition.stageId (스테고사우루스 = Stage_1)")]
        [SerializeField] private int _tutorialStageId = 1;

        [Tooltip("튜토리얼 전체를 건너뛰는 키. 바꾸면 UI의 '건너뛰기 [F]' 문구도 같이 바꿀 것")]
        [SerializeField] private Key _skipKey = Key.F;

        [Tooltip("단계를 완료한 뒤 다음 단계를 띄우기까지 기다리는 시간(초)")]
        [SerializeField] private float _stepInterval = 3f;

        private readonly Queue<TutorialStepData> _remaining = new();
        private AsyncOperationHandle<TutorialSequence> _loadHandle;
        private AsyncOperationHandle<TutorialSequence> _flyLoadHandle;
        private TutorialSequence _sequence;
        private TutorialSequence _flySequence;
        private TutorialKind _tutorialKind;
        private TutorialStepData _current;
        private int _stepNumber;
        private int _stepCount;
        private bool _isModalOpen;
        private bool _isTestRun;
        private bool _hasPendingStart;
        private bool _isFlying;
        private bool _isWaitingForNextStep;
        private float _stepWaitRemaining;

        private bool IsRunning => _current != null;

        private void Awake()
        {
            _loadHandle = Addressables.LoadAssetAsync<TutorialSequence>(TutorialSequence.Address);
            _loadHandle.Completed += handle => OnSequenceLoaded(handle, TutorialKind.Basic);
            _flyLoadHandle = Addressables.LoadAssetAsync<TutorialSequence>(TutorialSequence.FlyAddress);
            _flyLoadHandle.Completed += handle => OnSequenceLoaded(handle, TutorialKind.Fly);
        }

        private void OnDestroy()
        {
            if (_loadHandle.IsValid()) Addressables.Release(_loadHandle);
            if (_flyLoadHandle.IsValid()) Addressables.Release(_flyLoadHandle);
        }

        private void OnEnable()
        {
            EventBus<StageStateChangedEvent>.Subscribe(OnStageStateChanged);
            EventBus<UIModalChangedEvent>.Subscribe(OnModalChanged);
            EventBus<UsingFairyDinoEvent>.Subscribe(OnUsingFairyDino);
        }

        private void OnDisable()
        {
            EventBus<StageStateChangedEvent>.Unsubscribe(OnStageStateChanged);
            EventBus<UIModalChangedEvent>.Unsubscribe(OnModalChanged);
            EventBus<UsingFairyDinoEvent>.Unsubscribe(OnUsingFairyDino);
        }

        private void OnSequenceLoaded(AsyncOperationHandle<TutorialSequence> handle, TutorialKind kind)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                string address = kind == TutorialKind.Basic ? TutorialSequence.Address : TutorialSequence.FlyAddress;
                string tutorialName = kind == TutorialKind.Basic ? "튜토리얼" : "날기 튜토리얼";
                Debug.LogWarning($"[TutorialDataManager] Addressables 주소 '{address}'를 불러오지 못해 {tutorialName} 없이 진행합니다.", this);
                if (_tutorialKind == kind) _hasPendingStart = false;
                return;
            }

            if (kind == TutorialKind.Basic) _sequence = handle.Result;
            else _flySequence = handle.Result;
            if (_hasPendingStart && _tutorialKind == kind)
            {
                _hasPendingStart = false;
                StartTutorial(_isTestRun);
            }
        }

        private void OnStageStateChanged(StageStateChangedEvent e)
        {
            if (e.To == StageState.Washing)
            {
                if (IsRunning || _hasPendingStart) return;

                if (ShouldStart())
                {
                    RequestStart(TutorialKind.Basic, false);
                }
                else if (GameSession.CurrentStage != null
                         && GameServices.Progress != null
                         && GameServices.Progress.IsTutorialCompleted
                         && GameServices.Progress.ShouldShowFlyTutorial)
                {
                    RequestStart(TutorialKind.Fly, false);
                }
            }
            else
            {
                // 세차가 끝나 버리면 완료로 치지 않음. 다음에 처음부터 다시 함
                _hasPendingStart = false;
                if (IsRunning) Stop();
            }
        }

        private void OnModalChanged(UIModalChangedEvent e) => _isModalOpen = e.IsOpen;

        private void OnUsingFairyDino(UsingFairyDinoEvent e) => _isFlying = e.isUsing;

        private bool ShouldStart()
        {
            if (IsRunning || GameServices.Progress == null || GameServices.Progress.IsTutorialCompleted) return false;

            // Stage 씬에서 바로 Play하면 CurrentStage가 null이라 튜토리얼을 띄우지 않음 (테스트는 ContextMenu 사용)
            StageDefinition stage = GameSession.CurrentStage;
            return stage != null && stage.stageId == _tutorialStageId;
        }

        // 데이터를 아직 불러오는 중이면 불러온 뒤 시작
        private void RequestStart(TutorialKind kind, bool isTestRun)
        {
            _tutorialKind = kind;
            _isTestRun = isTestRun;
            _hasPendingStart = false;
            TutorialSequence sequence = kind == TutorialKind.Basic ? _sequence : _flySequence;
            AsyncOperationHandle<TutorialSequence> loadHandle = kind == TutorialKind.Basic ? _loadHandle : _flyLoadHandle;
            if (sequence != null)
            {
                StartTutorial(isTestRun);
            }
            else if (loadHandle.IsValid() && !loadHandle.IsDone)
            {
                _hasPendingStart = true;
            }
        }

        private void StartTutorial(bool isTestRun)
        {
            _isTestRun = isTestRun;
            _remaining.Clear();
            TutorialSequence sequence = _tutorialKind == TutorialKind.Basic ? _sequence : _flySequence;
            foreach (TutorialStepData step in sequence.Steps)
            {
                if (step != null) _remaining.Enqueue(step);
            }

            _stepCount = _remaining.Count;
            _stepNumber = 0;
            ShowNext();
        }

        private void ShowNext()
        {
            if (_remaining.Count == 0)
            {
                Finish("완료");
                return;
            }

            _current = _remaining.Dequeue();
            _stepNumber++;
            LogInEditor($"{_stepNumber}/{_stepCount}: {_current.Message}");
            EventBus<TutorialStepChangedEvent>.Publish(new TutorialStepChangedEvent(_current.Message, _stepNumber, _stepCount));
        }

        // 마지막 단계 완료 또는 건너뛰기: 완료로 저장
        private void Finish(string reason)
        {
            if (!_isTestRun && GameServices.Progress != null)
            {
                if (_tutorialKind == TutorialKind.Basic) GameServices.Progress.IsTutorialCompleted = true;
                else GameServices.Progress.IsFlyTutorialCompleted = true;
            }
            LogInEditor($"종료({reason})");
            End();
        }

        // 저장하지 않고 끝냄
        private void Stop()
        {
            LogInEditor("종료(중단)");
            End();
        }

        private void End()
        {
            _current = null;
            _remaining.Clear();
            _isWaitingForNextStep = false;
            _stepWaitRemaining = 0f;
            EventBus<TutorialEndedEvent>.Publish(new TutorialEndedEvent());
        }

        private void Update()
        {
            if (!IsRunning || _isModalOpen) return;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && mouse != null && _skipKey != Key.None && keyboard[_skipKey].wasPressedThisFrame)
            {
                Finish("건너뛰기");
                return;
            }

            if (_isWaitingForNextStep)
            {
                _stepWaitRemaining -= Time.deltaTime;
                if (_stepWaitRemaining <= 0f)
                {
                    _isWaitingForNextStep = false;
                    ShowNext();
                }
                return;
            }

            if (keyboard == null || mouse == null) return;

            var inputContext = new TutorialInputContext(mouse, keyboard, _isFlying);
            if (_current.IsDone(in inputContext))
            {
                if (_remaining.Count == 0)
                {
                    Finish("완료");
                    return;
                }

                _isWaitingForNextStep = true;
                _stepWaitRemaining = _stepInterval;
                EventBus<TutorialStepCompletedEvent>.Publish(new TutorialStepCompletedEvent());
            }
        }

        // Play 중 인스펙터에서 컴포넌트 우클릭 메뉴로 실행. 스테이지·완료 여부와 관계없이 시작하고, 완료해도 저장하지 않음
        [ContextMenu("Start Tutorial (Test)")]
        private void StartTutorialForTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TutorialDataManager] Play 중에만 테스트할 수 있습니다.", this);
                return;
            }
            if (IsRunning) Stop();
            RequestStart(TutorialKind.Basic, true);
        }

        [ContextMenu("Start Fly Tutorial (Test)")]
        private void StartFlyTutorialForTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TutorialDataManager] Play 중에만 테스트할 수 있습니다.", this);
                return;
            }
            if (IsRunning) Stop();
            RequestStart(TutorialKind.Fly, true);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogInEditor(string message) => Debug.Log($"[Tutorial] {message}", this);
    }
}
