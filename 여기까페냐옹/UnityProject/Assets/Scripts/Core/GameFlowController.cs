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

        ObservationManager obs;
        RegularManager regular;
        SaveGame save;

        void Start()
        {
            save = saveManager != null ? saveManager.LoadOrNew() : new SaveGame();
            obs = new ObservationManager(cfg, save);
            regular = new RegularManager();

            economy.cfg = cfg; economy.Init();
            cats.obs = obs;
            cats.save = save;                                // 단골 단계·선호자리 주입
            if (furniture != null) furniture.save = save;   // 배치 가구 영속 연결

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
                if (relationships != null) relationships.OnRelationStageUp += events.OnRelationStageUp;
                events.OnGrantReward += (gold, _) => { if (gold > 0) economy.AddGold(gold); };
            }

            cats.OnCatSpawned += HandleSpawn;
            day.OnDayEnded += HandleDayEnd;

            // 메뉴/좌석은 씬에서 CafeContext에 등록되어 있다고 가정
            day.StartDay();
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
                    // 둘 다 착석 중일 때만
                    if (a.State == CatState.Idle || a.State == CatState.FacilityAction || a.State == CatState.Eat)
                        if (b.State == CatState.Idle || b.State == CatState.FacilityAction || b.State == CatState.Eat)
                            relationships.RecordProximity(a.data, b.data);
                }
        }

        // §3.2 OnLeave 파이프라인
        void HandleSessionComplete(CatBrain brain)
        {
            var data = brain.data;
            var catSave = save.GetOrCreateCat(data.catId);

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
                totalWaitOver = 0f,
                orderFailed = brain.ServedMenu == null,
                hasDominant = cfg.atmosphereAffectsSatisfaction && atmo.hasDominant,
                dominantAtmosphere = atmo.dominant
            };
            float satisfaction = SatisfactionCalculator.Compute(data, sr, cfg);

            // 결제 (§3.4)
            int price = brain.ServedMenu != null ? brain.ServedMenu.price : 0;
            int pay = Mathf.RoundToInt(price * EconomyManager.TipMultiplier(satisfaction));
            economy.AddGold(pay);
            if (brain.ServedMenu != null) economy.AddIngredientCost(brain.ServedMenu.cost);

            // 방문·만족 이력
            catSave.PushSatisfaction(satisfaction);
            catSave.visitCount += 1;
            catSave.lastVisitDay = day.CurrentDay;

            // [프리플라이트 수정] 재방문 판정용 "이번 방문 실제 결과" 저장.
            //   재방문 보너스가 Recorded 상태가 아니라 '실제로 이번에 최애/선호좌석을 이용했는지'를 근거로.
            catSave.lastServedFavorite = brain.ServedFavorite;
            catSave.lastUsedPreferredSeat = brain.UsedPreferredSeat;
            catSave.lastUsedPreferredFacility = brain.UsedPreferredFacility;

            cats.Despawn(brain, satisfaction);
        }

        // §3.5/3.8 하루 종료
        void HandleDayEnd()
        {
            obs.OnDayEnd();                                  // 승격
            foreach (var c in save.cats) regular.Evaluate(c); // 단골(→이벤트 큐잉)
            // 대기 이벤트(단골·관계 에피소드) 재생·보상 (정산 화면에서 순차 표시)
            if (events != null) { while (events.TryPlayNext()) { } }
            economy.ResetDaily();
            if (saveManager != null) saveManager.Save(save); // 오토세이브
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

        public ObservationManager Observation => obs;
        public SaveGame Save => save;
    }
}
