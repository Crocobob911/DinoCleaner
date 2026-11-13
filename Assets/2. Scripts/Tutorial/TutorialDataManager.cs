using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DinoCleaner.Tutorial
{
    // Stage 씬에 하나 둔다. 튜토리얼 스테이지의 세차가 시작되면 단계 데이터를 하나씩 꺼내 UI에 알린다.
    // 단계 목록은 Addressables의 TutorialSequence에서 불러온다.
    // UI는 이벤트(TutorialStepChangedEvent, TutorialEndedEvent)만 받아 표시하고, 입력 판정은 여기서 한다.
    public class TutorialDataManager : MonoBehaviour
    {
        [Tooltip("튜토리얼을 진행할 스테이지의 StageDefinition.stageId (스테고사우루스 = Stage_1)")]
        [SerializeField] private int _tutorialStageId = 1;

        [Tooltip("튜토리얼 전체를 건너뛰는 키. 바꾸면 UI의 '건너뛰기 [F]' 문구도 같이 바꿀 것")]
        [SerializeField] private Key _skipKey = Key.F;

        private readonly Queue<TutorialStepData> _remaining = new();
        private AsyncOperationHandle<TutorialSequence> _loadHandle;
        private TutorialSequence _sequence;
        private TutorialStepData _current;
        private int _stepNumber;
        private int _stepCount;
        private bool _isModalOpen;
        private bool _isTestRun;
        private bool _hasPendingStart;

        private bool IsRunning => _current != null;

        private void Awake()
        {
            _loadHandle = Addressables.LoadAssetAsync<TutorialSequence>(TutorialSequence.Address);
            _loadHandle.Completed += OnSequenceLoaded;
        }

        private void OnDestroy()
        {
            if (_loadHandle.IsValid()) Addressables.Release(_loadHandle);
        }

        private void OnEnable()
        {
            EventBus<StageStateChangedEvent>.Subscribe(OnStageStateChanged);
            EventBus<UIModalChangedEvent>.Subscribe(OnModalChanged);
        }

        private void OnDisable()
        {
            EventBus<StageStateChangedEvent>.Unsubscribe(OnStageStateChanged);
            EventBus<UIModalChangedEvent>.Unsubscribe(OnModalChanged);
        }

        private void OnSequenceLoaded(AsyncOperationHandle<TutorialSequence> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogWarning($"[TutorialDataManager] Addressables 주소 '{TutorialSequence.Address}'를 불러오지 못해 튜토리얼 없이 진행합니다.", this);
                _hasPendingStart = false;
                return;
            }

            _sequence = handle.Result;
            if (_hasPendingStart)
            {
                _hasPendingStart = false;
                StartTutorial(_isTestRun);
            }
        }

        private void OnStageStateChanged(StageStateChangedEvent e)
        {
            if (e.To == StageState.Washing)
            {
                if (ShouldStart()) RequestStart(false);
            }
            else
            {
                // 세차가 끝나 버리면 완료로 치지 않음. 다음에 처음부터 다시 함
                _hasPendingStart = false;
                if (IsRunning) Stop();
            }
        }

        private void OnModalChanged(UIModalChangedEvent e) => _isModalOpen = e.IsOpen;

        private bool ShouldStart()
        {
            if (IsRunning || GameServices.Progress == null || GameServices.Progress.IsTutorialCompleted) return false;

            // Stage 씬에서 바로 Play하면 CurrentStage가 null이라 튜토리얼을 띄우지 않음 (테스트는 ContextMenu 사용)
            StageDefinition stage = GameSession.CurrentStage;
            return stage != null && stage.stageId == _tutorialStageId;
        }

        // 데이터를 아직 불러오는 중이면 불러온 뒤 시작
        private void RequestStart(bool isTestRun)
        {
            _isTestRun = isTestRun;
            if (_sequence != null)
            {
                StartTutorial(isTestRun);
            }
            else if (_loadHandle.IsValid() && !_loadHandle.IsDone)
            {
                _hasPendingStart = true;
            }
        }

        private void StartTutorial(bool isTestRun)
        {
            _isTestRun = isTestRun;
            _remaining.Clear();
            foreach (TutorialStepData step in _sequence.Steps)
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
            if (!_isTestRun && GameServices.Progress != null) GameServices.Progress.IsTutorialCompleted = true;
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
            EventBus<TutorialEndedEvent>.Publish(new TutorialEndedEvent());
        }

        private void Update()
        {
            if (!IsRunning || _isModalOpen) return;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (_skipKey != Key.None && keyboard[_skipKey].wasPressedThisFrame)
            {
                Finish("건너뛰기");
                return;
            }

            if (_current.IsDone(mouse, keyboard)) ShowNext();
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
            RequestStart(true);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogInEditor(string message) => Debug.Log($"[Tutorial] {message}", this);
    }
}
