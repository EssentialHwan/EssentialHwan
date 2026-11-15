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
        // 규칙: 바닥 접지선 y=4(스프라이트 피벗 y=4/32), 앉는 면 높이는 SeatSitHeightPx와 일치.
        // 고양이(32px)가 올라앉는 크기로 그려 '고양이용 가구'로 읽히게 한다.

        // 좌석별 앉는 면 높이(px, 접지선 기준). 고양이 표시 오프셋에 사용.
        public static int SeatSitHeightPx(string furnitureId) => furnitureId switch
        {
            "furn_seat_bar" => 13,
            "furn_seat_window" => 6,
            _ => 7,
        };

        public static PixelCanvas SeatNormal()
        {
            // 원목 카페 의자(등받이 + 네 다리)
            var c = New(); c.GroundShadow(16, 4, 9);
            var L = PixelPalette.WoodLight; var M = PixelPalette.WoodMid; var D = PixelPalette.WoodDark;
            // 뒷다리(살짝 안쪽) → 앞다리
            c.VLine(11, 5, 10, D); c.VLine(20, 5, 10, D);
            c.FillRect(9, 4, 10, 10, M); c.FillRect(21, 4, 22, 10, M);
            // 좌판
            c.FillRect(8, 10, 23, 12, M); c.HLine(8, 23, 12, L); c.HLine(8, 23, 10, D);
            // 등받이 기둥 + 가로대 2줄
            c.FillRect(9, 13, 10, 25, M); c.FillRect(21, 13, 22, 25, M);
            c.FillRect(9, 22, 22, 25, M); c.HLine(9, 22, 25, L);
            c.FillRect(11, 17, 20, 18, D);
            return Finish(c);
        }

        public static PixelCanvas SeatWindow()
        {
            // 창가 벤치: 낮은 원목 수납 벤치 + 하늘색 쿠션 + 작은 베개
            var c = New(); c.GroundShadow(16, 4, 12);
            var M = PixelPalette.WoodMid; var D = PixelPalette.WoodDark; var L = PixelPalette.WoodLight;
            c.FillRect(4, 4, 27, 9, M); c.HLine(4, 27, 9, L);
            c.RectOutline(6, 5, 15, 8, D); c.RectOutline(16, 5, 25, 8, D);        // 서랍 패널
            c.FillRoundRect(4, 10, 27, 12, 1, PixelPalette.BlueLight);
            c.HLine(5, 26, 10, PixelPalette.BlueMid);
            c.FillRoundRect(4, 12, 9, 16, 1, PixelPalette.CreamLight);            // 베개
            c.HLine(5, 8, 12, PixelPalette.CreamDark);
            return Finish(c);
        }

        public static PixelCanvas SeatCorner()
        {
            // 구석 1인 암체어(라일락) — 높은 등받이로 '숨는 자리' 느낌
            var c = New(); c.GroundShadow(16, 4, 11);
            var L = PixelPalette.LilacLight; var M = PixelPalette.LilacMid; var D = PixelPalette.LilacDark;
            c.FillRoundRect(7, 11, 24, 27, 3, M);            // 등받이
            c.FillRoundRect(9, 13, 22, 26, 2, L);
            c.VLine(16, 15, 24, M);                          // 터프팅 주름
            c.FillRect(8, 4, 9, 5, PixelPalette.WoodDark); c.FillRect(22, 4, 23, 5, PixelPalette.WoodDark); // 다리
            c.FillRect(6, 6, 25, 10, D);                     // 하부
            c.FillRect(8, 9, 23, 12, L); c.HLine(8, 23, 9, M); // 방석
            c.FillRoundRect(4, 6, 8, 16, 2, M); c.VLine(5, 8, 15, L);     // 팔걸이
            c.FillRoundRect(23, 6, 27, 16, 2, M); c.VLine(24, 8, 15, L);
            return Finish(c);
        }

        public static PixelCanvas SeatTwo()
        {
            // 2인 러브시트(세이지 그린) — 방석 2칸
            var c = New(); c.GroundShadow(16, 4, 13);
            var L = PixelPalette.GreenLight; var M = PixelPalette.GreenMid; var D = PixelPalette.GreenDark;
            c.FillRoundRect(3, 11, 28, 23, 2, M);            // 등받이
            c.FillRect(5, 13, 15, 21, L); c.FillRect(17, 13, 26, 21, L);
            c.FillRect(5, 4, 6, 5, PixelPalette.WoodDark); c.FillRect(25, 4, 26, 5, PixelPalette.WoodDark);
            c.FillRect(3, 6, 28, 9, D);
            c.FillRect(5, 9, 15, 12, L); c.FillRect(17, 9, 26, 12, L); c.VLine(16, 9, 12, D); // 방석 2
            c.FillRoundRect(1, 6, 5, 15, 2, M); c.VLine(2, 8, 14, L);
            c.FillRoundRect(26, 6, 30, 15, 2, M); c.VLine(27, 8, 14, L);
            return Finish(c);
        }

        public static PixelCanvas SeatSofa()
        {
            // 큰 3인 소파(로즈 핑크) — 낮고 깊은 등받이 + 단추 터프팅
            var c = New(); c.GroundShadow(16, 4, 14);
            var L = PixelPalette.PinkLight; var M = PixelPalette.PinkMid; var D = PixelPalette.PinkDark;
            c.FillRoundRect(2, 11, 29, 21, 2, M);
            c.FillRect(4, 13, 27, 20, L);
            for (int x = 7; x <= 25; x += 6) { c.Set(x, 17, M); c.Set(x + 3, 15, M); } // 터프팅 단추
            c.FillRect(4, 4, 5, 5, PixelPalette.WoodDark); c.FillRect(26, 4, 27, 5, PixelPalette.WoodDark);
            c.FillRect(2, 6, 29, 9, D);
            c.FillRect(5, 9, 26, 12, L); c.VLine(12, 9, 12, M); c.VLine(19, 9, 12, M); // 방석 3
            c.FillRoundRect(0, 6, 5, 16, 2, M); c.VLine(1, 8, 15, L);
            c.FillRoundRect(26, 6, 31, 16, 2, M); c.VLine(27, 8, 15, L);
            return Finish(c);
        }

        public static PixelCanvas SeatBar()
        {
            // 바 스툴(높은 1인석) — 금속 기둥 + 발받침 링 + 둥근 방석
            var c = New(); c.GroundShadow(16, 4, 7);
            c.FillRoundRect(11, 4, 20, 5, 1, PixelPalette.MetalDark);    // 받침
            c.FillRect(15, 5, 16, 14, PixelPalette.Metal); c.VLine(15, 5, 14, PixelPalette.MetalDark);
            c.HLine(12, 19, 9, PixelPalette.MetalDark);                    // 발받침 링
            c.FillRoundRect(9, 13, 22, 17, 2, PixelPalette.YellowMid);    // 방석
            c.HLine(10, 21, 17, PixelPalette.YellowLight); c.HLine(10, 21, 13, PixelPalette.YellowDark);
            return Finish(c);
        }

        // ── 시설 6 ──
        public static PixelCanvas FacTower()
        {
            // 캣타워(32×48): 고양이보다 높아야 '오르는 곳'으로 읽힌다. 기둥 2개 + 발판 3 + 꼭대기 하우스.
            var c = new PixelCanvas(32, 48); c.GroundShadow(16, 4, 11);
            var L = PixelPalette.CreamLight; var M = PixelPalette.CreamMid; var D = PixelPalette.CreamDark;
            c.FillRoundRect(5, 4, 26, 7, 1, PixelPalette.WoodMid); c.HLine(5, 26, 7, PixelPalette.WoodLight); // 받침
            // 사이잘 기둥(줄무늬)
            for (int y = 8; y <= 38; y++) { var col = (y % 3 == 0) ? D : M; c.HLine(9, 11, y, col); c.HLine(20, 22, Mathf.Min(y, 30), col); }
            c.FillRoundRect(16, 18, 29, 21, 1, PixelPalette.WoodMid); c.HLine(16, 29, 21, PixelPalette.WoodLight); // 중단 발판
            c.FillRoundRect(3, 28, 16, 31, 1, PixelPalette.WoodMid); c.HLine(3, 16, 31, PixelPalette.WoodLight);   // 좌측 발판
            // 꼭대기 하우스(구멍)
            c.FillRoundRect(4, 38, 27, 46, 2, PixelPalette.PinkMid);
            c.FillRect(6, 40, 25, 45, PixelPalette.PinkLight);
            c.Disc(16, 41, 3, PixelPalette.PinkDark);
            // 매달린 털공
            c.VLine(25, 14, 17, PixelPalette.Outline); c.Disc(25, 12, 2, PixelPalette.YellowMid);
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

        // 스크래처(32×46): 고양이가 쭉 늘어나 긁을 만큼 높은 사이잘 기둥 + 넓은 받침 + 꼭대기 발판 + 털공
        public static PixelCanvas FacScratcher()
        {
            var c = new PixelCanvas(32, 46); c.GroundShadow(16, 4, 12);
            c.FillRoundRect(3, 4, 28, 8, 2, PixelPalette.WoodMid); c.HLine(4, 27, 8, PixelPalette.WoodLight);   // 받침
            c.HLine(4, 27, 4, PixelPalette.WoodDark);
            // 사이잘 줄(감긴 결 + 긁힌 보풀)
            for (int y = 9; y <= 38; y++)
            {
                var col = (y % 2 == 0) ? PixelPalette.CreamMid : PixelPalette.CreamLight;
                c.HLine(11, 20, y, col);
                c.Set(11, y, PixelPalette.CreamDark); c.Set(20, y, PixelPalette.CreamDark);
            }
            foreach (var (x, y) in new[] { (12, 15), (18, 18), (13, 22), (19, 26), (14, 29) }) { c.Set(x, y, PixelPalette.CreamDark); c.Set(x + 1, y + 1, PixelPalette.CreamDark); }
            c.FillRoundRect(6, 38, 25, 42, 2, PixelPalette.PinkMid); c.HLine(7, 24, 42, PixelPalette.PinkLight);  // 꼭대기 발판(카펫)
            c.VLine(23, 33, 37, PixelPalette.Outline); c.Disc(23, 31, 2, PixelPalette.YellowMid); c.Set(22, 32, PixelPalette.YellowLight);   // 털공
            return Finish(c);
        }

        // 과일 박스(40×30): 뚜껑이 열린 골판지 상자 + 과일 라벨. 고양이는 누구나 상자를 좋아한다.
        public static PixelCanvas FacFruitBox()
        {
            var c = new PixelCanvas(40, 30); c.GroundShadow(20, 4, 15);
            var K1 = new Color32(222, 176, 120, 255); var K2 = new Color32(198, 150, 98, 255); var K3 = new Color32(166, 120, 76, 255);
            c.FillRect(6, 4, 33, 18, K1);                                     // 앞면
            c.FillRect(6, 4, 7, 18, K2); c.HLine(6, 33, 4, K3);
            c.FillRect(8, 18, 31, 20, K3);                                    // 안쪽(어두운 입구)
            // 열린 뚜껑(양옆으로 젖혀짐)
            for (int i = 0; i < 6; i++) { c.HLine(1 + i / 2, 6, 19 + i, K2); c.HLine(33, 38 - i / 2, 19 + i, K2); }
            c.HLine(2, 6, 24, K1); c.HLine(33, 37, 24, K1);
            // 라벨: 빨간 딸기·주황 귤·초록 잎
            c.FillRoundRect(11, 8, 28, 15, 2, new Color32(252, 244, 226, 255));
            c.Disc(15, 11, 2, new Color32(222, 72, 84, 255)); c.Set(15, 13, PixelPalette.GreenMid); c.Set(14, 11, new Color32(255, 220, 120, 255));
            c.Disc(20, 11, 2, new Color32(244, 150, 52, 255)); c.Set(20, 13, PixelPalette.GreenMid);
            c.Disc(25, 11, 2, new Color32(170, 210, 90, 255)); c.Set(25, 13, PixelPalette.GreenDark);
            c.HLine(12, 27, 9, new Color32(210, 190, 160, 255));
            // 테이프·접힌 선
            c.VLine(20, 4, 7, new Color32(236, 214, 160, 255)); c.VLine(20, 16, 17, new Color32(236, 214, 160, 255));
            return Finish(c);
        }

        // 온열 방석(32×20): 주황 체크무늬 방석 + 전선 + 아지랑이(따뜻한 걸 좋아하는 고양이가 모인다)
        public static PixelCanvas FacHeatPad()
        {
            var c = new PixelCanvas(32, 22); c.GroundShadow(16, 4, 13);
            var A = new Color32(246, 168, 108, 255); var B = new Color32(232, 132, 86, 255); var Cc = new Color32(252, 204, 150, 255);
            c.FillRoundRect(3, 4, 28, 10, 3, A);
            for (int x = 4; x <= 27; x++) for (int y = 5; y <= 9; y++) if (((x / 3) + (y / 2)) % 2 == 0) c.Set(x, y, B);
            c.HLine(5, 26, 10, Cc);
            c.HLine(28, 31, 6, PixelPalette.Outline); c.Set(31, 5, PixelPalette.MetalDark);       // 전선·플러그
            // 아지랑이
            foreach (var x in new[] { 9, 16, 23 })
                for (int k = 0; k < 7; k++) c.Blend(x + ((k / 2) % 2 == 0 ? 0 : 1), 12 + k, new Color32(255, 170, 120, 120));
            return Finish(c);
        }

        // 자동 낚싯대 장난감(받침만 — 낚싯대·깃털은 런타임에 흔들어 그린다)
        public static PixelCanvas FacWandToy()
        {
            var c = new PixelCanvas(24, 16); c.GroundShadow(12, 4, 8);
            c.FillRoundRect(5, 4, 18, 11, 2, PixelPalette.BlueMid); c.HLine(6, 17, 11, PixelPalette.BlueLight);
            c.Disc(12, 8, 2, PixelPalette.YellowMid); c.Set(12, 8, PixelPalette.White);          // 모터 버튼
            c.FillRect(11, 11, 13, 13, PixelPalette.MetalDark);                                   // 낚싯대 꽂는 곳
            return Finish(c);
        }

        // ── 캣휠(60×62) ── 받침·다리만(정적). 바퀴는 WheelSpin(프레임)으로 런타임에 돌린다.
        public const int WheelW = 60, WheelH = 62, WheelCX = 30, WheelCY = 34, WheelR = 26;
        public static PixelCanvas FacWheel()
        {
            var c = new PixelCanvas(WheelW, WheelH); c.GroundShadow(30, 4, 24);
            var W1 = PixelPalette.WoodLight; var W2 = PixelPalette.WoodMid; var W3 = PixelPalette.WoodDark;
            c.FillRoundRect(6, 4, 53, 8, 2, W2); c.HLine(7, 52, 8, W1); c.HLine(6, 53, 4, W3);       // 받침 판
            // A자 다리(양쪽) → 축
            for (int i = 0; i <= 26; i++)
            {
                float u = i / 26f;
                int lx = Mathf.RoundToInt(Mathf.Lerp(10, WheelCX - 2, u)), rx = Mathf.RoundToInt(Mathf.Lerp(49, WheelCX + 2, u));
                int y = Mathf.RoundToInt(Mathf.Lerp(8, WheelCY, u));
                c.FillRect(lx - 1, y, lx + 1, y, W2); c.Set(lx - 1, y, W1);
                c.FillRect(rx - 1, y, rx + 1, y, W2); c.Set(rx + 1, y, W3);
            }
            Finish(c);
            // 바퀴도 함께 그려 둔다(상점 아이콘·배치 미리보기용). 게임에선 WheelView가 같은 자리에 돌아가는 바퀴를 덮는다.
            var w = WheelSpin(0);
            for (int y = 0; y < WheelH; y++) for (int x = 0; x < WheelW; x++) { var p = w.Get(x, y); if (p.a > 0) c.Blend(x, y, p); }
            return c;
        }

        // 바퀴 한 장(돌아가는 느낌: 바퀴살 8개 → 45° 주기, 프레임 4장으로 11.25°씩)
        public static PixelCanvas WheelSpin(int frame)
        {
            var c = new PixelCanvas(WheelW, WheelH);
            var rimO = new Color32(118, 182, 172, 255); var rim = new Color32(156, 214, 202, 255); var rimL = new Color32(204, 238, 228, 255);
            var track = new Color32(250, 240, 222, 255); var back = new Color32(246, 236, 220, 120); var spoke = new Color32(130, 192, 182, 255);
            int cx = WheelCX, cy = WheelCY, R = WheelR;
            for (int y = cy - R - 1; y <= cy + R + 1; y++)
                for (int x = cx - R - 1; x <= cx + R + 1; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= R - 4) c.Set(x, y, back);                                  // 뒷판(반투명)
                    else if (d <= R - 3) c.Set(x, y, track);                            // 달리는 바닥면
                    else if (d <= R - 1) c.Set(x, y, y > cy ? rimL : rim);              // 테두리(위쪽 밝게)
                    else if (d <= R) c.Set(x, y, rimO);
                }
            float baseAng = -frame * 11.25f * Mathf.Deg2Rad;                            // 시계 방향
            for (int k = 0; k < 8; k++)
            {
                float a = baseAng + k * Mathf.PI / 4f;
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                for (float r = 3; r <= R - 4; r += 0.5f) c.Set(Mathf.RoundToInt(cx + ca * r), Mathf.RoundToInt(cy + sa * r), spoke);
                // 테두리 표식(돌아가는 게 잘 보이게)
                c.Set(Mathf.RoundToInt(cx + ca * (R - 2)), Mathf.RoundToInt(cy + sa * (R - 2)), k % 2 == 0 ? PixelPalette.PinkMid : PixelPalette.YellowMid);
            }
            c.Disc(cx, cy, 3, PixelPalette.YellowMid); c.Set(cx - 1, cy + 1, PixelPalette.YellowLight); c.Set(cx, cy, PixelPalette.MetalDark);
            return c;
        }

        public static PixelCanvas WandRod()
        {
            var c = new PixelCanvas(3, 34);
            for (int y = 0; y < 34; y++) { c.Set(1, y, y < 4 ? PixelPalette.MetalDark : (y % 6 == 0 ? PixelPalette.PinkMid : PixelPalette.WoodLight)); }
            c.Set(0, 0, PixelPalette.MetalDark); c.Set(2, 0, PixelPalette.MetalDark);
            return c;
        }

        public static PixelCanvas WandFeather()
        {
            var c = new PixelCanvas(14, 22);
            var P = PixelPalette.PinkMid; var L = PixelPalette.PinkLight; var Y = PixelPalette.YellowMid; var B = PixelPalette.BlueMid;
            for (int y = 3; y < 21; y++)
            {
                int w = y < 10 ? (y - 2) * 2 / 3 : (21 - y) / 2;
                for (int x = 7 - w; x <= 6 + w; x++) c.Set(x, y, x == 6 || x == 7 ? Y : ((x + y) % 3 == 0 ? B : (y % 2 == 0 ? P : L)));
            }
            c.Set(6, 21, Y); c.Set(7, 21, Y); c.VLine(6, 0, 2, PixelPalette.Outline);
            c.AutoOutline(PixelPalette.Outline);
            return c;
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
            // 바닥에 까는 타원 러그(64×24, 원근감 있게 납작). 중앙 피벗.
            var c = new PixelCanvas(64, 24);
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31f, dy = (y - 11.5f) / 11f;
                    float d = dx * dx + dy * dy;
                    if (d > 1f) continue;
                    var col = d > 0.78f ? PixelPalette.PinkMid : d > 0.62f ? PixelPalette.CreamLight
                            : d > 0.35f ? PixelPalette.PinkLight : PixelPalette.CreamLight;
                    c.Set(x, y, col);
                }
            for (int x = 20; x <= 43; x += 4) c.Set(x, 11, PixelPalette.PinkMid);   // 가운데 무늬
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
