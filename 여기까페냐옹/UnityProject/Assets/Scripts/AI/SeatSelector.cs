using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.AI
{
    // 사양서 §2.3 — 좌석 선택 + fallback (fallback은 별도 코드가 아니라 '선호 좌석이 후보에 없음'으로 자연 발생).
    // [프리플라이트 수정] MVP에서 부분일치(SharesAnyTag)·분위기 보너스·단골 고정자리(favoriteSeat) 제거.
    //   → 취향 규칙 엄격화: "정확히 선호 좌석이면 지배 점수, 아니면 전부 동일(fallback)".
    //   → 잘못된 추론("Quiet 태그라 구석석 선호") 발생 여지 제거.
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
            BalanceConfig cfg)
        {
            ISeat best = null; float bestScore = float.NegativeInfinity;
            foreach (var s in allSeats)
            {
                if (!s.IsFree) continue;                 // 사용중 제외
                float score = Score(cat, s, entrance, nearbyFriendCount(s), nearbyStrangerCount(s), cfg);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            if (best == null) return new SeatChoice { seat = null, isFallback = false };

            bool matchesPref = Contains(best.Tags, cat.seatPreference);
            return new SeatChoice { seat = best, isFallback = !matchesPref };
        }

        public static float Score(CatData cat, ISeat seat, Vector3 entrance,
                                  int nearbyFriends, int nearbyStrangers, BalanceConfig cfg)
        {
            // Preference (Truth 사용 — 관찰과 무관). MVP: 정확 일치만 지배, 그 외 전부 fallback 동일.
            float pref = Contains(seat.Tags, cat.seatPreference) ? cfg.prefExact : cfg.prefNone;

            // Personality (좌석 형태 성향만 — 분위기/취향과 무관)
            float persona = 0;
            if (cat.HasPersonality(PersonalityTag.Introvert) &&
                (Contains(seat.Tags, SeatTag.Corner) || Contains(seat.Tags, SeatTag.Quiet)))
                persona += cfg.personalityBonus;
            if (cat.HasPersonality(PersonalityTag.Social) && seat.IsCentral) persona += 20;

            // ※ 분위기 보너스 제거(P2): 분위기는 좌석 AI에 영향 주지 않는다.
            //    (오추론 방지 — "Quiet 카페라 구석석 선호" 같은 신호를 만들지 않음)

            // Social (친구 근접만 — 관계 시스템)
            float social = nearbyFriends * cfg.socialFriend;
            if (cat.HasPersonality(PersonalityTag.Introvert)) social -= nearbyStrangers * 20;
            else if (cat.HasPersonality(PersonalityTag.Social)) social += nearbyStrangers * 15;

            float dist = Vector3.Distance(entrance, seat.WorldPos) * cfg.distancePenaltyPerUnit;
            float rnd = Random.Range(-cfg.seatRandom, cfg.seatRandom);

            return pref + persona + social - dist + rnd;
        }

        static bool Contains(IReadOnlyList<SeatTag> tags, SeatTag t)
        {
            for (int i = 0; i < tags.Count; i++) if (tags[i] == t) return true;
            return false;
        }
    }
}
