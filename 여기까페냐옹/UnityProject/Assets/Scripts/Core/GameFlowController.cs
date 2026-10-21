using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;
using YeogiCafe.Economy;
using YeogiCafe.Observation;
using YeogiCafe.Progression;
using YeogiCafe.Save;

namespace YeogiCafe.Core
{
    // 전체 배선 허브. 사양서 §3 파이프라인을 매니저 이벤트로 연결.
    // OnSessionComplete → 만족→결제→경제→관찰 스테이징→history. OnDayEnded → 승격→정산→재방문→단골.
    public class GameFlowController : MonoBehaviour
    {
        public BalanceConfig cfg;
        public DayManager day;
        public CatManager cats;
        public CafeContext cafe;
        public EconomyManager economy;
        public SaveManager saveManager;
        public YeogiCafe.Cafe.FurnitureManager furniture;   // 선택: 세이브 주입용

        public CustomerSpawner spawner;   // 손님 곡선 갱신용(선택)
        public YeogiCafe.Cafe.CafeManager cafeManager;  // 성장 승급 연동(선택)
        public RelationshipManager relationships;        // 관계 세이브 연결(선택)
        public EventManager events;                      // 에피소드/관계 이벤트(선택)
        public System.Func<string, FurnitureData> furnitureLookup;   // 세이브의 배치 가구 복원용(선택)
        public YeogiCafe.Order.OrderSystem orderSystem;               // 영업 종료 시 남은 주문 정리

        ObservationManager obs;
        RegularManager regular;
        SaveGame save;

        void Start()
        {
            save = saveManager != null ? saveManager.LoadOrNew() : new SaveGame();
            // 정산까지 끝낸 날에 껐다면(다음 날 버튼 전) 그날은 끝난 것으로 보고 다음 날 아침부터
            if (save.initialized && save.settledDay == save.currentDay) save.currentDay++;
            obs = new ObservationManager(cfg, save);
            regular = new RegularManager();

            // 이어하기 복원: 골드·일차·배치 가구. (이전엔 관찰 기록만 남고 골드/가구가 초기화되던 문제)
            economy.cfg = cfg; economy.Init();
            if (save.initialized) economy.SetGold(save.gold);
            else { save.initialized = true; save.gold = economy.Gold; }
            economy.OnGoldChanged += g => save.gold = g;
            day.RestoreDay(save.currentDay);
            cats.obs = obs;
            cats.save = save;                                // 단골 단계·선호자리 주입
            if (furniture != null)
            {
                furniture.save = save;                        // 배치 가구 영속 연결
                if (furnitureLookup != null) furniture.RestoreFromSave(furnitureLookup);
            }

            // 발견 기록 시 스포너 곡선 갱신 (밸런스 문서 06)
            obs.OnPreferenceRecorded += (_, __) => SyncSpawnerProgress();
            SyncSpawnerProgress();

            // 카페 승급 시: 손님 곡선·해금 풀 즉시 반영 (성장→새 손님 연쇄)
            if (cafeManager != null)
            {
                cafeManager.save = save;
                cafeManager.OnCafeUpgraded += _ => SyncSpawnerProgress();
            }
            if (relationships != null)
            {
                relationships.save = save;
                // 하루(아침) 시작 시 관계 근접 일일 상한 초기화
                day.OnPhaseChanged += p => { if (p == DayPhase.Morning) relationships.OnDayStart(); };
            }

            // 이벤트: 단골·관계 승급 → 에피소드 큐잉 → 정산 시 재생·보상
            if (events != null)
            {
                events.save = save;
                regular.OnRegularStageUp += events.OnRegularStageUp;
                // 단골 단계가 오르면 그날 밤 일지에 그 고양이 이야기 한 장
                regular.OnRegularStageUp += (id, st) => { if (st >= 2) save.storyQueue.Add(new StoryDTO { catId = id, kind = st >= 3 ? 3 : 2 }); };
                if (relationships != null) relationships.OnRelationStageUp += events.OnRelationStageUp;
                events.OnGrantReward += (gold, _) => { if (gold > 0) economy.AddGold(gold); };
            }

            // 메뉴: 가진 주방 기구로 만들 수 있는 메뉴만 메뉴판에(기구당 2개, 업그레이드 단계는 별 확률에)
            allMenus = new List<MenuData>(cafe.GetAvailableMenus());
            allMenus.RemoveAll(m => m == null || m.seasonal);   // 한정 메뉴는 SyncMenus가 계절마다 따로 붙인다
            YeogiCafe.Food.RecipeBook.Migrate(save);
            YeogiCafe.Visual.NyanPlaque.Ensure(save);           // 문 위 냥슐랭 명판
            SyncMenus();
            YeogiCafe.Order.OrderSystem.MenuLevel = id => save.GetMenuStat(id).level;
            GameEvents.OnEvent += (id, _) =>
            {
                if (id == GameEvents.MouseCaught || id == GameEvents.MouseEscaped)
                {
                    int v = id == GameEvents.MouseEscaped ? 2 : 1;
                    if (save.mouseStateDay != day.CurrentDay) { save.mouseStateDay = day.CurrentDay; save.mouseState = 0; }
                    save.mouseState = Mathf.Max(save.mouseState, v);
                }
            };

            cats.OnCatSpawned += HandleSpawn;
            day.OnDayEnded += HandleDayEnd;

            // 메뉴/좌석은 씬에서 CafeContext에 등록되어 있다고 가정
            day.StartDay();
        }

        public event System.Action<ReviewDTO> OnReview;     // 새 리뷰(토스트용)
        List<MenuData> allMenus;
        public IReadOnlyList<MenuData> AllMenus => allMenus;
        public void SyncMenus()
        {
            if (allMenus == null || save == null) return;
            var list = YeogiCafe.Food.RecipeBook.Available(allMenus, save);
            foreach (var m in SeasonBook.Today(save, t => YeogiCafe.Food.RecipeBook.ToolOwned(save, t))) list.Add(m);   // 계절 한정
            cafe.SetMenu(list);
        }

        void SyncSpawnerProgress()
        {
            if (spawner == null) return;
            spawner.save = save;                     // 해금 필터용
            spawner.cafeLevel = save.cafeLevel;
            int discovered = 0;
            foreach (var c in save.cats)
                if (c.observation.RecordedAxisCount() > 0) discovered++;
            spawner.discoveredCats = discovered;
        }

        void HandleSpawn(CatBrain brain)
        {
            brain.OnSessionComplete += HandleSessionComplete;
        }

        float relCheckTimer;
        const float RelCheckInterval = 2f;

        void Update()
        {
            // 관계 근접 누적: 착석한 잠재 짝이 동시에 있으면 점수(일1회 상한은 매니저가 처리)
            if (relationships == null || cats == null) return;
            relCheckTimer += Time.deltaTime;
            if (relCheckTimer < RelCheckInterval) return;
            relCheckTimer = 0;

            var active = cats.Active;
            for (int i = 0; i < active.Count; i++)
                for (int j = i + 1; j < active.Count; j++)
                {
                    var a = active[i]; var b = active[j];
                    if (a == null || b == null) continue;
                    if (a.IsSpecialGuest || b.IsSpecialGuest) continue;
                    // 둘 다 착석 중일 때만
                    if (a.State == CatState.Idle || a.State == CatState.FacilityAction || a.State == CatState.Eat)
                        if (b.State == CatState.Idle || b.State == CatState.FacilityAction || b.State == CatState.Eat)
                            relationships.RecordProximity(a.data, b.data);
                }
        }

        readonly System.Random giftRng = new();

        // 배달냥: 나온 메뉴만 판매가의 70%로(별·냥슐랭 배율 반영) 사 간다. 도감·리뷰·단골과 무관.
        void HandleDelivery(CatBrain brain)
        {
            int pay = 0, cost = 0;
            foreach (var (menu, stars) in brain.DeliveryServed)
            {
                pay += Mathf.RoundToInt(menu.price * YeogiCafe.Food.DeliveryCat.PayRate
                                        * YeogiCafe.Food.RecipeBook.StarPayMult(stars) * YeogiCafe.Food.NyanGuide.PayMult(save.nyanStars));
                cost += menu.cost;
                if (stars > 0) save.GetMenuStat(menu.menuId).sold++;
            }
            if (pay > 0) economy.AddGold(pay);
            if (cost > 0) economy.AddIngredientCost(cost);
            int got = brain.DeliveryServed.Count, want = brain.DeliveryOrdered;
            CustomerSpawner.Toast?.Invoke(got == 0 ? YeogiCafe.Loc.L.Get("delivery.fail")
                : got < want ? YeogiCafe.Loc.L.Get("delivery.partial", ("a", got.ToString()), ("b", want.ToString()), ("g", pay.ToString()))
                : YeogiCafe.Loc.L.Get("delivery.done", ("n", got.ToString()), ("g", pay.ToString())));
            cats.Despawn(brain, got == want ? 80f : 30f);
        }

        // §3.2 OnLeave 파이프라인
        void HandleSessionComplete(CatBrain brain)
        {
            var data = brain.data;
            if (brain.IsDelivery) { HandleDelivery(brain); return; }
            var catSave = save.GetOrCreateCat(data.catId);
            if (brain.State == CatState.Leave && brain.ServedMenu != null && !brain.IsSpecialGuest) lastLeftToday = data.catId;

            // 만족도 계산 (§3.3). 분위기 승수는 cfg.atmosphereAffectsSatisfaction=true(정식)일 때만 반영.
            var atmo = furniture != null ? furniture.CurrentAtmosphere : default;
            var sr = new SessionResult
            {
                servedFavorite = brain.ServedFavorite,
                servedPreferredTag = false,          // MVP: favorite만
                wasForcedSecond = brain.ForcedSecond,
                usedPreferredSeat = brain.UsedPreferredSeat,
                usedPreferredFacility = brain.UsedPreferredFacility,
                fastService = true,                  // S1: 서빙 즉시
                totalWaitOver = 0f,     // TODO(밸런스): 주문~서빙 대기시간을 만족도에 반영할지 기획 결정 필요
                orderFailed = brain.ServedMenu == null,
                hasDominant = cfg.atmosphereAffectsSatisfaction && atmo.hasDominant,
                dominantAtmosphere = atmo.dominant,
                touchMood = brain.TouchMood,
                wrongDish = brain.WrongDish, mindRead = brain.MindRead
            };
            float satisfaction = SatisfactionCalculator.Compute(data, sr, cfg);
            // 음식 별·냥슐랭 스타·가구 별
            int fseat = YeogiCafe.Food.FurnitureStars.Stars(save, brain.SeatFurnitureId);
            int ffac = 1; foreach (var f in brain.UsedFacilityIds) ffac = Mathf.Max(ffac, YeogiCafe.Food.FurnitureStars.Stars(save, f));
            satisfaction += YeogiCafe.Food.RecipeBook.StarSatisfaction(brain.ServedMenu != null ? brain.ServedStars : 0)
                          + YeogiCafe.Food.NyanGuide.SatisfactionBonus(save.nyanStars)
                          + YeogiCafe.Food.FurnitureStars.Satisfaction(Mathf.Max(fseat, ffac));
            // 인테리어 테마가 좋아하는 분위기면 + / 제철 한정 메뉴가 최애와 같은 맛 계열이면 +
            if (ThemeBook.Matches(save.theme, data.atmospherePreference)) satisfaction += ThemeBook.Bonus;
            if (brain.ServedMenu != null && brain.ServedMenu.seasonal && allMenus != null)
            {
                var fav = allMenus.Find(m => m != null && m.menuId == data.foodFavoriteMenuId);
                if (SeasonBook.Likes(brain.ServedMenu, fav)) satisfaction += SeasonBook.Bonus;
            }
            satisfaction = Mathf.Clamp(satisfaction, 0f, 100f);

            // 결제 (§3.4)
            // 쥐 소동 때 쥐를 쫓아 뛰쳐나간 고양이는 계산을 잊는다(재료비는 그대로 나감)
            int price = brain.ServedMenu != null && !brain.ChaseSkippedPay ? brain.ServedMenu.price : 0;
            if (brain.ServedMenu != null && brain.ChaseSkippedPay) LostToMice += brain.ServedMenu.price;
            int pay = Mathf.RoundToInt(price * EconomyManager.TipMultiplier(satisfaction)
                                       * YeogiCafe.Food.RecipeBook.StarPayMult(brain.ServedStars)
                                       * YeogiCafe.Food.NyanGuide.PayMult(save.nyanStars));
            economy.AddGold(pay);
            if (brain.ServedMenu != null) economy.AddIngredientCost(brain.ServedMenu.cost);
            if (brain.ServedMenu != null && brain.ServedStars > 0) save.GetMenuStat(brain.ServedMenu.menuId).sold++;
            YeogiCafe.Food.FurnitureStars.CountUse(save, brain.SeatFurnitureId);
            foreach (var f in brain.UsedFacilityIds) YeogiCafe.Food.FurnitureStars.CountUse(save, f);

            // 냥슐랭 심사원: 도감·단골·선물과 무관. 채점만 하고 떠난다.
            if (YeogiCafe.Food.NyanGuide.IsInspector(data.catId))
            {
                int placed = save.placedFurniture.Count;
                YeogiCafe.Food.NyanGuide.Evaluate(save, day.CurrentDay, brain.ServedStars, brain.WrongDish, brain.ServeWait, placed);
                cats.Despawn(brain, satisfaction);
                return;
            }

            // 만족한 고양이는 다음 날 아침 문 앞에 선물(아직 모르는 취향의 힌트)
            if (satisfaction >= cfg.giftThreshold && giftRng.NextDouble() < cfg.giftChance && !save.pendingGifts.Exists(g => g.catId == data.catId))
            {
                var obsRec = catSave.observation;
                string gid = YeogiCafe.Data.GiftCatalog.Pick(data, a => obsRec.Get(a).state == ObsState.Recorded, giftRng);
                if (gid != null) save.pendingGifts.Add(new GiftDTO { catId = data.catId, giftId = gid, day = day.CurrentDay });
            }

            // 방문·만족 이력
            catSave.PushSatisfaction(satisfaction);
            catSave.visitCount += 1;
            catSave.lastVisitDay = day.CurrentDay;

            // [프리플라이트 수정] 재방문 판정용 "이번 방문 실제 결과" 저장.
            //   재방문 보너스가 Recorded 상태가 아니라 '실제로 이번에 최애/선호좌석을 이용했는지'를 근거로.
            catSave.lastServedFavorite = brain.ServedFavorite;
            catSave.lastUsedPreferredSeat = brain.UsedPreferredSeat;
            catSave.lastUsedPreferredFacility = brain.UsedPreferredFacility;

            // 한 줄 리뷰(먹은 것·자리·시설·쥐 소동)
            int mouse = brain.ChaseSkippedPay ? 2 : (save.mouseStateDay == day.CurrentDay ? save.mouseState : 0);
            var review = YeogiCafe.Food.ReviewBook.Write(save, new YeogiCafe.Food.ReviewBook.Visit
            {
                cat = data, day = day.CurrentDay, satisfaction = satisfaction,
                menu = brain.ServedMenu, stars = brain.ServedStars, favorite = brain.ServedFavorite, forcedSecond = brain.ForcedSecond,
                wrongDish = brain.WrongDish, orderFailed = brain.ServedMenu == null,
                preferredSeat = brain.UsedPreferredSeat, preferredFacility = brain.UsedPreferredFacility, mouse = mouse
            });
            OnReview?.Invoke(review);

            cats.Despawn(brain, satisfaction);
        }

        // §3.5/3.8 하루 종료
        void HandleDayEnd()
        {
            // 영업 종료: 아직 카페에 남은 손님을 먼저 정리(결제·만족 확정) → 그 다음 승격.
            // 순서가 바뀌면 남은 손님의 오늘 단서가 내일로 새거나 정산에서 빠진다.
            var remaining = new List<CatBrain>(cats.Active);
            // 마감 놀아주기 상대: 아직 남아 쉬던 손님 중 하나(없으면 오늘 마지막으로 기분 좋게 나간 손님)
            ClosingGuestId = null;
            var cands = new List<CatBrain>();
            foreach (var b in remaining)
                if (b != null && !b.IsSpecialGuest && (b.State == CatState.Idle || b.State == CatState.FacilityAction || b.State == CatState.Eat || b.State == CatState.WaitFood))
                    cands.Add(b);
            if (cands.Count > 0) ClosingGuestId = cands[Random.Range(0, cands.Count)].data.catId;
            else ClosingGuestId = lastLeftToday;
            foreach (var b in remaining) if (b != null) b.ForceLeave();
            if (orderSystem != null) orderSystem.ClearAll();

            obs.OnDayEnd();                                  // 승격
            foreach (var c in save.cats) regular.Evaluate(c); // 단골(→이벤트 큐잉)
            // 졸업: 세 가지 취향을 다 알아낸 단골 3단계 고양이 → 졸업 편지(이후 가끔만 놀러 옴)
            foreach (var c in save.cats)
            {
                if (c.graduated || c.regularStage < 3 || c.observation.RecordedAxisCount() < 3) continue;
                c.graduated = true;
                save.storyQueue.Add(new StoryDTO { catId = c.catId, kind = 9 });
                YeogiCafe.Core.GameEvents.Raise(YeogiCafe.Core.GameEvents.Graduate, c.catId);
            }
            // 대기 이벤트(단골·관계 에피소드) 재생·보상 (정산 화면에서 순차 표시)
            if (events != null) { while (events.TryPlayNext()) { } }
            save.settledDay = save.currentDay;               // 오늘 장사는 끝(정산 중 종료해도 이 날을 다시 하지 않게)
            if (saveManager != null) saveManager.Save(save); // 오토세이브
            // 정산 화면은 '남은 손님 정리 + 승격'이 끝난 뒤에 열어야 오늘 결과(…/★)가 반영된다.
            // (이전엔 Phase=Settlement 시점에 열려 승격 전 상태가 표시됐다)
            OnSettlementReady?.Invoke();
            // 정산 화면에서 플레이어가 취향 기록·구매 후 StartNextDay() 호출
        }

        // §3.7 정산 이후 플레이어가 "다음 날" → 재방문 큐 편성 후 영업 시작
        public List<CatData> PrepareNextDayQueue(IReadOnlyList<CatData> allDiscoverable,
                                                 System.Func<string, CatData> lookup)
        {
            day.AdvanceToNextDay();
            var queue = new List<CatData>();
            foreach (var cs in save.cats)
            {
                var cd = lookup(cs.catId);
                if (cd == null) continue;
                // [프리플라이트 수정] Recorded 상태가 아니라 '지난 방문 실제 결과'로 재방문 판정
                float chance = RevisitSystem.RevisitChance(cs, day.CurrentDay, cfg);
                if (Random.value < chance) queue.Add(cd);
            }
            return queue;   // 스포너에 SetQueue로 전달(미발견 신규는 스포너 spawnWeight 테이블에서 추가)
        }

        // 정산 화면 [다음 날] → 일차 증가 + 기록 결과 저장 + 영업 시작.
        // (정산 중 플레이어의 [기록]·구매가 HandleDayEnd 이후에 일어나므로 여기서 다시 저장해야 유실이 없다.)
        public event System.Action OnSettlementReady;

        public void BeginNextDay()
        {
            economy.ResetDaily();
            LostToMice = 0; lastLeftToday = null;                            // 정산 화면에서 오늘 매출을 다 보여준 뒤 초기화
            day.AdvanceToNextDay();
            save.currentDay = day.CurrentDay;
            if (saveManager != null) saveManager.Save(save);
            SyncSpawnerProgress();
            SyncMenus();                                       // 계절이 바뀌면 한정 메뉴도 바뀐다
            day.StartDay();
        }

        // 창 닫기·게임 종료: 영업 중이면 구매·판매·배치·업그레이드는 남기고 오늘 번 돈(매출)과 재료비만 되돌려 저장한다.
        // 다시 켜면 그날 아침부터 다시 영업(같은 날을 반복해 돈을 불리는 건 막는다).
        public void SaveOnQuit()
        {
            if (saveManager == null || save == null || !save.initialized) return;
            if (save.settledDay != save.currentDay && economy != null)
                save.gold = Mathf.Max(0, economy.Gold - economy.TodayRevenue + economy.TodayCost);
            saveManager.Save(save);
        }
        bool quitSaved;
        void OnApplicationQuit() { if (quitSaved) return; quitSaved = true; SaveOnQuit(); }

        public int LostToMice { get; set; }
        public string ClosingGuestId { get; private set; }   // 마감 직전 놀아줄 마지막 손님
        string lastLeftToday;                  // 오늘 쥐 소동으로 못 받은 음식값(정산 표시용)
        public ObservationManager Observation => obs;
        public SaveGame Save => save;
    }
}
