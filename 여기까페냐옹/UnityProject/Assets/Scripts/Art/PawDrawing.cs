using UnityEngine;

namespace YeogiCafe.Art
{
    // 플레이어(바리스타 고양이)의 앞발 — 1인칭 주방·밤 도감 화면에서 쓰는 고해상도 도트.
    // 팔(아래에서 올라오는 원기둥) + 둥근 발 + 발가락 4개를 픽셀마다 입체 셰이딩(5톤)하고,
    // 손목 털뭉치·털결·발가락 사이 홈·따뜻한 갈색 외곽선으로 마감한다.
    public static class PawDrawing
    {
        public const int W = 60, H = 88;

        // 크림색 털(밝은 쪽 → 어두운 쪽)
        static readonly Color32 F0 = new(255, 251, 243, 255);
        static readonly Color32 F1 = new(248, 238, 222, 255);
        static readonly Color32 F2 = new(236, 220, 198, 255);
        static readonly Color32 F3 = new(214, 194, 168, 255);
        static readonly Color32 F4 = new(186, 162, 136, 255);
        static readonly Color32 Line = new(120, 92, 74, 255);
        static readonly Color32 Bean = new(238, 150, 160, 255);
        static readonly Color32 BeanL = new(250, 190, 196, 255);
        static readonly Color32 BeanD = new(206, 112, 128, 255);

        static readonly Vector3 Light = new Vector3(-0.5f, 0.55f, 0.67f).normalized;

        static Color32 Tone(float l) => l > 0.86f ? F0 : l > 0.68f ? F1 : l > 0.48f ? F2 : l > 0.28f ? F3 : F4;

        // 구/원기둥 법선으로 밝기
        static float Shade(float nx, float ny)
        {
            float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
            return Mathf.Clamp01(Vector3.Dot(new Vector3(nx, ny, nz), Light) * 0.9f + 0.3f);
        }

        // 뒤에서 본 앞발(털 쪽). pressed=true면 살짝 눌려 납작하고 발가락이 벌어짐.
        public static PixelCanvas Back(bool pressed = false)
        {
            var c = new PixelCanvas(W, H);
            var z = new float[W * H];
            for (int i = 0; i < z.Length; i++) z[i] = -1f;

            void Put(int x, int y, float depth, Color32 col)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                if (z[y * W + x] > depth) return;
                z[y * W + x] = depth; c.Set(x, y, col);
            }

            // 팔: 아래(y=0)에서 위로, 살짝 안쪽(오른쪽)으로 기울어짐
            for (int y = 0; y <= 56; y++)
            {
                float t = y / 56f;
                float cx = 27f + t * 3f;
                float r = Mathf.Lerp(16f, 13f, t) + (t > 0.8f ? (t - 0.8f) * 12f : 0f);   // 손목 쪽은 털로 살짝 부풂
                for (int x = (int)(cx - r) - 1; x <= (int)(cx + r) + 1; x++)
                {
                    float nx = (x + 0.5f - cx) / r;
                    if (Mathf.Abs(nx) > 1f) continue;
                    float l = Shade(nx, 0.15f);
                    // 털결: 세로로 흐르는 가는 결(어두운 한 톤)
                    // 털결: 비스듬한 짧은 결 몇 가닥(점 노이즈 대신)
                    float fur = Mathf.Sin(x * 0.9f - y * 0.55f);
                    if (fur > 0.97f && nx > -0.6f && nx < 0.7f && (y / 5) % 2 == 0) l -= 0.16f;
                    Put(x, y, 0.2f * (1f - nx * nx), Tone(l));
                }
            }

            // 손목 양옆 털 삐침(팔 가장자리에 붙은 작은 뾰족 털)
            for (int k = 0; k < 3; k++)
            {
                int y = 47 + k * 3;
                float t = y / 56f;
                float cx = 27f + t * 3f, r = Mathf.Lerp(16f, 13f, t) + (t > 0.8f ? (t - 0.8f) * 12f : 0f);
                int lx = Mathf.RoundToInt(cx - r), rx = Mathf.RoundToInt(cx + r);
                Put(lx - 1, y, 0.1f, F3); Put(lx - 2, y + 1, 0.1f, F3); Put(lx - 1, y + 1, 0.1f, F2);
                Put(rx + 1, y, 0.1f, F4); Put(rx + 2, y + 1, 0.1f, F4); Put(rx + 1, y + 1, 0.1f, F3);
            }

            // 발(둥근 덩어리)
            float pcx = 30f, pcy = pressed ? 64f : 66f, prx = pressed ? 19f : 17.5f, pry = pressed ? 12f : 14f;
            for (int y = (int)(pcy - pry) - 1; y <= (int)(pcy + pry) + 1; y++)
                for (int x = (int)(pcx - prx) - 1; x <= (int)(pcx + prx) + 1; x++)
                {
                    float nx = (x + 0.5f - pcx) / prx, ny = (y + 0.5f - pcy) / pry;
                    if (nx * nx + ny * ny > 1f) continue;
                    Put(x, y, 0.5f + (1f - nx * nx - ny * ny) * 0.3f, Tone(Shade(nx, ny)));
                }

            // 발가락 4개(위쪽 가장자리를 따라 볼록)
            float spread = pressed ? 9.5f : 8.2f;
            for (int i = 0; i < 4; i++)
            {
                float tx = pcx - spread * 1.5f + spread * i;
                float ty = pcy + pry - 3.5f - (i == 1 || i == 2 ? -1.2f : 1.0f);
                float tr = (i == 1 || i == 2) ? 5.6f : 5.0f;
                for (int y = (int)(ty - tr) - 1; y <= (int)(ty + tr) + 1; y++)
                    for (int x = (int)(tx - tr) - 1; x <= (int)(tx + tr) + 1; x++)
                    {
                        float nx = (x + 0.5f - tx) / tr, ny = (y + 0.5f - ty) / (tr * 0.92f);
                        if (nx * nx + ny * ny > 1f) continue;
                        Put(x, y, 0.85f + (1f - nx * nx - ny * ny) * 0.2f + i * 0.001f, Tone(Shade(nx * 0.8f, ny) + 0.04f));
                    }
            }

            // 발가락 사이 홈(짧은 세로선) + 발 아래쪽 경계 그늘
            for (int i = 0; i < 3; i++)
            {
                int gx = Mathf.RoundToInt(pcx - spread + spread * i);
                int gy = Mathf.RoundToInt(pcy + pry - 2f);
                for (int d = 0; d < 6; d++) { var p = c.Get(gx, gy - d); if (p.a == 255) c.Set(gx, gy - d, d < 4 ? F4 : F3); }
            }

            // 발등 하이라이트(왼쪽 위 반짝)
            c.Set(21, 72, F0); c.Set(22, 72, F0); c.Set(21, 71, F0);

            Outline(c);
            return c;
        }

        // 발바닥(젤리) 쪽 — 도장 찍기 직전에 살짝 보이는 프레임
        public static PixelCanvas Pads()
        {
            var c = new PixelCanvas(W, H);
            // 팔은 같게
            var back = Back(true);
            for (int y = 0; y < 56; y++) for (int x = 0; x < W; x++) c.Set(x, y, back.Get(x, y));
            float pcx = 30f, pcy = 66f;
            for (int y = 50; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float nx = (x + 0.5f - pcx) / 18f, ny = (y + 0.5f - pcy) / 15f;
                    if (nx * nx + ny * ny > 1f) continue;
                    c.Set(x, y, Tone(Shade(nx, ny) + 0.1f));
                }
            Bean3(c, 30, 61, 8, 6);                       // 큰 젤리
            int[] bx = { 16, 25, 35, 44 }; int[] by = { 72, 77, 77, 72 };
            for (int i = 0; i < 4; i++) Bean3(c, bx[i], by[i], 3.6f, 3.6f);
            Outline(c);
            return c;
        }

        static void Bean3(PixelCanvas c, float cx, float cy, float rx, float ry)
        {
            for (int y = (int)(cy - ry) - 1; y <= (int)(cy + ry) + 1; y++)
                for (int x = (int)(cx - rx) - 1; x <= (int)(cx + rx) + 1; x++)
                {
                    float nx = (x + 0.5f - cx) / rx, ny = (y + 0.5f - cy) / ry;
                    if (nx * nx + ny * ny > 1f) continue;
                    c.Set(x, y, (nx < -0.2f && ny > 0.2f) ? BeanL : (nx * nx + ny * ny > 0.65f ? BeanD : Bean));
                }
        }

        static void Outline(PixelCanvas c)
        {
            var snap = (Color32[])c.Pixels.Clone();
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    if (snap[y * c.W + x].a != 0) continue;
                    bool edge = false;
                    for (int d = 0; d < 4 && !edge; d++)
                    {
                        int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0), ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        if (nx < 0 || ny < 0 || nx >= c.W || ny >= c.H) continue;
                        if (snap[ny * c.W + nx].a == 255) edge = true;
                    }
                    if (edge && y > 0) c.Set(x, y, Line);
                }
        }

        public static PixelCanvas Mirror(PixelCanvas src)
        {
            var c = new PixelCanvas(src.W, src.H);
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                    c.Set(src.W - 1 - x, y, src.Get(x, y));
            return c;
        }

        // 도감에 찍는 고양이 손도장(잉크) 40×40 — 가장자리가 살짝 번진 느낌
        public static PixelCanvas Stamp()
        {
            var c = new PixelCanvas(40, 40);
            var ink = new Color32(214, 92, 108, 255);
            var inkL = new Color32(232, 130, 142, 230);
            void Blob(float cx, float cy, float rx, float ry)
            {
                for (int y = (int)(cy - ry) - 2; y <= (int)(cy + ry) + 2; y++)
                    for (int x = (int)(cx - rx) - 2; x <= (int)(cx + rx) + 2; x++)
                    {
                        float nx = (x + 0.5f - cx) / rx, ny = (y + 0.5f - cy) / ry;
                        float d = nx * nx + ny * ny;
                        float wob = Mathf.Sin(x * 2.3f + y * 1.7f) * 0.12f;
                        if (d > 1f + wob) continue;
                        // 잉크 결: 가장자리는 진하고 가운데는 아주 조금 옅게(종이 질감)
                        c.Set(x, y, d > 0.85f + wob ? inkL : ink);
                    }
            }
            // 큰 젤리: 위는 둥글고 아래는 세 갈래로 살짝 물결
            Blob(20, 15, 9.5f, 6.5f);
            Blob(13.5f, 10.5f, 4.2f, 4f); Blob(20, 9.5f, 4.4f, 4f); Blob(26.5f, 10.5f, 4.2f, 4f);
            // 발가락 4개
            Blob(7.5f, 24, 3.5f, 4.3f); Blob(14.5f, 30, 3.8f, 4.5f); Blob(25.5f, 30, 3.8f, 4.5f); Blob(32.5f, 24, 3.5f, 4.3f);
            return c;
        }

        // 연필 체크 표시(√) 24×24
        public static PixelCanvas Check()
        {
            var c = new PixelCanvas(24, 24);
            var red = new Color32(214, 86, 96, 255);
            var redL = new Color32(236, 132, 140, 255);
            void Dot(float x, float y, float r, Color32 col)
            {
                for (int yy = (int)(y - r); yy <= (int)(y + r); yy++)
                    for (int xx = (int)(x - r); xx <= (int)(x + r); xx++)
                        if ((xx + 0.5f - x) * (xx + 0.5f - x) + (yy + 0.5f - y) * (yy + 0.5f - y) <= r * r) c.Set(xx, yy, col);
            }
            // 짧은 획(왼쪽 아래로) → 긴 획(오른쪽 위로), 끝으로 갈수록 가늘게
            for (float t = 0; t <= 1f; t += 0.04f) Dot(Mathf.Lerp(3, 9, t), Mathf.Lerp(12, 5, t), Mathf.Lerp(1.6f, 2.2f, t), red);
            for (float t = 0; t <= 1f; t += 0.03f) Dot(Mathf.Lerp(9, 21, t), Mathf.Lerp(5, 20, t), Mathf.Lerp(2.2f, 1.1f, t), t > 0.7f ? redL : red);
            return c;
        }
    }
}
