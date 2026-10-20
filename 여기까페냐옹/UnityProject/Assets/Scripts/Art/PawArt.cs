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

        // 발끝(맨 위 불투명 줄)의 칸 기준 높이 비율 — 피벗을 발끝에 맞출 때
        public static float TipY(int frame)
        {
            Load(); if (tex == null) return 0.92f;
            int col = frame % Cols, row = frame / Cols;
            int x0 = col * cw, y0 = (Rows - 1 - row) * ch;
            var px = tex.GetPixels32();
            for (int y = ch - 1; y >= 0; y--)
                for (int x = 0; x < cw; x++)
                    if (px[(y0 + y) * tex.width + x0 + x].a > 0) return (y + 1f) / ch;
            return 0.92f;
        }

        // 작게 줄인 그림(커서 등): 칸을 factor배 줄여 면적 평균 → 도트가 뭉개지지 않게 Point 필터
        static readonly Dictionary<int, Sprite> small = new();
        public static Sprite Small(int frame, bool left, int factor)
        {
            Load();
            if (tex == null) return null;
            int key = (frame * 2 + (left ? 1 : 0)) * 16 + factor;
            if (small.TryGetValue(key, out var s) && s != null) return s;
            var src = (left ? texFlip : tex).GetPixels32();
            int W = (left ? texFlip : tex).width;
            int col = frame % Cols, row = frame / Cols, x0 = col * cw, y0 = (Rows - 1 - row) * ch;
            int sw = cw / factor, sh = ch / factor;
            var o = new Color32[sw * sh];
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                {
                    float r = 0, g = 0, b = 0, a = 0; int n = factor * factor;
                    for (int dy = 0; dy < factor; dy++)
                        for (int dx = 0; dx < factor; dx++)
                        {
                            var c = src[(y0 + y * factor + dy) * W + x0 + x * factor + dx];
                            float al = c.a / 255f; r += c.r * al; g += c.g * al; b += c.b * al; a += al;
                        }
                    if (a / n < 0.45f) { o[y * sw + x] = new Color32(0, 0, 0, 0); continue; }
                    o[y * sw + x] = new Color32((byte)(r / a), (byte)(g / a), (byte)(b / a), 255);
                }
            var t = new Texture2D(sw, sh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(o); t.Apply();
            s = Sprite.Create(t, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            small[key] = s;
            return s;
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
