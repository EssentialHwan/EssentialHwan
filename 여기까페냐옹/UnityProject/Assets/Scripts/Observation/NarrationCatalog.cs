using System.Collections.Generic;
using YeogiCafe.Data;

namespace YeogiCafe.Observation
{
    // 관찰 서술 텍스트 매핑(문서 01 §D). 시설/좌석 태그 → 로컬라이즈 키.
    // 관찰 밀도 강화: 시설별로 다른 서술이 나와 "무엇을 하는지"가 더 잘 읽힌다.
    // 취향 미언급 — 관찰 가능한 행동만.
    public static class NarrationCatalog
    {
        // 좌석 서술 키 (좌석 태그 우선순위 순)
        public static string SeatKey(IReadOnlyList<SeatTag> tags, bool fallback)
        {
            if (fallback) return "narr.seat.fallback";
            if (Has(tags, SeatTag.Window) || Has(tags, SeatTag.OutsideView)) return "narr.seat.window";
            if (Has(tags, SeatTag.Corner)) return "narr.seat.corner";
            return "narr.seat.generic";
        }

        // 시설 서술 키: 가구 종류가 분명하면 그 가구의 행동(캣휠에서 '스크래처를 긁는다'가 뜨던 문제), 아니면 태그로 추정
        public static string FacilityKey(string facilityId, IReadOnlyList<FacilityTag> tags)
        {
            string id = string.IsNullOrEmpty(facilityId) ? "" : facilityId.Split('#')[0];
            switch (id)
            {
                case "furn_fac_wheel":   return "narr.facility.wheel";
                case "furn_fac_wand":    return "narr.facility.wand";
                case "furn_fac_scratch": return "narr.facility.scratch";
                case "furn_fac_tower":   return "narr.facility.tower";
                case "furn_fac_cushion": return "narr.facility.cushion";
                case "furn_fac_toybox":  return "narr.facility.toybox";
                case "furn_fac_fruitbox": return "narr.facility.box";
                case "furn_fac_heatpad": return "narr.facility.warm";
            }
            return FacilityKey(tags);
        }

        // 시설 서술 키 (시설 태그 → 대표 행동)
        public static string FacilityKey(IReadOnlyList<FacilityTag> tags)
        {
            if (Has(tags, FacilityTag.Height)) return "narr.facility.tower";
            if (Has(tags, FacilityTag.Soft) || Has(tags, FacilityTag.Relax)) return "narr.facility.cushion";
            if (Has(tags, FacilityTag.Social) && Has(tags, FacilityTag.Play)) return "narr.facility.toybox";
            if (Has(tags, FacilityTag.Play)) return "narr.facility.play";      // 어떤 놀이 기구인지 모를 땐 중립 문장
            if (Has(tags, FacilityTag.Natural)) return "narr.facility.plant";
            return "narr.facility.generic";
        }

        // 음식 서술 키 (완식 속도)
        public static string FoodKey(bool fast) => fast ? "narr.food.fast" : "narr.food.slow";

        static bool Has<T>(IReadOnlyList<T> list, T v)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
                if (EqualityComparer<T>.Default.Equals(list[i], v)) return true;
            return false;
        }
    }
}
