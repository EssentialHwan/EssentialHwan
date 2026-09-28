using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;

namespace YeogiCafe.Tests
{
    // 사양서 §17 T1/T3/T4 — 좌석 선택 + fallback + pref 지배력.
    public class SeatSelectionTests
    {
        class StubSeat : ISeat
        {
            public string SeatId { get; set; }
            public List<SeatTag> TagsList = new();
            public IReadOnlyList<SeatTag> Tags => TagsList;
            public Vector3 WorldPos { get; set; }
            public bool IsFree { get; set; } = true;
            public bool IsCentral { get; set; }
            public AtmosphereAxis LocalDominantAxis { get; set; }
            public void Occupy(object cat) => IsFree = false;
            public void Vacate() => IsFree = true;
        }

        BalanceConfig cfg;
        CatData cheese;

        [SetUp]
        public void Setup()
        {
            cfg = ScriptableObject.CreateInstance<BalanceConfig>();
            cheese = ScriptableObject.CreateInstance<CatData>();
            cheese.catId = "cat_cheese";
            cheese.seatPreference = SeatTag.Window;
            cheese.personalityTags = new[] { PersonalityTag.Relaxed, PersonalityTag.Glutton };
        }

        // T1: 창가석 없음 → 일반석 fallback (isFallback == true)
        [Test]
        public void NoWindowSeat_FallsBackToNormal()
        {
            var seats = new List<ISeat> {
                new StubSeat { SeatId = "n1", TagsList = { SeatTag.Normal } },
                new StubSeat { SeatId = "c1", TagsList = { SeatTag.Corner } },
            };
            var choice = SeatSelector.Choose(cheese, seats, Vector3.zero, _ => 0, _ => 0, cfg);
            Assert.IsNotNull(choice.seat);
            Assert.IsTrue(choice.isFallback, "선호(창가석)가 없으면 fallback이어야 한다");
        }

        // T3: 창가석 존재 → 창가석 선택 (pref_exact 지배)
        [Test]
        public void WindowSeatPresent_SelectsWindow_NotFallback()
        {
            var window = new StubSeat { SeatId = "w1", TagsList = { SeatTag.Window, SeatTag.OutsideView } };
            var seats = new List<ISeat> {
                new StubSeat { SeatId = "n1", TagsList = { SeatTag.Normal } },
                window,
            };
            // 랜덤 편차가 있어도 pref_exact(100)이 지배 → 다회 반복해도 창가석
            for (int i = 0; i < 20; i++)
            {
                var choice = SeatSelector.Choose(cheese, seats, Vector3.zero, _ => 0, _ => 0, cfg);
                Assert.AreEqual("w1", choice.seat.SeatId, "취향 신호가 노이즈를 압도해야 한다");
                Assert.IsFalse(choice.isFallback);
            }
        }

        // T4: 창가석이 점유중이면 후보 제외 → 차선
        [Test]
        public void WindowSeatOccupied_ExcludedFromCandidates()
        {
            var seats = new List<ISeat> {
                new StubSeat { SeatId = "w1", TagsList = { SeatTag.Window }, IsFree = false },
                new StubSeat { SeatId = "n1", TagsList = { SeatTag.Normal } },
            };
            var choice = SeatSelector.Choose(cheese, seats, Vector3.zero, _ => 0, _ => 0, cfg);
            Assert.AreEqual("n1", choice.seat.SeatId);
            Assert.IsTrue(choice.isFallback);
        }

        // 단골 "늘 그 자리"(27장): 높은 단계면 이전 자리 강하게 선호
        [Test]
        public void RegularFavoriteSeat_BiasesChoice()
        {
            // 취향 좌석이 아닌 일반석2를 favorite로 지정, 단골 5단계 → 그 자리 선호
            var normal1 = new StubSeat { SeatId = "n1", TagsList = { SeatTag.Normal } };
            var normal2 = new StubSeat { SeatId = "n2", TagsList = { SeatTag.Normal } };
            var seats = new List<ISeat> { normal1, normal2 };
            // 취향(창가석)이 후보에 없어 둘 다 pref_none. favorite=n2 + 5단계 → n2 +100
            int n2Count = 0;
            for (int i = 0; i < 20; i++)
            {
                var choice = SeatSelector.Choose(cheese, seats, Vector3.zero, _ => 0, _ => 0, cfg,
                                                 favoriteSeatId: "n2", regularStage: 5);
                if (choice.seat.SeatId == "n2") n2Count++;
            }
            Assert.Greater(n2Count, 18, "단골 5단계는 늘 그 자리에 앉는 경향");
        }

        // 전 좌석 만석 → seat == null (성격분기 트리거)
        [Test]
        public void AllOccupied_ReturnsNull()
        {
            var seats = new List<ISeat> {
                new StubSeat { SeatId = "w1", TagsList = { SeatTag.Window }, IsFree = false },
            };
            var choice = SeatSelector.Choose(cheese, seats, Vector3.zero, _ => 0, _ => 0, cfg);
            Assert.IsNull(choice.seat);
        }
    }
}
