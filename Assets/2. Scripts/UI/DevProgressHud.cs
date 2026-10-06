using DinoWash.Core;
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

        void OnGUI()
        {
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 32 };

            string text = _progress.IsReady
                ? $"Clean: {_progress.TotalProgress:P0}"
                : "Measuring...";

            GUI.Label(new Rect(20, 20, 600, 50), text, _style);
            GUI.Label(new Rect(20, 60, 600, 50), $"Updates: {_updateCount}", _style);
        }
    }
}