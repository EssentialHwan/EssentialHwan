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
        // 고양이 애니메이션 상태(프레임 변형 파라미터로 통일된 실루엣 유지)
        public enum CatPose { Idle, Walk, Sleep, Eat }

        public static PixelCanvas Cat(string catId) => Cat(catId, CatPose.Idle, 0);

        // pose/frame으로 같은 고양이의 상태별 프레임을 생성. 실루엣·팔레트·아웃라인은 전부 동일.
        public static PixelCanvas Cat(string catId, CatPose pose, int frame)
        {
            var c = new PixelCanvas(32, 32);
            var fur = PixelPalette.Fur(catId);

            if (pose == CatPose.Sleep) { DrawCatSleeping(c, catId, fur); return c; }

            // 프레임별 미세 변형: 몸 들썩임(bob), 꼬리 위치, 눈 깜빡임
            int bob = (pose == CatPose.Walk && frame == 1) ? 1 : 0;   // 걷기 2프레임 상하
            int idleBob = (pose == CatPose.Idle && frame == 1) ? 1 : 0;
            int yOff = bob + idleBob;

            // 접지 그림자(고정)
            c.GroundShadow(16, 4, 10);
            DrawCatBody(c, catId, fur, yOff, pose, frame);
            c.AutoOutline(PixelPalette.Outline);
            return c;
        }

        // 잠자는 포즈(웅크림) — 별도 실루엣이지만 같은 팔레트/아웃라인
        static void DrawCatSleeping(PixelCanvas c, string catId, PixelPalette.FurSet fur)
        {
            c.GroundShadow(16, 5, 12);
            // 웅크린 타원 몸통
            c.Disc(16, 11, 9, fur.mid);
            c.Disc(16, 10, 8, fur.light);
            c.Disc(16, 13, 7, fur.shade);          // 아래 그림자
            // 말린 꼬리
            c.HLine(8, 24, 7, fur.mid); c.Set(24, 8, fur.mid); c.Set(23, 9, fur.mid);
            // 머리(옆으로 기댄)
            c.Disc(10, 13, 5, fur.light);
            DrawEar(c, 7, 17, fur); DrawEar(c, 12, 17, fur);
            // 감은 눈(ㅡ)
            c.HLine(8, 9, 13, PixelPalette.Outline);
            c.Set(11, 12, PixelPalette.CatNose);   // 코
            // Zzz
            c.Set(22, 20, PixelPalette.OutlineSoft); c.Set(24, 22, PixelPalette.OutlineSoft); c.Set(26, 24, PixelPalette.OutlineSoft);
            ApplyMarkings(c, catId, fur);
            c.AutoOutline(PixelPalette.Outline);
        }

        // 공용 몸통 드로잉(idle/walk/eat 공유)
        static void DrawCatBody(PixelCanvas c, string catId, PixelPalette.FurSet fur, int yOff, CatPose pose, int frame)
        {
            // (원래 Cat 본문을 yOff 적용해 재사용)
            DrawCatCore(c, catId, fur, yOff, pose, frame);
        }

        static void DrawCatCore(PixelCanvas c, string catId, PixelPalette.FurSet fur, int yOff, CatPose pose, int frame)
        {
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

            // 눈: eat는 반쯤 감음, idle frame1은 깜빡임
            bool blink = (pose == CatPose.Idle && frame == 1);
            bool halfEye = (pose == CatPose.Eat);
            if (blink)
            {
                c.HLine(13, 14, 23, PixelPalette.Outline); c.HLine(18, 19, 23, PixelPalette.Outline);
            }
            else
            {
                c.Set(13, 23, PixelPalette.CatEye); c.Set(13, 24, halfEye ? PixelPalette.CatEye : PixelPalette.Highlight);
                c.Set(18, 23, PixelPalette.CatEye); c.Set(18, 24, halfEye ? PixelPalette.CatEye : PixelPalette.Highlight);
                if (halfEye) { c.Set(13, 24, PixelPalette.Outline); c.Set(18, 24, PixelPalette.Outline); }
            }

            // 코(연분홍) + 입
            c.Set(15, 21, PixelPalette.CatNose); c.Set(16, 21, PixelPalette.CatNose);
            c.Set(15, 20, PixelPalette.OutlineSoft); c.Set(16, 20, PixelPalette.OutlineSoft);
            // eat: 음식 그릇
            if (pose == CatPose.Eat)
            {
                c.FillRect(12, 2, 19, 4, PixelPalette.CreamMid);
                c.Disc(16, 4, 2, PixelPalette.PinkMid);   // 음식
            }

            // 꼬리(walk frame1은 위로 흔들림)
            int tailTop = (pose == CatPose.Walk && frame == 1) ? 14 : 11;
            c.VLine(24, 6, tailTop, fur.mid); c.HLine(24, 27, tailTop, fur.mid); c.VLine(27, tailTop - 2, tailTop + 1, fur.mid);

            // 앞발 2개(bob 적용)
            c.FillRect(12, 4 + yOff, 14, 6 + yOff, fur.light);
            c.FillRect(17, 4 + yOff, 19, 6 + yOff, fur.light);

            // 고유 무늬
            ApplyMarkings(c, catId, fur);
            // ※ 아웃라인은 호출자(Cat)가 마지막에 적용
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
        // 고양이 스프라이트시트 (프레임 가로 배치, 각 32×32).
        // 순서: idle0, idle1, walk0, walk1, eat0, eat1, sleep  (총 7프레임)
        // ─────────────────────────────────────────────
        public const int CatFrameCount = 7;

        public static PixelCanvas CatSheet(string catId)
        {
            var frames = new[]
            {
                Cat(catId, CatPose.Idle, 0), Cat(catId, CatPose.Idle, 1),
                Cat(catId, CatPose.Walk, 0), Cat(catId, CatPose.Walk, 1),
                Cat(catId, CatPose.Eat, 0),  Cat(catId, CatPose.Eat, 1),
                Cat(catId, CatPose.Sleep, 0),
            };
            var sheet = new PixelCanvas(32 * frames.Length, 32);
            for (int f = 0; f < frames.Length; f++)
                Blit(sheet, frames[f], f * 32, 0);
            return sheet;
        }

        static void Blit(PixelCanvas dst, PixelCanvas src, int ox, int oy)
        {
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                {
                    var p = src.Get(x, y);
                    if (p.a != 0) dst.Set(ox + x, oy + y, p);
                }
        }

        // ─────────────────────────────────────────────
        // 배경 타일 (32×32, 심리스) — 카페 바닥/벽
        // ─────────────────────────────────────────────
        public static PixelCanvas FloorTile()
        {
            var c = new PixelCanvas(32, 32);
            c.FillRect(0, 0, 31, 31, PixelPalette.WoodLight);
            // 나무 판자 결(가로 2줄) + 이음새
            c.HLine(0, 31, 0, PixelPalette.WoodMid);
            c.HLine(0, 31, 16, PixelPalette.WoodMid);
            for (int x = 2; x < 32; x += 7) { c.Set(x, 5, PixelPalette.WoodDark); c.Set(x + 3, 21, PixelPalette.WoodDark); }
            // 세로 이음새(엇갈리게)
            c.VLine(10, 1, 15, PixelPalette.WoodMid); c.VLine(22, 17, 31, PixelPalette.WoodMid);
            return c;
        }

        public static PixelCanvas WallTile()
        {
            var c = new PixelCanvas(32, 32);
            c.FillRect(0, 0, 31, 31, PixelPalette.CreamLight);
            // 은은한 세로 줄무늬 벽지
            for (int x = 0; x < 32; x += 6) c.VLine(x, 0, 31, PixelPalette.CreamMid);
            // 하단 걸레받이(나무)
            c.FillRect(0, 0, 31, 3, PixelPalette.WoodMid);
            c.HLine(0, 31, 4, PixelPalette.WoodDark);
            return c;
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
