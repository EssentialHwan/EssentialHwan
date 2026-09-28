using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Progression
{
    // 사양서 §3.7 — 다음날 재방문 롤.
    public static class RevisitSystem
    {
        // [프리플라이트 권장] 세이브의 '지난 방문 실제 결과'를 그대로 사용하는 오버로드.
        //   → Recorded 상태가 아니라 실제 이용 여부로 판정(오보너스 방지).
        public static float RevisitChance(CatSaveData cat, int currentDay, BalanceConfig cfg)
            => RevisitChance(cat, currentDay, cat.lastServedFavorite, cat.lastUsedPreferredSeat, cfg);

        public static float RevisitChance(CatSaveData cat, int currentDay,
                                           bool lastServedFavorite, bool lastUsedPreferredSeat,
                                           BalanceConfig cfg)
        {
            float p = cfg.revisitBase;
            p += cat.LastSatisfaction / 100f * cfg.revisitSatWeight;
            if (lastServedFavorite) p += cfg.revisitFav;
            if (lastUsedPreferredSeat) p += cfg.revisitSeat;
            p += RegularBonus(cat.regularStage);
            if (cat.lastVisitDay >= 0) p -= (currentDay - cat.lastVisitDay) * cfg.revisitDecayPerDay;
            return Mathf.Clamp(p, 0.05f, 0.95f);
        }

        static float RegularBonus(int stage) => stage switch
        {
            <= 1 => 0f,
            2 => 0.15f,
            3 => 0.3f,
            4 => 0.45f,
            _ => 0.6f
        };
    }
}
