using UnityEngine;

namespace DinoCleaner.Core
{
    // 진행 정보와 설정의 구현체를 모듈끼리 직접 참조하지 않고 얻기 위한 곳
    public static class GameServices
    {
        public static IGameProgress Progress { get; private set; }
        public static IGameSettings Settings { get; private set; }

        public static void Register(IGameProgress progress, IGameSettings settings)
        {
            Progress = progress;
            Settings = settings;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Progress = null;
            Settings = null;
        }
    }
}
