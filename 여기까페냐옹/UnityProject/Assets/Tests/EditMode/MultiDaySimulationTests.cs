using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;
using YeogiCafe.Observation;
using YeogiCafe.Progression;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 다일 헤드리스 시뮬 — 씬 없이 전체 루프가 맞물려 도는지 검증.
    // 관찰→기록→만족→재방문→단골 체인이 여러 날에 걸쳐 끊기지 않음을 보장.
    public class MultiDaySimulationTests
    {
        class StubSeat : ISeat
        {
            public string SeatId { get; set; }
            public List<SeatTag> T = new();
            public IReadOnlyList<SeatTag> Tags => T;
            public Vector3 WorldPos => Vector3.zero;
            public bool IsFree { get; set; } = true;
            public bool IsCentral => false;
            public AtmosphereAxis LocalDominantAxis => AtmosphereAxis.Quiet;
            public void Occupy(object c) => IsFree = false;
            public void Vacate() => IsFree = true;
        }

        [Test]
        public void ThreeCats_TenDays_FullChain_NoBreak()
        {
            var cfg = ScriptableObject.CreateInstance<BalanceConfig>();
            var save = new SaveGame();
            var obs = new ObservationManager(cfg, save);
            var regular = new RegularManager();

            // 3마리 Truth
            var cats = new[] {
                MakeCat("cat_cheese", SeatTag.Window, "menu_fishcake", PersonalityTag.Relaxed),
                MakeCat("cat_calico", SeatTag.Corner, "menu_parfait", PersonalityTag.Curious),
                MakeCat("cat_cow",    SeatTag.TwoSeat, "menu_tuna_sand", PersonalityTag.Social),
            };
            var menus = new List<MenuData> {
                Menu("menu_fishcake"), Menu("menu_parfait"), Menu("menu_tuna_sand")
            };
            // 카페에 각 고양이 선호 좌석이 모두 존재(발견 가능 환경)
            var seats = new List<ISeat> {
                Seat("w", SeatTag.Window, SeatTag.OutsideView),
                Seat("c", SeatTag.Corner),
                Seat("t2", SeatTag.TwoSeat),
                Seat("n", SeatTag.Normal),
            };

            for (int day = 1; day <= 10; day++)
            {
                foreach (var cat in cats)
                {
                    // 좌석 선택
                    var choice = SeatSelector.Choose(cat, seats, Vector3.zero, _ => 0, _ => 0, cfg);
                    Assert.IsNotNull(choice.seat, $"day{day} {cat.catId}: 좌석 배정 실패");
                    string seatKey = choice.seat.Tags.Count > 0 ? choice.seat.Tags[0].ToString() : "Normal";
                    obs.RecordClue(cat.catId, ClueType.SeatUse, seatKey, choice.isFallback, null);
                    if (!choice.isFallback && seatKey == "Window")
                        obs.RecordClue(cat.catId, ClueType.IdleGaze, "Window", false, null);
                    choice.seat.Vacate(); // 즉시 반납(시뮬 단순화)

                    // 주문(최애 제공)
                    var order = OrderDecider.Choose(cat, menus);
                    obs.RecordClue(cat.catId, ClueType.FoodEat, order.primary.menuId, false, null);

                    // 만족
                    var cs = save.GetOrCreateCat(cat.catId);
                    var sr = new SessionResult {
                        servedFavorite = order.wasFavorite,
                        usedPreferredSeat = !choice.isFallback,
                        fastService = true
                    };
                    float sat = SatisfactionCalculator.Compute(cat, sr, cfg);
                    cs.PushSatisfaction(sat);
                    cs.visitCount++;
                    cs.lastVisitDay = day;
                }

                obs.OnDayEnd();
                foreach (var cs in save.cats) regular.Evaluate(cs);

                // 기록 가능한 축은 즉시 기록(플레이어 행동 시뮬)
                foreach (var cat in cats)
                {
                    foreach (PrefAxis ax in System.Enum.GetValues(typeof(PrefAxis)))
                        obs.TryRecord(cat.catId, ax, out _);
                }
            }

            // 검증: 10일 후 모든 고양이가 최소 좌석·음식 발견 + 단골 승급
            foreach (var cat in cats)
            {
                var cs = save.GetOrCreateCat(cat.catId);
                Assert.GreaterOrEqual(cs.observation.RecordedAxisCount(), 2, $"{cat.catId}: 최소 2축 발견");
                Assert.GreaterOrEqual(cs.regularStage, 2, $"{cat.catId}: 단골 승급");
                Assert.AreEqual(10, cs.visitCount, $"{cat.catId}: 10일 방문");

                // 재방문 확률이 높게 유지
                float rc = RevisitSystem.RevisitChance(cs, 11, true, true, cfg);
                Assert.Greater(rc, 0.5f, $"{cat.catId}: 재방문 확률 유지");
            }
        }

        static CatData MakeCat(string id, SeatTag seat, string fav, PersonalityTag p)
        {
            var c = ScriptableObject.CreateInstance<CatData>();
            c.catId = id; c.seatPreference = seat; c.foodFavoriteMenuId = fav;
            c.facilityPreference = FacilityTag.Soft;
            c.personalityTags = new[] { p, p };
            return c;
        }
        static MenuData Menu(string id) { var m = ScriptableObject.CreateInstance<MenuData>(); m.menuId = id; m.price = 60; return m; }
        static ISeat Seat(string id, params SeatTag[] tags)
        {
            var s = new StubSeat { SeatId = id };
            s.T.AddRange(tags);
            return s;
        }
    }
}
