# QA 추적표 (사양서 §17 T1~T22)

> 자동 = EditMode 테스트로 커버(dotnet 검증 완료). 씬 = 씬 배선 후 PlayMode/수동 필요.

| # | 케이스 | 유형 | 상태 | 커버 위치 |
|---|---|---|---|---|
| T1 | 창가석 무→일반석 fallback, seat `?` | 자동 | ✅ | SeatSelectionTests.NoWindowSeat_FallsBackToNormal + EndToEnd |
| T2 | **[P1]** fallback cap2→가짜가설 차단 | 자동 | ✅ | ObservationPipelineTests.Fallback_SeatClue_CappedAt2 |
| T3 | 창가석 유→선택·창밖응시 단서 | 자동 | ✅ | SeatSelectionTests.WindowSeatPresent_SelectsWindow |
| T4 | 창가석 만석→차선 | 자동 | ✅ | SeatSelectionTests.WindowSeatOccupied_Excluded |
| T5 | 반복 관찰→Confirmable(★) | 자동 | ✅ | ObservationPipelineTests.StrongSeatClue_Reaches_Confirmable |
| T6 | 캣타워 무→관찰 불가 | 자동 | ✅ | FacilityChannelTests.NoPreferredFacility_NoStrongSignalPath |
| T7 | 캣타워 유→이용확률↑·단서 | 자동 | ✅ | FacilityChannelTests.PreferredFacility_HasHigherUseChance |
| T8 | 생선케이크 완식→강단서 | 자동 | ✅ | EndToEnd(FoodEat weight=strong) |
| T9 | 최애 품절→차선·만족↓ | 자동 | ✅ | SatisfactionAndOrderTests.ForcedSecondChoice_LowersFoodSatisfaction |
| T10 | Suspected→기록 버튼 비활성 | 자동 | ✅ | ObservationPipelineTests(Food Unknown 기록 실패) |
| T11 | **[P4]** Confirmable→기록→✓+보너스 | 자동 | ✅ | ObservationPipelineTests.Record_OnlyFromConfirmable |
| T12 | 종료→재실행→기록 유지 | 씬 | ⬜ | SaveManager 구현됨. PlayMode 저장/로드 테스트 필요 |
| T13 | Truth가 세이브에 없음 | 자동 | ✅ | ObservationPipelineTests.SaveModel_DoesNotContain_Truth |
| T14 | 3축 충족→만족90+ | 자동 | ✅ | SatisfactionAndOrderTests.ThreeAxisSatisfied_Reaches90Plus |
| T15 | 방문·축 Recorded→단골 승급 | 자동 | ✅ | RegularAndBranchTests.RegularStage_Upgrades (강등 없음 포함) |
| T16 | **[P3]** 가구 OR경로 데드락 없음 | 자동 | ✅ | CafeGrowthTests.P3_ORPath_PreventsDeadlock (CafeManager 로직) |
| T17 | **[P7]** maxSitDuration→Pay | 씬 | ⬜ | CatBrain sitDuration 상한 구현됨. PlayMode 타이밍 테스트 |
| T18 | **[P2]** MVP 분위기 만족 무영향 | 자동 | ✅ | SatisfactionAndOrderTests.MVP_AtmosphereDoesNotAffectSatisfaction |
| T19 | (정식) 선호분위기→×1.1 | 자동 | ✅ | AtmosphereTests.Multiplier_Match_Opposite_Neutral + 파이프라인 연결 |
| T20 | J-B 만석 성격분기 | 자동 | ✅ | RegularAndBranchTests.FullCafe_PersonalityBranch |
| T21 | 사용 중 가구 삭제→안전 재탐색 | 씬 | ⬜ | FurnitureManager 제거 로직 S2에서 |
| T22 | 서빙 지연→불만 Leave | 씬 | ⬜ | ServingSystem 미구현(S2) |

## 요약 (38 EditMode 테스트 통과)
- **자동 커버(✅) 18/22 케이스**: T1~T11, T13~T16, T18~T20 — 핵심 불변식·만족·시설채널·오추론방지·단골·성격분기·성장데드락·분위기승수·음식2계층 전부 자동화.
- **PlayMode 스텁(⬜) 4건**: T12(저장 왕복), T17(sitDuration 타이밍), T21(가구제거), T22(서빙 지연) — `Assets/Tests/PlayMode/`에 스텁 존재, 씬 배선(M1/M3) 후 구현.

## 남은 테스트 (실제 Unity 필요)
1. T12 저장→로드 왕복 (PlayMode, 파일 I/O)
2. T17 maxSitDuration 타이밍 (PlayMode)
3. T21 사용 중 가구 삭제 안전성 (PlayMode)
4. T22 서빙 지연→불만 Leave (PlayMode)
