using NUnit.Framework;
using YeogiCafe.Cafe;

namespace YeogiCafe.Tests
{
    // 사양서 §17 T16 (P3) — 성장 데드락 방지 + 조건 판정(순수 Evaluate).
    public class CafeGrowthTests
    {
        static CafeLevelReq Lv2() => new CafeLevelReq {
            level = 2, goldCost = 800,
            minDiscoveredCats = 2, minRecordedAxesTotal = 3, minRegulars = 1,
            orAltFurnitureCount = 5
        };

        [Test]
        public void P3_ORPath_FurniturePlacement_PreventsDeadlock()
        {
            // 관찰 성과 전무 + 골드 충분 + 가구 5종 → OR 경로로 승급 가능(데드락 없음)
            var s = new GrowthState { gold = 900, discoveredCats = 0, recordedAxesTotal = 0, regulars = 0, furnitureCount = 5 };
            bool ok = CafeManager.Evaluate(s, Lv2(), out var hint);
            Assert.IsTrue(ok, "가구 OR 경로로 데드락 없이 승급 가능해야");
            Assert.IsNull(hint);
        }

        [Test]
        public void ObservationPath_Succeeds()
        {
            var s = new GrowthState { gold = 900, discoveredCats = 2, recordedAxesTotal = 3, regulars = 1, furnitureCount = 0 };
            Assert.IsTrue(CafeManager.Evaluate(s, Lv2(), out _), "관찰 성과 경로로 승급");
        }

        [Test]
        public void Blocked_WhenGoldInsufficient()
        {
            var s = new GrowthState { gold = 100, discoveredCats = 5, recordedAxesTotal = 9, regulars = 3, furnitureCount = 10 };
            bool ok = CafeManager.Evaluate(s, Lv2(), out var hint);
            Assert.IsFalse(ok);
            Assert.IsTrue(hint.Contains("골드"), "골드 부족 힌트");
        }

        [Test]
        public void Blocked_WhenNoPathMet_ShowsHint()
        {
            // 골드는 충분하지만 관찰·가구 둘 다 미달 → 원인 힌트
            var s = new GrowthState { gold = 900, discoveredCats = 1, recordedAxesTotal = 1, regulars = 0, furnitureCount = 2 };
            bool ok = CafeManager.Evaluate(s, Lv2(), out var hint);
            Assert.IsFalse(ok);
            Assert.IsTrue(hint.Contains("발견") || hint.Contains("가구"), "방향 힌트 노출");
        }
    }
}
