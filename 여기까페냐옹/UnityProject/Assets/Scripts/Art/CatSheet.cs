using System.Collections.Generic;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 도트 시트 고양이(4×4 = 16포즈, 칸 56×56). Resources/CatSheets/{catId}.bytes 가 있으면 리그 대신 이것을 쓴다.
    // 포즈 번호: 0 앉기 · 1 윙크 · 2 눈웃음 · 3 야옹 / 4~7 걷기 / 8 식빵 · 9 잠 · 10 그루밍 · 11 먹기 / 12 엉덩이 들기 · 13 기지개 · 14 웅크림 · 15 꾹꾹이
    //           16 하악 · 17 뒹굴(옆으로 누움) · 18 점프 · 19 냥펀치(앞발 번쩍)
    //           20~24 식빵 표정(마감 놀아주기 확대용): 20 눈웃음 · 21 감은 눈 · 22 화남 · 23 입 벌림 · 24 반쯤 감음
    // 클립(CatClips 이름)마다 포즈 번호와 살짝 흔들기(dx,dy)·눕히기(rot)를 조합해 프레임을 만든다.
    public static class CatSheet
    {
        public const int Cell = 56;

        struct K { public int f, dx, dy; public bool rot, flip; public K(int f, int dx = 0, int dy = 0, bool rot = false, bool flip = false) { this.f = f; this.dx = dx; this.dy = dy; this.rot = rot; this.flip = flip; } }
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
            ["rollover"] = new[] { new K(17), new K(17, 1), new K(17, 2), new K(2), new K(17, 0, 0, false, true), new K(17, -1, 0, false, true), new K(17, -2, 0, false, true), new K(2) },
            ["purr"] = Seq(2, 2, 2, 2, 2, 2),
            ["sniff"] = Rep(4, 6, 1, -1),
            ["lick"] = Rep(10, 6, 0, 1),
            ["pounce"] = new[] { new K(14), new K(14, 1), new K(14), new K(14, 1), new K(18, 2, 4), new K(18, 3, 2), new K(12, 3), new K(12, 3) },
            ["meow"] = Seq(3, 3, 3, 0, 0, 0),
            ["flinch"] = Rep(8, 6, -1, 0),
            ["hiss"] = Rep(16, 6, 1, 0),
            ["swat"] = Seq(0, 19, 19, 0, 19, 19),
            ["knead"] = Rep(15, 8, 0, 1),
            ["suckle"] = Rep(15, 8, 1, 0),
            ["jumpup"] = Seq(14, 14, 18, 18, 0, 0),
            ["jumpdown"] = Seq(0, 18, 18, 18, 14, 0),
            ["gaze"] = Seq(0, 0, 0, 0, 0, 0, 0, 0, 3, 0),
        };

        // 리디자인 시트(4칸×13줄 이상): 기본 25칸 + 28번부터 추가 프레임
        //  28~31 뛰기 · 32~35 앉아 대기(32 꼬리왼 33 꼬리오 34 깜빡 35 갸웃) · 36~39 그루밍 · 40~41 먹기 · 42~43 꾹꾹이
        //  44~45 잠 숨쉬기 · 46 졸린 식빵 · 47 귀 쫑긋 식빵 · 48~51 덮치기 · 52~59 걷기(컷아웃)
        // 걷기: AI가 네 다리 순서를 못 지켜서, 부품(몸통·앞다리·뒷다리)을 받아 Tools/catart/newcats/cutout_parts.py로 생성.
        static readonly Dictionary<string, K[]> Map2 = new()
        {
            ["walk"] = Seq(52, 53, 54, 55, 56, 57, 58, 59),                       // 컷아웃 걷기 8프레임(부품 그림 + 다리 회전)
            ["stand"] = Seq(52, 52, 52, 52, 52, 52, 52, 52),
            ["sniff"] = new[] { new K(52), new K(52, 1, -1), new K(52), new K(52, 1, -1), new K(52), new K(52) },
            ["run"] = Seq(28, 29, 30, 31, 28, 29, 30, 31),
            ["sit"] = Seq(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 34, 0),
            ["gaze"] = Seq(0, 0, 0, 0, 0, 0, 0, 0, 35, 35),
            ["groom"] = Seq(36, 37, 37, 38, 38, 37, 36, 39),
            ["lick"] = Seq(36, 37, 37, 36, 37, 37),
            ["eat"] = Seq(40, 41, 40, 41, 40, 41),
            ["knead"] = Seq(42, 43, 42, 43, 42, 43, 42, 43),
            ["suckle"] = Seq(42, 43, 42, 43, 42, 43, 42, 43),
            ["sleep"] = Seq(44, 44, 44, 45, 45, 45),
            ["loaf"] = Seq(8, 8, 8, 8, 46, 46, 8, 47),
            ["pounce"] = Seq(48, 49, 48, 49, 50, 50, 51, 51),
        };
        static bool IsV2(string catId) { var px = Load(catId); return px != null && px.Length / sheetW[catId] >= Cell * 13; }
        static K[] SeqFor(string catId, string clip)
        {
            if (IsV2(catId) && Map2.TryGetValue(clip, out var k2)) return k2;
            return Map.TryGetValue(clip, out var k) ? k : Map["sit"];
        }

        public static bool Has(string catId) => Load(catId) != null;
        public static bool HasCutoutWalk(string catId) => Load(catId) != null && IsV2(catId);   // 걷기 52~59(컷아웃, 보폭 3.2px/프레임)

        static readonly Dictionary<string, Color32[]> sheets = new();
        static readonly Dictionary<string, int> sheetW = new();

        static Color32[] Load(string catId)
        {
            if (sheets.TryGetValue(catId, out var px)) return px;
            px = null;
            var ta = Resources.Load<TextAsset>("CatSheets/" + catId);             // Resources/CatSheets/cat_x.bytes (내용은 PNG)
            if (ta != null)
            {
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (t.LoadImage(ta.bytes)) { px = t.GetPixels32(); sheetW[catId] = t.width; }
                Object.Destroy(t);
            }
            if (px == null) Debug.LogWarning($"[CatSheet] {catId} 시트 없음 → 리그 그림 사용");
            sheets[catId] = px;
            return px;
        }

        // 시트 한 칸의 픽셀(x: 0~55 왼→오, yImg: 0~55 위→아래). 없거나 범위 밖이면 투명.
        public static Color32 Px(string catId, int frame, int x, int yImg)
        {
            var src = Load(catId);
            if (src == null || x < 0 || yImg < 0 || x >= Cell || yImg >= Cell) return default;
            int sw = sheetW[catId], sh = src.Length / sw;
            int cx = (frame % 4) * Cell, cy = (frame / 4) * Cell;
            if (cy + Cell > sh) return default;
            return src[(sh - 1 - (cy + yImg)) * sw + cx + x];
        }

        public static int FrameCount(string clip) => Map.TryGetValue(clip, out var k) ? k.Length : 1;
        public static int FrameCount(string catId, string clip) => SeqFor(catId, clip).Length;

        // 클립 i번째 프레임을 w×h 캔버스(바닥 = ground)에 그린다. Unity 텍스처 좌표(아래가 0).
        public static Color32[] Frame(string catId, string clip, int i, int w, int h, int ground)
        {
            var src = Load(catId);
            var o = new Color32[w * h];
            if (src == null) return o;
            var seq = SeqFor(catId, clip);
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
                    else { tx = ox + (k.flip ? Cell - 1 - x : x); ty = oy + (Cell - 1 - y); }
                    if (tx < 0 || tx >= w || ty < 0 || ty >= h) continue;
                    o[ty * w + tx] = c;
                }
            return o;
        }
    }
}
