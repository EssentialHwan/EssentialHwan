using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;
using YeogiCafe.Economy;
using YeogiCafe.Observation;
using YeogiCafe.Progression;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 사양서 §3 전체 파이프라인 통합 검증(씬 없이 로직 조립).
    // 시나리오: 창가석 없이 며칠 → 창가석 구매 → 발견 → 만족·재방문·단골까지.
    public class EndToEndFlowTests
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
            public void Occupy(object cat) => IsFree = false;
            public void Vacate() => IsFree = true;
        }

        BalanceConfig cfg;
        SaveGame save;
        ObservationManager obs;
        RegularManager regular;
        CatData cheese;
        MenuData fishcake;

        [SetUp]
        public void Setup()
        {
            cfg = ScriptableObject.CreateInstance<BalanceConfig>();
            save = new SaveGame();
            obs = new ObservationManager(cfg, save);
            regular = new RegularManager();

            cheese = ScriptableObject.CreateInstance<CatData>();
            cheese.catId = "cat_cheese";
            cheese.seatPreference = SeatTag.Window;
            cheese.foodFavoriteMenuId = "menu_fishcake";
            cheese.personalityTags = new[] { PersonalityTag.Relaxed, PersonalityTag.Glutton };

            fishcake = ScriptableObject.CreateInstance<MenuData>();
            fishcake.menuId = "menu_fishcake"; fishcake.price = 60; fishcake.cost = 20;
        }

        // 하루 시뮬: 좌석 선택 → 좌석/음식 단서 기록 → 만족 → history → 단골 평가 → 승격
        float SimulateDay(List<ISeat> seats, int day)
        {
            var choice = SeatSelector.Choose(cheese, seats, Vector3.zero, _ => 0, _ => 0, cfg);
            var cs = save.GetOrCreateCat(cheese.catId);

            if (choice.seat != null)
            {
                string seatKey = choice.seat.Tags.Count > 0 ? choice.seat.Tags[0].ToString() : "Normal";
                obs.RecordClue(cheese.catId, ClueType.SeatUse, seatKey, choice.isFallback, null);
                if (!choice.isFallback && seatKey == "Window")
                    obs.RecordClue(cheese.catId, ClueType.IdleGaze, "Window", false, null);
            }
            // 음식: 최애 제공(강한 단서)
            var order = OrderDecider.Choose(cheese, new List<MenuData> { fishcake });
            obs.RecordClue(cheese.catId, ClueType.FoodEat, order.primary.menuId, false, null);

            var sr = new SessionResult
            {
                servedFavorite = order.wasFavorite,
                usedPreferredSeat = choice.seat != null && !choice.isFallback,
                fastService = true
            };
            float sat = SatisfactionCalculator.Compute(cheese, sr, cfg);
            cs.PushSatisfaction(sat);
            cs.visitCount++; cs.lastVisitDay = day;

            obs.OnDayEnd();
            regular.Evaluate(cs);
            return sat;
        }

        [Test]
        public void FullArc_NoWindow_Then_Buy_Then_Discover_Then_Regular()
        {
            var normalOnly = new List<ISeat> {
                new StubSeat { SeatId = "n1", T = { SeatTag.Normal } }
            };
            // 1) 창가석 없이 3일 → 음식은 발견 진행, 좌석은 fallback cap으로 정체
            for (int d = 1; d <= 3; d++) SimulateDay(normalOnly, d);
            var cs = save.GetOrCreateCat(cheese.catId);
            Assert.AreNotEqual(ObsState.Recorded, cs.observation.seat.state, "창가석 없이는 좌석 발견 불가");
            Assert.LessOrEqual(cs.observation.seat.GetOrCreate("Normal").fallbackSum, cfg.fallbackCap);

            // 음식은 강한 단서 누적 → Confirmable 이상
            Assert.That(cs.observation.food.state,
                Is.EqualTo(ObsState.Confirmable).Or.EqualTo(ObsState.Recorded).Or.EqualTo(ObsState.Suspected));

            // 2) 창가석 구매 후 3일
            var withWindow = new List<ISeat> {
                new StubSeat { SeatId = "w1", T = { SeatTag.Window, SeatTag.OutsideView } },
                new StubSeat { SeatId = "n1", T = { SeatTag.Normal } }
            };
            for (int d = 4; d <= 6; d++) SimulateDay(withWindow, d);

            // 3) 좌석축이 Confirmable(★) 도달 → 기록
            Assert.AreEqual(ObsState.Confirmable, cs.observation.seat.state, "창가석 반복 관찰로 기록 가능해야 함");
            Assert.IsTrue(obs.TryRecord(cheese.catId, PrefAxis.Seat, out _));
            Assert.AreEqual("Window", cs.observation.seat.recordedValue);

            // 4) 만족·방문 누적 → 단골 승급(3단계 조건: 방문6·2축 Recorded 필요)
            // 음식도 기록 시도(Confirmable면)
            if (cs.observation.food.state == ObsState.Confirmable)
                obs.TryRecord(cheese.catId, PrefAxis.Food, out _);

            regular.Evaluate(cs);
            Assert.GreaterOrEqual(cs.regularStage, 2, "방문·만족 누적으로 최소 2단계");

            // 5) 재방문 확률: 만족 높고 최애·선호좌석 → 높은 확률
            float rc = RevisitSystem.RevisitChance(cs, 7, true, true, cfg);
            Assert.Greater(rc, 0.5f, "취향 충족 시 재방문 확률이 높아야 함");
        }
    }
}
