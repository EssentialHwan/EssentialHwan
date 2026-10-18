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
        // [프리플라이트] 단골 고정자리(favoriteSeat)는 MVP 제거 → 포스트 MVP. RegularStage/FavoriteSeatId 삭제.

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
        public CatState State => state;
        // 이번 방문 중 쓰다듬기 기분(표현 레이어가 기록, 만족도에 반영). ±SessionMoodClamp
        public int TouchMood { get; private set; }
        public void AddTouchMood(int d) => TouchMood = UnityEngine.Mathf.Clamp(TouchMood + d, -CatTemperament.SessionMoodClamp, CatTemperament.SessionMoodClamp);
        public ISeat CurrentSeat => seat;                                   // 표현(CatView)용 읽기 전용
        public Vector3 CurrentSeatPos => seat != null ? seat.WorldPos : transform.position;
        public System.Action<IFacility> OnFacilityUse;                      // 표현용: 시설 이용 순간
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

        // ── 대장냥이 자리 뺏기 ──
        public System.Func<IReadOnlyList<CatBrain>> GetOthers;                // 매니저 주입
        public static System.Action<CatBrain, CatBrain> OnSeatStolen;         // (대장, 쫓겨난 고양이) — 표현용
        CatBrain stealFrom;
        float fleeDelay = -1f;

        // 밥을 다 먹고 자기 자리에 앉아 쉬는(그루밍 등) 고양이 중 하나를 고른다. 선호 좌석이면 우선.
        ISeat TrySteal()
        {
            if (GetOthers == null || !CatTemperament.IsBoss(data) || Random.value > CatTemperament.BossStealChance) return null;
            CatBrain pick = null; bool pickPref = false;
            foreach (var o in GetOthers())
            {
                if (o == null || o == this || o.seat == null || o.state != CatState.Idle || !o.AtSeat) continue;
                if (o.FacilityPhase != FacPhase.None || CatTemperament.IsBoss(o.data)) continue;
                bool pref = HasTag(o.seat, data.seatPreference);
                if (pick == null || (pref && !pickPref)) { pick = o; pickPref = pref; }
            }
            if (pick == null) return null;
            var s = pick.seat;
            pick.LoseSeat();
            stealFrom = pick;
            return s;
        }

        void LoseSeat()
        {
            if (seat == null) return;
            SetOccupant?.Invoke(seat, null); seat.Vacate(); lostSeat = seat; seat = null;
        }

        // 대장이 도착해 하악하면 잠시 뒤 슬금슬금 다른 데로
        public void Flee(float delay) { fleeDelay = delay; }

        void DoFindSeat()
        {
            var stolen = TrySteal();
            if (stolen != null)
            {
                seat = stolen; seatFallback = !HasTag(stolen, data.seatPreference);
                seat.Occupy(this);
                SetOccupant?.Invoke(seat, data.catId);
                UsedPreferredSeat = !seatFallback;
                Obs?.RecordClue(data.catId, ClueType.SeatUse, SeatKey(seat), seatFallback,
                                Narrate(NarrationCatalog.SeatKey(seat.Tags, seatFallback)));
                sitDuration = Mathf.Min(BaseSit() * PersonalityFactor(), cfg.maxSitDuration);
                sitTimer = 0;
                if (Mover != null) { Mover.MoveTo(seat.WorldPos); walkingToSeat = true; }
                state = CatState.Sit;
                return;
            }
            var choice = SeatSelector.Choose(data, GetSeats(), Entrance, NearbyFriends, NearbyStrangers, cfg);
            if (choice.seat == null)
            {
                // 만석 → 성격분기 (부록 J-B)
                state = FullCafePolicy.DecideWhenFull(data);
                return;
            }
            seat = choice.seat; seatFallback = choice.isFallback;
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
            if (stealFrom != null)
            {
                if (stealFrom != null && !stealFrom.IsFinished) { OnSeatStolen?.Invoke(this, stealFrom); stealFrom.Flee(1.5f); }
                stealFrom = null;
            }
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
                                // 최애면 '순식간에 비움', 아니면 '천천히' — 플레이어가 차이를 읽을 수 있는 서술(수치 노출 없음)
                                Narrate(order.wasFavorite ? "narr.food.fast" : "narr.food.slow"));

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

        void DoEat() { decisionTimer = 0; NewRest(); state = CatState.Idle; }

        // ── 식사 후 자유 시간: 쉬기 / 아무 데나 돌아다니기 / 시설 이용(걸어가 올라가서 한참 쉬다 내려옴) / 자리로 돌아가기 ──
        public enum FacPhase { None, Approach, Perched, Dismount }
        public FacPhase FacilityPhase { get; private set; }
        public IFacility CurrentFacility { get; private set; }
        public float FacilityPerch => CurrentFacility is IFacilitySpot sp ? sp.PerchHeight : 0f;
        public bool FacilityWall => CurrentFacility is IFacilitySpot sp && sp.WallMounted;
        ISeat lostSeat;                                                     // 대장에게 뺏긴 자리(쫓겨날 때까지 그 위에 앉아 있는 모습 유지)
        public ISeat VisualSeat => seat ?? lostSeat;
        public bool AtSeat => VisualSeat != null && (transform.position - VisualSeat.WorldPos).sqrMagnitude < 0.03f;
        public bool IsMoving => Mover != null && !Mover.HasArrived;
        public System.Func<Vector3> RandomFloorPoint;                      // 매니저 주입(없으면 돌아다니지 않음)

        float restTimer, restDuration = 6f, facTimer, facDuration;

        // 우다다: 갑자기 카페를 몇 군데 전력질주
        readonly Queue<Vector3> zoomPath = new();
        public bool IsZooming { get; private set; }
        float baseSpeed = -1f;

        public void StartZoomies()
        {
            if (RandomFloorPoint == null || Mover == null) return;
            zoomPath.Clear();
            int n = Random.Range(3, 6);
            for (int i = 0; i < n; i++) zoomPath.Enqueue(RandomFloorPoint());
            IsZooming = true;
            if (Mover is SimpleLerpMover m) { if (baseSpeed < 0f) baseSpeed = m.speed; m.speed = baseSpeed * 3.2f; }
            lostSeat = null;
            Mover.MoveTo(zoomPath.Dequeue());
        }

        void TickZoomies()
        {
            if (IsMoving) return;
            if (zoomPath.Count > 0) { Mover.MoveTo(zoomPath.Dequeue()); return; }
            IsZooming = false;
            if (Mover is SimpleLerpMover m && baseSpeed > 0f) m.speed = baseSpeed;
            NewRest();
        }
        const float DismountTime = 0.55f;

        void DoActionLoop()
        {
            float dt = Time.deltaTime;
            sitTimer += dt;

            // 시설 이용 중
            if (FacilityPhase != FacPhase.None) { TickFacility(dt); return; }
            if (fleeDelay >= 0f)
            {
                fleeDelay -= dt;
                if (fleeDelay < 0f && RandomFloorPoint != null) GoTo(RandomFloorPoint());
                return;
            }

            if (IsZooming) { TickZoomies(); return; }
            if (sitTimer >= sitDuration) { state = CatState.Pay; return; }
            if (IsMoving) return;                                            // 걷는 중
            state = CatState.Idle;
            restTimer += dt; decisionTimer += dt;
            if (restTimer < restDuration || decisionTimer < DecisionInterval) return;
            decisionTimer = 0;

            // ① 시설: 선호·성격 기반 확률(사양 §2.5). 한 번 쓰면 오래(≈1분) 다시 안 감 → 왔다갔다 반복 방지
            var dec = FacilityDecider.Tick(data, GetFacilities(),
                        f => recentUse.TryGetValue(f.FacilityId, out var v) ? v : 0f, cfg);
            DecayRecentUse();
            if (!dec.isIdle && dec.facility != null && dec.facility.IsFree)
            {
                StartFacility(dec.facility);
                return;
            }

            // ② 돌아다니기 / 자리로 돌아가기 / 그대로 쉬기
            float roam = 0.35f;
            if (data.HasPersonality(PersonalityTag.Active) || data.HasPersonality(PersonalityTag.Playful)) roam += 0.25f;
            if (data.HasPersonality(PersonalityTag.Curious)) roam += 0.15f;
            if (data.HasPersonality(PersonalityTag.Relaxed) || data.HasPersonality(PersonalityTag.Introvert)) roam -= 0.2f;
            if (Random.value < CatTemperament.ZoomiesChance(data)) { StartZoomies(); return; }   // 우다다!
            float r = Random.value;
            if (r < roam && RandomFloorPoint != null) GoTo(RandomFloorPoint());
            else if (r < roam + 0.2f && seat != null && !AtSeat) GoTo(seat.WorldPos);
            else NewRest();
        }

        void NewRest()
        {
            restTimer = 0f;
            float k = data.HasPersonality(PersonalityTag.Relaxed) ? 1.6f : data.HasPersonality(PersonalityTag.Active) ? 0.7f : 1f;
            restDuration = Random.Range(7f, 14f) * k;
        }

        void GoTo(Vector3 p)
        {
            lostSeat = null;
            if (Mover != null) Mover.MoveTo(p);
            NewRest();
        }

        // 다른 고양이에게 다가가기(표현 레이어의 상호작용 연출이 요청). 쉬는 중일 때만.
        public bool RequestApproach(Vector3 p)
        {
            if (IsZooming) return false;
            if (state != CatState.Idle || FacilityPhase != FacPhase.None || IsMoving || Mover == null) return false;
            GoTo(p);
            restDuration = Mathf.Max(restDuration, 6f);
            return true;
        }

        void StartFacility(IFacility f)
        {
            recentUse[f.FacilityId] = 1.0f;                                  // 감쇠 0.05/결정 → 약 1분간 같은 시설 회피
            CurrentFacility = f;
            if (f is IFacilitySpot sp) sp.Use(this);
            state = CatState.FacilityAction;
            FacilityPhase = FacPhase.Approach;
            Vector3 dest = f is IFacilitySpot spot ? spot.BaseWorldPos : (f is Component c ? c.transform.position : transform.position);
            if (Mover != null) Mover.MoveTo(dest);

            OnFacilityUse?.Invoke(f);
        }

        // 시설에 도착한 순간 기록(서술 말풍선이 실제로 올라앉을 때 뜨게)
        void RecordFacilityClue(IFacility f)
        {
            bool pref = SharesFacilityPref(f);
            if (pref) UsedPreferredFacility = true;
            Obs?.RecordClue(data.catId, ClueType.FacilityUse, FacilityKey(f),
                            isFallback: !pref,
                            Narrate(NarrationCatalog.FacilityKey(f.Tags)));
        }

        void TickFacility(float dt)
        {
            switch (FacilityPhase)
            {
                case FacPhase.Approach:
                    if (IsMoving) return;
                    FacilityPhase = FacPhase.Perched; facTimer = 0f;
                    RecordFacilityClue(CurrentFacility);
                    // 선호 시설이면 더 오래, 느긋한 고양이는 더 오래
                    facDuration = Random.Range(9f, 15f) * (SharesFacilityPref(CurrentFacility) ? 1.4f : 1f)
                                  * (data.HasPersonality(PersonalityTag.Relaxed) ? 1.3f : 1f);
                    break;
                case FacPhase.Perched:
                    facTimer += dt;
                    if (facTimer >= facDuration || sitTimer >= sitDuration) { FacilityPhase = FacPhase.Dismount; facTimer = 0f; }
                    break;
                case FacPhase.Dismount:
                    facTimer += dt;
                    if (facTimer < (FacilityPerch > 0.05f ? DismountTime : 0.1f)) return;
                    EndFacility();
                    state = CatState.Idle;
                    decisionTimer = 0f;
                    // 내려와서 바로 근처를 좀 걷다가 쉰다(또는 자리로)
                    if (seat != null && Random.value < 0.5f) GoTo(seat.WorldPos);
                    else if (RandomFloorPoint != null) GoTo(RandomFloorPoint());
                    else NewRest();
                    break;
            }
        }

        void EndFacility()
        {
            if (CurrentFacility is IFacilitySpot sp) sp.Release();
            CurrentFacility = null;
            FacilityPhase = FacPhase.None;
        }

        void DecayRecentUse()
        {
            var keys = new List<string>(recentUse.Keys);
            foreach (var k in keys) recentUse[k] = Mathf.Max(0, recentUse[k] - 0.05f);
        }

        void DoPay()
        {
            if (IsZooming) { IsZooming = false; zoomPath.Clear(); if (Mover is SimpleLerpMover m && baseSpeed > 0f) m.speed = baseSpeed; }
            EndFacility();
            if (seat != null) { SetOccupant?.Invoke(seat, null); seat.Vacate(); seat = null; } // 자리 반납(회전)
            state = CatState.Leave; /* 만족도·결제·세션확정은 매니저가 OnLeave에서 */
        }

        // 영업 종료 시 남은 손님 정리: 자리를 반납하고 세션을 즉시 확정(결제·만족 파이프라인 1회 실행).
        public void ForceLeave()
        {
            if (sessionCompleted) return;
            EndFacility();
            if (IsZooming) { IsZooming = false; zoomPath.Clear(); if (Mover is SimpleLerpMover m && baseSpeed > 0f) m.speed = baseSpeed; }
            if (seat != null) { SetOccupant?.Invoke(seat, null); seat.Vacate(); seat = null; }
            if (OrderSystem != null) OrderSystem.DropForCat(data.catId);
            if (state == CatState.WaitFood) ServedMenu = null;   // 못 받고 떠나면 결제 없음
            state = CatState.Leave;
            sessionCompleted = true;
            OnSessionComplete?.Invoke(this);
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
