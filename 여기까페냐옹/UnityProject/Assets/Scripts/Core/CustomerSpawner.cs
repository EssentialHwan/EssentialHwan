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

        [Header("재방문(밸런스 문서 06 §F)")]
        public int maxVisitsPerDay = 2;             // 같은 고양이는 하루 최대 2번(전엔 무제한 → 매출 7~9배)
        readonly Dictionary<string, int> visitsToday = new();
        readonly HashSet<string> comersToday = new();
        readonly HashSet<string> firstMetToday = new();   // 오늘 처음 만난 고양이는 호기심에 한 번 더(최대 3회) — 1일차가 너무 비지 않게
        int rolledDay = -1;

        // 아침마다: 지난 방문 만족·단골 단계로 오늘 올 고양이를 굴린다(RevisitSystem). 처음 보는 고양이는 늘 올 수 있다.
        void RollToday()
        {
            rolledDay = day.CurrentDay;
            visitsToday.Clear(); comersToday.Clear(); firstMetToday.Clear();
            if (save == null || cfg == null) return;
            var pool = ResolvePool();
            foreach (var d in pool)
            {
                var cs = save.GetOrCreateCat(d.catId);
                if (cs.visitCount == 0 || Random.value < YeogiCafe.Progression.RevisitSystem.RevisitChance(cs, day.CurrentDay, cfg))
                    comersToday.Add(d.catId);
            }
            // 너무 썰렁한 날 방지: 최소 2마리(또는 해금된 전부)
            for (int guard = 0; comersToday.Count < Mathf.Min(2, pool.Count) && guard < 20; guard++)
                comersToday.Add(pool[Random.Range(0, pool.Count)].catId);
        }

        bool CanComeToday(CatData d)
        {
            if (save == null) return true;
            visitsToday.TryGetValue(d.catId, out int n);
            if (n >= (firstMetToday.Contains(d.catId) ? maxVisitsPerDay + 1 : maxVisitsPerDay)) return false;
            return save.GetOrCreateCat(d.catId).visitCount == 0 || comersToday.Contains(d.catId) || firstMetToday.Contains(d.catId);
        }

        void CountVisit(CatData d)
        {
            if (save != null && save.GetOrCreateCat(d.catId).visitCount == 0) firstMetToday.Add(d.catId);
            visitsToday.TryGetValue(d.catId, out int n); visitsToday[d.catId] = n + 1;
        }
        bool firstSpawnDone;   // [버그수정] 아침 시작 시 첫 손님을 즉시 스폰(≈48초 무반응 제거)

        [Header("즉시 첫 스폰")]
        // Play 직후 스폰 간격(30s×1.6≈48s)만큼 아무 일도 안 일어나 '고양이가 안 움직인다'로 보이던 문제.
        // 아침 phase 진입 즉시 1마리를 스폰해 플레이어가 바로 이동/착석을 볼 수 있게 한다.
        public bool immediateFirstSpawn = true;

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
            if (p == DayPhase.Morning) { timer = 0; queueIndex = 0; firstSpawnDone = false; }
        }

        // 첫 손님 즉시 스폰(가능하면). 성공 시 true.
        bool TrySpawnFirst()
        {
            if (!immediateFirstSpawn || firstSpawnDone) return false;
            firstSpawnDone = true;
            var pool = ResolvePool();
            if (pool.Count == 0 || cafe.FreeSeatCount <= 0) return false;
            var data = NextNotInCafe(pool);
            if (data == null) return false;
            CountVisit(data);
            cats.Spawn(data);
            return true;
        }

        void Update()
        {
            if (day == null || cats == null || cafe == null) return;
            if (day.Phase != DayPhase.Morning && day.Phase != DayPhase.Lunch && day.Phase != DayPhase.Evening) return;
            if (rolledDay != day.CurrentDay) RollToday();

            // [버그수정] 아침 시작 즉시 첫 손님 1마리 스폰(대기시간 없이 바로 움직임이 보이도록)
            if (TrySpawnFirst()) { timer = 0; return; }

            // 스폰 풀: 해금된 전체 고양이(allCats+save) 우선, 없으면 고정 큐(S1)
            var pool = ResolvePool();
            if (pool.Count == 0) return;

            timer += Time.deltaTime * day.speed;
            if (timer < day.SpawnInterval * IntervalMultiplier()) return;
            timer = 0;

            // P7: 여유 좌석이 없으면 스폰 보류(만석 강요 방지)
            if (cafe.FreeSeatCount <= 0) return;

            var data = NextNotInCafe(pool);
            if (data != null) { CountVisit(data); cats.Spawn(data); }
        }

        // 같은 고양이(개체)가 동시에 두 마리 있을 수 없다 — 이미 카페 안(또는 입장 중)이면 건너뛴다.
        CatData NextNotInCafe(List<CatData> pool)
        {
            for (int tries = 0; tries < pool.Count; tries++)
            {
                var d = pool[queueIndex % pool.Count];
                queueIndex++;
                if (d == null || cats.IsInCafe(d.catId) || !CanComeToday(d)) continue;
                // 졸업한 고양이는 가끔만 놀러 온다
                if (save != null && save.GetOrCreateCat(d.catId).graduated && Random.value < 0.67f) continue;
                return d;
            }
            return null;
        }
    }
}
