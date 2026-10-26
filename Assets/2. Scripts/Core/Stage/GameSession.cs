using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoCleaner.Core
{
    // 어느 스테이지를 골랐는지 씬을 기억하고, 씬 이동
    public static class GameSession
    {
        public const string HomeScene = "Home";
        public const string StageScene = "Stage";

        public static StageDefinition CurrentStage { get; private set; }

        // 출근: 홈 -> 스테이지
        public static void EnterStage(StageDefinition stage)
        {
            CurrentStage = stage;
            SceneManager.LoadScene(StageScene);
        }

        // 퇴근: 스테이지 -> 홈
        public static void GoHome() 
        { 
            SceneManager.LoadScene(HomeScene);
        }

        // Play를 시작할 때마다 비움 (지난 Play에서 고른 스테이지가 남지 않도록)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => CurrentStage = null;
    }
}