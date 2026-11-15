namespace YeogiCafe.Data
{
    // 인테리어 테마: 벽지·바닥·징두리를 통째로 바꾼다. 테마마다 분위기 축이 하나 있어,
    // 그 분위기를 좋아하는 고양이(CatData.atmospherePreference)가 더 만족하고 말풍선으로 티를 낸다.
    public static class ThemeBook
    {
        public struct Theme { public string id; public int price; public AtmosphereAxis axis; public bool none; }
        public static readonly Theme[] All =
        {
            new Theme { id = "default", price = 0, none = true },
            new Theme { id = "nordic", price = 800, axis = AtmosphereAxis.Natural },
            new Theme { id = "tatami", price = 1000, axis = AtmosphereAxis.Quiet },
            new Theme { id = "retro", price = 1200, axis = AtmosphereAxis.Luxury },
            new Theme { id = "candy", price = 1200, axis = AtmosphereAxis.Lively },
            new Theme { id = "cabin", price = 1500, axis = AtmosphereAxis.Warm },
        };

        public static Theme Get(string id)
        {
            foreach (var t in All) if (t.id == id) return t;
            return All[0];
        }

        public const float Bonus = 8f;
        public static bool Matches(string themeId, AtmosphereAxis pref)
        {
            var t = Get(themeId);
            return !t.none && t.axis == pref;
        }
    }
}
