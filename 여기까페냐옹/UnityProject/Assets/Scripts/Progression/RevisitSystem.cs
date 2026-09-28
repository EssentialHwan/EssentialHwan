using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Progression
{
    // 사양서 §3.7 — 다음날 재방문 롤.
    public static class RevisitSystem
    {
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
