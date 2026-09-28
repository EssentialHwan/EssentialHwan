using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Core;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 사양서 §32 — 해금 필터 서비스.
    public class UnlockServiceTests
    {
        static CatData Cat(string id, UnlockType t, int v)
        {
            var c = ScriptableObject.CreateInstance<CatData>();
            c.catId = id;
            c.unlockCondition = new UnlockCondition { type = t, value = v };
            return c;
        }

        [Test]
        public void UnlockedCats_FiltersByCondition()
        {
            var all = new List<CatData> {
                Cat("a", UnlockType.Always, 0),
                Cat("b", UnlockType.CafeLevel, 2),
                Cat("c", UnlockType.CafeLevel, 3),
            };
            var save = new SaveGame { cafeLevel = 2 };
            var unlocked = UnlockService.UnlockedCats(all, save);
            var ids = unlocked.ConvertAll(x => x.catId);
            Assert.Contains("a", ids);
            Assert.Contains("b", ids);
            Assert.IsFalse(ids.Contains("c"), "Lv3 조건은 Lv2에서 잠김");
        }

        [Test]
        public void NewlyUnlocked_ReportsOnce()
        {
            var all = new List<CatData> { Cat("a", UnlockType.Always, 0), Cat("b", UnlockType.CafeLevel, 2) };
            var save = new SaveGame { cafeLevel = 1 };
            var known = new HashSet<string>();

            var first = UnlockService.NewlyUnlockedCatIds(all, save, known);
            Assert.Contains("a", first);
            Assert.IsFalse(first.Contains("b"));

            // 같은 상태 재호출 → 신규 없음
            Assert.AreEqual(0, UnlockService.NewlyUnlockedCatIds(all, save, known).Count);

            // 레벨 업 → b 신규 해금
            save.cafeLevel = 2;
            var second = UnlockService.NewlyUnlockedCatIds(all, save, known);
            Assert.Contains("b", second);
        }
    }
}
