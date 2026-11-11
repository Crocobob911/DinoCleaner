using System.IO;
using UnityEngine;

namespace DinoCleaner.Core
{
    /// <summary>
    /// SettingsData를 settings.json 에 읽고 씁니다. (save.json과 같은 폴더)
    /// 슬라이더를 끄는 동안 매번 쓰지 않도록, 값을 바꾸면 MarkDirty()만 하고
    /// 설정 창이 닫힐 때·게임이 꺼질 때 SaveIfDirty()로 한 번에 저장합니다.
    /// </summary>
    public static class SettingsSystem
    {
        private const string FileName = "settings.json";

        private static SettingsData data;
        private static bool dirty;

        public static string FilePath => Path.Combine(SaveSystem.SaveDirectory, FileName);
        public static bool IsDirty => dirty;

        public static SettingsData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            var loaded = new SettingsData();
            data = JsonFile.TryReadInto(FilePath, loaded, "SettingsSystem", "설정 파일을 읽지 못해 기본값을 사용합니다.")
                ? Sanitize(loaded)
                : new SettingsData();
            dirty = false;
        }

        // 값이 바뀌었음을 표시. 실제 저장은 SaveIfDirty()/Save()
        public static void MarkDirty() => dirty = true;

        public static void SaveIfDirty()
        {
            if (dirty) Save();
        }

        public static void Save()
        {
            if (data == null) return;
            data.version = SettingsData.CurrentVersion;
            if (JsonFile.WriteAtomic(FilePath, data, "SettingsSystem")) dirty = false;
        }

        // 설정 파일을 지우고 기본값으로
        public static void ResetToDefault()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            data = null;
            dirty = false;
        }

        public static int Clamp(int value) => Mathf.Clamp(value, SettingsData.MinValue, SettingsData.MaxValue);

        // 파일을 직접 고쳐 범위를 벗어난 값이 들어와도 0~100으로
        private static SettingsData Sanitize(SettingsData s)
        {
            s.bgmVolume = Clamp(s.bgmVolume);
            s.sfxVolume = Clamp(s.sfxVolume);
            s.mouseSensitivity = Clamp(s.mouseSensitivity);
            return s;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            data = null;
            dirty = false;
            Application.quitting -= SaveIfDirty;
        }

        // 설정 창을 닫지 않고 게임을 꺼도(에디터에서는 Play 종료) 바뀐 설정이 남도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookQuit() => Application.quitting += SaveIfDirty;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("DinoCleaner/Save/설정 초기화 (기본값)")]
        private static void ResetMenu()
        {
            ResetToDefault();
            Debug.Log($"[SettingsSystem] 설정을 기본값으로 되돌렸습니다: {FilePath}");
        }
#endif
    }
}
