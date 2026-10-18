using System;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 고해상도 고양이 도트 리그(옆모습, 오른쪽을 봄). 64×48 캔버스, 접지선 y=4.
    // 고양이를 타원·캡슐(몸통/가슴/엉덩이/머리/다리/꼬리)로 조립 → 픽셀마다 입체 셰이딩(4톤) →
    // 개체 무늬(태비 줄·삼색 얼룩·턱시도·샴 포인트) → 털색보다 어두운 색 외곽선.
    // 포즈는 숫자 파라미터라 프레임 사이를 보간해 '부드러운' 모션을 만든다(걷기 8프레임 등).
    public static class CatRig
    {
        public const int W = 64, H = 48, Ground = 4;

        // ───────── 포즈 파라미터 ─────────
        public struct Leg { public float jx, jy, px, py, r, kx, ky; public bool far, show, knee; }   // knee: 두 마디(뒷다리 뒤꿈치)

        public class Pose
        {
            public float bodyX = 30, bodyY = 20, bodyRx = 15, bodyRy = 7.5f, bodyAng;
            public float chestX = 40, chestY = 21, chestR = 8;
            public float hipX = 19, hipY = 21, hipR = 8;
            public float headX = 49, headY = 31, headR = 10, headTilt;   // tilt(라디안): +는 아래로 숙임
            public float earFold;                 // 0=쫑긋, 1=납작(잘 때)
            public float eyeOpen = 1;             // 0=감음
            public bool tongue;
            public Leg fn, ff, bn, bf;            // front/back · near/far
            public float tailX = 15, tailY = 24, tailAng = 2.5f, tailCurl = -0.08f, tailLen = 18, tailW = 2.6f;
            public bool tailFront;                // 몸 앞으로 감싸는 꼬리(앉기·식빵·잠)
            public float shadowRx = 16;
            public bool bowl;                     // 먹기: 앞에 그릇

            public Pose Clone() => (Pose)MemberwiseClone();
        }

        // ───────── 개체 팔레트 ─────────
        struct Fur { public Color32 hi, light, mid, shade, deep; }

        static Fur F(int r, int g, int b)
        {
            Color32 Mul(float k, int add = 0) => new Color32(
                (byte)Mathf.Clamp(r * k + add, 0, 255), (byte)Mathf.Clamp(g * k + add, 0, 255), (byte)Mathf.Clamp(b * k + add * 0.6f, 0, 255), 255);
            return new Fur { hi = Mul(1.08f, 18), light = Mul(1.0f), mid = Mul(0.88f), shade = Mul(0.74f), deep = Mul(0.58f) };
        }

        static readonly Fur Orange = F(238, 176, 98);
        static readonly Fur White  = F(246, 240, 232);
        static readonly Fur Black  = F(84, 78, 92);
        static readonly Fur Gray   = F(178, 180, 188);
        static readonly Fur BlueGray = F(140, 158, 176);
        static readonly Fur Cream  = F(240, 222, 190);
        static readonly Fur Seal   = F(122, 96, 80);

        static Color32 EyeColor(string id) => id switch
        {
            "cat_siamese" => new Color32(110, 168, 220, 255),
            "cat_black"   => new Color32(232, 196, 84, 255),
            "cat_tuxedo"  => new Color32(150, 196, 110, 255),
            "cat_gray"    => new Color32(232, 176, 80, 255),
            _             => new Color32(120, 170, 110, 255),
        };

        // 픽셀별 무늬 → 어느 털색 세트를 쓸지
        static Fur Coat(string id, Part part, float x, float y, float u, float v)
        {
            switch (id)
            {
                case "cat_cheese":   return Orange;
                case "cat_mackerel": return BlueGray;
                case "cat_gray":     return part == Part.Muzzle ? White : Gray;
                case "cat_black":    return Black;
                case "cat_siamese":
                    if (part == Part.Ear || part == Part.Tail || part == Part.Muzzle) return Seal;
                    if (part == Part.Leg && v < 0.2f) return Seal;
                    if (part == Part.Head && u > 0.35f) return Seal;           // 얼굴 마스크(앞쪽)
                    return Cream;
                case "cat_tuxedo":
                    if (part == Part.Muzzle) return White;
                    if (part == Part.Leg && v < -0.1f) return White;           // 흰 양말
                    if (part == Part.Chest && u > -0.1f && v < 0.35f) return White;   // 가슴 턱받이
                    if (part == Part.Body && v < -0.35f) return White;          // 배
                    return Black;
                case "cat_cow":
                    if (part == Part.Muzzle || part == Part.Leg) return White;
                    return Blob(x, y, 0.21f, 1.7f) > 0.62f ? Black : White;
                case "cat_calico":
                    if (part == Part.Muzzle || part == Part.Leg) return White;
                    float b = Blob(x, y, 0.19f, 0.4f);
                    if (b > 0.58f) return Orange;
                    if (b < -0.55f) return Black;
                    return White;
            }
            return Cream;
        }

        static float Blob(float x, float y, float f, float seed)
            => (Mathf.Sin(x * f + seed) * Mathf.Sin(y * f * 1.3f + seed * 2.1f) + Mathf.Sin((x + y) * f * 0.55f + seed * 3.7f)) * 0.75f;

        static bool Tabby(string id) => id == "cat_cheese" || id == "cat_mackerel";

        // ───────── 래스터 ─────────
        enum Part : byte { None, Body, Chest, Hip, Head, Muzzle, Ear, Leg, Tail, Paw }

        struct Px { public Part part; public float z, light, u, v; public bool far; }

        public static PixelCanvas Render(string catId, Pose p)
        {
            var buf = new Px[W * H];
            var L = new Vector3(-0.55f, 0.6f, 0.58f).normalized;   // 왼쪽 위 빛

            // 그림자
            var c = new PixelCanvas(W, H);
            for (int x = -(int)p.shadowRx; x <= p.shadowRx; x++)
            {
                float t = 1f - x * x / (p.shadowRx * p.shadowRx);
                if (t <= 0) continue;
                int ry = Mathf.Max(1, Mathf.RoundToInt(2.2f * Mathf.Sqrt(t)));
                for (int y = -ry; y <= ry; y++) c.Blend((int)p.bodyX + x, Ground + y, new Color32(74, 54, 47, 55));
            }

            var shadow = (Color32[])c.Pixels.Clone();

            // 뒤쪽(먼) 다리 → 꼬리(뒤) → 몸 → 가까운 다리 → 머리 → (앞으로 감싼) 꼬리
            DrawLeg(buf, p.bf, 1f, L); DrawLeg(buf, p.ff, 1f, L);
            if (!p.tailFront) DrawTail(buf, p, 2f, L);
            Ellipse(buf, p.hipX, p.hipY, p.hipR, p.hipR, 0, 3f, Part.Hip, L);
            Ellipse(buf, p.bodyX, p.bodyY, p.bodyRx, p.bodyRy, p.bodyAng, 3.5f, Part.Body, L);
            Ellipse(buf, p.chestX, p.chestY, p.chestR, p.chestR * 1.05f, 0, 4f, Part.Chest, L);
            DrawLeg(buf, p.bn, 5f, L); DrawLeg(buf, p.fn, 5f, L);
            DrawHead(buf, p, L);
            if (p.tailFront) DrawTail(buf, p, 6f, L);

            // 땅 아래는 자름(식빵·잠 포즈에서 몸 아랫면이 바닥에 붙어 보이게)
            for (int y = 0; y < Ground; y++) for (int x = 0; x < W; x++) buf[y * W + x].part = Part.None;

            // ── 색 칠하기 ──
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    var q = buf[y * W + x];
                    if (q.part == Part.None) continue;
                    var fur = Coat(catId, q.part, x, y, q.u, q.v);
                    float li = q.light - (q.far ? 0.28f : 0f);
                    // 태비 줄무늬(몸 둘레를 감는 띠)
                    if (Tabby(catId) && (q.part == Part.Body || q.part == Part.Hip || q.part == Part.Tail || q.part == Part.Head || q.part == Part.Leg))
                    {
                        float s = Mathf.Sin(x * 0.85f + q.v * 2.2f + (q.part == Part.Head ? y * 0.9f : 0f));
                        if (s > 0.55f && q.v > -0.55f) li -= 0.3f;
                    }
                    if (q.part == Part.Muzzle) li += 0.08f;
                    Color32 col = li > 0.86f ? fur.hi : li > 0.62f ? fur.light : li > 0.38f ? fur.mid : li > 0.16f ? fur.shade : fur.deep;
                    c.Set(x, y, col);
                }

            // 부위 경계 내부선(머리-몸, 가까운 다리-몸): 앞쪽 부위 가장자리에 한 톤 어두운 선
            var snap = (Color32[])c.Pixels.Clone();
            for (int y = 1; y < H - 1; y++)
                for (int x = 1; x < W - 1; x++)
                {
                    var q = buf[y * W + x];
                    if (q.part == Part.None) continue;
                    foreach (var (dx, dy) in N4)
                    {
                        var n = buf[(y + dy) * W + x + dx];
                        if (n.part == Part.None || n.part == q.part) continue;
                        bool edgePart = q.part == Part.Head || q.part == Part.Leg || q.part == Part.Paw || (q.part == Part.Tail && p.tailFront);
                        if (edgePart && q.z > n.z + 0.9f)
                        {
                            var s = snap[y * W + x];
                            c.Set(x, y, Darken(s, 0.8f));
                            break;
                        }
                    }
                }

            DrawFace(c, buf, catId, p);
            if (p.bowl) DrawBowl(c, p);

            // 얼굴 장식(혀·수염)이 땅 아래로 삐져나오면 그림자로 되돌림
            for (int y = 0; y < Ground; y++)
                for (int x = 0; x < W; x++)
                    if (c.Pixels[y * W + x].a == 255) c.Pixels[y * W + x] = shadow[y * W + x];

            // 외곽선: 바깥 픽셀에 '인접 털색의 어두운 버전'(검정 대신 부드럽게)
            snap = (Color32[])c.Pixels.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (snap[y * W + x].a == 255) continue;
                    Color32? nb = null;
                    foreach (var (dx, dy) in N4)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                        var s = snap[ny * W + nx];
                        if (s.a == 255) { nb = s; break; }
                    }
                    if (nb.HasValue) c.Set(x, y, Outline(nb.Value));
                }
            return c;
        }

        static readonly (int, int)[] N4 = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        static Color32 Darken(Color32 c, float k) => new((byte)(c.r * k), (byte)(c.g * k), (byte)(c.b * k), 255);
        static Color32 Outline(Color32 c)
        {
            // 따뜻한 갈색 쪽으로 끌어당긴 어두운 색 → 팔레트 통일(PixelPalette.Outline 계열)
            return new Color32((byte)(c.r * 0.32f + 50), (byte)(c.g * 0.28f + 34), (byte)(c.b * 0.28f + 30), 255);
        }

        static void Put(Px[] buf, int x, int y, Part part, float z, float light, float u, float v, bool far = false)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            ref var q = ref buf[y * W + x];
            if (q.part != Part.None && q.z > z) return;
            q.part = part; q.z = z; q.light = light; q.u = u; q.v = v; q.far = far;
        }

        static float Shade(float u, float v, Vector3 L)
        {
            float w = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u - v * v));
            var n = new Vector3(u, v, w);
            return Mathf.Clamp01(Vector3.Dot(n, L) * 0.85f + 0.32f);
        }

        static void Ellipse(Px[] buf, float cx, float cy, float rx, float ry, float ang, float z, Part part, Vector3 L, bool far = false)
        {
            float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
            int R = (int)Mathf.Ceil(Mathf.Max(rx, ry)) + 1;
            for (int y = (int)cy - R; y <= (int)cy + R; y++)
                for (int x = (int)cx - R; x <= (int)cx + R; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float lx = (dx * ca + dy * sa) / rx, ly = (-dx * sa + dy * ca) / ry;
                    if (lx * lx + ly * ly > 1f) continue;
                    // 법선은 화면 기준으로 되돌려 빛 계산
                    float u = lx * ca - ly * sa, v = lx * sa + ly * ca;
                    Put(buf, x, y, part, z + (1f - lx * lx - ly * ly) * 0.5f, Shade(u, v, L), lx, ly, far);
                }
        }

        // 굵기가 변하는 캡슐(다리·꼬리 마디)
        static void Capsule(Px[] buf, float x0, float y0, float x1, float y1, float r0, float r1, float z, Part part, Vector3 L, bool far = false, float vBase = 0f)
        {
            float minx = Mathf.Min(x0, x1) - Mathf.Max(r0, r1) - 1, maxx = Mathf.Max(x0, x1) + Mathf.Max(r0, r1) + 1;
            float miny = Mathf.Min(y0, y1) - Mathf.Max(r0, r1) - 1, maxy = Mathf.Max(y0, y1) + Mathf.Max(r0, r1) + 1;
            float ax = x1 - x0, ay = y1 - y0, len2 = ax * ax + ay * ay + 1e-4f;
            for (int y = (int)miny; y <= (int)maxy; y++)
                for (int x = (int)minx; x <= (int)maxx; x++)
                {
                    float px = x + 0.5f - x0, py = y + 0.5f - y0;
                    float t = Mathf.Clamp01((px * ax + py * ay) / len2);
                    float qx = px - ax * t, qy = py - ay * t;
                    float r = Mathf.Lerp(r0, r1, t);
                    float d = Mathf.Sqrt(qx * qx + qy * qy);
                    if (d > r) continue;
                    float nx = qx / r, ny = qy / r;
                    // v: 관절(0) → 끝(-1) 방향 좌표(양말·포인트 무늬용)
                    Put(buf, x, y, part, z + (1f - d / r) * 0.3f, Shade(nx, ny, L), nx, vBase - t, far);
                }
        }

        static void DrawLeg(Px[] buf, Leg l, float z, Vector3 L)
        {
            if (!l.show) return;
            float r = l.r <= 0 ? 2.6f : l.r;
            if (l.knee)
            {
                Capsule(buf, l.jx, l.jy, l.kx, l.ky, r * 1.45f, r * 0.95f, z, Part.Leg, L, l.far, 1f);       // 허벅지(굵게)
                Capsule(buf, l.kx, l.ky, l.px, l.py + 1.4f, r * 0.95f, r * 0.8f, z + 0.1f, Part.Leg, L, l.far, 0f);
            }
            else Capsule(buf, l.jx, l.jy, l.px, l.py + 1.4f, r * 1.15f, r * 0.85f, z, Part.Leg, L, l.far, 1f);
            // 발(앞쪽으로 살짝 긴 타원)
            Ellipse(buf, l.px + 0.8f, l.py + 1.4f, r * 1.15f, r * 0.75f, 0, z + 0.2f, Part.Leg, L, l.far);
        }

        static void DrawTail(Px[] buf, Pose p, float z, Vector3 L)
        {
            const int Seg = 10;
            float x = p.tailX, y = p.tailY, a = p.tailAng, step = p.tailLen / Seg;
            for (int i = 0; i < Seg; i++)
            {
                float nx = x + Mathf.Cos(a) * step, ny = y + Mathf.Sin(a) * step;
                float r0 = Mathf.Lerp(p.tailW, p.tailW * 0.72f, i / (float)Seg);
                float r1 = Mathf.Lerp(p.tailW, p.tailW * 0.72f, (i + 1) / (float)Seg);
                Capsule(buf, x, y, nx, ny, r0, r1, z + i * 0.01f, Part.Tail, L, false, 1f - i / (float)Seg);
                x = nx; y = ny; a += p.tailCurl;
            }
        }

        static void DrawHead(Px[] buf, Pose p, Vector3 L)
        {
            float t = p.headTilt;
            float ct = Mathf.Cos(t), st = Mathf.Sin(t);
            Vector2 Rot(float dx, float dy) => new Vector2(p.headX + dx * ct + dy * st, p.headY - dx * st + dy * ct);

            float k = p.headR / 9f;   // 귀 크기는 머리 크기에 비례
            // 귀(뒤 귀 먼저, 어둡게) — 쫑긋 ↔ 납작 보간
            DrawEar(buf, p, Rot(-6f * k, 5f * k), Rot((-6.5f - p.earFold * 3f) * k, (14f - p.earFold * 6f) * k), Rot(-0.5f * k, 7.5f * k), 6.5f, true, L);
            Ellipse(buf, p.headX, p.headY, p.headR * 1.08f, p.headR, -t * 0.5f, 7f, Part.Head, L);
            // 볼살(아래쪽으로 퍼진 귀여운 실루엣)
            var cheek = Rot(1.5f, -3.5f);
            Ellipse(buf, cheek.x, cheek.y, p.headR * 0.95f, p.headR * 0.6f, 0, 7.05f, Part.Head, L);
            var muz = Rot(p.headR * 0.78f, -2.2f);
            Ellipse(buf, muz.x, muz.y, 3.4f, 2.6f, 0, 7.6f, Part.Muzzle, L);
            DrawEar(buf, p, Rot(-0.5f * k, 7f * k), Rot((2.5f - p.earFold * 4f) * k, (15f - p.earFold * 6.5f) * k), Rot(6.5f * k, 4.5f * k), 7.8f, false, L);
        }

        static void DrawEar(Px[] buf, Pose p, Vector2 a, Vector2 tip, Vector2 b, float z, bool far, Vector3 L)
        {
            int minx = (int)Mathf.Min(a.x, Mathf.Min(tip.x, b.x)) - 1, maxx = (int)Mathf.Max(a.x, Mathf.Max(tip.x, b.x)) + 1;
            int miny = (int)Mathf.Min(a.y, Mathf.Min(tip.y, b.y)) - 1, maxy = (int)Mathf.Max(a.y, Mathf.Max(tip.y, b.y)) + 1;
            for (int y = miny; y <= maxy; y++)
                for (int x = minx; x <= maxx; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f);
                    if (!InTri(q, a, tip, b)) continue;
                    // 귀 안쪽(분홍)은 가까운 귀에만 — 중심 쪽으로 반쯤 줄인 삼각형, 머리 쪽 아랫부분만
                    var cen = (a + tip + b) / 3f;
                    var ia = cen + (a - cen) * 0.45f; var it = cen + (tip - cen) * 0.45f; var ib = cen + (b - cen) * 0.45f;
                    bool inner = !far && InTri(q, ia, it, ib);
                    Put(buf, x, y, inner ? Part.Muzzle : Part.Ear, z + (inner ? 0.05f : 0f), far ? 0.45f : 0.75f, inner ? 9f : 0f, 0f, far);
                }
        }

        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
        static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        static readonly Color32 Pink = new(226, 140, 150, 255);
        static readonly Color32 PinkLight = new(246, 186, 192, 255);
        static readonly Color32 EyeDark = new(52, 40, 44, 255);
        static readonly Color32 Shine = new(255, 252, 245, 255);

        static void DrawFace(PixelCanvas c, Px[] buf, string id, Pose p)
        {
            float t = p.headTilt, ct = Mathf.Cos(t), st = Mathf.Sin(t);
            Vector2 Rot(float dx, float dy) => new Vector2(p.headX + dx * ct + dy * st, p.headY - dx * st + dy * ct);

            // 귀 안쪽 분홍(Muzzle 마커 u==9 로 표시해 둔 픽셀)
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (buf[y * W + x].part == Part.Muzzle && buf[y * W + x].u > 8f) c.Set(x, y, PinkLight);

            // 눈: 크고 둥글게(3×4), 세로 동공 + 하이라이트. 감으면 아래로 휜 선.
            var e = Rot(4.2f, 1.2f);
            int ex = Mathf.RoundToInt(e.x), ey = Mathf.RoundToInt(e.y);
            if (p.eyeOpen > 0.5f)
            {
                var iris = EyeColor(id);
                for (int dy = -2; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        c.Set(ex + dx, ey + dy, (dx == 0 || dy == -2 || dy == 1) ? EyeDark : iris);
                c.Set(ex - 1, ey - 2, iris); c.Set(ex + 1, ey - 2, iris);
                c.Set(ex - 1, ey + 1, EyeDark); c.Set(ex + 1, ey + 1, EyeDark);
                c.Set(ex - 1, ey, Shine); c.Set(ex - 1, ey + 1, Shine);           // 반짝임
                c.Set(ex + 2, ey, EyeDark); c.Set(ex + 2, ey - 1, EyeDark);       // 눈꼬리
            }
            else
            {
                c.Set(ex - 1, ey, EyeDark); c.Set(ex, ey - 1, EyeDark); c.Set(ex + 1, ey - 1, EyeDark); c.Set(ex + 2, ey, EyeDark);
            }
            // 코·입
            var n = Rot(p.headR * 0.78f + 3f, -1.2f);
            int nx = Mathf.RoundToInt(n.x), ny = Mathf.RoundToInt(n.y);
            c.Set(nx, ny, Pink); c.Set(nx - 1, ny, Pink); c.Set(nx, ny - 1, Pink);
            c.Set(nx - 1, ny - 2, EyeDark); c.Set(nx - 2, ny - 3, EyeDark);
            if (p.tongue) { c.Set(nx, ny - 3, Pink); c.Set(nx + 1, ny - 3, Pink); c.Set(nx + 1, ny - 4, PinkLight); }
            // 볼 홍조(살짝)
            var ch = Rot(1.5f, -2.5f);
            c.Blend(Mathf.RoundToInt(ch.x), Mathf.RoundToInt(ch.y), new Color32(240, 140, 150, 90));
            c.Blend(Mathf.RoundToInt(ch.x) + 1, Mathf.RoundToInt(ch.y), new Color32(240, 140, 150, 90));
            // 수염(얼굴 밖으로 2가닥)
            for (int i = 0; i < 4; i++)
            {
                c.Blend(nx + 2 + i, ny - 1 + (i > 1 ? 0 : 0), new Color32(255, 250, 240, 150));
                c.Blend(nx + 2 + i, ny - 3 - i / 2, new Color32(255, 250, 240, 120));
            }
        }

        static void DrawBowl(PixelCanvas c, Pose p)
        {
            int bx = Mathf.RoundToInt(p.headX + 6);
            for (int y = Ground; y <= Ground + 4; y++)
            {
                int half = 6 - (Ground + 4 - y) / 2;
                c.HLine(bx - half, bx + half, y, y == Ground + 4 ? PixelPalette.CreamLight : PixelPalette.BlueMid);
            }
            c.HLine(bx - 4, bx + 4, Ground + 5, PixelPalette.PinkMid);       // 음식
            c.HLine(bx - 3, bx + 3, Ground + 6, PixelPalette.PinkLight);
        }

        // ───────── 기본 포즈 ─────────
        static Leg LegOf(float jx, float jy, float px, float py, bool far, float r = 2.6f)
            => new Leg { jx = jx, jy = jy, px = px, py = py, far = far, show = true, r = r };

        // 뒷다리: 엉덩이 → 뒤꿈치(뒤로 꺾임) → 발
        static Leg Hind(float jx, float jy, float px, float py, bool far)
            => new Leg { jx = jx, jy = jy, kx = px - 3.5f, ky = py + 6.5f, px = px, py = py, far = far, show = true, knee = true, r = 2.5f };

        public static Pose Stand()
        {
            var p = new Pose();
            p.fn = LegOf(42, 17, 43, Ground, false); p.ff = LegOf(38, 17, 38, Ground, true);
            p.bn = Hind(20, 19, 19, Ground, false); p.bf = Hind(24, 19, 23, Ground, true);
            return p;
        }

        public static Pose Sit()
        {
            var p = new Pose
            {
                bodyX = 30, bodyY = 17, bodyRx = 9, bodyRy = 12.5f, bodyAng = -0.45f,
                chestX = 36, chestY = 19, chestR = 7.5f,
                hipX = 25, hipY = 12, hipR = 10,
                headX = 39, headY = 33, headR = 10,
                tailX = 17, tailY = 6, tailAng = -0.15f, tailCurl = 0.06f, tailLen = 22, tailW = 2.5f, tailFront = true,
                shadowRx = 14,
            };
            p.fn = LegOf(38, 16, 39, Ground, false, 2.5f); p.ff = LegOf(34.5f, 16, 35, Ground, true, 2.5f);
            p.bn = new Leg { jx = 27, jy = 9, px = 31, py = Ground, r = 2.8f, show = true };   // 접힌 뒷발(발만 보임)
            p.bf = default;
            return p;
        }

        public static Pose Loaf()
        {
            var p = new Pose
            {
                bodyX = 30, bodyY = 10, bodyRx = 17, bodyRy = 9.5f, bodyAng = 0.04f,
                chestX = 40, chestY = 11, chestR = 8,
                hipX = 19, hipY = 10.5f, hipR = 9,
                headX = 46, headY = 20, headR = 10,
                tailX = 14, tailY = 6, tailAng = -0.2f, tailCurl = 0.035f, tailLen = 26, tailW = 2.5f, tailFront = true,
                shadowRx = 18,
            };
            p.fn = new Leg { jx = 46, jy = 7, px = 49, py = Ground, r = 2.4f, show = true };   // 앞발 끝만 살짝
            return p;
        }

        public static Pose Curl()
        {
            var p = new Pose
            {
                bodyX = 29, bodyY = 13, bodyRx = 14, bodyRy = 11.5f, bodyAng = 0.15f,
                chestX = 35, chestY = 12, chestR = 8,
                hipX = 22, hipY = 14, hipR = 10.5f,
                headX = 41, headY = 13, headR = 9, headTilt = 0.85f, earFold = 0.25f, eyeOpen = 0,
                tailX = 16, tailY = 6, tailAng = -0.1f, tailCurl = 0.11f, tailLen = 32, tailW = 3.2f, tailFront = true,
                shadowRx = 17,
            };
            return p;
        }

        public static Pose Lerp(Pose a, Pose b, float t)
        {
            float F(float x, float y) => x + (y - x) * t;
            Leg LL(Leg x, Leg y) => new Leg
            {
                jx = F(x.jx, y.jx), jy = F(x.jy, y.jy), px = F(x.px, y.px), py = F(x.py, y.py),
                kx = F(x.knee ? x.kx : (x.jx + x.px) / 2, y.knee ? y.kx : (y.jx + y.px) / 2),
                ky = F(x.knee ? x.ky : (x.jy + x.py) / 2, y.knee ? y.ky : (y.jy + y.py) / 2), knee = x.knee || y.knee,
                r = F(x.r == 0 ? 2.6f : x.r, y.r == 0 ? 2.6f : y.r), far = x.far || y.far,
                show = t < 0.5f ? x.show : y.show
            };
            var p = new Pose
            {
                bodyX = F(a.bodyX, b.bodyX), bodyY = F(a.bodyY, b.bodyY), bodyRx = F(a.bodyRx, b.bodyRx), bodyRy = F(a.bodyRy, b.bodyRy), bodyAng = F(a.bodyAng, b.bodyAng),
                chestX = F(a.chestX, b.chestX), chestY = F(a.chestY, b.chestY), chestR = F(a.chestR, b.chestR),
                hipX = F(a.hipX, b.hipX), hipY = F(a.hipY, b.hipY), hipR = F(a.hipR, b.hipR),
                headX = F(a.headX, b.headX), headY = F(a.headY, b.headY), headR = F(a.headR, b.headR), headTilt = F(a.headTilt, b.headTilt),
                earFold = F(a.earFold, b.earFold), eyeOpen = F(a.eyeOpen, b.eyeOpen), tongue = t < 0.5f ? a.tongue : b.tongue,
                fn = LL(a.fn, b.fn), ff = LL(a.ff, b.ff), bn = LL(a.bn, b.bn), bf = LL(a.bf, b.bf),
                tailX = F(a.tailX, b.tailX), tailY = F(a.tailY, b.tailY), tailAng = F(a.tailAng, b.tailAng), tailCurl = F(a.tailCurl, b.tailCurl),
                tailLen = F(a.tailLen, b.tailLen), tailW = F(a.tailW, b.tailW), tailFront = t < 0.5f ? a.tailFront : b.tailFront,
                shadowRx = F(a.shadowRx, b.shadowRx), bowl = t < 0.5f ? a.bowl : b.bowl,
            };
            return p;
        }
    }
}
