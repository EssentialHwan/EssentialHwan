using UnityEngine;

namespace YeogiCafe.Art
{
    // 가구 18종 (32×32). 좌석6 + 시설6 + 장식6. 전부 공용 팔레트·아웃라인·접지그림자.
    public static class FurnitureDrawing
    {
        static PixelCanvas New() => new(32, 32);

        static PixelCanvas Finish(PixelCanvas c)
        {
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        // ── 좌석 6 ──
        public static PixelCanvas SeatNormal()
        {
            var c = New(); c.GroundShadow(16, 5, 10);
            c.FillRect(9, 6, 22, 9, PixelPalette.WoodMid);      // 좌면
            c.FillRect(9, 6, 22, 7, PixelPalette.WoodLight);
            c.VLine(10, 2, 6, PixelPalette.WoodDark); c.VLine(21, 2, 6, PixelPalette.WoodDark); // 등받이 다리
            c.VLine(10, 9, 12, PixelPalette.WoodDark); c.VLine(21, 9, 12, PixelPalette.WoodDark);
            return Finish(c);
        }

        public static PixelCanvas SeatWindow()
        {
            var c = New(); c.GroundShadow(16, 4, 12);
            // 창문(하늘) 배경
            c.FillRect(6, 14, 25, 27, PixelPalette.BlueLight);
            c.FillRect(6, 22, 25, 27, PixelPalette.GreenLight);  // 바깥 풀밭
            c.RectOutline(6, 14, 25, 27, PixelPalette.WoodDark); // 창틀
            c.VLine(16, 14, 27, PixelPalette.WoodDark); c.HLine(6, 25, 21, PixelPalette.WoodDark);
            // 창가 벤치
            c.FillRect(7, 8, 24, 11, PixelPalette.CreamMid);
            c.FillRect(7, 8, 24, 9, PixelPalette.CreamLight);
            return Finish(c);
        }

        public static PixelCanvas SeatCorner()
        {
            var c = New(); c.GroundShadow(16, 5, 11);
            c.FillRect(7, 7, 24, 16, PixelPalette.LilacMid);     // 아늑한 구석 소파
            c.FillRect(7, 7, 24, 9, PixelPalette.LilacLight);
            c.FillRect(7, 7, 9, 16, PixelPalette.LilacLight);    // 팔걸이
            c.FillRect(22, 7, 24, 16, PixelPalette.LilacLight);
            return Finish(c);
        }

        public static PixelCanvas SeatTwo()
        {
            var c = New(); c.GroundShadow(16, 5, 12);
            c.FillRect(4, 8, 14, 14, PixelPalette.GreenMid);     // 좌석 1
            c.FillRect(18, 8, 28, 14, PixelPalette.GreenMid);    // 좌석 2
            c.FillRect(4, 8, 14, 9, PixelPalette.GreenLight); c.FillRect(18, 8, 28, 9, PixelPalette.GreenLight);
            c.FillRect(13, 10, 19, 12, PixelPalette.WoodMid);    // 가운데 테이블
            return Finish(c);
        }

        public static PixelCanvas SeatSofa()
        {
            var c = New(); c.GroundShadow(16, 4, 13);
            c.FillRect(5, 6, 27, 16, PixelPalette.PinkMid);      // 소파 등
            c.FillRect(5, 12, 27, 16, PixelPalette.PinkLight);   // 방석
            c.FillRect(5, 6, 8, 16, PixelPalette.PinkLight);     // 팔걸이
            c.FillRect(24, 6, 27, 16, PixelPalette.PinkLight);
            return Finish(c);
        }

        public static PixelCanvas SeatBar()
        {
            var c = New(); c.GroundShadow(16, 4, 8);
            c.FillRect(8, 20, 23, 23, PixelPalette.WoodDark);    // 바 상판
            c.FillRect(8, 20, 23, 21, PixelPalette.WoodMid);
            c.VLine(12, 4, 20, PixelPalette.WoodMid);            // 스툴 다리
            c.Disc(12, 22, 3, PixelPalette.CreamMid);            // 스툴 방석
            return Finish(c);
        }

        // ── 시설 6 ──
        public static PixelCanvas FacTower()
        {
            var c = New(); c.GroundShadow(16, 3, 9);
            c.FillRect(13, 3, 18, 26, PixelPalette.WoodMid);     // 기둥
            c.FillRect(14, 3, 15, 26, PixelPalette.WoodLight);
            c.FillRect(8, 24, 23, 28, PixelPalette.CreamMid);    // 상단 발판
            c.FillRect(10, 12, 21, 15, PixelPalette.CreamMid);   // 중단 발판
            c.FillRect(6, 4, 12, 8, PixelPalette.GreenLight);    // 매달린 공
            return Finish(c);
        }

        public static PixelCanvas FacCushion()
        {
            var c = New(); c.GroundShadow(16, 5, 11);
            c.Disc(16, 11, 9, PixelPalette.PinkMid);
            c.Disc(16, 12, 8, PixelPalette.PinkLight);
            c.Disc(16, 12, 3, PixelPalette.PinkMid);             // 가운데 눌린 자국
            return Finish(c);
        }

        public static PixelCanvas FacToybox()
        {
            var c = New(); c.GroundShadow(16, 5, 10);
            c.FillRect(8, 6, 23, 15, PixelPalette.WoodMid);      // 바구니
            c.FillRect(8, 6, 23, 15, PixelPalette.WoodMid);
            for (int x = 9; x <= 22; x += 3) c.VLine(x, 6, 15, PixelPalette.WoodDark); // 엮은 결
            c.Disc(12, 17, 2, PixelPalette.PinkMid);             // 공들
            c.Disc(19, 18, 2, PixelPalette.BlueMid);
            c.Disc(16, 16, 2, PixelPalette.YellowMid);
            return Finish(c);
        }

        public static PixelCanvas FacScratcher()
        {
            var c = New(); c.GroundShadow(16, 4, 7);
            c.FillRect(12, 4, 19, 24, PixelPalette.CreamMid);    // 기둥
            for (int y = 5; y <= 23; y += 2) c.HLine(12, 19, y, PixelPalette.CreamLight); // 결
            c.FillRect(9, 3, 22, 6, PixelPalette.WoodMid);       // 상단
            return Finish(c);
        }

        public static PixelCanvas FacPlant()
        {
            var c = New(); c.GroundShadow(16, 4, 8);
            c.FillRect(11, 4, 20, 11, PixelPalette.WoodMid);     // 화분
            c.FillRect(11, 4, 20, 5, PixelPalette.WoodLight);
            c.Disc(16, 18, 7, PixelPalette.GreenMid);            // 잎 뭉치
            c.Disc(13, 20, 4, PixelPalette.GreenLight);
            c.Disc(20, 19, 4, PixelPalette.GreenLight);
            return Finish(c);
        }

        public static PixelCanvas FacWindow()
        {
            var c = New();
            c.FillRect(6, 6, 25, 25, PixelPalette.BlueLight);    // 하늘
            c.Disc(21, 20, 3, PixelPalette.YellowLight);         // 해
            c.FillRect(6, 6, 25, 10, PixelPalette.BlueMid);      // 상단 그라데
            c.RectOutline(6, 6, 25, 25, PixelPalette.WoodDark);  // 창틀
            c.VLine(16, 6, 25, PixelPalette.WoodDark); c.HLine(6, 25, 16, PixelPalette.WoodDark);
            return Finish(c);
        }

        // ── 장식 6 (분위기 기여만) ──
        public static PixelCanvas DecoClock()
        {
            var c = New();
            c.Disc(16, 16, 8, PixelPalette.WoodMid);
            c.Disc(16, 16, 6, PixelPalette.White);
            c.VLine(16, 16, 21, PixelPalette.Outline); c.HLine(16, 19, 16, PixelPalette.Outline);
            return Finish(c);
        }

        public static PixelCanvas DecoFrame()
        {
            var c = New();
            c.FillRect(8, 8, 23, 23, PixelPalette.WoodMid);
            c.FillRect(10, 10, 21, 21, PixelPalette.GreenLight);
            c.Disc(16, 15, 3, PixelPalette.GreenMid);            // 풍경 언덕
            c.FillRect(10, 18, 21, 21, PixelPalette.GreenMid);
            return Finish(c);
        }

        public static PixelCanvas DecoRug()
        {
            var c = New(); c.GroundShadow(16, 6, 13);
            c.FillRect(5, 8, 27, 18, PixelPalette.WoodLight);
            c.RectOutline(5, 8, 27, 18, PixelPalette.PinkMid);
            c.RectOutline(8, 11, 24, 15, PixelPalette.PinkMid);
            return Finish(c);
        }

        public static PixelCanvas DecoLamp()
        {
            var c = New(); c.GroundShadow(16, 4, 5);
            c.VLine(16, 4, 18, PixelPalette.WoodDark);           // 기둥
            for (int y = 18; y <= 24; y++) { int w = (24 - y) + 3; c.HLine(16 - w, 16 + w, y, PixelPalette.YellowLight); } // 갓
            c.Disc(16, 20, 2, PixelPalette.Highlight);           // 빛
            return Finish(c);
        }

        public static PixelCanvas DecoCurtain()
        {
            var c = New();
            c.FillRect(6, 4, 25, 27, PixelPalette.BlueLight);
            for (int x = 8; x <= 24; x += 4) c.VLine(x, 4, 27, PixelPalette.BlueMid); // 주름
            c.HLine(5, 26, 4, PixelPalette.WoodDark);            // 커튼봉
            return Finish(c);
        }

        public static PixelCanvas DecoVase()
        {
            var c = New(); c.GroundShadow(16, 4, 6);
            c.FillRect(12, 4, 19, 14, PixelPalette.LilacMid);    // 화병
            c.FillRect(13, 4, 15, 14, PixelPalette.LilacLight);
            c.Disc(12, 18, 2, PixelPalette.PinkMid);             // 꽃
            c.Disc(20, 18, 2, PixelPalette.YellowMid);
            c.Disc(16, 20, 2, PixelPalette.PinkLight);
            c.VLine(16, 14, 18, PixelPalette.GreenMid);          // 줄기
            return Finish(c);
        }
    }
}
