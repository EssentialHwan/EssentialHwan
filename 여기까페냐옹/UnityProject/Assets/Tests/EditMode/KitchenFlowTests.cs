using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Order;

namespace YeogiCafe.Tests
{
    // 주방 1인칭 조리 흐름: 주문 → 기구 누름(조리 시작) → 완성 → 기구 누름(서빙).
    public class KitchenFlowTests
    {
        GameObject go; OrderSystem os;

        [SetUp] public void SetUp() { go = new GameObject("os"); os = go.AddComponent<OrderSystem>(); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(go); }

        static MenuData Menu(string id, float cook = 3f)
        {
            var m = ScriptableObject.CreateInstance<MenuData>();
            m.menuId = id; m.cookTime = cook; return m;
        }

        [Test]
        public void EveryMenu_HasATool()
        {
            foreach (var id in new[] { "menu_americano", "menu_milk", "menu_fishcake", "menu_parfait",
                                       "menu_tuna_sand", "menu_catnip_tea", "menu_pudding", "menu_grilled_fish" })
            {
                string tool = CookingStations.ToolFor(id);
                Assert.IsTrue(System.Array.Exists(CookingStations.All, s => s.id == tool), $"{id} → {tool}");
            }
        }

        [Test]
        public void PressTool_StartsOldestMatchingOrder_ThenServesWhenReady()
        {
            var coffee = Menu("menu_americano");
            var t1 = os.Place("cat_a", coffee);
            var t2 = os.Place("cat_b", coffee);

            Assert.AreEqual(OrderSystem.ToolResult.Nothing, os.PressTool("tool_oven", out _), "맞는 주문 없는 기구");
            Assert.AreEqual(OrderSystem.ToolResult.Started, os.PressTool("tool_coffee", out var started));
            Assert.AreSame(t1, started, "가장 오래 기다린 주문부터");
            Assert.AreEqual(OrderSystem.ToolResult.Busy, os.PressTool("tool_coffee", out _), "기구 하나에 하나씩");

            t1.cookProgress = 99f; t1.state = TicketState.Ready;   // 조리 완료 시뮬
            bool served = false; os.OnTicketServed += _ => served = true;
            Assert.AreEqual(OrderSystem.ToolResult.Served, os.PressTool("tool_coffee", out _));
            Assert.IsTrue(served);
            Assert.AreEqual(TicketState.Served, t1.state);
            Assert.AreEqual(TicketState.Queued, t2.state, "다음 주문은 대기 유지");
        }

        [Test]
        public void DropForCat_RemovesOnlyThatCatsOrders()
        {
            os.Place("cat_a", Menu("menu_milk"));
            os.Place("cat_b", Menu("menu_milk"));
            os.DropForCat("cat_a");
            Assert.AreEqual(1, os.Tickets.Count);
            Assert.AreEqual("cat_b", os.Tickets[0].catId);
        }
    }
}
