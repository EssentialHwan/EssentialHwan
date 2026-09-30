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

        float narrationTimer;

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
                flow ??= bootstrap.flow;
                economy ??= bootstrap.economy;
                day ??= bootstrap.day;
                cafe ??= bootstrap.cafe;
            }
            flow ??= FindObjectOfType<GameFlowController>();
            economy ??= FindObjectOfType<EconomyManager>();
            day ??= FindObjectOfType<DayManager>();
            cafe ??= FindObjectOfType<CafeContext>();
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

            BuildHud(root.transform);

            shop = new ShopPanel(this, root.transform);
            settlement = new SettlementPanel(this, root.transform);
            catalog = new CatalogPanel(this, root.transform);
        }

        void BuildHud(Transform parent)
        {
            // 상단 바
            var bar = UiFactory.Panel("HUD_Top", parent, UiFactory.CreamDim);
            UiFactory.SetRect(bar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                              new Vector2(0, -36), new Vector2(0, 72));

            goldText = UiFactory.Label("Gold", bar.transform, "💰 0G", 26, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)goldText.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                              new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(220, 60));

            phaseText = UiFactory.Label("Phase", bar.transform, "준비", 26, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)phaseText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(220, 60));

            guestText = UiFactory.Label("Guests", bar.transform, "", 22, TextAnchor.MiddleRight, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)guestText.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(-260, 0), new Vector2(180, 60));

            // 시간 진행바
            var barBg = UiFactory.Panel("TimeBarBg", bar.transform, UiFactory.Wood);
            UiFactory.SetRect(barBg, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(-20, 0), new Vector2(220, 16));
            timeBarFill = UiFactory.Panel("TimeBarFill", barBg.transform, UiFactory.Green).GetComponent<Image>();
            timeBarFill.type = Image.Type.Filled;
            timeBarFill.fillMethod = Image.FillMethod.Horizontal;
            timeBarFill.fillAmount = 0f;
            var frt = (RectTransform)timeBarFill.transform;
            UiFactory.Anchor(frt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 하단 서술(말풍선) 영역 + 상점/도감 버튼
            narrationText = UiFactory.Label("Narration", parent, "", 22, TextAnchor.LowerLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)narrationText.transform, new Vector2(0, 0), new Vector2(1, 0),
                              new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(-40, 40));

            var shopBtn = UiFactory.Button("ShopBtn", parent, "🛒 " + L.Get("ui.shop.buy"),
                                           UiFactory.Wood, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)shopBtn.transform, new Vector2(1, 0), new Vector2(1, 0),
                              new Vector2(1, 0), new Vector2(-20, 20), new Vector2(150, 56));
            shopBtn.onClick.AddListener(() => shop.Toggle());

            var catBtn = UiFactory.Button("CatalogBtn", parent, "📖 " + L.Get("ui.pause.catalog"),
                                          UiFactory.Wood, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)catBtn.transform, new Vector2(1, 0), new Vector2(1, 0),
                              new Vector2(1, 0), new Vector2(-180, 20), new Vector2(150, 56));
            catBtn.onClick.AddListener(() => catalog.Toggle());
        }

        void HookEvents()
        {
            if (economy != null) economy.OnGoldChanged += _ => RefreshHud();
            if (day != null)
            {
                day.OnPhaseChanged += p =>
                {
                    RefreshHud();
                    if (p == DayPhase.Settlement) settlement.Show();
                };
            }
            if (flow != null && flow.Observation != null)
                flow.Observation.OnBehaviorNarration += ShowNarration;
        }

        void Update()
        {
            if (day != null && timeBarFill != null && bootstrap != null && bootstrap.balance != null)
                timeBarFill.fillAmount = HudFormatter.TimeProgress(day.DayTimer, bootstrap.balance.dayLength);

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
            if (phaseText != null && day != null) phaseText.text = HudFormatter.Phase(day.Phase);
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
            if (day != null) day.StartDay();
            RefreshHud();
        }

        // ── 패널이 접근하는 헬퍼 ──
        public SaveGame Save => flow != null ? flow.Save : null;
        public ObservationManager Obs => flow != null ? flow.Observation : null;
        public EconomyManager Economy => economy;

        public bool TryBuy(FurnitureData data)
        {
            bool ok = bootstrap != null && bootstrap.BuyFurniture(data);
            if (ok) RefreshHud();
            return ok;
        }

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
