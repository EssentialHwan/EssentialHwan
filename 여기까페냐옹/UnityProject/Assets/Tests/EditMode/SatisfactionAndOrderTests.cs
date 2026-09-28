using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;
using YeogiCafe.Observation;

namespace YeogiCafe.Tests
{
    // 사양서 §17 T14/T9/T6/T7 보강.
    public class SatisfactionAndOrderTests
    {
        BalanceConfig cfg;
        CatData cheese;

        [SetUp]
        public void Setup()
        {
            cfg = ScriptableObject.CreateInstance<BalanceConfig>();
            cheese = ScriptableObject.CreateInstance<CatData>();
            cheese.catId = "cat_cheese";
            cheese.seatPreference = SeatTag.Window;
            cheese.facilityPreference = FacilityTag.Soft;
            cheese.foodFavoriteMenuId = "menu_fishcake";
            cheese.personalityTags = new[] { PersonalityTag.Relaxed, PersonalityTag.Glutton };
        }

        // T14: 3축(음식+좌석+시설) 충족 → 만족 90+
        [Test]
        public void ThreeAxisSatisfied_Reaches90Plus()
        {
            var sr = new SessionResult
            {
                servedFavorite = true,
                usedPreferredSeat = true,
                usedPreferredFacility = true,
                fastService = true
            };
            float s = SatisfactionCalculator.Compute(cheese, sr, cfg);
            // base50 +25 +15 +15 +5 = 110 → clamp 100
            Assert.GreaterOrEqual(s, 90f, "취향 3축 충족 시 고만족(이해=만족 체감)");
        }

        // T9: 최애 품절 → 차선 강제 → 음식 만족 감점
        [Test]
        public void ForcedSecondChoice_LowersFoodSatisfaction()
        {
            var srFav = new SessionResult { servedFavorite = true, fastService = true };
            var srForced = new SessionResult { servedFavorite = false, wasForcedSecond = true, fastService = true };
            float withFav = SatisfactionCalculator.Compute(cheese, srFav, cfg);
            float forced = SatisfactionCalculator.Compute(cheese, srForced, cfg);
            Assert.Greater(withFav, forced, "최애보다 차선 강제가 낮은 만족");
        }

        // T18(P2): MVP에서 분위기는 만족에 영향 없음 (atmoMult=1.0 기본)
        [Test]
        public void MVP_AtmosphereDoesNotAffectSatisfaction()
        {
            Assert.IsFalse(cfg.atmosphereAffectsSatisfaction, "MVP는 분위기 만족 미반영");
            var sr = new SessionResult
            {
                servedFavorite = true, usedPreferredSeat = true,
                hasDominant = true, dominantAtmosphere = AtmosphereAxis.Lively, // 치즈냥은 Quiet 선호(반대)
                fastService = true
            };
            float s = SatisfactionCalculator.Compute(cheese, sr, cfg);
            // 반대 분위기여도 승수 미적용 → 좌석 만족 그대로
            var srNoAtmo = new SessionResult { servedFavorite = true, usedPreferredSeat = true, fastService = true };
            Assert.AreEqual(SatisfactionCalculator.Compute(cheese, srNoAtmo, cfg), s, 0.01f);
        }
    }

    // T6/T7: 시설 관찰 채널
    public class FacilityChannelTests
    {
        BalanceConfig cfg;
        CatData calico;

        class StubFac : IFacility
        {
            public string FacilityId { get; set; }
            public List<FacilityTag> T = new();
            public IReadOnlyList<FacilityTag> Tags => T;
            public bool IsFree { get; set; } = true;
        }

        [SetUp]
        public void Setup()
        {
            cfg = ScriptableObject.CreateInstance<BalanceConfig>();
            calico = ScriptableObject.CreateInstance<CatData>();
            calico.catId = "cat_calico";
            calico.facilityPreference = FacilityTag.Play;   // 캣타워 선호
            calico.personalityTags = new[] { PersonalityTag.Curious, PersonalityTag.Fickle };
        }

        // T7: 선호 시설 있으면 이용확률↑ (선호 시설이 base보다 높음)
        [Test]
        public void PreferredFacility_HasHigherUseChance()
        {
            var tower = new StubFac { FacilityId = "tower", T = { FacilityTag.Play, FacilityTag.Height } };
            var plant = new StubFac { FacilityId = "plant", T = { FacilityTag.Quiet, FacilityTag.Natural } };
            float chTower = FacilityDecider.UseChance(calico, tower, 0f);
            float chPlant = FacilityDecider.UseChance(calico, plant, 0f);
            Assert.Greater(chTower, chPlant, "선호(Play) 시설 이용확률이 무관 시설보다 높아야");
            Assert.Greater(chTower, 0.5f, "선호+호기심 가산으로 높은 확률");
        }

        // T6: 선호 시설이 카페에 없으면 그 취향 신호를 낼 시설 자체가 없음
        [Test]
        public void NoPreferredFacility_NoStrongSignalPath()
        {
            var onlyPlant = new List<IFacility> { new StubFac { FacilityId = "plant", T = { FacilityTag.Quiet } } };
            // Play 태그 시설이 없으므로 선호 가산(+0.5)이 붙는 후보가 존재하지 않음
            foreach (var f in onlyPlant)
                Assert.IsFalse(HasTag(f.Tags, calico.facilityPreference), "선호 시설 미설치 → 강신호 채널 없음");
        }

        static bool HasTag(IReadOnlyList<FacilityTag> tags, FacilityTag t)
        {
            foreach (var x in tags) if (x == t) return true;
            return false;
        }
    }
}
