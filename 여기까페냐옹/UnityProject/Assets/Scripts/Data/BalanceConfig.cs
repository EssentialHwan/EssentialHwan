using UnityEngine;

namespace YeogiCafe.Data
{
    // 사양서 §0.4. 전부 초기 테스트용 가정값 — 플레이테스트로 조정.
    [CreateAssetMenu(menuName = "YeogiCafe/BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        [Header("경제/시간")]
        public int startGold = 300;
        public float dayLength = 720f;              // 12분
        public Vector3 phaseSeconds = new Vector3(180, 240, 300); // 3:4:5 (부록 J-A)
        // 밸런스 시뮬(문서 06) 반영: 초반 손님 8명 규모가 되도록 스폰 간격 상향.
        // 카페 레벨·발견 진행에 따라 CustomerSpawner가 간격을 점차 낮춰 14명까지 증가.
        public float spawnMorning = 30f, spawnLunch = 20f, spawnEvening = 15f;

        [Header("좌석 선택 (SeatSelectionScore)")]
        public int prefExact = 100, prefPartial = 40, prefNone = 10;
        public int personalityBonus = 30, atmosphereSeatBonus = 20, socialFriend = 40;
        public float distancePenaltyPerUnit = 0.5f;
        public int seatRandom = 10;

        [Header("관찰 (P1 fallback cap / P4 임계)")]
        public float clueStrong = 3f, clueMedium = 1.5f, clueFallback = 0.5f;
        public float fallbackCap = 2f;              // P1: 가짜 가설 차단
        // 짐작 임계 3 → 4.5 (2026-11-12 진행 시뮬: 평균 엔딩 16→18일, 가격 조정과 합쳐 21일).
        // fallback cap(2)보다 커야 P1 가짜 가설 차단이 유지된다.
        public float thrSuspected = 4.5f, thrConfirmable = 7f;
        public int leadGap = 3;
        public float refuteThreshold = 5f;          // 부록 J-F 강한 반증 강등

        [Header("만족도")]
        public int satBase = 50, foodFav = 25, foodTag = 15, foodForced = -5;
        public int seatPref = 15, facilityPref = 15, serviceFast = 5, orderFail = -10;
        public int touchPerMood = 5;
        public int wrongDish = -12, mindRead = 8;   // 모호 주문 오답/정답
        public float giftThreshold = 70f, giftChance = 0.6f;   // 만족도 이상이면 확률로 다음 날 선물                // 쓰다듬기 반응 1단계당 만족도(±2단계)
        public float atmoMatch = 1.1f, atmoNeutral = 1.0f, atmoOpposite = 0.9f; // MVP=1.0
        public float waitPenaltyPerSec = 0.5f;
        public bool atmosphereAffectsSatisfaction = false; // P2: MVP false

        [Header("재방문/단골/자리")]
        public float revisitBase = 0.2f, revisitSatWeight = 0.5f;
        public float revisitFav = 0.1f, revisitSeat = 0.1f, revisitDecayPerDay = 0.02f;
        public float maxSitDuration = 90f;          // P7 자리 독점 방지
        public int[] regularVisitReq = { 0, 3, 6, 10, 15 };

        [Header("보상")]
        public int discoveryBonusGold = 20;         // P9 발견 성공 소액 보너스

        [Header("메뉴 수입 배수 (결제 금액에만 적용)")]
        // 2주차부터 하루 순수입이 ~1000G로 남아돌던 문제(진행 시뮬) → 손님·배달냥 결제에만 0.7배.
        // 재료비·메뉴 강화 비용(price 기준)은 그대로라 돈이 덜 남는다.
        public float menuPayMult = 0.7f;

        // 튜닝 개정 번호. 데이터 에셋(BalanceConfig.asset)은 PC에만 있어 기본값을 바꿔도 기존 에셋엔 반영되지 않는다.
        // 예전 에셋은 이 필드가 없어 0으로 읽히므로, 로드될 때 한 번 새 값으로 올려 준다.
        // 이후 인스펙터에서 직접 바꾼 값은 존중된다(개정 번호가 이미 최신이므로).
        public int tuningRevision = 0;
        const int CurrentTuningRevision = 1;

        void OnEnable()
        {
            if (tuningRevision >= CurrentTuningRevision) return;
            if (tuningRevision < 1) { thrSuspected = 4.5f; menuPayMult = 0.7f; }
            tuningRevision = CurrentTuningRevision;
        }
    }
}
