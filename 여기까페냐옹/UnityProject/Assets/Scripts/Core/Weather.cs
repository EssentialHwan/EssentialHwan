using YeogiCafe.Save;

namespace YeogiCafe.Core
{
    // 오늘의 날씨(동물의 숲 참고, 계절 없이 날씨만). 행동에만 영향 — 관찰 단서 가중치는 건드리지 않는다(추리 공정성).
    //  맑음: 더 돌아다니고 우다다가 잦음 / 흐림: 보통 / 비: 덜 돌아다니고 일찍 식빵·낮잠, 창밖 비 구경
    public enum WeatherKind { Sunny = 0, Cloudy = 1, Rain = 2 }

    public static class Weather
    {
        public static WeatherKind Current { get; private set; } = WeatherKind.Sunny;

        public static void EnsureToday(SaveGame s)
        {
            if (s == null) return;
            if (s.weatherDay != s.currentDay)
            {
                s.weatherDay = s.currentDay;
                // 첫날은 맑음(튜토리얼), 이후 맑음 50% / 흐림 25% / 비 25%
                var r = new System.Random(s.currentDay * 7919 + 13).NextDouble();
                s.weather = s.currentDay <= 1 ? 0 : r < 0.5 ? 0 : r < 0.75 ? 1 : 2;
            }
            Current = (WeatherKind)s.weather;
        }

        // 개발용: 오늘 날씨를 다음 것으로(맑음→흐림→비)
        public static void DebugCycle(SaveGame s)
        {
            if (s == null) return;
            s.weatherDay = s.currentDay; s.weather = (s.weather + 1) % 3; Current = (WeatherKind)s.weather;
        }

        public static float RoamBonus => Current == WeatherKind.Sunny ? 0.1f : Current == WeatherKind.Rain ? -0.2f : 0f;
        public static float ZoomiesMult => Current == WeatherKind.Sunny ? 1.5f : Current == WeatherKind.Rain ? 0.4f : 1f;
        public static float RestMult => Current == WeatherKind.Rain ? 1.35f : 1f;
        public static bool SleepyDay => Current == WeatherKind.Rain;
    }
}
