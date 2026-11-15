using NUnit.Framework;
using YeogiCafe.Data;

namespace YeogiCafe.Tests
{
    public class ThemeSeasonTests
    {
        [Test]
        public void Season_CyclesEverySevenDays()
        {
            Assert.AreEqual(0, SeasonBook.Season(1));
            Assert.AreEqual(0, SeasonBook.Season(7));
            Assert.AreEqual(1, SeasonBook.Season(8));
            Assert.AreEqual(3, SeasonBook.Season(28));
            Assert.AreEqual(0, SeasonBook.Season(29));
        }

        [Test]
        public void SeasonalMenus_AreNotToolBaseMenus()
        {
            foreach (var m in SeasonBook.Menus)
            {
                Assert.AreEqual(m.toolId, YeogiCafe.Order.CookingStations.ToolFor(m.id), m.id);
                foreach (var b in YeogiCafe.Order.CookingStations.MenusFor(m.toolId)) Assert.AreNotEqual(m.id, b);
            }
        }

        [Test]
        public void Theme_MatchesOnlyItsMood()
        {
            Assert.IsFalse(ThemeBook.Matches("default", AtmosphereAxis.Quiet));
            Assert.IsTrue(ThemeBook.Matches("tatami", AtmosphereAxis.Quiet));
            Assert.IsFalse(ThemeBook.Matches("tatami", AtmosphereAxis.Lively));
        }
    }
}
