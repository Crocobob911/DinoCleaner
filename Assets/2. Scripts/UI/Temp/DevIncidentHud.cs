using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    // 임시: 돌발 상황이 진행 중이면 화면 위쪽에 경고 표시. 정식 UI가 생기면 삭제.
    public class DevIncidentHud : MonoBehaviour
    {
        IncidentDefinition _current;
        GUIStyle _style;

        void OnEnable()
        {
            EventBus<IncidentStartedEvent>.Subscribe(OnStarted);
            EventBus<IncidentEndedEvent>.Subscribe(OnEnded);
        }

        void OnDisable()
        {
            EventBus<IncidentStartedEvent>.Unsubscribe(OnStarted);
            EventBus<IncidentEndedEvent>.Unsubscribe(OnEnded);
        }

        void OnStarted(IncidentStartedEvent e) => _current = e.Incident;
        void OnEnded(IncidentEndedEvent e) => _current = null;

        void OnGUI()
        {
            if (_current == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 36,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                _style.normal.textColor = Color.red;
            }

            GUI.Label(new Rect(0, 140, Screen.width, 100), $"!! {_current.displayName} !!\n{_current.warning}", _style);
        }
    }
}
