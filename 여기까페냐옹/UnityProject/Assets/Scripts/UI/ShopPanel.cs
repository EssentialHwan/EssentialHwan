using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Data;
using YeogiCafe.Loc;

namespace YeogiCafe.UI
{
    // 상점 패널 — 가구/기물 목록을 나열하고 [구매] 시 실제로 BuyAndPlace를 호출한다.
    // 구매 성공 → CafeContext에 좌석/시설 등록 → CatBrain 후보 등장 → 관찰 채널 개방.
    public class ShopPanel
    {
        readonly GameUiController owner;
        readonly GameObject rootGo;
        Text feedback;
        RectTransform listContent;

        public ShopPanel(GameUiController owner, Transform parent)
        {
            this.owner = owner;
            var panel = UiFactory.Panel("ShopPanel", parent, UiFactory.PanelBg);
            rootGo = panel.gameObject;
            UiFactory.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              Vector2.zero, new Vector2(720, 520));

            UiFactory.Label("Title", panel.transform, "🛒 " + L.Get("ui.shop.buy"), 30,
                            TextAnchor.UpperCenter, UiFactory.Ink);

            feedback = UiFactory.Label("Feedback", panel.transform, L.Get("ui.shop.hint"), 20,
                                       TextAnchor.LowerCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)feedback.transform, new Vector2(0, 0), new Vector2(1, 0),
                              new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(-20, 40));

            // 목록 컨테이너(세로 스택)
            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panel.transform, false);
            listContent = (RectTransform)listGo.transform;
            UiFactory.SetRect(listContent, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -30), new Vector2(-40, -110));
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            var closeBtn = UiFactory.Button("Close", panel.transform, "✕", UiFactory.Pink, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)closeBtn.transform, new Vector2(1, 1), new Vector2(1, 1),
                              new Vector2(1, 1), new Vector2(-10, -10), new Vector2(48, 48));
            closeBtn.onClick.AddListener(Hide);

            BuildRows();
            rootGo.SetActive(false);
        }

        void BuildRows()
        {
            foreach (var item in owner.ShopItems)
            {
                if (item == null) continue;
                var captured = item;
                var row = UiFactory.Panel("Row_" + item.furnitureId, listContent, UiFactory.Cream);
                ((RectTransform)row.transform).sizeDelta = new Vector2(0, 64);

                string name = L.Get(item.displayNameKey);
                var nameLabel = UiFactory.Label("Name", row.transform, $"{name}   {item.price}G", 22,
                                                TextAnchor.MiddleLeft, UiFactory.Ink);
                UiFactory.SetRect((RectTransform)nameLabel.transform, new Vector2(0, 0), new Vector2(1, 1),
                                  new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(-160, 0));

                var buyBtn = UiFactory.Button("Buy", row.transform, L.Get("ui.shop.buy"),
                                              UiFactory.Green, UiFactory.Ink, out _);
                UiFactory.SetRect((RectTransform)buyBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                                  new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(120, 48));
                buyBtn.onClick.AddListener(() => OnBuy(captured));
            }
        }

        void OnBuy(FurnitureData data)
        {
            bool ok = owner.TryBuy(data);
            feedback.text = ok
                ? L.Get("ui.shop.hint")
                : L.Get("ui.shop.insufficient");
        }

        public void Toggle()
        {
            bool next = !rootGo.activeSelf;
            rootGo.SetActive(next);
            if (next) feedback.text = L.Get("ui.shop.hint");
        }
        public void Hide() => rootGo.SetActive(false);
    }
}
