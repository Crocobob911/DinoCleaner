using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    // 화면 왼쪽 위에 청소율을 표시하는 임시 HUD
    public class DevProgressHud : MonoBehaviour
    {
        [Tooltip("ICleanProgress를 구현한 컴포넌트 (구의 CleanableObject)")]
        [SerializeField] MonoBehaviour target;
        [SerializeField] float toastDuration = 2f;

        ICleanProgress _progress;
        int _updateCount;
        GUIStyle _style;
        GUIStyle _toastStyle;

        string _toast;          // 화면에 띄울 알림 문구
        float _toastUntil;      // 알림을 지울 시각


        void OnEnable()
        {
            _progress = target as ICleanProgress;
            if (_progress == null)
            {
                Debug.LogError("[DevProgressHud] target이 ICleanProgress가 아닙니다.", this);
                enabled = false;
                return;
            }
            _progress.Changed += OnChanged;

            // 이벤트 버스 구독해서 부위 완료 시 화면 업데이트 연결
            EventBus<RegionCompleteEvent>.Subscribe(OnRegionComplete);
        }

        void OnDisable()
        {
            if (_progress != null) _progress.Changed -= OnChanged;
            EventBus<RegionCompleteEvent>.Unsubscribe(OnRegionComplete);
        }

        void OnChanged(ICleanProgress progress) => _updateCount++;

        void OnRegionComplete(RegionCompleteEvent e)
        {
            _toast = $"{e.Region.DisplayName} CLEAN!";
            _toastUntil = Time.time + toastDuration;
            Debug.Log($"[HUD] 부위 완료 이벤트 받음: {e.Region.DisplayName} ({e.Region.Progress:P0})");
        }

        // 디버그용 간단한 GUI. 실제 UI에선 Unity UI로 교체 필요
        void OnGUI()
        {
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 32 };
            if (_toastStyle == null)
                _toastStyle = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            // 완료 알림 (화면 가운데 위쪽)
            if (Time.time < _toastUntil)
                GUI.Label(new Rect(0, 80, Screen.width, 80), _toast, _toastStyle);

            float y = 20;
            if (!_progress.IsReady)
            {
                GUI.Label(new Rect(20, y, 800, 50), "Measuring...", _style);
                return;
            }

            GUI.Label(new Rect(20, y, 800, 50), $"Clean: {_progress.TotalProgress:P0}   (Updates: {_updateCount})", _style);
            y += 50;

            foreach (var region in _progress.Regions)
            {
                string check = region.IsCompleted ? "[v]" : "[  ]";
                GUI.Label(new Rect(20, y, 800, 50), $"{check} {region.DisplayName}: {region.Progress:P0}", _style);
                y += 40;
            }
        }
    }
}