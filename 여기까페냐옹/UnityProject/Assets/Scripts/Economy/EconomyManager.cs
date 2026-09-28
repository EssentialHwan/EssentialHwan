using System;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.Economy
{
    // 사양서 §13 — 단일 재화 Gold. 결제 배수·구매·정산.
    public class EconomyManager : MonoBehaviour
    {
        public BalanceConfig cfg;

        public int Gold { get; private set; }
        public int TodayRevenue { get; private set; }
        public int TodayCost { get; private set; }

        public event Action<int> OnGoldChanged;

        public void Init() { Gold = cfg != null ? cfg.startGold : 300; OnGoldChanged?.Invoke(Gold); }

        public void AddGold(int amount)
        {
            Gold += amount;
            if (amount > 0) TodayRevenue += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public bool TrySpend(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }

        public void AddIngredientCost(int cost) => TodayCost += cost;

        public void ResetDaily() { TodayRevenue = 0; TodayCost = 0; }
        public int TodayNet => TodayRevenue - TodayCost;

        // §3.4 결제 배수
        public static float TipMultiplier(float satisfaction)
            => satisfaction < 40 ? 0.8f
             : satisfaction < 70 ? 1.0f
             : satisfaction < 90 ? 1.15f
             : 1.3f;
    }
}
