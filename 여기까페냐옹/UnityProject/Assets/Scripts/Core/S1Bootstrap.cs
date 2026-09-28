using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Cafe;
using YeogiCafe.Data;
using YeogiCafe.Economy;
using YeogiCafe.Save;

namespace YeogiCafe.Core
{
    // S1 씬 조립 가이드 겸 실행 진입점.
    // 인스펙터에서 매니저 참조를 연결하거나, 없으면 런타임에 최소 구성으로 생성.
    // 목적: "빈 씬 + 이 컴포넌트"로 관찰 루프가 돌아가는 최소 실행체.
    public class S1Bootstrap : MonoBehaviour
    {
        [Header("데이터")]
        public BalanceConfig balance;
        public List<CatData> starterCats = new();     // 치즈/삼색/젖소 (S1 고정 큐)
        public List<CatData> allCats = new();          // 8마리 전체 (해금 필터, S3)
        public List<MenuData> starterMenus = new();
        public List<FurnitureData> starterSeats = new(); // 최소 일반석
        public FurnitureData windowSeatData;             // 상점 구매 대상(창가석)
        public CafeLevelConfig cafeLevelConfig;          // 성장 조건 SO

        [Header("매니저 (비우면 자동 생성)")]
        public DayManager day;
        public CatManager cats;
        public CustomerSpawner spawner;
        public CafeContext cafe;
        public EconomyManager economy;
        public SaveManager saveManager;
        public FurnitureManager furniture;
        public CafeManager cafeManager;
        public YeogiCafe.Progression.RelationshipManager relationships;
        public YeogiCafe.Progression.EventManager events;
        public List<CatEventData> allEvents = new();     // 에피소드+관계 이벤트
        public GameFlowController flow;

        void Awake()
        {
            EnsureManagers();
            WireReferences();
            SeedCafe();
        }

        void EnsureManagers()
        {
            if (cafe == null) cafe = gameObject.AddComponent<CafeContext>();
            if (economy == null) economy = gameObject.AddComponent<EconomyManager>();
            if (saveManager == null) saveManager = gameObject.AddComponent<SaveManager>();
            if (day == null) day = gameObject.AddComponent<DayManager>();
            if (cats == null) cats = gameObject.AddComponent<CatManager>();
            if (spawner == null) spawner = gameObject.AddComponent<CustomerSpawner>();
            if (furniture == null) furniture = gameObject.AddComponent<FurnitureManager>();
            if (cafeManager == null) cafeManager = gameObject.AddComponent<CafeManager>();
            if (relationships == null) relationships = gameObject.AddComponent<YeogiCafe.Progression.RelationshipManager>();
            if (events == null) events = gameObject.AddComponent<YeogiCafe.Progression.EventManager>();
            if (flow == null) flow = gameObject.AddComponent<GameFlowController>();
        }

        void WireReferences()
        {
            day.cfg = balance;
            economy.cfg = balance;
            cats.cfg = balance; cats.cafe = cafe;
            spawner.cfg = balance; spawner.day = day; spawner.cats = cats; spawner.cafe = cafe;
            spawner.fixedQueue = new List<CatData>(starterCats);
            spawner.allCats = new List<CatData>(allCats);   // 해금 풀(비면 fixedQueue 사용)
            furniture.cafe = cafe; furniture.economy = economy;
            cafeManager.economy = economy; cafeManager.config = cafeLevelConfig;
            cafe.relationships = relationships;   // 친구 근접 판정 연결
            flow.cfg = balance; flow.day = day; flow.cats = cats; flow.cafe = cafe;
            flow.economy = economy; flow.saveManager = saveManager; flow.furniture = furniture;
            events.allEvents = new List<CatEventData>(allEvents);
            flow.spawner = spawner; flow.cafeManager = cafeManager; flow.relationships = relationships; flow.events = events;
        }

        void SeedCafe()
        {
            cafe.SetMenu(starterMenus);
            // 시작 좌석 배치(FurnitureManager 슬롯이 없으면 직접 생성)
            foreach (var seatData in starterSeats)
            {
                var go = new GameObject("Seat_" + seatData.furnitureId);
                var sb = go.AddComponent<SeatBehaviour>();
                sb.source = seatData;
                cafe.RegisterSeat(sb);
            }
        }

        // 상점 버튼에서 호출: 창가석 구매
        public bool BuyWindowSeat() => windowSeatData != null && furniture.BuyAndPlace(windowSeatData);
    }
}
