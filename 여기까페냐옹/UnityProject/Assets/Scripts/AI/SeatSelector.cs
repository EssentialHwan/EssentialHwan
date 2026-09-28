using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.AI
{
    // 사양서 §2.3 — 좌석 선택 + fallback (fallback은 별도 코드가 아니라 '선호 좌석이 후보에 없음'으로 자연 발생).
    public interface ISeat
    {
        string SeatId { get; }
        IReadOnlyList<SeatTag> Tags { get; }
        Vector3 WorldPos { get; }
        bool IsFree { get; }
        bool IsCentral { get; }
        AtmosphereAxis LocalDominantAxis { get; }
        void Occupy(object cat);
        void Vacate();
    }

    public struct SeatChoice
    {
        public ISeat seat;         // null이면 좌석 없음 → 성격분기(J-B)
        public bool isFallback;    // 선호 좌석이 아니면 true → 약한 단서(P1)
    }

    public static class SeatSelector
    {
        public static SeatChoice Choose(
            CatData cat, IReadOnlyList<ISeat> allSeats, Vector3 entrance,
            System.Func<ISeat, int> nearbyFriendCount,
            System.Func<ISeat, int> nearbyStrangerCount,
            BalanceConfig cfg,
            string favoriteSeatId = null, int regularStage = 1)
        {
            ISeat best = null; float bestScore = float.NegativeInfinity;
            foreach (var s in allSeats)
            {
                if (!s.IsFree) continue;                 // 사용중 제외
                float score = Score(cat, s, entrance, nearbyFriendCount(s), nearbyStrangerCount(s), cfg,
                                    favoriteSeatId, regularStage);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            if (best == null) return new SeatChoice { seat = null, isFallback = false };

            bool matchesPref = Contains(best.Tags, cat.seatPreference);
            return new SeatChoice { seat = best, isFallback = !matchesPref };
        }

        public static float Score(CatData cat, ISeat seat, Vector3 entrance,
                                  int nearbyFriends, int nearbyStrangers, BalanceConfig cfg,
                                  string favoriteSeatId = null, int regularStage = 1)
        {
            // Preference (Truth 사용 — 관찰과 무관)
            float pref;
            if (Contains(seat.Tags, cat.seatPreference))        pref = cfg.prefExact;   // +100 지배
            else if (SharesAnyTag(seat.Tags, cat.seatPreference)) pref = cfg.prefPartial; // +40
            else                                                pref = cfg.prefNone;    // +10

            // 단골 "늘 그 자리"(27장): 단계가 높을수록 이전 자리 강하게 선호
            if (!string.IsNullOrEmpty(favoriteSeatId) && seat.SeatId == favoriteSeatId && regularStage >= 2)
                pref += (regularStage - 1) * 25f;   // 2단계 +25 … 5단계 +100

            // Personality
            float persona = 0;
            if (cat.HasPersonality(PersonalityTag.Introvert) &&
                (Contains(seat.Tags, SeatTag.Corner) || Contains(seat.Tags, SeatTag.Quiet)))
                persona += cfg.personalityBonus;
            if (cat.HasPersonality(PersonalityTag.Social) && seat.IsCentral) persona += 20;

            // Atmosphere (MVP 생략 가능)
            float atmo = seat.LocalDominantAxis == cat.atmospherePreference ? cfg.atmosphereSeatBonus : 0;

            // Social
            float social = nearbyFriends * cfg.socialFriend;
            if (cat.HasPersonality(PersonalityTag.Introvert)) social -= nearbyStrangers * 20;
            else if (cat.HasPersonality(PersonalityTag.Social)) social += nearbyStrangers * 15;

            float dist = Vector3.Distance(entrance, seat.WorldPos) * cfg.distancePenaltyPerUnit;
            float rnd = Random.Range(-cfg.seatRandom, cfg.seatRandom);

            return pref + persona + atmo + social - dist + rnd;
        }

        static bool Contains(IReadOnlyList<SeatTag> tags, SeatTag t)
        {
            for (int i = 0; i < tags.Count; i++) if (tags[i] == t) return true;
            return false;
        }
        // 부분 일치(예: 선호 Window인데 좌석이 Quiet/OutsideView 등 관련 태그 보유)
        static bool SharesAnyTag(IReadOnlyList<SeatTag> tags, SeatTag pref)
        {
            // S1 단순화: 동일 태그 없을 때 관련 그룹만 부분일치로 취급
            foreach (var t in tags)
                if ((pref == SeatTag.Window && (t == SeatTag.OutsideView || t == SeatTag.Quiet)) ||
                    (pref == SeatTag.Corner && t == SeatTag.Quiet))
                    return true;
            return false;
        }
    }
}
