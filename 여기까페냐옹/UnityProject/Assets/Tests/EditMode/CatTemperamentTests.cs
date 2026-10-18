using NUnit.Framework;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;

namespace YeogiCafe.Tests
{
    public class CatTemperamentTests
    {
        static CatData Cat(PersonalityTag p1)
        {
            var c = ScriptableObject.CreateInstance<CatData>();
            c.personalityTags = new[] { p1, PersonalityTag.Curious };
            return c;
        }

        [Test]
        public void 첫_성격으로_손길_기질이_정해진다()
        {
            Assert.AreEqual(TouchStyle.Affectionate, CatTemperament.Style(Cat(PersonalityTag.Relaxed)));
            Assert.AreEqual(TouchStyle.Grumpy, CatTemperament.Style(Cat(PersonalityTag.Introvert)));
            Assert.AreEqual(TouchStyle.Shy, CatTemperament.Style(Cat(PersonalityTag.Lonely)));
            Assert.AreEqual(TouchStyle.Fickle, CatTemperament.Style(Cat(PersonalityTag.Fickle)));
            Assert.AreEqual(TouchStyle.Playful, CatTemperament.Style(Cat(PersonalityTag.Active)));
        }

        [Test]
        public void 낯가림_고양이는_단골이_되면_손길을_허락한다()
        {
            Assert.AreEqual(TouchResult.Flinch, CatTemperament.React(TouchStyle.Shy, 1, 1, 0.9f));
            Assert.AreEqual(TouchResult.Love, CatTemperament.React(TouchStyle.Shy, 1, 2, 0.9f));
        }

        [Test]
        public void 까칠한_고양이는_하악하고_기분이_나빠진다()
        {
            var r = CatTemperament.React(TouchStyle.Grumpy, 1, 1, 0.5f);
            Assert.AreEqual(TouchResult.Hiss, r);
            Assert.Less(CatTemperament.MoodDelta(r), 0);
        }

        [Test]
        public void 관찰_기록으로_도감_문구가_정해진다()
        {
            Assert.IsNull(CatTemperament.ObservedKey(new int[5]));
            Assert.AreEqual("touch.loves", CatTemperament.ObservedKey(new[] { 2, 0, 0, 0, 0 }));
            Assert.AreEqual("touch.fickle", CatTemperament.ObservedKey(new[] { 1, 0, 0, 0, 1 }));
            Assert.AreEqual("touch.warmed", CatTemperament.ObservedKey(new[] { 1, 0, 0, 2, 0 }));
            Assert.IsNull(CatTemperament.ObservedKey(null));
        }

        [Test]
        public void 친구끼리는_싸우지_않는다()
        {
            for (float r = 0; r < 1f; r += 0.05f)
            {
                var k = CatTemperament.Pair(TouchStyle.Grumpy, TouchStyle.Fickle, 2, r);
                Assert.AreNotEqual(SocialKind.Fight, k);
                Assert.AreNotEqual(SocialKind.Hiss, k);
            }
        }

        [Test]
        public void 까칠이와_변덕이_처음_만나면_싸울_수_있다()
        {
            bool fight = false;
            for (float r = 0; r < 1f; r += 0.05f)
                if (CatTemperament.Pair(TouchStyle.Grumpy, TouchStyle.Fickle, 0, r) == SocialKind.Fight) fight = true;
            Assert.IsTrue(fight);
        }
    }
}
