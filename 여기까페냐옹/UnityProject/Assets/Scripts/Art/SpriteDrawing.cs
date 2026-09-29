using UnityEngine;

namespace YeogiCafe.Art
{
    // 스프라이트별 픽셀 배치 로직(순수). 모든 함수가 PixelPalette + PixelCanvas만 사용 →
    // 실루엣·아웃라인·셰이딩·접지그림자 규칙이 전부 통일되어 "한 게임의 리소스"가 된다.
    // 규격: 고양이 32×32, 가구 32×32, 아이콘 16×16.
    public static class SpriteDrawing
    {
        // ─────────────────────────────────────────────
        // 고양이 (32×32, 정면 앉은 포즈). catId로 팔레트 스왑 + 고유 무늬.
        // ─────────────────────────────────────────────
        public static PixelCanvas Cat(string catId)
        {
            var c = new PixelCanvas(32, 32);
            var fur = PixelPalette.Fur(catId);

            // 접지 그림자
            c.GroundShadow(16, 4, 10);

            // 몸통(둥근 사다리꼴) — 아래가 넓은 안정적 실루엣
            for (int y = 5; y <= 16; y++)
            {
                int half = 6 + (16 - y) / 2;                 // 아래로 갈수록 넓게
                c.HLine(16 - half, 15 + half, y, fur.mid);
            }
            // 몸통 셰이딩(아래·측면 어둡게)
            for (int y = 5; y <= 8; y++) c.HLine(16 - (6 + (16 - y) / 2), 15 + (6 + (16 - y) / 2), y, fur.shade);
            // 배 하이라이트(가운데 밝게)
            c.FillRect(13, 9, 18, 15, fur.light);

            // 머리(원형)
            c.Disc(16, 22, 8, fur.mid);
            c.Disc(16, 23, 7, fur.light);                    // 얼굴 밝은 면
            // 볼 셰이딩
            c.HLine(9, 12, 20, fur.shade); c.HLine(19, 22, 20, fur.shade);

            // 귀(삼각) — 좌우
            DrawEar(c, 10, 27, fur);
            DrawEar(c, 21, 27, fur);

            // 눈 2개(통일 초록) + 하이라이트
            c.Set(13, 23, PixelPalette.CatEye); c.Set(13, 24, PixelPalette.CatEye);
            c.Set(18, 23, PixelPalette.CatEye); c.Set(18, 24, PixelPalette.CatEye);
            c.Set(13, 24, PixelPalette.Highlight); c.Set(18, 24, PixelPalette.Highlight);

            // 코(연분홍) + 입
            c.Set(15, 21, PixelPalette.CatNose); c.Set(16, 21, PixelPalette.CatNose);
            c.Set(15, 20, PixelPalette.OutlineSoft); c.Set(16, 20, PixelPalette.OutlineSoft);

            // 꼬리(오른쪽으로 말린)
            c.VLine(24, 6, 11, fur.mid); c.HLine(24, 27, 11, fur.mid); c.VLine(27, 9, 12, fur.mid);

            // 앞발 2개
            c.FillRect(12, 4, 14, 6, fur.light);
            c.FillRect(17, 4, 19, 6, fur.light);

            // 고유 무늬
            ApplyMarkings(c, catId, fur);

            // 통일 아웃라인
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        static void DrawEar(PixelCanvas c, int baseX, int baseY, PixelPalette.FurSet fur)
        {
            for (int i = 0; i < 3; i++) c.HLine(baseX - i, baseX + 1 + i, baseY - 2 + i, fur.mid);
            c.Set(baseX, baseY - 1, PixelPalette.CatNose);   // 귀 안쪽 분홍
            c.Set(baseX + 1, baseY - 1, PixelPalette.CatNose);
        }

        // 고양이별 고유 무늬(태비 줄무늬/얼룩/포인트) — 팔레트 스왑만으로 안 되는 개성 부여
        static void ApplyMarkings(PixelCanvas c, string catId, PixelPalette.FurSet fur)
        {
            switch (catId)
            {
                case "cat_cheese":   // 오렌지 태비 줄무늬
                case "cat_mackerel": // 블루그레이 태비
                    for (int y = 24; y <= 28; y += 2) { c.Set(16, y, fur.shade); c.Set(15, y, fur.shade); }
                    c.HLine(11, 13, 8, fur.shade); c.HLine(18, 20, 8, fur.shade);
                    c.HLine(11, 13, 12, fur.shade); c.HLine(18, 20, 12, fur.shade);
                    break;
                case "cat_calico":   // 삼색 얼룩(오렌지+검정 패치)
                    c.Disc(11, 25, 3, new Color32(224, 168, 84, 255));
                    c.Disc(20, 10, 3, new Color32(90, 82, 88, 255));
                    c.Disc(19, 26, 2, new Color32(90, 82, 88, 255));
                    break;
                case "cat_cow":      // 젖소 검정 얼룩
                    c.Disc(11, 11, 3, new Color32(88, 84, 90, 255));
                    c.Disc(20, 24, 3, new Color32(88, 84, 90, 255));
                    break;
                case "cat_tuxedo":   // 턱시도(가슴·얼굴 흰 V)
                    c.FillRect(14, 6, 17, 12, PixelPalette.White);
                    c.Set(16, 22, PixelPalette.White); c.Set(15, 21, PixelPalette.White);
                    break;
                case "cat_siamese":  // 샴 포인트(귀·얼굴 진하게)
                    c.HLine(9, 12, 27, fur.shade); c.HLine(20, 23, 27, fur.shade);
                    c.Disc(16, 21, 3, fur.shade);
                    break;
                // cat_black, cat_gray: 단색 — 셰이딩만으로 충분
            }
        }

        // ─────────────────────────────────────────────
        // 아이콘 (16×16) — UI 통일(별/체크/물음표/골드/시계/돋보기/자물쇠/하트)
        // ─────────────────────────────────────────────
        public static PixelCanvas IconStar()
        {
            var c = new PixelCanvas(16, 16);
            // 5각 별(대칭 근사)
            c.HLine(7, 8, 13, PixelPalette.YellowMid);
            c.HLine(6, 9, 12, PixelPalette.YellowLight);
            c.HLine(5, 10, 11, PixelPalette.YellowLight);
            c.HLine(2, 13, 9, PixelPalette.YellowLight);
            c.HLine(4, 11, 8, PixelPalette.YellowLight);
            c.HLine(4, 6, 7, PixelPalette.YellowMid); c.HLine(9, 11, 7, PixelPalette.YellowMid);
            c.HLine(3, 5, 5, PixelPalette.YellowMid); c.HLine(10, 12, 5, PixelPalette.YellowMid);
            c.HLine(2, 4, 3, PixelPalette.YellowMid); c.HLine(11, 13, 3, PixelPalette.YellowMid);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas IconCheck()
        {
            var c = new PixelCanvas(16, 16);
            for (int i = 0; i < 4; i++) c.Set(4 + i, 7 - i, PixelPalette.GreenMid);
            for (int i = 0; i < 7; i++) c.Set(7 + i, 4 + i, PixelPalette.GreenMid);
            // 두께
            for (int i = 0; i < 4; i++) c.Set(4 + i, 8 - i, PixelPalette.GreenLight);
            for (int i = 0; i < 7; i++) c.Set(7 + i, 5 + i, PixelPalette.GreenLight);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas IconQuestion()
        {
            var c = new PixelCanvas(16, 16);
            c.HLine(5, 9, 12, PixelPalette.LilacMid);
            c.Set(10, 11, PixelPalette.LilacMid); c.Set(10, 10, PixelPalette.LilacMid);
            c.Set(9, 9, PixelPalette.LilacMid); c.Set(8, 8, PixelPalette.LilacMid);
            c.Set(8, 7, PixelPalette.LilacMid); c.Set(8, 6, PixelPalette.LilacMid);
            c.Set(8, 3, PixelPalette.LilacMid); // 점
            c.Set(4, 11, PixelPalette.LilacMid); c.Set(10, 12, PixelPalette.LilacMid);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas IconGold()
        {
            var c = new PixelCanvas(16, 16);
            c.Disc(8, 8, 6, PixelPalette.YellowMid);
            c.Disc(8, 8, 5, PixelPalette.YellowLight);
            c.Disc(8, 9, 3, PixelPalette.YellowMid);
            c.Set(8, 11, PixelPalette.Highlight); c.Set(7, 11, PixelPalette.Highlight);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas IconClock()
        {
            var c = new PixelCanvas(16, 16);
            c.Disc(8, 8, 6, PixelPalette.CreamLight);
            c.Disc(8, 8, 5, PixelPalette.White);
            c.VLine(8, 8, 11, PixelPalette.Outline);   // 분침
            c.HLine(8, 10, 8, PixelPalette.Outline);   // 시침
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas IconMagnifier()
        {
            var c = new PixelCanvas(16, 16);
            c.Disc(6, 10, 4, PixelPalette.BlueLight);
            c.Disc(6, 10, 3, PixelPalette.White);
            for (int i = 0; i < 4; i++) c.Set(9 + i, 7 - i, PixelPalette.WoodDark); // 손잡이
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas IconLock()
        {
            var c = new PixelCanvas(16, 16);
            c.FillRect(4, 3, 11, 9, PixelPalette.WoodMid);      // 몸통
            c.FillRect(5, 4, 10, 8, PixelPalette.WoodLight);
            c.RectOutline(6, 9, 9, 13, PixelPalette.WoodDark);  // 고리
            c.Set(7, 6, PixelPalette.Outline); c.Set(8, 6, PixelPalette.Outline); // 열쇠구멍
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        public static PixelCanvas IconHeart()
        {
            var c = new PixelCanvas(16, 16);
            c.Disc(6, 10, 2, PixelPalette.PinkMid); c.Disc(9, 10, 2, PixelPalette.PinkMid);
            for (int y = 9; y >= 4; y--) { int w = y - 2; c.HLine(8 - w / 2, 7 + w / 2, y, PixelPalette.PinkLight); }
            c.Disc(6, 10, 2, PixelPalette.PinkLight); c.Disc(9, 10, 2, PixelPalette.PinkLight);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }
    }
}
