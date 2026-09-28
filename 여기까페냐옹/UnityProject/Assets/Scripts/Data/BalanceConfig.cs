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
        public float thrSuspected = 3f, thrConfirmable = 7f;
        public int leadGap = 3;
        public float refuteThreshold = 5f;          // 부록 J-F 강한 반증 강등

        [Header("만족도")]
        public int satBase = 50, foodFav = 25, foodTag = 15, foodForced = -5;
        public int seatPref = 15, facilityPref = 15, serviceFast = 5, orderFail = -10;
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
    }
}
