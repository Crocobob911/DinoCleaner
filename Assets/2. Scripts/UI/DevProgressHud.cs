using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    // 임시 HUD: 청소 진행도, 클리어 화면, 퇴근버튼
    public class DevProgressHud : MonoBehaviour
    {
        //[Tooltip("ICleanProgress를 구현한 컴포넌트 (구의 CleanableObject)")]
        //[SerializeField] MonoBehaviour target;
        
        [SerializeField] float toastDuration = 2f;

        StageState _state;
        ICleanProgress _progress;
        StageResult _result;

        string _toast;
        float _toastUntil;

        GUIStyle _style, _titleStyle, _toastStyle;

        void OnEnable()
        {
            EventBus<StageStateChangedEvent>.Subscribe(OnStateChanged);
            EventBus<DinoSpawnedEvent>.Subscribe(OnDinoArrived);
            EventBus<RegionCompleteEvent>.Subscribe(OnRegionComplete);
            EventBus<StageFinishedEvent>.Subscribe(OnStageFinished);
        }

        void OnDisable()
        {
            EventBus<StageStateChangedEvent>.Unsubscribe(OnStateChanged);
            EventBus<DinoSpawnedEvent>.Unsubscribe(OnDinoArrived);
            EventBus<RegionCompleteEvent>.Unsubscribe(OnRegionComplete);
            EventBus<StageFinishedEvent>.Unsubscribe(OnStageFinished);
        }

        void OnStateChanged(StageStateChangedEvent e) => _state = e.To;
        void OnDinoArrived(DinoSpawnedEvent e) => _progress = e.Progress;
        void OnStageFinished(StageFinishedEvent e) => _result = e.Result;

        void OnRegionComplete(RegionCompleteEvent e)
        {
            if (e.Source != _progress) return;   // 지금 세차 중인 공룡 것만
            _toast = $"{e.Region.DisplayName} CLEAN!";
            _toastUntil = Time.time + toastDuration;
        }

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 28 };
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };
                _toastStyle = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }

            if (Time.time < _toastUntil)
                GUI.Label(new Rect(0, 80, Screen.width, 80), _toast, _toastStyle);

            float y = 20;
            string stageName = GameSession.CurrentStage != null ? GameSession.CurrentStage.displayName : "(Test)";
            GUI.Label(new Rect(20, y, 900, 40), $"{stageName}  |  State: {_state}", _style);
            y += 44;

            switch (_state)
            {
                case StageState.Arriving:
                    GUI.Label(new Rect(20, y, 800, 40), "Customer arriving...", _style);
                    break;

                case StageState.Washing:
                    y = DrawProgress(y);
                    if (GUI.Button(new Rect(20, y + 10, 200, 50), "Finish Wash"))
                        EventBus<WashFinishRequestedEvent>.Publish(new WashFinishRequestedEvent());
                    break;

                case StageState.Judging:
                    GUI.Label(new Rect(20, y, 800, 40), "Judging...", _style);
                    break;

                case StageState.Result:
                    y = DrawResult(y);
                    if (GUI.Button(new Rect(20, y + 10, 200, 50), "Leave Work"))   // 퇴근
                        GameSession.GoHome();
                    break;
            }
        }

        float DrawProgress(float y)
        {
            if (_progress == null || !_progress.IsReady)
            {
                GUI.Label(new Rect(20, y, 800, 40), "Measuring...", _style);
                return y + 40;
            }

            GUI.Label(new Rect(20, y, 800, 40), $"Clean: {_progress.TotalProgress:P0}", _style);
            y += 44;
            foreach (var region in _progress.Regions)
            {
                string check = region.IsCompleted ? "[v]" : "[  ]";
                GUI.Label(new Rect(20, y, 800, 40), $"{check} {region.DisplayName}: {region.Progress:P0}", _style);
                y += 36;
            }
            return y;
        }

        float DrawResult(float y)
        {
            if (_result == null) return y;

            GUI.Label(new Rect(20, y, 800, 50), _result.Success ? "STAGE CLEAR!" : "FAILED...", _titleStyle); y += 56;
            GUI.Label(new Rect(20, y, 800, 40), $"Clean {_result.TotalProgress:P0} / Time {_result.WashTime:F1}s", _style); y += 40;
            GUI.Label(new Rect(20, y, 800, 40), $"Reward: {_result.Reward}", _style); y += 40;
            foreach (var region in _result.Regions)
            {
                GUI.Label(new Rect(20, y, 800, 40), $"  {region.DisplayName}: {region.Progress:P0}", _style);
                y += 36;
            }
            return y;
        }
    }
}