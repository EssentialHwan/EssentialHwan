using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Loc;
using YeogiCafe.Order;
using YeogiCafe.Visual;

namespace YeogiCafe.UI
{
    // 주방(1인칭 카운터 시점).
    // 화면 아래 40%: 카운터 상판 + 조리 기구 5종 + 바리스타 고양이 앞발 두 개(전경, UI).
    // 화면 위 60%: 실제 카페 홀(카메라를 뒤로 빼서 방 전체를 위쪽에 담음) — 손님이 실시간으로 계속 보인다.
    // 조리 대기 시간 = 관찰 시간. 홀 말풍선(행동 서술)도 이 화면에서 그대로 뜬다.
    public class KitchenView
    {
        const float CounterHeightRatio = 0.40f;         // 전경이 차지하는 화면 비율
        const float TransitionSeconds = 0.35f;

        readonly GameUiController owner;
        readonly OrderSystem orders;
        readonly GameObject rootGo;
        readonly RectTransform root;
        readonly RectTransform pawL, pawR, ticketRail;
        readonly List<ToolWidget> tools = new();
        readonly List<GameObject> ticketCards = new();
        readonly Text hint;

        class ToolWidget
        {
            public string id; public RectTransform rt; public Image glow;
            public Image barBg, barFill; public Image dish; public Text label;
        }

        // 카메라 전환
        Camera cam; Vector3 hallPos; float hallSize;
        float t; bool open;
        Vector2 pawLHome, pawRHome;
        float pawPunchT = -1f; RectTransform pawPunch; Vector2 pawPunchTarget;
        float railTimer;

        public bool IsOpen => open;

        public KitchenView(GameUiController owner, Transform canvas, OrderSystem orders)
        {
            this.owner = owner; this.orders = orders;
            var go = new GameObject("KitchenView", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            root = (RectTransform)go.transform;
            UiFactory.Anchor(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rootGo = go;

            // ── 카운터(전경 하단): 원목 앞판 + 그 위 대리석 상판 띠 ──
            var front = TiledImage("CounterFront", RuntimeSprites.Get("counter_front", CookingDrawing.CounterFrontTile));
            UiFactory.Anchor(front, Vector2.zero, new Vector2(1, CounterHeightRatio), Vector2.zero, new Vector2(0, -64));
            var top = TiledImage("CounterTop", RuntimeSprites.Get("counter_top", CookingDrawing.CounterTopTile));
            UiFactory.Anchor(top, new Vector2(0, CounterHeightRatio), new Vector2(1, CounterHeightRatio), new Vector2(0, -64), Vector2.zero);

            // 카운터 윗면 가장자리 그림자(홀과 전경 경계)
            var edge = UiFactory.Panel("CounterEdge", root, new Color(0.29f, 0.21f, 0.18f, 0.6f));
            UiFactory.Anchor(edge, new Vector2(0, CounterHeightRatio), new Vector2(1, CounterHeightRatio), new Vector2(0, 0), new Vector2(0, 4));

            // ── 기구 5종(카운터 위에 한 줄) ──
            int n = CookingStations.All.Length;
            for (int i = 0; i < n; i++)
            {
                var st = CookingStations.All[i];
                float x = Mathf.Lerp(0.2f, 0.8f, n == 1 ? 0.5f : i / (float)(n - 1));   // 양 끝은 앞발 자리
                tools.Add(BuildTool(st, x));
            }

            // ── 주문 레일(카운터 바로 위, 홀과의 경계) ──
            var railGo = new GameObject("TicketRail", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            railGo.transform.SetParent(root, false);
            ticketRail = (RectTransform)railGo.transform;
            // 주문 레일은 화면 왼쪽 위(HUD 바로 아래) — 카운터 위 기구를 가리지 않게
            UiFactory.SetRect(ticketRail, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -72), new Vector2(-40, 64));
            var hl = railGo.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 8; hl.childAlignment = TextAnchor.UpperLeft;
            hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = hl.childForceExpandHeight = false;

            // ── 앞발 두 개(가장 앞) ──
            pawL = BuildPaw("PawL", RuntimeSprites.Get("paw_left", CookingDrawing.PawLeft), new Vector2(0.08f, 0f));
            pawR = BuildPaw("PawR", RuntimeSprites.Get("paw_right", CookingDrawing.PawRight), new Vector2(0.92f, 0f));
            pawLHome = pawL.anchoredPosition; pawRHome = pawR.anchoredPosition;

            hint = UiFactory.Label("Hint", root, "", 16, TextAnchor.LowerCenter, UiFactory.Cream);
            UiFactory.SetRect((RectTransform)hint.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                              new Vector2(0, 6), new Vector2(600, 22));
            hint.text = L.Get("ui.kitchen.hint");

            rootGo.SetActive(false);
        }

        // 도트 타일을 화면에서 약 4배로 반복(Point 필터라 선명)
        RectTransform TiledImage(string name, Sprite s)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(root, false);
            var img = go.GetComponent<Image>();
            img.sprite = s; img.type = Image.Type.Tiled; img.raycastTarget = false;
            img.pixelsPerUnitMultiplier = 100f / (32f * 4f);   // 스프라이트 PPU32·캔버스 참조100 기준 4배
            return (RectTransform)go.transform;
        }

        ToolWidget BuildTool(CookingStations.Station st, float xNorm)
        {
            var w = new ToolWidget { id = st.id };
            var btnGo = new GameObject("Tool_" + st.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(root, false);
            w.rt = (RectTransform)btnGo.transform;
            // 기구는 대리석 상판 위에 올려놓은 것처럼(상판 띠 중간에 바닥선)
            UiFactory.SetRect(w.rt, new Vector2(xNorm, CounterHeightRatio), new Vector2(xNorm, CounterHeightRatio),
                              new Vector2(0.5f, 0f), new Vector2(0, -40), new Vector2(144, 144));    // 48px × 3배
            var img = btnGo.GetComponent<Image>();
            img.sprite = RuntimeSprites.Get(st.id, st.draw);
            img.preserveAspect = true;
            var btn = btnGo.GetComponent<Button>();
            var colors = btn.colors; colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f); colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = colors;
            string id = st.id;
            btn.onClick.AddListener(() => OnToolPressed(id, w));

            // 할 일 있음 표시(은은한 노란 테두리)
            w.glow = UiFactory.Panel("Glow", w.rt, new Color(1f, 0.9f, 0.5f, 0f)).GetComponent<Image>();
            UiFactory.Anchor((RectTransform)w.glow.transform, Vector2.zero, Vector2.one, new Vector2(-6, -6), new Vector2(6, 6));
            w.glow.transform.SetAsFirstSibling(); w.glow.raycastTarget = false;

            w.label = UiFactory.Label("Name", w.rt, L.Get(st.nameKey), 16, TextAnchor.UpperCenter, UiFactory.Cream);
            UiFactory.SetRect((RectTransform)w.label.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1),
                              new Vector2(0, -2), new Vector2(160, 20));
            w.label.raycastTarget = false;

            // 조리 진행 바
            w.barBg = UiFactory.Panel("BarBg", w.rt, UiFactory.WoodDark).GetComponent<Image>();
            UiFactory.SetRect((RectTransform)w.barBg.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0),
                              new Vector2(0, 6), new Vector2(110, 12));
            w.barFill = UiFactory.Panel("BarFill", w.barBg.transform, UiFactory.Green).GetComponent<Image>();
            w.barFill.sprite = UiFactory.WhiteSprite; w.barFill.type = Image.Type.Filled; w.barFill.fillMethod = Image.FillMethod.Horizontal;
            UiFactory.Anchor((RectTransform)w.barFill.transform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            w.barBg.raycastTarget = w.barFill.raycastTarget = false;

            // 완성품(통통 튀는 접시)
            var dishGo = new GameObject("Dish", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dishGo.transform.SetParent(w.rt, false);
            w.dish = dishGo.GetComponent<Image>(); w.dish.preserveAspect = true; w.dish.raycastTarget = false;
            UiFactory.SetRect((RectTransform)dishGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0),
                              new Vector2(0, 22), new Vector2(72, 72));
            return w;
        }

        RectTransform BuildPaw(string name, Sprite sprite, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(root, false);
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            UiFactory.SetRect(rt, anchor, anchor, new Vector2(0.5f, 0f), new Vector2(0, -110), new Vector2(200, 280));  // 40×56px × 5배
            return rt;
        }

        // ── 열기/닫기 ──
        public void Toggle() { if (open) Close(); else Open(); }

        public void Open()
        {
            if (open) return;
            cam = Camera.main;
            hallPos = CafeLayout.CameraPos; hallSize = CafeLayout.CameraSize;   // 전환 도중 다시 열어도 기준이 흔들리지 않게
            open = true; t = 0f;
            rootGo.SetActive(true);
            rootGo.transform.SetAsLastSibling();
            RefreshRail(true);
        }

        public void Close()
        {
            if (!open) return;
            open = false; t = 0f;
        }

        // 홀 카메라 목표: 방(y 0~7.5)이 화면 위 60%에 들어오도록 뒤로 빠지고 아래로 내림
        void KitchenCam(out Vector3 pos, out float size)
        {
            float roomTop = 7.6f, roomBottom = -0.1f;
            float visible = (roomTop - roomBottom) / (1f - CounterHeightRatio);
            size = visible * 0.5f;
            pos = new Vector3(CafeLayout.CameraPos.x, roomTop - size, CafeLayout.CameraPos.z);
        }

        public void Tick(float dt)
        {
            // 카메라 보간(열 때·닫을 때 모두)
            if (cam != null && (open || rootGo.activeSelf))
            {
                t = Mathf.Min(1f, t + dt / TransitionSeconds);
                float k = t * t * (3f - 2f * t);
                KitchenCam(out var kPos, out var kSize);
                if (open)
                {
                    cam.transform.position = Vector3.Lerp(hallPos, kPos, k);
                    cam.orthographicSize = Mathf.Lerp(hallSize, kSize, k);
                }
                else
                {
                    cam.transform.position = Vector3.Lerp(kPos, hallPos, k);
                    cam.orthographicSize = Mathf.Lerp(kSize, hallSize, k);
                    if (t >= 1f) rootGo.SetActive(false);
                }
                // 전경은 아래에서 올라오듯
                root.anchoredPosition = new Vector2(0, (open ? (1f - k) : k) * -300f);
            }
            if (!rootGo.activeSelf) return;

            AnimatePaws(dt);
            RefreshTools();
            railTimer -= dt;
            if (railTimer <= 0f) { railTimer = 0.25f; RefreshRail(false); }
        }

        void AnimatePaws(float dt)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 2.2f) * 6f;
            pawL.anchoredPosition = pawLHome + new Vector2(0, bob);
            pawR.anchoredPosition = pawRHome + new Vector2(0, -bob);
            if (pawPunch != null && pawPunchT >= 0f)
            {
                pawPunchT += dt;
                const float outT = 0.10f, backT = 0.18f;
                Vector2 home = pawPunch == pawL ? pawLHome : pawRHome;
                float k = pawPunchT < outT ? pawPunchT / outT : 1f - Mathf.Clamp01((pawPunchT - outT) / backT);
                pawPunch.anchoredPosition = Vector2.Lerp(home, pawPunchTarget, k * 0.55f);
                if (pawPunchT > outT + backT) { pawPunchT = -1f; pawPunch = null; }
            }
        }

        void Punch(ToolWidget w)
        {
            // 화면 왼쪽 절반 기구는 왼발, 오른쪽은 오른발이 톡
            bool left = w.rt.anchorMin.x < 0.5f;
            pawPunch = left ? pawL : pawR;
            var home = left ? pawLHome : pawRHome;
            Vector2 screenDelta = new Vector2((w.rt.anchorMin.x - (left ? 0.08f : 0.92f)) * root.rect.width, 180f);
            pawPunchTarget = home + screenDelta;
            pawPunchT = 0f;
        }

        void OnToolPressed(string id, ToolWidget w)
        {
            if (orders == null) return;
            Punch(w);
            var r = orders.PressTool(id, out var ticket);
            switch (r)
            {
                case OrderSystem.ToolResult.Started:
                    hint.text = L.Get("ui.kitchen.cooking", ("menu", MenuName(ticket)));
                    break;
                case OrderSystem.ToolResult.Served:
                    hint.text = L.Get("ui.kitchen.served", ("name", CatName(ticket)), ("menu", MenuName(ticket)));
                    owner.ShowCatBubble(ticket.catId, L.Get("ui.kitchen.thanks"));
                    break;
                case OrderSystem.ToolResult.Busy:
                    hint.text = L.Get("ui.kitchen.busy");
                    break;
                default:
                    hint.text = L.Get("ui.kitchen.noorder");
                    break;
            }
            RefreshRail(true);
        }

        void RefreshTools()
        {
            float pulse = 0.35f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
            foreach (var w in tools)
            {
                var active = orders != null ? orders.CookingOrReadyAt(w.id) : null;
                bool hasQueued = orders != null && orders.OldestQueuedFor(w.id) != null;
                w.barBg.gameObject.SetActive(active != null && active.state == TicketState.Cooking);
                if (active != null && active.state == TicketState.Cooking) w.barFill.fillAmount = active.CookRatio;

                bool ready = active != null && active.state == TicketState.Ready;
                w.dish.gameObject.SetActive(ready);
                if (ready)
                {
                    w.dish.sprite = DishSprite(active.menu != null ? active.menu.menuId : "");
                    var drt = (RectTransform)w.dish.transform;
                    drt.anchoredPosition = new Vector2(0, 22 + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 10f);
                }
                // 누를 일이 있는 기구만 빛나게(완성품 서빙 or 대기 주문 조리)
                bool actionable = ready || (active == null && hasQueued);
                var g = w.glow.color; g.a = actionable ? pulse : 0f; w.glow.color = g;
            }
        }

        void RefreshRail(bool force)
        {
            if (orders == null) return;
            foreach (var c in ticketCards) Object.Destroy(c);
            ticketCards.Clear();
            foreach (var tk in orders.Tickets)
            {
                if (tk.state == TicketState.Served || tk.state == TicketState.Cancelled) continue;
                var card = UiFactory.Panel("Ticket_" + tk.catId, ticketRail, UiFactory.Cream);
                card.sizeDelta = new Vector2(150, 60);
                ticketCards.Add(card.gameObject);

                var catIcon = Icon(card, CatSprite(tk.catId), new Vector2(4, 4), 44);
                var dish = Icon(card, DishSprite(tk.menu != null ? tk.menu.menuId : ""), new Vector2(48, 10), 36);
                var nameT = UiFactory.Label("Menu", card, MenuName(tk), 14, TextAnchor.UpperLeft, UiFactory.Ink);
                UiFactory.SetRect((RectTransform)nameT.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                                  new Vector2(88, -6), new Vector2(60, 34));
                nameT.horizontalOverflow = HorizontalWrapMode.Wrap;

                // 상태: 대기(인내심 바) / 조리 중 / 완성
                var bar = UiFactory.Panel("Patience", card, UiFactory.WoodDark);
                UiFactory.SetRect(bar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(4, 4), new Vector2(-8, 6));
                var fill = UiFactory.Panel("Fill", bar, tk.state == TicketState.Queued
                                            ? (tk.PatienceRatio > 0.35f ? UiFactory.Green : UiFactory.PinkDeep)
                                            : UiFactory.YellowLight).GetComponent<Image>();
                fill.sprite = UiFactory.WhiteSprite; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillAmount = tk.state == TicketState.Queued ? tk.PatienceRatio : tk.state == TicketState.Cooking ? tk.CookRatio : 1f;
                UiFactory.Anchor((RectTransform)fill.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        static Image Icon(RectTransform parent, Sprite s, Vector2 pos, float size)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.sprite = s; img.preserveAspect = true; img.raycastTarget = false;
            UiFactory.SetRect((RectTransform)go.transform, Vector2.zero, Vector2.zero, Vector2.zero, pos, new Vector2(size, size));
            return img;
        }

        public static Sprite DishSprite(string menuId) => RuntimeSprites.Get("dish_" + menuId, () => CookingDrawing.Dish(menuId));

        Sprite CatSprite(string catId)
        {
            if (string.IsNullOrEmpty(catId)) return null;
            return CatSpriteBank.Portrait(catId);
        }

        string CatName(OrderTicket t) => t == null ? "" : L.Get($"cat.{t.catId}.name");
        static string MenuName(OrderTicket t) => t == null || t.menu == null ? "" : L.Get($"menu.{t.menu.menuId}");
    }
}
