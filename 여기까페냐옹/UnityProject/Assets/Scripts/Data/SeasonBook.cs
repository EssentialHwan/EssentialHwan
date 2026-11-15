using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Data
{
    // 계절(게임 안 7일마다 봄→여름→가을→겨울)과 계절 한정 메뉴.
    // 한정 메뉴는 골드로 한 번 '연구'하면 그 계절마다 메뉴판에 오른다. 추리 정답(최애)은 바꾸지 않는다 —
    // 같은 맛 계열을 좋아하는 고양이가 가끔 시켜 보고, 먹으면 '제철 보너스'로 조금 더 만족한다.
    public static class SeasonBook
    {
        public const int DaysPerSeason = 7;
        public static int Season(int day) => ((Mathf.Max(1, day) - 1) / DaysPerSeason) % 4;   // 0 봄 1 여름 2 가을 3 겨울

        public struct SeasonMenu
        {
            public string id, toolId; public int season, price, cost, research; public float cook; public FoodTag[] tags;
        }

        public static readonly SeasonMenu[] Menus =
        {
            new SeasonMenu { id = "menu_sakura_pudding", toolId = "tool_oven",   season = 0, price = 75, cost = 22, cook = 5f, research = 400, tags = new[] { FoodTag.Sweet } },
            new SeasonMenu { id = "menu_watermelon",     toolId = "tool_board",  season = 1, price = 70, cost = 20, cook = 4f, research = 400, tags = new[] { FoodTag.Fruit } },
            new SeasonMenu { id = "menu_potato_latte",   toolId = "tool_coffee", season = 2, price = 65, cost = 18, cook = 4f, research = 500, tags = new[] { FoodTag.Warm, FoodTag.Sweet } },
            new SeasonMenu { id = "menu_taiyaki",        toolId = "tool_oven",   season = 3, price = 80, cost = 24, cook = 6f, research = 500, tags = new[] { FoodTag.Fish, FoodTag.Sweet } },
        };

        public static bool IsSeasonal(string menuId)
        {
            foreach (var m in Menus) if (m.id == menuId) return true;
            return false;
        }

        public static SeasonMenu? Get(string menuId)
        {
            foreach (var m in Menus) if (m.id == menuId) return m;
            return null;
        }

        static readonly Dictionary<string, MenuData> data = new();
        // 한정 메뉴는 런타임에 만든 MenuData(에셋 생성 없이)
        public static MenuData Data(string menuId)
        {
            if (data.TryGetValue(menuId, out var d) && d != null) return d;
            var def = Get(menuId); if (def == null) return null;
            var m = ScriptableObject.CreateInstance<MenuData>();
            m.name = menuId; m.menuId = menuId; m.displayNameKey = "menu." + menuId;
            m.price = def.Value.price; m.cost = def.Value.cost; m.cookTime = def.Value.cook; m.foodTags = def.Value.tags;
            m.seasonal = true;
            data[menuId] = m;
            return m;
        }

        // 오늘 메뉴판에 올릴 한정 메뉴(연구했고, 지금 계절이고, 만드는 기구가 있을 때)
        public static IEnumerable<MenuData> Today(YeogiCafe.Save.SaveGame s, System.Func<string, bool> toolOwned)
        {
            if (s == null) yield break;
            int season = Season(s.currentDay);
            foreach (var m in Menus)
                if (m.season == season && s.seasonResearched.Contains(m.id) && (toolOwned == null || toolOwned(m.toolId)))
                    yield return Data(m.id);
        }

        // 제철 보너스: 고양이 최애와 맛 계열이 겹치는 한정 메뉴를 먹었을 때
        public const float Bonus = 8f;
        public static bool Likes(MenuData seasonal, MenuData favorite)
        {
            if (seasonal == null || favorite == null || seasonal.foodTags == null) return false;
            foreach (var t in seasonal.foodTags) if (favorite.HasTag(t)) return true;
            return false;
        }
    }
}
