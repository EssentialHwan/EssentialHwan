using UnityEngine;

namespace YeogiCafe.Art
{
    // 카페 공간(배경) 리소스. 바닥·벽·징두리·창·문·카운터·조명·메뉴판.
    // 가구/고양이와 같은 팔레트·아웃라인 규칙 → 공간과 오브젝트의 톤이 어긋나지 않는다.
    // 타일(바닥/벽/징두리)은 32×32 심리스, 소품은 크기 자유(1유닛=32px).
    public static class RoomDrawing
    {
        // ── 타일 ──

        // 따뜻한 원목 마루(가로 판재 4줄, 이음새 엇갈림). 심리스.
        public static PixelCanvas FloorTile()
        {
            var c = new PixelCanvas(32, 32);
            var tones = new[] { PixelPalette.WoodLight, PixelPalette.WoodMid, PixelPalette.WoodLight, PixelPalette.WoodMid };
            for (int row = 0; row < 4; row++)
            {
                int y0 = row * 8;
                c.FillRect(0, y0, 31, y0 + 7, tones[row]);
                c.HLine(0, 31, y0, PixelPalette.WoodDark);                 // 판재 사이 그림자
                int seam = (row % 2 == 0) ? 11 : 27;                       // 엇갈린 이음새
                c.VLine(seam, y0 + 1, y0 + 7, PixelPalette.WoodDark);
                // 나뭇결(점 몇 개)
                c.Set((seam + 7) % 32, y0 + 4, PixelPalette.WoodDark);
                c.Set((seam + 15) % 32, y0 + 3, row % 2 == 0 ? PixelPalette.WoodMid : PixelPalette.WoodLight);
            }
            return c;
        }

        // 크림 벽지(은은한 세로 줄 + 작은 점무늬). 심리스.
        public static PixelCanvas WallTile()
        {
            var c = new PixelCanvas(32, 32);
            c.FillRect(0, 0, 31, 31, PixelPalette.WallLight);
            for (int x = 0; x < 32; x += 8) c.VLine(x, 0, 31, PixelPalette.WallMid);
            for (int y = 3; y < 32; y += 8)
                for (int x = 4; x < 32; x += 8)
                    c.Set(x, y + ((x / 8) % 2) * 4, PixelPalette.CreamMid);
            return c;
        }

        // 징두리 판벽(벽 하단 원목 패널 + 걸레받이). 32×32, 위쪽이 벽지와 만남.
        public static PixelCanvas WainscotTile()
        {
            var c = new PixelCanvas(32, 32);
            c.FillRect(0, 0, 31, 31, PixelPalette.WoodMid);
            c.FillRect(0, 0, 31, 3, PixelPalette.WoodDeep);              // 걸레받이
            c.HLine(0, 31, 4, PixelPalette.WoodDark);
            c.FillRect(0, 28, 31, 31, PixelPalette.WoodLight);           // 상단 몰딩
            c.HLine(0, 31, 27, PixelPalette.WoodDark);
            c.RectOutline(3, 7, 28, 24, PixelPalette.WoodDark);          // 패널 몰딩
            c.HLine(4, 27, 23, PixelPalette.WoodLight);
            c.VLine(4, 8, 23, PixelPalette.WoodLight);
            return c;
        }

        // ── 소품 ──

        // 큰 창(64×48): 하늘·구름·먼 나무·창틀·창턱.
        public static PixelCanvas WindowBig()
        {
            var c = new PixelCanvas(64, 48);
            // 하늘 그라데이션(위 진하게)
            c.FillRect(4, 6, 59, 45, PixelPalette.SkyLow);
            c.FillRect(4, 30, 59, 45, PixelPalette.SkyTop);
            c.Dither(4, 26, 59, 29, PixelPalette.SkyTop);
            // 구름
            c.Disc(16, 34, 3, PixelPalette.White); c.Disc(20, 35, 4, PixelPalette.White); c.Disc(25, 34, 3, PixelPalette.White);
            c.Disc(44, 39, 2, PixelPalette.White); c.Disc(47, 40, 3, PixelPalette.White);
            // 먼 나무·언덕
            for (int x = 4; x <= 59; x++)
            {
                int h = 10 + (int)(3 * Mathf.Sin(x * 0.35f)) + ((x / 7) % 2);
                c.VLine(x, 6, 6 + h, PixelPalette.GreenMid);
                c.Set(x, 6 + h, PixelPalette.GreenLight);
            }
            c.FillRect(4, 6, 59, 9, PixelPalette.GreenDark);
            // 창틀
            c.RectOutline(2, 4, 61, 47, PixelPalette.WoodDark);
            c.RectOutline(3, 5, 60, 46, PixelPalette.WoodMid);
            c.FillRect(31, 5, 32, 46, PixelPalette.WoodMid); c.VLine(31, 5, 46, PixelPalette.WoodDark);
            c.FillRect(3, 25, 60, 26, PixelPalette.WoodMid); c.HLine(3, 60, 25, PixelPalette.WoodDark);
            // 유리 반사광
            for (int i = 0; i < 6; i++) { c.Set(8 + i, 42 - i, PixelPalette.Highlight); c.Set(37 + i, 42 - i, PixelPalette.Highlight); }
            // 창턱
            c.FillRect(0, 0, 63, 3, PixelPalette.WoodLight);
            c.HLine(0, 63, 0, PixelPalette.WoodDark);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        // 출입문(32×64): 원목 문 + 유리창 + 손잡이 + 걸린 'OPEN' 팻말 대신 고양이 발자국 팻말.
        public static PixelCanvas Door()
        {
            var c = new PixelCanvas(32, 64);
            c.FillRect(2, 0, 29, 61, PixelPalette.WoodDeep);              // 문틀
            c.FillRect(4, 0, 27, 59, PixelPalette.WoodMid);               // 문짝
            c.VLine(4, 0, 59, PixelPalette.WoodLight);
            c.FillRect(7, 34, 24, 54, PixelPalette.SkyLow);               // 유리
            c.FillRect(7, 34, 24, 39, PixelPalette.GreenMid);
            c.RectOutline(7, 34, 24, 54, PixelPalette.WoodDark);
            c.Set(10, 51, PixelPalette.Highlight); c.Set(11, 50, PixelPalette.Highlight); c.Set(12, 49, PixelPalette.Highlight);
            c.RectOutline(7, 6, 24, 28, PixelPalette.WoodDark);           // 하부 패널
            c.FillRect(22, 26, 24, 30, PixelPalette.YellowMid);           // 손잡이
            // 팻말(발자국)
            c.FillRect(10, 42, 21, 49, PixelPalette.CreamLight);
            c.RectOutline(10, 42, 21, 49, PixelPalette.WoodDark);
            c.Disc(15, 45, 1, PixelPalette.PinkMid); c.Set(13, 47, PixelPalette.PinkMid); c.Set(15, 48, PixelPalette.PinkMid); c.Set(17, 47, PixelPalette.PinkMid);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        // 카운터(96×56): 원목 바 + 커피머신 + 컵 + 케이크 진열. 접지선 y=4.
        public static PixelCanvas Counter()
        {
            var c = new PixelCanvas(96, 56);
            c.GroundShadow(48, 4, 44);
            // 몸체
            c.FillRect(4, 4, 91, 30, PixelPalette.WoodMid);
            for (int x = 10; x < 88; x += 14) c.RectOutline(x, 8, x + 10, 26, PixelPalette.WoodDark);
            c.FillRect(4, 4, 91, 6, PixelPalette.WoodDeep);
            // 상판
            c.FillRect(2, 30, 93, 33, PixelPalette.CreamMid); c.HLine(2, 93, 33, PixelPalette.CreamLight);
            c.HLine(2, 93, 30, PixelPalette.CreamDark);
            // 커피머신
            c.FillRoundRect(60, 34, 80, 50, 2, PixelPalette.Metal);
            c.FillRect(62, 46, 78, 49, PixelPalette.MetalDark);
            c.FillRect(64, 36, 76, 44, PixelPalette.MetalDark);
            c.FillRect(67, 36, 69, 39, PixelPalette.Metal); c.FillRect(71, 36, 73, 39, PixelPalette.Metal);
            c.Disc(66, 47, 1, PixelPalette.PinkMid); c.Disc(74, 47, 1, PixelPalette.GreenMid);
            // 컵 2
            c.FillRect(84, 34, 88, 38, PixelPalette.White); c.Set(89, 36, PixelPalette.White);
            c.FillRect(52, 34, 56, 38, PixelPalette.White);
            // 케이크 진열 돔
            c.FillRoundRect(14, 34, 34, 44, 4, PixelPalette.SkyLow);
            c.FillRect(18, 34, 30, 38, PixelPalette.PinkLight); c.HLine(18, 30, 38, PixelPalette.CreamLight);
            c.Set(24, 40, PixelPalette.PinkMid);
            c.Set(17, 42, PixelPalette.Highlight); c.Set(18, 43, PixelPalette.Highlight);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        // 펜던트 조명(16×40): 천장 줄 + 갓 + 전구. 위쪽이 천장에 붙는다(피벗 상단).
        public static PixelCanvas PendantLamp()
        {
            var c = new PixelCanvas(16, 40);
            c.VLine(8, 14, 39, PixelPalette.WoodDeep);
            for (int y = 6; y <= 13; y++) { int w = (y - 6) / 2 + 3; c.HLine(8 - w, 7 + w, y, PixelPalette.YellowMid); }
            c.HLine(2, 13, 6, PixelPalette.YellowDark);
            c.Disc(8, 4, 2, PixelPalette.Highlight);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        // 메뉴 칠판(40×32): 분필 글씨 줄 + 고양이 낙서.
        public static PixelCanvas MenuBoard()
        {
            var c = new PixelCanvas(40, 32);
            c.FillRect(1, 1, 38, 30, PixelPalette.WoodMid);
            c.FillRect(3, 3, 36, 28, PixelPalette.Chalk);
            c.HLine(6, 22, 24, PixelPalette.ChalkLine);
            for (int y = 19; y >= 7; y -= 4) { c.HLine(6, 18, y, PixelPalette.ChalkLine); c.HLine(26, 32, y, PixelPalette.ChalkLine); }
            // 고양이 얼굴 낙서
            c.Disc(30, 24, 2, PixelPalette.ChalkLine); c.Set(28, 27, PixelPalette.ChalkLine); c.Set(32, 27, PixelPalette.ChalkLine);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        // 화분 큰 것(바닥 장식, 32×48): 공간을 채우는 몬스테라.
        public static PixelCanvas BigPlant()
        {
            var c = new PixelCanvas(32, 48);
            c.GroundShadow(16, 4, 9);
            c.FillRoundRect(9, 4, 22, 14, 1, PixelPalette.CreamMid);
            c.HLine(9, 22, 14, PixelPalette.CreamLight); c.HLine(10, 21, 5, PixelPalette.CreamDark);
            c.Disc(16, 26, 8, PixelPalette.GreenMid);
            c.Disc(10, 32, 6, PixelPalette.GreenMid); c.Disc(22, 33, 6, PixelPalette.GreenMid);
            c.Disc(16, 38, 6, PixelPalette.GreenLight);
            c.Disc(11, 31, 3, PixelPalette.GreenLight); c.Disc(21, 32, 3, PixelPalette.GreenLight);
            c.VLine(16, 15, 22, PixelPalette.GreenDark);
            c.Set(14, 26, PixelPalette.GreenDark); c.Set(18, 28, PixelPalette.GreenDark);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }
    }
}
