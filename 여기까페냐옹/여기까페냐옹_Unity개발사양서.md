# 《여기까페냐옹》 Unity 개발 사양서 (Technical Design Spec)

> **출처**: `여기까페냐옹_게임기획서.md`(검증·확정본, P1~P10 반영 + 부록 J A~J 확정)를 개발용 사양으로 변환.
> **엔진**: Unity (C#) / PC·Steam / 싱글플레이 / Input System 사용.
> **아키텍처 원칙**: Data(SO) ↔ Logic(Manager) ↔ View(UI). 이벤트 채널로 디커플링. 태그 기반, 하드코딩 분기 금지.
> **각 시스템 서술 포맷(요청 13항목)**: ①목적 ②입력 데이터 ③내부 데이터 ④처리 순서 ⑤상태 ⑥조건 ⑦출력 ⑧UI ⑨저장 데이터 ⑩예외 처리 ⑪개발 난이도 ⑫의존관계 ⑬Unity 구현 방식 ⑭테스트 케이스.

---

## 0. 공통 규약 · 타입 정의

### 0.1 핵심 열거형·태그

```csharp
public enum PersonalityTag { Relaxed, Curious, Social, Lonely, Active, Introvert, Glutton, Playful, Fickle }
public enum SeatTag        { Normal, Window, Corner, TwoSeat, Sofa, BarTable, Quiet, OutsideView }
public enum FacilityTag    { Play, Height, Active, Soft, Relax, Quiet, Social }
public enum FoodTag        { Fish, Sweet, Fruit, Meal, Warm }
public enum AtmosphereAxis { Quiet, Lively, Warm, Natural, Luxury }   // 조용/활기/따뜻/자연/고급

public enum PrefAxis       { Food, Seat, Facility }                  // 관찰 3축(P2: 분위기 제외)
public enum ClueType       { SeatUse, FacilityUse, FoodEat, SocialAction, IdleGaze }
public enum ObsState       { Unknown, Suspected, Confirmable, Recorded } // P4 4단계
```

### 0.2 Truth vs Observed 분리 (문서 최상위 불변식)

| 구분 | 클래스 | 저장 | 접근 주체 |
|---|---|---|---|
| Truth(숨김) | `CatData`(SO) | ❌ (원본 데이터) | 게임 로직만 |
| Observed(공개) | `ObservationRecord` | ✅ 세이브 | UI·도감·로직 |

**규칙**: UI 레이어는 `CatData`의 취향 필드 접근 금지. 오직 `ObservationRecord`만 읽는다.

### 0.3 이벤트 채널 (ScriptableObject Event)

```
OnPhaseChanged(DayPhase)         OnCatSpawned(CatInstance)     OnCatSeated(CatInstance, Seat)
OnOrderPlaced(OrderTicket)       OnFoodServed(OrderTicket)     OnCatPaid(CatInstance, int gold)
OnCatLeft(CatInstance, float satisfaction)                     OnClueRecorded(catId, Clue)
OnFurniturePlaced/Removed(Furniture)                           OnAtmosphereRecalculated(AtmoSnapshot)
OnDayEnded(DayResult)            OnPreferenceRecorded(catId, PrefAxis)  OnRegularStageUp(catId, int stage)
```

### 0.4 밸런스 상수 (`BalanceConfig` SO — 전부 초기 가정값)

```csharp
[CreateAssetMenu] public class BalanceConfig : ScriptableObject {
  public int   startGold = 300;
  public float dayLength = 720f;                 // 12분
  public Vector3 phaseSeconds = new(180,240,300);// 3:4:5분 (부록 J-A)
  public float spawnInterval_Morning=20, spawnInterval_Lunch=12, spawnInterval_Evening=9;
  // 좌석 선택
  public int  pref_exact=100, pref_partial=40, pref_none=10;
  public int  personality_bonus=30, atmosphere_seatbonus=20, social_friend=40;
  public float distancePenaltyPerUnit=0.5f; public int seatRandom=10;
  // 관찰
  public float clue_strong=3f, clue_medium=1.5f, clue_fallback=0.5f, fallbackCap=2f; // P1
  public float thr_suspected=3f, thr_confirmable=7f; public int leadGap=3;           // P4
  public float refute_threshold=5f;              // 강한 반증 자동 강등(부록 J-F)
  // 만족도
  public int sat_base=50, food_fav=25, food_tag=15, food_forced=-5;
  public int seat_pref=15, facility_pref=15, service_fast=5, orderFail=-10;
  public float atmo_match=1.1f, atmo_neutral=1.0f, atmo_opposite=0.9f; // MVP=1.0
  public float waitPenaltyPerSec=0.5f;
  // 재방문/단골
  public float revisit_base=0.2f, revisit_satW=0.5f, revisit_fav=0.1f, revisit_seat=0.1f, revisit_decay=0.02f;
  public float maxSitDuration=90f;               // P7
  public int[] regularVisitReq = {0,3,6,10,15};
}
```

---
## 1. 데이터 계층 (ScriptableObject + 세이브 DTO)

### 1.1 CatData (Truth, SO)

```csharp
[CreateAssetMenu(menuName="Cat/CatData")]
public class CatData : ScriptableObject {
  public string catId;                 // "cat_cheese"
  public string displayNameKey;        // 로컬라이즈 키
  public Sprite portrait; public string species;
  public PersonalityTag[] personalityTags;   // [0]=즉시공개, [1]=관찰공개 (부록 J-C)
  // ── Truth(숨김, 세이브 금지) ──
  public string foodFavoriteMenuId;          // 최애 메뉴
  public FoodTag[] foodPreferredTags;        // 계열(정식 판정, P10)
  public SeatTag seatPreference;
  public FacilityTag facilityPreference;
  public AtmosphereAxis atmospherePreference;
  // ── 행동/진행 ──
  public BehaviorWeight behaviorWeight;
  public float spawnWeight = 1f;
  public UnlockCondition unlockCondition;
  public string[] relationshipHintCatIds;
  public CatEventData[] eventList;
}
```

### 1.2 FurnitureData / MenuData (SO)

```csharp
[CreateAssetMenu(menuName="Cafe/FurnitureData")]
public class FurnitureData : ScriptableObject {
  public string furnitureId; public string displayNameKey;
  public FurnitureType type;              // Seat / Facility / Decoration
  public SeatTag[] seatTags;              // type==Seat
  public FacilityTag[] facilityTags;      // type==Facility
  public FacilityAction[] actions;
  public AtmoContribution[] atmosphere;   // (axis,value)[]
  public int price; public Vector2Int size;
  public UnlockCondition unlockCondition;
}

[CreateAssetMenu(menuName="Cafe/MenuData")]
public class MenuData : ScriptableObject {
  public string menuId; public string displayNameKey;
  public int price, cost; public float cookTime;
  public FoodTag[] foodTags; public int satiety, satisfaction;
  public UnlockCondition unlockCondition;
}
```

### 1.3 세이브 DTO (JSON 직렬화, `SaveManager`)

```csharp
[Serializable] public class SaveGame {
  public int version = 1;
  public int gold; public int currentDay; public int cafeLevel;
  public List<PlacedFurniture> placedFurniture;   // furnitureId + gridPos
  public List<string> unlockedMenuIds;
  public List<CatSaveData> cats;
  public List<RelationSaveData> relations;         // 정식
  public List<string> completedEventIds;
}

[Serializable] public class CatSaveData {
  public string catId;
  public int visitCount; public int lastVisitDay;
  public List<float> satisfactionHistory;          // 단일 소스(P5), 최근 5
  public int regularStage;
  public bool personality2Revealed;                // 부록 J-C
  public ObservationRecord observation;            // 발견한 취향(필수 저장)
}

[Serializable] public class ObservationRecord {    // Truth 미포함!
  public AxisRecord food, seat, facility;          // 3축(P2)
}
[Serializable] public class AxisRecord {
  public ObsState state = ObsState.Unknown;        // ?/…/★/✓
  public List<ClueTally> clueWeights;              // targetTagOrId → 누적 weight(cap 반영)
  public string recordedValue;                     // Recorded 시 확정된 태그/메뉴
}
```

**저장 규칙**: `satisfactionHistory` 단일 소스 → `last=Last()`, `avg=Average()` 파생. Truth는 어떤 DTO에도 없음(QA로 검증).

---
## 2. 고양이 행동 AI (CatBrain) — 상세 사양 + 의사코드

### 2.1 13항목 사양

| # | 항목 | 내용 |
|---|---|---|
| ① | 목적 | 성격·환경에 따라 자율 행동하여 **관찰 단서(Clue)** 를 생성. 취향 대상이 없으면 fallback. |
| ② | 입력 데이터 | `CatData`(Truth), `BalanceConfig`, 카페 상태(빈 좌석/시설 목록), 다른 `CatInstance` 목록, `AtmosphereSnapshot`, 현재 메뉴, `DayPhase` |
| ③ | 내부 데이터 | `CatInstance{ chosenSeat, currentState, sitTimer, sitDuration, orderTicket, lastAction, sessionLog:List<Clue> }` |
| ④ | 처리 순서 | HFSM 전이(2.2) + 분기 시 Utility 점수(2.3~2.6) |
| ⑤ | 상태 | Enter, FindSeat, Sit, DecideOrder, Order, WaitFood, Eat, {FacilityAction, Idle}(MVP) + {SocialAction, MoveSeat}(정식), Pay, Leave |
| ⑥ | 조건 | 상태 전이 조건은 2.2 표. `maxSitDuration` 도달 시 강제 Pay(P7) |
| ⑦ | 출력 | `RecordClue()` 호출, `OnCatSeated/OnOrderPlaced/OnCatPaid/OnCatLeft` 이벤트, 최종 satisfaction |
| ⑧ | UI | 머리 위 말풍선(주문/행동), 인내심 게이지(WaitFood), 서술 이펙트("창밖을 오래 본다") |
| ⑨ | 저장 | 직접 저장 없음. Leave 시 세션 결과를 `ObservationManager`/`RegularManager`/`Economy`로 전달 → 각자 저장 |
| ⑩ | 예외 | 좌석 없음→성격별 분기(J-B); 서빙 지연→불만 Leave; 이용 중 가구 삭제→상태 안전 종료 후 FindSeat 재진입 |
| ⑪ | 난이도 | ★★★★ (최고. HFSM+점수식+관찰 훅, 버그 온상) |
| ⑫ | 의존 | 좌석/시설/메뉴/분위기/관찰/관계/경제 전부와 연결(허브) |
| ⑬ | 구현 | `MonoBehaviour CatBrain` + HFSM(상태 클래스 or enum+switch). `NavMeshAgent` 이동. 결정 tick 3s. |
| ⑭ | 테스트 | 2.8 |

### 2.2 HFSM 전이표 (MVP)

```
Enter      ── 좌석후보 수집 ──▶ FindSeat
FindSeat   ── 좌석확정 ──▶ Sit        | ── 후보없음 ──▶ (J-B 성격분기) WaitOutside/LeaveDisappointed
Sit        ── sitDuration 설정 ──▶ DecideOrder
DecideOrder── 메뉴선택 ──▶ Order
Order      ── 티켓등록 ──▶ WaitFood
WaitFood   ── 서빙완료 ──▶ Eat        | ── 인내심0 ──▶ Leave(불만)
Eat        ── 식사완료 ──▶ ActionLoop
ActionLoop ── 매 tick ActionScore 최고 실행: FacilityAction | Idle
           ── sitTimer ≥ min(sitDuration, maxSitDuration) ──▶ Pay
Pay        ── 결제완료 ──▶ Leave
Leave      ── 세션확정·디스폰 ──▶ (end)
```

### 2.3 좌석 선택 — `SeatSelector.Evaluate` (의사코드)

```
function ChooseSeat(cat, cafe):
    candidates = cafe.Seats.Where(s => s.IsFree)          // 사용중 제외
    if candidates.isEmpty:
        return FALLBACK_NO_SEAT                            // → J-B 성격분기
    best = null; bestScore = -inf
    for seat in candidates:
        score = SeatSelectionScore(cat, seat, cafe)
        if score > bestScore: bestScore = score; best = seat
    return best

function SeatSelectionScore(cat, seat, cafe):
    # --- Preference (Truth 사용, 관찰과 무관) ---
    if seat.tags.Contains(cat.Truth.seatPreference):  pref = cfg.pref_exact      # +100
    elif seat.tags.IntersectsPartial(cat.Truth):      pref = cfg.pref_partial    # +40
    else:                                             pref = cfg.pref_none       # +10
    # --- Personality ---
    persona = 0
    if cat.Has(Introvert) and seat.tags.Contains(Corner|Quiet): persona += cfg.personality_bonus
    if cat.Has(Social)    and seat.IsCentral:                   persona += 20
    # --- Atmosphere (MVP: 생략 가능) ---
    atmo = seat.LocalDominantAxis == cat.Truth.atmospherePreference ? cfg.atmosphere_seatbonus : 0
    # --- Social ---
    social = 0
    foreach other in cafe.SeatedCats near seat:
        if cat.IsFriend(other): social += cfg.social_friend                      # +40
        elif cat.Has(Introvert): social -= 20
        elif cat.Has(Social):    social += 15
    # --- Distance & Random ---
    dist = Distance(cafe.Entrance, seat) * cfg.distancePenaltyPerUnit
    rnd  = Random(-cfg.seatRandom, +cfg.seatRandom)
    return pref + persona + atmo + social - dist + rnd
```

> **불변식**: `pref_exact(100)`가 성격·랜덤 합(≈±60)을 항상 압도 → 취향 신호가 노이즈에 묻히지 않음(추리 가능성 보장). **fallback은 여기서 "선호 좌석이 후보에 없음"으로 자연 발생**(별도 코드 아님) → 무관 좌석이 pref_none으로만 뽑힘.

### 2.4 음식 선택 — `OrderDecider.Choose` (의사코드)

```
function ChooseMenu(cat, availableMenus):
    if availableMenus.isEmpty: return null                # 예외: 전 품절
    best=null; bestScore=-inf
    for m in availableMenus:                              # 해금 & 판매중만
        s = 0
        if m.menuId == cat.Truth.foodFavoriteMenuId: s += 100           # 최애
        elif MVP==false and m.foodTags.Intersects(cat.Truth.foodPreferredTags): s += 40  # 계열(정식, P10)
        if cat.Has(Glutton) and m.foodTags.Contains(Meal): s += 20
        s += Random(-10,10)
        if s > bestScore: bestScore=s; best=m
    # 식탐: 추가 주문 확률
    order = { primary: best }
    if cat.Has(Glutton) and Random01() < 0.4: order.extra = ChooseSecond(cat, availableMenus)
    return order
```

### 2.5 시설 선택 — `ActionScore` 내 FacilityUseChance (의사코드)

```
function ActionLoopTick(cat, cafe):                       # Eat 이후 매 3s
    actions = []
    # Idle 항상 후보
    actions.add( Score(Idle) = cfg.base_idle - repeatPenalty(Idle) + Random )
    # 각 이용가능 시설
    for f in cafe.Facilities.Where(f => f.IsFree):
        chance = FacilityUseChance(cat, f)
        actions.add( Score(UseFacility(f)) = chance*100
                     + envBonus(f) + personaBonus(cat,f)
                     - repeatPenalty(f) + Random )
    # (정식) SocialAction 후보: 친구 인접 시
    chosen = actions.MaxBy(a => a.score)
    Execute(chosen)                                        # → RecordClue

function FacilityUseChance(cat, f):
    p = 0.15                                               # base
    if f.tags.Intersects(cat.Truth.facilityPreference): p += 0.5   # 선호 시설
    if cat.Has(Curious):                    p += 0.3
    if cat.Has(Playful) and f.tags.Has(Play): p += 0.3
    p -= recentUsePenalty(cat, f)
    return clamp01(p)
```

> **관찰 채널 규칙**: `cafe.Facilities`에 선호 시설이 **없으면** 그 시설 행동 자체가 후보에 없어 **Clue 미생성**(도감 '?' 유지). 설치되어야 관찰 가능(기획 15장).

### 2.6 다른 고양이 상호작용 — SocialAction (정식, 의사코드)

```
function SocialTick(cat, cafe):                            # SocialAction 진입 시
    target = cafe.SeatedCats.Near(cat).MaxBy(o => RelationScore(cat,o) + (cat.IsFriend(o)?50:0))
    if target == null: return Idle
    PlayInteraction(cat, target)                           # 대화/함께놀기 애니
    RelationshipManager.AddPairScore(cat.id, target.id, +delta, dailyCap=1)  # 일 1회 상한
    RecordClue(cat.id, SocialAction, target.id, weight=medium)
    if cat.Has(Lonely): cat.session.socialSatisfied = true
```
## 3. 연결된 데이터 흐름 — 관찰 → 기록 → 만족 → 재방문 → 단골 (하나의 파이프라인)

> 요청: 좌석/시설/음식/상호작용/fallback/관찰기록/만족도/재방문/단골까지 **끊김 없는 흐름**으로. 아래는 `CatBrain.Leave` 시점을 기준으로 한 실행 순서 의사코드.

### 3.1 행동 중 — Clue 기록 (실시간)

```
# 행동이 일어날 때마다 호출 (2.3~2.6에서 Execute 직후)
function RecordClue(catId, type, target, isFallback):
    weight = isFallback ? cfg.clue_fallback           # 0.5
           : (type==FoodEat && ate.fast && ate.finished) ? cfg.clue_strong  # 3
           : cfg.clue_medium                                                 # 1.5
    cat.session.log.add(Clue{type, target, weight, phase})
    # 실시간 서술 이펙트(수치 숨김)
    UI.ShowBehaviorBubble(catId, Narrate(type, target))   # "창밖을 오래 바라본다"
```

### 3.2 퇴장(Leave) — 만족도 확정 → 세션 반영

```
function OnLeave(cat):
    sat = ComputeSatisfaction(cat)                     # 3.3
    gold = ComputePayment(cat, sat)                    # 3.4
    Economy.AddGold(gold); Day.AccumulateRevenue(gold, cat.orderCost)
    # 세션 로그를 '오늘의 관찰 버퍼'로 이관 (아직 영속 아님)
    ObservationManager.StageSessionClues(cat.id, cat.session.log)
    cat.save.satisfactionHistory.Push(sat, maxLen=5)   # 단일 소스(P5)
    cat.save.visitCount += 1; cat.save.lastVisitDay = Day.current
    Emit OnCatLeft(cat, sat)
    Despawn(cat)
```

### 3.3 만족도 계산 — `SatisfactionCalculator` (의사코드, P2·P6 반영)

```
function ComputeSatisfaction(cat):
    s = cfg.sat_base                                   # 50
    # 음식
    if cat.served == cat.Truth.foodFavoriteMenuId: s += cfg.food_fav      # +25
    elif !MVP and served.tags ∩ cat.Truth.foodPreferredTags: s += cfg.food_tag
    elif cat.wasForcedSecondChoice: s += cfg.food_forced                   # -5
    # 분위기 승수 (MVP=1.0)
    atmoMult = MVP ? 1.0 :
        (cafe.DominantAxis==cat.Truth.atmospherePreference ? cfg.atmo_match
         : cafe.DominantAxis==Opposite(cat.Truth) ? cfg.atmo_opposite : 1.0)
    # 좌석/시설 (승수 적용)
    seatBonus = cat.usedSeat.tags.Contains(cat.Truth.seatPreference) ? cfg.seat_pref : 0
    facBonus  = cat.usedPreferredFacility ? cfg.facility_pref : 0
    s += seatBonus * atmoMult + facBonus * atmoMult
    # 사회성
    if cat.Has(Lonely): s += cat.session.socialSatisfied ? +10 : -10
    if cat.Has(Social) and cat.session.satWithFriend:  s += 10
    # 서비스/페널티
    if cat.serveWaitTime < fastThreshold: s += cfg.service_fast
    s -= cat.session.totalWaitOver * cfg.waitPenaltyPerSec
    if cat.orderFailed: s += cfg.orderFail
    return clamp(s, 0, 100)
```

### 3.4 결제 (의사코드)

```
function ComputePayment(cat, sat):
    tip = sat<40 ? 0.8 : sat<70 ? 1.0 : sat<90 ? 1.15 : 1.3
    return round(cat.orderTotalPrice * tip)
```

### 3.5 영업 종료 — 관찰 정리 → 추론 승격 (의사코드, P1·P4)

```
function OnDayEnd():
    for catId in ObservationManager.stagedToday.keys:
        rec = save[catId].observation
        for clue in staged[catId]:
            axis = AxisOf(clue.type)                    # Food/Seat/Facility
            tally = rec[axis].clueWeights[clue.target]
            add = clue.weight
            if clue.isFallback:                          # P1: fallback cap 2
                add = min(add, cfg.fallbackCap - tally.fallbackSum)
                tally.fallbackSum += add
            tally.total += max(0, add)
        # 축별 상태 승격 (시스템 자동, P4)
        for axis in [Food,Seat,Facility]:
            top = rec[axis].clueWeights.MaxBy(t=>t.total)
            second = rec[axis].clueWeights.SecondMax()
            if rec[axis].state == Recorded: continue     # 이미 확정
            if top.total >= cfg.thr_confirmable and (top.total - second.total) >= cfg.leadGap:
                rec[axis].state = Confirmable            # ★ "기록 가능"
            elif top.total >= cfg.thr_suspected:
                rec[axis].state = Suspected              # …
            # 강한 반증 시 Recorded→Suspected 강등 (부록 J-F)
            if rec[axis].state==Recorded and Refutation(axis) >= cfg.refute_threshold:
                rec[axis].state = Suspected
    ShowSettlementScreen()                               # 매출 + 관찰 카드
```

> **fallback cap 효과**: 창가석 없이 일반석만 앉으면 seat축 Normal 태그 `fallbackSum`이 2에서 멈춤 → `thr_suspected(3)` 미달 → **가짜 "일반석 선호" 가설 안 생김**(P1). 정상(창가석 존재) 단서는 cap 없이 승격.

### 3.6 취향 기록 — 플레이어 능동 확정 (의사코드, P4·P9)

```
function OnPlayerClickRecord(catId, axis):              # Confirmable(★)에서만 버튼 활성
    rec = save[catId].observation[axis]
    assert rec.state == Confirmable
    rec.state = Recorded                                # ✓
    rec.recordedValue = rec.clueWeights.MaxBy(t=>t.total).target
    Emit OnPreferenceRecorded(catId, axis)
    UI.PlayDiscoveryFx(catId, axis)                     # 도감 ✓ 채움 + 토스트 + 보너스(P9)
    Economy.AddGold(discoveryBonus)
    SaveManager.Save()                                  # 오토세이브
```

### 3.7 다음날 준비 — 재방문 롤 (의사코드)

```
function PrepareNextDay():
    Day.current += 1
    queue = []
    for cat in discoveredCats:                          # 최소 1회 발견
        p = cfg.revisit_base                            # 0.2
        p += cat.save.satisfactionHistory.Last()/100 * cfg.revisit_satW
        if cat.lastServedFavorite: p += cfg.revisit_fav
        if cat.lastUsedPreferredSeat: p += cfg.revisit_seat
        p += RegularBonus(cat.save.regularStage)        # 단골일수록 대폭↑
        p -= (Day.current - cat.save.lastVisitDay) * cfg.revisit_decay
        p = clamp(p, 0.05, 0.95)
        if Random01() < p: queue.add(cat)
    # 미발견 고양이는 spawnWeight 신규 등장 테이블에서 별도 편성
    queue.addRange( NewCatRoll(spawnWeightTable) )
    CustomerSpawner.SetQueue(queue)
```

### 3.8 단골 판정 (의사코드, P5 파생값 사용)

```
function EvaluateRegular(cat):                          # 매일 정산 시
    v = cat.save.visitCount
    avg = cat.save.satisfactionHistory.Average()        # 파생(P5)
    recorded = CountRecordedAxes(cat.save.observation)  # ✓ 축 수(P4)
    stage = cat.save.regularStage
    if stage<2 and v>=3  and avg>=60:                       stage=2
    if stage<3 and v>=6  and recorded>=2:                   stage=3
    if stage<4 and v>=10 and recorded>=3:                   stage=4; QueueEpisode(cat)
    if stage<5 and v>=15 and cat.episodeCompleted:         stage=5; GrantSpecialReward(cat)
    if stage != cat.save.regularStage:
        cat.save.regularStage = stage; Emit OnRegularStageUp(cat.id, stage)
    # 강등 없음 (부록 J-H)
```

### 3.9 전체 흐름 요약도

```
[행동] SeatSelector/OrderDecider/FacilityUseChance/SocialTick
   │  (선호 대상 없으면 fallback: 무관 좌석/차선 메뉴/시설행동 미발생)
   ▼
RecordClue(weight; fallback=0.5 & cap2)  ──실시간──▶ 서술 이펙트(UI)
   ▼ (Leave)
ComputeSatisfaction ─▶ ComputePayment ─▶ Economy.AddGold
   ▼                         satisfactionHistory.Push (단일소스)
StageSessionClues
   ▼ (DayEnd)
추론 승격: … → ★(Confirmable)   [fallback은 cap으로 승격 차단]
   ▼ (플레이어 기록)
Recorded(✓) ─▶ OnPreferenceRecorded ─▶ 도감/성장조건/보너스 ─▶ Save
   ▼ (NextDay)
RevisitChance 롤(만족·최애·선호좌석·단골보너스) ─▶ 방문 큐
   ▼ (정산)
EvaluateRegular(visit·avgSat·recordedAxes) ─▶ 단골 단계↑ ─▶ 에피소드/보상
   ▼
카페 성장(Gold + Recorded축수/가구수 OR경로) ─▶ 새 가구 슬롯 ─▶ [새 관찰 채널]
```
## 4. 좌석 시스템 (SeatSystem)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | 취향·성격·상황을 좌석 선택으로 표현. 가장 쉬운 관찰 축 | |
| ② 입력 | 배치된 Seat 가구, `CatData.seatPreference`, 성격, 다른 고양이 위치, 분위기, 입구 위치, `BalanceConfig` | |
| ③ 내부 | `Seat{ seatId, tags, worldPos, occupiedBy, IsCentral, LocalDominantAxis }` | |
| ④ 처리 | `SeatSelector.Evaluate`(2.3) → 최고 점수 착석 | |
| ⑤ 상태 | Seat: Free / Occupied / (Removed) | |
| ⑥ 조건 | 후보 = Free 좌석만. 선호 좌석 없거나 만석 → fallback(무관 좌석) | |
| ⑦ 출력 | `chosenSeat`, `OnCatSeated`, SeatUse Clue(선호=강, fallback=약) | |
| ⑧ UI | 좌석 하이라이트(배치 모드), 착석 애니 | |
| ⑨ 저장 | 좌석 자체는 `PlacedFurniture`로 저장. 런타임 점유는 미저장 | |
| ⑩ 예외 | 전 좌석 만석→J-B 성격분기; 사용 중 좌석 가구 삭제→고양이 상태 종료 후 재탐색 | |
| ⑪ 난이도 | ★★★ (점수식·근접 탐색) | |
| ⑫ 의존 | CatBrain, FurnitureManager, AtmosphereSystem, RelationshipManager | |
| ⑬ 구현 | `SeatRegistry`(FurnitureManager가 등록/해제), `SeatSelector` static. NavMesh 목적지=worldPos | |
| ⑭ 테스트 | 창가석 무→fallback·약단서 / 유→선택·강단서 / 만석→차선 / pref_exact 지배 확인 | |

---

## 5. 시설 시스템 (FacilitySystem)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | 앉기 외 행동으로 시설 취향 관찰 채널 제공 | |
| ② 입력 | 배치 Facility, `facilityPreference`, 성격, `BalanceConfig` | |
| ③ 내부 | `Facility{ id, tags, actions, occupiedBy, recentUseBy:Dict }` | |
| ④ 처리 | `FacilityUseChance`(2.5) → ActionScore 최고면 이용 | |
| ⑤ 상태 | Free / InUse | |
| ⑥ 조건 | 선호 시설 미설치→관련 행동·Clue 미발생(도감 '?') | |
| ⑦ 출력 | FacilityAction 실행, FacilityUse Clue | |
| ⑧ UI | 이용 애니(ClimbTop/Sleep/Play), 서술 이펙트 | |
| ⑨ 저장 | 가구로 저장. 이용 이력 미저장 | |
| ⑩ 예외 | 점유 중→대기/대체 행동; 이용 중 삭제→안전 종료 | |
| ⑪ 난이도 | ★★★ | |
| ⑫ 의존 | CatBrain, FurnitureManager, ObservationManager | |
| ⑬ 구현 | `FacilityRegistry`, action별 애니 이벤트 훅에서 RecordClue | |
| ⑭ 테스트 | 캣타워 무→삼색냥 관찰불가 / 유→이용확률↑·단서 / 점유 중→대체 | |

---

## 6. 가구 시스템 (FurnitureManager)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | 가구=좌석/시설 실체 + 분위기 기여 + **관찰 채널 개방** | |
| ② 입력 | `FurnitureData`, 배치 그리드, 보유 인벤토리, Gold | |
| ③ 내부 | `PlacedFurniture{ data, gridPos, rotation, instanceId }`, 그리드 점유 맵 | |
| ④ 처리 | 구매→인벤토리; 배치→SeatRegistry/FacilityRegistry 등록 + `AtmosphereSystem.Recalc()`; 제거→역처리 | |
| ⑤ 상태 | Owned / Placed / (Stored) | |
| ⑥ 조건 | 배치는 그리드 공간·충돌 검사 통과 시만. 구매는 Gold·해금 조건 | |
| ⑦ 출력 | `OnFurniturePlaced/Removed`, `OnAtmosphereRecalculated` | |
| ⑧ UI | 가구 구매창(가설 유도 카피), 배치 그리드+분위기 프리뷰, 회전(R)/삭제 | |
| ⑨ 저장 | `List<PlacedFurniture>`(furnitureId+gridPos+rotation) | |
| ⑩ 예외 | 공간부족/충돌→배치 거부 피드백; 사용 중 가구 제거→점유 고양이 재탐색 | |
| ⑪ 난이도 | ★★★ (그리드·재계산 연동) | |
| ⑫ 의존 | Economy, Seat/Facility/Atmosphere, CatBrain(재탐색), CafeManager(슬롯) | |
| ⑬ 구현 | 그리드 배치(타일). 배치/제거 시에만 registry·분위기 갱신(이벤트) | |
| ⑭ 테스트 | 배치→좌석 등록·분위기 갱신 / 충돌 거부 / 사용 중 삭제 안전 | |

---

## 7. 카페 분위기 (AtmosphereSystem)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | 카페 톤 형성 + 선호 분위기 시 좌석/시설 만족 **승수**(P2). MVP는 시각효과만 | |
| ② 입력 | 배치 가구의 `atmosphereContribution`, (정식) 일일상황 임시보정 | |
| ③ 내부 | `AtmosphereSnapshot{ axisValues[5], DominantAxis }` | |
| ④ 처리 | 배치/제거 시 `Recalc()`: 축별 합산 → `DominantAxis=argmax`(동률/저값→None) | |
| ⑤ 상태 | (스냅샷 값) | |
| ⑥ 조건 | 상대 최댓값으로 지배 판정 | |
| ⑦ 출력 | `OnAtmosphereRecalculated`, `atmoMult` 제공(정식) | |
| ⑧ UI | 5축 분위기 미터 + "조용한/활기찬 카페" 라벨 | |
| ⑨ 저장 | 미저장(가구에서 파생) | |
| ⑩ 예외 | 가구 0개→모든 축 0→None(중립) | |
| ⑪ 난이도 | ★★ | |
| ⑫ 의존 | FurnitureManager, SatisfactionCalculator(정식), SituationManager(정식) | |
| ⑬ 구현 | 이벤트 구동 재계산(매 프레임 X). MVP는 만족 미반영 플래그 | |
| ⑭ 테스트 | 캣타워 다수→활기참 지배 / MVP atmoMult=1.0 / 정식 ×1.1·×0.9 | |

---
## 8. 관찰 시스템 (ObservationManager)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | Truth 비노출, 행동 Clue를 축적·추론 승격해 플레이어 추리 유도 (게임의 심장) | |
| ② 입력 | CatBrain의 `RecordClue()` 콜, `BalanceConfig`(weight·cap·임계) | |
| ③ 내부 | `stagedToday: Dict<catId, List<Clue>>`(영업 중 버퍼), 각 축 `clueWeights`(total, fallbackSum) | |
| ④ 처리 | 실시간 stage(3.1) → DayEnd 승격(3.5): fallback cap 적용 → …/★ 승격, 강한 반증 시 강등 | |
| ⑤ 상태 | 축별 `ObsState`: Unknown→Suspected→Confirmable→Recorded | |
| ⑥ 조건 | Suspected≥3, Confirmable≥7 & 격차≥3, fallback cap=2, 반증≥5 강등 | |
| ⑦ 출력 | `AxisRecord.state` 갱신, 관찰 정리 카드 데이터, 서술 이펙트 | |
| ⑧ UI | 실시간 말풍선/이펙트(수치 숨김), 관찰 정리 카드, 도감 근거 목록 | |
| ⑨ 저장 | `ObservationRecord`(축별 state·clueWeights·recordedValue) — **필수 저장, Truth 미포함** | |
| ⑩ 예외 | fallback만 반복→Suspected 미만 정체+힌트; 대상 미설치→Clue 미생성 | |
| ⑪ 난이도 | ★★★★ (파이프라인 핵심) | |
| ⑫ 의존 | CatBrain(입력), SaveManager, 도감UI, CafeManager(성장조건), RegularManager | |
| ⑬ 구현 | 싱글턴 매니저 + 이벤트. 승격은 DayEnd 배치 처리(성능·결정성) | |
| ⑭ 테스트 | 8·11·12번(P1·P4) 통과 / Truth 세이브 부재 검사 | |

---

## 9. 취향 기록 시스템 (Recording)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | 발견을 플레이어 능동 행위로 → 성취감 + 세이브 확정(P4·P9) | |
| ② 입력 | `Confirmable(★)` 상태 축, 플레이어 '기록' 클릭 | |
| ③ 내부 | 관찰 정리 화면의 축별 버튼 상태 | |
| ④ 처리 | 3.6: Confirmable→Recorded, recordedValue 확정, 보너스, 저장 | |
| ⑤ 상태 | 버튼: 비활성(Suspected) / 활성(Confirmable) / 완료(Recorded) | |
| ⑥ 조건 | Confirmable에서만 기록 가능. Recorded 재기록 금지 | |
| ⑦ 출력 | `OnPreferenceRecorded`, 도감 ✓, discoveryBonus | |
| ⑧ UI | 관찰 정리 카드: [행동 요약]+[기록하기]+[도감]; 발견 연출 | |
| ⑨ 저장 | `AxisRecord.state=Recorded`, `recordedValue` 즉시 오토세이브 | |
| ⑩ 예외 | 이미 Recorded→버튼 완료 표시; 반증 누적→자동 강등(J-F) | |
| ⑪ 난이도 | ★★ | |
| ⑫ 의존 | ObservationManager, SaveManager, Economy, 도감 | |
| ⑬ 구현 | 정산 화면 서브 패널. 클릭 핸들러→ObservationManager.Record() | |
| ⑭ 테스트 | 11(Suspected 비활성)·12(Confirmable 기록→✓·연출)·13(재실행 유지) | |

---

## 10. 만족도 (SatisfactionCalculator)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | 관찰·환경 조정 성과를 100점으로 수치화 → 결제·재방문·단골 연결 | |
| ② 입력 | 세션 결과(served 메뉴, usedSeat, usedFacility, wait, social), Truth, atmoMult | |
| ③ 내부 | 계산 임시값 | |
| ④ 처리 | 3.3 계산식 (분위기=승수 P2, 명명 P6) | |
| ⑤ 상태 | 없음(순수 계산) | |
| ⑥ 조건 | clamp 0~100. MVP atmoMult=1.0, 음식 favorite만 | |
| ⑦ 출력 | `float satisfaction` → 결제·history·재방문 | |
| ⑧ UI | 퇴장 표정(😻/🙂/😾), 정산 요약(원수치 숨김) | |
| ⑨ 저장 | 결과만 `satisfactionHistory`에 push(단일 소스 P5) | |
| ⑩ 예외 | 주문 실패/품절→페널티; 대기 초과→감점 | |
| ⑪ 난이도 | ★★ | |
| ⑫ 의존 | CatBrain, AtmosphereSystem, Economy, Revisit/Regular | |
| ⑬ 구현 | static 계산기. 상수는 BalanceConfig | |
| ⑭ 테스트 | 3축 충족→90+ / MVP 분위기 무영향 / 대기·품절 감점 | |

---

## 11. 재방문 (RevisitSystem)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | 만족·취향충족·단골이 다음날 방문 확률로 이어짐 | |
| ② 입력 | `satisfactionHistory.Last()`, 최애 제공, 선호좌석, 단골단계, 미방문일수 | |
| ③ 내부 | 방문 큐 | |
| ④ 처리 | 3.7 RevisitChance 롤 → 방문 큐 편성(+신규 spawnWeight) | |
| ⑤ 상태 | 없음(일일 롤) | |
| ⑥ 조건 | clamp 0.05~0.95 | |
| ⑦ 출력 | `CustomerSpawner` 큐 | |
| ⑧ UI | "오늘 올 것 같은 단골" 힌트(단골만) | |
| ⑨ 저장 | `lastVisitDay`, `visitCount`(방문 확정 시) | |
| ⑩ 예외 | 미발견 고양이는 신규 등장 테이블로 분리 | |
| ⑪ 난이도 | ★★ | |
| ⑫ 의존 | Satisfaction, Regular, DayManager, CustomerSpawner | |
| ⑬ 구현 | DayManager.PrepareNextDay()에서 일괄 | |
| ⑭ 테스트 | 만족90→확률↑ / 단골 보너스 / 장기미방문 decay | |

---

## 12. 단골 (RegularManager)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | 숫자 아닌 **행동·이야기 변화**로 표현. 관찰 성과와 결속 | |
| ② 입력 | `visitCount`, `avgSatisfaction`(파생 P5), Recorded 축 수(P4), 에피소드 완료 | |
| ③ 내부 | `regularStage`(1~5) | |
| ④ 처리 | 3.8 매일 정산 시 승급 판정. 강등 없음(J-H) | |
| ⑤ 상태 | Stage 1~5 | |
| ⑥ 조건 | 3/6/10/15 방문 + avg60 / 2·3축 Recorded / 에피소드 | |
| ⑦ 출력 | `OnRegularStageUp`, 에피소드 큐잉, 특별 보상, 행동 변화(전용 자리·모션) | |
| ⑧ UI | 도감·정산 단계 뱃지, 서술적 변화 | |
| ⑨ 저장 | `regularStage` | |
| ⑩ 예외 | 미방문 강등 없음; 에피소드 미방문→대기 유지 | |
| ⑪ 난이도 | ★★★ (에피소드 연동) | |
| ⑫ 의존 | Satisfaction, Observation(Recorded), EventManager, Relationship | |
| ⑬ 구현 | 정산 시 Evaluate. 단계별 행동 변화는 CatBrain 파라미터 오버라이드 | |
| ⑭ 테스트 | 14·19(방문10·3축✓→단골4→에피소드) / 강등 없음 | |

---

## 13. 경제 (EconomyManager)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | 단일 재화 Gold로 관찰 채널(가구)·메뉴·확장 재투자. 돈=이해의 부산물 | |
| ② 입력 | 결제액, 이벤트·단골·발견 보너스, 지출(구매·확장) | |
| ③ 내부 | `gold`, 일일 `revenue`, `ingredientCost` | |
| ④ 처리 | AddGold/SpendGold, 일일 정산(매출-원가=순익) | |
| ⑤ 상태 | 없음(잔액) | |
| ⑥ 조건 | 지출은 잔액 충분 시만 | |
| ⑦ 출력 | 잔액 변경 이벤트, 정산 데이터 | |
| ⑧ UI | 상단 Gold, 일일 매출바, 정산 화면 | |
| ⑨ 저장 | `gold` | |
| ⑩ 예외 | 잔액 부족 구매→거부 피드백 | |
| ⑪ 난이도 | ★ | |
| ⑫ 의존 | Payment, Furniture/Menu 구매, CafeManager | |
| ⑬ 구현 | 싱글턴 + 이벤트. 인플레 방지(가구=며칠 저축) | |
| ⑭ 테스트 | 결제 배수 반영 / 잔액 부족 거부 / 정산 순익 계산 | |

---

## 14. 카페 성장 (CafeManager)

| # | 항목 | 내용 |
|---|---|---|
| ① 목적 | Gold+발견+단골 복합조건으로 성장 → 경영·관찰 결속. 데드락 방지(P3) | |
| ② 입력 | Gold, 발견 고양이 수, Recorded 축 수, 가구 종류 수, 단골 수, 영업일 | |
| ③ 내부 | `cafeLevel`, 레벨별 조건표(OR 경로 포함) | |
| ④ 처리 | `TryUpgrade()`: AND/OR 조건 검사 → 슬롯·영역·스폰 확장 | |
| ⑤ 상태 | Level 1~5 | |
| ⑥ 조건 | 31장 표(OR 대체 경로 필수) | |
| ⑦ 출력 | 배치 영역·가구 슬롯·스폰 테이블 확장 | |
| ⑧ UI | 성장 화면 체크리스트 + **막힌 조건 힌트**(P3) | |
| ⑨ 저장 | `cafeLevel` | |
| ⑩ 예외 | 조건 미달→비활성+원인 힌트(영구 데드락 없음) | |
| ⑪ 난이도 | ★★ | |
| ⑫ 의존 | Economy, Observation, Regular, Furniture | |
| ⑬ 구현 | 조건 데이터(SO)로 레벨 정의. 코드 무수정 확장 | |
| ⑭ 테스트 | 15(P3: 가구 5종 OR 경로로 데드락 없음) / 힌트 노출 | |

---
## 15. 보조 시스템 (요약 사양)

### 15.1 DayManager
- **목적**: 하루 타이머·시간대(3:4:5분 J-A)·다음날 준비. **입력**: BalanceConfig. **상태**: Prep/Morning/Lunch/Evening/Settlement. **처리**: `dayTimer` 누적(배속 1x/2x J-I), phase 전환 시 `OnPhaseChanged`. 종료 시 스폰 중단→순차 퇴장→OnDayEnd. **저장**: currentDay. **의존**: Spawner·Observation·Economy·Revisit. **난이도** ★★.

### 15.2 CustomerSpawner
- **목적**: phase별·재방문 큐 기반 스폰. **처리**: interval 타이머로 큐에서 pop→`CatManager.Spawn`. **조건**: 동시 체류 < 좌석 수(P7 회전율 압박 제거). **예외**: 만석→J-B 성격분기. **난이도** ★★.

### 15.3 OrderManager / CookingManager / ServingSystem
- **주문**: 티켓 큐(2.4). **조리**: 원탭(티켓 클릭→cookTime 진행바→트레이). **서빙**: 클릭 배정 후 자동 이동(J-G)→WaitFood→Eat. **예외**: 인내심0→불만 Leave. **난이도** 조리·서빙 ★★.

### 15.4 RelationshipManager `[MVP 제외 → 정식]`
- 페어 `RelationScore`(일1회 상한), 임계→친구. 친구=함께방문·인접·PlayWithFriend. `relationshipHints`로 짝 제한. **저장**: 페어 점수·단계.

### 15.5 SituationManager `[MVP 제외 → 정식]`
- 하루 1개 상황(가중), 임시 보정(스폰/분위기/선호강도), 종료 시 소멸. **핵심**: 보정 상한 < 취향 신호(전략 보존).

### 15.6 EventManager
- `CatEventData` trigger 평가(단골/관계/방문/날짜)→큐잉→재생. 1회성 `completed` 저장.

### 15.7 SaveManager
- JSON, `persistentDataPath`. MVP 단일 오토세이브(정산 완료 시), 정식 오토+수동(J-J). 임시파일→교체(무결성), `version` 필드. **필수**: ObservationRecord.

### 15.8 UIManager / 도감(CatalogUI)
- 패널 전환(씬 최소). 도감 카드: 성격1 즉시·성격2 관찰(J-C), 3축 아이콘(?/…/★/✓)+근거. 실시간 관찰=서술 텍스트만.

---

## 16. Unity 시스템 구조 (매니저·씬·데이터 배치)

### 16.1 매니저 계층
```
[Bootstrap Scene]
 └ GameStateManager (Title/Prep/Business/Settlement/Shop/Growth)
     ├ DayManager ── CustomerSpawner ── CatManager ─(N)─ CatBrain
     ├ FurnitureManager ── SeatRegistry / FacilityRegistry ── AtmosphereSystem
     ├ MenuManager ── OrderManager ── CookingManager ── ServingSystem
     ├ ObservationManager ★  ── SatisfactionCalculator(static)
     ├ RevisitSystem ── RegularManager ── EventManager
     ├ RelationshipManager(정식) ── SituationManager(정식)
     ├ EconomyManager ── CafeManager
     └ SaveManager ── UIManager ── CatalogUI
```
- **통신**: `ScriptableObject 이벤트 채널`(0.3). 매니저 간 직접 참조 최소화.
- **척추**: `CatBrain → ObservationManager → SaveManager`. 이 경로 결합도 관리가 최우선.

### 16.2 씬 구성
- **Bootstrap**(매니저·부트) / **Cafe**(플레이 공간·그리드) — 상태 전환은 씬이 아니라 **UI 패널 전환** 권장(로딩 최소, 1인 개발 유리).

### 16.3 데이터 폴더
```
Assets/Data/Cats/*.asset      (CatData SO)
Assets/Data/Furniture/*.asset (FurnitureData SO)
Assets/Data/Menus/*.asset     (MenuData SO)
Assets/Data/Events/*.asset    (CatEventData SO)
Assets/Data/Balance.asset     (BalanceConfig SO)
Assets/Data/Balance/*.csv     (가격·확률 튜닝 → SO 임포트)
```

---

## 17. 통합 테스트 매트릭스 (기획서 45장 → 자동/수동 매핑)

| # | 케이스 | 대상 시스템 | 유형 | 기대 |
|---|---|---|---|---|
| T1 | 창가석 무→치즈냥 일반석 fallback, seat `?` | Seat/Obs | 자동 | 약단서만, 승격 X |
| T2 | **[P1]** 창가석 무 반복→일반석 fallbackSum cap2 정체 | Obs | 자동 | Suspected 미승격(가짜가설 차단) |
| T3 | 창가석 유→창가석 선택·창밖응시 단서 | Seat/Obs | 자동 | 강단서 생성 |
| T4 | 창가석 만석→차선 선택 | Seat | 자동 | 창가 단서 없음 |
| T5 | 창가석 반복 관찰→weight≥7&격차≥3 | Obs | 자동 | Confirmable(★) |
| T6 | 캣타워 무→삼색냥 관찰 불가 | Facility/Obs | 자동 | Clue 미생성 |
| T7 | 캣타워 유→이용확률↑·ClimbTop 단서 | Facility | 자동 | 단서 생성 |
| T8 | 생선케이크→완식·빠름 강단서 | Order/Obs | 자동 | weight 3 |
| T9 | 최애 품절→차선·음식만족↓ | Order/Sat | 자동 | food_forced −5 |
| T10 | Suspected(…)→기록 버튼 비활성 | Recording | 수동/자동 | 비활성 |
| T11 | **[P4]** Confirmable(★)→기록→Recorded(✓)+연출 | Recording | 자동 | ✓·보너스 |
| T12 | 종료→재실행→ObservationRecord 유지 | Save | 자동 | Recorded 보존 |
| T13 | Truth가 세이브 파일에 없음 | Save | 자동 | 필드 부재 |
| T14 | 3축 충족→만족90+ | Sat | 자동 | ≥90 |
| T15 | 방문10·3축✓→단골4→에피소드 큐 | Regular | 자동 | stage4 |
| T16 | **[P3]** 골드충분·가구無→가구5종 OR경로 승급 | Cafe | 자동 | 데드락 없음 |
| T17 | **[P7]** 느긋냥 maxSitDuration 도달→Pay | CatBrain | 자동 | 자리 독점 없음 |
| T18 | **[P2]** MVP 분위기 만족 무영향(atmoMult=1.0) | Atmo/Sat | 자동 | 무변화 |
| T19 | (정식) 선호분위기 지배→만족 ×1.1 | Atmo/Sat | 자동 | 증폭 |
| T20 | J-B 만석 성격분기(대기/귀가) | Spawner | 자동 | 성격별 분기 |
| T21 | 사용 중 가구 삭제→고양이 안전 재탐색 | Furniture/Brain | 자동 | 크래시 없음 |
| T22 | 서빙 지연→인내심0→불만 Leave·페널티 | Serving/Sat | 자동 | 감점 |

> **자동화 권장**: T1~T18은 헤드리스 시뮬(고정 시드 Random)로 EditMode/PlayMode 테스트 가능. Random 편차는 테스트 시 시드 고정.

---

## 18. 개발 순서 (기획 49장 대응, 사양 기준)

| Phase | 사양 산출물 |
|---|---|
| P0 기반 | 입력/카메라(줌)·그리드·좌석 배치(에디터 수동) |
| P1 관찰 루프(수직 슬라이스, 최우선) | §0·1 데이터, §2.2~2.5(Seat/Order/Facility·fallback), §3.1·3.5·3.6(Clue→승격→기록), §15.7 Save → **T1~T13 통과 후 1차 플레이테스트 게이트** |
| P2 경영 살 | §15.3 주문/조리/서빙, §10·13 만족·경제, §11·12 재방문·단골, §6·14 상점·성장(T14~T17) |
| P3 콘텐츠·정식 | §15.4·15.5 관계·상황, §15.6 이벤트, §7 분위기 승수, UI/튜토리얼/QA(T18~T22) |

---

## 부록. 사양 ↔ 기획서 확정사항 매핑

| 확정(부록 J) | 사양 반영 |
|---|---|
| A 3:4:5 | §0.4 phaseSeconds, §15.1 |
| B 만석 성격분기 | §2.2, §4⑩, §15.2, T20 |
| C 성격1 즉시·2 관찰 | §1.1 personalityTags, §15.8, CatSaveData.personality2Revealed |
| D HFSM+Utility | §2 전체 |
| E 분위기 MVP전역/정식국소 | §7 |
| F 힌트+강한반증 강등 | §3.5, §8⑩ |
| G 클릭배정 자동이동 | §15.3 |
| H 단골 강등 없음 | §3.8, §12⑩ |
| I 배속 1x/2x | §15.1 |
| J 세이브 MVP단일/정식오토+수동 | §15.7 |
| P1 fallback cap2 | §0.4, §3.5, T2 |
| P2 분위기 승수 | §3.3, §7, T18/19 |
| P3 성장 OR경로 | §14, T16 |
| P4 4단계 추론 | §0.1, §3.5/3.6, T10/11 |
| P5 만족 단일소스 | §1.3, §3.8 |
| P6 명명 분리 | §2.3(SeatSelectionScore)/§3.3(SeatSatisfactionBonus) |
| P7 maxSitDuration | §0.4, §2.2, T17 |
| P8 MVP 축소 | §2.2, §15.4/15.5 배지 |
| P9 발견 연출 | §3.6, §9⑧ |
| P10 음식 favorite만(MVP) | §2.4, §3.3 |

---

## 부록 S. 첫 출시(1.0) 스코프 연동

> 이 사양서는 **전체 시스템**을 기술한다. 첫 Steam 출시(1.0) 구현 범위는 **`여기까페냐옹_출시스코프_1.0.md`** 를 최종 기준으로 한다.
> - **S1(코어 수직 슬라이스)에서 구현할 파일 목록**은 출시 스코프 §G에 파일 단위로 분해되어 있다.
> - 1.0에서 **미구현/삭제**로 확정된 시스템(관계·일일상황·분위기 승수·음식 계열·MoveSeat/Social·플레이어 성장)은 본 사양서에 명세가 있어도 **1.0 빌드에 포함하지 않는다.**
> - 1.0 콘텐츠 수치: 고양이 8 / 메뉴 8 / 가구 18 / 시설 6 / 좌석 5 / 맵 1 / 이벤트 0 / 에피소드 8 / 단골 3단계 / 성장 3레벨.

---

## 문서 종료

이 사양서는 검증·확정된 기획서를 Unity 구현 단위로 변환한 것이다. 핵심은 §2(CatBrain HFSM+Utility)와 §3(관찰→기록→만족→재방문→단골 단일 파이프라인)이며, 나머지 시스템은 이 파이프라인에 데이터를 공급/소비하는 역할로 정의된다. 개발은 §18 순서(및 출시 스코프 §F/§G)로 P1/S1 관찰 루프를 먼저 완성해 T1~T13을 통과시킨 뒤 1차 플레이테스트 게이트에서 핵심 재미를 검증한다.
