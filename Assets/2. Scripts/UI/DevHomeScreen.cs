using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    // Home 씬의 임시 레벨 선택 화면. 버튼을 누르면 그 스테이지로 들어간다.
    public class DevHomeScreen : MonoBehaviour
    {
        [Tooltip("stage 선택 화면에 나올 스테이지들 (위에서부터 순서대로)")]
        [SerializeField] StageDefinition[] stages;

        GUIStyle _titleStyle;

        void OnGUI()
        {
            if (_titleStyle == null)
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };

            float y = 20;
            GUI.Label(new Rect(20, y, 800, 50), "HOME - Select Stage", _titleStyle);
            y += 70;

            if (stages == null) return;

            foreach (var stage in stages)
            {
                if (stage == null) continue;

                if (GUI.Button(new Rect(20, y, 360, 56), stage.displayName))
                    GameSession.EnterStage(stage);

                y += 66;
            }
        }
    }
}