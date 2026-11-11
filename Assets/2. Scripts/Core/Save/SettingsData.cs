using System;

namespace DinoCleaner.Core
{
    /// <summary>
    /// 환경설정. 게임 진행(save.json)과 분리된 settings.json 에 저장되어, 새 게임을 해도 유지됩니다.
    /// 항목 추가 방법은 SaveData와 같습니다: public 필드 추가 → 초기값이 기본값.
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        public const int CurrentVersion = 1;
        public const int MinValue = 0;
        public const int MaxValue = 100;

        public int version = CurrentVersion;

        // 모두 0~100. 기본값은 기존 TemporaryGameServices 값 그대로
        public int bgmVolume = 50;
        public int sfxVolume = 65;
        public int mouseSensitivity = 40;
    }
}
