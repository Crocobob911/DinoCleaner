using System;

namespace DinoCleaner.Core
{
    // 업그레이드 하나의 구매 단계. 이름·설명·비용은 저장하지 않고 코드의 정의에서 가져옴
    [Serializable]
    public class UpgradeSaveEntry
    {
        public string id;
        public int level;

        public UpgradeSaveEntry() { } // JsonUtility 역직렬화용

        public UpgradeSaveEntry(string id, int level)
        {
            this.id = id;
            this.level = level;
        }
    }
}
