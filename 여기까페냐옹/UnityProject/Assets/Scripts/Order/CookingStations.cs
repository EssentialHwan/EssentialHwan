using System.Collections.Generic;
using YeogiCafe.Art;

namespace YeogiCafe.Order
{
    // 주방 기구 5종과 메뉴 매핑(데이터 한 곳). 기구를 누르면 그 기구로 만드는 메뉴 중 가장 오래된 주문을 조리한다.
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
            { "menu_americano", "tool_coffee" },
            { "menu_milk", "tool_kettle" },     { "menu_catnip_tea", "tool_kettle" },
            { "menu_fishcake", "tool_oven" },   { "menu_pudding", "tool_oven" },
            { "menu_tuna_sand", "tool_board" }, { "menu_parfait", "tool_board" },
            { "menu_grilled_fish", "tool_grill" },
        };

        public static string ToolFor(string menuId)
            => menuId != null && menuToTool.TryGetValue(menuId, out var t) ? t : "tool_board";
    }
}
