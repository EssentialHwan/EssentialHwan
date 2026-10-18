using UnityEngine;

namespace YeogiCafe.Art
{
    // 주방(1인칭 카운터 시점) 리소스: 바리스타 고양이 앞발, 조리 기구 5종, 메뉴 8종 접시 아이콘, 카운터 상판.
    // 같은 팔레트·아웃라인 규칙 → 홀(3인칭)과 주방(1인칭)이 한 게임처럼 보인다.
    public static class CookingDrawing
    {
        // 플레이어(바리스타 고양이) 털색 — 손님 8마리와 겹치지 않는 크림+분홍 젤리
        static readonly Color32 FurL = new(252, 244, 232, 255);
        static readonly Color32 FurM = new(236, 222, 204, 255);
        static readonly Color32 FurS = new(206, 188, 166, 255);

        // ── 앞발(왼쪽). 화면 아래에서 비스듬히 올라오는 팔 + 발등(발가락 4개). 오른발은 좌우반전해서 사용 ──
        // 플레이어 앞발은 PawDrawing(고해상도)로 이전. 왼발 = 바깥쪽이 왼쪽.
        public static PixelCanvas PawLeft() => PawDrawing.Back();
        public static PixelCanvas PawRight() => PawDrawing.Mirror(PawDrawing.Back());
        public static PixelCanvas PawLeftPressed() => PawDrawing.Back(true);
        public static PixelCanvas PawRightPressed() => PawDrawing.Mirror(PawDrawing.Back(true));

        static PixelCanvas Mirror(PixelCanvas src)
        {
            var c = new PixelCanvas(src.W, src.H);
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                    c.Set(src.W - 1 - x, y, src.Get(x, y));
            return c;
        }

        // ── 조리 기구 5종 (48×48, 바닥선 y=2) ──

        public static PixelCanvas ToolCoffee()
        {
            var c = new PixelCanvas(48, 48);
            c.FillRoundRect(8, 2, 39, 40, 3, PixelPalette.Metal);
            c.FillRect(10, 30, 37, 38, PixelPalette.MetalDark);                 // 상단 패널
            c.Disc(16, 34, 2, PixelPalette.PinkMid); c.Disc(23, 34, 2, PixelPalette.GreenMid);
            c.FillRect(30, 32, 35, 36, PixelPalette.Chalk);                     // 작은 화면
            c.FillRect(14, 12, 33, 28, PixelPalette.MetalDark);                 // 추출 공간
            c.FillRect(20, 24, 27, 28, PixelPalette.Metal);                     // 그룹헤드
            c.VLine(22, 20, 23, PixelPalette.WoodDeep); c.VLine(25, 20, 23, PixelPalette.WoodDeep); // 커피 줄기
            c.FillRect(19, 13, 28, 19, PixelPalette.White);                     // 컵
            c.HLine(19, 28, 19, PixelPalette.WoodDark);
            c.FillRect(29, 15, 30, 17, PixelPalette.White);
            c.FillRect(12, 4, 35, 8, PixelPalette.MetalDark);                   // 드립 트레이
            for (int x = 13; x < 35; x += 3) c.Set(x, 7, PixelPalette.Metal);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas ToolKettle()
        {
            var c = new PixelCanvas(48, 48);
            c.FillRoundRect(6, 2, 41, 9, 1, PixelPalette.MetalDark);           // 인덕션
            c.HLine(10, 37, 9, PixelPalette.Metal);
            c.Disc(36, 5, 1, PixelPalette.PinkMid);
            c.Disc(23, 20, 11, PixelPalette.BlueMid);                           // 주전자 몸통
            c.Disc(21, 22, 9, PixelPalette.BlueLight);
            c.FillRect(13, 10, 33, 13, PixelPalette.BlueMid);
            c.FillRect(19, 31, 27, 33, PixelPalette.BlueDark);                  // 뚜껑
            c.Disc(23, 35, 2, PixelPalette.WoodMid);                            // 손잡이 꼭지
            for (int i = 0; i < 8; i++) c.Set(34 + i, 18 + i, PixelPalette.BlueMid);   // 주둥이
            for (int i = 0; i < 8; i++) c.Set(34 + i, 17 + i, PixelPalette.BlueDark);
            c.FillRect(6, 16, 8, 26, PixelPalette.WoodDark);                    // 손잡이
            c.Set(17, 25, PixelPalette.Highlight); c.Set(16, 24, PixelPalette.Highlight);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas ToolOven()
        {
            var c = new PixelCanvas(48, 48);
            c.FillRoundRect(4, 2, 43, 42, 2, PixelPalette.CreamMid);
            c.FillRect(6, 34, 41, 40, PixelPalette.CreamDark);                  // 다이얼 패널
            c.Disc(12, 37, 2, PixelPalette.WoodDark); c.Disc(20, 37, 2, PixelPalette.WoodDark);
            c.FillRect(30, 36, 38, 38, PixelPalette.Chalk);
            c.FillRect(8, 8, 39, 30, PixelPalette.WoodDeep);                    // 문
            c.FillRect(11, 11, 36, 27, PixelPalette.YellowDark);                // 유리 안 불빛
            c.FillRect(13, 13, 34, 25, PixelPalette.YellowMid);
            c.HLine(13, 34, 16, PixelPalette.MetalDark);                        // 선반
            c.HLine(10, 37, 31, PixelPalette.Metal);                            // 손잡이
            c.FillRect(8, 2, 11, 4, PixelPalette.MetalDark); c.FillRect(36, 2, 39, 4, PixelPalette.MetalDark);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas ToolBoard()
        {
            var c = new PixelCanvas(48, 48);
            c.FillRoundRect(3, 2, 44, 14, 3, PixelPalette.WoodLight);           // 도마
            c.HLine(5, 42, 14, PixelPalette.Highlight);
            c.HLine(5, 42, 3, PixelPalette.WoodDark);
            c.Disc(40, 9, 1, PixelPalette.WoodDark);                            // 걸이 구멍
            // 칼
            c.FillRect(8, 15, 26, 17, PixelPalette.Metal); c.HLine(8, 26, 17, PixelPalette.Highlight);
            c.FillRect(27, 15, 34, 17, PixelPalette.WoodDeep);
            // 재료(빵·딸기·참치)
            c.FillRoundRect(10, 19, 22, 25, 2, PixelPalette.YellowLight); c.HLine(11, 21, 25, PixelPalette.YellowMid);
            c.Disc(30, 22, 3, PixelPalette.PinkMid); c.Set(30, 25, PixelPalette.GreenMid);
            c.FillRoundRect(33, 28, 42, 33, 2, PixelPalette.PinkLight);
            // 소스병
            c.FillRoundRect(13, 27, 19, 40, 2, PixelPalette.YellowMid); c.FillRect(15, 41, 17, 43, PixelPalette.PinkMid);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas ToolGrill()
        {
            var c = new PixelCanvas(48, 48);
            c.FillRoundRect(5, 2, 42, 18, 2, PixelPalette.MetalDark);           // 본체
            c.FillRect(7, 14, 40, 17, PixelPalette.Chalk);                      // 숯
            for (int x = 9; x < 39; x += 4) { c.Set(x, 15, PixelPalette.PinkDark); c.Set(x + 2, 16, PixelPalette.YellowMid); }
            for (int x = 7; x <= 40; x += 3) c.VLine(x, 18, 19, PixelPalette.Metal);   // 석쇠
            c.HLine(6, 41, 19, PixelPalette.Metal);
            // 생선
            c.FillRoundRect(12, 20, 32, 25, 2, PixelPalette.BlueLight);
            for (int i = 0; i < 4; i++) c.VLine(33 + i, 20 + i / 2, 25 - i / 2, PixelPalette.BlueMid);   // 꼬리
            c.Set(14, 23, PixelPalette.Outline);
            for (int x = 17; x < 31; x += 4) c.VLine(x, 21, 24, PixelPalette.WoodDeep);              // 그릴 자국
            // 연기
            c.Set(18, 30, PixelPalette.CreamDark); c.Set(19, 32, PixelPalette.CreamDark); c.Set(18, 34, PixelPalette.CreamDark);
            c.Set(26, 31, PixelPalette.CreamDark); c.Set(27, 33, PixelPalette.CreamDark);
            c.FillRect(5, 2, 8, 4, PixelPalette.Outline); c.FillRect(39, 2, 42, 4, PixelPalette.Outline);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        // ── 메뉴 접시 아이콘 (24×24) — 주문 말풍선·티켓·완성품 ──

        public static PixelCanvas Dish(string menuId)
        {
            var c = new PixelCanvas(24, 24);
            switch (menuId)
            {
                case "menu_americano":
                    Cup(c, PixelPalette.White, PixelPalette.WoodDeep); break;
                case "menu_catnip_tea":
                    Cup(c, PixelPalette.White, PixelPalette.GreenMid);
                    c.Set(12, 15, PixelPalette.GreenDark); break;
                case "menu_milk":
                    c.FillRect(8, 3, 15, 17, PixelPalette.SkyLow); c.FillRect(9, 4, 14, 14, PixelPalette.White);
                    c.HLine(8, 15, 17, PixelPalette.BlueLight); break;
                case "menu_fishcake":
                    Plate(c);
                    c.FillRoundRect(5, 6, 16, 12, 2, PixelPalette.YellowLight);
                    c.HLine(6, 15, 12, PixelPalette.PinkLight);
                    for (int i = 0; i < 3; i++) c.VLine(17 + i, 7 + i, 11 - i, PixelPalette.YellowLight);
                    c.Set(7, 9, PixelPalette.Outline); break;
                case "menu_parfait":
                    for (int y = 4; y <= 16; y++) { int h = 2 + (y - 4) / 3; c.HLine(12 - h, 11 + h, y, y > 11 ? PixelPalette.White : PixelPalette.PinkLight); }
                    c.VLine(11, 1, 3, PixelPalette.SkyLow); c.VLine(12, 1, 3, PixelPalette.SkyLow);
                    c.Disc(12, 18, 2, PixelPalette.PinkMid); c.Set(12, 21, PixelPalette.GreenMid); break;
                case "menu_tuna_sand":
                    Plate(c);
                    for (int i = 0; i < 10; i++) c.HLine(5 + i / 2, 18 - i, 6 + i, i < 3 ? PixelPalette.PinkLight : PixelPalette.YellowLight);
                    c.HLine(5, 18, 6, PixelPalette.YellowMid); break;
                case "menu_pudding":
                    Plate(c);
                    for (int y = 6; y <= 14; y++) { int h = 6 - (y - 6) / 3; c.HLine(12 - h, 11 + h, y, PixelPalette.YellowLight); }
                    c.HLine(9, 14, 15, PixelPalette.WoodMid); c.HLine(10, 13, 16, PixelPalette.WoodDark);
                    c.Disc(12, 18, 1, PixelPalette.PinkMid); break;
                case "menu_grilled_fish":
                    Plate(c);
                    c.FillRoundRect(4, 6, 16, 10, 2, PixelPalette.BlueMid);
                    for (int i = 0; i < 3; i++) c.VLine(17 + i, 6 + i, 10 - i, PixelPalette.BlueMid);
                    for (int x = 7; x < 16; x += 3) c.VLine(x, 7, 9, PixelPalette.WoodDeep);
                    c.Set(5, 8, PixelPalette.Outline); break;
                default:
                    Plate(c); break;
            }
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        static void Cup(PixelCanvas c, Color32 cup, Color32 drink)
        {
            c.FillRoundRect(5, 2, 18, 5, 1, PixelPalette.CreamMid);              // 받침
            c.FillRoundRect(7, 4, 16, 14, 1, cup);
            c.HLine(8, 15, 13, drink); c.HLine(8, 15, 12, drink);
            c.FillRect(17, 8, 19, 11, cup); c.Set(18, 9, PixelPalette.Transparent);  // 손잡이
            c.Set(10, 16, PixelPalette.CreamDark); c.Set(11, 18, PixelPalette.CreamDark); c.Set(13, 17, PixelPalette.CreamDark); // 김
        }

        static void Plate(PixelCanvas c)
        {
            c.FillRoundRect(2, 2, 21, 6, 2, PixelPalette.White);
            c.HLine(4, 19, 2, PixelPalette.CreamDark);
        }

        // ── 카운터(1인칭 전경): 상판(대리석, 가로 타일 32×16) + 앞판(원목, 타일 64×32) ──
        public static PixelCanvas CounterTopTile()
        {
            var c = new PixelCanvas(32, 16);
            c.FillRect(0, 0, 31, 15, PixelPalette.WallLight);
            c.HLine(0, 31, 15, PixelPalette.Highlight);
            c.HLine(0, 31, 0, PixelPalette.CreamDark); c.HLine(0, 31, 1, PixelPalette.CreamMid);
            c.Set(5, 9, PixelPalette.CreamMid); c.Set(6, 10, PixelPalette.CreamMid); c.Set(19, 6, PixelPalette.CreamMid);
            c.Set(26, 11, PixelPalette.CreamMid); c.Set(13, 4, PixelPalette.CreamMid);
            return c;
        }

        public static PixelCanvas CounterFrontTile()
        {
            var c = new PixelCanvas(64, 32);
            c.FillRect(0, 0, 63, 31, PixelPalette.WoodMid);
            for (int x = 0; x < 64; x += 32) { c.RectOutline(x + 3, 4, x + 28, 27, PixelPalette.WoodDark); c.HLine(x + 4, x + 27, 26, PixelPalette.WoodLight); }
            c.HLine(0, 63, 31, PixelPalette.WoodDeep);
            return c;
        }
    }
}
