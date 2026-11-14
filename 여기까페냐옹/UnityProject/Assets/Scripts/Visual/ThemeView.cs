using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Art;

namespace YeogiCafe.Visual
{
    // 인테리어 테마 적용: 씬의 Room 타일(바닥·벽지·징두리) 스프라이트를 테마 타일로 바꾼다. 기본 테마면 원래대로.
    public static class ThemeView
    {
        static readonly Dictionary<SpriteRenderer, Sprite> original = new();
        static readonly Dictionary<string, Sprite> cache = new();
        static string shown;
        static int roomId;

        static Sprite Tile(string key, System.Func<PixelCanvas> draw)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var c = draw();
            var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = key };
            tex.SetPixels32(c.Pixels); tex.Apply();
            s = YeogiCafe.Art.LitSprites.Create(tex, new Rect(0, 0, c.W, c.H), Vector2.zero, 32f);   // 씬 타일과 같은 왼쪽 아래 피벗
            cache[key] = s;
            return s;
        }

        public static Sprite Swatch(string theme) => RuntimeSprites.Get("theme_swatch_" + theme, () => ThemeDrawing.Swatch(theme));

        public static void Apply(string theme, bool force = false)
        {
            if (string.IsNullOrEmpty(theme)) theme = "default";
            var room = GameObject.Find("Room");
            if (room == null) return;
            if (room.GetInstanceID() != roomId) { Reset(); roomId = room.GetInstanceID(); }   // 씬을 다시 불러왔으면 처음부터
            if (!force && shown == theme) return;
            shown = theme;
            Sprite floor = null, wall = null, wains = null;
            if (theme != "default")
            {
                floor = Tile("theme_floor_" + theme, () => ThemeDrawing.Floor(theme));
                wall = Tile("theme_wall_" + theme, () => ThemeDrawing.Wall(theme));
                wains = Tile("theme_wains_" + theme, () => ThemeDrawing.Wainscot(theme));
            }
            foreach (var sr in room.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || !sr.name.StartsWith("tile_")) continue;
                if (!original.ContainsKey(sr)) original[sr] = sr.sprite;
                Sprite pick = null;
                if (sr.sortingOrder == YSort.FloorTiles) pick = floor;
                else if (sr.sortingOrder == YSort.WallTiles) pick = wall;
                else if (sr.sortingOrder == YSort.WallTiles + 1) pick = wains;
                else continue;
                sr.sprite = pick != null ? pick : original[sr];
            }
        }

        public static void Reset() { shown = null; original.Clear(); }
    }
}
