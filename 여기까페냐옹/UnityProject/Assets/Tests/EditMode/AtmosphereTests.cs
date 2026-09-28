using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Cafe;
using YeogiCafe.Data;

namespace YeogiCafe.Tests
{
    // 사양서 §16 / T18·T19 보강 — 분위기 지배 판정 + 승수.
    public class AtmosphereTests
    {
        static FurnitureData Furn(params (AtmosphereAxis, int)[] contribs)
        {
            var f = ScriptableObject.CreateInstance<FurnitureData>();
            var list = new List<FurnitureData.AtmoContribution>();
            foreach (var (axis, val) in contribs)
                list.Add(new FurnitureData.AtmoContribution { axis = axis, value = val });
            f.atmosphere = list.ToArray();
            return f;
        }

        [Test]
        public void Dominant_IsRelativeMax_AboveThreshold()
        {
            var placed = new List<FurnitureData> {
                Furn((AtmosphereAxis.Lively, 4)),   // 캣타워
                Furn((AtmosphereAxis.Quiet, 2)),
            };
            var snap = AtmosphereSystem.Calculate(placed);
            Assert.IsTrue(snap.hasDominant);
            Assert.AreEqual(AtmosphereAxis.Lively, snap.dominant);
        }

        [Test]
        public void NoDominant_WhenBelowThreshold()
        {
            var placed = new List<FurnitureData> { Furn((AtmosphereAxis.Quiet, 2)) }; // <3
            var snap = AtmosphereSystem.Calculate(placed);
            Assert.IsFalse(snap.hasDominant, "임계 미만 → 중립");
        }

        [Test]
        public void NoDominant_WhenTie()
        {
            var placed = new List<FurnitureData> {
                Furn((AtmosphereAxis.Quiet, 4)),
                Furn((AtmosphereAxis.Warm, 4)),
            };
            var snap = AtmosphereSystem.Calculate(placed);
            Assert.IsFalse(snap.hasDominant, "1·2위 동률 → 중립");
        }

        [Test]
        public void Multiplier_Match_Opposite_Neutral()
        {
            var cfg = ScriptableObject.CreateInstance<BalanceConfig>();
            var lively = AtmosphereSystem.Calculate(new List<FurnitureData> { Furn((AtmosphereAxis.Lively, 5)) });

            // 활기참 지배 + 활기참 선호 → 1.1
            Assert.AreEqual(cfg.atmoMatch, AtmosphereSystem.SatisfactionMultiplier(lively, AtmosphereAxis.Lively, cfg), 0.001f);
            // 활기참 지배 + 조용함 선호(반대) → 0.9
            Assert.AreEqual(cfg.atmoOpposite, AtmosphereSystem.SatisfactionMultiplier(lively, AtmosphereAxis.Quiet, cfg), 0.001f);
            // 활기참 지배 + 따뜻함 선호(무관) → 1.0
            Assert.AreEqual(cfg.atmoNeutral, AtmosphereSystem.SatisfactionMultiplier(lively, AtmosphereAxis.Warm, cfg), 0.001f);
        }
    }
}
