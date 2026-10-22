using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Economy;

namespace YeogiCafe.Tests
{
    // 영업 중 종료 → 오늘 매출 되돌림. 그 돈을 이미 썼다면 빚(마이너스)으로 남아야 '벌고→사고→끄기' 반복이 공짜가 되지 않는다.
    public class EconomyDebtTests
    {
        EconomyManager eco; GameObject go;
        [SetUp] public void SetUp() { go = new GameObject("eco"); eco = go.AddComponent<EconomyManager>(); eco.RestoreGold(100); eco.ResetDaily(); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(go); }

        [Test]
        public void QuitRollback_KeepsDebt_WhenTodaysRevenueWasSpent()
        {
            eco.AddGold(1000);                       // 오늘 매출
            Assert.IsTrue(eco.TrySpend(1000));       // 테마 구매
            int saved = eco.Gold - eco.TodayRevenue + eco.TodayCost;   // GameFlowController.SaveOnQuit 와 같은 식
            Assert.AreEqual(-900, saved);
            eco.RestoreGold(saved);
            Assert.AreEqual(-900, eco.Gold);
            Assert.IsFalse(eco.TrySpend(1));         // 빚이 있으면 못 산다
        }

        [Test]
        public void IngredientCost_DoesNotEraseDebt()
        {
            eco.RestoreGold(-50);
            eco.AddIngredientCost(10);
            Assert.AreEqual(-50, eco.Gold);
            eco.RestoreGold(5);
            eco.AddIngredientCost(10);
            Assert.AreEqual(0, eco.Gold);           // 양수일 때는 0까지만
        }
    }
}
