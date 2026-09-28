using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Observation;

namespace YeogiCafe.AI
{
    // 사양서 §2.2 — HFSM (S1 선형판). 분기(Move/Social)는 1.0 제외.
    // 이 스켈레톤은 컴파일·구조 검증용. 이동은 NavMeshAgent 연결 지점만 표시.
    public class CatBrain : MonoBehaviour
    {
        [Header("주입")]
        public CatData data;
        public BalanceConfig cfg;

        // 외부 컨텍스트 제공자 (매니저가 주입) — 테스트/구현 분리
        public System.Func<IReadOnlyList<ISeat>> GetSeats;
        public System.Func<IReadOnlyList<IFacility>> GetFacilities;
        public System.Func<IReadOnlyList<MenuData>> GetAvailableMenus;
        // [S2] 주문 시스템 주입(있으면 WaitFood 경로 사용, 없으면 S1 직행)
        public YeogiCafe.Order.OrderSystem OrderSystem;
        public System.Func<ISeat, int> NearbyFriends = _ => 0;
        public System.Func<ISeat, int> NearbyStrangers = _ => 0;
        public Vector3 Entrance;
        public ObservationManager Obs;
        // 좌석 점유자 등록 콜백(정식 관계용). (seat, catId or null)
        public System.Action<ISeat, string> SetOccupant;
        // 단골 "늘 그 자리"(27장): 매니저가 주입
        public int RegularStage = 1;
        public string FavoriteSeatId;

        CatState state = CatState.Enter;
        ISeat seat; bool seatFallback;
        float sitTimer, sitDuration, decisionTimer;
        readonly Dictionary<string, float> recentUse = new();

        const float DecisionInterval = 3f;

        // ── 세션 추적 (OnLeave 파이프라인 §3.2/3.3 입력) ──
        public MenuData ServedMenu { get; private set; }
        public bool ServedFavorite { get; private set; }
        public bool ForcedSecond { get; private set; }
        public bool UsedPreferredSeat { get; private set; }
        public bool UsedPreferredFacility { get; private set; }
        public string ChosenSeatId { get; private set; }
        public CatState State => state;
        public bool IsFinished => state == CatState.Leave ||
                                  state == CatState.LeaveDisappointed;

        // 매니저가 구독: Leave 진입 시 1회 호출
        public System.Action<CatBrain> OnSessionComplete;
        bool sessionCompleted;

        // 이동(선택). null이면 즉시 도착으로 간주(테스트/프로토).
        public ICatMover Mover;
        bool walkingToSeat;

        void Update()
        {
            switch (state)
            {
                case CatState.Enter:      state = CatState.FindSeat; break;
                case CatState.FindSeat:   DoFindSeat(); break;
                case CatState.Sit:        DoSit(); break;
                case CatState.DecideOrder: DoOrder(); break;
                case CatState.WaitFood:   DoWaitFood(); break;
                case CatState.Eat:        DoEat(); break;
                case CatState.FacilityAction:
                case CatState.Idle:       DoActionLoop(); break;
                case CatState.Pay:        DoPay(); break;
                case CatState.Leave:
                case CatState.LeaveDisappointed:
                    if (!sessionCompleted) { sessionCompleted = true; OnSessionComplete?.Invoke(this); }
                    break;
                case CatState.WaitOutside: DoWaitOutside(); break;
            }
        }

        float waitOutsideTimer;
        const float WaitOutsidePatience = 20f;  // 자리 안 나면 결국 귀가

        void DoWaitOutside()
        {
            waitOutsideTimer += Time.deltaTime;
            // 자리가 나면 재탐색
            var seats = GetSeats();
            bool anyFree = false;
            for (int i = 0; i < seats.Count; i++) if (seats[i].IsFree) { anyFree = true; break; }
            if (anyFree) { waitOutsideTimer = 0; state = CatState.FindSeat; return; }
            // 오래 기다려도 자리 없음 → 귀가
            if (waitOutsideTimer >= WaitOutsidePatience) state = CatState.LeaveDisappointed;
        }

        void DoFindSeat()
        {
            var choice = SeatSelector.Choose(data, GetSeats(), Entrance, NearbyFriends, NearbyStrangers, cfg,
                                             FavoriteSeatId, RegularStage);
            if (choice.seat == null)
            {
                // 만석 → 성격분기 (부록 J-B)
                state = FullCafePolicy.DecideWhenFull(data);
                return;
            }
            seat = choice.seat; seatFallback = choice.isFallback;
            ChosenSeatId = seat.SeatId;
            seat.Occupy(this);                 // 좌석 점유 → 다른 고양이 후보에서 제외
            SetOccupant?.Invoke(seat, data.catId);  // 관계 근접 판정용
            UsedPreferredSeat = !seatFallback;
            // 관찰 훅: 좌석 이용 (fallback이면 약한 단서). 서술은 NarrationCatalog로 세분화.
            Obs?.RecordClue(data.catId, ClueType.SeatUse, SeatKey(seat), seatFallback,
                            Narrate(NarrationCatalog.SeatKey(seat.Tags, seatFallback)));
            // 창가 계열이면 응시 단서 (강한 좌석 신호)
            if (!seatFallback && HasTag(seat, SeatTag.OutsideView))
                Obs?.RecordClue(data.catId, ClueType.IdleGaze, SeatKey(seat), false,
                                Narrate("narr.seat.window"));
            sitDuration = Mathf.Min(BaseSit() * PersonalityFactor(), cfg.maxSitDuration); // P7
            sitTimer = 0;
            // 좌석까지 이동 후 착석
            if (Mover != null) { Mover.MoveTo(seat.WorldPos); walkingToSeat = true; }
            state = CatState.Sit;
        }

        void DoSit()
        {
            // 이동 중이면 도착 대기
            if (walkingToSeat && Mover != null && !Mover.HasArrived) return;
            walkingToSeat = false;
            state = CatState.DecideOrder;
        }

        void DoOrder()
        {
            var order = OrderDecider.Choose(data, GetAvailableMenus(), mvpMode: true);
            ServedMenu = order.primary;
            ServedFavorite = order.wasFavorite;
            ForcedSecond = order.wasForcedSecond;
            if (order.primary != null)
                Obs?.RecordClue(data.catId, ClueType.FoodEat, order.primary.menuId,
                                isFallback: order.wasForcedSecond,
                                order.wasFavorite ? $"{data.displayNameKey}가 맛있게 먹는다." : null);

            // [S2] OrderSystem 있으면 티켓 등록 후 서빙 대기, 없으면 S1 직행
            if (OrderSystem != null && order.primary != null)
            {
                currentTicket = OrderSystem.Place(data.catId, order.primary);
                state = CatState.WaitFood;
            }
            else state = CatState.Eat;
        }

        YeogiCafe.Order.OrderTicket currentTicket;

        void DoWaitFood()
        {
            if (currentTicket == null) { state = CatState.Eat; return; }
            if (currentTicket.state == YeogiCafe.Order.TicketState.Served) { state = CatState.Eat; return; }
            if (currentTicket.state == YeogiCafe.Order.TicketState.Cancelled)
            {
                // 인내심 소진 → 불만 퇴장 (만족도·매출 페널티는 세션결과 orderFailed로)
                ServedMenu = null;
                if (seat != null) { SetOccupant?.Invoke(seat, null); seat.Vacate(); seat = null; }
                state = CatState.Leave;
            }
        }

        void DoEat() { decisionTimer = 0; state = CatState.Idle; }

        void DoActionLoop()
        {
            sitTimer += Time.deltaTime; decisionTimer += Time.deltaTime;
            if (sitTimer >= sitDuration) { state = CatState.Pay; return; }
            if (decisionTimer < DecisionInterval) return;
            decisionTimer = 0;

            var dec = FacilityDecider.Tick(data, GetFacilities(),
                        f => recentUse.TryGetValue(f.FacilityId, out var v) ? v : 0f, cfg);
            if (!dec.isIdle && dec.facility != null)
            {
                recentUse[dec.facility.FacilityId] = 0.3f; // 반복 억제
                bool pref = SharesFacilityPref(dec.facility);
                if (pref) UsedPreferredFacility = true;
                Obs?.RecordClue(data.catId, ClueType.FacilityUse, FacilityKey(dec.facility),
                                isFallback: !pref,
                                Narrate(NarrationCatalog.FacilityKey(dec.facility.Tags)));
            }
            // recentUse 감쇠
            var keys = new List<string>(recentUse.Keys);
            foreach (var k in keys) recentUse[k] = Mathf.Max(0, recentUse[k] - 0.05f);
        }

        void DoPay()
        {
            if (seat != null) { SetOccupant?.Invoke(seat, null); seat.Vacate(); seat = null; } // 자리 반납(회전)
            state = CatState.Leave; /* 만족도·결제·세션확정은 매니저가 OnLeave에서 */
        }

        // ── helpers ──
        // 로컬라이즈 서술 + {name} 치환. L 미로드 상황(테스트)에서도 안전.
        string Narrate(string key)
        {
            string catName = YeogiCafe.Loc.L.Get($"cat.{data.catId}.name");
            return YeogiCafe.Loc.L.Get(key, ("name", catName));
        }
        float BaseSit() => 30f;
        float PersonalityFactor() => data.HasPersonality(PersonalityTag.Relaxed) ? 1.5f : 1.0f;
        static string SeatKey(ISeat s) => s.Tags.Count > 0 ? s.Tags[0].ToString() : "Normal";
        static string FacilityKey(IFacility f) => f.Tags.Count > 0 ? f.Tags[0].ToString() : "Facility";
        static bool HasTag(ISeat s, SeatTag t) { foreach (var x in s.Tags) if (x == t) return true; return false; }
        bool SharesFacilityPref(IFacility f) { foreach (var x in f.Tags) if (x == data.facilityPreference) return true; return false; }
    }
}
