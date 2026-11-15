using UnityEngine;

namespace YeogiCafe.Art
{
    // 시작 화면 배경(320×180, 화면에 4배로 깔림). 벽지·간판·넓은 창(아침 하늘·마을)·징두리 판자·나무 바닥·조명·화분.
    public static class TitleDrawing
    {
        public const int W = 320, H = 180;
        static Color32 C(int r, int g, int b, int a = 255) => new((byte)r, (byte)g, (byte)b, (byte)a);
        static Color32 Mix(Color32 a, Color32 b, float t) => C((int)(a.r + (b.r - a.r) * t), (int)(a.g + (b.g - a.g) * t), (int)(a.b + (b.b - a.b) * t));

        static readonly Color32 Out = C(92, 64, 52), WoodL = C(214, 172, 128), WoodM = C(186, 140, 98), WoodD = C(150, 108, 74), WoodDD = C(118, 82, 58);
        static readonly Color32 Wall = C(247, 236, 218), WallS = C(238, 224, 202), Cream = C(252, 246, 234);

        public static PixelCanvas Background()
        {
            var c = new PixelCanvas(W, H);
            // 벽지(세로 줄무늬 + 작은 점)
            c.FillRect(0, 44, W - 1, H - 1, Wall);
            for (int x = 0; x < W; x += 12) c.FillRect(x, 44, x + 5, H - 1, WallS);
            for (int y = 52; y < H; y += 14) for (int x = (y / 14 % 2) * 6 + 3; x < W; x += 12) c.Set(x, y, C(232, 200, 196));

            Window(c, 34, 66, 286, 126);

            // 징두리 판자
            c.FillRect(0, 40, W - 1, 64, WoodM);
            c.HLine(0, W - 1, 64, WoodD); c.HLine(0, W - 1, 63, WoodL); c.HLine(0, W - 1, 41, WoodD);
            for (int x = 6; x + 36 < W; x += 44) { c.RectOutline(x, 45, x + 36, 59, WoodD); c.HLine(x + 1, x + 35, 58, WoodL); }

            // 나무 바닥(원근감 있는 판자)
            for (int y = 0; y < 40; y++)
            {
                var row = (y / 5) % 2 == 0 ? WoodL : C(206, 164, 120);
                c.HLine(0, W - 1, y, row);
                if (y % 5 == 0) c.HLine(0, W - 1, y, C(178, 134, 94));
            }
            for (int y = 0; y < 40; y += 5)
                for (int x = ((y / 5) * 37) % 60; x < W; x += 60) c.VLine(x, y + 1, y + 4, C(178, 134, 94));
            c.HLine(0, W - 1, 39, WoodD);

            // 천장 조명 두 개
            Lamp(c, 70, H - 1); Lamp(c, 250, H - 1);
            // 왼쪽 화분, 오른쪽 작은 칠판
            Plant(c, 14, 40);
            Board(c, 292, 72);
            return c;
        }

        static void Window(PixelCanvas c, int x0, int y0, int x1, int y1)
        {
            // 하늘(아침: 위 하늘색 → 아래 살구빛)
            for (int y = y0; y <= y1; y++)
            {
                float t = (y - y0) / (float)(y1 - y0);
                c.HLine(x0, x1, y, Mix(C(252, 214, 186), C(170, 214, 238), t));
            }
            // 구름
            void Cloud(int cx, int cy, int w)
            {
                for (int i = 0; i < w; i += 5) c.Disc(cx + i, cy + ((i / 5) % 2), 4, C(255, 252, 248));
                c.HLine(cx - 3, cx + w + 2, cy - 3, C(240, 234, 236));
            }
            Cloud(60, 112, 26); Cloud(150, 118, 34); Cloud(236, 108, 22);
            // 먼 지붕들(파스텔 집)
            var houses = new[] { C(240, 190, 196), C(200, 214, 236), C(246, 222, 170), C(214, 200, 232), C(236, 200, 176) };
            int hx = x0 - 6, k = 0;
            while (hx < x1)
            {
                int w = 22 + (k * 7) % 12, h = 18 + (k * 11) % 14;
                var col = houses[k % houses.Length];
                c.FillRect(Mathf.Max(x0, hx), y0, Mathf.Min(x1, hx + w), y0 + h, col);
                for (int r = 0; r < 7; r++) c.HLine(Mathf.Max(x0, hx - 1 + r), Mathf.Min(x1, hx + w + 1 - r), y0 + h + 1 + r, C(214, 120, 104));
                for (int wx = hx + 4; wx + 3 < hx + w && wx + 3 <= x1; wx += 8)
                    if (wx >= x0) c.FillRect(wx, y0 + h - 8, wx + 3, y0 + h - 4, C(255, 246, 214));
                hx += w + 3; k++;
            }
            // 창틀(굵은 나무 + 십자 창살)
            for (int t = 0; t < 4; t++) c.RectOutline(x0 - t, y0 - t, x1 + t, y1 + t, t == 0 ? WoodDD : t == 3 ? Out : WoodM);
            for (int i = 1; i < 4; i++) { int x = x0 + (x1 - x0) * i / 4; c.FillRect(x - 1, y0, x + 1, y1, WoodM); c.VLine(x - 1, y0, y1, WoodL); }
            c.FillRect(x0, y0 + (y1 - y0) / 2 - 1, x1, y0 + (y1 - y0) / 2 + 1, WoodM);
            // 유리 반사 사선
            for (int i = 0; i < 3; i++)
                for (int d = 0; d < 10; d++) { int x = x0 + 14 + i * 64 + d, y = y1 - 6 - d; if (y > y0) { c.Blend(x, y, C(255, 255, 255, 110)); c.Blend(x + 1, y, C(255, 255, 255, 70)); } }
            // 창턱
            c.FillRect(x0 - 8, y0 - 8, x1 + 8, y0 - 4, WoodL); c.HLine(x0 - 8, x1 + 8, y0 - 8, WoodDD); c.HLine(x0 - 8, x1 + 8, y0 - 4, WoodD);
        }

        static void Lamp(PixelCanvas c, int x, int top)
        {
            c.VLine(x, top - 14, top, Out);
            for (int r = 0; r < 7; r++) c.HLine(x - 3 - r, x + 3 + r, top - 15 - r, r == 6 ? WoodDD : C(240, 196, 120));
            c.HLine(x - 9, x + 9, top - 22, Out);
            c.Disc(x, top - 24, 2, C(255, 240, 190));
            for (int r = 4; r < 22; r += 3)                                    // 은은한 빛 번짐
                for (int a = -r; a <= r; a++) c.Blend(x + a, top - 24 - r / 2, C(255, 236, 180, 10));
        }

        static void Plant(PixelCanvas c, int x, int floorY)
        {
            c.FillRect(x - 6, floorY - 2, x + 6, floorY + 10, C(214, 140, 112)); c.HLine(x - 7, x + 7, floorY + 10, C(186, 110, 90));
            c.RectOutline(x - 6, floorY - 2, x + 6, floorY + 10, Out);
            var g1 = C(126, 170, 112); var g2 = C(160, 200, 132);
            int[] lx = { -8, -3, 3, 8, 0, -6, 6 }; int[] ly = { 18, 24, 22, 16, 30, 28, 26 };
            for (int i = 0; i < lx.Length; i++) { c.Disc(x + lx[i], floorY + ly[i], 4, i % 2 == 0 ? g1 : g2); }
            for (int i = 0; i < lx.Length; i++) c.Set(x + lx[i] - 1, floorY + ly[i] + 2, C(200, 230, 170));
        }

        static void Board(PixelCanvas c, int x, int y)
        {
            c.FillRect(x - 12, y, x + 12, y + 24, C(70, 96, 80)); c.RectOutline(x - 13, y - 1, x + 13, y + 25, WoodD);
            for (int i = 0; i < 4; i++) c.HLine(x - 8, x + 4 - (i % 2) * 5, y + 18 - i * 4, C(220, 230, 214));
            c.Disc(x + 7, y + 6, 2, C(240, 200, 120));
        }

        // 간판(로고 뒤 나무판). 9-slice 아님: 그대로 확대해서 쓴다.
        public static PixelCanvas Sign()
        {
            var c = new PixelCanvas(150, 40);
            c.FillRoundRect(2, 2, 147, 33, 5, WoodM);
            for (int y = 6; y < 32; y += 6) c.HLine(6, 143, y, WoodD);
            c.FillRoundRect(6, 5, 143, 30, 4, C(250, 240, 222));
            c.RectOutline(6, 5, 143, 30, WoodL);
            c.AutoOutline(Out);
            // 걸이 끈 + 고리
            c.VLine(26, 34, 39, Out); c.VLine(123, 34, 39, Out);
            c.Set(25, 34, WoodDD); c.Set(124, 34, WoodDD);
            // 모서리 발바닥
            void Paw(int px, int py)
            {
                c.Disc(px, py, 2, C(236, 160, 170));
                c.Set(px - 3, py + 3, C(236, 160, 170)); c.Set(px, py + 4, C(236, 160, 170)); c.Set(px + 3, py + 3, C(236, 160, 170));
            }
            Paw(14, 11); Paw(135, 11);
            return c;
        }
    }
}
