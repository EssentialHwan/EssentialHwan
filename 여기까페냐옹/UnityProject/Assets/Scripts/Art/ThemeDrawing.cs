using UnityEngine;

namespace YeogiCafe.Art
{
    // 인테리어 테마 타일(32×32 심리스): 바닥·벽지·징두리. 기본 테마는 RoomDrawing 그대로.
    public static class ThemeDrawing
    {
        static Color32 C(int r, int g, int b) => new Color32((byte)r, (byte)g, (byte)b, 255);

        // ── 바닥 ──
        public static PixelCanvas Floor(string theme)
        {
            var c = new PixelCanvas(32, 32);
            switch (theme)
            {
                case "nordic":      // 밝은 애쉬 원목(넓은 판)
                    Planks(c, new[] { C(236, 224, 202), C(226, 212, 186), C(236, 224, 202), C(222, 206, 180) }, C(196, 178, 150), 8);
                    break;
                case "tatami":      // 다다미: 짚 결 + 초록 테두리 천
                    c.FillRect(0, 0, 31, 31, C(206, 198, 140));
                    for (int y = 1; y < 32; y += 2) c.HLine(0, 31, y, C(192, 186, 126));
                    c.FillRect(0, 14, 31, 17, C(84, 112, 82)); c.HLine(0, 31, 14, C(66, 92, 66)); c.HLine(0, 31, 17, C(104, 132, 98));
                    c.VLine(0, 0, 13, C(176, 168, 112)); c.VLine(16, 18, 31, C(176, 168, 112));
                    break;
                case "retro":       // 짙은 헤링본 마루
                    c.FillRect(0, 0, 31, 31, C(116, 76, 52));
                    for (int y = 0; y < 32; y += 8)
                        for (int x = 0; x < 32; x += 8)
                        {
                            bool flip = ((x + y) / 8) % 2 == 0;
                            var col = flip ? C(132, 88, 60) : C(104, 68, 46);
                            for (int k = 0; k < 8; k++) c.Set(x + k, y + (flip ? k : 7 - k), C(84, 54, 38));
                            c.FillRect(x + 1, y + 1, x + 3, y + 3, col);
                            c.FillRect(x + 4, y + 4, x + 6, y + 6, col);
                        }
                    break;
                case "candy":       // 파스텔 체크 타일
                    for (int y = 0; y < 32; y++)
                        for (int x = 0; x < 32; x++)
                            c.Set(x, y, ((x / 16) + (y / 16)) % 2 == 0 ? C(250, 214, 222) : C(204, 234, 232));
                    c.HLine(0, 31, 0, C(230, 196, 206)); c.VLine(0, 0, 31, C(230, 196, 206));
                    c.HLine(0, 31, 16, C(188, 218, 216)); c.VLine(16, 0, 31, C(188, 218, 216));
                    break;
                case "cabin":       // 넓고 짙은 통나무 판
                    Planks(c, new[] { C(156, 108, 70), C(140, 96, 60), C(160, 112, 72), C(136, 92, 58) }, C(104, 70, 44), 8);
                    for (int i = 0; i < 4; i++) c.Set((i * 9 + 5) % 32, i * 8 + 4, C(112, 76, 48));       // 옹이
                    break;
                default:
                    return RoomDrawing.FloorTile();
            }
            return c;
        }

        static void Planks(PixelCanvas c, Color32[] tones, Color32 seam, int h)
        {
            for (int row = 0; row < 32 / h; row++)
            {
                int y0 = row * h;
                c.FillRect(0, y0, 31, y0 + h - 1, tones[row % tones.Length]);
                c.HLine(0, 31, y0, seam);
                int s = (row % 2 == 0) ? 9 : 25;
                c.VLine(s, y0 + 1, y0 + h - 1, seam);
            }
        }

        // ── 벽지 ──
        public static PixelCanvas Wall(string theme)
        {
            var c = new PixelCanvas(32, 32);
            switch (theme)
            {
                case "nordic":      // 하얀 세로 판벽
                    c.FillRect(0, 0, 31, 31, C(246, 244, 238));
                    for (int x = 0; x < 32; x += 8) c.VLine(x, 0, 31, C(230, 228, 220));
                    break;
                case "tatami":      // 창호지 + 나무 문살
                    c.FillRect(0, 0, 31, 31, C(246, 238, 216));
                    for (int x = 0; x < 32; x += 16) { c.VLine(x, 0, 31, C(156, 116, 78)); }
                    for (int y = 0; y < 32; y += 10) c.HLine(0, 31, y, C(156, 116, 78));
                    break;
                case "retro":       // 와인색 다마스크(작은 마름모)
                    c.FillRect(0, 0, 31, 31, C(146, 56, 70));
                    for (int y = 4; y < 32; y += 16)
                        for (int x = 4; x < 32; x += 16)
                        {
                            int ox = (y / 16) % 2 == 0 ? 0 : 8;
                            int cx = (x + ox) % 32, cy = y + 4;
                            for (int k = 0; k <= 3; k++) { c.Set((cx - k + 32) % 32, cy + k - 3 < 0 ? 0 : cy + k - 3, C(170, 80, 92)); c.Set((cx + k) % 32, cy + k - 3 < 0 ? 0 : cy + k - 3, C(170, 80, 92)); c.Set((cx - k + 32) % 32, cy - k + 3, C(170, 80, 92)); c.Set((cx + k) % 32, cy - k + 3, C(170, 80, 92)); }
                        }
                    break;
                case "candy":       // 민트 바탕 + 하트·별 물방울
                    c.FillRect(0, 0, 31, 31, C(226, 244, 238));
                    for (int y = 4; y < 32; y += 10)
                        for (int x = 3 + (y / 10 % 2) * 6; x < 32; x += 12)
                        {
                            var col = ((x + y) / 4) % 2 == 0 ? C(248, 184, 200) : C(250, 222, 150);
                            c.Set(x, y, col); c.Set(x + 1, y, col); c.Set(x, y + 1, col); c.Set(x + 1, y + 1, col);
                        }
                    break;
                case "cabin":       // 가로 통나무 벽
                    for (int y0 = 0; y0 < 32; y0 += 8)
                    {
                        c.FillRect(0, y0, 31, y0 + 7, C(176, 124, 80));
                        c.HLine(0, 31, y0 + 6, C(196, 144, 96));       // 둥근 윗면 하이라이트
                        c.HLine(0, 31, y0 + 1, C(150, 104, 66));
                        c.HLine(0, 31, y0, C(104, 70, 44));             // 틈
                        int k = (y0 / 8) % 2 == 0 ? 6 : 22;
                        c.Set(k, y0 + 4, C(140, 96, 60)); c.Set(k + 1, y0 + 3, C(140, 96, 60));
                    }
                    break;
                default:
                    return RoomDrawing.WallTile();
            }
            return c;
        }

        // ── 징두리(벽 아래 띠) ──
        public static PixelCanvas Wainscot(string theme)
        {
            Color32 body, dark, light, trim;
            switch (theme)
            {
                case "nordic": body = C(232, 230, 224); dark = C(200, 198, 192); light = C(248, 246, 242); trim = C(176, 172, 166); break;
                case "tatami": body = C(118, 84, 58); dark = C(92, 64, 44); light = C(146, 106, 74); trim = C(70, 48, 34); break;
                case "retro": body = C(92, 60, 42); dark = C(70, 44, 30); light = C(204, 164, 84); trim = C(56, 36, 26); break;    // 놋쇠 몰딩
                case "candy": body = C(250, 236, 240); dark = C(232, 196, 210); light = C(255, 250, 252); trim = C(220, 170, 190); break;
                case "cabin": body = C(150, 146, 140); dark = C(118, 114, 110); light = C(176, 172, 166); trim = C(96, 90, 86); break;   // 돌 벽
                default: return RoomDrawing.WainscotTile();
            }
            var c = new PixelCanvas(32, 32);
            c.FillRect(0, 0, 31, 31, body);
            c.FillRect(0, 0, 31, 3, trim);
            c.HLine(0, 31, 4, dark);
            c.FillRect(0, 28, 31, 31, light);
            c.HLine(0, 31, 27, dark);
            if (theme == "cabin")
            {
                for (int y = 6; y < 26; y += 7) for (int x = (y / 7 % 2) * 6; x < 32; x += 12) c.RectOutline(x, y, Mathf.Min(31, x + 10), y + 5, dark);
            }
            else
            {
                c.RectOutline(3, 7, 28, 24, dark);
                c.HLine(4, 27, 23, light); c.VLine(4, 8, 23, light);
            }
            return c;
        }

        // 상점 카드용 미리보기(32×32): 위 벽지 · 가운데 징두리 · 아래 바닥
        public static PixelCanvas Swatch(string theme)
        {
            var w = Wall(theme); var s = Wainscot(theme); var f = Floor(theme);
            var c = new PixelCanvas(32, 32);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    Color32 col = y >= 20 ? w.Get(x, y) : y >= 12 ? s.Get(x, (y - 12) * 4) : f.Get(x, y * 2);
                    c.Set(x, y, col);
                }
            c.RectOutline(0, 0, 31, 31, PixelPalette.Outline);
            return c;
        }
    }
}
