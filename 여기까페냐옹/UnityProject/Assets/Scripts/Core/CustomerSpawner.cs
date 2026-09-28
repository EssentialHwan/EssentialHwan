using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.Core
{
    // 사양서 §15.2 — phase별 스폰. S1은 고정 큐(3마리 반복)로 단순화.
    // 회전율 압박 제거(P7): 동시 체류 < 좌석 수가 되도록 스폰을 억제.
    public class CustomerSpawner : MonoBehaviour
    {
        public BalanceConfig cfg;
        public DayManager day;
        public CatManager cats;
        public CafeContext cafe;

        [Header("S1 고정 큐(순환)")]
        public List<CatData> fixedQueue = new();   // 치즈/삼색/젖소

        [Header("전체 고양이 풀 (해금 필터 대상, S3)")]
        public List<CatData> allCats = new();      // 8마리 전체
        public YeogiCafe.Save.SaveGame save;       // 해금 판정용(선택)

        [Header("손님 수 곡선 (밸런스 문서 06)")]
        public int cafeLevel = 1;                   // CafeManager와 동기
        public int discoveredCats = 0;              // 발견 진행

        float timer;
        int queueIndex;

        // 밸런스 시뮬: 초반 8명 → 레벨·발견에 따라 14명까지. 간격 배수로 조절.
        // 진행도 0(초반)일수록 간격 ×1.6, 진행도 높을수록 ×1.0에 수렴.
        float IntervalMultiplier()
        {
            float progress = Mathf.Clamp01((cafeLevel - 1) * 0.34f + discoveredCats * 0.08f);
            return Mathf.Lerp(1.6f, 1.0f, progress);   // 초반 넓게 → 후반 촘촘
        }

        List<CatData> ResolvePool()
        {
            if (allCats != null && allCats.Count > 0)
                return UnlockService.UnlockedCats(allCats, save);   // save==null이면 Always만
            return fixedQueue;
        }

        void OnEnable()
        {
            if (day != null) day.OnPhaseChanged += OnPhaseChanged;
        }
        void OnDisable()
        {
            if (day != null) day.OnPhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(DayPhase p)
        {
            if (p == DayPhase.Morning) { timer = 0; queueIndex = 0; }
        }

        void Update()
        {
            if (day == null || cats == null || cafe == null) return;
            if (day.Phase != DayPhase.Morning && day.Phase != DayPhase.Lunch && day.Phase != DayPhase.Evening) return;

            // 스폰 풀: 해금된 전체 고양이(allCats+save) 우선, 없으면 고정 큐(S1)
            var pool = ResolvePool();
            if (pool.Count == 0) return;

            timer += Time.deltaTime * day.speed;
            if (timer < day.SpawnInterval * IntervalMultiplier()) return;
            timer = 0;

            // P7: 여유 좌석이 없으면 스폰 보류(만석 강요 방지)
            if (cafe.FreeSeatCount <= 0) return;

            var data = pool[queueIndex % pool.Count];
            queueIndex++;
            cats.Spawn(data);
        }
    }
}
