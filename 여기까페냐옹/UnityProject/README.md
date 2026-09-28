# 《여기까페냐옹》 Unity 프로젝트 (S1 코어 스켈레톤)

관찰·추리형 고양이 카페 경영 시뮬. 이 폴더는 **S1(코어 수직 슬라이스)** 의 컴파일 가능한 코드 뼈대다.
목표는 "보기 좋음"이 아니라 **핵심 재미(관찰→추론→가구구매→행동변화→발견) 검증**이다.

## 관련 문서 (상위 폴더)
- `여기까페냐옹_게임기획서.md` — 전체 비전(검증·확정본)
- `여기까페냐옹_Unity개발사양서.md` — 시스템별 13항목 사양 + 의사코드
- `여기까페냐옹_출시스코프_1.0.md` — **첫 출시 범위·8마리 조합표·S1 태스크 분해** (구현 시 최종 기준)

## 코드 지도 (Assets/Scripts)
| 폴더 | 파일 | 사양 |
|---|---|---|
| Data | Tags, BalanceConfig, CatData, FurnitureData, MenuData | §0.1, §0.4, §1.1, §1.2 |
| Save | SaveModels (SaveGame/CatSaveData/ObservationRecord…) | §1.3 — **Truth 미포함** |
| AI | SeatSelector, OrderDecider, FacilityDecider, CatBrain | §2.2~2.5 |
| Observation | ObservationManager(심장), SatisfactionCalculator | §3.1/3.3/3.5/3.6 |
| Progression | RevisitSystem, RegularManager | §3.7/3.8 |

## 핵심 불변식 (절대 위반 금지)
1. **Truth vs Observed 분리**: `CatData` 취향은 세이브·UI에 노출 금지. UI는 `ObservationRecord`만 읽는다.
2. **fallback ≠ 취향**: 선호 대상이 후보에 없으면 자연 발생. fallback 단서는 weight 0.5 + 누적 cap 2 → 가짜 가설 차단.
3. **가구 = 관찰 채널**: 카페에 없으면 관련 Clue 미생성(도감 '?').
4. **4단계 추론**: `? → … → ★(Confirmable) → ✓(Recorded)`. ✓는 플레이어 기록만.
5. **태그 매칭**: `if (cat == 치즈냥)` 같은 하드코딩 분기 금지.

## 테스트
`Assets/Tests/EditMode/` — Unity Test Framework(EditMode). 핵심 파이프라인 8케이스(T1~T5, T11, T13) 통과 확인됨.
Unity에서 Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All.

## S1 진행 현황

### 구현 완료 (컴파일·테스트 검증됨)
- **데이터/세이브**: Tags, BalanceConfig, CatData/FurnitureData/MenuData, SaveModels(Truth 미포함), SaveManager
- **AI**: SeatSelector, OrderDecider, FacilityDecider, CatBrain(HFSM 선형)
- **관찰**: ObservationManager(심장), SatisfactionCalculator
- **진행**: RevisitSystem, RegularManager
- **카페/코어**: CafeContext, SeatBehaviour/FacilityBehaviour, FurnitureManager, DayManager, CatManager, CustomerSpawner(고정 큐), EconomyManager, GameFlowController(§3 파이프라인 배선), **S1Bootstrap(빈 씬 실행 진입점)**
- **UI 모델**: ObservationCardModel(카드 데이터), BehaviorNarrationRelay(서술 중계)
- **테스트**: EditMode 9종 통과 (T1~T5, T11, T13 + 통합 아크 1종)

### 남은 S1 작업
1. **씬 배선**: 빈 씬에 `S1Bootstrap` 붙이고 데이터(치즈/삼색/젖소, 메뉴, 일반석, 창가석) 연결 + 좌석 슬롯(Transform) 배치
2. **뷰 연결**: ObservationCardModel → 실제 UI(TMP/버튼), BehaviorNarrationRelay → 말풍선, 상점 버튼 → `S1Bootstrap.BuyWindowSeat()`
3. **NavMeshAgent**: `ICatMover` 구현체를 `NavMeshCatMover`로 교체(현재 SimpleLerpMover 직선이동으로 동작)
4. ~~WaitOutside 재진입~~ ✅ 구현됨(자리 나면 FindSeat 복귀, 20s 후 귀가)
5. **DoD**: 창가석 사서 → 다음날 치즈냥 창가 착석 → 기록 → 도감 ✓ → 재실행 유지
6. **진짜 게이트**: 테스터가 스스로 "가구 사서 확인해볼래"라고 말하는가?

### 구현된 이동/좌석 동작
- `ICatMover`(SimpleLerpMover 기본) — FSM과 이동 분리. NavMesh로 교체 가능.
- 좌석 점유/반납: 착석 시 `Occupy`, Pay 시 `Vacate` → 실제 만석/회전 동작.

## 빠른 실행 (에디터)
1. 메뉴 **여기까페냐옹 ▸ 1.0 콘텐츠 에셋 생성** → 8마리/8메뉴/11가구 SO 자동 생성 (`Assets/Data/`)
2. `BalanceConfig` 에셋 1개 생성 (Create ▸ YeogiCafe ▸ BalanceConfig)
3. 빈 GameObject에 `S1Bootstrap` 추가 → `balance`, `starterCats`(치즈/삼색/젖소), `starterMenus`, `starterSeats`(일반석), `windowSeatData`(창가석) 연결
4. Play → 고양이 스폰·착석·관찰 단서 로그 확인
5. 상점 버튼(임시)에서 `BuyWindowSeat()` → 다음날 창가석 착석 관찰

## 에디터 도구
- `Assets/Editor/ContentGenerator.cs` — CONTENT_AUTHORING.md 표를 SO 에셋으로 원클릭 생성 (반복 수작업 제거)
