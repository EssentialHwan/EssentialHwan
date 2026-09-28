using System.Collections.Generic;
using YeogiCafe.Data;

namespace YeogiCafe.Cafe
{
    // 사양서 §7 / §16 — 분위기 계산. 배치 가구의 기여 합산 → 지배 축(상대 최댓값).
    // MVP: 만족 미반영(시각효과·라벨만). 정식: SatisfactionCalculator에 승수 제공.
    // 순수 계산 클래스(테스트 용이). FurnitureManager가 배치/제거 시 Recalc 호출.
    public struct AtmosphereSnapshot
    {
        public int quiet, lively, warm, natural, luxury;
        public AtmosphereAxis dominant;
        public bool hasDominant;

        public int Get(AtmosphereAxis a) => a switch
        {
            AtmosphereAxis.Quiet => quiet,
            AtmosphereAxis.Lively => lively,
            AtmosphereAxis.Warm => warm,
            AtmosphereAxis.Natural => natural,
            _ => luxury
        };
    }

    public static class AtmosphereSystem
    {
        // 지배 판정 임계: 1위가 이 값 미만이면 중립(None). 1·2위 동률도 중립.
        public const int DominanceThreshold = 3;

        public static AtmosphereSnapshot Calculate(IEnumerable<FurnitureData> placed)
        {
            var s = new AtmosphereSnapshot();
            foreach (var f in placed)
            {
                if (f == null || f.atmosphere == null) continue;
                foreach (var c in f.atmosphere)
                {
                    switch (c.axis)
                    {
                        case AtmosphereAxis.Quiet: s.quiet += c.value; break;
                        case AtmosphereAxis.Lively: s.lively += c.value; break;
                        case AtmosphereAxis.Warm: s.warm += c.value; break;
                        case AtmosphereAxis.Natural: s.natural += c.value; break;
                        case AtmosphereAxis.Luxury: s.luxury += c.value; break;
                    }
                }
            }
            ComputeDominant(ref s);
            return s;
        }

        static void ComputeDominant(ref AtmosphereSnapshot s)
        {
            AtmosphereAxis top = AtmosphereAxis.Quiet;
            int topVal = int.MinValue, secondVal = int.MinValue;
            foreach (AtmosphereAxis a in System.Enum.GetValues(typeof(AtmosphereAxis)))
            {
                int v = s.Get(a);
                if (v > topVal) { secondVal = topVal; topVal = v; top = a; }
                else if (v > secondVal) { secondVal = v; }
            }
            // 상대 최댓값 + 임계 + 동률 아님
            s.hasDominant = topVal >= DominanceThreshold && topVal > secondVal;
            s.dominant = top;
        }

        // 정식 만족 승수(문서 06 / P2). MVP는 호출 안 함.
        public static float SatisfactionMultiplier(AtmosphereSnapshot snap, AtmosphereAxis catPref, BalanceConfig cfg)
        {
            if (!snap.hasDominant) return cfg.atmoNeutral;
            if (snap.dominant == catPref) return cfg.atmoMatch;
            if (IsOpposite(snap.dominant, catPref)) return cfg.atmoOpposite;
            return cfg.atmoNeutral;
        }

        static bool IsOpposite(AtmosphereAxis a, AtmosphereAxis b)
            => (a == AtmosphereAxis.Quiet && b == AtmosphereAxis.Lively)
            || (a == AtmosphereAxis.Lively && b == AtmosphereAxis.Quiet);
    }
}
