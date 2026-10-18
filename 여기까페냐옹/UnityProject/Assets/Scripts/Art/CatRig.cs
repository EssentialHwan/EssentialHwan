using System;
using UnityEngine;

namespace YeogiCafe.Art
{
    // 고해상도 고양이 도트 리그(몸은 옆모습, 얼굴은 3/4로 정면을 살짝 봄 → 두 눈이 보이는 귀여운 비율). 72×60 캔버스, 접지선 y=4.
    // 고양이를 타원·캡슐(몸통/가슴/엉덩이/머리/다리/꼬리)로 조립 → 픽셀마다 입체 셰이딩(4톤) →
    // 개체 무늬(태비 줄·삼색 얼룩·턱시도·샴 포인트) → 털색보다 어두운 색 외곽선.
    // 포즈는 숫자 파라미터라 프레임 사이를 보간해 '부드러운' 모션을 만든다(걷기 8프레임 등).
    public static class CatRig
    {
        public const int W = 72, H = 60, Ground = 4;
        const float OX = 4f;      // 포즈 좌표(0~64 기준) → 캔버스 여백만큼 오른쪽으로

        // ───────── 포즈 파라미터 ─────────
        public struct Leg { public float jx, jy, px, py, r, kx, ky; public bool far, show, knee, over; }   // over: 머리보다 앞(그루밍·손짓)   // knee: 두 마디(뒷다리 뒤꿈치)

        public class Pose
        {
            public float bodyX = 30, bodyY = 20, bodyRx = 15, bodyRy = 7.5f, bodyAng;
            public float chestX = 40, chestY = 21, chestR = 8;
            public float hipX = 19, hipY = 21, hipR = 8;
            public float headX = 49, headY = 31, headR = 10, headTilt;   // tilt(라디안): +는 아래로 숙임
            public float earFold;                 // 0=쫑긋, 1=납작(잘 때·화날 때)
            public float eyeOpen = 1;             // 0=감음
            public Face face;                     // 표정(eyeOpen<0.5면 감은 눈 우선)
            public bool mouthOpen;                // 하악·야옹
            public bool tongue;
            public Leg fn, ff, bn, bf;            // front/back · near/far
            public float tailX = 15, tailY = 24, tailAng = 2.5f, tailCurl = -0.08f, tailLen = 18, tailW = 2.6f;
            public bool tailFront;                // 몸 앞으로 감싸는 꼬리(앉기·식빵·잠)
            public float shadowRx = 16;
            public bool bowl;                     // 먹기: 앞에 그릇

            public Pose Clone() => (Pose)MemberwiseClone();
        }

        public enum Face : byte { Normal, Happy, Angry, Wide, Sleepy }

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
                    if (part == Part.Head && (u - 0.32f) * (u - 0.32f) + (v + 0.3f) * (v + 0.3f) * 1.3f < 0.32f) return Seal;   // 얼굴 가운데 마스크
                    return Cream;
                case "cat_tuxedo":
                    if (part == Part.Muzzle) return White;
                    if (part == Part.Leg && v < -0.1f) return White;           // 흰 양말
                    if (part == Part.Chest && u > -0.1f && v < 0.35f) return White;   // 가슴 턱받이
                    if (part == Part.Body && v < -0.35f) return White;          // 배
                    return Black;
                case "cat_cow":
                    if (part == Part.Muzzle || part == Part.Leg) return White;
                    if (part == Part.Tail) return v < 0.35f ? Black : White;                // 꼬리 끝 검정
                    if (part == Part.Body || part == Part.Hip)                              // 등에 큰 얼룩 두 개
                    {
                        float d1 = (x - 24f) * (x - 24f) / 30f + (y - 19f) * (y - 19f) / 22f;
                        float d2 = (x - 36f) * (x - 36f) / 16f + (y - 20f) * (y - 20f) / 14f;
                        return d1 < 1f || d2 < 1f ? Black : White;
                    }
                    return Blob(x, y, 0.21f, 1.7f) > 0.62f ? Black : White;
                case "cat_calico":
                    if (part == Part.Muzzle || part == Part.Leg) return White;
                    if (part == Part.Tail) return v < 0.3f ? Black : v < 0.7f ? Orange : White;
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
            p = Shift(p, OX);
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
            DrawLeg(buf, p.bn, 5f, L); if (!p.fn.over) DrawLeg(buf, p.fn, 5f, L);
            DrawHead(buf, p, L);
            if (p.fn.over) DrawLeg(buf, p.fn, 8.5f, L);
            if (p.tailFront) DrawTail(buf, p, 6f, L);

            // 땅 아래는 자름(식빵·잠 포즈에서 몸 아랫면이 바닥에 붙어 보이게)
            for (int y = 0; y < Ground; y++) for (int x = 0; x < W; x++) buf[y * W + x].part = Part.None;

            // ── 색 칠하기 ──
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    var q = buf[y * W + x];
                    if (q.part == Part.None) continue;
                    // 무늬 좌표는 부위 기준(머리는 머리 중심, 몸은 몸 중심) → 움직여도 얼룩이 미끄러지지 않는다
                    float mx, my;
                    switch (q.part)
                    {
                        case Part.Head: case Part.Muzzle: case Part.Ear: mx = x - p.headX + 40f; my = y - p.headY + 30f; break;
                        case Part.Tail: mx = 12f + q.v * 9f; my = 24f; break;
                        default: mx = x - p.bodyX + 30f; my = y - p.bodyY + 15f; break;
                    }
                    var fur = Coat(catId, q.part, mx, my, q.u, q.v);
                    float li = q.light - (q.far ? 0.28f : 0f);
                    // 태비 줄무늬(몸 둘레를 감는 띠)
                    if (Tabby(catId) && (q.part == Part.Body || q.part == Part.Hip || q.part == Part.Tail || q.part == Part.Leg))
                    {
                        float s = Mathf.Sin(x * 0.85f + q.v * 2.2f);
                        if (s > 0.55f && q.v > -0.55f) li -= 0.3f;
                    }
                    // 이마 M자 줄무늬(머리 위쪽 짧은 세로줄 3개)
                    if (Tabby(catId) && q.part == Part.Head && q.v > 0.45f && Mathf.Abs(q.u - 0.15f) < 0.5f)
                    {
                        float s = Mathf.Sin((q.u - 0.15f) * 9.5f);
                        if (s > 0.6f) li -= 0.3f;
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

        static Pose Shift(Pose s, float dx)
        {
            var p = s.Clone();
            p.bodyX += dx; p.chestX += dx; p.hipX += dx; p.headX += dx; p.tailX += dx;
            Leg M(Leg l) { l.jx += dx; l.px += dx; l.kx += dx; return l; }
            p.fn = M(p.fn); p.ff = M(p.ff); p.bn = M(p.bn); p.bf = M(p.bf);
            return p;
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
        static void Capsule(Px[] buf, float x0, float y0, float x1, float y1, float r0, float r1, float z, Part part, Vector3 L, bool far = false, float vBase = 0f, float vScale = 1f)
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
                    Put(buf, x, y, part, z + (1f - d / r) * 0.3f, Shade(nx, ny, L), nx, vBase - t * vScale, far);
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
                Capsule(buf, x, y, nx, ny, r0, r1, z + i * 0.01f, Part.Tail, L, false, 1f - i / (float)Seg, 1f / Seg);
                x = nx; y = ny; a += p.tailCurl;
            }
        }

        // 머리: 3/4 얼굴. 귀 두 개가 머리 위 양쪽, 주둥이는 얼굴 앞쪽 아래(옆얼굴처럼 튀어나오지 않음).
        static void DrawHead(Px[] buf, Pose p, Vector3 L)
        {
            float t = p.headTilt;
            float ct = Mathf.Cos(t), st = Mathf.Sin(t);
            Vector2 Rot(float dx, float dy) => new Vector2(p.headX + dx * ct + dy * st, p.headY - dx * st + dy * ct);

            float R = p.headR, k = R / 10f;
            float fold = p.earFold;
            // 뒤 귀(왼쪽 위) — 납작해지면 뒤로 눕는다
            DrawEar(buf, Rot(-8.6f * k, 3.6f * k), Rot((-8.4f - fold * 5f) * k, (13.6f - fold * 7.5f) * k), Rot(-2.2f * k, 8.4f * k), 6.5f, true, L);
            Ellipse(buf, p.headX, p.headY, R * 1.12f, R * 0.96f, -t * 0.5f, 7f, Part.Head, L);
            // 볼살(아래로 퍼진 동그란 실루엣)
            var cheek = Rot(0.6f, -R * 0.32f);
            Ellipse(buf, cheek.x, cheek.y, R * 1.06f, R * 0.62f, 0, 7.05f, Part.Head, L);
            var muz = Rot(R * 0.42f, -R * 0.42f);
            Ellipse(buf, muz.x, muz.y, R * 0.38f, R * 0.27f, 0, 7.6f, Part.Muzzle, L);
            // 앞 귀(오른쪽 위)
            DrawEar(buf, Rot(2.4f * k, 8.6f * k), Rot((7.6f - fold * 1f) * k, (14.4f - fold * 8f) * k), Rot(9f * k, 3.4f * k), 7.8f, false, L);
        }

        static void DrawEar(Px[] buf, Vector2 a, Vector2 tip, Vector2 b, float z, bool far, Vector3 L)
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
                    bool inner = InTri(q, ia, it, ib);
                    Put(buf, x, y, inner ? Part.Muzzle : Part.Ear, z + (inner ? 0.05f : 0f), far ? 0.5f : 0.75f, inner ? (far ? 8.5f : 9f) : 0f, 0f, false);
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
            float R = p.headR;
            int I(float v) => Mathf.RoundToInt(v);

            // 귀 안쪽 분홍(Muzzle 마커 u>8): 앞 귀는 밝게, 뒤 귀는 한 톤 어둡게
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    var q = buf[y * W + x];
                    if (q.part == Part.Muzzle && q.u > 8f) c.Set(x, y, q.u > 8.7f ? PinkLight : Pink);
                }

            var face = p.eyeOpen < 0.5f ? Face.Sleepy : p.face;
            // 두 눈(3/4): 뒤 눈은 얼굴 중앙, 앞 눈은 주둥이 위 앞쪽
            var e1 = Rot(-R * 0.08f, R * 0.08f);
            var e2 = Rot(R * 0.62f, R * 0.08f);
            Eye(c, I(e1.x), I(e1.y), face, EyeColor(id), false);
            Eye(c, I(e2.x), I(e2.y), face, EyeColor(id), true);

            // 볼 홍조(눈 아래 양쪽)
            var b1 = Rot(-R * 0.22f, -R * 0.3f); var b2 = Rot(R * 0.78f, -R * 0.3f);
            var blush = new Color32(240, 130, 140, 110);
            c.Blend(I(b1.x) - 1, I(b1.y), blush); c.Blend(I(b1.x), I(b1.y), blush); c.Blend(I(b1.x) + 1, I(b1.y), blush);
            c.Blend(I(b2.x), I(b2.y), blush); c.Blend(I(b2.x) + 1, I(b2.y), blush);

            // 코(분홍 역삼각) + ω 입
            var n = Rot(R * 0.42f, -R * 0.24f);
            int nx = I(n.x), ny = I(n.y);
            c.Set(nx - 1, ny, Pink); c.Set(nx, ny, Pink); c.Set(nx + 1, ny, Pink); c.Set(nx, ny - 1, Pink);
            if (p.mouthOpen)
            {
                // 벌린 입(하악·야옹): 어두운 입속 + 송곳니
                for (int dy = 2; dy <= 4; dy++) for (int dx = -2; dx <= 2; dx++)
                        if (Mathf.Abs(dx) + (dy == 4 ? 1 : 0) <= 2) c.Set(nx + dx, ny - dy, dy == 4 && dx == 0 ? Pink : EyeDark);
                c.Set(nx - 1, ny - 2, Shine); c.Set(nx + 1, ny - 2, Shine);
            }
            else
            {
                c.Set(nx, ny - 2, EyeDark);
                c.Set(nx - 1, ny - 3, EyeDark); c.Set(nx - 2, ny - 2, EyeDark);
                c.Set(nx + 1, ny - 3, EyeDark); c.Set(nx + 2, ny - 2, EyeDark);
                if (p.tongue) { c.Set(nx, ny - 3, Pink); c.Set(nx, ny - 4, PinkLight); c.Set(nx + 1, ny - 4, Pink); }
            }
            // 수염: 양쪽 볼에서 바깥으로
            var wc = new Color32(255, 250, 240, 150);
            for (int i = 0; i < 4; i++)
            {
                c.Blend(nx + 5 + i, ny - 2 + i / 3, wc); c.Blend(nx + 5 + i, ny - 4 - i / 3, wc);
                c.Blend(nx - 7 - i, ny - 2 + i / 3, new Color32(255, 250, 240, 90));
            }
        }

        // 눈 4×5 큰 눈(반짝임 2점). small=앞 눈(약간 좁게). 표정별 모양.
        static void Eye(PixelCanvas c, int x, int y, Face f, Color32 iris, bool small)
        {
            switch (f)
            {
                case Face.Sleepy:   // ‿ 감은 눈(편안)
                    c.Set(x - 1, y, EyeDark); c.Set(x, y - 1, EyeDark); c.Set(x + 1, y - 1, EyeDark); if (!small) c.Set(x + 2, y, EyeDark);
                    return;
                case Face.Happy:    // ^ 웃는 눈
                    c.Set(x - 1, y - 1, EyeDark); c.Set(x, y, EyeDark); c.Set(x + 1, y, EyeDark); if (!small) c.Set(x + 2, y - 1, EyeDark);
                    else c.Set(x + 2, y - 1, EyeDark);
                    return;
                case Face.Angry:    // 뜬 눈 + 위 눈꺼풀을 사선으로 깎고 V자 눈썹
                {
                    int ww = small ? 4 : 5, hh = 5, tp = y + 1, lf = x - 1;
                    for (int r = 0; r < hh; r++)
                        for (int col = 0; col < ww; col++)
                        {
                            // 눈꺼풀 사선: 뒤 눈은 앞쪽(오른쪽)이 낮게, 앞 눈은 뒤쪽(왼쪽)이 낮게
                            int cut = small ? (ww - 1 - col) / 2 : col / 2;
                            if (r < cut) continue;
                            if ((r == hh - 1) && (col == 0 || col == ww - 1)) continue;
                            Color32 px = (col > 0 && col < ww - 1 && r >= 2) ? iris : EyeDark;
                            if (col == ww / 2 && r >= 2) px = EyeDark;      // 세로 동공
                            c.Set(lf + col, tp - r, px);
                        }
                    for (int col = -1; col <= ww; col++)
                    {
                        int drop = small ? (ww - col) / 2 : (col + 1) / 2;
                        c.Set(lf + col, tp + 2 - drop, EyeDark);
                    }
                    return;
                }
            }
            int w = small ? 4 : 5, h = f == Face.Wide ? 7 : 6;
            int top = y + 2, left = x - 1;
            for (int r = 0; r < h; r++)
                for (int col = 0; col < w; col++)
                {
                    bool corner = (r == 0 || r == h - 1) && (col == 0 || col == w - 1);
                    if (corner) continue;
                    Color32 px = EyeDark;
                    if (r >= h - 2 && col > 0 && col < w - 1) px = iris;                 // 아래쪽 홍채 빛
                    if (f == Face.Wide && r >= 1 && r <= h - 2 && col > 0 && col < w - 1) px = iris;
                    c.Set(left + col, top - r, px);
                }
            c.Set(left + 1, top - 1, Shine); c.Set(left + 2, top - 1, Shine);
            c.Set(left + 1, top - 2, Shine); if (!small) c.Set(left + 2, top - 2, Shine);
            if (!small) c.Set(left + w - 2, top - h + 2, new Color32(255, 252, 245, 200));
        }

        static void DrawBowl(PixelCanvas c, Pose p)
        {
            int bx = Mathf.RoundToInt(p.headX + p.headR * 0.5f);
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
            => new Leg { jx = jx, jy = jy, kx = px - 2.6f, ky = py + 4.4f, px = px, py = py, far = far, show = true, knee = true, r = 2.6f };

        // 귀여운 비율: 큰 머리(반지름 12), 짧은 몸통·짧은 다리, 동그란 엉덩이
        public static Pose Stand()
        {
            var p = new Pose
            {
                bodyX = 30, bodyY = 15.5f, bodyRx = 12.5f, bodyRy = 7f,
                chestX = 37.5f, chestY = 16.5f, chestR = 7.5f,
                hipX = 22, hipY = 16.5f, hipR = 7.8f,
                headX = 45, headY = 28, headR = 12,
                tailX = 17, tailY = 20, tailAng = 2.3f, tailCurl = -0.08f, tailLen = 17, tailW = 2.7f,
                shadowRx = 15,
            };
            p.fn = LegOf(40, 12, 41, Ground, false, 2.8f); p.ff = LegOf(36, 12, 36, Ground, true, 2.8f);
            p.bn = Hind(22, 14, 21.5f, Ground, false); p.bf = Hind(25.5f, 14, 25.5f, Ground, true);
            return p;
        }

        public static Pose Sit()
        {
            var p = new Pose
            {
                bodyX = 30, bodyY = 15, bodyRx = 8.5f, bodyRy = 11f, bodyAng = -0.4f,
                chestX = 35, chestY = 17, chestR = 7.5f,
                hipX = 26, hipY = 11, hipR = 9.5f,
                headX = 38, headY = 31, headR = 12,
                tailX = 18, tailY = 6, tailAng = -0.15f, tailCurl = 0.06f, tailLen = 22, tailW = 2.6f, tailFront = true,
                shadowRx = 14,
            };
            p.fn = LegOf(37, 14, 38, Ground, false, 2.6f); p.ff = LegOf(33.5f, 14, 34, Ground, true, 2.6f);
            p.bn = new Leg { jx = 28, jy = 8, px = 31, py = Ground, r = 2.9f, show = true };   // 접힌 뒷발(발만 보임)
            p.bf = default;
            return p;
        }

        public static Pose Loaf()
        {
            var p = new Pose
            {
                bodyX = 30, bodyY = 10, bodyRx = 15.5f, bodyRy = 9f, bodyAng = 0.03f,
                chestX = 39, chestY = 11, chestR = 8,
                hipX = 20, hipY = 10.5f, hipR = 8.5f,
                headX = 44, headY = 20, headR = 12,
                tailX = 15, tailY = 6, tailAng = -0.2f, tailCurl = 0.035f, tailLen = 25, tailW = 2.6f, tailFront = true,
                shadowRx = 18,
            };
            p.fn = new Leg { jx = 46, jy = 7, px = 50, py = Ground, r = 2.5f, show = true };   // 앞발 끝만 살짝
            return p;
        }

        // 둥글게 말아 자기: 얼굴은 몸 위에 얹고 꼬리로 감싼다(코·입이 땅에 묻히지 않게 고개는 조금만 숙임)
        public static Pose Curl()
        {
            var p = new Pose
            {
                bodyX = 29, bodyY = 12, bodyRx = 14, bodyRy = 10.5f, bodyAng = 0.12f,
                chestX = 35, chestY = 11, chestR = 8,
                hipX = 22, hipY = 13, hipR = 10,
                headX = 41, headY = 15, headR = 10.5f, headTilt = 0.32f, earFold = 0.35f, eyeOpen = 0,
                tailX = 16, tailY = 5.5f, tailAng = -0.1f, tailCurl = 0.1f, tailLen = 31, tailW = 3.2f, tailFront = true,
                shadowRx = 17,
            };
            return p;
        }

        // 배 보이며 뒹굴기(애교): 옆으로 누워 네 발을 들고 머리를 뒤로 젖힘
        public static Pose BellyUp()
        {
            var p = new Pose
            {
                bodyX = 30, bodyY = 10, bodyRx = 15, bodyRy = 7.5f, bodyAng = 0.05f,
                chestX = 39, chestY = 10.5f, chestR = 7,
                hipX = 21, hipY = 10, hipR = 8,
                headX = 47, headY = 14, headR = 11.5f, headTilt = -0.55f, face = Face.Happy,
                tailX = 17, tailY = 8, tailAng = 3.0f, tailCurl = 0.07f, tailLen = 14, tailW = 2.6f,
                shadowRx = 18,
            };
            p.fn = new Leg { jx = 39, jy = 13, px = 42, py = 22, r = 2.5f, show = true };
            p.ff = new Leg { jx = 36, jy = 13, px = 37, py = 22, r = 2.5f, show = true, far = true };
            p.bn = new Leg { jx = 23, jy = 13, px = 25, py = 22, r = 2.7f, show = true };
            p.bf = new Leg { jx = 20, jy = 13, px = 19, py = 21, r = 2.7f, show = true, far = true };
            return p;
        }

        // 하악(위협): 등을 둥글게 세우고 털을 부풀림, 귀는 뒤로, 꼬리는 빳빳이
        public static Pose Hiss()
        {
            var p = Stand();
            p.bodyY = 19.5f; p.bodyRy = 8.5f; p.bodyRx = 12f; p.bodyAng = 0.05f;
            p.hipY = 18; p.hipR = 8.6f; p.chestY = 17; p.chestR = 8f;
            p.headX = 46; p.headY = 24; p.headTilt = 0.12f;
            p.earFold = 0.9f; p.face = Face.Angry; p.mouthOpen = true;
            p.tailX = 16; p.tailY = 22; p.tailAng = 1.75f; p.tailCurl = -0.02f; p.tailW = 4.0f; p.tailLen = 18;
            p.fn.jy = 15; p.ff.jy = 15; p.bn.jy = 17; p.bf.jy = 17;
            p.bn.ky += 2; p.bf.ky += 2;
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
                show = t < 0.5f ? x.show : y.show, over = t < 0.5f ? x.over : y.over
            };
            var p = new Pose
            {
                bodyX = F(a.bodyX, b.bodyX), bodyY = F(a.bodyY, b.bodyY), bodyRx = F(a.bodyRx, b.bodyRx), bodyRy = F(a.bodyRy, b.bodyRy), bodyAng = F(a.bodyAng, b.bodyAng),
                chestX = F(a.chestX, b.chestX), chestY = F(a.chestY, b.chestY), chestR = F(a.chestR, b.chestR),
                hipX = F(a.hipX, b.hipX), hipY = F(a.hipY, b.hipY), hipR = F(a.hipR, b.hipR),
                headX = F(a.headX, b.headX), headY = F(a.headY, b.headY), headR = F(a.headR, b.headR), headTilt = F(a.headTilt, b.headTilt),
                earFold = F(a.earFold, b.earFold), eyeOpen = F(a.eyeOpen, b.eyeOpen), tongue = t < 0.5f ? a.tongue : b.tongue,
                face = t < 0.5f ? a.face : b.face, mouthOpen = t < 0.5f ? a.mouthOpen : b.mouthOpen,
                fn = LL(a.fn, b.fn), ff = LL(a.ff, b.ff), bn = LL(a.bn, b.bn), bf = LL(a.bf, b.bf),
                tailX = F(a.tailX, b.tailX), tailY = F(a.tailY, b.tailY), tailAng = F(a.tailAng, b.tailAng), tailCurl = F(a.tailCurl, b.tailCurl),
                tailLen = F(a.tailLen, b.tailLen), tailW = F(a.tailW, b.tailW), tailFront = t < 0.5f ? a.tailFront : b.tailFront,
                shadowRx = F(a.shadowRx, b.shadowRx), bowl = t < 0.5f ? a.bowl : b.bowl,
            };
            return p;
        }
    }
}
