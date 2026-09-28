using NUnit.Framework;
using YeogiCafe.Data;
using YeogiCafe.Loc;
using YeogiCafe.UI;

namespace YeogiCafe.Tests
{
    public class HudFormatterTests
    {
        [SetUp]
        public void Setup()
        {
            L.Load("key,ko,en\nui.cafe.customers,손님 {n},Guests {n}\nui.regular.stage,단골 {s}단계,Regular Lv.{s}\n");
            L.Current = Language.Ko;
        }

        [Test]
        public void Gold_Formats()
        {
            Assert.AreEqual("💰 300G", HudFormatter.Gold(300));
        }

        [Test]
        public void Guests_UsesLocalizedToken()
        {
            Assert.AreEqual("손님 5", HudFormatter.Guests(5));
        }

        [Test]
        public void RegularBadge_EmptyAtStage1()
        {
            Assert.AreEqual("", HudFormatter.RegularBadge(1));
            Assert.IsTrue(HudFormatter.RegularBadge(3).Contains("단골"));
        }

        [Test]
        public void TimeProgress_Clamped()
        {
            Assert.AreEqual(0.5f, HudFormatter.TimeProgress(360f, 720f), 0.001f);
            Assert.AreEqual(1f, HudFormatter.TimeProgress(999f, 720f), 0.001f);
            Assert.AreEqual(0f, HudFormatter.TimeProgress(10f, 0f), 0.001f);
        }

        [Test]
        public void Phase_Labels()
        {
            Assert.IsTrue(HudFormatter.Phase(DayPhase.Morning).Contains("아침"));
            Assert.IsTrue(HudFormatter.Phase(DayPhase.Evening).Contains("저녁"));
        }
    }
}
