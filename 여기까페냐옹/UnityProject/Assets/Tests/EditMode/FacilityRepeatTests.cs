using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;

namespace YeogiCafe.Tests
{
    // 캣타워 왔다갔다 버그 회귀: 한 번 쓴 시설은 한동안(감쇠 0.05/결정) 다시 고르지 않는다.
    public class FacilityRepeatTests
    {
        class Fac : IFacility
        {
            public string FacilityId => "tower";
            public IReadOnlyList<FacilityTag> Tags => new[] { FacilityTag.Height };
            public bool IsFree => true;
        }

        [Test]
        public void 방금_쓴_선호_시설은_확률이_0이다()
        {
            var cat = ScriptableObject.CreateInstance<CatData>();
            cat.personalityTags = new[] { PersonalityTag.Curious, PersonalityTag.Playful };   // 가장 자주 가는 조합
            cat.facilityPreference = FacilityTag.Height;
            Assert.AreEqual(0f, FacilityDecider.UseChance(cat, new Fac(), 1.0f), 1e-4);
            // 10번 결정(≈30초) 뒤에도 기본 확률보다 낮다
            Assert.Less(FacilityDecider.UseChance(cat, new Fac(), 1.0f - 0.05f * 10), 0.5f);
        }
    }
}
