using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Core;
using YeogiCafe.Data;
using YeogiCafe.Economy;
using YeogiCafe.Loc;
using YeogiCafe.Observation;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 게임 UI 총괄 컨트롤러 — 코드로 Canvas/HUD/상점/정산/도감을 구성하고 매니저에 배선한다.
    // UGUI 기본 위젯(UnityEngine.UI.Text/Button/Image)만 사용(TMP 의존 회피).
    // SceneBuilder가 [GameRoot]에 이 컴포넌트를 붙이고 참조를 주입하면 런타임에 전체 UI가 뜬다.
    public class GameUiController : MonoBehaviour
    {
        [Header("배선 (없으면 씬에서 자동 탐색)")]
        public S1Bootstrap bootstrap;
        public GameFlowController flow;
        public EconomyManager economy;
        public DayManager day;
        public CafeContext cafe;

        // ── HUD 위젯 ──
        Text goldText, phaseText, guestText, narrationText;
        Image timeBarFill;

        // ── 패널 컨트롤러 ──
        ShopPanel shop;
        SettlementPanel settlement;
        ClosingPlayPanel closing;
        TutorialGuide guide;
        bool guideBegun;
        SettingsPanel settings;
        CatalogPanel catalog;
        BubbleLayer bubbles;
        KitchenView kitchen;
        YeogiCafe.Progression.QuestBoard quests;
        GameObject questPanel; Text questList, questBtnLabel;
        FurnitureEditMode editMode;
        Text kitchenBtnLabel;

        float narrationTimer, hudTimer;
        TitleScreen title;
        public void OpenSettings() { settings?.Show(); }

        void Start()
        {
            Resolve();
            BuildUi();
            HookEvents();
            RefreshHud();
        }

        void Resolve()
        {
            if (bootstrap == null) bootstrap = FindObjectOfType<S1Bootstrap>();
            if (bootstrap != null)
            {
                if (flow == null) flow = bootstrap.flow;
                if (economy == null) economy = bootstrap.economy;
                if (day == null) day = bootstrap.day;
                if (cafe == null) cafe = bootstrap.cafe;
            }
            if (flow == null) flow = FindObjectOfType<GameFlowController>();
            if (economy == null) economy = FindObjectOfType<EconomyManager>();
            if (day == null) day = FindObjectOfType<DayManager>();
            if (cafe == null) cafe = FindObjectOfType<CafeContext>();
        }

        Canvas root;

        void BuildUi()
        {
            var canvasGo = new GameObject("[UICanvas]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            root = canvasGo.GetComponent<Canvas>();
            root.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            // 이벤트 시스템(입력) — 없으면 버튼 클릭이 안 먹음.
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                                        typeof(UnityEngine.EventSystems.StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            bubbles = new BubbleLayer(root.transform);
            if (bootstrap != null && bootstrap.interactions != null)
            { bootstrap.interactions.say = ShowCatBubble; bootstrap.interactions.toast = ShowToast; }
            YeogiCafe.Visual.MouseRaid.Toast = ShowToast;
            CustomerSpawner.Toast = ShowToast;
            if (flow != null) flow.OnReview += r =>
            {
                var c = LookupCat(r.catId);
                if (c != null && (r.paws >= 5 || r.paws <= 1))      // 아주 좋거나 아주 나쁜 리뷰만 알림
                    ShowToast(L.Get("ui.review.toast", ("name", L.Get($"cat.{r.catId}.name")), ("paws", YeogiCafe.Food.ReviewBook.Paws(r.paws)), ("text", r.text)));
            };
            PawCursor.Ensure();
            TextFitGuard.Ensure();                       // 글자 넘침 안전망(모든 UI 글자)
            BuildHud(root.transform);

            kitchen = new KitchenView(this, root.transform, bootstrap != null ? bootstrap.orderSystem : null);
            shop = new ShopPanel(this, root.transform);
            settlement = new SettlementPanel(this, root.transform);
            closing = new ClosingPlayPanel(this, root.transform);
            settings = new SettingsPanel(this, root.transform);
            SettingsPanel.ApplySaved(day);
            catalog = new CatalogPanel(this, root.transform);
            editMode = new FurnitureEditMode(this, root.transform);
            guide = new TutorialGuide(this, root.transform);

            quests = new YeogiCafe.Progression.QuestBoard(() => Save,
                () => Save != null ? (IReadOnlyList<CatData>)UnlockService.UnlockedCats(AllCats, Save) : AllCats);
            quests.addGold = g => { if (economy != null) economy.AddGold(g); RefreshHud(); };
            quests.toast = ShowToast;
            quests.OnChanged += RefreshQuests;
            var qp = UiFactory.Panel("QuestPanel", root.transform, new Color(1f, 0.97f, 0.88f, 0.97f));
            UiFactory.SetRect(qp, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(300, -74), new Vector2(480, 170));
            UiFactory.Frame(qp, UiFactory.FrameStyle.Paper);
            questPanel = qp.gameObject;
            var qt = UiFactory.Label("T", qp.transform, L.Get("ui.quest.title"), 18, TextAnchor.UpperLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)qt.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -12), new Vector2(-36, 24));
            // 의뢰 문장이 길어도 칸 안에서 줄바꿈 → 패널 높이를 글 길이에 맞춘다(RefreshQuests)
            questList = UiFactory.Label("L", qp.transform, "", 16, TextAnchor.UpperLeft, UiFactory.Ink);
            questList.lineSpacing = 1.15f;
            UiFactory.SetRect((RectTransform)questList.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -42), new Vector2(-36, 100));
            questPanel.SetActive(false);

            // 시작 화면(맨 위). 세이브가 있으면 '이어하기 · N일차'
            var sm = FindObjectOfType<YeogiCafe.Save.SaveManager>();
            title = new TitleScreen(this, root.transform, sm != null && sm.HasSave(), day != null ? day.CurrentDay : 1);
            // 시작 화면의 '새 게임'으로 다시 불러온 직후: 오프닝부터
            if (OpeningPanel.Pending) { OpeningPanel.Pending = false; PlayOpening(); }
        }

        void BuildHud(Transform parent)
        {
            // 상단 바 하나에 정보+버튼을 모아 카페 화면(바닥 앞줄)을 가리지 않게 한다.
            var bar = UiFactory.Panel("HUD_Top", parent, UiFactory.CreamDim);
            UiFactory.SetRect(bar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                              Vector2.zero, new Vector2(0, 64));
            UiFactory.Frame(bar, UiFactory.FrameStyle.Plank);

            goldText = UiFactory.Label("Gold", bar.transform, "0G", 26, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)goldText.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                              new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(140, 56));
            goldText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // 손님 수: 판자 나뭇결 선 위에 글자가 겹치지 않게 작은 종이 배지 위에
            var guestPill = UiFactory.Panel("GuestPill", bar.transform, UiFactory.Cream);
            UiFactory.Frame(guestPill, UiFactory.FrameStyle.Card, UiFactory.Cream);
            UiFactory.SetRect(guestPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(160, 0), new Vector2(110, 40));
            guestText = UiFactory.Label("Guests", guestPill.transform, "", 19, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.Anchor((RectTransform)guestText.transform, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0));

            phaseText = UiFactory.Label("Phase", bar.transform, "", 24, TextAnchor.MiddleCenter, UiFactory.Ink);
            phaseText.horizontalOverflow = HorizontalWrapMode.Overflow;   // 한 줄 — 칸보다 길면 TextFitGuard가 줄인다
            UiFactory.SetRect((RectTransform)phaseText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(0.5f, 0.5f), new Vector2(4, 8), new Vector2(330, 36));      // 왼쪽 묶음(~452)과 오른쪽 버튼(~834) 사이

            // 하루 진행 바(시간대 텍스트 바로 아래)
            var barBg = UiFactory.Panel("TimeBarBg", bar.transform, UiFactory.Wood);
            UiFactory.SetRect(barBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -18), new Vector2(240, 10));
            timeBarFill = UiFactory.Panel("TimeBarFill", barBg.transform, UiFactory.Green).GetComponent<Image>();
            timeBarFill.type = Image.Type.Filled;
            timeBarFill.fillMethod = Image.FillMethod.Horizontal;
            timeBarFill.fillAmount = 0f;
            UiFactory.Anchor((RectTransform)timeBarFill.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // Filled 타입은 sprite가 있어야 채움이 보인다 → 흰 1px 스프라이트 사용
            timeBarFill.sprite = UiFactory.WhiteSprite;

            var shopBtn = UiFactory.Button("ShopBtn", bar.transform, L.Get("ui.shop.buy"),
                                           UiFactory.Wood, UiFactory.Cream, out var shopLbl);
            shopLbl.horizontalOverflow = HorizontalWrapMode.Overflow;     // 한 줄 유지 — 길면 TextFitGuard가 줄인다(언어별 길이 차)
            UiFactory.SetRect((RectTransform)shopBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(104, 48));
            shopBtn.onClick.AddListener(() => { catalog.Hide(); shop.Toggle(); });

            var catBtn = UiFactory.Button("CatalogBtn", bar.transform, L.Get("ui.pause.catalog"),
                                          UiFactory.Wood, UiFactory.Cream, out var catLbl);
            catLbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.SetRect((RectTransform)catBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(-122, 0), new Vector2(104, 48));
            catBtn.onClick.AddListener(() => { shop.Hide(); catalog.Toggle(); });

            // 주방(1인칭 조리) ↔ 홀 전환 — 가장 자주 누르는 버튼이라 가운데 왼쪽에 크게
            var kitBtn = UiFactory.Button("KitchenBtn", bar.transform, L.Get("ui.kitchen.open"),
                                          UiFactory.Green, UiFactory.Ink, out kitchenBtnLabel);
            kitchenBtnLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.SetRect((RectTransform)kitBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(-232, 0), new Vector2(104, 48));
            kitBtn.onClick.AddListener(ToggleKitchen);

            // 오늘의 의뢰(왼쪽, 손님 수 옆)
            var qBtn = UiFactory.Button("QuestBtn", bar.transform, L.Get("ui.quest.btn"), UiFactory.YellowLight, UiFactory.Ink, out questBtnLabel);
            questBtnLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.SetRect((RectTransform)qBtn.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                              new Vector2(0, 0.5f), new Vector2(278, 0), new Vector2(112, 44));
            qBtn.onClick.AddListener(ToggleQuests);

            // 가구 배치(끌어서 옮기기)
            var editBtn = UiFactory.Button("EditBtn", bar.transform, L.Get("ui.edit.open"),
                                           UiFactory.Wood, UiFactory.Cream, out var editLbl);
            editLbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.SetRect((RectTransform)editBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(-342, 0), new Vector2(104, 48));
            editBtn.onClick.AddListener(() => { shop.Hide(); catalog.Hide(); CloseKitchen(); editMode.Toggle(); });

            // 설정 — 의뢰 버튼 오른쪽(가운데 시간 표시와 겹치지 않게 작게). ESC로도 열림
            var optBtn = UiFactory.Button("OptBtn", bar.transform, L.Get("opt.btn"), UiFactory.CreamDim, UiFactory.WoodDark, out var optLbl);
            optLbl.fontSize = 18; optLbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.SetRect((RectTransform)optBtn.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                              new Vector2(0, 0.5f), new Vector2(396, 0), new Vector2(64, 44));
            optBtn.onClick.AddListener(() => settings.Toggle());

            // (구) 하단 서술 텍스트는 고양이 머리 위 말풍선(BubbleLayer)으로 대체. 필드는 호환용으로 유지.
            narrationText = null;
        }

        void HookEvents()
        {
            if (economy != null) economy.OnGoldChanged += _ => RefreshHud();
            if (economy != null) economy.OnGoldDelta += ShowGoldDelta;
            if (day != null)
            {
                day.OnPhaseChanged += _ => RefreshHud();
            }
            if (flow != null)
            {
                flow.OnSettlementReady += () =>
                {
                    shop.Hide(); catalog.Hide(); CloseKitchen(); editMode.Exit(); questPanel.SetActive(false);
                    // 마감 직전: 마지막 손님과 놀아주기 → 끝나면 밤 정산(도감)
                    var guest = flow.ClosingGuestId != null ? LookupCat(flow.ClosingGuestId) : null;
                    if (guest != null && Save != null) { closing.Show(guest, Save.GetOrCreateCat(guest.catId), () => { settlement.Show(); guide?.OnSettlement(); RefreshHud(); }); guide?.OnClosing(); }
                    else { settlement.Show(); guide?.OnSettlement(); }
                };
                TryHookObservation();
            }
        }

        // GameFlowController.Start(세이브 로드·관찰 매니저 생성)가 이 컴포넌트보다 늦게 돌 수 있어
        // 관찰 이벤트는 준비될 때까지 매 프레임 재시도해서 연결한다(말풍선이 안 뜨던 원인).
        bool ordersHooked;
        void TryHookOrders()
        {
            var os = bootstrap != null ? bootstrap.orderSystem : null;
            if (ordersHooked || os == null) return;
            os.OnTicketPlaced += t => { if (!YeogiCafe.Food.DeliveryCat.IsDelivery(t.catId)) ShowCatBubble(t.catId, t.vague
                ? L.Get("ui.order.askhint", ("hint", L.Get(t.hintKey)))
                : L.Get("ui.order.ask", ("menu", t.menu != null ? L.Get($"menu.{t.menu.menuId}") : ""))); };
            os.OnTicketCancelled += t => ShowCatBubble(t.catId, L.Get("ui.order.gaveup"));
            os.OnTicketPlaced += _ => guide?.OnOrderPlaced();
            os.OnTicketServed += _ => guide?.OnServed();
            os.OnTicketReady += _ => YeogiCafe.Audio.SoundManager.Sfx("sfx_bell", 0.45f, 0.02f);
            ordersHooked = true;
        }

        public void ShowCatBubble(string catId, string text) => bubbles?.Show(catId, text);

        // 화면 위쪽 알림(선물·의뢰 완료 등) — 3.5초 뒤 사라짐
        Text toastText; CanvasGroup toastCg; float toastT;
        readonly Queue<string> toastQueue = new();
        // 창(상점·도감·설정·마감 놀아주기·정산)이 떠 있으면 알림은 기다렸다가 닫힌 뒤에 — 창 제목·탭을 가리지 않게
        bool ModalOpen => (shop != null && shop.IsOpen) || (catalog != null && catalog.IsOpen) || (settings != null && settings.IsOpen)
                          || (closing != null && closing.IsOpen) || (settlement != null && settlement.IsOpen);

        public void ShowToast(string text)
        {
            if ((toastCg != null && toastT > 0.6f) || ModalOpen) { toastQueue.Enqueue(text); return; }   // 앞 알림이 끝나면 / 창이 닫히면 차례로
            if (toastText == null)
            {
                var bg = UiFactory.Panel("Toast", root.transform, new Color(1f, 0.98f, 0.93f, 0.97f));
                UiFactory.SetRect(bg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -76), new Vector2(720, 64));
                UiFactory.Frame(bg, UiFactory.FrameStyle.Paper);
                toastCg = bg.gameObject.AddComponent<CanvasGroup>(); toastCg.blocksRaycasts = false;
                toastText = UiFactory.Label("T", bg.transform, "", 18, TextAnchor.MiddleCenter, UiFactory.Ink);
                UiFactory.Anchor((RectTransform)toastText.transform, Vector2.zero, Vector2.one, new Vector2(20, 8), new Vector2(-20, -8));
            }
            toastText.text = text; toastT = 3.5f; toastCg.alpha = 1f;
            // 주방에서는 위쪽 주문표를 가리지 않게 화면 아래 양손 사이로, 홀에서는 상단 바 아래
            var tbg = (RectTransform)toastText.transform.parent;
            bool inKitchen = kitchen != null && kitchen.IsOpen;
            tbg.anchorMin = tbg.anchorMax = tbg.pivot = new Vector2(0.5f, inKitchen ? 0f : 1f);
            tbg.anchoredPosition = new Vector2(0, inKitchen ? 40 : -76);
            YeogiCafe.Audio.SoundManager.Sfx("sfx_boop", 0.35f, 0.04f, "toast", 0.4f);
            var trt = (RectTransform)toastText.transform.parent;          // 긴 알림은 두 줄로 → 높이 맞춤
            trt.sizeDelta = new Vector2(inKitchen ? 600 : 720, Mathf.Max(64f, toastText.preferredHeight + 20f));   // 양손(좌우 끝 ~250)과 안 겹치는 폭
            toastText.transform.parent.SetAsLastSibling();
        }

        void ToggleKitchen()
        {
            shop.Hide(); catalog.Hide(); editMode.Exit();
            kitchen.Toggle();
            if (kitchenBtnLabel != null) kitchenBtnLabel.text = L.Get(kitchen.IsOpen ? "ui.kitchen.close" : "ui.kitchen.open");
        }

        void CloseKitchen()
        {
            if (kitchen == null || !kitchen.IsOpen) return;
            kitchen.Close();
            if (kitchenBtnLabel != null) kitchenBtnLabel.text = L.Get("ui.kitchen.open");
        }

        bool obsHooked;
        void TryHookObservation()
        {
            if (obsHooked || flow == null || flow.Observation == null) return;
            flow.Observation.OnCatNarration += (catId, text) => bubbles.Show(catId, text);
            obsHooked = true;
        }

        // ── 골드 증감 표시: 보유 골드 오른쪽에 +60G(초록) / -350G(빨강)이 떠올랐다 사라진다 ──
        class GoldPop { public RectTransform rt; public Text t; public float age; public int sum; }
        readonly List<GoldPop> goldPops = new();
        void ShowGoldDelta(int d)
        {
            if (goldText == null || d == 0) return;
            // 같은 방향 변화가 연달아 오면 한 숫자로 합친다(재료비·팁 등 잔돈이 줄줄이 뜨지 않게)
            var last = goldPops.Count > 0 ? goldPops[goldPops.Count - 1] : null;
            if (last != null && last.age < 0.35f && Mathf.Sign(last.sum) == Mathf.Sign(d)) { last.sum += d; last.age = 0f; SetPopText(last); return; }
            var t = UiFactory.Label("GoldPop", goldText.canvas.rootCanvas.transform, "", 24, TextAnchor.MiddleLeft, Color.white);   // 상단 바는 밖으로 나간 글자를 잘라서 캔버스에 직접
            t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false;
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.2f, 0.1f, 0.08f, 0.8f); o.effectDistance = new Vector2(1.5f, -1.5f);
            var rt = (RectTransform)t.transform;
            UiFactory.SetRect(rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(140, 30));
            var p = new GoldPop { rt = rt, t = t, sum = d };
            SetPopText(p);
            goldPops.Add(p);
        }
        static void SetPopText(GoldPop p)
        {
            p.t.text = (p.sum > 0 ? "+" : "-") + Mathf.Abs(p.sum) + "G";
            p.t.color = p.sum > 0 ? new Color(0.35f, 0.78f, 0.35f) : new Color(0.92f, 0.32f, 0.32f);
        }
        void TickGoldPops()
        {
            if (goldText == null) return;
            var grt = goldText.rectTransform;
            Vector3 basePos = grt.TransformPoint(new Vector3(grt.rect.xMin + 2f, grt.rect.center.y, 0f));   // 골드 글자 왼쪽 가운데
            float k = goldText.canvas.rootCanvas.scaleFactor;
            for (int i = goldPops.Count - 1; i >= 0; i--)
            {
                var p = goldPops[i];
                p.age += Time.unscaledDeltaTime;
                if (p.rt == null || p.age > 1.6f) { if (p.rt != null) Destroy(p.rt.gameObject); goldPops.RemoveAt(i); continue; }
                int stack = goldPops.Count - 1 - i;                       // 새 것일수록 아래(골드 옆), 이전 것은 위로 밀림
                p.rt.position = basePos + new Vector3(0f, -(46f + p.age * 10f + stack * 26f) * k, 0f);   // 골드 바로 아래로 살짝 내려가며 사라짐
                p.rt.SetAsLastSibling();
                var c = p.t.color; c.a = p.age < 1.0f ? 1f : Mathf.Clamp01(1f - (p.age - 1.0f) / 0.6f); p.t.color = c;
            }
        }

        void Update()
        {
            TickGoldPops();
            TryHookObservation();
            if (Save != null) Weather.EnsureToday(Save);
            if (quests != null && Save != null && Save.questDay != Save.currentDay && flow != null) { quests.EnsureToday(); RefreshQuests(); }
            TryHookOrders();
            kitchen?.Tick(Time.unscaledDeltaTime);
            closing?.Tick(Time.unscaledDeltaTime);
            settings?.Tick();
            title?.Tick(Time.unscaledDeltaTime);
            if (Input.GetKeyDown(KeyCode.Escape) && opening == null && settings != null && (closing == null || !closing.IsOpen)) settings.Toggle();   // ESC = 설정
            // 단축키: C 도감 · B 구매 · K 주방 (영업 중 화면에서만)
            bool hotkeysOk = (title == null || !title.IsOpen) && (closing == null || !closing.IsOpen) && (settlement == null || !settlement.IsOpen)
                             && (settings == null || !settings.IsOpen) && !EndingPanel.IsOpen;
            if (hotkeysOk && Input.GetKeyDown(KeyCode.C)) { shop.Hide(); catalog.Toggle(); }
            if (hotkeysOk && Input.GetKeyDown(KeyCode.B)) { catalog.Hide(); shop.Toggle(); }
            if (hotkeysOk && Input.GetKeyDown(KeyCode.K)) { shop.Hide(); catalog.Hide(); ToggleKitchen(); }
            if (opening != null) { opening.Tick(Time.unscaledDeltaTime); return; }   // 오프닝 동안은 다른 입력·안내 멈춤
            editMode?.Tick(Time.unscaledDeltaTime);
            catalog?.Tick();
            if (guide != null)
            {
                if (!guideBegun && Save != null && (title == null || !title.IsOpen)) { guideBegun = true; guide.Begin(); }
                guide.Tick(Time.unscaledDeltaTime);
                guide.SetCompact(settlement != null && settlement.IsOpen);
                guide.BringToFront();
            }
            if (day != null && timeBarFill != null && bootstrap != null && bootstrap.balance != null)
                timeBarFill.fillAmount = HudFormatter.TimeProgress(day.DayTimer, bootstrap.balance.dayLength);

            bubbles?.Tick(Time.unscaledDeltaTime);
            if (toastCg != null && toastT > 0f) { toastT -= Time.unscaledDeltaTime; toastCg.alpha = Mathf.Clamp01(toastT / 0.5f); }
            if (toastCg != null && toastT <= 0.6f && toastQueue.Count > 0 && !ModalOpen) { toastT = 0f; ShowToast(toastQueue.Dequeue()); }
            else if (toastCg == null && toastQueue.Count > 0 && !ModalOpen) ShowToast(toastQueue.Dequeue());
            hudTimer -= Time.unscaledDeltaTime;
            if (hudTimer <= 0f) { hudTimer = 0.5f; RefreshHud(); }   // 손님 수 등 이벤트 없는 값 주기 갱신

            if (narrationTimer > 0f)
            {
                narrationTimer -= Time.unscaledDeltaTime;
                if (narrationTimer <= 0f && narrationText != null) narrationText.text = "";
            }
        }

        void ShowNarration(string text)
        {
            if (narrationText == null) return;
            narrationText.text = text;
            narrationTimer = 4f;
        }

        public void RefreshHud()
        {
            if (goldText != null && economy != null) goldText.text = HudFormatter.Gold(economy.Gold);
            if (phaseText != null && day != null)
                phaseText.text = L.Get("ui.cafe.day", ("n", day.CurrentDay.ToString())) + "  " + HudFormatter.Phase(day.Phase)
                                 + "  " + L.Get("weather." + (int)Weather.Current);
            if (guestText != null && cafe != null)
            {
                int seated = cafe.SeatCount - cafe.FreeSeatCount;
                guestText.text = HudFormatter.Guests(seated);
            }
        }

        // 정산 화면의 [다음 날] → 영업 재개. (재방문 큐 편성은 flow가 담당하지만 S1은 단순 재시작)
        void ToggleQuests()
        {
            quests.EnsureToday();
            questPanel.SetActive(!questPanel.activeSelf);
            if (questPanel.activeSelf) { questPanel.transform.SetAsLastSibling(); RefreshQuests(); }
        }

        void RefreshQuests()
        {
            var s = Save; if (s == null || questList == null) return;
            var sb = new System.Text.StringBuilder();
            int done = 0;
            foreach (var q in s.quests)
            {
                if (q.done) done++;
                sb.Append(q.done ? "<color=#6F9A55>" + L.Get("quest.donetag") + "</color> " : $"[{q.progress}/{q.goal}] ").Append(L.Get(q.id))
                  .Append($"  <color=#B8864E>+{q.reward}G</color>\n");
            }
            questList.text = sb.ToString().TrimEnd('\n');
            float h = questList.preferredHeight;
            var lrt = (RectTransform)questList.transform; lrt.sizeDelta = new Vector2(lrt.sizeDelta.x, h + 4);
            var prt = (RectTransform)questPanel.transform; prt.sizeDelta = new Vector2(prt.sizeDelta.x, 42 + h + 22);
            if (questBtnLabel != null) questBtnLabel.text = $"{L.Get("ui.quest.btn")} {done}/{s.quests.Count}";
        }

        public DayManager Day => day;
        public CafeContext Cafe => cafe;
        public CatManager Cats => bootstrap != null ? bootstrap.cats : null;
        public YeogiCafe.Cafe.FurnitureManager Furniture => bootstrap != null ? bootstrap.furniture : null;
        public IReadOnlyList<FurnitureData> AllFurniture => bootstrap != null && bootstrap.allFurniture != null ? (IReadOnlyList<FurnitureData>)bootstrap.allFurniture : new List<FurnitureData>();

        OpeningPanel opening;
        public bool OpeningOpen => opening != null;
        public void PlayOpening()
        {
            if (opening != null) return;
            settings?.Hide();
            opening = new OpeningPanel(root.transform, () => { opening = null; RefreshHud(); });
        }

        public void DebugTutorial() => guide?.DebugReplay();
        public void DebugEnding() => new EndingPanel(root.transform, Save, AllCats, null, preview: true);

        // ── 현지화 QA: 현재 언어로 화면을 하나씩 열어 스크린샷 + 글자 넘침 보고서를 Build/uicheck 에 남긴다 ──
        public bool UiTourRunning { get; private set; }
        public void DebugUiTour() { if (!UiTourRunning) StartCoroutine(UiTour()); }

        System.Collections.IEnumerator UiTour()
        {
            UiTourRunning = true;
            string lang = L.Current.ToString().ToLowerInvariant();
            string dir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Build", "uicheck");
            System.IO.Directory.CreateDirectory(dir);
            var report = new System.Text.StringBuilder();
            report.Append($"# UI tour  lang={lang}  screen={Screen.width}x{Screen.height}  {System.DateTime.Now}\n");
            int n = 0;
            float prevScale = Time.timeScale;

            System.Collections.IEnumerator Shot(string name)
            {
                yield return new WaitForSecondsRealtime(0.7f);
                Canvas.ForceUpdateCanvases();
                yield return new WaitForEndOfFrame();
                string file = System.IO.Path.Combine(dir, $"{lang}_{n:00}_{name}.png");
                var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
                System.IO.File.WriteAllBytes(file, tex.EncodeToPNG()); Destroy(tex);
                string rep = TextFitGuard.Report();
                report.Append($"\n## {n:00} {name}\n").Append(string.IsNullOrEmpty(rep) ? "(ok)\n" : rep);
                n++;
                yield return new WaitForSecondsRealtime(0.3f);
            }
            void CloseAll()
            {
                shop.Hide(); catalog.Hide(); settings.Hide(); CloseKitchen(); editMode.Exit();
                settlement.DebugCloseGuess(); settlement.DebugCloseStory(); settlement.Hide();
                closing.DebugClose(); if (questPanel.activeSelf) questPanel.SetActive(false);
                guide.DebugStep(-1);
            }

            CloseAll();
            yield return Shot("hud");
            ToggleQuests(); yield return Shot("quests"); questPanel.SetActive(false);
            for (int m = 0; m < 4; m++) { shop.DebugTab(m); yield return Shot("shop" + m); }
            shop.Hide();
            catalog.DebugTab(false, false); yield return Shot("catalog_list");
            catalog.DebugTab(true, false); yield return Shot("catalog_rel");
            catalog.DebugTab(false, true); yield return Shot("catalog_review");
            catalog.Hide();
            settings.Show(); yield return Shot("settings"); settings.Hide();
            ToggleKitchen(); yield return Shot("kitchen");
            kitchen.DebugPicker(null); yield return Shot("kitchen_picker"); kitchen.DebugClosePicker();
            CloseKitchen();
            var cats = AllCats; CatData c0 = null; foreach (var c in cats) if (c != null) { c0 = c; break; }
            settlement.Show(); settlement.DebugCloseStory(); yield return Shot("settle");
            if (c0 != null)
            {
                settlement.DebugGuess(c0, PrefAxis.Food); yield return Shot("settle_guess"); settlement.DebugCloseGuess();
                settlement.DebugStory(new StoryDTO { catId = c0.catId, kind = 2 }); yield return Shot("story_lv2"); settlement.DebugCloseStory();
                settlement.DebugStory(new StoryDTO { catId = c0.catId, kind = 9 }); yield return Shot("story_grad"); settlement.DebugCloseStory();
            }
            settlement.DebugReport(); yield return Shot("settle_report"); settlement.DebugCloseStory();
            settlement.Hide();
            if (c0 != null && Save != null)
            {
                closing.Show(c0, Save.GetOrCreateCat(c0.catId), null); yield return Shot("closing"); closing.DebugClose();
            }
            var end = new EndingPanel(root.transform, Save, AllCats, null, preview: true); yield return Shot("ending"); end.DebugClose();
            var op = opening = new OpeningPanel(root.transform, () => { opening = null; });
            for (int p = 0; p < 5; p++) { op.DebugPage(p); yield return Shot("opening" + (p + 1)); }
            op.DebugFinish();
            Time.timeScale = prevScale;
            for (int st = 0; st < 5; st++) { guide.DebugStep(st); yield return Shot("tutorial" + st); }
            guide.DebugStep(-1);
            settlement.Show(); settlement.DebugCloseStory(); guide.SetCompact(true); guide.DebugStep(4); yield return Shot("tutorial_settle_compact");
            guide.DebugStep(-1); guide.SetCompact(false); settlement.Hide();
            editMode.Enter(); yield return Shot("editmode"); editMode.Exit();
            CloseAll();

            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, $"textfit_{lang}.txt"), report.ToString());
            ShowToast($"UI check done: {n} shots → Build/uicheck");
            UiTourRunning = false;
        }

        public void StartNextDay()
        {
            // 최종 목표 달성(모두의 취향 기록) → 엔딩 편지 한 번, 닫으면 자유 영업
            if (EndingPanel.Ready(Save, AllCats))
            {
                new EndingPanel(root.transform, Save, AllCats, StartNextDay);
                return;
            }
            settlement.Hide();
            guide?.OnNextDay();
            if (flow != null) flow.BeginNextDay();       // 일차 증가·오토세이브·영업 시작
            else if (day != null) day.StartDay();
            RefreshHud();
        }

        // ── 패널이 접근하는 헬퍼 ──
        public SaveGame Save => flow != null ? flow.Save : null;
        public ObservationManager Obs => flow != null ? flow.Observation : null;
        public EconomyManager Economy => economy;
        public int LostToMice => flow != null ? flow.LostToMice : 0;

        public bool TryBuy(FurnitureData data)
        {
            bool ok = bootstrap != null && bootstrap.BuyFurniture(data);
            LastBuyFailedForSpace = !ok && bootstrap != null && bootstrap.furniture != null &&
                                    bootstrap.furniture.LastFail == YeogiCafe.Cafe.FurnitureManager.FailReason.NoSpace;
            if (ok) RefreshHud();
            return ok;
        }
        public bool LastBuyFailedForSpace { get; private set; }

        public GameFlowController Flow => flow;
        public FurnitureData LookupFurniture(string id)
        {
            if (bootstrap == null) return null;
            foreach (var f in bootstrap.allFurniture) if (f != null && f.furnitureId == id) return f;
            return null;
        }

        // ── 레시피·음식 강화·가구 강화 ──
        public bool TryBuyTool(string toolId, out string msg)
        {
            msg = null; var s = Save; if (s == null || economy == null) return false;
            var t = System.Array.Find(YeogiCafe.Food.RecipeBook.Tools, x => x.id == toolId);
            if (t.id == null || YeogiCafe.Food.RecipeBook.ToolOwned(s, toolId)) return false;
            if (!YeogiCafe.Food.RecipeBook.ToolUnlocked(s, t)) { msg = L.Get("ui.recipe.locked", ("n", t.cafeLevel.ToString())); return false; }
            if (!economy.TrySpend(t.price)) { msg = L.Get("ui.shop.insufficient"); return false; }
            s.ownedTools.Add(toolId);
            flow.SyncMenus();
            msg = L.Get("ui.tool.bought", ("tool", L.Get(YeogiCafe.Order.CookingStations.NameKey(toolId))));
            YeogiCafe.Audio.SoundManager.Sfx("sfx_chime", 0.5f, 0.02f);
            RefreshHud();
            return true;
        }

        public bool TryUpgradeMenu(MenuData m, out string msg)
        {
            msg = null; var s = Save; if (s == null || m == null || economy == null) return false;
            var st = s.GetMenuStat(m.menuId);
            if (st.level >= YeogiCafe.Food.RecipeBook.MaxLevel) return false;
            int need = YeogiCafe.Food.RecipeBook.NeedSold(st.level), cost = YeogiCafe.Food.RecipeBook.UpgradeCost(m, st.level);
            if (st.sold < need) { msg = L.Get("ui.recipe.needsold", ("n", (need - st.sold).ToString())); return false; }
            if (!economy.TrySpend(cost)) { msg = L.Get("ui.shop.insufficient"); return false; }
            st.level++;
            msg = L.Get("ui.recipe.upgraded", ("menu", L.Get("menu." + m.menuId)), ("n", (st.level + 1).ToString()));
            YeogiCafe.Audio.SoundManager.Sfx("sfx_chime", 0.5f, 0.02f);
            RefreshHud();
            return true;
        }

        public bool TryUpgradeFurniture(FurnitureData d, out string msg)
        {
            msg = null; var s = Save; if (s == null || d == null || economy == null) return false;
            var st = s.GetFurnStat(d.furnitureId);
            if (st.stars >= YeogiCafe.Food.FurnitureStars.Max) return false;
            int need = YeogiCafe.Food.FurnitureStars.NeedUses(st.stars), cost = YeogiCafe.Food.FurnitureStars.Cost(d, st.stars);
            if (st.uses < need) { msg = L.Get("ui.furnup.needuses", ("n", (need - st.uses).ToString())); return false; }
            if (!economy.TrySpend(cost)) { msg = L.Get("ui.shop.insufficient"); return false; }
            st.stars++;
            msg = L.Get("ui.furnup.done", ("name", ShopPanel.FurnitureName(d)), ("s", YeogiCafe.Food.FurnitureStars.StarText(st.stars)));
            YeogiCafe.Audio.SoundManager.Sfx("sfx_chime", 0.5f, 0.02f);
            RefreshHud();
            return true;
        }

        // 정산 화면 [카페 넓히기] — 같은 방 확장(앞줄·가장자리 자리 개방). 실패 시 이유 문구 반환.
        public bool TryExpandCafe(out string message)
        {
            message = null;
            var cm = bootstrap != null ? bootstrap.cafeManager : null;
            if (cm == null || cm.save == null) { message = ""; return false; }
            if (!cm.CanUpgrade(out var hint)) { message = hint; return false; }
            bool ok = cm.TryUpgrade();
            message = ok ? L.Get("ui.settle.expanded") : hint;
            if (ok) RefreshHud();
            return ok;
        }
        public int CafeLevel => bootstrap != null && bootstrap.cafeManager != null && bootstrap.cafeManager.save != null
                                ? bootstrap.cafeManager.save.cafeLevel : 1;

        public IReadOnlyList<FurnitureData> ShopItems =>
            bootstrap != null ? bootstrap.shopFurniture : new List<FurnitureData>();

        // catId → CatData 조회(도감/정산용)
        public CatData LookupCat(string catId)
        {
            if (bootstrap == null) return null;
            foreach (var c in bootstrap.allCats) if (c != null && c.catId == catId) return c;
            return null;
        }

        public IReadOnlyList<CatData> AllCats =>
            bootstrap != null ? bootstrap.allCats : new List<CatData>();
    }
}
