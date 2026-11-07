using System.Collections.Generic;
using YeogiCafe.Art;

namespace YeogiCafe.Order
{
    // 주방 기구 5종 × 메뉴 2개 매핑(데이터 한 곳). 기구를 누르면 메뉴 고르기 창 → 고른 메뉴를 기다리는 주문을 조리한다.
    public static class CookingStations
    {
        public struct Station
        {
            public string id, nameKey;
            public System.Func<PixelCanvas> draw;
        }

        public static readonly Station[] All =
        {
            new Station { id = "tool_coffee", nameKey = "tool.coffee", draw = CookingDrawing.ToolCoffee },
            new Station { id = "tool_kettle", nameKey = "tool.kettle", draw = CookingDrawing.ToolKettle },
            new Station { id = "tool_oven",   nameKey = "tool.oven",   draw = CookingDrawing.ToolOven },
            new Station { id = "tool_board",  nameKey = "tool.board",  draw = CookingDrawing.ToolBoard },
            new Station { id = "tool_grill",  nameKey = "tool.grill",  draw = CookingDrawing.ToolGrill },
        };

        static readonly Dictionary<string, string> menuToTool = new()
        {
            { "menu_americano", "tool_coffee" }, { "menu_latte", "tool_coffee" },
            { "menu_milk", "tool_kettle" },     { "menu_catnip_tea", "tool_kettle" },
            { "menu_fishcake", "tool_oven" },   { "menu_pudding", "tool_oven" },
            { "menu_tuna_sand", "tool_board" }, { "menu_parfait", "tool_board" },
            { "menu_grilled_fish", "tool_grill" }, { "menu_skewer", "tool_grill" },
            // 계절 한정(SeasonBook)
            { "menu_sakura_pudding", "tool_oven" }, { "menu_watermelon", "tool_board" },
            { "menu_potato_latte", "tool_coffee" }, { "menu_taiyaki", "tool_oven" },
        };

        public static string NameKey(string toolId)
        {
            foreach (var s in All) if (s.id == toolId) return s.nameKey;
            return toolId;
        }
        public static Station? Get(string toolId)
        {
            foreach (var s in All) if (s.id == toolId) return s;
            return null;
        }

        public static IEnumerable<string> MenusFor(string toolId)
        {
            foreach (var kv in menuToTool) if (kv.Value == toolId && !YeogiCafe.Data.SeasonBook.IsSeasonal(kv.Key)) yield return kv.Key;   // 기구 기본 메뉴 2개(한정 메뉴 제외)
        }

        public static string ToolFor(string menuId)
            => menuId != null && menuToTool.TryGetValue(menuId, out var t) ? t : "tool_board";
    }
}
