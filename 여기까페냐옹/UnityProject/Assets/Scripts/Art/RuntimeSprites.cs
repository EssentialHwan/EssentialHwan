using System;
using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 코드로 그린 PixelCanvas를 런타임 Sprite로 변환·캐시. (주방 UI처럼 씬에 미리 배치하지 않는 리소스용)
    // 정식 아트 교체: Resources/ArtOverride/{key}.png 가 있으면 그것을 우선 사용한다.
    public static class RuntimeSprites
    {
        static readonly Dictionary<string, Sprite> cache = new();

        public static Sprite Get(string key, Func<PixelCanvas> draw)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = Resources.Load<Sprite>("ArtOverride/" + key);
            if (s == null)
            {
                var c = draw();
                var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,          // 도트 선명하게
                    wrapMode = TextureWrapMode.Clamp,
                    name = key
                };
                tex.SetPixels32(c.Pixels);
                tex.Apply();
                s = Sprite.Create(tex, new Rect(0, 0, c.W, c.H), new Vector2(0.5f, 0f), 32f, 0, SpriteMeshType.FullRect);
                s.name = key;
            }
            cache[key] = s;
            return s;
        }
    }
}
