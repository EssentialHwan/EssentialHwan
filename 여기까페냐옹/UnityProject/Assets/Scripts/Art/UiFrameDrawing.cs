using UnityEngine;

namespace YeogiCafe.Art
{
    // UI 창·버튼 테두리(9-slice용 도트). 밋밋한 단색 사각형 대신 원목 테두리·종이·말풍선 느낌.
    // 모든 스프라이트는 가장자리(Border)만 늘어나지 않고 가운데가 늘어난다(RuntimeSprites.GetSliced).
    public static class UiFrameDrawing
    {
        static Color32 C(int r, int g, int b, int a = 255) => new((byte)r, (byte)g, (byte)b, (byte)a);

        static readonly Color32 Ink = C(74, 54, 47);
        static readonly Color32 WoodHi = C(214, 172, 124), Wood = C(184, 142, 100), WoodMid = C(164, 122, 84), WoodDark = C(126, 88, 60);
        static readonly Color32 Cream = C(250, 240, 222), CreamShade = C(236, 220, 196);
        static readonly Color32 Gold = C(240, 204, 120), GoldDark = C(184, 140, 72);

        static bool InRound(int x, int y, int w, int h, int r)
        {
            int cx = x < r ? r : x > w - 1 - r ? w - 1 - r : x;
            int cy = y < r ? r : y > h - 1 - r ? h - 1 - r : y;
            float dx = x - cx, dy = y - cy;
            return dx * dx + dy * dy <= (r + 0.25f) * (r + 0.25f);
        }

        // 원목 창틀: 바깥 잉크선 · 나무 띠(윗면 밝게) · 안쪽 그림자선 · 크림 안판 · 네 귀퉁이 금색 못
        public const int WindowSize = 40, WindowBorder = 13;
        public static PixelCanvas Window()
        {
            int S = WindowSize;
            var c = new PixelCanvas(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    if (!InRound(x, y, S, S, 6)) continue;
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                    Color32 col;
                    if (d == 0) col = Ink;
                    else if (d <= 8)
                    {
                        // 나무 띠: 위쪽·왼쪽은 밝게, 아래·오른쪽은 어둡게 + 나뭇결
                        bool topLeft = (S - 1 - y) == d || x == d;
                        col = d == 1 ? WoodHi : topLeft ? Wood : WoodMid;
                        if (d >= 3 && d <= 7 && ((x * 7 + y * 3) % 11 == 0)) col = WoodDark;
                        if (d == 8) col = WoodDark;
                    }
                    else if (d == 9) col = Ink;
                    else col = d == 10 && y > S / 2 ? CreamShade : Cream;
                    c.Set(x, y, col);
                }
            // 금색 못(네 귀퉁이)
            foreach (var (px, py) in new[] { (4, 4), (S - 5, 4), (4, S - 5), (S - 5, S - 5) })
            {
                c.Set(px, py, Gold); c.Set(px + 1, py, Gold); c.Set(px, py + 1, Gold); c.Set(px + 1, py + 1, GoldDark);
                c.Set(px, py + 1, C(255, 240, 190));
            }
            return c;
        }

        // 종이: 크림 바탕, 갈색 얇은 선, 오른쪽 위 접힌 귀퉁이, 아래에 얇은 그림자
        public const int PaperSize = 24, PaperBorder = 8;
        public static PixelCanvas Paper()
        {
            int S = PaperSize;
            var c = new PixelCanvas(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    if (!InRound(x, y, S, S, 2)) continue;
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                    Color32 col = d == 0 ? C(150, 112, 82) : y == 1 ? C(214, 196, 168) : C(253, 247, 234);
                    if (d == 0 && y == 0) col = C(110, 80, 60);      // 바닥선 진하게(살짝 들뜬 느낌)
                    c.Set(x, y, col);
                }
            // 접힌 귀퉁이(오른쪽 위 6px)
            for (int i = 0; i < 6; i++)
                for (int j = 0; j <= i; j++)
                {
                    int x = S - 6 + i, y = S - 1 - j;
                    c.Set(x, y, PixelPalette.Transparent);
                }
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 6 - i; j++) c.Set(S - 7 + i, S - 7 + j + i, C(232, 214, 186));
                c.Set(S - 7 + i, S - 1 - i + 0, C(150, 112, 82));
                c.Set(S - 7, S - 7 + i, C(150, 112, 82)); c.Set(S - 7 + i, S - 7, C(150, 112, 82));
            }
            return c;
        }

        // 카드(목록 행): 둥근 모서리 + 1px 테두리 + 아래 2px 그림자. 흰색 위주라 색으로 물들일 수 있다.
        public const int CardSize = 16, CardBorder = 5;
        public static PixelCanvas Card()
        {
            int S = CardSize;
            var c = new PixelCanvas(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    if (!InRound(x, y, S, S, 3)) continue;
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                    Color32 col = d == 0 ? C(140, 120, 110) : y <= 2 ? C(220, 214, 208) : y == S - 2 ? C(255, 255, 255) : C(246, 244, 240);
                    c.Set(x, y, col);
                }
            return c;
        }

        // 버튼: 흰색 기반(Image.color로 물들임). 위 1px 하이라이트, 아래 3px 입체 턱, 테두리는 색의 어두운 판
        public const int ButtonSize = 16, ButtonBorder = 6;
        public static PixelCanvas Button(bool pressed)
        {
            int S = ButtonSize;
            var c = new PixelCanvas(S, S);
            int lip = pressed ? 1 : 3;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    if (!InRound(x, y, S, S, 4)) continue;
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                    Color32 col;
                    if (d == 0) col = C(96, 82, 78);
                    else if (y <= lip) col = C(176, 164, 160);                 // 아래 턱(그림자)
                    else if (y == S - 2) col = C(255, 255, 255);
                    else col = C(238, 236, 234);
                    c.Set(x, y, col);
                }
            return c;
        }

        // 말풍선 몸통: 흰 둥근 사각 + 잉크 테두리
        public const int BubbleSize = 16, BubbleBorder = 6;
        public static PixelCanvas Bubble()
        {
            int S = BubbleSize;
            var c = new PixelCanvas(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    if (!InRound(x, y, S, S, 5)) continue;
                    bool edge = !InRound(x - 1, y - 1, S - 2, S - 2, 4) || x == 0 || y == 0 || x == S - 1 || y == S - 1;
                    c.Set(x, y, edge ? Ink : y == 1 ? C(236, 228, 220) : C(255, 252, 246));
                }
            return c;
        }

        // 말풍선 꼬리(아래로 뾰족, 위쪽은 몸통과 겹쳐 이어짐)
        public static PixelCanvas BubbleTail()
        {
            var c = new PixelCanvas(10, 7);
            for (int y = 0; y < 7; y++)
            {
                int half = y * 5 / 6;                 // 아래(0)는 뾰족, 위(6)는 넓게
                for (int x = 5 - half - 1; x <= 4 + half; x++)
                {
                    if (x < 0 || x > 9) continue;
                    bool edge = x == 5 - half - 1 || x == 4 + half || y == 0;
                    c.Set(x, y, y == 6 ? C(255, 252, 246) : edge ? Ink : C(255, 252, 246));
                }
            }
            return c;
        }

        // 상단 HUD 원목 판자: 나뭇결 + 아래 진한 가장자리 + 위 밝은 선
        public const int PlankW = 32, PlankH = 24, PlankBorder = 6;
        public static PixelCanvas Plank()
        {
            var c = new PixelCanvas(PlankW, PlankH);
            for (int y = 0; y < PlankH; y++)
                for (int x = 0; x < PlankW; x++)
                {
                    Color32 col = y == 0 ? Ink : y <= 2 ? WoodDark : y == PlankH - 1 ? WoodHi : C(232, 210, 178);
                    if (y > 2 && y < PlankH - 1 && ((x * 5 + y * 11) % 23 == 0)) col = C(220, 194, 158);
                    c.Set(x, y, col);
                }
            return c;
        }
    }
}
