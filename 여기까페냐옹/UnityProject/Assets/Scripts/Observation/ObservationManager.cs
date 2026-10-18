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
        public event Action<string, string> OnCatNarration; // (catId, narrationText) — 고양이 머리 위 말풍선
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
            if (!string.IsNullOrEmpty(narration))
            {
                OnBehaviorNarration?.Invoke(narration);
                OnCatNarration?.Invoke(catId, narration);
            }
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

        // ── 추리 기록: 플레이어가 선택지(메뉴/가구) 중 하나를 고른다 ──
        // 짐작(…)부터 도전 가능, 하루 한 번. 정답이면 ✓ + 보너스(짐작 단계에서 맞히면 1.5배), 오답이면 기록되고 내일 다시.
        public enum GuessResult { Correct, Wrong, NotReady, TriedToday, AlreadyRecorded }

        public GuessResult CanGuess(string catId, PrefAxis axis, int day)
        {
            var rec = save.GetOrCreateCat(catId).observation.Get(axis);
            if (rec.state == ObsState.Recorded) return GuessResult.AlreadyRecorded;
            if (rec.state == ObsState.Unknown) return GuessResult.NotReady;
            if (rec.lastGuessDay == day) return GuessResult.TriedToday;
            return GuessResult.Correct;   // = 도전 가능
        }

        public GuessResult TryGuess(string catId, PrefAxis axis, string guessId, bool isCorrect, int day, out int bonusGold)
        {
            bonusGold = 0;
            var can = CanGuess(catId, axis, day);
            if (can != GuessResult.Correct) return can;
            var rec = save.GetOrCreateCat(catId).observation.Get(axis);
            rec.lastGuessDay = day;
            if (!isCorrect)
            {
                if (!rec.wrongGuesses.Contains(guessId)) rec.wrongGuesses.Add(guessId);
                return GuessResult.Wrong;
            }
            bool early = rec.state == ObsState.Suspected;
            rec.state = ObsState.Recorded;
            rec.recordedValue = guessId;
            bonusGold = Mathf.RoundToInt(cfg.discoveryBonusGold * (early ? 1.5f : 1f));
            OnPreferenceRecorded?.Invoke(catId, axis);
            return GuessResult.Correct;
        }

        // ── 묶음 채점(오브라 딘식): 골라 둔 답을 한꺼번에 확인. 전부 맞아야 기록, 아니면 몇 개 맞았는지만 알려준다.
        public struct BatchItem { public PrefAxis axis; public string guessId; public bool correct; }

        public bool SubmitBatch(string catId, List<BatchItem> items, int day, out int correctCount, out int bonusGold)
        {
            correctCount = 0; bonusGold = 0;
            if (items == null || items.Count == 0) return false;
            foreach (var it in items)
                if (CanGuess(catId, it.axis, day) != GuessResult.Correct) return false;
            foreach (var it in items) if (it.correct) correctCount++;
            bool all = correctCount == items.Count;
            var obsRec = save.GetOrCreateCat(catId).observation;
            if (all)
            {
                foreach (var it in items)
                {
                    TryGuess(catId, it.axis, it.guessId, true, day, out int b);
                    bonusGold += b;
                    obsRec.Get(it.axis).pendingGuess = null;
                }
                if (items.Count >= 2) bonusGold += cfg.discoveryBonusGold * (items.Count - 1);   // 한꺼번에 맞힌 보너스
                return true;
            }
            foreach (var it in items)
            {
                var r = obsRec.Get(it.axis);
                r.lastGuessDay = day;
                // 한 칸만 냈을 때만 무엇이 틀렸는지 알 수 있다
                if (items.Count == 1 && !r.wrongGuesses.Contains(it.guessId)) r.wrongGuesses.Add(it.guessId);
            }
            return false;
        }

        public IReadOnlyDictionary<string, List<Clue>> StagedToday => stagedToday;
    }
}
