using System;

namespace DinoCleaner.Core
{
    /// <summary>
    /// 로컬에 저장되는 모든 진행 정보. JSON 파일 하나로 저장됩니다.
    ///
    /// [저장할 요소를 추가하는 법]
    /// 1. 아래에 public 필드를 추가합니다. (int, bool, string, float, [Serializable] 클래스/구조체, List&lt;T&gt; 가능 / Dictionary 불가)
    /// 2. 필드 초기값이 "새 게임"의 값이 됩니다. 예전 세이브 파일에 없는 필드도 이 초기값으로 채워집니다.
    /// 3. 값을 바꾼 곳에서 SaveSystem.Save()를 호출합니다.
    /// 필드 이름을 바꾸거나 지우면 예전 세이브의 그 값은 사라집니다. 그럴 때는 CurrentVersion을 올리고 SaveSystem.Migrate에 변환을 적습니다.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // 세이브 구조가 호환되지 않게 바뀔 때만 1씩 올림
        public const int CurrentVersion = 1;

        // 새 게임 시작 자금 (2026-11-11 기획 결정: 0원)
        public const int NewGameMoney = 0;

        public int version = CurrentVersion;

        // 보유 돈. 직접 바꾸지 말고 Wallet을 거칠 것
        public int money = NewGameMoney;

        // TODO(다음 작업): 구매한 업그레이드 단계
        // public List<UpgradeSaveEntry> upgrades = new();
    }
}
