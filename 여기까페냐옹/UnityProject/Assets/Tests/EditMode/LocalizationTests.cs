using NUnit.Framework;
using YeogiCafe.Loc;

namespace YeogiCafe.Tests
{
    // 문서 07 로컬라이즈 파서 검증 (순수 로직).
    public class LocalizationTests
    {
        const string Csv =
            "key,ko,en\n" +
            "ui.hello,안녕,Hello\n" +
            "ui.token,{name}의 취향,{name}'s preference\n" +
            "ui.only_ko,한국어만,\n";

        [SetUp]
        public void Setup() { L.Load(Csv); L.Current = Language.Ko; }

        [Test]
        public void Get_ReturnsKoByDefault()
        {
            Assert.AreEqual("안녕", L.Get("ui.hello"));
        }

        [Test]
        public void Get_ReturnsEn_WhenEnglish()
        {
            L.Current = Language.En;
            Assert.AreEqual("Hello", L.Get("ui.hello"));
        }

        [Test]
        public void Get_ReplacesToken()
        {
            Assert.AreEqual("치즈냥의 취향", L.Get("ui.token", ("name", "치즈냥")));
        }

        [Test]
        public void Get_FallsBackToKo_WhenEnMissing()
        {
            L.Current = Language.En;
            Assert.AreEqual("한국어만", L.Get("ui.only_ko"), "영어 비어있으면 ko 폴백");
        }

        [Test]
        public void Get_ReturnsKey_WhenMissing()
        {
            Assert.AreEqual("no.such.key", L.Get("no.such.key"));
        }
    }
}
