using DinoCleaner.Core;
using DinoCleaner.Data;
using UnityEngine;

namespace DinoCleaner.UI
{
    // 임시 도구 HUD: 지금 든 도구와 청소기 상태(물/거품, 길이, 너비, 방향)를 화면 왼쪽 아래에 표시.
    // 이벤트만 받으므로 Player/Equipment 코드를 몰라도 된다. 정식 HUD가 생기면 삭제.
    public class DevToolHud : MonoBehaviour
    {
        ToolType _tool = ToolType.CleaningTool;
        bool _flying;

        bool _hasState;                          // 청소기 상태를 한 번이라도 받았는지
        CleaningToolStateChangedEvent _state;

        GUIStyle _style, _titleStyle;

        void OnEnable()
        {
            EventBus<CleaningToolStateChangedEvent>.Subscribe(OnStateChanged);
            EventBus<CurrentToolChangedEvent>.Subscribe(OnToolChanged);
            EventBus<UsingFairyDinoEvent>.Subscribe(OnFairyChanged);
        }

        void OnDisable()
        {
            EventBus<CleaningToolStateChangedEvent>.Unsubscribe(OnStateChanged);
            EventBus<CurrentToolChangedEvent>.Unsubscribe(OnToolChanged);
            EventBus<UsingFairyDinoEvent>.Unsubscribe(OnFairyChanged);
        }

        void OnStateChanged(CleaningToolStateChangedEvent e)
        {
            _state = e;
            _hasState = true;
        }

        void OnToolChanged(CurrentToolChangedEvent e) => _tool = e.toolType;
        void OnFairyChanged(UsingFairyDinoEvent e) => _flying = e.isUsing;

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
            }

            const float x = 20f;
            float y = Screen.height - 200f;
            GUI.Box(new Rect(x - 10f, y - 10f, 640f, 190f), GUIContent.none);

            GUI.Label(new Rect(x, y, 620f, 40f), $"Tool: {_tool}{(_flying ? "   (Flying)" : "")}", _titleStyle);
            y += 42f;

            if (_tool != ToolType.CleaningTool)
            {
                GUI.Label(new Rect(x, y, 620f, 30f), "(not a cleaning tool)", _style);
                return;
            }

            if (!_hasState)
            {
                // CleaningTool은 키를 눌러 상태가 바뀔 때만 이벤트를 보냄
                GUI.Label(new Rect(x, y, 620f, 30f), "No state yet - press 1 / 2 / R / Wheel", _style);
                y += 30f;
                DrawKeyGuide(x, y);
                return;
            }

            string mode = (_state.flags & CleanerFlags.Bubble) != 0 ? "BUBBLE" : "WATER";
            GUI.Label(new Rect(x, y, 620f, 30f),
                      $"Mode: {mode}    Flags: {_state.flags}    Cast: {_state.castShape}", _style);
            y += 30f;

            GUI.Label(new Rect(x, y, 620f, 30f),
                      $"Length: {_state.CurrentLength}    Width: {Bar(_state.CurrentWidth, _state.MaxWidthLimit)} {_state.CurrentWidth}/{_state.MaxWidthLimit}",
                      _style);
            y += 30f;

            string dir = _state.Direction == NozzleDirection.Horizontal ? "Horizontal (---)" : "Vertical ( | )";
            GUI.Label(new Rect(x, y, 620f, 30f), $"Direction: {dir}", _style);
            y += 30f;

            DrawKeyGuide(x, y);
        }

        void DrawKeyGuide(float x, float y)
        {
            GUI.Label(new Rect(x, y, 620f, 30f), "[1] Water/Bubble   [2] Length   [R] Direction   [Wheel] Width", _style);
        }

        // 예: Bar(2, 4) → "[##--]"
        static string Bar(int current, int max)
        {
            max = Mathf.Max(max, 1);
            current = Mathf.Clamp(current, 0, max);
            return "[" + new string('#', current) + new string('-', max - current) + "]";
        }
    }
}
