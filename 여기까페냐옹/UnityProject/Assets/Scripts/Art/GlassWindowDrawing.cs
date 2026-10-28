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
            if (season == 1 && weather == 0 && phase != 2) { top = C(56, 146, 232); low = C(160, 214, 248); }   // 여름: 쨍하고 진한 파랑
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

            // ── 여름: 집 뒤로 솟은 뭉게구름(적란운) ──
            if (season == 1 && weather == 0)
            {
                var cb = phase == 2 ? C(252, 214, 196) : C(255, 255, 255);
                var cbs = phase == 2 ? C(226, 176, 170) : C(214, 228, 244);
                foreach (int cx in new[] { 58, 196 })
                {
                    for (int k = 0; k < 5; k++)
                    {
                        int r = 13 - k * 2, yy = 30 + k * 8;
                        c.Disc(cx - 8 + (k % 2) * 4, yy, r, cb); c.Disc(cx + 9 - (k % 2) * 3, yy + 2, r - 1, cb);
                    }
                    for (int x = cx - 20; x <= cx + 20; x++) c.Blend(x, 30, new Color32(cbs.r, cbs.g, cbs.b, 160));
                }
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

            // ── 먼 시내(10/29): 집 줄 뒤로 옅은 빌딩·교회 첨탑·언덕 — 하늘색에 섞어 거리감 ──
            {
                var haze = Lerp(low, C(120, 130, 156), weather == 2 ? 0.45f : 0.32f);
                if (phase == 2 && weather == 0) haze = Lerp(low, C(150, 110, 140), 0.35f);
                var hazeD = Mul(haze, 0.94f);
                var frng = new System.Random(11);
                bool lightsF = phase == 2 || weather == 2;
                for (int x = 0; x < GW; x++) { int hy = 30 + (int)(4 * Mathf.Sin(x * 0.045f) + 2 * Mathf.Sin(x * 0.13f + 1f)); c.VLine(x, 22, hy, Lerp(haze, low, 0.35f)); }   // 언덕
                int fx = -4;
                while (fx < GW)
                {
                    int fw = 10 + frng.Next(0, 16), fh = 16 + frng.Next(0, 20);
                    c.FillRect(fx, 22, fx + fw - 1, 22 + fh, haze);
                    for (int wy = 26; wy < 20 + fh; wy += 4)
                        for (int wx = fx + 2; wx < fx + fw - 2; wx += 3)
                            c.Set(wx, wy, lightsF && frng.NextDouble() < 0.5 ? C(250, 220, 150) : hazeD);
                    if (frng.NextDouble() < 0.15) { c.VLine(fx + fw / 2, 23 + fh, 30 + fh, haze); c.Set(fx + fw / 2, 31 + fh, lightsF ? C(255, 120, 110) : hazeD); }   // 안테나
                    fx += fw + frng.Next(4, 14);
                }
                // 교회 첨탑 하나
                c.FillRect(178, 22, 186, 44, haze); for (int k = 0; k < 6; k++) c.HLine(178 + k, 186 - k, 45 + k, haze); c.VLine(182, 51, 54, hazeD); c.HLine(181, 183, 53, hazeD);
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
                // 1층 가게(10/29): 쇼윈도 + 문 + 차양 + 간판 그림(빵·꽃·생선·책·커피·실타래). 높은 집만
                bool shop = h >= 19 && rng.NextDouble() < 0.75;
                int firstWin = baseY + 6;
                if (shop)
                {
                    int kind = rng.Next(0, 6);
                    var dark = Mul(wall, 0.55f);
                    var glassC = lights ? C(255, 222, 150) : weather == 2 ? C(120, 134, 156) : C(196, 222, 236);
                    c.FillRect(bx + 2, baseY, bx + w - 9, baseY + 5, glassC);                      // 쇼윈도
                    c.RectOutline(bx + 1, baseY, bx + w - 8, baseY + 6, dark);
                    for (int ix = bx + 4; ix < bx + w - 10; ix += 4) c.Set(ix, baseY + 1, ShopItem(kind, ix));   // 진열된 물건
                    c.FillRect(bx + w - 6, baseY, bx + w - 3, baseY + 6, Mul(wall, 0.45f));       // 문
                    c.Set(bx + w - 4, baseY + 3, C(240, 210, 120));                                 // 손잡이
                    var a1 = ShopColor(kind, season); var a2 = C(250, 240, 230);
                    if (weather == 2) { a1 = Mul(a1, 0.75f); a2 = Mul(a2, 0.75f); }
                    for (int x = bx + 1; x < bx + w - 1; x++) { var ac = ((x / 3) % 2 == 0) ? a1 : a2; c.VLine(x, baseY + 7, baseY + 9, ac); c.Set(x, baseY + 7, (x % 3 == 1) ? Mul(ac, 0.8f) : ac); }   // 차양
                    int sx = bx + w / 2 - 4;                                                         // 간판
                    c.FillRect(sx, baseY + 10, sx + 8, baseY + 14, C(250, 244, 230)); c.RectOutline(sx, baseY + 10, sx + 8, baseY + 14, Mul(a1, 0.8f));
                    SignIcon(c, sx + 2, baseY + 11, kind);
                    firstWin = baseY + 17;
                }
                // 창문들
                for (int wy = firstWin; wy < baseY + h - 4; wy += 9)
                    for (int wx = bx + 4; wx < bx + w - 6; wx += 9)
                    {
                        bool lit = lights && rng.NextDouble() < 0.6;
                        var glass = lit ? C(255, 220, 140) : weather == 2 ? C(110, 124, 146) : C(170, 206, 230);
                        c.FillRect(wx, wy, wx + 4, wy + 4, glass);
                        c.HLine(wx, wx + 4, wy + 2, Mul(glass, 0.85f));
                        if (season != 3 && rng.NextDouble() < 0.3)                                   // 창가 화분(꽃)
                        {
                            c.HLine(wx - 1, wx + 5, wy - 1, C(150, 96, 70));
                            var fl = season == 2 ? C(236, 150, 70) : (wx % 2 == 0 ? C(240, 110, 140) : C(250, 210, 90));
                            c.Set(wx, wy, fl); c.Set(wx + 2, wy, C(96, 160, 90)); c.Set(wx + 4, wy, fl);
                        }
                    }
                if (!shop && rng.NextDouble() < 0.5)                                                  // 가게 아닌 집: 작은 차양만
                {
                    var a1 = season == 1 ? C(70, 140, 214) : C(222, 96, 104); var a2 = C(250, 240, 230);   // 여름 차양은 시원한 파랑
                    if (weather == 2) { a1 = Mul(a1, 0.75f); a2 = Mul(a2, 0.75f); }
                    for (int x = bx + 2; x < bx + w - 2; x++) { var ac = ((x / 3) % 2 == 0) ? a1 : a2; c.VLine(x, baseY + 2, baseY + 5, ac); }
                }
                if (hi % 3 == 1 && season != 3) { int chx = bx + w - 7; c.FillRect(chx, baseY + h + 2, chx + 2, baseY + h + 7, C(150, 90, 80)); }   // 굴뚝
                bx += w + rng.Next(2, 6);
                // 집 사이 가로수
                if (rng.NextDouble() < (season == 1 ? 0.85 : 0.55) && bx < GW) { Tree(c, bx - 2, baseY, weather, phase, season); }   // 여름은 가로수가 무성
            }

            // ── 길·보도 ──
            var road = weather == 2 ? C(70, 74, 86) : C(128, 126, 132);
            var walk = weather == 2 ? C(120, 122, 134) : season == 3 ? C(232, 236, 244) : C(206, 196, 184);
            c.FillRect(0, 9, GW - 1, baseY - 1, road);
            var paint = weather == 2 ? C(170, 170, 160) : C(240, 232, 200);
            for (int x = 6; x < GW; x += 22) if (x + 9 < 98 || x > 124) c.HLine(x, x + 9, 15, paint);   // 차선(횡단보도 자리는 비움)
            for (int y = 10; y <= 20; y += 2) c.HLine(100, 122, y, weather == 2 ? C(150, 152, 156) : C(232, 230, 222));   // 횡단보도
            c.FillRect(0, 0, GW - 1, 8, walk);
            c.HLine(0, GW - 1, 8, Mul(walk, 0.8f));                                   // 연석
            for (int x = 0; x < GW; x += 12) c.VLine(x, 0, 7, Mul(walk, 0.9f));        // 보도블록
            c.HLine(0, GW - 1, 21, Mul(walk, 0.85f));                                 // 건너편 보도 턱
            // 보도 소품·가로등은 NearProps로 따로 그린다(10/29): 지나가는 차보다 앞에 보여야 해서 StreetLife가 차 위·행인 아래에 깐다

            // ── 계절 소품: 봄 꽃잎 · 가을 낙엽 · 겨울 눈송이 ──
            if (season == 0 && weather != 2)
                for (int i = 0; i < 26; i++) { int x = rng.Next(0, GW), y = rng.Next(10, GH); c.Set(x, y, C(255, 206, 222)); if (i % 3 == 0) c.Set(x + 1, y, C(248, 180, 200)); }
            if (season == 1)                                                   // 여름: 보도 화단의 해바라기
                for (int x = 8; x < GW; x += 14)
                {
                    if (x % 96 > 24 && x % 96 < 36) continue;                       // 가로등 자리 비우기
                    var stem = weather == 2 ? C(70, 110, 70) : C(88, 150, 76);
                    c.VLine(x, 2, 8, stem); c.Set(x + 1, 5, stem);
                    var pet = weather == 2 ? C(196, 160, 60) : phase == 2 ? C(250, 180, 70) : C(255, 206, 50);
                    c.Disc(x, 10, 2, pet); c.Set(x, 10, C(120, 80, 50)); c.Set(x + 1, 10, C(120, 80, 50));
                }
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

        // 가게 종류: 0 빵 1 꽃 2 생선 3 책 4 커피 5 실타래
        static Color32 ShopColor(int k, int season) => k switch
        {
            0 => C(222, 150, 80), 1 => C(236, 120, 160), 2 => C(80, 150, 210), 3 => C(110, 150, 100), 4 => C(150, 100, 80), _ => C(170, 120, 200)
        };
        static Color32 ShopItem(int k, int x) => k switch
        {
            0 => x % 8 == 0 ? C(210, 150, 80) : C(240, 200, 120), 1 => x % 8 == 0 ? C(240, 110, 140) : C(250, 210, 90), 2 => C(120, 170, 220),
            3 => x % 8 == 0 ? C(200, 80, 80) : C(80, 120, 190), 4 => C(250, 250, 245), _ => x % 8 == 0 ? C(230, 120, 170) : C(140, 180, 230)
        };
        static void SignIcon(PixelCanvas c, int x, int y, int k)
        {
            switch (k)
            {
                case 0: c.HLine(x, x + 4, y, C(200, 130, 60)); c.HLine(x + 1, x + 3, y + 1, C(230, 170, 90)); c.Set(x + 2, y + 2, C(230, 170, 90)); break;   // 빵
                case 1: c.Set(x + 2, y, C(90, 150, 80)); c.Set(x + 2, y + 1, C(90, 150, 80)); c.Set(x + 1, y + 2, C(236, 110, 150)); c.Set(x + 3, y + 2, C(236, 110, 150)); c.Set(x + 2, y + 2, C(250, 210, 90)); c.Set(x + 2, y + 3, C(236, 110, 150)); break;   // 꽃
                case 2: c.HLine(x, x + 3, y + 1, C(80, 140, 200)); c.Set(x + 4, y, C(80, 140, 200)); c.Set(x + 4, y + 2, C(80, 140, 200)); c.Set(x + 1, y + 1, C(30, 30, 40)); break;   // 생선
                case 3: c.HLine(x, x + 4, y, C(200, 80, 80)); c.HLine(x, x + 4, y + 1, C(80, 120, 190)); c.HLine(x + 1, x + 4, y + 2, C(110, 160, 100)); break;   // 책
                case 4: c.FillRect(x + 1, y, x + 3, y + 2, C(140, 90, 70)); c.Set(x + 4, y + 1, C(140, 90, 70)); c.Set(x + 2, y + 3, C(200, 200, 210)); break;   // 커피
                default: c.Disc(x + 2, y + 1, 1, C(200, 120, 200)); c.Set(x + 4, y, C(200, 120, 200)); break;   // 실타래
            }
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
                         : new Color32(70, 152, 74, 255);                     // 여름: 짙은 초록
            if (weather == 2) leaf = Mul(leaf, 0.72f);
            else if (phase == 2) leaf = Lerp(leaf, new Color32(170, 130, 96, 255), 0.25f);
            var leafL = Mul(leaf, 1.12f);
            c.VLine(x, y, y + 9, trunk);
            if (season == 1) { c.Disc(x, y + 15, 8, leaf); c.Disc(x - 3, y + 17, 4, leafL); c.Disc(x + 3, y + 12, 3, Mul(leaf, 0.85f)); return; }   // 여름은 더 크고 풍성하게
            c.Disc(x, y + 14, 6, leaf); c.Disc(x - 2, y + 16, 3, leafL);
        }

        // 창틀: 원목 테두리 + 세로 문살(4칸) + 가로 상단 띠 + 아래 창턱. 유리는 투명, 반사광만 살짝.
        public static PixelCanvas Frame() => Frame(GW);

        // 큰 통창(가로 128px = 4유닛, 2칸) 같은 좁은 창: 같은 풍경의 가운데 부분을 잘라 쓴다
        // 가까운 보도의 세로로 선 것들(가로등·정류장·우체통·벤치·거치대) — 투명 바탕. 길의 차보다 앞, 보도 행인보다 뒤에 그린다.
        public static PixelCanvas NearProps(int gw, int weather, int phase, int season)
        {
            var c = new PixelCanvas(GW, GH);
            bool lights = phase == 2 || weather == 2;
            // 거리 소품(10/29): 우체통 · 버스 정류장 · 벤치 · 자전거 거치대
            {
                var ink = C(60, 52, 58);
                c.FillRect(66, 1, 70, 7, C(214, 66, 66)); c.HLine(66, 70, 7, C(170, 44, 52)); c.HLine(67, 69, 5, ink); c.VLine(68, 0, 1, ink);   // 우체통
                c.VLine(160, 1, 15, C(90, 96, 110)); c.Disc(160, 15, 2, C(70, 130, 210)); c.Set(160, 15, C(250, 250, 250));                     // 정류장 표지판
                c.FillRect(164, 1, 186, 1, C(150, 156, 168)); c.VLine(164, 1, 12, C(150, 156, 168)); c.VLine(186, 1, 12, C(150, 156, 168));
                c.FillRect(162, 12, 188, 13, C(110, 120, 140));                                                                              // 정류장 지붕
                for (int y = 2; y < 12; y++) c.Blend(165, y, C(200, 226, 240, 90));
                c.FillRect(168, 3, 182, 4, C(170, 110, 76)); c.VLine(169, 1, 3, ink); c.VLine(181, 1, 3, ink);                              // 정류장 벤치
                c.FillRect(206, 3, 220, 4, C(170, 110, 76)); c.HLine(206, 220, 6, C(170, 110, 76)); c.VLine(207, 1, 6, ink); c.VLine(219, 1, 6, ink);   // 벤치
                for (int x = 236; x < 250; x += 4) { c.VLine(x, 1, 4, C(150, 156, 168)); c.Set(x + 1, 4, C(150, 156, 168)); }                 // 자전거 거치대
            }
            // 가로등
            for (int x = 30; x < GW; x += 96)
            {
                c.VLine(x, 1, 30, C(70, 72, 80)); c.HLine(x - 2, x + 2, 30, C(70, 72, 80));
                var lamp = lights ? C(255, 226, 150) : C(220, 226, 230);
                c.FillRect(x - 2, 31, x + 2, 33, lamp);
                if (lights) for (int r = 1; r < 6; r++) { c.Blend(x - r, 32, C(255, 226, 150, 60)); c.Blend(x + r, 32, C(255, 226, 150, 60)); }
            }
            if (gw >= GW) return c;
            var part = new PixelCanvas(gw, GH); int ox = (GW - gw) / 2;
            for (int y = 0; y < GH; y++) for (int x = 0; x < gw; x++) part.Set(x, y, c.Get(x + ox, y));
            return part;
        }

        public static PixelCanvas OutsidePart(int gw, int weather, int phase, int season)
        {
            var full = Outside(weather, phase, season);
            if (gw >= GW) return full;
            var c = new PixelCanvas(gw, GH);
            int ox = (GW - gw) / 2;
            for (int y = 0; y < GH; y++) for (int x = 0; x < gw; x++) c.Set(x, y, full.Get(x + ox, y));
            return c;
        }

        // 창틀(유리 가로 gw px). 문살은 64px마다(256 → 4칸, 128 → 2칸)
        public static PixelCanvas Frame(int gw)
        {
            int GW = gw, FW = gw + Border * 2;
            var c = new PixelCanvas(FW, FH);
            var dark = PixelPalette.WoodDeep; var mid = PixelPalette.WoodMid; var light = PixelPalette.WoodLight;
            int gx0 = Border, gy0 = Border + 3, gx1 = Border + GW - 1, gy1 = gy0 + GH - 1;
            // 테두리
            c.FillRect(0, 3, FW - 1, FH - 1, mid);
            for (int y = gy0; y <= gy1; y++) for (int x = gx0; x <= gx1; x++) c.Set(x, y, PixelPalette.Transparent);
            c.RectOutline(0, 3, FW - 1, FH - 1, dark);
            c.RectOutline(gx0 - 1, gy0 - 1, gx1 + 1, gy1 + 1, dark);
            c.HLine(1, FW - 2, FH - 2, light);                                   // 위쪽 하이라이트
            // 문살(64px마다)
            int panes = Mathf.Max(1, GW / 64);
            for (int i = 1; i < panes; i++)
            {
                int mx = gx0 + GW * i / panes;
                c.FillRect(mx - 1, gy0, mx + 1, gy1, mid);
                c.VLine(mx - 1, gy0, gy1, light); c.VLine(mx + 1, gy0, gy1, dark);
            }
            // 위쪽 가로 띠(채광창)
            int ty = gy1 - 13;
            c.FillRect(gx0, ty - 1, gx1, ty + 1, mid); c.HLine(gx0, gx1, ty + 1, light); c.HLine(gx0, gx1, ty - 1, dark);
            // 창턱(아래로 살짝 튀어나옴)
            c.FillRect(0, 0, FW - 1, 4, light); c.HLine(0, FW - 1, 0, dark); c.HLine(0, FW - 1, 4, dark);
            // 유리 반사광(사선, 반투명)
            for (int i = 0; i < panes; i++)
            {
                int px = gx0 + 10 + GW * i / panes;
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
