using System;
using YeogiCafe.Save;

namespace YeogiCafe.Data
{
    // 사양서 §24/§32 — 해금 조건(데이터 주도). 고양이·메뉴·가구 공용.
    // 코드 수정 없이 조건만 데이터로. 순수 평가(테스트 용이).
    public enum UnlockType { Always, CafeLevel, DiscoveredCats, RecordedAxesTotal, RegularCount, DayReached }

    [Serializable]
    public class UnlockCondition
    {
        public UnlockType type = UnlockType.Always;
        public int value;

        // save 상태 기준 해금 여부 판정
        public bool IsMet(SaveGame save)
        {
            if (save == null) return type == UnlockType.Always;
            switch (type)
            {
                case UnlockType.Always: return true;
                case UnlockType.CafeLevel: return save.cafeLevel >= value;
                case UnlockType.DiscoveredCats: return CountDiscovered(save) >= value;
                case UnlockType.RecordedAxesTotal: return CountRecorded(save) >= value;
                case UnlockType.RegularCount: return CountRegulars(save) >= value;
                case UnlockType.DayReached: return save.currentDay >= value;
                default: return true;
            }
        }

        static int CountDiscovered(SaveGame s) { int n = 0; foreach (var c in s.cats) if (c.visitCount > 0) n++; return n; }
        static int CountRecorded(SaveGame s) { int n = 0; foreach (var c in s.cats) n += c.observation.RecordedAxisCount(); return n; }
        static int CountRegulars(SaveGame s) { int n = 0; foreach (var c in s.cats) if (c.regularStage >= 2) n++; return n; }
    }
}
