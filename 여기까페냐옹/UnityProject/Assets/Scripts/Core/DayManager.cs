using System;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.Core
{
    // 사양서 §15.1 — 하루 타이머·시간대(3:4:5 J-A)·배속(J-I). 종료→정산→다음날.
    public class DayManager : MonoBehaviour
    {
        public BalanceConfig cfg;

        public DayPhase Phase { get; private set; } = DayPhase.Prep;
        public float DayTimer { get; private set; }
        public int CurrentDay { get; private set; } = 1;

        // [버그수정] 기본 배속 2x — 12분 하루가 1x면 스폰/이동이 느려 '멈춘 듯' 보이던 체감 문제 완화.
        [Range(1, 3)] public float speed = 1f;   // 배속 1x/2x/3x
        // 1배속 = 하루 6분(게임 720초를 2배로 흘림). 2배속 3분, 3배속 2분.
        public const float BaseRate = 2f;
        public float Rate => speed * BaseRate;
        bool running;

        public event Action<DayPhase> OnPhaseChanged;
        public event Action OnDayEnded;

        public void StartDay()
        {
            DayTimer = 0f; running = true;
            SetPhase(DayPhase.Morning);
        }

        void Update()
        {
            if (!running) return;
            DayTimer += Time.deltaTime * Rate;

            float m = cfg.phaseSeconds.x;
            float l = cfg.phaseSeconds.x + cfg.phaseSeconds.y;
            float e = l + cfg.phaseSeconds.z;

            if (Phase == DayPhase.Morning && DayTimer >= m) SetPhase(DayPhase.Lunch);
            else if (Phase == DayPhase.Lunch && DayTimer >= l) SetPhase(DayPhase.Evening);
            else if (Phase == DayPhase.Evening && DayTimer >= e) EndDay();
        }

        void SetPhase(DayPhase p) { Phase = p; OnPhaseChanged?.Invoke(p); }

        void EndDay()
        {
            running = false;
            SetPhase(DayPhase.Settlement);
            // 매니저 순서: 신규 스폰 중단 → 잔여 고양이 순차 퇴장 → 관찰 승격 → 정산 → 다음날 재방문 롤
            OnDayEnded?.Invoke();
        }

        public void AdvanceToNextDay() => CurrentDay++;

        // 세이브에서 일차 복원(이어하기)
        public void RestoreDay(int dayNumber) { if (dayNumber > 0) CurrentDay = dayNumber; }

        // 개발용: 남은 영업 시간을 건너뛰고 즉시 정산으로
        public void DebugEndDayNow() { if (running) { DayTimer = cfg.phaseSeconds.x + cfg.phaseSeconds.y + cfg.phaseSeconds.z; EndDay(); } }

        public float SpawnInterval => Phase switch
        {
            DayPhase.Morning => cfg.spawnMorning,
            DayPhase.Lunch   => cfg.spawnLunch,
            DayPhase.Evening => cfg.spawnEvening,
            _ => float.MaxValue
        };
    }
}
