using System.IO;
using UnityEditor;
using UnityEngine;

namespace DinoCleaner.Core.Editor
{
    /// <summary>
    /// 저장 기능만 따로 눌러보는 창. 씬이나 Play 없이 에디터에서 바로 동작하며, 실제 세이브 파일을 읽고 씁니다.
    /// 메뉴: DinoCleaner > Save > 세이브 테스트 창
    /// </summary>
    public class SaveTesterWindow : EditorWindow
    {
        private readonly Wallet wallet = new();
        private int amount = 100;
        private string lastResult = "";
        private Vector2 scroll;

        [MenuItem("DinoCleaner/Save/세이브 테스트 창")]
        private static void Open() => GetWindow<SaveTesterWindow>("Save Tester");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("세이브 파일", SaveSystem.FilePath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("파일 존재", File.Exists(SaveSystem.FilePath) ? "있음" : "없음 (새 게임 상태)");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("메모리 위 보유 돈", wallet.Money.ToString(), EditorStyles.boldLabel);
            amount = EditorGUILayout.IntField("금액", amount);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Income (+금액)"))
                {
                    wallet.Income(amount);
                    lastResult = $"Income {amount} → {wallet.Money}";
                }
                if (GUILayout.Button("TryConsume (-금액)"))
                {
                    bool ok = wallet.TryConsume(amount);
                    lastResult = ok ? $"Consume {amount} 성공 → {wallet.Money}" : $"Consume {amount} 실패 (돈 부족) → {wallet.Money}";
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("파일에서 다시 불러오기"))
                {
                    SaveSystem.Load();
                    lastResult = $"다시 불러옴 → {wallet.Money}";
                }
                if (GUILayout.Button("세이브 삭제"))
                {
                    SaveSystem.DeleteSave();
                    lastResult = "세이브 삭제됨 (다음 접근 시 새 게임)";
                }
                if (GUILayout.Button("폴더 열기"))
                {
                    EditorUtility.RevealInFinder(SaveSystem.SaveDirectory + "/");
                }
            }

            if (!string.IsNullOrEmpty(lastResult)) EditorGUILayout.HelpBox(lastResult, MessageType.Info);
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play 중에 여기서 돈을 바꾸면 값과 파일은 바뀌지만, 이미 열린 상점 UI는 다시 열어야 갱신됩니다.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("파일 내용 (디스크)", EditorStyles.boldLabel);
            string json = File.Exists(SaveSystem.FilePath) ? File.ReadAllText(SaveSystem.FilePath) : "(파일 없음)";
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(json, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void OnInspectorUpdate() => Repaint();
    }
}
