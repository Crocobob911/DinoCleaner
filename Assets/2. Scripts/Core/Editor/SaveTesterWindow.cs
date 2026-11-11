using System.Collections.Generic;
using System.IO;
using DinoCleaner.Cleaning;
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
            DrawTutorialSection();
            DrawSeparator();
            DrawStageSection();
            DrawSeparator();
            DrawSlotSection();
            DrawSeparator();
            DrawUpgradeSection();
            DrawSeparator();
            DrawSettingsSection();
            DrawSeparator();
            DrawDirtMaskSection();

            if (!string.IsNullOrEmpty(lastResult)) EditorGUILayout.HelpBox(lastResult, MessageType.Info);
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play 중: 업그레이드 구매는 실제 게임 로직(GameServices.Progress)을 사용합니다. " +
                    "돈 버튼·다시 불러오기·삭제는 파일만 바꾸므로, 열린 UI와 게임 내 업그레이드 표시는 Play를 다시 시작해야 맞춰집니다.",
                    MessageType.Warning);
            }

            DrawSeparator();
            EditorGUILayout.LabelField("save.json (게임 진행)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(ReadOrNone(SaveSystem.FilePath), GUILayout.MinHeight(120));
            EditorGUILayout.LabelField("settings.json (설정)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(ReadOrNone(SettingsSystem.FilePath), GUILayout.MinHeight(70));

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

        private void DrawTutorialSection()
        {
            EditorGUILayout.LabelField("튜토리얼", EditorStyles.boldLabel);
            bool completed = SaveSystem.Data.tutorialCompleted;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("완료 여부", completed ? "완료 (다시 안 뜸)" : "미완료 (스테이지에서 뜸)");
                if (GUILayout.Button(completed ? "미완료로 되돌리기" : "완료로 표시", GUILayout.Width(130)))
                {
                    // Play 중에는 실제 게임 경로(GameServices.Progress)로 바꿔야 실행 중인 게임에도 반영됨
                    if (Application.isPlaying && GameServices.Progress != null) GameServices.Progress.IsTutorialCompleted = !completed;
                    else
                    {
                        SaveSystem.Data.tutorialCompleted = !completed;
                        SaveSystem.Save();
                    }
                    lastResult = $"튜토리얼 완료 여부 → {!completed}";
                }
            }
        }

        private void DrawStageSection()
        {
            EditorGUILayout.LabelField("공룡(스테이지)별 클리어", EditorStyles.boldLabel);

            // 프로젝트의 모든 StageDefinition (stageId 순)
            var stages = new List<StageDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:StageDefinition"))
            {
                var s = AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (s != null) stages.Add(s);
            }
            stages.Sort((a, b) => a.stageId.CompareTo(b.stageId));

            var seen = new HashSet<int>();
            foreach (StageDefinition stage in stages)
            {
                bool cleared = SaveSystem.Data.IsStageCleared(stage.stageId);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"[{stage.stageId}] {stage.displayName} ({stage.name})", GUILayout.Width(260));
                    EditorGUILayout.LabelField(cleared ? "클리어" : "미클리어", GUILayout.Width(60));

                    using (new EditorGUI.DisabledScope(stage.stageId <= 0))
                    {
                        if (GUILayout.Button(cleared ? "기록 지우기" : "클리어 처리", GUILayout.Width(90)))
                        {
                            if (cleared) SaveSystem.Data.clearedStageIds.Remove(stage.stageId);
                            else SaveSystem.Data.MarkStageCleared(stage.stageId);
                            SaveSystem.Save();
                            lastResult = $"[{stage.stageId}] {stage.displayName} → {(cleared ? "미클리어" : "클리어")}";
                        }
                    }
                }

                if (stage.stageId <= 0)
                    EditorGUILayout.HelpBox($"{stage.name}: Stage Id가 0이라 클리어가 기록되지 않습니다. Inspector에서 지정하세요.", MessageType.Warning);
                else if (!seen.Add(stage.stageId))
                    EditorGUILayout.HelpBox($"{stage.name}: Stage Id {stage.stageId}가 다른 스테이지와 겹칩니다. 클리어 기록이 섞입니다.", MessageType.Error);
            }

            if (stages.Count == 0) EditorGUILayout.LabelField("StageDefinition 에셋이 없습니다.");
        }

        private void DrawSlotSection()
        {
            EditorGUILayout.LabelField("공룡별 진행도 슬롯 (save.json + Dirt/dirt_{ID}.png)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("저장 기능만 있음. 언제 저장·삭제할지(이어하기)는 스테이지 쪽에서 연결 예정.", EditorStyles.wordWrappedMiniLabel);

            List<StageSlot> slots = SaveSystem.Data.stageSlots;
            if (slots.Count == 0)
            {
                EditorGUILayout.LabelField("슬롯 없음");
                return;
            }

            foreach (StageSlot slot in slots.ToArray())
            {
                bool hasDirt = File.Exists(StageSlots.DirtPath(slot.stageId));
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        $"[{slot.stageId}] 진행도 {slot.progress:P0} · 청소 {slot.washTime:F0}초 · 부위 {slot.regions.Count}개 · 오물 지도 {(hasDirt ? "있음" : "없음")}");

                    if (GUILayout.Button("슬롯 삭제", GUILayout.Width(80)))
                    {
                        StageSlots.Delete(slot.stageId);
                        lastResult = $"[{slot.stageId}] 진행도 슬롯과 오물 지도 파일 삭제";
                    }
                    if (hasDirt && GUILayout.Button("PNG", GUILayout.Width(45)))
                    {
                        EditorUtility.RevealInFinder(StageSlots.DirtPath(slot.stageId));
                    }
                }

                foreach (RegionSlot r in slot.regions)
                    EditorGUILayout.LabelField($"      부위 [{r.id}] {r.progress:P0}{(r.completed ? " ✔완료" : "")}");
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

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("설정 (settings.json — 새 게임을 해도 유지)", EditorStyles.boldLabel);

            // Play 중에는 실제 게임 경로(GameServices.Settings)로 바꿔야 실행 중인 게임에도 반영됨
            IGameSettings live = Application.isPlaying ? GameServices.Settings : null;
            SettingsData s = SettingsSystem.Data;

            int bgm = EditorGUILayout.IntSlider("BGM", s.bgmVolume, SettingsData.MinValue, SettingsData.MaxValue);
            int sfx = EditorGUILayout.IntSlider("SFX", s.sfxVolume, SettingsData.MinValue, SettingsData.MaxValue);
            int sens = EditorGUILayout.IntSlider("마우스 감도", s.mouseSensitivity, SettingsData.MinValue, SettingsData.MaxValue);

            if (bgm != s.bgmVolume || sfx != s.sfxVolume || sens != s.mouseSensitivity)
            {
                if (live != null)
                {
                    live.BgmVolume = bgm;
                    live.SfxVolume = sfx;
                    live.MouseSensitivity = sens;
                }
                else
                {
                    s.bgmVolume = bgm;
                    s.sfxVolume = sfx;
                    s.mouseSensitivity = sens;
                    SettingsSystem.MarkDirty();
                }
            }

            EditorGUILayout.LabelField("저장 대기", SettingsSystem.IsDirty ? "바뀐 값 있음 (아직 파일에 안 씀)" : "없음");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!SettingsSystem.IsDirty))
                {
                    if (GUILayout.Button("지금 저장 (창 닫기와 같은 효과)"))
                    {
                        SettingsSystem.SaveIfDirty();
                        lastResult = "설정을 settings.json에 저장";
                    }
                }
                if (GUILayout.Button("파일에서 다시 불러오기"))
                {
                    SettingsSystem.Load();
                    lastResult = "설정을 파일에서 다시 불러옴 (저장 안 한 변경은 버려짐)";
                }
                if (GUILayout.Button("기본값으로"))
                {
                    SettingsSystem.ResetToDefault();
                    lastResult = "설정 파일 삭제 → 기본값 (BGM 50, SFX 65, 감도 40)";
                }
            }
        }

        // ===== [기술 검증] 오물 지도 저장/불러오기 =====

        private static string DirtTestPath(CleanableObject obj) =>
            Path.Combine(SaveSystem.SaveDirectory, "DirtTest", obj.name.Replace("(Clone)", "").Trim() + ".png");

        private void DrawDirtMaskSection()
        {
            EditorGUILayout.LabelField("[기술 검증] 오물 지도 + 부위별 진행도 저장/불러오기", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "① Play → 공룡을 반쯤 닦기 → 저장   ② Play 종료 후 다시 Play → 공룡 도착 후 불러오기   ③ 모양·전체 진행도·부위별 진행도가 저장할 때와 같은지 비교",
                EditorStyles.wordWrappedMiniLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play 중에만 동작합니다. (오물 지도는 공룡이 생성될 때 만들어짐)", MessageType.Info);
                return;
            }

            CleanableObject[] targets = Object.FindObjectsByType<CleanableObject>(FindObjectsSortMode.None);
            if (targets.Length == 0)
            {
                EditorGUILayout.LabelField("씬에 공룡(CleanableObject)이 없습니다. 도착할 때까지 기다리세요.");
                return;
            }

            foreach (CleanableObject target in targets)
            {
                string path = DirtTestPath(target);
                DirtMaskSnapshot.Info saved = DirtMaskSnapshot.ReadInfo(path);

                EditorGUILayout.LabelField(target.name, EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("전체 진행도",
                    $"지금 {(target.IsReady ? $"{target.TotalProgress:P1}" : "측정 중…")}   |   저장할 때 {(saved != null ? $"{saved.totalProgress:P1}" : "-")}");

                // 부위별: 지금 vs 저장할 때
                foreach (RegionProgress r in target.Regions)
                {
                    DirtMaskSnapshot.RegionRecord old = saved?.regions.Find(x => x.id == r.RegionId);
                    string now = $"{r.Progress:P0}{(r.IsCompleted ? " ✔완료" : "")}";
                    string then = old != null ? $"{old.progress:P0}{(old.completed ? " ✔완료" : "")}" : "-";
                    EditorGUILayout.LabelField($"   [{r.RegionId}] {r.DisplayName}", $"지금 {now}   |   저장할 때 {then}");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!target.IsReady))
                    {
                        if (GUILayout.Button("오물 저장"))
                        {
                            lastResult = Describe(target, DirtMaskSnapshot.Save(target, path));
                        }
                    }

                    // 첫 측정(전부 진흙 기준)이 끝난 뒤에 불러와야 진행도가 0%로 돌아가지 않음
                    using (new EditorGUI.DisabledScope(!File.Exists(path) || !target.IsReady))
                    {
                        if (GUILayout.Button("오물 불러오기"))
                        {
                            DirtMaskSnapshot.Result r = DirtMaskSnapshot.Load(target, path);
                            lastResult = Describe(target, r) +
                                (r.Ok ? "\n0.25초쯤 뒤 '지금'이 '저장할 때'와 같아지는지 확인하세요. 이미 완료됐던 부위는 완료 알림이 다시 뜨지 않아야 합니다." : "");
                        }
                    }

                    using (new EditorGUI.DisabledScope(!File.Exists(path)))
                    {
                        if (GUILayout.Button("파일 위치"))
                        {
                            EditorUtility.RevealInFinder(path);
                        }
                    }
                }
            }
        }

        private static string Describe(CleanableObject target, DirtMaskSnapshot.Result r) =>
            r.Ok ? $"[{target.name}] {r.Message} — 파일 {r.Bytes / 1024f:F0} KB, {r.Milliseconds:F1} ms"
                 : $"[{target.name}] {r.Message}";

        private static string ReadOrNone(string path) => File.Exists(path) ? File.ReadAllText(path) : "(파일 없음)";

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
