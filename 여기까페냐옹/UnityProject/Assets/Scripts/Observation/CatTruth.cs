using YeogiCafe.Data;

namespace YeogiCafe.Observation
{
    // 추리 정답 판정 전용(Truth 접근은 여기만). 세이브·UI 표시에는 쓰지 않는다.
    public static class CatTruth
    {
        public static bool IsFood(CatData cat, MenuData menu) =>
            cat != null && menu != null && menu.menuId == cat.foodFavoriteMenuId;

        // 좌석/시설은 '그 고양이가 좋아하는 성질'을 가진 가구면 정답
        public static bool IsSeat(CatData cat, FurnitureData f)
        {
            if (cat == null || f == null || f.seatTags == null) return false;
            foreach (var t in f.seatTags) if (t == cat.seatPreference) return true;
            return false;
        }

        public static bool IsFacility(CatData cat, FurnitureData f)
        {
            if (cat == null || f == null || f.facilityTags == null) return false;
            foreach (var t in f.facilityTags) if (t == cat.facilityPreference) return true;
            return false;
        }
    }
}
