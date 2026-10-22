using UnityEngine;

namespace YeogiCafe.Art
{
    // 통유리 창(가게 앞 쇼윈도). 바깥 풍경은 날씨(맑음/흐림/비)와 시간대(아침/점심/저녁)에 따라 다르게 그린다.
    // 바깥(OutsideView)과 창틀(Frame)을 따로 그려서, 그 사이에 빗줄기 같은 움직이는 연출을 끼운다.
    public static class GlassWindowDrawing
    {
        public const int GW = 256, GH = 70;            // 유리 안쪽(바깥 풍경) 크기 = 8 × 2.2유닛
        public const int Border = 5;                   // 창틀 두께
        public const int FW = GW + Border * 2, FH = GH + Border * 2 + 3;   // 창틀(아래 창턱 포함)

        static Color32 C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);
        static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), (byte)(a.a + (b.a - a.a) * t));
        }
        static Color32 Mul(Color32 c, float k) => new Color32((byte)Mathf.Clamp(c.r * k, 0, 255), (byte)Mathf.Clamp(c.g * k, 0, 255), (byte)Mathf.Clamp(c.b * k, 0, 255), c.a);

        // weather: 0 맑음, 1 흐림, 2 비 / phase: 0 아침, 1 점심, 2 저녁
        public static PixelCanvas Outside(int weather, int phase) => Outside(weather, phase, 1);

        // season: 0 봄(벚꽃) 1 여름(초록) 2 가을(단풍) 3 겨울(눈)
        public static PixelCanvas Outside(int weather, int phase, int season)
        {
            var c = new PixelCanvas(GW, GH);
            var rng = new System.Random(7);

            // ── 하늘 ──
            Color32 top, low;
            if (weather == 2) { top = C(92, 104, 126); low = C(140, 150, 166); }
            else if (weather == 1) { top = C(150, 162, 180); low = C(196, 202, 210); }
            else if (phase == 0) { top = C(150, 200, 236); low = C(250, 226, 196); }
            else if (phase == 2) { top = C(126, 110, 176); low = C(248, 170, 128); }
            else { top = C(110, 178, 232); low = C(190, 226, 244); }
            if (phase == 2 && weather > 0) { top = Mul(top, 0.8f); low = Lerp(low, C(206, 150, 130), 0.4f); }
            for (int y = 0; y < GH; y++)
            {
                float t = y / (float)(GH - 1);
                var col = Lerp(low, top, t);
                for (int x = 0; x < GW; x++)
                {
                    // 디더로 계단 부드럽게
                    bool d = ((x + y) & 1) == 0 && (y % 6 == 0);
                    c.Set(x, y, d ? Lerp(col, top, 0.15f) : col);
                }
            }

            // ── 해 ──
            if (weather == 0)
            {
                if (phase == 2) Sun(c, 200, 34, 11, C(255, 214, 140), C(255, 236, 190));
                else if (phase == 0) Sun(c, 38, 55, 8, C(255, 240, 190), C(255, 250, 225));
                else Sun(c, 150, 53, 7, C(255, 246, 210), C(255, 255, 240));
            }

            // ── 구름 ──
            int clouds = weather == 0 ? 4 : weather == 1 ? 9 : 12;
            Color32 cl = weather == 2 ? C(120, 128, 146) : weather == 1 ? C(214, 218, 226) : phase == 2 ? C(250, 206, 190) : C(255, 255, 255);
            Color32 cs = Mul(cl, 0.88f);
            for (int i = 0; i < clouds; i++)
            {
                int cx = rng.Next(0, GW), cy = weather == 0 ? rng.Next(48, 66) : rng.Next(42, 69);
                int s = weather == 0 ? rng.Next(4, 7) : rng.Next(6, 10);
                Cloud(c, cx, cy, s, cl, cs);
            }

            // ── 건너편 거리: 집·가게 줄 ──
            int baseY = 22;
            bool lights = phase == 2 || weather == 2;
            int bx = -6;
            var houseCols = new[] { C(246, 226, 200), C(240, 196, 196), C(196, 222, 206), C(214, 202, 234), C(236, 214, 170), C(206, 220, 236) };
            int hi = 0;
            while (bx < GW)
            {
                int w = 26 + rng.Next(0, 18), h = 13 + rng.Next(0, 17);
                var wall = houseCols[hi++ % houseCols.Length];
                if (weather == 2) wall = Mul(wall, 0.72f); else if (weather == 1) wall = Mul(wall, 0.88f);
                if (phase == 2) wall = Lerp(wall, C(200, 140, 150), 0.25f);
                var roof = weather == 2 ? C(90, 82, 96) : C(176, 104, 92);
                c.FillRect(bx, baseY, bx + w - 1, baseY + h, wall);
                // 지붕(삼각 또는 평지붕)
                if (hi % 2 == 0)
                    for (int k = 0; k <= w / 2 + 2; k++) c.HLine(bx - 2 + k, bx + w + 1 - k, baseY + h + 1 + k / 2, roof);
                else c.FillRect(bx - 1, baseY + h + 1, bx + w, baseY + h + 3, roof);
                if (season == 3)                                       // 겨울: 지붕을 눈이 덮는다(처마 한 줄만 기와색, 그늘진 오른쪽은 푸른 회색)
                {
                    var snow = weather == 2 ? C(196, 202, 216) : phase == 2 ? C(226, 222, 240) : C(248, 250, 255);
                    var shade = Mul(snow, 0.86f);
                    if (hi % 2 == 0)
                        for (int k = 1; k <= w / 2 + 2; k++)
                        {
                            int x0 = bx - 2 + k, x1 = bx + w + 1 - k, y = baseY + h + 1 + k / 2, mid = (x0 + x1) / 2;
                            c.HLine(x0, mid, y, snow);
                            if (x1 > mid) c.HLine(mid + 1, x1, y, shade);
                        }
                    else
                    {
                        c.FillRect(bx - 1, baseY + h + 2, bx + w, baseY + h + 4, snow);
                        for (int x = bx; x < bx + w; x += 5) c.Set(x, baseY + h + 1, snow);   // 처마에 매달린 눈
                    }
                }
                // 창문들
                for (int wy = baseY + 6; wy < baseY + h - 4; wy += 9)
                    for (int wx = bx + 4; wx < bx + w - 6; wx += 9)
                    {
                        bool lit = lights && rng.NextDouble() < 0.6;
                        var glass = lit ? C(255, 220, 140) : weather == 2 ? C(110, 124, 146) : C(170, 206, 230);
                        c.FillRect(wx, wy, wx + 4, wy + 4, glass);
                        c.HLine(wx, wx + 4, wy + 2, Mul(glass, 0.85f));
                    }
                // 가게 차양(줄무늬)
                if (rng.NextDouble() < 0.5)
                {
                    var a1 = C(222, 96, 104); var a2 = C(250, 240, 230);
                    if (weather == 2) { a1 = Mul(a1, 0.75f); a2 = Mul(a2, 0.75f); }
                    for (int x = bx + 2; x < bx + w - 2; x++) { var ac = ((x / 3) % 2 == 0) ? a1 : a2; c.VLine(x, baseY + 2, baseY + 5, ac); }
                }
                bx += w + rng.Next(2, 6);
                // 집 사이 가로수
                if (rng.NextDouble() < 0.55 && bx < GW) { Tree(c, bx - 2, baseY, weather, phase, season); }
            }

            // ── 길·보도 ──
            var road = weather == 2 ? C(70, 74, 86) : C(128, 126, 132);
            var walk = weather == 2 ? C(120, 122, 134) : season == 3 ? C(232, 236, 244) : C(206, 196, 184);
            c.FillRect(0, 9, GW - 1, baseY - 1, road);
            for (int x = 6; x < GW; x += 22) c.HLine(x, x + 9, 15, weather == 2 ? C(170, 170, 160) : C(240, 232, 200));   // 차선
            c.FillRect(0, 0, GW - 1, 8, walk);
            c.HLine(0, GW - 1, 8, Mul(walk, 0.8f));                                   // 연석
            for (int x = 0; x < GW; x += 12) c.VLine(x, 0, 7, Mul(walk, 0.9f));        // 보도블록
            // 가로등
            for (int x = 30; x < GW; x += 96)
            {
                c.VLine(x, 1, 30, C(70, 72, 80)); c.HLine(x - 2, x + 2, 30, C(70, 72, 80));
                var lamp = lights ? C(255, 226, 150) : C(220, 226, 230);
                c.FillRect(x - 2, 31, x + 2, 33, lamp);
                if (lights) for (int r = 1; r < 6; r++) { c.Blend(x - r, 32, C(255, 226, 150, 60)); c.Blend(x + r, 32, C(255, 226, 150, 60)); }
            }

            // ── 계절 소품: 봄 꽃잎 · 가을 낙엽 · 겨울 눈송이 ──
            if (season == 0 && weather != 2)
                for (int i = 0; i < 26; i++) { int x = rng.Next(0, GW), y = rng.Next(10, GH); c.Set(x, y, C(255, 206, 222)); if (i % 3 == 0) c.Set(x + 1, y, C(248, 180, 200)); }
            if (season == 2)
                for (int i = 0; i < 22; i++) { int x = rng.Next(0, GW), y = rng.Next(0, 8); c.Set(x, y, i % 2 == 0 ? C(222, 128, 60) : C(200, 92, 54)); }
            if (season == 3 && weather != 2)
                for (int i = 0; i < 40; i++) { int x = rng.Next(0, GW), y = rng.Next(10, GH); c.Set(x, y, C(255, 255, 255)); }

            // ── 비: 물웅덩이 반사 + 안개 ──
            if (weather == 2)
            {
                for (int i = 0; i < 6; i++)
                {
                    int px = rng.Next(10, GW - 30), py = rng.Next(1, 7), pw = rng.Next(10, 24);
                    c.HLine(px, px + pw, py, C(150, 170, 200));
                    c.HLine(px + 3, px + pw - 3, py - 1 < 0 ? 0 : py - 1, C(130, 150, 180));
                }
                for (int y = 18; y < GH; y++) for (int x = 0; x < GW; x++) c.Blend(x, y, C(170, 180, 200, 40));
            }
            else if (weather == 1)
                for (int y = 30; y < GH; y++) for (int x = 0; x < GW; x++) c.Blend(x, y, C(220, 224, 230, 24));
            return c;
        }

        static void Sun(PixelCanvas c, int x, int y, int r, Color32 core, Color32 glow)
        {
            for (int k = r + 6; k > r; k--) c.Disc(x, y, k, new Color32(glow.r, glow.g, glow.b, 40));
            c.Disc(x, y, r, core);
        }

        static void Cloud(PixelCanvas c, int x, int y, int s, Color32 col, Color32 shade)
        {
            c.Disc(x, y, s, col); c.Disc(x + s, y + 1, s + 1, col); c.Disc(x + 2 * s, y, s, col);
            c.HLine(x - s + 1, x + 3 * s - 1, y - s + 1, shade);
        }

        static void Tree(PixelCanvas c, int x, int y, int weather, int phase, int season = 1)
        {
            var trunk = new Color32(120, 88, 70, 255);
            if (season == 3)                                            // 겨울: 앙상한 가지 + 눈
            {
                c.VLine(x, y, y + 15, trunk);
                c.Set(x - 1, y + 10, trunk); c.Set(x - 2, y + 11, trunk); c.Set(x + 1, y + 12, trunk); c.Set(x + 2, y + 13, trunk);
                c.Set(x - 2, y + 12, new Color32(248, 250, 255, 255)); c.Set(x + 2, y + 14, new Color32(248, 250, 255, 255)); c.Set(x, y + 16, new Color32(248, 250, 255, 255));
                return;
            }
            Color32 leaf = season == 0 ? new Color32(244, 186, 204, 255)        // 봄: 벚꽃
                         : season == 2 ? new Color32(224, 134, 66, 255)         // 가을: 단풍
                         : new Color32(122, 178, 108, 255);
            if (weather == 2) leaf = Mul(leaf, 0.72f);
            else if (phase == 2) leaf = Lerp(leaf, new Color32(170, 130, 96, 255), 0.25f);
            var leafL = Mul(leaf, 1.12f);
            c.VLine(x, y, y + 9, trunk);
            c.Disc(x, y + 14, 6, leaf); c.Disc(x - 2, y + 16, 3, leafL);
        }

        // 창틀: 원목 테두리 + 세로 문살(4칸) + 가로 상단 띠 + 아래 창턱. 유리는 투명, 반사광만 살짝.
        public static PixelCanvas Frame()
        {
            var c = new PixelCanvas(FW, FH);
            var dark = PixelPalette.WoodDeep; var mid = PixelPalette.WoodMid; var light = PixelPalette.WoodLight;
            int gx0 = Border, gy0 = Border + 3, gx1 = Border + GW - 1, gy1 = gy0 + GH - 1;
            // 테두리
            c.FillRect(0, 3, FW - 1, FH - 1, mid);
            for (int y = gy0; y <= gy1; y++) for (int x = gx0; x <= gx1; x++) c.Set(x, y, PixelPalette.Transparent);
            c.RectOutline(0, 3, FW - 1, FH - 1, dark);
            c.RectOutline(gx0 - 1, gy0 - 1, gx1 + 1, gy1 + 1, dark);
            c.HLine(1, FW - 2, FH - 2, light);                                   // 위쪽 하이라이트
            // 문살(세로 3줄 → 4칸)
            for (int i = 1; i < 4; i++)
            {
                int mx = gx0 + GW * i / 4;
                c.FillRect(mx - 1, gy0, mx + 1, gy1, mid);
                c.VLine(mx - 1, gy0, gy1, light); c.VLine(mx + 1, gy0, gy1, dark);
            }
            // 위쪽 가로 띠(채광창)
            int ty = gy1 - 13;
            c.FillRect(gx0, ty - 1, gx1, ty + 1, mid); c.HLine(gx0, gx1, ty + 1, light); c.HLine(gx0, gx1, ty - 1, dark);
            // 창턱(아래로 살짝 튀어나옴)
            c.FillRect(0, 0, FW - 1, 4, light); c.HLine(0, FW - 1, 0, dark); c.HLine(0, FW - 1, 4, dark);
            // 유리 반사광(사선, 반투명)
            for (int i = 0; i < 4; i++)
            {
                int px = gx0 + 10 + GW * i / 4;
                for (int k = 0; k < 14; k++)
                {
                    c.Set(px + k, gy1 - 6 - k, new Color32(255, 255, 255, 70));
                    c.Set(px + k + 3, gy1 - 6 - k, new Color32(255, 255, 255, 40));
                    if (k < 7) c.Set(px + k + 9, gy1 - 6 - k, new Color32(255, 255, 255, 35));
                }
            }
            return c;
        }
    }
}
