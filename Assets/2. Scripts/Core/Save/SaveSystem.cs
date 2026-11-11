using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("DinoCleaner.Core.Tests")]

namespace DinoCleaner.Core
{
    /// <summary>
    /// SaveData를 Application.persistentDataPath/save.json 에 읽고 씁니다.
    /// 처음 Data에 접근할 때 자동으로 불러오고, 저장은 값이 바뀐 쪽에서 Save()를 호출합니다.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        private static SaveData data;

        // 자동 테스트가 실제 세이브를 덮어쓰지 않도록 임시 폴더를 쓰게 할 때만 설정. 게임 코드에서는 건드리지 말 것
        internal static string DirectoryOverride;

        public static string SaveDirectory => DirectoryOverride ?? Application.persistentDataPath;
        public static string FilePath => Path.Combine(SaveDirectory, FileName);

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            data = new SaveData();
            string path = FilePath;
            if (!File.Exists(path)) return;

            try
            {
                // 새 객체에 덮어쓰므로, 파일에 없는 필드는 SaveData의 초기값이 유지됨
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), data);
                Migrate(data);
            }
            catch (Exception e)
            {
                string brokenPath = Path.Combine(SaveDirectory,
                    $"save_broken_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                Debug.LogError($"[SaveSystem] 세이브 파일을 읽지 못해 새 게임으로 시작합니다. 원본은 {brokenPath} 로 옮겼습니다.\n{e}");
                TryMove(path, brokenPath);
                data = new SaveData();
            }
        }

        public static void Save()
        {
            if (data == null) return; // 불러온 적도 바꾼 적도 없음

            data.version = SaveData.CurrentVersion;
            string path = FilePath;
            string tempPath = path + ".tmp";

            try
            {
                // 임시 파일에 다 쓴 뒤 교체해서, 쓰는 도중 꺼져도 기존 세이브가 깨지지 않게 함
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Replace(tempPath, path, null);
                else File.Move(tempPath, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 저장에 실패했습니다: {path}\n{e}");
            }
        }

        public static void DeleteSave()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            data = null;
        }

        // 예전 버전 세이브를 현재 구조로 바꿈. CurrentVersion을 올릴 때 여기에 단계를 추가
        private static void Migrate(SaveData loaded)
        {
            // 예) if (loaded.version < 2) { ...; loaded.version = 2; }
        }

        private static void TryMove(string from, string to)
        {
            try { File.Move(from, to); }
            catch (Exception e) { Debug.LogWarning($"[SaveSystem] 깨진 세이브를 옮기지 못했습니다.\n{e}"); }
        }

        // Domain Reload를 꺼도 지난 Play의 값이 남지 않도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            data = null;
            DirectoryOverride = null;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("DinoCleaner/Save/세이브 삭제 (새 게임)")]
        private static void DeleteSaveMenu()
        {
            DeleteSave();
            Debug.Log($"[SaveSystem] 세이브를 삭제했습니다: {FilePath}");
        }

        [UnityEditor.MenuItem("DinoCleaner/Save/세이브 폴더 열기")]
        private static void OpenSaveFolderMenu()
        {
            UnityEditor.EditorUtility.RevealInFinder(SaveDirectory + "/");
        }
#endif
    }
}
