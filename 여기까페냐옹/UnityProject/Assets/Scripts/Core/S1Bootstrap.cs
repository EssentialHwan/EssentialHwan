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
        public List<FurnitureData> shopFurniture = new();// 상점 UI 판매 목록(비면 allFurniture - 시작 좌석으로 자동 구성)
        public List<FurnitureData> allFurniture = new(); // 18종 전체(세이브 복원·상점 목록 원천)
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
            BuildShopListIfEmpty();
            EnsureManagers();
            WireReferences();
            SeedCafe();
        }

        void BuildShopListIfEmpty()
        {
            // 직렬화된 목록이 비었거나 전체 가구보다 이상하게 적으면(에디터 재임포트 타이밍 문제) 다시 구성
            int expected = allFurniture.Count;
            if (shopFurniture != null && shopFurniture.Count > 0 && (allFurniture.Count == 0 || shopFurniture.Count >= expected)) return;
            shopFurniture = new List<FurnitureData>();
            foreach (var f in allFurniture)
                if (f != null) shopFurniture.Add(f);
            // 가격 오름차순 → 초반에 살 수 있는 것부터 보이게
            shopFurniture.Sort((a, b) => a.price.CompareTo(b.price));
        }

        FurnitureData LookupFurniture(string id)
        {
            foreach (var f in allFurniture) if (f != null && f.furnitureId == id) return f;
            return null;
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
            flow.furnitureLookup = LookupFurniture;
        }

        [Header("씬 배치")]
        public Transform entrance;              // 없으면 CafeLayout.Entrance에 자동 생성
        public Transform seatRoot;              // 좌석을 놓을 부모(없으면 자동)

        // 좌석·상점 슬롯은 CafeLayout(공간 배치표)을 따른다 → 벽·창·문 아트와 위치가 맞는다.
        void SeedCafe()
        {
            cafe.SetMenu(starterMenus);

            if (entrance == null)
            {
                var e = new GameObject("Entrance");
                e.transform.position = CafeLayout.Entrance;
                entrance = e.transform;
            }
            cafe.entrance = entrance;

            if (seatRoot == null) seatRoot = new GameObject("Seats").transform;
            for (int i = 0; i < starterSeats.Count; i++)
            {
                var seatData = starterSeats[i];
                if (seatData == null) continue;
                var go = new GameObject("Seat_" + seatData.furnitureId);
                go.transform.SetParent(seatRoot);
                var p = i < CafeLayout.StarterSeats.Length ? CafeLayout.StarterSeats[i]
                                                           : new Vector2(2f + i * 2.2f, 1.9f);
                go.transform.position = new Vector3(p.x, p.y, 0f);
                var sb = go.AddComponent<SeatBehaviour>();
                sb.source = seatData;
                cafe.RegisterSeat(sb);
            }

            if (furniture.slots.Count == 0)
            {
                var slotRoot = new GameObject("FurnitureSlots").transform;
                int n = 0;
                foreach (var spec in CafeLayout.Slots)
                {
                    var slotGo = new GameObject($"Slot_{n++}_{spec.kind}");
                    slotGo.transform.SetParent(slotRoot);
                    slotGo.transform.position = new Vector3(spec.pos.x, spec.pos.y, 0f);
                    furniture.slots.Add(new FurnitureManager.SlotEntry
                    {
                        slot = slotGo.transform, kind = spec.kind,
                        nearWindow = spec.nearWindow, minCafeLevel = spec.minLevel
                    });
                }
            }
        }

        // 상점 버튼에서 호출: 창가석 구매
        public bool BuyWindowSeat() => windowSeatData != null && furniture.BuyAndPlace(windowSeatData);

        // 상점 UI에서 호출: 임의 가구 구매·배치(→ CafeContext 등록 → 관찰 채널 개방)
        public bool BuyFurniture(FurnitureData data) => data != null && furniture.BuyAndPlace(data);
    }
}
