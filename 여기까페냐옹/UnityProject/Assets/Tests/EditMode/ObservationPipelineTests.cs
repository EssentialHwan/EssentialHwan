using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Observation;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 사양서 §17 T1/T2/T5/T11/T13 — 관찰 파이프라인 핵심 불변식 검증.
    public class ObservationPipelineTests
    {
        BalanceConfig cfg;
        SaveGame save;
        ObservationManager obs;
        const string CAT = "cat_cheese";

        [SetUp]
        public void Setup()
        {
            cfg = ScriptableObject.CreateInstance<BalanceConfig>(); // 기본 상수
            save = new SaveGame();
            obs = new ObservationManager(cfg, save);
        }

        // T2 (P1): 창가석 없이 일반석 fallback만 반복 → fallbackSum cap 2 → Suspected 미승격 (가짜 가설 차단)
        [Test]
        public void Fallback_SeatClue_CappedAt2_DoesNotSuspect()
        {
            for (int day = 0; day < 10; day++) // 10일 반복
            {
                obs.RecordClue(CAT, ClueType.SeatUse, "Normal", isFallback: true, null);
                obs.OnDayEnd();
            }
            var seat = save.GetOrCreateCat(CAT).observation.seat;
            var normal = seat.GetOrCreate("Normal");
            Assert.LessOrEqual(normal.fallbackSum, cfg.fallbackCap, "fallback 누계는 cap을 넘지 않아야 한다");
            Assert.AreEqual(ObsState.Unknown, seat.state, "가짜 '일반석 선호' 가설이 생기면 안 된다");
        }

        // T5: 창가석 강한 단서 반복 → Confirmable(★)
        [Test]
        public void StrongSeatClue_Reaches_Confirmable()
        {
            for (int i = 0; i < 3; i++)
            {
                // 강한 좌석 신호(창밖 응시=Seat축, 비-fallback)
                obs.RecordClue(CAT, ClueType.IdleGaze, "Window", isFallback: false, null);
                obs.RecordClue(CAT, ClueType.SeatUse, "Window", isFallback: false, null);
            }
            obs.OnDayEnd();
            var seat = save.GetOrCreateCat(CAT).observation.seat;
            Assert.AreEqual(ObsState.Confirmable, seat.state, "충분한 강한 단서는 Confirmable(★)이어야 한다");
        }

        // T11 (P4): Confirmable에서만 기록 가능 → Recorded(✓)
        [Test]
        public void Record_OnlyFromConfirmable_SetsRecorded()
        {
            // 먼저 Confirmable로 만든다
            for (int i = 0; i < 3; i++)
            {
                obs.RecordClue(CAT, ClueType.IdleGaze, "Window", false, null);
                obs.RecordClue(CAT, ClueType.SeatUse, "Window", false, null);
            }
            obs.OnDayEnd();

            // Suspected(…) 상태에서는 기록 불가여야 하므로, 음식축은 아직 Unknown → 실패 확인
            Assert.IsFalse(obs.TryRecord(CAT, PrefAxis.Food, out _), "Unknown/Suspected 축은 기록 불가");

            // Seat은 Confirmable → 성공
            bool ok = obs.TryRecord(CAT, PrefAxis.Seat, out int bonus);
            Assert.IsTrue(ok);
            Assert.AreEqual(ObsState.Recorded, save.GetOrCreateCat(CAT).observation.seat.state);
            Assert.AreEqual("Window", save.GetOrCreateCat(CAT).observation.seat.recordedValue);
            Assert.Greater(bonus, 0, "발견 보너스(P9) 지급");
        }

        // T13: Truth는 세이브 DTO에 존재하지 않는다 (구조적 보증 — 필드 부재)
        [Test]
        public void SaveModel_DoesNotContain_TruthPreferenceFields()
        {
            var t = typeof(CatSaveData);
            Assert.IsNull(t.GetField("seatPreference"), "세이브에 Truth 좌석 취향 필드가 있으면 안 된다");
            Assert.IsNull(t.GetField("foodFavoriteMenuId"), "세이브에 최애 필드가 있으면 안 된다");
            var obsT = typeof(ObservationRecord);
            Assert.IsNull(obsT.GetField("atmospherePreference"), "관찰기록에 Truth가 있으면 안 된다");
        }
    }
}
