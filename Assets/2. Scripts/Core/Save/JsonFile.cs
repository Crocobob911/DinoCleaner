using System;
using System.IO;
using UnityEngine;

namespace DinoCleaner.Core
{
    // SaveSystem·SettingsSystem이 같이 쓰는 JSON 파일 읽기/쓰기
    internal static class JsonFile
    {
        // 파일 내용을 target에 덮어씀. 파일이 없거나 깨졌으면 false (깨진 파일은 *_broken_날짜.json 으로 옮겨둠)
        // false일 때 target은 일부만 덮어썼을 수 있으므로 호출한 쪽에서 새 객체를 쓸 것
        public static bool TryReadInto(string path, object target, string logTag, string brokenMessage)
        {
            if (!File.Exists(path)) return false;

            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), target);
                return true;
            }
            catch (Exception e)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                string brokenPath = Path.Combine(Path.GetDirectoryName(path) ?? "",
                    $"{name}_broken_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                Debug.LogError($"[{logTag}] {brokenMessage} 원본은 {brokenPath} 로 옮겼습니다.\n{e}");

                try { File.Move(path, brokenPath); }
                catch (Exception moveError) { Debug.LogWarning($"[{logTag}] 깨진 파일을 옮기지 못했습니다.\n{moveError}"); }
                return false;
            }
        }

        // 임시 파일에 다 쓴 뒤 교체해서, 쓰는 도중 꺼져도 기존 파일이 깨지지 않게 함
        public static bool WriteAtomic(string path, object data, string logTag)
        {
            string tempPath = path + ".tmp";
            try
            {
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Replace(tempPath, path, null);
                else File.Move(tempPath, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{logTag}] 저장에 실패했습니다: {path}\n{e}");
                return false;
            }
        }
    }
}
