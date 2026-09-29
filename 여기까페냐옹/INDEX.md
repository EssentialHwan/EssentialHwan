# 《여기까페냐옹》 프로젝트 인덱스

# 《여기까페냐옹》 — 프로젝트 인덱스

관찰·추리형 고양이 카페 경영 시뮬 (PC/Steam, 1인+AI 개발). **이 파일이 전체 산출물의 진입점이다.**

> **한 줄 상태**: 기획 전 영역 완성 + **기획서의 거의 모든 시스템을 코드로 구현**(S1 코어·S2 정식확장·S3 콘텐츠·관계 시스템). **66 EditMode 테스트 통과(dotnet 헤드리스 검증)**, 밸런스/다일 시뮬 검증, 콘텐츠 생성기·로컬라이즈·서술 세분화·UI 뷰 헬퍼 완비. **남은 것은 오직 Unity 에디터 GUI 작업(M1 씬 배선) → 재미 게이트(M2).**

## 📄 핵심 문서 (읽는 순서)

| 순서 | 문서 | 용도 |
|---|---|---|
| 1 | `여기까페냐옹_게임기획서.md` | 전체 비전. 세계관·핵심재미·52장 시스템 + 부록 |
| 2 | `여기까페냐옹_Unity개발사양서.md` | 구현 사양. 13항목 + CatBrain/파이프라인 의사코드 |
| 3 | `여기까페냐옹_출시스코프_1.0.md` | **첫 출시 범위 확정** (1.0 구현 최종 기준) |
| 4 | `UnityProject/README.md` | 코드 프로젝트 가이드·실행법·진행현황 |
| 5 | `UnityProject/Assets/Data/CONTENT_AUTHORING.md` | SO 저작표 |
| 6 | `UnityProject/Assets/Tests/QA_TRACKING.md` | QA 커버리지 추적 |

## 📗 개발 가능 상세 기획 (`기획/` — 출시까지 필요한 전 영역)

| # | 문서 | 용도 |
|---|---|---|
| 01 | `기획/01_내러티브_대사_스크립트.md` | 단골 에피소드 8편·서술 텍스트·로컬키 (확정 대사) |
| 02 | `기획/02_튜토리얼_온보딩_스크립트.md` | 첫 세션 10단계 스크립트 + 완주 지표 |
| 03 | `기획/03_UIUX_화면명세.md` | 8개 화면 와이어프레임·상호작용·바인딩 |
| 04 | `기획/04_아트_사운드_에셋목록.md` | 전 에셋 목록 + AI 프롬프트 + 조달 전략 |
| 05 | `기획/05_오디오_기획.md` | BGM 3·SFX 10·이벤트 매핑 |
| 06 | `기획/06_밸런스_스프레드시트.md` | 수치표 + **dotnet 경제곡선 시뮬 검증** |
| 07 | `기획/07_로컬라이제이션_텍스트시트.md` | 한/영 CSV 스키마·키 |
| 08 | `기획/08_Steam_출시_체크리스트.md` | 스토어·빌드·QA·마케팅 실무 |
| 09 | `기획/09_접근성_옵션_명세.md` | 옵션 메뉴·색약·힌트 강도 |
| 10 | `기획/10_개발_로드맵_마일스톤.md` | **M0~M8 출시 로드맵·게이트** |
| 11 | `기획/11_시장_포지셔닝.md` | 차별점·경쟁분석·세일즈 훅 |

## 💻 코드 (UnityProject/Assets) — 35 스크립트 + 에디터/로컬라이즈, dotnet 컴파일·테스트 검증

```
Scripts/Data/         Tags, BalanceConfig, CatData, FurnitureData, MenuData, CatEventData
Scripts/Save/         SaveModels(Truth 미포함), SaveManager
Scripts/AI/           SeatSelector, OrderDecider, FacilityDecider, FullCafePolicy,
                      ICatMover(+SimpleLerpMover), NavMeshCatMover, CatBrain(HFSM)
Scripts/Observation/  ObservationManager(심장), SatisfactionCalculator
Scripts/Progression/  RevisitSystem, RegularManager, EventManager, RelationshipManager
Scripts/Cafe/         SeatBehaviour, FacilityBehaviour, FurnitureManager, CafeManager, AtmosphereSystem
Scripts/Order/        OrderSystem (주문·조리·서빙)
Scripts/Core/         CafeContext, DayManager, CatManager, CustomerSpawner, EconomyManager,
                      UnlockService, GameFlowController(전체 배선), S1Bootstrap(실행 진입점)
Scripts/UI/           ObservationCardModel, BehaviorNarrationRelay, SettlementCardView, HudFormatter
Scripts/Observation/  ObservationManager, SatisfactionCalculator, NarrationCatalog
Scripts/Localization/ Localization(L 로더, 한/영·토큰치환·폴백)
Scripts/Art/          PixelPalette(단일팔레트)·PixelCanvas(공용붓)·SpriteDrawing·FurnitureDrawing
Editor/               ContentGenerator (SO 원클릭) · PixelArtGenerator (도트 PNG 34개 원클릭)
Localization/         strings.csv (한/영)
Art/                  Generated/ (생성된 PNG: 고양이8·가구18·아이콘8) — 톤 통일
```

## ✅ 테스트 (66 EditMode [Test], 전부 통과 — dotnet 헤드리스 검증; Unity Test Runner 실행은 M1 후)
- Observation·SeatSelection·SatisfactionAndOrder·FacilityChannel·Atmosphere·FoodTier
- RegularAndBranch·CafeGrowth·Unlock(Condition·Service)·Relationship·Event·OrderSystem
- SaveContract·Localization·StringsCsvIntegrity·NarrationCatalog·HudFormatter
- EndToEndFlow·MultiDaySimulation(10일 전체 체인)
- PlayMode 스텁 4(T12·T17·T21·T22, 씬 배선 후) · QA 18/22 자동화

## 🎯 핵심 재미 (절대 불변)
```
숨겨진 취향 → CatBrain 행동(+fallback) → Clue 관찰 → 4단계 추론(?→…→★→✓)
→ 플레이어 기록(발견) → 만족도 → 재방문 → (다음날) 행동 변화 확인 → 도감
+ 가구 = 관찰 채널 개방
```

## 🚦 현재 상태 (마일스톤 M0 완료)
- **기획 전 영역 완성**: 게임기획·사양·출시스코프 + 기획/ 11개 상세문서(내러티브·튜토리얼·UI·아트·오디오·밸런스·로컬라이즈·출시체크·접근성·로드맵·포지셔닝) → **개발 가능 수준**
- **S1 코어 코드**: 로직·매니저·배선·부트스트랩·이동·서빙·성장 스켈레톤 + 23 테스트 통과 (dotnet 검증)
- **밸런스**: 경제 곡선 dotnet 시뮬 검증(일순익 176G·Lv2 6일·Lv3 12일 목표 부합)
- **Git**: 버전 관리 + 다수 커밋
- **다음(M1, Unity 필요)**: SO 생성 → 씬 배선 → UI 뷰 → **M2 재미 게이트**(관찰 재미 외부 검증) → M3 경영 → M4 콘텐츠 → M5 폴리시 → M6 QA → M7 출시
- **로드맵**: `기획/10_개발_로드맵_마일스톤.md`

## 📌 첫 출시 1.0 수치
고양이 8 · 메뉴 8 · 가구 18(좌석6+시설6+장식6) · 맵 1 · 이벤트 0 · 단골 에피소드 8 · 단골 3단계 · 성장 3레벨 · UI 8종 · 한/영
