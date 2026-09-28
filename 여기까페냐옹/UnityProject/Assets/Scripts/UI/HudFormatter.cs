using YeogiCafe.Data;
using YeogiCafe.Loc;

namespace YeogiCafe.UI
{
    // HUD 표시 문자열 포맷(로직). UI 위젯과 분리 → 테스트 가능.
    public static class HudFormatter
    {
        public static string Gold(int g) => $"💰 {g}G";

        public static string Phase(DayPhase p) => p switch
        {
            DayPhase.Morning => "🌅 아침",
            DayPhase.Lunch => "☀ 점심",
            DayPhase.Evening => "🌆 저녁",
            DayPhase.Settlement => "📊 정산",
            _ => "준비"
        };

        public static string Guests(int n) => L.Get("ui.cafe.customers", ("n", n.ToString()));

        public static string RegularBadge(int stage)
        {
            if (stage <= 1) return "";
            string stars = new string('★', stage - 1) + new string('☆', 5 - (stage - 1));
            return L.Get("ui.regular.stage", ("s", (stage - 1).ToString())) + " " + stars;
        }

        // 남은 시간 바 비율 (0~1)
        public static float TimeProgress(float dayTimer, float dayLength)
            => dayLength <= 0 ? 0 : UnityEngine.Mathf.Clamp01(dayTimer / dayLength);
    }
}
