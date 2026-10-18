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

        // 고양이 본능: 가끔 한쪽 앞발이 허공 꾹꾹이·그루밍을 시작한다. 그동안 그쪽 앞발로는 기구를 못 누른다(반대 발은 가능).
        enum Distract { None, Knead, Groom, Suckle }
        RectTransform tongue;                        // 1인칭 그루밍: 화면 아래(입)에서 올라오는 혀
        class Fx { public RectTransform rt; public Text t; public float life; }
        readonly List<Fx> fx = new();
        Distract distract; RectTransform distractPaw; float distractT, nextDistract = 20f;
        const float DistractSeconds = 2.4f;
        float DistractLen => distract == Distract.Groom ? 3.0f : distract == Distract.Suckle ? 3.2f : DistractSeconds;
        public bool PawBusy(bool left) => distract != Distract.None && distractPaw == (left ? pawL : pawR);

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
            {
                var tg = new GameObject("Tongue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                tg.transform.SetParent(root, false);
                var timg = tg.GetComponent<Image>(); timg.raycastTarget = false;
                timg.sprite = RuntimeSprites.Get("my_tongue", PawDrawing.Tongue);
                tongue = (RectTransform)tg.transform;
                UiFactory.SetRect(tongue, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -10), new Vector2(PawDrawing.TW, PawDrawing.TH) * 3.4f);
                tongue.localScale = new Vector3(1f, 0f, 1f);
                tongue.SetSiblingIndex(pawL.GetSiblingIndex());     // 앞발보다 뒤
            }

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
            UiFactory.SetRect(rt, anchor, anchor, new Vector2(0.5f, 0f), new Vector2(0, -40), new Vector2(PawDrawing.W, PawDrawing.H) * 3.4f);  // 고해상도 앞발
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

        void TickDistract(float dt)
        {
            if (distract == Distract.None)
            {
                if (pawPunch != null) return;
                nextDistract -= dt;
                if (nextDistract > 0f) return;
                float r = Random.value;
                distract = r < 0.4f ? Distract.Knead : r < 0.75f ? Distract.Groom : Distract.Suckle;
                distractPaw = Random.value < 0.5f ? pawL : pawR;
                distractT = 0f;
                hint.text = L.Get(distract == Distract.Knead ? "ui.kitchen.knead" : distract == Distract.Groom ? "ui.kitchen.groom" : "ui.kitchen.suckle");
                return;
            }
            distractT += dt;
            if (distractT < DistractLen) return;
            tongue.localScale = new Vector3(1f, 0f, 1f);
            distract = Distract.None; nextDistract = Random.Range(18f, 34f);
            distractPaw.localEulerAngles = Vector3.zero; distractPaw.localScale = Vector3.one;
            bool left = distractPaw == pawL;
            distractPaw.GetComponent<Image>().sprite = RuntimeSprites.Get(left ? "paw_left" : "paw_right", left ? CookingDrawing.PawLeft : CookingDrawing.PawRight);
            distractPaw = null;
        }

        void AnimateDistract()
        {
            if (distract == Distract.None || distractPaw == null) return;
            bool left = distractPaw == pawL;
            var home = left ? pawLHome : pawRHome;
            float rise = Mathf.Clamp01(distractT / 0.25f) * Mathf.Clamp01((DistractLen - distractT) / 0.25f);   // 들어올렸다 내려놓기
            var img = distractPaw.GetComponent<Image>();
            if (distract == Distract.Knead)
            {
                // 허공에서 발가락 쫙 폈다 오므렸다(꾹·꾹)
                int kb = (int)(distractT / 0.2f);
                bool press = kb % 2 == 0;
                if (press && kb != lastLick && rise > 0.5f) { lastLick = kb; YeogiCafe.Audio.SoundManager.Sfx("sfx_knead", 0.35f); }
                distractPaw.anchoredPosition = home + new Vector2(0, rise * (110f + (press ? -14f : 0f)));
                img.sprite = press
                    ? RuntimeSprites.Get(left ? "paw_left_press" : "paw_right_press", left ? CookingDrawing.PawLeftPressed : CookingDrawing.PawRightPressed)
                    : RuntimeSprites.Get(left ? "paw_left" : "paw_right", left ? CookingDrawing.PawLeft : CookingDrawing.PawRight);
                distractPaw.localEulerAngles = Vector3.zero;
            }
            else if (distract == Distract.Groom)
            {
                // 1인칭 그루밍: 앞발을 입(화면 아래 가운데) 앞으로 가져오면 혀가 쭉 올라와 발등을 할짝 — 혀가 지나갈 때 발이 살짝 밀려 올라감
                float toward = (left ? 1f : -1f) * root.rect.width * 0.30f;
                float cyc = (distractT * 2.6f) % 1f;                                 // 한 번 핥는 주기
                float lickUp = cyc < 0.45f ? cyc / 0.45f : 1f - (cyc - 0.45f) / 0.55f;
                float push = Mathf.Clamp01((lickUp - 0.6f) / 0.4f);
                distractPaw.anchoredPosition = home + new Vector2(toward * rise, rise * (150f + push * 22f));
                distractPaw.localEulerAngles = new Vector3(0, 0, rise * ((left ? -28f : 28f) + push * (left ? 6f : -6f)));
                float k = rise > 0.95f ? Mathf.SmoothStep(0f, 1f, lickUp) : 0f;
                tongue.anchoredPosition = new Vector2(AbsPos(distractPaw).x - root.rect.width * 0.5f, -10f);   // 혀는 핥는 앞발 바로 아래에서
                tongue.localScale = new Vector3(1f - k * 0.08f, k * 0.95f, 1f);
                tongue.localEulerAngles = new Vector3(0, 0, left ? 8f : -8f);
                if (k > 0.85f && ((int)(distractT * 2.6f)) != lastLick) { lastLick = (int)(distractT * 2.6f); Float(AbsPos(distractPaw) + new Vector2(0, 120), L.Get("ui.kitchen.lick")); YeogiCafe.Audio.SoundManager.Sfx("sfx_lick", 0.5f); }
            }
            else
            {
                // 내 쭙쭙이: 앞발을 뒤집어 말랑한 젤리를 펼치고, 입가에 대고 쭙·쭙(리듬 맞춰 살짝 빨려 들어감)
                float toward = (left ? 1f : -1f) * root.rect.width * 0.30f;
                float suck = Mathf.Pow(Mathf.Abs(Mathf.Sin(distractT * 6.5f)), 3f);
                img.sprite = RuntimeSprites.Get(left ? "paw_pads_l" : "paw_pads_r", left ? (System.Func<PixelCanvas>)PawDrawing.Pads : () => PawDrawing.Mirror(PawDrawing.Pads()));
                distractPaw.anchoredPosition = home + new Vector2(toward * rise, rise * (120f - suck * 18f));
                distractPaw.localEulerAngles = new Vector3(0, 0, rise * (left ? -16f : 16f));
                distractPaw.localScale = new Vector3(1f + suck * 0.04f, 1f - suck * 0.05f, 1f);
                int beat = (int)(distractT * 6.5f / Mathf.PI);
                if (rise > 0.9f && beat != lastLick) { lastLick = beat; Float(AbsPos(distractPaw) + new Vector2(0, 150), L.Get("ui.kitchen.suck")); YeogiCafe.Audio.SoundManager.Sfx("sfx_suck", 0.5f); }
            }
        }

        int lastLick = -1;
        Vector2 AbsPos(RectTransform p) => new Vector2(p.anchorMin.x * root.rect.width + p.anchoredPosition.x, p.anchoredPosition.y);

        void Float(Vector2 at, string text)
        {
            var lbl = UiFactory.Label("Fx", root, text, 20, TextAnchor.MiddleCenter, UiFactory.Cream);
            lbl.horizontalOverflow = HorizontalWrapMode.Overflow; lbl.raycastTarget = false;
            var o = lbl.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.29f, 0.21f, 0.18f, 0.9f); o.effectDistance = new Vector2(2, -2);
            var rt = (RectTransform)lbl.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.sizeDelta = new Vector2(200, 30);
            rt.anchoredPosition = at;
            fx.Add(new Fx { rt = rt, t = lbl, life = 0.9f });
        }

        void TickFx(float dt)
        {
            for (int i = fx.Count - 1; i >= 0; i--)
            {
                var f = fx[i]; f.life -= dt;
                if (f.rt == null || f.life <= 0f) { if (f.rt != null) Object.Destroy(f.rt.gameObject); fx.RemoveAt(i); continue; }
                f.rt.anchoredPosition += new Vector2(0, 50f * dt);
                var c = f.t.color; c.a = Mathf.Clamp01(f.life / 0.45f); f.t.color = c;
            }
        }

        void AnimatePaws(float dt)
        {
            TickFx(dt);
            float bob = Mathf.Sin(Time.unscaledTime * 2.2f) * 6f;
            pawL.anchoredPosition = pawLHome + new Vector2(0, bob);
            pawR.anchoredPosition = pawRHome + new Vector2(0, -bob);
            TickDistract(dt);
            AnimateDistract();
            if (pawPunch != null && pawPunchT >= 0f)
            {
                pawPunchT += dt;
                const float outT = 0.10f, backT = 0.18f;
                Vector2 home = pawPunch == pawL ? pawLHome : pawRHome;
                float k = pawPunchT < outT ? pawPunchT / outT : 1f - Mathf.Clamp01((pawPunchT - outT) / backT);
                pawPunch.anchoredPosition = Vector2.Lerp(home, pawPunchTarget, k * 0.55f);
                var img = pawPunch.GetComponent<Image>();
                bool left = pawPunch == pawL;
                img.sprite = pawPunchT < outT + 0.06f
                    ? RuntimeSprites.Get(left ? "paw_left_press" : "paw_right_press", left ? CookingDrawing.PawLeftPressed : CookingDrawing.PawRightPressed)
                    : RuntimeSprites.Get(left ? "paw_left" : "paw_right", left ? CookingDrawing.PawLeft : CookingDrawing.PawRight);
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
            if (PawBusy(w.rt.anchorMin.x < 0.5f))
            {
                hint.text = L.Get(distract == Distract.Knead ? "ui.kitchen.busyknead" : distract == Distract.Groom ? "ui.kitchen.busygroom" : "ui.kitchen.busysuckle");
                return;
            }
            Punch(w);
            var r = orders.PressTool(id, out var ticket);
            switch (r)
            {
                case OrderSystem.ToolResult.Started:
                    YeogiCafe.Audio.SoundManager.Sfx(id.Contains("coffee") || id.Contains("kettle") ? "sfx_pour" : "sfx_sizzle", 0.5f);
                    hint.text = L.Get("ui.kitchen.cooking", ("menu", MenuName(ticket)));
                    break;
                case OrderSystem.ToolResult.Served:
                    YeogiCafe.Audio.SoundManager.Sfx("sfx_chime", 0.5f, 0.02f);
                    YeogiCafe.Audio.SoundManager.Sfx("sfx_coin", 0.35f, 0.02f);
                    hint.text = L.Get("ui.kitchen.served", ("name", CatName(ticket)), ("menu", MenuName(ticket)));
                    GameEvents.Raise(GameEvents.Serve, ticket.catId);
                    if (ticket.vague && ticket.IsCorrectDish)
                    { owner.ShowCatBubble(ticket.catId, L.Get("ui.order.mindread")); GameEvents.Raise(GameEvents.MindRead, ticket.catId); }
                    else if (!ticket.IsCorrectDish)
                    { owner.ShowCatBubble(ticket.catId, L.Get("ui.order.wrongdish")); GameEvents.Raise(GameEvents.WrongDish, ticket.catId); }
                    else owner.ShowCatBubble(ticket.catId, L.Get("ui.kitchen.thanks"));
                    break;
                case OrderSystem.ToolResult.Busy:
                    YeogiCafe.Audio.SoundManager.Sfx("sfx_boop", 0.4f);
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
                card.sizeDelta = new Vector2(212, 64);
                UiFactory.Frame(card, UiFactory.FrameStyle.Paper, tk.unresolved ? (Color?)UiFactory.YellowLight : null);
                ticketCards.Add(card.gameObject);

                var catIcon = Icon(card, CatSprite(tk.catId), new Vector2(6, 10), 46);
                var dish = Icon(card, tk.unresolved ? RuntimeSprites.Get("emote_Question", () => EmoteDrawing.Draw(EmoteDrawing.Kind.Question))
                                                    : DishSprite(tk.menu != null ? tk.menu.menuId : ""), new Vector2(52, 14), 38);
                var nameT = UiFactory.Label("Menu", card, tk.unresolved ? L.Get(tk.hintKey) : MenuName(tk), tk.unresolved ? 13 : 15, TextAnchor.MiddleLeft, UiFactory.Ink);
                if (tk.unresolved)
                {
                    // 힌트 주문: 카드를 눌러 메뉴를 고른다
                    var btn = card.gameObject.AddComponent<Button>();
                    var captured = tk;
                    btn.onClick.AddListener(() => OpenPicker(captured));
                }
                UiFactory.SetRect((RectTransform)nameT.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                                  new Vector2(94, -6), new Vector2(110, 42));
                nameT.horizontalOverflow = HorizontalWrapMode.Wrap;
                nameT.resizeTextForBestFit = true; nameT.resizeTextMinSize = 11; nameT.resizeTextMaxSize = nameT.fontSize;
                nameT.verticalOverflow = VerticalWrapMode.Truncate;

                // 상태: 대기(인내심 바) / 조리 중 / 완성
                var bar = UiFactory.Panel("Patience", card, UiFactory.WoodDark);
                UiFactory.SetRect(bar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(8, 6), new Vector2(-16, 6));
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

        // ── 힌트 주문 메뉴 고르기 ──
        GameObject pickerGo;
        void OpenPicker(OrderTicket tk)
        {
            ClosePicker();
            var menus = owner.Cafe != null ? owner.Cafe.GetAvailableMenus() : null;
            if (menus == null || menus.Count == 0) return;
            var box = UiFactory.Panel("Picker", root, UiFactory.PanelBg);
            pickerGo = box.gameObject;
            int cols = Mathf.Min(4, menus.Count), rows = (menus.Count + 3) / 4;
            UiFactory.SetRect(box, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                              new Vector2(0, -140), new Vector2(40 + cols * 112, 84 + rows * 100));
            UiFactory.Frame(box, UiFactory.FrameStyle.Window);
            var title = UiFactory.Label("T", box.transform, L.Get("ui.order.pick", ("name", CatName(tk)), ("hint", L.Get(tk.hintKey))),
                                        17, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(-20, 30));
            for (int i = 0; i < menus.Count; i++)
            {
                var m = menus[i];
                var b = UiFactory.Button("M_" + m.menuId, box.transform, L.Get("menu." + m.menuId), UiFactory.Cream, UiFactory.Ink, out var bl);
                bl.fontSize = 13; bl.alignment = TextAnchor.LowerCenter;
                UiFactory.SetRect((RectTransform)b.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                                  new Vector2(20 + (i % 4) * 112, -44 - (i / 4) * 100), new Vector2(104, 92));
                Icon((RectTransform)b.transform, DishSprite(m.menuId), new Vector2(28, 30), 48);
                var mm = m;
                b.onClick.AddListener(() =>
                {
                    orders.Resolve(tk, mm);
                    hint.text = L.Get("ui.order.picked", ("menu", L.Get("menu." + mm.menuId)));
                    ClosePicker();
                    RefreshRail(true);
                });
            }
            var close = UiFactory.Button("X", box.transform, "×", UiFactory.Pink, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-6, -6), new Vector2(32, 32));
            close.onClick.AddListener(ClosePicker);
        }

        void ClosePicker() { if (pickerGo != null) Object.Destroy(pickerGo); pickerGo = null; }

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
