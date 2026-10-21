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
            string menu = v.menu != null ? "@menu." + v.menu.menuId : "";
            var parts = new List<string>();
            if (v.orderFailed) { parts.Add(Pick("review.fail")); paws = 1; }
            else if (v.wrongDish) parts.Add(Pick("review.wrong", ("menu", menu)));
            else if (v.forcedSecond) parts.Add(Pick("review.nofav", ("menu", menu)));          // 먹고 싶은 메뉴가 없었다 → 레시피 힌트
            else if (v.menu != null) parts.Add(Pick(v.stars >= 3 ? "review.star3" : v.stars == 2 ? "review.star2" : "review.star1", ("menu", menu)));

            var extra = new List<string>();
            if (v.preferredSeat) extra.Add(Pick("review.seat", ("seat", $"@tag.seat.{v.cat.seatPreference}")));
            if (v.preferredFacility) extra.Add(Pick("review.fac", ("fac", $"@tag.fac.{v.cat.facilityPreference}")));
            if (v.mouse == 1) extra.Add(Pick("review.mouse.caught"));
            if (v.mouse == 2) extra.Add(Pick("review.mouse.escaped"));
            if (s.nyanStars >= 3 && paws >= 4) extra.Add(Pick("review.nyan"));
            if (extra.Count > 0 && (parts.Count == 0 || Random.value < 0.7f)) parts.Add(extra[Random.Range(0, extra.Count)]);

            var r = new ReviewDTO { day = v.day, catId = v.cat.catId, paws = paws, parts = parts };
            r.text = Text(r);
            s.reviews.Add(r);
            if (s.reviews.Count > Keep) s.reviews.RemoveRange(0, s.reviews.Count - Keep);
            return r;
        }

        // 지금 언어로 리뷰 문장을 그린다. 구버전(문장만 저장된) 리뷰는 저장된 그대로.
        public static string Text(ReviewDTO r)
        {
            if (r == null) return "";
            if (r.parts == null || r.parts.Count == 0) return r.text ?? "";
            var sb = new System.Text.StringBuilder();
            foreach (var code in r.parts)
            {
                var seg = code.Split('|');
                var args = new (string, string)[seg.Length - 1];
                for (int i = 1; i < seg.Length; i++)
                {
                    int eq = seg[i].IndexOf('=');
                    string name = eq > 0 ? seg[i].Substring(0, eq) : seg[i], val = eq > 0 ? seg[i].Substring(eq + 1) : "";
                    if (val.StartsWith("@")) val = L.Get(val.Substring(1));
                    args[i - 1] = (name, val);
                }
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(L.Get(seg[0], args));
            }
            return sb.ToString();
        }

        // 구버전 리뷰(문장만 저장)는 그 문장을 쓴 언어 화면에서만 보인다 — 다른 언어 목록에 섞이지 않게
        public static bool Visible(ReviewDTO r) =>
            r != null && ((r.parts != null && r.parts.Count > 0) || ScriptOf(r.text) == L.Current);

        // 문장에 쓰인 글자로 언어 추정: 한글 → 한국어, 가나 → 일본어, 한자만 → 중국어, 그 밖 → 영어
        static Language ScriptOf(string s)
        {
            if (string.IsNullOrEmpty(s)) return L.Current;
            bool han = false;
            foreach (char c in s)
            {
                if (c >= 0xAC00 && c <= 0xD7A3) return Language.Ko;
                if (c >= 0x3040 && c <= 0x30FF) return Language.Ja;
                if (c >= 0x4E00 && c <= 0x9FFF) han = true;
            }
            return han ? Language.Zh : Language.En;
        }

        static string Pick(string key, params (string, string)[] args)
        {
            // 같은 뜻 문장 3가지 중 하나(key.1~3, 없으면 key) — 키와 인자만 기록
            string k = $"{key}.{Random.Range(1, 4)}";
            if (!L.Has(k)) k = key;          // 변형 문장이 없는 키(1개짜리)는 기본 키로 — 'missing key' 경고 없이
            var sb = new System.Text.StringBuilder(k);
            foreach (var (n, v) in args) sb.Append('|').Append(n).Append('=').Append(v);
            return sb.ToString();
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

        public static string Paws(int n) { n = UnityEngine.Mathf.Clamp(n, 0, 5); return (n > 0 ? "<color=#E0588A>" + new string('♥', n) + "</color>" : "") + (n < 5 ? "<color=#D8C8CC>" + new string('♥', 5 - n) + "</color>" : ""); }
    }
}
