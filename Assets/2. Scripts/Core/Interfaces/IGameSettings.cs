namespace DinoCleaner.Core
{
    // 환경설정 값. 모두 0~100
    public interface IGameSettings
    {
        int BgmVolume { get; set; }
        int SfxVolume { get; set; }
        int MouseSensitivity { get; set; }
    }
}
