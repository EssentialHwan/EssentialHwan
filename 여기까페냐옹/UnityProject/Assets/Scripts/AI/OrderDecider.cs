using System.Collections.Generic;
using System.Linq;
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
        // 최애를 늘 시키면 음식 취향은 추리할 게 없다 → 최애 60%, 최애와 맛 계열(태그)이 겹치는 메뉴 25%, 나머지 15%.
        // 식탐: 식사(Meal) 메뉴 쪽으로 조금 더. mvpMode는 호환용(무시).
        public static OrderResult Choose(CatData cat, IReadOnlyList<MenuData> available, bool mvpMode = true)
        {
            var result = new OrderResult();
            if (available == null || available.Count == 0) return result; // 전 품절

            MenuData fav = null;
            foreach (var m in available) if (m.menuId == cat.foodFavoriteMenuId) fav = m;
            var related = new List<MenuData>(); var others = new List<MenuData>();
            foreach (var m in available)
            {
                if (m == fav) continue;
                // 최애가 메뉴판에 없으면(미연구·미해금) 고양이의 선호 맛 계열로 대신 묶는다 — 그래야 계열 단서가 끊기지 않음
                bool rel = fav != null ? SharesAnyTag(m, fav) : PrefersTag(cat, m);
                if (rel) related.Add(m); else others.Add(m);
            }

            float r = Random.value;
            // 제철 한정 메뉴: 최애와 같은 맛 계열이면 가끔(20%) 먹어 본다 — 단서는 '천천히 먹음'(최애 아님)
            MenuData season = null;
            if (fav != null) foreach (var m in available) if (m != null && m.seasonal && SeasonBook.Likes(m, fav)) { season = m; break; }
            if (season != null && Random.value < 0.2f) result.primary = season;
            if (result.primary == null && cat.HasPersonality(PersonalityTag.Glutton) && r >= 0.6f && Random.value < 0.35f)
            {
                var meals = available.Where(m => m.HasTag(FoodTag.Meal) && m != fav).ToList();
                if (meals.Count > 0) result.primary = meals[Random.Range(0, meals.Count)];
            }
            if (result.primary == null)
            {
                if (fav != null && r < 0.6f) result.primary = fav;
                else if (related.Count > 0 && (r < 0.85f || others.Count == 0)) result.primary = related[Random.Range(0, related.Count)];
                else if (others.Count > 0) result.primary = others[Random.Range(0, others.Count)];
                else result.primary = fav ?? available[Random.Range(0, available.Count)];
            }

            result.wasFavorite = result.primary != null && result.primary.menuId == cat.foodFavoriteMenuId;
            result.wasForcedSecond = !string.IsNullOrEmpty(cat.foodFavoriteMenuId) && fav == null;

            if (cat.HasPersonality(PersonalityTag.Glutton) && Random.value < 0.4f && available.Count > 1)
                result.extra = ChooseSecond(result.primary, available);

            return result;
        }

        static bool PrefersTag(CatData cat, MenuData m)
        {
            if (cat.foodPreferredTags == null) return false;
            foreach (var t in cat.foodPreferredTags) if (m.HasTag(t)) return true;
            return false;
        }

        static bool SharesAnyTag(MenuData a, MenuData b)
        {
            if (a.foodTags == null || b.foodTags == null) return false;
            foreach (var t in a.foodTags) if (b.HasTag(t)) return true;
            return false;
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
