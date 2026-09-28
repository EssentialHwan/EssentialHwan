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

        [Range(1, 3)] public float speed = 1f;   // 배속 1x/2x(/3x 옵션)
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
            DayTimer += Time.deltaTime * speed;

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

        public float SpawnInterval => Phase switch
        {
            DayPhase.Morning => cfg.spawnMorning,
            DayPhase.Lunch   => cfg.spawnLunch,
            DayPhase.Evening => cfg.spawnEvening,
            _ => float.MaxValue
        };
    }
}
