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

        // [프리플라이트] 부분일치 제거 검증: Quiet 태그만 있는 구석석은 창가석 선호에 가산 없음(fallback 동일)
        [Test]
        public void NoPartialMatch_QuietSeatIsPlainFallback()
        {
            var corner = new StubSeat { SeatId = "c1", TagsList = { SeatTag.Corner, SeatTag.Quiet } };
            var normal = new StubSeat { SeatId = "n1", TagsList = { SeatTag.Normal } };
            // 치즈냥(느긋·식탐, 내향 아님) — 구석/Quiet 성격 보너스도 없음.
            // 창가석 없음 → 구석석·일반석 둘 다 pref_none → 어느 쪽도 취향 신호로 굳어지면 안 됨.
            int cornerCount = 0;
            for (int i = 0; i < 40; i++)
            {
                var choice = SeatSelector.Choose(cheese, new List<ISeat> { corner, normal },
                                                 Vector3.zero, _ => 0, _ => 0, cfg);
                if (choice.seat.SeatId == "c1") cornerCount++;
                Assert.IsTrue(choice.isFallback, "창가석 아니면 항상 fallback");
            }
            // 랜덤 편차로 대략 반반이어야 함(구석석이 부분일치로 지배하면 안 됨)
            Assert.Less(cornerCount, 36, "Quiet 태그가 구석석을 취향처럼 만들면 안 됨(부분일치 제거)");
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
