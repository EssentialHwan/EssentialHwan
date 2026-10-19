using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Loc;
using YeogiCafe.Save;

namespace YeogiCafe.Food
{
    // 고양이 손님 한 줄 리뷰. 먹은 메뉴·별, 앉은 자리, 논 시설, 쥐 소동 이야기를 냥체로 남긴다.
    // 좋은 자리·시설 이야기는 그 고양이 취향의 약한 힌트가 된다(정답 기록은 아님).
    // 평판(최근 20개 평균 발바닥) → 다음 날 손님 재방문·스폰, 냥슐랭 심사원 방문 확률↑.
    public static class ReviewBook
    {
        public const int Keep = 40, Window = 20;

        public struct Visit
        {
            public CatData cat; public int day; public float satisfaction;
            public MenuData menu; public int stars; public bool favorite, forcedSecond, wrongDish, orderFailed;
            public bool preferredSeat, preferredFacility; public int mouse;      // 0 없음 1 잡음 2 놓침
        }

        public static ReviewDTO Write(SaveGame s, in Visit v)
        {
            int paws = Mathf.Clamp(Mathf.RoundToInt(v.satisfaction / 20f), 1, 5);
            string menu = v.menu != null ? L.Get("menu." + v.menu.menuId) : "";
            var parts = new List<string>();
            if (v.orderFailed) { parts.Add(Pick("review.fail")); paws = 1; }
            else if (v.wrongDish) parts.Add(Pick("review.wrong", ("menu", menu)));
            else if (v.forcedSecond) parts.Add(Pick("review.nofav", ("menu", menu)));          // 먹고 싶은 메뉴가 없었다 → 레시피 힌트
            else if (v.menu != null) parts.Add(Pick(v.stars >= 3 ? "review.star3" : v.stars == 2 ? "review.star2" : "review.star1", ("menu", menu)));

            var extra = new List<string>();
            if (v.preferredSeat) extra.Add(Pick("review.seat", ("seat", L.Get($"tag.seat.{v.cat.seatPreference}"))));
            if (v.preferredFacility) extra.Add(Pick("review.fac", ("fac", L.Get($"tag.fac.{v.cat.facilityPreference}"))));
            if (v.mouse == 1) extra.Add(Pick("review.mouse.caught"));
            if (v.mouse == 2) extra.Add(Pick("review.mouse.escaped"));
            if (s.nyanStars >= 3 && paws >= 4) extra.Add(Pick("review.nyan"));
            if (extra.Count > 0 && (parts.Count == 0 || Random.value < 0.7f)) parts.Add(extra[Random.Range(0, extra.Count)]);

            var r = new ReviewDTO { day = v.day, catId = v.cat.catId, paws = paws, text = string.Join(" ", parts) };
            s.reviews.Add(r);
            if (s.reviews.Count > Keep) s.reviews.RemoveRange(0, s.reviews.Count - Keep);
            return r;
        }

        static string Pick(string key, params (string, string)[] args)
        {
            // 같은 뜻 문장 3가지 중 하나(key.1~3, 없으면 key)
            string k = $"{key}.{Random.Range(1, 4)}";
            string t = L.Get(k, args);
            if (t == k) t = L.Get(key, args);
            return t;
        }

        // 평판 1~5 (리뷰가 적으면 3에 가깝게)
        public static float Reputation(SaveGame s)
        {
            if (s == null || s.reviews.Count == 0) return 3f;
            int n = Mathf.Min(Window, s.reviews.Count); float sum = 0;
            for (int i = s.reviews.Count - n; i < s.reviews.Count; i++) sum += s.reviews[i].paws;
            float avg = sum / n;
            return Mathf.Lerp(3f, avg, n / (float)Window);
        }

        // 평판 보너스: 3.5 이상부터. (재방문 확률 가산, 심사원 방문 확률 가산, 스폰 간격 배수)
        public static float RevisitBonus(float rep) => rep >= 4.5f ? 0.15f : rep >= 4f ? 0.1f : rep >= 3.5f ? 0.05f : 0f;
        public static float InspectorBonus(float rep) => rep >= 4.5f ? 0.15f : rep >= 4f ? 0.1f : rep >= 3.5f ? 0.05f : 0f;
        public static float SpawnIntervalMult(float rep) => Mathf.Clamp(1f - (rep - 3f) * 0.08f, 0.84f, 1.1f);

        public static string Paws(int n) => new string('●', n) + new string('○', 5 - n);
    }
}
