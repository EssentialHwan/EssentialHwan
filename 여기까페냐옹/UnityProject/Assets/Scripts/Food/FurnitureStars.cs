using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Food
{
    // 가구 강화(★1~3, 같은 종류 전부에 적용). 고양이들이 그 가구를 충분히 쓴 뒤 골드를 내면 별이 오른다.
    // 효과: 그 자리/시설을 쓴 손님 만족 +4/★, 냥슐랭 가구 점수↑.
    public static class FurnitureStars
    {
        public const int Max = 3;
        static readonly int[] UseReq = { 0, 6, 18 };          // ★1→2: 6번, ★2→3: 18번 사용
        static readonly float[] CostMul = { 0, 0.8f, 1.8f };   // × 가구 가격

        public static int Stars(SaveGame s, string id) => s == null || string.IsNullOrEmpty(id) ? 1 : Mathf.Clamp(s.GetFurnStat(id).stars, 1, Max);
        public static int NeedUses(int stars) => stars >= Max ? int.MaxValue : UseReq[stars];
        public static int Cost(FurnitureData d, int stars) => stars >= Max ? 0 : Mathf.RoundToInt(d.price * CostMul[stars] / 10f) * 10;
        public static float Satisfaction(int stars) => 4f * (Mathf.Clamp(stars, 1, Max) - 1);

        public static void CountUse(SaveGame s, string id) { if (s != null && !string.IsNullOrEmpty(id)) s.GetFurnStat(id).uses++; }

        public static bool CanUpgrade(SaveGame s, FurnitureData d, int gold, out int cost)
        {
            var st = s.GetFurnStat(d.furnitureId);
            cost = Cost(d, st.stars);
            return st.stars < Max && st.uses >= NeedUses(st.stars) && gold >= cost;
        }

        // 놓인 가구들의 별 합(냥슐랭 가구 점수용)
        public static int PlacedStarSum(SaveGame s)
        {
            int n = 0;
            foreach (var p in s.placedFurniture) n += Stars(s, p.furnitureId);
            return n;
        }

        public static string StarText(int stars) => new string('★', stars) + new string('☆', Max - stars);
    }
}
