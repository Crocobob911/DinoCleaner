using System;
using UnityEngine;

namespace DinoCleaner.Core
{
    /// <summary>
    /// 보유 돈의 유일한 출입구. 돈이 들어오거나(Income) 나갈 때(TryConsume)마다 바로 로컬에 저장합니다.
    /// </summary>
    public class Wallet
    {
        public int Money => SaveSystem.Data.money;

        // 돈이 바뀐 뒤 호출 (저장까지 끝난 상태)
        public event Action Changed;

        public bool CanAfford(int cost) => cost >= 0 && Money >= cost;

        public void Income(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[Wallet] Income에 음수({amount})가 들어왔습니다. 돈을 쓰려면 TryConsume을 사용하세요.");
                return;
            }
            if (amount == 0) return;

            SaveData save = SaveSystem.Data;
            save.money = (int)Math.Min((long)save.money + amount, int.MaxValue);
            Commit();
        }

        // 돈이 부족하면 아무것도 하지 않고 false
        public bool TryConsume(int cost)
        {
            if (!CanAfford(cost)) return false;
            if (cost == 0) return true;

            SaveSystem.Data.money -= cost;
            Commit();
            return true;
        }

        private void Commit()
        {
            SaveSystem.Save();
            Changed?.Invoke();
        }
    }
}
