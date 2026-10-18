using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 도트 시트 고양이(4×4 = 16포즈, 칸 56×56). Resources/CatSheets/{catId}.png.bytes 가 있으면 리그 대신 이것을 쓴다.
    // 포즈 번호: 0 앉기 · 1 윙크 · 2 눈웃음 · 3 야옹 / 4~7 걷기 / 8 식빵 · 9 잠 · 10 그루밍 · 11 먹기 / 12 엉덩이 들기 · 13 기지개 · 14 웅크림 · 15 꾹꾹이
    // 클립(CatClips 이름)마다 포즈 번호와 살짝 흔들기(dx,dy)·눕히기(rot)를 조합해 프레임을 만든다.
    public static class CatSheet
    {
        public const int Cell = 56;

        struct K { public int f, dx, dy; public bool rot; public K(int f, int dx = 0, int dy = 0, bool rot = false) { this.f = f; this.dx = dx; this.dy = dy; this.rot = rot; } }
        static K[] Seq(params int[] f) { var a = new K[f.Length]; for (int i = 0; i < f.Length; i++) a[i] = new K(f[i]); return a; }
        static K[] Rep(int f, int n, int dx = 0, int dy = 0)
        {
            var a = new K[n];
            for (int i = 0; i < n; i++) a[i] = new K(f, i % 2 == 1 ? dx : 0, i % 2 == 1 ? dy : 0);
            return a;
        }

        static readonly Dictionary<string, K[]> Map = new()
        {
            ["walk"] = Seq(4, 5, 6, 7, 4, 5, 6, 7),
            ["stand"] = Seq(4, 4, 4, 4, 4, 4, 4, 4),
            ["run"] = new[] { new K(4), new K(5, 0, 2), new K(6, 0, 1), new K(7), new K(5, 0, 2), new K(6, 0, 1) },
            ["sit"] = Seq(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2),
            ["loaf"] = Seq(8, 8, 8, 8, 8, 8, 8, 8),
            ["sleep"] = Seq(9, 9, 9, 9, 9, 9),
            ["groom"] = Rep(10, 8, 0, 1),
            ["stretch"] = Seq(0, 13, 13, 13, 13, 13, 13, 13, 0, 0),
            ["order"] = Seq(0, 0, 3, 3, 3, 0, 0, 0),
            ["eat"] = Rep(11, 6, 0, -1),
            ["sitdown"] = Seq(4, 0, 0, 0, 0),
            ["liedown"] = Seq(0, 0, 8, 8, 8),
            ["curlup"] = Seq(8, 8, 9, 9, 9),
            ["rub"] = new[] { new K(2), new K(2, 1), new K(1, 1), new K(1), new K(2), new K(2, -1), new K(1, -1), new K(1) },
            ["rollover"] = new[] { new K(2, 0, 0, true), new K(2, 1, 0, true), new K(2, 2, 0, true), new K(2, 1, 0, true), new K(2, 0, 0, true), new K(2, -1, 0, true), new K(2, -2, 0, true), new K(2, -1, 0, true) },
            ["purr"] = Seq(2, 2, 2, 2, 2, 2),
            ["sniff"] = Rep(4, 6, 1, -1),
            ["lick"] = Rep(10, 6, 0, 1),
            ["pounce"] = new[] { new K(14), new K(14, 1), new K(14), new K(14, 1), new K(5, 2, 4), new K(5, 3, 2), new K(12, 3), new K(12, 3) },
            ["meow"] = Seq(3, 3, 3, 0, 0, 0),
            ["flinch"] = Rep(8, 6, -1, 0),
            ["hiss"] = Rep(3, 6, 1, 0),
            ["swat"] = Seq(14, 12, 14, 12, 14, 12),
            ["knead"] = Rep(15, 8, 0, 1),
            ["suckle"] = Rep(15, 8, 1, 0),
            ["jumpup"] = Seq(14, 14, 5, 5, 0, 0),
            ["jumpdown"] = Seq(0, 14, 5, 5, 14, 0),
            ["gaze"] = Seq(0, 0, 0, 0, 0, 0, 0, 0, 3, 0),
        };

        public static bool Has(string catId) => Load(catId) != null;

        static readonly Dictionary<string, Color32[]> sheets = new();
        static readonly Dictionary<string, int> sheetW = new();

        static Color32[] Load(string catId)
        {
            if (sheets.TryGetValue(catId, out var px)) return px;
            px = null;
            var ta = Resources.Load<TextAsset>("CatSheets/" + catId);
            if (ta != null)
            {
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (t.LoadImage(ta.bytes)) { px = t.GetPixels32(); sheetW[catId] = t.width; }
                Object.Destroy(t);
            }
            sheets[catId] = px;
            return px;
        }

        public static int FrameCount(string clip) => Map.TryGetValue(clip, out var k) ? k.Length : 1;

        // 클립 i번째 프레임을 w×h 캔버스(바닥 = ground)에 그린다. Unity 텍스처 좌표(아래가 0).
        public static Color32[] Frame(string catId, string clip, int i, int w, int h, int ground)
        {
            var src = Load(catId);
            var o = new Color32[w * h];
            if (src == null) return o;
            if (!Map.TryGetValue(clip, out var seq)) seq = Map["sit"];
            var k = seq[i % seq.Length];
            int sw = sheetW[catId], sh = src.Length / sw;
            int cx = (k.f % 4) * Cell, cyTop = (k.f / 4) * Cell;          // 칸 왼쪽 위(이미지 기준)
            int ox = (w - Cell) / 2 + k.dx, oy = ground - 1 + k.dy;     // 시트 칸 맨 아래 1줄은 여백

            // 그림자(바닥 타원)
            int minX = Cell, maxX = -1;
            for (int y = 0; y < Cell; y++)
                for (int x = 0; x < Cell; x++)
                    if (src[(sh - 1 - (cyTop + y)) * sw + cx + x].a > 0) { if (x < minX) minX = x; if (x > maxX) maxX = x; }
            if (maxX >= 0)
            {
                float rx = (maxX - minX) * (k.rot ? 0.5f : 0.4f), mid = ox + (minX + maxX) * 0.5f;
                if (k.rot) { rx = 18; mid = w * 0.5f + k.dx; }
                for (int y = ground - 2; y <= ground + 1; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = (x + 0.5f - mid) / rx, v = (y + 0.5f - ground) / 2.2f;
                        if (u * u + v * v <= 1f && y >= 0 && y < h) o[y * w + x] = new Color32(40, 24, 30, 70);
                    }
            }

            for (int y = 0; y < Cell; y++)
                for (int x = 0; x < Cell; x++)
                {
                    var c = src[(sh - 1 - (cyTop + y)) * sw + cx + x];
                    if (c.a == 0) continue;
                    int tx, ty;
                    if (k.rot)
                    {   // 반시계 90°: 머리가 왼쪽, 발이 오른쪽 → 옆으로 누워 뒹굴
                        tx = ox + y - 6; ty = ground + (x - minX);
                    }
                    else { tx = ox + x; ty = oy + (Cell - 1 - y); }
                    if (tx < 0 || tx >= w || ty < 0 || ty >= h) continue;
                    o[ty * w + tx] = c;
                }
            return o;
        }
    }
}
