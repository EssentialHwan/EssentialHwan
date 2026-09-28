using System;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Observation
{
    // 사양서 §3.1/3.5/3.6 — 관찰 심장. Clue 기록 → cap 적용 → 4단계 승격 → 플레이어 기록.
    public struct Clue
    {
        public ClueType type;
        public string target;      // SeatTag/FacilityTag 이름 또는 menuId
        public float weight;
        public bool isFallback;
    }

    public class ObservationManager
    {
        readonly BalanceConfig cfg;
        readonly SaveGame save;
        // 영업 중 버퍼: 영속 아님
        readonly Dictionary<string, List<Clue>> stagedToday = new();

        public event Action<string> OnBehaviorNarration; // (narrationText) — UI 말풍선
        public event Action<string, PrefAxis> OnPreferenceRecorded;

        public ObservationManager(BalanceConfig cfg, SaveGame save) { this.cfg = cfg; this.save = save; }

        // §3.1 실시간 — 행동마다 호출
        public void RecordClue(string catId, ClueType type, string target, bool isFallback, string narration)
        {
            float weight = isFallback ? cfg.clueFallback
                         : (type == ClueType.FoodEat) ? cfg.clueStrong
                         : cfg.clueMedium;
            if (!stagedToday.TryGetValue(catId, out var list)) { list = new(); stagedToday[catId] = list; }
            list.Add(new Clue { type = type, target = target, weight = weight, isFallback = isFallback });
            if (!string.IsNullOrEmpty(narration)) OnBehaviorNarration?.Invoke(narration);
        }

        // §3.5 영업 종료 — 승격 (fallback cap → …/★, 강한 반증 강등)
        public void OnDayEnd()
        {
            foreach (var kv in stagedToday)
            {
                var rec = save.GetOrCreateCat(kv.Key).observation;
                foreach (var clue in kv.Value)
                {
                    var axisRec = rec.Get(clue.type.ToAxis());
                    var tally = axisRec.GetOrCreate(clue.target);
                    float add = clue.weight;
                    if (clue.isFallback) // P1 cap
                    {
                        float room = Mathf.Max(0f, cfg.fallbackCap - tally.fallbackSum);
                        add = Mathf.Min(add, room);
                        tally.fallbackSum += add;
                    }
                    tally.total += Mathf.Max(0f, add);
                }
                foreach (PrefAxis axis in Enum.GetValues(typeof(PrefAxis)))
                    Promote(rec.Get(axis));
            }
            stagedToday.Clear();
        }

        void Promote(AxisRecord axisRec)
        {
            if (axisRec.state == ObsState.Recorded)
            {
                // 강한 반증 시 강등 (부록 J-F) — 확정값 외 태그가 임계 넘게 쌓이면
                var topOther = axisRec.Second();
                if (topOther != null && topOther.total >= cfg.refuteThreshold)
                    axisRec.state = ObsState.Suspected;
                return;
            }
            var top = axisRec.Top();
            var second = axisRec.Second();
            float topT = top?.total ?? 0f;
            float secT = second?.total ?? 0f;

            if (topT >= cfg.thrConfirmable && (topT - secT) >= cfg.leadGap)
                axisRec.state = ObsState.Confirmable;   // ★
            else if (topT >= cfg.thrSuspected)
                axisRec.state = ObsState.Suspected;     // …
            // fallback cap 덕분에 fallback-only 태그는 thrSuspected(3) 미달 → 가짜 가설 차단(P1)
        }

        // §3.6 플레이어 능동 기록 (Confirmable에서만)
        public bool TryRecord(string catId, PrefAxis axis, out int bonusGold)
        {
            bonusGold = 0;
            var rec = save.GetOrCreateCat(catId).observation.Get(axis);
            if (rec.state != ObsState.Confirmable) return false;
            rec.state = ObsState.Recorded;              // ✓
            rec.recordedValue = rec.Top()?.target;
            bonusGold = cfg.discoveryBonusGold;         // P9
            OnPreferenceRecorded?.Invoke(catId, axis);
            return true;
        }

        public IReadOnlyDictionary<string, List<Clue>> StagedToday => stagedToday;
    }
}
