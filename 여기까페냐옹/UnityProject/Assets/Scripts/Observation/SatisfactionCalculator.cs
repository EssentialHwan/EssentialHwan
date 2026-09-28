using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.Observation
{
    // 사양서 §3.3 — 만족도. P2: 분위기는 승수(MVP=1.0), P6: 명명 분리.
    public struct SessionResult
    {
        public bool servedFavorite;
        public bool servedPreferredTag;   // 정식
        public bool wasForcedSecond;
        public bool usedPreferredSeat;
        public bool usedPreferredFacility;
        public bool lonelyAndAlone;
        public bool lonelyAndCompanied;
        public bool socialWithFriend;
        public bool fastService;
        public float totalWaitOver;       // 초과 대기 초
        public bool orderFailed;
        public AtmosphereAxis dominantAtmosphere;
        public bool hasDominant;
    }

    public static class SatisfactionCalculator
    {
        public static float Compute(CatData cat, in SessionResult r, BalanceConfig cfg)
        {
            float s = cfg.satBase;

            // 음식 (MVP: favorite만. servedPreferredTag는 정식에서만 세팅됨)
            if (r.servedFavorite) s += cfg.foodFav;
            else if (r.servedPreferredTag) s += cfg.foodTag;
            else if (r.wasForcedSecond) s += cfg.foodForced;

            // 분위기 승수 (MVP=1.0)
            float atmoMult = cfg.atmoNeutral;
            if (cfg.atmosphereAffectsSatisfaction && r.hasDominant)
            {
                if (r.dominantAtmosphere == cat.atmospherePreference) atmoMult = cfg.atmoMatch;
                else if (IsOpposite(r.dominantAtmosphere, cat.atmospherePreference)) atmoMult = cfg.atmoOpposite;
            }

            float seatBonus = r.usedPreferredSeat ? cfg.seatPref : 0;
            float facBonus = r.usedPreferredFacility ? cfg.facilityPref : 0;
            s += (seatBonus + facBonus) * atmoMult;

            // 사회성
            if (cat.HasPersonality(PersonalityTag.Lonely))
                s += r.lonelyAndCompanied ? 10 : (r.lonelyAndAlone ? -10 : 0);
            if (cat.HasPersonality(PersonalityTag.Social) && r.socialWithFriend) s += 10;

            // 서비스/페널티
            if (r.fastService) s += cfg.serviceFast;
            s -= r.totalWaitOver * cfg.waitPenaltyPerSec;
            if (r.orderFailed) s += cfg.orderFail;

            return Mathf.Clamp(s, 0f, 100f);
        }

        static bool IsOpposite(AtmosphereAxis a, AtmosphereAxis b)
            => (a == AtmosphereAxis.Quiet && b == AtmosphereAxis.Lively) ||
               (a == AtmosphereAxis.Lively && b == AtmosphereAxis.Quiet);
    }
}
