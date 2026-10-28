using UnityEngine;

namespace YeogiCafe.Art
{
    // 쥐 소동용 작은 생쥐(옆모습, 오른쪽을 봄)와 벽 아래 쥐구멍.
    public static class MouseDrawing
    {
        public const int W = 28, H = 14;
        static Color32 C(int r, int g, int b, int a = 255) => new((byte)r, (byte)g, (byte)b, (byte)a);
        static readonly Color32 G0 = C(214, 210, 216), G1 = C(178, 174, 184), G2 = C(142, 138, 152), G3 = C(108, 104, 120);
        static readonly Color32 Pink = C(236, 150, 160), PinkD = C(200, 112, 126), Eye = C(40, 30, 36), Line = C(80, 66, 78);

        // frame 0·1 = 달리기(다리 교대·몸 들썩), 2 = 잡힘(납작, 눈 X)
        public static PixelCanvas Draw(int frame)
        {
            var c = new PixelCanvas(W, H);
            bool caught = frame == 2;
            float bob = frame == 1 ? 1f : 0f;
            float bx = 13f, by = caught ? 4f : 5.5f + bob, brx = caught ? 8.5f : 7.5f, bry = caught ? 3.2f : 4.4f;
            // 꼬리(분홍 가는 곡선) — 몸 뒤에서 왼쪽으로
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f;
                int x = Mathf.RoundToInt(bx - brx + 1 - t * 6f);
                int y = Mathf.RoundToInt(by - 1 + Mathf.Sin(t * 3.2f + frame) * 2f + t * 2f);
                c.Set(x, y, Pink);
            }
            // 다리
            if (!caught)
            {
                int a = frame == 0 ? 0 : 2;
                c.Set(8 + a, 1, PinkD); c.Set(8 + a, 2, PinkD); c.Set(15 - a, 1, PinkD); c.Set(15 - a, 2, PinkD);
            }
            // 몸
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float nx = (x + 0.5f - bx) / brx, ny = (y + 0.5f - by) / bry;
                    if (nx * nx + ny * ny > 1f) continue;
                    float l = -nx * 0.3f + ny * 0.6f;
                    c.Set(x, y, l > 0.35f ? G0 : l > -0.1f ? G1 : l > -0.5f ? G2 : G3);
                }
            // 머리(앞으로 뾰족)
            float hx = bx + brx - 1.5f, hy = by + 0.5f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = x + 0.5f - hx, dy = y + 0.5f - hy;
                    float r = 3.6f - Mathf.Max(0f, dx) * 0.5f;
                    if (dx < -3.5f || dx > 6.5f || dx * dx * 0.35f + dy * dy > r * r) continue;
                    c.Set(x, y, dy > 1f ? G0 : G1);
                }
            int nxp = Mathf.RoundToInt(hx + 6f), nyp = Mathf.RoundToInt(hy);
            c.Set(nxp, nyp, Pink); c.Set(nxp - 1, nyp, Pink);
            // 귀(동그란 분홍 속)
            int ex = Mathf.RoundToInt(hx - 2f), ey = Mathf.RoundToInt(hy + 3.5f);
            for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++)
                    if (x * x + y * y <= 5) c.Set(ex + x, ey + y, (x == 0 && (y == 0 || y == -1)) ? Pink : G1);
            // 눈
            int eyx = Mathf.RoundToInt(hx + 2f), eyy = Mathf.RoundToInt(hy + 1f);
            if (caught) { c.Set(eyx - 1, eyy + 1, Eye); c.Set(eyx + 1, eyy - 1, Eye); c.Set(eyx, eyy, Eye); c.Set(eyx - 1, eyy - 1, Eye); c.Set(eyx + 1, eyy + 1, Eye); }
            else { c.Set(eyx, eyy, Eye); c.Set(eyx, eyy + 1, Eye); }
            // 수염
            c.Blend(nxp - 1, nyp + 1, C(255, 255, 255, 140)); c.Blend(nxp + 1, nyp + 1, C(255, 255, 255, 140)); c.Blend(nxp + 1, nyp - 1, C(255, 255, 255, 140));
            c.AutoOutline(Line);
            return c;
        }

        // 벽 아래 쥐구멍(아치형 어두운 구멍)
        public static PixelCanvas Hole()
        {
            var c = new PixelCanvas(16, 11);
            for (int y = 0; y < 11; y++)
                for (int x = 0; x < 16; x++)
                {
                    float dx = (x + 0.5f - 8f) / 7.5f, dy = y / 10.5f;
                    bool inArch = dy < 0.45f ? Mathf.Abs(dx) <= 1f : dx * dx + (dy - 0.45f) * (dy - 0.45f) / 0.3f <= 1f;
                    if (!inArch) continue;
                    bool rim = Mathf.Abs(dx) > 0.82f || (dy >= 0.45f && dx * dx + (dy - 0.45f) * (dy - 0.45f) / 0.3f > 0.7f);
                    c.Set(x, y, rim ? C(110, 76, 54) : C(40, 28, 26));
                }
            return c;
        }
    }
}
