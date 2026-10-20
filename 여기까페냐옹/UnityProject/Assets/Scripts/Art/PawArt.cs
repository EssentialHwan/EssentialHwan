using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 1인칭 앞발 원화(Resources/PawArt/paws.bytes = PNG, 4×3 같은 크기 칸, 발 바닥선 맞춤).
    // 1줄 손등: 0 기본 · 1 누름 · 2 꾹꾹(폄) · 3 꾹꾹(오므림)
    // 2줄 그루밍(혀 포함): 4 들어올림 · 5 혀 닿음 · 6 핥기 · 7 젖은 털 반짝
    // 3줄 쭙쭙이(입에 문 발): 8 오므림 · 9 폄 · 10 하트 · 11 폄(반복용)
    // 원화는 오른발 기준 → 왼발은 좌우 반전 텍스처. 없으면 null(기존 코드 그림으로).
    public static class PawArt
    {
        public const int Idle = 0, Press = 1, Spread = 2, Curl = 3, Groom0 = 4, Groom1 = 5, Groom2 = 6, Groom3 = 7, Suck0 = 8, Suck1 = 9, Suck2 = 10, Suck3 = 11;
        const int Cols = 4, Rows = 3;
        static bool tried;
        static Texture2D tex, texFlip;
        static int cw, ch;
        static readonly Dictionary<int, Sprite> cache = new();

        public static bool Has { get { Load(); return tex != null; } }
        public static Vector2 CellSize { get { Load(); return new Vector2(cw, ch); } }

        static void Load()
        {
            if (tried) return;
            tried = true;
            var ta = Resources.Load<TextAsset>("PawArt/paws");
            if (ta == null) return;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!t.LoadImage(ta.bytes)) return;
            t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
            tex = t; cw = t.width / Cols; ch = t.height / Rows;
            // 왼발용 좌우 반전(칸마다 반전 → 같은 칸 배치 유지)
            var src = t.GetPixels32(); var dst = new Color32[src.Length];
            int W = t.width, H = t.height;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int cx = x / cw, lx = x % cw;
                    dst[y * W + cx * cw + (cw - 1 - lx)] = src[y * W + x];
                }
            texFlip = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texFlip.SetPixels32(dst); texFlip.Apply();
        }

        public static Sprite Get(int frame, bool left)
        {
            Load();
            if (tex == null) return null;
            int key = frame * 2 + (left ? 1 : 0);
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            int col = frame % Cols, row = frame / Cols;
            var rect = new Rect(col * cw, (Rows - 1 - row) * ch, cw, ch);       // 텍스처는 아래가 0
            s = Sprite.Create(left ? texFlip : tex, rect, new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "paw_art_" + key;
            cache[key] = s;
            return s;
        }
    }
}
