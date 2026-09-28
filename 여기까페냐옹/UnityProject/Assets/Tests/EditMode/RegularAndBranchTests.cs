using NUnit.Framework;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;
using YeogiCafe.Progression;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 사양서 §17 T15/T20 보강.
    public class RegularAndBranchTests
    {
        BalanceConfig cfg;

        [SetUp]
        public void Setup() { cfg = ScriptableObject.CreateInstance<BalanceConfig>(); }

        // T15: 단골 승급 경계 — 방문3·avg60→2단계, 방문6·2축Recorded→3단계, 강등 없음
        [Test]
        public void RegularStage_UpgradesByVisitsAndRecordedAxes_NoDemotion()
        {
            var regular = new RegularManager();
            var cat = new CatSaveData { catId = "cat_cheese" };

            // 방문 2회, 만족 낮음 → 여전히 1단계
            cat.visitCount = 2; cat.PushSatisfaction(50); regular.Evaluate(cat);
            Assert.AreEqual(1, cat.regularStage);

            // 방문 3회 + 평균 60 → 2단계
            cat.visitCount = 3; cat.PushSatisfaction(70); regular.Evaluate(cat);
            Assert.AreEqual(2, cat.regularStage);

            // 방문 6회 + 2축 Recorded → 3단계
            cat.visitCount = 6;
            cat.observation.food.state = ObsState.Recorded;
            cat.observation.seat.state = ObsState.Recorded;
            regular.Evaluate(cat);
            Assert.AreEqual(3, cat.regularStage);

            // 조건 하락(만족 급락)에도 강등 없음 (부록 J-H)
            cat.PushSatisfaction(0); cat.PushSatisfaction(0);
            regular.Evaluate(cat);
            Assert.AreEqual(3, cat.regularStage, "강등 없음");
        }

        // T20: 만석 성격분기 — 느긋/내향=대기, 활발/변덕=귀가
        [Test]
        public void FullCafe_PersonalityBranch()
        {
            var relaxed = MakeCat(PersonalityTag.Relaxed, PersonalityTag.Glutton);
            var introvert = MakeCat(PersonalityTag.Introvert, PersonalityTag.Lonely);
            var active = MakeCat(PersonalityTag.Active, PersonalityTag.Playful);
            var fickle = MakeCat(PersonalityTag.Fickle, PersonalityTag.Curious);

            Assert.AreEqual(CatState.WaitOutside, FullCafePolicy.DecideWhenFull(relaxed));
            Assert.AreEqual(CatState.WaitOutside, FullCafePolicy.DecideWhenFull(introvert));
            Assert.AreEqual(CatState.LeaveDisappointed, FullCafePolicy.DecideWhenFull(active));
            Assert.AreEqual(CatState.LeaveDisappointed, FullCafePolicy.DecideWhenFull(fickle));
        }

        static CatData MakeCat(PersonalityTag p1, PersonalityTag p2)
        {
            var c = ScriptableObject.CreateInstance<CatData>();
            c.personalityTags = new[] { p1, p2 };
            return c;
        }
    }
}
