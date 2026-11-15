using System;
using YeogiCafe.Save;

namespace YeogiCafe.Data
{
    // 사양서 §24/§32 — 해금 조건(데이터 주도). 고양이·메뉴·가구 공용.
    // 코드 수정 없이 조건만 데이터로. 순수 평가(테스트 용이).
    public enum UnlockType { Always, CafeLevel, DiscoveredCats, RecordedAxesTotal, RegularCount, DayReached, FurnitureSet, Shelter }

    [Serializable]
    public class UnlockCondition
    {
        public UnlockType type = UnlockType.Always;
        public int value;
        public string[] requireFurniture;   // FurnitureSet: 이 가구들이 모두 놓여 있고 카페 Lv ≥ value (숨은 고양이 소환 조건)

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
                case UnlockType.Shelter: return ShelterBook.TierOf(save.shelterDonated) >= value;   // 보호소 후원 단계
                case UnlockType.FurnitureSet:
                    if (save.cafeLevel < value) return false;
                    if (requireFurniture != null)
                        foreach (var id in requireFurniture)
                            if (!save.placedFurniture.Exists(p => p.furnitureId == id)) return false;
                    return true;
                default: return true;
            }
        }

        // 발견 = 취향을 하나라도 기록한 고양이(방문만으론 X) — 도감 '발견 n/9'·카페 성장 조건과 같은 기준
        static int CountDiscovered(SaveGame s) { int n = 0; foreach (var c in s.cats) if (c.observation.RecordedAxisCount() > 0) n++; return n; }
        static int CountRecorded(SaveGame s) { int n = 0; foreach (var c in s.cats) n += c.observation.RecordedAxisCount(); return n; }
        static int CountRegulars(SaveGame s) { int n = 0; foreach (var c in s.cats) if (c.regularStage >= 2) n++; return n; }
    }
}
