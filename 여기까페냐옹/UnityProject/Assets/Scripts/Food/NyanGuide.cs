using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Food
{
    // 냥슐랭 가이드: 변장한 심사원 고양이가 가끔 손님으로 온다. 그가 받은 음식 별·서빙 속도·가구·쥐 소동을 종합 채점해
    // 기준을 넘으면 냥슐랭 스타가 하나 오른다(0~5, 내려가지 않음). 스타가 오를수록 모든 손님의 만족·결제·재방문이 늘어난다.
    public static class NyanGuide
    {
        public const string InspectorId = "cat_inspector";
        public const int MaxStars = 5;
        static readonly int[] PassScore = { 50, 60, 70, 80, 88 };     // 현재 스타 n → n+1 에 필요한 총점

        // ── 스타 보너스 (밸런스) ──
        public static float SatisfactionBonus(int stars) => 2.5f * stars;                  // 5★ +12.5
        public static float PayMult(int stars) => 1f + 0.06f * stars;                      // 5★ ×1.30
        public static float RevisitBonus(int stars) => 0.03f * stars;                      // 5★ +0.15
        public static float SpawnIntervalMult(int stars) => 1f - 0.04f * stars;            // 5★ 간격 −20%

        // ── 방문 확률 ── 3일차부터. 기본 18% + 평판 보너스 + 안 온 날마다 4%, 8일 넘게 안 왔으면 확정.
        public static bool RollVisit(SaveGame s, int day, float reputationBonus)
        {
            if (s == null || day < 3 || s.nyanStars >= MaxStars) return false;
            int since = s.inspectorLastDay < 0 ? day : day - s.inspectorLastDay;
            if (since < 2) return false;                                                   // 이틀 연속은 안 옴
            if (since >= 8) return true;
            return Random.value < 0.18f + reputationBonus + 0.04f * since;
        }

        static CatData data;
        public static CatData Data
        {
            get
            {
                if (data != null) return data;
                data = ScriptableObject.CreateInstance<CatData>();
                data.catId = InspectorId; data.displayNameKey = "cat.cat_inspector.name"; data.name = InspectorId;
                data.personalityTags = new[] { PersonalityTag.Curious, PersonalityTag.Introvert };
                data.foodFavoriteMenuId = ""; data.seatPreference = SeatTag.Normal;
                data.facilityPreference = FacilityTag.Quiet; data.atmospherePreference = AtmosphereAxis.Quiet;
                data.unlockCondition = new UnlockCondition { type = UnlockType.Always };
                return data;
            }
        }

        public static bool IsInspector(string catId) => catId == InspectorId;

        // ── 채점 ──
        // dishStars: 심사원이 받은 음식 별(0=못 받음) · serveWait: 주문→서빙(게임 초) · placed: 놓인 가구 수
        public static InspectorReportDTO Evaluate(SaveGame s, int day, int dishStars, bool wrongDish, float serveWait, int placed)
        {
            var r = new InspectorReportDTO { day = day, starsBefore = s.nyanStars };
            r.dish = dishStars <= 0 ? 0 : wrongDish ? 20 : dishStars == 1 ? 35 : dishStars == 2 ? 70 : 100;
            r.speed = dishStars <= 0 ? 0 : Mathf.RoundToInt(Mathf.Clamp01(1f - (serveWait - 25f) / 50f) * 100f);   // 25초 이내 만점, 75초면 0
            r.furniture = Mathf.Clamp(FurnitureStars.PlacedStarSum(s) * 4 + s.cafeLevel * 12, 0, 100);   // 가구 수 × 별 + 카페 레벨
            int mouse = s.mouseStateDay == day ? s.mouseState : 0;
            r.clean = mouse == 0 ? 100 : mouse == 1 ? 85 : 30;
            r.total = Mathf.RoundToInt(r.dish * 0.35f + r.speed * 0.25f + r.furniture * 0.25f + r.clean * 0.15f);
            bool pass = s.nyanStars < MaxStars && r.total >= PassScore[s.nyanStars];
            if (pass) s.nyanStars++;
            r.starsAfter = s.nyanStars;
            s.lastReport = r; s.inspectorLastDay = day;
            return r;
        }

        public static int NextPassScore(int stars) => stars >= MaxStars ? 0 : PassScore[stars];
        public static string Stars(int n) => new string('★', n) + new string('☆', MaxStars - n);
    }
}
