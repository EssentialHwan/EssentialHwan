using System.Collections.Generic;
using NUnit.Framework;
using YeogiCafe.Data;
using YeogiCafe.Observation;

namespace YeogiCafe.Tests
{
    // 문서 01 §D — 관찰 서술 매핑. 시설/좌석 태그별로 다른 서술 키.
    public class NarrationCatalogTests
    {
        [Test]
        public void Seat_Fallback_UsesFallbackKey()
        {
            Assert.AreEqual("narr.seat.fallback",
                NarrationCatalog.SeatKey(new List<SeatTag> { SeatTag.Normal }, fallback: true));
        }

        [Test]
        public void Seat_Window_UsesWindowKey()
        {
            Assert.AreEqual("narr.seat.window",
                NarrationCatalog.SeatKey(new List<SeatTag> { SeatTag.Window, SeatTag.OutsideView }, false));
        }

        [Test]
        public void Facility_DistinctKeysPerType()
        {
            Assert.AreEqual("narr.facility.tower", NarrationCatalog.FacilityKey(new List<FacilityTag> { FacilityTag.Height }));
            Assert.AreEqual("narr.facility.cushion", NarrationCatalog.FacilityKey(new List<FacilityTag> { FacilityTag.Soft }));
            Assert.AreEqual("narr.facility.toybox", NarrationCatalog.FacilityKey(new List<FacilityTag> { FacilityTag.Play, FacilityTag.Social }));
            Assert.AreEqual("narr.facility.plant", NarrationCatalog.FacilityKey(new List<FacilityTag> { FacilityTag.Natural }));
        }

        [Test]
        public void Food_SpeedKeys()
        {
            Assert.AreEqual("narr.food.fast", NarrationCatalog.FoodKey(true));
            Assert.AreEqual("narr.food.slow", NarrationCatalog.FoodKey(false));
        }
    }
}
