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
                              new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(170, 56));
            goldText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // 손님 수: 판자 나뭇결 선 위에 글자가 겹치지 않게 작은 종이 배지 위에
            var guestPill = UiFactory.Panel("GuestPill", bar.transform, UiFactory.Cream);
            UiFactory.Frame(guestPill, UiFactory.FrameStyle.Card, UiFactory.Cream);
            UiFactory.SetRect(guestPill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(186, 0), new Vector2(124, 40));
            guestText = UiFactory.Label("Guests", guestPill.transform, "", 19, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.Anchor((RectTransform)guestText.transform, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0));

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

            // 오늘의 의뢰(왼쪽, 손님 수 옆)
            var qBtn = UiFactory.Button("QuestBtn", bar.transform, L.Get("ui.quest.btn"), UiFactory.YellowLight, UiFactory.Ink, out questBtnLabel);
            UiFactory.SetRect((RectTransform)qBtn.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                              new Vector2(0, 0.5f), new Vector2(330, 0), new Vector2(110, 44));
            qBtn.onClick.AddListener(ToggleQuests);

            // 가구 배치(끌어서 옮기기)
            var editBtn = UiFactory.Button("EditBtn", bar.transform, L.Get("ui.edit.open"),
                                           UiFactory.Wood, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)editBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                              new Vector2(1, 0.5f), new Vector2(-404, 0), new Vector2(120, 48));
            editBtn.onClick.AddListener(() => { shop.Hide(); catalog.Hide(); CloseKitchen(); editMode.Toggle(); });

            // 설정 — 의뢰 버튼 오른쪽(가운데 시간 표시와 겹치지 않게 작게). ESC로도 열림
            var optBtn = UiFactory.Button("OptBtn", bar.transform, L.Get("opt.btn"), UiFactory.CreamDim, UiFactory.WoodDark, out var optLbl);
            optLbl.fontSize = 18;
            UiFactory.SetRect((RectTransform)optBtn.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                              new Vector2(0, 0.5f), new Vector2(446, 0), new Vector2(68, 44));
            optBtn.onClick.AddListener(() => settings.Toggle());

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
                flow.OnSettlementReady += () =>
                {
                    shop.Hide(); catalog.Hide(); CloseKitchen(); editMode.Exit(); questPanel.SetActive(false);
                    // 마감 직전: 마지막 손님과 놀아주기 → 끝나면 밤 정산(도감)
                    var guest = flow.ClosingGuestId != null ? LookupCat(flow.ClosingGuestId) : null;
                    if (guest != null && Save != null) closing.Show(guest, Save.GetOrCreateCat(guest.catId), () => { settlement.Show(); RefreshHud(); });
                    else settlement.Show();
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
            os.OnTicketPlaced += t => ShowCatBubble(t.catId, t.vague
                ? L.Get("ui.order.askhint", ("hint", L.Get(t.hintKey)))
                : L.Get("ui.order.ask", ("menu", t.menu != null ? L.Get($"menu.{t.menu.menuId}") : "")));
            os.OnTicketCancelled += t => ShowCatBubble(t.catId, L.Get("ui.order.gaveup"));
            os.OnTicketReady += _ => YeogiCafe.Audio.SoundManager.Sfx("sfx_bell", 0.45f, 0.02f);
            ordersHooked = true;
        }

        public void ShowCatBubble(string catId, string text) => bubbles?.Show(catId, text);

        // 화면 위쪽 알림(선물·의뢰 완료 등) — 3.5초 뒤 사라짐
        Text toastText; CanvasGroup toastCg; float toastT;
        readonly Queue<string> toastQueue = new();
        public void ShowToast(string text)
        {
            if (toastCg != null && toastT > 0.6f) { toastQueue.Enqueue(text); return; }   // 앞 알림이 끝나면 차례로
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
            YeogiCafe.Audio.SoundManager.Sfx("sfx_boop", 0.35f, 0.04f, "toast", 0.4f);
            var trt = (RectTransform)toastText.transform.parent;          // 긴 알림은 두 줄로 → 높이 맞춤
            trt.sizeDelta = new Vector2(720, Mathf.Max(64f, toastText.preferredHeight + 20f));
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

        void Update()
        {
            TryHookObservation();
            if (Save != null) Weather.EnsureToday(Save);
            if (quests != null && Save != null && Save.questDay != Save.currentDay && flow != null) { quests.EnsureToday(); RefreshQuests(); }
            TryHookOrders();
            kitchen?.Tick(Time.unscaledDeltaTime);
            closing?.Tick(Time.unscaledDeltaTime);
            settings?.Tick();
            title?.Tick(Time.unscaledDeltaTime);
            if (Input.GetKeyDown(KeyCode.Escape) && settings != null && (closing == null || !closing.IsOpen)) settings.Toggle();   // ESC = 설정
            editMode?.Tick(Time.unscaledDeltaTime);
            if (day != null && timeBarFill != null && bootstrap != null && bootstrap.balance != null)
                timeBarFill.fillAmount = HudFormatter.TimeProgress(day.DayTimer, bootstrap.balance.dayLength);

            bubbles?.Tick(Time.unscaledDeltaTime);
            if (toastCg != null && toastT > 0f) { toastT -= Time.unscaledDeltaTime; toastCg.alpha = Mathf.Clamp01(toastT / 0.5f); }
            if (toastCg != null && toastT <= 0.6f && toastQueue.Count > 0) { toastT = 0f; ShowToast(toastQueue.Dequeue()); }
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
                sb.Append(q.done ? "<color=#6F9A55>[완료]</color> " : $"[{q.progress}/{q.goal}] ").Append(L.Get(q.id))
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
