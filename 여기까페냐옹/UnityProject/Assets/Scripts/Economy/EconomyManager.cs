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
        public event Action<int> OnGoldDelta;          // 판매·구매로 늘고 준 양(HUD 골드 옆 +/- 표시용)

        public void Init() { Gold = cfg != null ? cfg.startGold : 300; OnGoldChanged?.Invoke(Gold); }

        // 이어하기: 저장된 잔액 복원(매출 집계에는 포함하지 않음)
        public void SetGold(int amount) { Gold = Mathf.Max(0, amount); OnGoldChanged?.Invoke(Gold); }

        public void AddGold(int amount)
        {
            Gold += amount;
            if (amount > 0) TodayRevenue += amount;
            OnGoldChanged?.Invoke(Gold);
            if (amount != 0) OnGoldDelta?.Invoke(amount);
        }

        public bool TrySpend(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            if (amount != 0) OnGoldDelta?.Invoke(-amount);
            return true;
        }

        // 재료비: 정산 표시만 하던 것을 실제 골드에서도 뺀다(0 미만으로는 안 내려감)
        public void AddIngredientCost(int cost)
        {
            TodayCost += cost;
            int before = Gold;
            Gold = Mathf.Max(0, Gold - cost);
            OnGoldChanged?.Invoke(Gold);
            if (before != Gold) OnGoldDelta?.Invoke(Gold - before);
        }

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
