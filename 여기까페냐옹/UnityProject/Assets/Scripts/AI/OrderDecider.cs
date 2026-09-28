using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.AI
{
    // 사양서 §2.4 — 음식 선택. MVP는 foodFavorite(정확 일치)만 강판정.
    public struct OrderResult
    {
        public MenuData primary;
        public MenuData extra;          // 식탐 추가 주문
        public bool wasFavorite;
        public bool wasForcedSecond;    // 최애 품절로 차선 선택 → 만족 −5
    }

    public static class OrderDecider
    {
        public static OrderResult Choose(CatData cat, IReadOnlyList<MenuData> available, bool mvpMode = true)
        {
            var result = new OrderResult();
            if (available == null || available.Count == 0) return result; // 전 품절

            MenuData best = null; float bestScore = float.NegativeInfinity;
            bool favoriteAvailable = false;
            foreach (var m in available)
            {
                float s = 0;
                bool isFav = m.menuId == cat.foodFavoriteMenuId;
                if (isFav) { s += 100; favoriteAvailable = true; }
                else if (!mvpMode && SharesFoodTag(m, cat.foodPreferredTags)) s += 40; // 정식(P10)
                if (cat.HasPersonality(PersonalityTag.Glutton) && m.HasTag(FoodTag.Meal)) s += 20;
                s += Random.Range(-10f, 10f);
                if (s > bestScore) { bestScore = s; best = m; }
            }

            result.primary = best;
            result.wasFavorite = best != null && best.menuId == cat.foodFavoriteMenuId;
            // 최애가 메뉴에 정의돼 있으나 이번 판매목록엔 없어서 차선을 고른 경우
            result.wasForcedSecond = !string.IsNullOrEmpty(cat.foodFavoriteMenuId) && !favoriteAvailable;

            if (cat.HasPersonality(PersonalityTag.Glutton) && Random.value < 0.4f && available.Count > 1)
                result.extra = ChooseSecond(best, available);

            return result;
        }

        static MenuData ChooseSecond(MenuData exclude, IReadOnlyList<MenuData> available)
        {
            foreach (var m in available) if (m != exclude) return m;
            return null;
        }

        static bool SharesFoodTag(MenuData m, FoodTag[] prefs)
        {
            if (prefs == null) return false;
            foreach (var p in prefs) if (m.HasTag(p)) return true;
            return false;
        }
    }
}
