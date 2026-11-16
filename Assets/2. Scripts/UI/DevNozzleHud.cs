using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    // 임시 노즐 HUD: 현재 노즐 정보 + 화면 중앙 조준점
    public class DevNozzleHud : MonoBehaviour
    {
        CleaningToolData _nozzle;
        GUIStyle _style, _titleStyle, _crossStyle;

        void OnEnable() => EventBus<NozzleChangedEvent>.Subscribe(OnNozzleChanged);
        void OnDisable() => EventBus<NozzleChangedEvent>.Unsubscribe(OnNozzleChanged);

        void OnNozzleChanged(NozzleChangedEvent e) => _nozzle = e.currentTool;

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold };
                _crossStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
            }

            // 조준점
            GUI.Label(new Rect(Screen.width / 2f - 20, Screen.height / 2f - 20, 40, 40), "+", _crossStyle);

            float y = Screen.height - 130;

            if (_nozzle == null)
            {
                GUI.Label(new Rect(20, y, 600, 40), "Nozzle: (none)", _titleStyle);
                return;
            }

            //if (_nozzle.icon != null)
            //    GUI.DrawTexture(new Rect(20, y - 74, 64, 64), _nozzle.icon.texture, ScaleMode.ScaleToFit);

            GUI.Label(new Rect(20, y, 700, 44), $"Nozzle: {_nozzle.toolName}", _titleStyle);
            y += 44;
            GUI.Label(new Rect(20, y, 900, 32),
                      $"Flags: {_nozzle.Flags}   Strength: {_nozzle.Strength}   Radius: {_nozzle.Radius}m", _style);
            y += 32;
            GUI.Label(new Rect(20, y, 900, 32), "[1~9] change nozzle   [Left Click] spray", _style);
        }
    }
}