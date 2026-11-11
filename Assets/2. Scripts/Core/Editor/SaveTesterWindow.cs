using System.Collections.Generic;
using System.IO;
using DinoCleaner.UI;
using UnityEditor;
using UnityEngine;

namespace DinoCleaner.Core.Editor
{
    /// <summary>
    /// 저장 기능만 따로 눌러보는 창. 씬 없이 에디터에서 바로 동작하며, 실제 세이브 파일을 읽고 씁니다.
    /// 메뉴: DinoCleaner > Save > 세이브 테스트 창
    /// </summary>
    public class SaveTesterWindow : EditorWindow
    {
        private readonly Wallet wallet = new();
        private int amount = 100;
        private string lastResult = "";
        private Vector2 scroll;

        // Play 중이 아닐 때 업그레이드 정의·구매 로직을 쓰기 위한 진행 정보.
        // TemporaryGameServices가 실제 구현으로 바뀌면 이 부분만 교체
        private IGameProgress editModeProgress;

        private IGameProgress Progress
        {
            get
            {
                if (Application.isPlaying) return GameServices.Progress;
                return editModeProgress ??= new TemporaryGameServices();
            }
        }

        [MenuItem("DinoCleaner/Save/세이브 테스트 창")]
        private static void Open() => GetWindow<SaveTesterWindow>("Save Tester");

        [MenuItem("DinoCleaner/Save/업그레이드 기록만 초기화")]
        private static void ResetUpgradesMenu()
        {
            SaveSystem.Data.upgrades.Clear();
            SaveSystem.Save();
            Debug.Log("[SaveTester] 업그레이드 기록을 초기화했습니다. (돈은 그대로)");
            foreach (SaveTesterWindow window in Resources.FindObjectsOfTypeAll<SaveTesterWindow>()) window.Invalidate();
        }

        // 세이브가 메모리 밖에서 바뀌었을 때, 업그레이드 목록을 세이브에서 다시 읽도록
        private void Invalidate()
        {
            editModeProgress = null;
            Repaint();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawFileSection();
            DrawSeparator();
            DrawMoneySection();
            DrawSeparator();
            DrawUpgradeSection();

            if (!string.IsNullOrEmpty(lastResult)) EditorGUILayout.HelpBox(lastResult, MessageType.Info);
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play 중: 업그레이드 구매는 실제 게임 로직(GameServices.Progress)을 사용합니다. " +
                    "돈 버튼·다시 불러오기·삭제는 파일만 바꾸므로, 열린 UI와 게임 내 업그레이드 표시는 Play를 다시 시작해야 맞춰집니다.",
                    MessageType.Warning);
            }

            DrawSeparator();
            EditorGUILayout.LabelField("파일 내용 (디스크)", EditorStyles.boldLabel);
            string json = File.Exists(SaveSystem.FilePath) ? File.ReadAllText(SaveSystem.FilePath) : "(파일 없음)";
            EditorGUILayout.TextArea(json, GUILayout.MinHeight(120));

            EditorGUILayout.EndScrollView();
        }

        private void DrawFileSection()
        {
            EditorGUILayout.LabelField("세이브 파일", SaveSystem.FilePath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("파일 존재", File.Exists(SaveSystem.FilePath) ? "있음" : "없음 (새 게임 상태)");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("파일에서 다시 불러오기"))
                {
                    SaveSystem.Load();
                    Invalidate();
                    lastResult = $"다시 불러옴 → 돈 {wallet.Money}, 업그레이드 기록 {SaveSystem.Data.upgrades.Count}개";
                }
                if (GUILayout.Button("세이브 삭제 (새 게임)"))
                {
                    SaveSystem.DeleteSave();
                    Invalidate();
                    lastResult = "세이브 삭제됨 (다음 접근 시 새 게임)";
                }
                if (GUILayout.Button("폴더 열기"))
                {
                    EditorUtility.RevealInFinder(SaveSystem.SaveDirectory + "/");
                }
            }
        }

        private void DrawMoneySection()
        {
            EditorGUILayout.LabelField("돈", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("보유 돈", wallet.Money.ToString());
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
        }

        private void DrawUpgradeSection()
        {
            EditorGUILayout.LabelField("업그레이드", EditorStyles.boldLabel);

            IGameProgress progress = Progress;
            if (progress == null)
            {
                EditorGUILayout.HelpBox("GameServices.Progress가 아직 등록되지 않았습니다.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("구매: 실제 상점과 같은 로직(돈 확인 → 단계 +1 → 돈 차감 → 저장). 단계 직접 수정: 돈 변화 없이 세이브만 고침.",
                EditorStyles.wordWrappedMiniLabel);

            IReadOnlyList<UpgradeInfo> upgrades = progress.Upgrades;
            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeInfo u = upgrades[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{u.DisplayName} ({u.Id})", GUILayout.Width(170));
                    EditorGUILayout.LabelField($"Lv {u.Level}/{u.MaxLevel}", GUILayout.Width(70));
                    EditorGUILayout.LabelField(u.IsMaxLevel ? "최대" : $"다음 {u.NextCost}", GUILayout.Width(70));

                    using (new EditorGUI.DisabledScope(u.IsMaxLevel))
                    {
                        if (GUILayout.Button("구매", GUILayout.Width(50)))
                        {
                            int before = progress.Money;
                            bool ok = progress.TryUpgrade(u.Id);
                            lastResult = ok
                                ? $"{u.DisplayName} 구매 성공: Lv {u.Level} → {u.Level + 1}, 돈 {before} → {progress.Money}"
                                : $"{u.DisplayName} 구매 실패 (돈 부족: 보유 {before}, 필요 {u.NextCost})";
                        }
                    }

                    // Play 중에는 실행 중인 게임 상태와 어긋나므로 직접 수정 막음
                    using (new EditorGUI.DisabledScope(Application.isPlaying))
                    {
                        if (GUILayout.Button("-", GUILayout.Width(22))) SetLevelDirect(u, u.Level - 1);
                        if (GUILayout.Button("+", GUILayout.Width(22))) SetLevelDirect(u, u.Level + 1);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("업그레이드 기록만 초기화 (돈은 그대로)")) ResetUpgradesMenu();
            }
        }

        private void SetLevelDirect(UpgradeInfo u, int level)
        {
            level = Mathf.Clamp(level, 0, u.MaxLevel);
            SaveSystem.Data.SetUpgradeLevel(u.Id, level);
            SaveSystem.Save();
            Invalidate();
            lastResult = $"{u.DisplayName} 단계를 {level}로 직접 수정 (돈 변화 없음)";
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
            EditorGUILayout.Space(4);
        }

        private void OnInspectorUpdate() => Repaint();
    }
}
