using NUnit.Framework;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 사양서 §32 — 해금 조건 데이터 평가.
    public class UnlockConditionTests
    {
        SaveGame Save(int level = 1, int day = 1) => new SaveGame { cafeLevel = level, currentDay = day };

        [Test]
        public void Always_IsAlwaysMet()
        {
            var u = new UnlockCondition { type = UnlockType.Always };
            Assert.IsTrue(u.IsMet(Save()));
        }

        [Test]
        public void CafeLevel_Gate()
        {
            var u = new UnlockCondition { type = UnlockType.CafeLevel, value = 2 };
            Assert.IsFalse(u.IsMet(Save(level: 1)));
            Assert.IsTrue(u.IsMet(Save(level: 2)));
            Assert.IsTrue(u.IsMet(Save(level: 3)));
        }

        [Test]
        public void DiscoveredCats_Gate()
        {
            var save = Save();
            var u = new UnlockCondition { type = UnlockType.DiscoveredCats, value = 2 };
            Assert.IsFalse(u.IsMet(save));
            save.GetOrCreateCat("a").visitCount = 1;
            save.GetOrCreateCat("b").visitCount = 3;
            Assert.IsTrue(u.IsMet(save), "발견 2마리 → 해금");
        }

        [Test]
        public void RecordedAxes_Gate()
        {
            var save = Save();
            var u = new UnlockCondition { type = UnlockType.RecordedAxesTotal, value = 3 };
            var c = save.GetOrCreateCat("a");
            c.observation.food.state = ObsState.Recorded;
            c.observation.seat.state = ObsState.Recorded;
            Assert.IsFalse(u.IsMet(save), "2축 < 3");
            c.observation.facility.state = ObsState.Recorded;
            Assert.IsTrue(u.IsMet(save), "3축 → 해금");
        }

        [Test]
        public void DayReached_Gate()
        {
            var u = new UnlockCondition { type = UnlockType.DayReached, value = 5 };
            Assert.IsFalse(u.IsMet(Save(day: 4)));
            Assert.IsTrue(u.IsMet(Save(day: 5)));
        }

        [Test]
        public void NullSave_AlwaysType_True_OthersFalse()
        {
            Assert.IsTrue(new UnlockCondition { type = UnlockType.Always }.IsMet(null));
            Assert.IsFalse(new UnlockCondition { type = UnlockType.CafeLevel, value = 2 }.IsMet(null));
        }
    }
}
