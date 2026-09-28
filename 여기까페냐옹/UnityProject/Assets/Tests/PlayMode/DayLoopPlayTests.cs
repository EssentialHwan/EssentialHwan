#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace YeogiCafe.Tests.PlayMode
{
    // PlayMode 통합 테스트 스텁 — 실제 Unity에서 실행. T12/T17/T22 커버.
    // 여기서는 씬 셋업 훅만 정의(개발자가 S1Bootstrap 씬을 로드해 연결).
    // dotnet 검증 대상 아님(UNITY_INCLUDE_TESTS 가드).
    public class DayLoopPlayTests
    {
        // T12: 저장→로드 왕복. ObservationRecord.Recorded 유지, Truth 부재.
        [UnityTest]
        public IEnumerator T12_Save_Load_RoundTrip_KeepsObservation()
        {
            // TODO(M1): S1Bootstrap 씬 로드 → 하루 진행 → 좌석 기록 → Save →
            //           새 SaveManager.LoadOrNew() → observation.seat.state == Recorded 확인.
            Assert.Ignore("M1 씬 배선 후 구현 — 저장 왕복 검증");
            yield return null;
        }

        // T17: maxSitDuration 도달 시 Pay 전이(자리 무한 점유 없음).
        [UnityTest]
        public IEnumerator T17_MaxSitDuration_ForcesPay()
        {
            // TODO(M1): 느긋한 고양이 스폰 → 배속으로 maxSitDuration 초과 →
            //           상태가 Pay/Leave로 전이하고 좌석이 Vacate 되는지 확인.
            Assert.Ignore("M1 씬 배선 후 구현 — 자리 독점 방지 타이밍");
            yield return null;
        }

        // T22: 서빙 지연 → 인내심 0 → 불만 Leave + 매출/만족 페널티.
        [UnityTest]
        public IEnumerator T22_ServeDelay_CausesUnhappyLeave()
        {
            // TODO(M3): OrderSystem 연결 후, 조리/서빙 미처리로 patience 소진 →
            //           티켓 Cancelled → CatBrain 불만 Leave → orderFailed 만족 페널티 확인.
            Assert.Ignore("M3 서빙 시스템 연결 후 구현");
            yield return null;
        }

        // 스모크: 하루 루프가 예외 없이 완주하는가 (M1 게이트 핵심).
        [UnityTest]
        public IEnumerator Smoke_OneDayLoop_NoExceptions()
        {
            // TODO(M1): 씬 로드 → DayManager.StartDay() → 배속 → OnDayEnded까지 예외 0.
            Assert.Ignore("M1 씬 배선 후 구현 — 하루 완주 스모크");
            yield return null;
        }
    }
}
#endif
