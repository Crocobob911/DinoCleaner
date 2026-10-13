using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    // 화면 왼쪽 위에 청소율을 표시하는 임시 HUD
    public class DevProgressHud : MonoBehaviour
    {
        [Tooltip("ICleanProgress를 구현한 컴포넌트 (구의 CleanableObject)")]
        [SerializeField] MonoBehaviour target;

        ICleanProgress _progress;
        int _updateCount;
        GUIStyle _style;

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
        }

        void OnDisable()
        {
            if (_progress != null) _progress.Changed -= OnChanged;
        }

        void OnChanged(ICleanProgress progress) => _updateCount++;

        // 디버그용 간단한 GUI. 실제 UI에선 Unity UI로 교체 필요
        void OnGUI()
        {
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 32 };

            float y = 20;
            if (!_progress.IsReady)
            {
                GUI.Label(new Rect(20, y, 800, 50), "Measuring...", _style);
                return;
            }

            GUI.Label(new Rect(20, y, 800, 50), $"Clean: {_progress.TotalProgress:P0}   (Updates: {_updateCount})", _style);
            y += 50;

            // 부위별 줄
            foreach (var region in _progress.Regions)
            {
                string check = region.IsCompleted ? "[v]" : "[  ]";
                GUI.Label(new Rect(20, y, 800, 50), $"{check} {region.DisplayName}: {region.Progress:P0}", _style);
                y += 40;
            }
        }
    }
}