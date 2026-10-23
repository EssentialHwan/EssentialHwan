using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Food
{
    // 주방 기구·메뉴 업그레이드·완성 별점.
    // · 기구 하나 = 메뉴 2개. 1일차는 커피머신·오븐(메뉴 4개)만. 나머지 기구는 카페 레벨이 되면 상점 [주방 기구]에서 산다.
    //   최애 메뉴를 만드는 기구가 없으면 그 고양이는 음식 취향 단서를 못 남긴다(아쉬운 한숨) → 기구를 사면 추리 채널이 열린다.
    // · 레시피 강화: 그 메뉴를 일정 수 이상 팔았고 골드를 내면 단계↑ → 높은 별이 나올 확률↑.
    // · 별(1~3): 완성 순간 단계별 확률로 굴리고, 다 된 뒤 오래 방치하면(신선도) 하나 깎인다.
    public static class RecipeBook
    {
        public static readonly string[] StarterTools = { "tool_coffee", "tool_oven" };

        public struct ToolOffer { public string id; public int cafeLevel; public int price; }
        public static readonly ToolOffer[] Tools =
        {
            new ToolOffer { id = "tool_board",  cafeLevel = 1, price = 350 },
            new ToolOffer { id = "tool_kettle", cafeLevel = 2, price = 300 },
            new ToolOffer { id = "tool_grill",  cafeLevel = 3, price = 600 },
        };

        public static bool IsStarterTool(string toolId) => System.Array.IndexOf(StarterTools, toolId) >= 0;
        public static bool ToolOwned(SaveGame s, string toolId) => IsStarterTool(toolId) || (s != null && s.ownedTools.Contains(toolId));
        public static bool ToolUnlocked(SaveGame s, ToolOffer t) => s != null && s.cafeLevel >= t.cafeLevel;
        public static bool Owned(SaveGame s, string menuId) => ToolOwned(s, YeogiCafe.Order.CookingStations.ToolFor(menuId));

        public static List<MenuData> Available(IEnumerable<MenuData> all, SaveGame s)
            => all.Where(m => m != null && Owned(s, m.menuId)).ToList();

        // 옛 세이브: 산 레시피가 있으면 그 기구를 준 것으로
        public static void Migrate(SaveGame s)
        {
            if (s == null) return;
            foreach (var m in s.ownedRecipes)
            {
                var t = YeogiCafe.Order.CookingStations.ToolFor(m);
                if (!IsStarterTool(t) && !s.ownedTools.Contains(t)) s.ownedTools.Add(t);
            }
            s.ownedRecipes.Clear();
        }

        // ── 업그레이드 ──
        public const int MaxLevel = 3;
        static readonly int[] SoldReq = { 8, 20, 40 };
        static readonly int[] CostMul = { 3, 7, 14 };      // × 판매가
        public static int NeedSold(int level) => level >= MaxLevel ? int.MaxValue : SoldReq[level];
        public static int UpgradeCost(MenuData m, int level) => level >= MaxLevel ? 0 : m.price * CostMul[level];

        // 단계별 별 확률 (1★, 2★, 3★)
        static readonly float[,] StarOdds = { { .50f, .40f, .10f }, { .30f, .50f, .20f }, { .15f, .50f, .35f }, { .05f, .40f, .55f } };
        public static Vector3 Odds(int level) { level = Mathf.Clamp(level, 0, MaxLevel); return new Vector3(StarOdds[level, 0], StarOdds[level, 1], StarOdds[level, 2]); }

        public static int RollStars(int level)
        {
            var o = Odds(level); float r = Random.value;
            return r < o.x ? 1 : r < o.x + o.y ? 2 : 3;
        }

        public const float FreshSeconds = 8f;              // 다 되고 이만큼 지나 서빙하면 별 하나 깎임

        // 별 → 만족도·결제
        public static float StarSatisfaction(int stars) => stars switch { 1 => -6f, 2 => 4f, 3 => 12f, _ => 0f };
        public static float StarPayMult(int stars) => stars switch { 1 => 0.85f, 2 => 1f, 3 => 1.25f, _ => 1f };
        public static string StarText(int stars) => stars <= 0 ? "" : new string('★', stars) + new string('☆', 3 - stars);
    }
}
