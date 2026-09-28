using System.Collections.Generic;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 정산 화면 관찰 카드의 '데이터'만 정의(뷰와 분리). MonoBehaviour UI는 이 모델을 렌더.
    // Truth 접근 금지 — ObservationRecord만 읽는다.
    public struct AxisCardData
    {
        public PrefAxis axis;
        public ObsState state;        // ?/…/★/✓
        public string topTargetLabel; // 근거(가장 강한 단서 대상)
        public bool canRecord;        // Confirmable에서만 true
    }

    public struct CatCardData
    {
        public string catId;
        public string displayNameKey;
        public string personality1;   // 즉시 공개
        public string personality2;   // 관찰 공개(미공개면 "?")
        public List<string> behaviorSummary; // 오늘 행동 요약 텍스트
        public AxisCardData food, seat, facility;
    }

    public static class ObservationCardBuilder
    {
        // 정산 시 CatSaveData + 성격 공개상태 → 카드 데이터. (성격2는 부록 J-C 공개 규칙)
        public static CatCardData Build(CatData data, CatSaveData save, List<string> todaySummary)
        {
            return new CatCardData
            {
                catId = data.catId,
                displayNameKey = data.displayNameKey,
                personality1 = data.personalityTags.Length > 0 ? data.personalityTags[0].ToString() : "?",
                personality2 = save.personality2Revealed && data.personalityTags.Length > 1
                               ? data.personalityTags[1].ToString() : "?",
                behaviorSummary = todaySummary ?? new List<string>(),
                food = Axis(PrefAxis.Food, save.observation.food),
                seat = Axis(PrefAxis.Seat, save.observation.seat),
                facility = Axis(PrefAxis.Facility, save.observation.facility)
            };
        }

        static AxisCardData Axis(PrefAxis axis, AxisRecord rec)
        {
            var top = rec.Top();
            return new AxisCardData
            {
                axis = axis,
                state = rec.state,
                topTargetLabel = rec.state == ObsState.Recorded ? rec.recordedValue : (top?.target ?? "?"),
                canRecord = rec.state == ObsState.Confirmable
            };
        }

        public static string StateIcon(ObsState s) => s switch
        {
            ObsState.Unknown => "?",
            ObsState.Suspected => "…",
            ObsState.Confirmable => "★",
            ObsState.Recorded => "✓",
            _ => "?"
        };
    }
}
