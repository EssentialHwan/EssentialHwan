using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Order;
using YeogiCafe.Data;
using YeogiCafe.Loc;

namespace YeogiCafe.UI
{
    // 상점 패널 — 가구 카드 그리드(아이콘·이름·종류·가격). 카드를 누르면 구매·배치.
    // 구매 성공 → CafeContext에 좌석/시설 등록 → CatBrain 후보 등장 → 관찰 채널 개방.
    public class ShopPanel
    {
        readonly GameUiController owner;
        readonly GameObject rootGo;
        readonly Text feedback;
        readonly RectTransform grid;
        readonly List<(FurnitureData data, Text price, Button btn)> cards = new();
        // 탭: 0 가구 · 1 레시피(구매·음식 강화) · 2 가구 강화
        int mode;
        Text slotLabel;
        RectTransform listRoot;
        readonly List<Button> tabs = new();

        public ShopPanel(GameUiController owner, Transform parent)
        {
            this.owner = owner;
            var panel = UiFactory.Panel("ShopPanel", parent, UiFactory.PanelBg);
            rootGo = panel.gameObject;
            UiFactory.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -24), new Vector2(800, 600));
            UiFactory.Frame(panel, UiFactory.FrameStyle.Window);

            // 제목 자리 = 탭 4개(가운데 정렬) — 제목과 탭이 겹치지 않게 탭이 곧 제목

            feedback = UiFactory.Label("Feedback", panel.transform, L.Get("ui.shop.hint"), 18,
                                       TextAnchor.MiddleCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)feedback.transform, new Vector2(0, 0), new Vector2(1, 0),
                              new Vector2(0.5f, 0), new Vector2(130, 14), new Vector2(-320, 30));
            // 남은 칸(바닥·벽) — 사기 전에 자리가 있는지 보이게
            slotLabel = UiFactory.Label("Slots", panel.transform, "", 16, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)slotLabel.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 14), new Vector2(260, 30));
            feedback.verticalOverflow = VerticalWrapMode.Truncate;
            feedback.resizeTextForBestFit = true; feedback.resizeTextMinSize = 12; feedback.resizeTextMaxSize = 18;

            var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(panel.transform, false);
            grid = (RectTransform)gridGo.transform;
            UiFactory.SetRect(grid, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                              new Vector2(0, -66), new Vector2(760, 480));
            var g = gridGo.GetComponent<GridLayoutGroup>();
            // 가구가 21종이라 4열(6줄)로 — 창 밖으로 넘치지 않게
            g.cellSize = new Vector2(184, 74);
            g.spacing = new Vector2(8, 6);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 4;

            var closeBtn = UiFactory.Button("Close", panel.transform, "×", UiFactory.Pink, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)closeBtn.transform, new Vector2(1, 1), new Vector2(1, 1),
                              new Vector2(1, 1), new Vector2(-10, -10), new Vector2(40, 40));
            closeBtn.onClick.AddListener(Hide);

            BuildCards();

            // 레시피·강화 목록(2열 카드)
            var listGo = new GameObject("List", typeof(RectTransform), typeof(GridLayoutGroup));
            listGo.transform.SetParent(panel.transform, false);
            listRoot = (RectTransform)listGo.transform;
            UiFactory.SetRect(listRoot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(760, 480));
            var lg = listGo.GetComponent<GridLayoutGroup>();
            lg.cellSize = new Vector2(374, 76); lg.spacing = new Vector2(10, 6);
            lg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; lg.constraintCount = 2;

            string[] tabKeys = { "ui.shop.tab.furn", "ui.shop.tab.tool", "ui.shop.tab.recipe", "ui.shop.tab.furnup" };
            const float tabW = 150, tabGap = 8;
            float tabsW = tabKeys.Length * tabW + (tabKeys.Length - 1) * tabGap;
            for (int i = 0; i < tabKeys.Length; i++)
            {
                int idx = i;
                var t = UiFactory.Button("Tab" + i, panel.transform, L.Get(tabKeys[i]), UiFactory.Wood, UiFactory.Cream, out var tl);
                tl.fontSize = 18; UiFactory.FitOneLine(tl, tabW - 16, 12);
                UiFactory.SetRect((RectTransform)t.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 1),
                                  new Vector2(-tabsW / 2 - 18 + i * (tabW + tabGap), -22), new Vector2(tabW, 40));
                t.onClick.AddListener(() => { mode = idx; Render(); });
                tabs.Add(t);
            }
            rootGo.SetActive(false);
        }

        public static string FurnitureName(FurnitureData d) => L.Get($"furn.{d.furnitureId}.name");

        static string TypeLabel(FurnitureData d) => d.type switch
        {
            FurnitureType.Seat => L.Get("ui.shop.type.seat"),
            FurnitureType.Facility => L.Get("ui.shop.type.facility"),
            _ => L.Get("ui.shop.type.deco"),
        };

        void BuildCards()
        {
            foreach (var item in owner.ShopItems)
            {
                if (item == null) continue;
                var captured = item;
                var btn = UiFactory.Button("Card_" + item.furnitureId, grid, "", UiFactory.Cream, UiFactory.Ink, out var empty);
                UiFactory.Frame((RectTransform)btn.transform, UiFactory.FrameStyle.Card, UiFactory.Cream);
                Object.Destroy(empty.gameObject);

                // 아이콘(가구 스프라이트 그대로 — 화면 속 모습과 같게)
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(btn.transform, false);
                var icon = iconGo.GetComponent<Image>();
                icon.sprite = item.worldSprite; icon.preserveAspect = true; icon.raycastTarget = false;
                UiFactory.SetRect((RectTransform)iconGo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                  new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(54, 54));

                var name = UiFactory.Label("Name", btn.transform, FurnitureName(item), 17, TextAnchor.UpperLeft, UiFactory.Ink);
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                UiFactory.SetRect((RectTransform)name.transform, new Vector2(0, 1), new Vector2(1, 1),
                                  new Vector2(0, 1), new Vector2(66, -9), new Vector2(-72, 22));
                var type = UiFactory.Label("Type", btn.transform, TypeLabel(item), 13, TextAnchor.UpperLeft, UiFactory.WoodDark);
                UiFactory.SetRect((RectTransform)type.transform, new Vector2(0, 1), new Vector2(1, 1),
                                  new Vector2(0, 1), new Vector2(66, -31), new Vector2(-72, 18));
                var price = UiFactory.Label("Price", btn.transform, $"{item.price}G", 17, TextAnchor.LowerRight, UiFactory.Ink);
                UiFactory.SetRect((RectTransform)price.transform, new Vector2(0, 0), new Vector2(1, 0),
                                  new Vector2(0, 0), new Vector2(66, 9), new Vector2(-78, 22));
                name.raycastTarget = type.raycastTarget = price.raycastTarget = false;

                btn.onClick.AddListener(() => OnBuy(captured));
                cards.Add((item, price, btn));
            }
        }

        void Render()
        {
            grid.gameObject.SetActive(mode == 0);
            listRoot.gameObject.SetActive(mode != 0);
            for (int i = 0; i < tabs.Count; i++)
                tabs[i].GetComponent<Image>().color = i == mode ? UiFactory.PinkDeep : UiFactory.Wood;
            for (int i = listRoot.childCount - 1; i >= 0; i--) Object.Destroy(listRoot.GetChild(i).gameObject);
            if (mode == 1) BuildTools();
            else if (mode == 2) BuildRecipes();
            else if (mode == 3) BuildFurnUpgrades();
            RefreshAffordability();
        }

        // 카드 한 장: 아이콘 · 이름 · 설명 두 줄 · 오른쪽 아래 버튼
        Button Card(Sprite icon, string name, string line1, string line2, string btnText, bool btnOn, Color btnColor)
        {
            var card = UiFactory.Panel("Card", listRoot, UiFactory.Cream);
            UiFactory.Frame(card, UiFactory.FrameStyle.Card, UiFactory.Cream);
            var ig = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ig.transform.SetParent(card, false);
            var im = ig.GetComponent<Image>(); im.sprite = icon; im.preserveAspect = true; im.raycastTarget = false;
            UiFactory.SetRect((RectTransform)ig.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(54, 54));
            var n = UiFactory.Label("Name", card, name, 17, TextAnchor.UpperLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)n.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(68, -5), new Vector2(-76, 22));
            var a = UiFactory.Label("L1", card, line1, 13, TextAnchor.UpperLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)a.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(68, -26), new Vector2(-76, 20));
            UiFactory.FitOneLine(a, 374 - 76, 10);
            var b = UiFactory.Label("L2", card, line2, 13, TextAnchor.UpperLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)b.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(68, -46), new Vector2(140, 20));
            UiFactory.FitOneLine(b, 140, 10);      // 오른쪽 아래 버튼(x≥220)과 안 겹치게
            n.raycastTarget = a.raycastTarget = b.raycastTarget = false;
            var btn = UiFactory.Button("Btn", card, btnText, btnOn ? btnColor : UiFactory.CreamDim, btnOn ? UiFactory.Cream : UiFactory.WoodDark, out var bl);
            bl.fontSize = 14; UiFactory.FitOneLine(bl, 136, 10);
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 5), new Vector2(146, 26));
            btn.interactable = btnOn;
            return btn;
        }

        // 주방 기구: 기구 하나 = 메뉴 2개. 카페 레벨이 되면 산다.
        void BuildTools()
        {
            var s = owner.Save; if (s == null) return;
            int gold = owner.Economy != null ? owner.Economy.Gold : 0;
            foreach (var st in CookingStations.All)
            {
                string toolId = st.id;
                var icon = YeogiCafe.Art.RuntimeSprites.Get(st.id, st.draw);
                var names = new List<string>();
                foreach (var m in CookingStations.MenusFor(toolId)) names.Add(L.Get("menu." + m));
                string l1 = string.Join(" · ", names);
                if (YeogiCafe.Food.RecipeBook.ToolOwned(s, toolId))
                { Card(icon, L.Get(st.nameKey), l1, L.Get("ui.tool.owned"), L.Get("ui.tool.ownedbtn"), false, UiFactory.Wood); continue; }
                var offer = System.Array.Find(YeogiCafe.Food.RecipeBook.Tools, x => x.id == toolId);
                if (offer.id == null) continue;
                bool unlocked = YeogiCafe.Food.RecipeBook.ToolUnlocked(s, offer);
                var btn = Card(icon, L.Get(st.nameKey), l1,
                               unlocked ? L.Get("ui.tool.hint") : L.Get("ui.recipe.locked", ("n", offer.cafeLevel.ToString())),
                               unlocked ? L.Get("ui.recipe.buy", ("g", offer.price.ToString())) : L.Get("ui.recipe.lockedbtn", ("n", offer.cafeLevel.ToString())),
                               unlocked && gold >= offer.price, UiFactory.PinkDeep);
                btn.onClick.AddListener(() => { owner.TryBuyTool(toolId, out var msg); Render(); if (msg != null) feedback.text = msg; });
            }
            feedback.text = L.Get("ui.tool.tabhint");
        }

        // 레시피 강화: 가진 기구의 메뉴만
        void BuildRecipes()
        {
            var s = owner.Save; var flow = owner.Flow;
            if (s == null || flow == null || flow.AllMenus == null) return;
            int gold = owner.Economy != null ? owner.Economy.Gold : 0;
            foreach (var m in flow.AllMenus)
            {
                if (m == null || !YeogiCafe.Food.RecipeBook.Owned(s, m.menuId)) continue;
                var menu = m;
                string name = L.Get("menu." + m.menuId);
                var icon = KitchenView.DishSprite(m.menuId);
                var st = s.GetMenuStat(m.menuId);
                var o = YeogiCafe.Food.RecipeBook.Odds(st.level);
                string l1 = L.Get("ui.recipe.odds", ("a", Mathf.RoundToInt(o.x * 100).ToString()), ("b", Mathf.RoundToInt(o.y * 100).ToString()), ("c", Mathf.RoundToInt(o.z * 100).ToString()));
                string l2 = L.Get("ui.recipe.level", ("n", (st.level + 1).ToString()), ("sold", st.sold.ToString()));
                if (st.level >= YeogiCafe.Food.RecipeBook.MaxLevel) { Card(icon, name, l1, l2, L.Get("ui.recipe.max"), false, UiFactory.Wood); continue; }
                int need = YeogiCafe.Food.RecipeBook.NeedSold(st.level), cost = YeogiCafe.Food.RecipeBook.UpgradeCost(m, st.level);
                bool ok = st.sold >= need;
                var btn = Card(icon, name, l1, l2,
                               ok ? L.Get("ui.recipe.upgrade", ("g", cost.ToString())) : L.Get("ui.recipe.needsoldbtn", ("n", st.sold.ToString()), ("t", need.ToString())),
                               ok && gold >= cost, UiFactory.Green);
                btn.onClick.AddListener(() => { owner.TryUpgradeMenu(menu, out var msg); Render(); if (msg != null) feedback.text = msg; });
            }
            feedback.text = L.Get("ui.recipe.tabhint");
        }

        void BuildFurnUpgrades()
        {
            var s = owner.Save; if (s == null) return;
            int gold = owner.Economy != null ? owner.Economy.Gold : 0;
            var ids = new List<string>();
            foreach (var p in s.placedFurniture) if (!ids.Contains(p.furnitureId)) ids.Add(p.furnitureId);
            foreach (var f in s.furnStats) if (f.uses > 0 && !ids.Contains(f.furnitureId)) ids.Add(f.furnitureId);   // 시작 좌석 등
            int shown = 0;
            foreach (var id in ids)
            {
                var d = owner.LookupFurniture(id); if (d == null || d.type == FurnitureType.Decoration) continue;
                var st = s.GetFurnStat(id);
                string l1 = L.Get("ui.furnup.stars", ("s", YeogiCafe.Food.FurnitureStars.StarText(st.stars)), ("b", YeogiCafe.Food.FurnitureStars.Satisfaction(st.stars).ToString("0")));
                if (st.stars >= YeogiCafe.Food.FurnitureStars.Max) { Card(d.worldSprite, FurnitureName(d), l1, L.Get("ui.furnup.uses", ("n", st.uses.ToString())), L.Get("ui.recipe.max"), false, UiFactory.Wood); shown++; continue; }
                int need = YeogiCafe.Food.FurnitureStars.NeedUses(st.stars), cost = YeogiCafe.Food.FurnitureStars.Cost(d, st.stars);
                bool ok = st.uses >= need;
                var fd = d;
                var btn = Card(d.worldSprite, FurnitureName(d), l1, L.Get("ui.furnup.uses", ("n", st.uses.ToString())),
                               ok ? L.Get("ui.furnup.btn", ("g", cost.ToString())) : L.Get("ui.furnup.needbtn", ("n", st.uses.ToString()), ("t", need.ToString())),
                               ok && gold >= cost, UiFactory.Green);
                btn.onClick.AddListener(() => { owner.TryUpgradeFurniture(fd, out var msg); Render(); if (msg != null) feedback.text = msg; });
                if (++shown >= 12) break;
            }
            feedback.text = shown == 0 ? L.Get("ui.furnup.none") : L.Get("ui.furnup.tabhint");
        }

        void OnBuy(FurnitureData data)
        {
            bool ok = owner.TryBuy(data);
            feedback.text = ok ? L.Get("ui.shop.bought")
                          : owner.LastBuyFailedForSpace ? L.Get("ui.shop.nospace")
                          : L.Get("ui.shop.insufficient");
            RefreshAffordability();
        }

        void RefreshSlots()
        {
            var fm = owner.Furniture;
            if (slotLabel == null) return;
            slotLabel.gameObject.SetActive(mode == 0 && fm != null);
            if (fm == null) return;
            var f = fm.SlotCount(YeogiCafe.Cafe.FurnitureManager.SlotKind.Floor);
            var w = fm.SlotCount(YeogiCafe.Cafe.FurnitureManager.SlotKind.Wall);
            slotLabel.text = L.Get("ui.shop.slots", ("f", f.free.ToString()), ("ft", f.total.ToString()), ("w", w.free.ToString()), ("wt", w.total.ToString()));
            slotLabel.color = f.free == 0 ? UiFactory.PinkDeep : UiFactory.Ink;
            if (f.free >= 20) slotLabel.text = L.Get("ui.shop.slotsmany", ("w", w.free.ToString()));
        }

        // 살 수 없는 가격은 붉게 → 무엇을 목표로 돈을 모을지 보이게
        void RefreshAffordability()
        {
            RefreshSlots();
            int gold = owner.Economy != null ? owner.Economy.Gold : 0;
            foreach (var c in cards)
                c.price.color = gold >= c.data.price ? UiFactory.Ink : UiFactory.PinkDeep;
        }

        float lastToggle = -1f;
        public void Toggle()
        {
            // 같은 클릭이 두 번 들어와 열렸다 바로 닫히는 경우 방지(0.25초 안 재토글 무시)
            if (Time.unscaledTime - lastToggle < 0.25f) return;
            lastToggle = Time.unscaledTime;
            if (rootGo.activeSelf) Hide();
            else
            {
                try { Show(); } catch (System.Exception e) { Debug.LogException(e); rootGo.SetActive(true); }
                rootGo.transform.SetAsLastSibling();   // 주방 화면 위로
            }
        }

        void Show()
        {
            rootGo.SetActive(true);
            rootGo.transform.SetAsLastSibling();
            Render();
            if (mode == 0) feedback.text = L.Get("ui.shop.hint");
        }

        public void Hide() => rootGo.SetActive(false);
        public void DebugTab(int m) { if (!IsOpen) Toggle(); mode = m; Render(); }
        public bool IsOpen => rootGo.activeSelf;
    }
}
