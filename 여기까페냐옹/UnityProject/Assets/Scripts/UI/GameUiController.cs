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
        CatalogPanel catalog;
        BubbleLayer bubbles;
        KitchenView kitchen;
        Text kitchenBtnLabel;

        float narrationTimer, hudTimer;

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
            BuildHud(root.transform);

            kitchen = new KitchenView(this, root.transform, bootstrap != null ? bootstrap.orderSystem : null);
            shop = new ShopPanel(this, root.transform);
            settlement = new SettlementPanel(this, root.transform);
            catalog = new CatalogPanel(this, root.transform);
        }

        void BuildHud(Transform parent)
        {
            // 상단 바 하나에 정보+버튼을 모아 카페 화면(바닥 앞줄)을 가리지 않게 한다.
            var bar = UiFactory.Panel("HUD_Top", parent, UiFactory.CreamDim);
            UiFactory.SetRect(bar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                              Vector2.zero, new Vector2(0, 64));

            goldText = UiFactory.Label("Gold", bar.transform, "0G", 26, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)goldText.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                              new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(170, 56));

            guestText = UiFactory.Label("Guests", bar.transform, "", 20, TextAnchor.MiddleLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)guestText.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                              new Vector2(0, 0.5f), new Vector2(190, 0), new Vector2(160, 56));

            phaseText = UiFactory.Label("Phase", bar.transform, "", 24, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)phaseText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(260, 36));

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
                                           UiFactory.Wood, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)shopBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(120, 48));
            shopBtn.onClick.AddListener(() => { catalog.Hide(); shop.Toggle(); });

            var catBtn = UiFactory.Button("CatalogBtn", bar.transform, L.Get("ui.pause.catalog"),
                                          UiFactory.Wood, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)catBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(-144, 0), new Vector2(120, 48));
            catBtn.onClick.AddListener(() => { shop.Hide(); catalog.Toggle(); });

            // 주방(1인칭 조리) ↔ 홀 전환 — 가장 자주 누르는 버튼이라 가운데 왼쪽에 크게
            var kitBtn = UiFactory.Button("KitchenBtn", bar.transform, L.Get("ui.kitchen.open"),
                                          UiFactory.Green, UiFactory.Ink, out kitchenBtnLabel);
            UiFactory.SetRect((RectTransform)kitBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(-274, 0), new Vector2(120, 48));
            kitBtn.onClick.AddListener(ToggleKitchen);

            // (구) 하단 서술 텍스트는 고양이 머리 위 말풍선(BubbleLayer)으로 대체. 필드는 호환용으로 유지.
            narrationText = null;
        }

        void HookEvents()
        {
            if (economy != null) economy.OnGoldChanged += _ => RefreshHud();
            if (day != null)
            {
                day.OnPhaseChanged += _ => RefreshHud();
            }
            if (flow != null)
            {
                flow.OnSettlementReady += () => { shop.Hide(); catalog.Hide(); CloseKitchen(); settlement.Show(); };
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
            os.OnTicketPlaced += t => ShowCatBubble(t.catId,
                L.Get("ui.order.ask", ("menu", t.menu != null ? L.Get($"menu.{t.menu.menuId}") : "")));
            os.OnTicketCancelled += t => ShowCatBubble(t.catId, L.Get("ui.order.gaveup"));
            ordersHooked = true;
        }

        public void ShowCatBubble(string catId, string text) => bubbles?.Show(catId, text);

        void ToggleKitchen()
        {
            shop.Hide(); catalog.Hide();
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

        void Update()
        {
            TryHookObservation();
            TryHookOrders();
            kitchen?.Tick(Time.unscaledDeltaTime);
            if (day != null && timeBarFill != null && bootstrap != null && bootstrap.balance != null)
                timeBarFill.fillAmount = HudFormatter.TimeProgress(day.DayTimer, bootstrap.balance.dayLength);

            bubbles?.Tick(Time.unscaledDeltaTime);
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
                phaseText.text = L.Get("ui.cafe.day", ("n", day.CurrentDay.ToString())) + "  " + HudFormatter.Phase(day.Phase);
            if (guestText != null && cafe != null)
            {
                int seated = cafe.SeatCount - cafe.FreeSeatCount;
                guestText.text = HudFormatter.Guests(seated);
            }
        }

        // 정산 화면의 [다음 날] → 영업 재개. (재방문 큐 편성은 flow가 담당하지만 S1은 단순 재시작)
        public void StartNextDay()
        {
            settlement.Hide();
            if (flow != null) flow.BeginNextDay();       // 일차 증가·오토세이브·영업 시작
            else if (day != null) day.StartDay();
            RefreshHud();
        }

        // ── 패널이 접근하는 헬퍼 ──
        public SaveGame Save => flow != null ? flow.Save : null;
        public ObservationManager Obs => flow != null ? flow.Observation : null;
        public EconomyManager Economy => economy;

        public bool TryBuy(FurnitureData data)
        {
            bool ok = bootstrap != null && bootstrap.BuyFurniture(data);
            LastBuyFailedForSpace = !ok && bootstrap != null && bootstrap.furniture != null &&
                                    bootstrap.furniture.LastFail == YeogiCafe.Cafe.FurnitureManager.FailReason.NoSpace;
            if (ok) RefreshHud();
            return ok;
        }
        public bool LastBuyFailedForSpace { get; private set; }

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
