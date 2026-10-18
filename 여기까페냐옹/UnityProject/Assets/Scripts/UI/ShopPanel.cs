using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        public ShopPanel(GameUiController owner, Transform parent)
        {
            this.owner = owner;
            var panel = UiFactory.Panel("ShopPanel", parent, UiFactory.PanelBg);
            rootGo = panel.gameObject;
            UiFactory.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -24), new Vector2(800, 600));
            UiFactory.Frame(panel, UiFactory.FrameStyle.Window);

            var title = UiFactory.Label("Title", panel.transform, L.Get("ui.shop.title"), 26,
                                        TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(0, 40));

            feedback = UiFactory.Label("Feedback", panel.transform, L.Get("ui.shop.hint"), 18,
                                       TextAnchor.MiddleCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)feedback.transform, new Vector2(0, 0), new Vector2(1, 0),
                              new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(-40, 30));

            var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(panel.transform, false);
            grid = (RectTransform)gridGo.transform;
            UiFactory.SetRect(grid, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                              new Vector2(0, -58), new Vector2(760, 490));
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

        void OnBuy(FurnitureData data)
        {
            bool ok = owner.TryBuy(data);
            feedback.text = ok ? L.Get("ui.shop.bought")
                          : owner.LastBuyFailedForSpace ? L.Get("ui.shop.nospace")
                          : L.Get("ui.shop.insufficient");
            RefreshAffordability();
        }

        // 살 수 없는 가격은 붉게 → 무엇을 목표로 돈을 모을지 보이게
        void RefreshAffordability()
        {
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
            if (rootGo.activeSelf) Hide(); else { Show(); rootGo.transform.SetAsLastSibling(); }   // 주방 화면 위로
        }

        void Show()
        {
            rootGo.SetActive(true);
            rootGo.transform.SetAsLastSibling();
            feedback.text = L.Get("ui.shop.hint");
            RefreshAffordability();
        }

        public void Hide() => rootGo.SetActive(false);
        public bool IsOpen => rootGo.activeSelf;
    }
}
