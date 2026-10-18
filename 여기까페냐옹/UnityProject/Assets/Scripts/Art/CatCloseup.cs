using UnityEngine;

namespace YeogiCafe.Art
{
    // 마감 직전 '마지막 손님 놀아주기'용 고해상도 클로즈업(정면 식빵 자세, 168×128).
    // 털색·무늬는 CatRig와 같은 함수(CoatTone)를 써서 홀의 그 고양이와 똑같아 보인다.
    // 표정(눈·귀·입)·고개 기울기·꼬리 끝만 바꿔 그린 프레임을 캐시해 UI에서 갈아 끼운다.
    public static class CatCloseup
    {
        public const int W = 168, H = 128, Ground = 8;

        public enum Eyes : byte { Open, Half, Closed, Happy, Angry, Wide }
        public struct State
        {
            public Eyes eyes; public bool earsFlat, mouthOpen; public int lean; public int tail;
            public string Key => $"{(int)eyes}{(earsFlat ? 1 : 0)}{(mouthOpen ? 1 : 0)}{lean + 1}{tail}";
        }

        // 클릭 판정용 부위(UI가 캔버스 좌표로 물어봄)
        public enum Zone { None, Head, Cheek, Chin, Back, Paw }
        public static Zone ZoneAt(float x, float y, int lean = 0)
        {
            float hx = 84 + lean * 4, hy = 74;
            float dx = (x - hx) / 38f, dy = (y - hy) / 32f;
            if (dx * dx + dy * dy <= 1f || (y > 90 && Mathf.Abs(x - hx) < 44 && y < 124))
            {
                if (y >= 52 && y < 62 && Mathf.Abs(x - hx) < 14) return Zone.Chin;
                if (y < 74 && Mathf.Abs(x - hx) >= 16) return Zone.Cheek;
                if (y >= 82) return Zone.Head;
                return Zone.Chin;                                   // 얼굴 가운데 아래쪽(입 주변)은 턱 취급
            }
            if (y < 22 && ((x - 68) * (x - 68) / 150f + (y - 12) * (y - 12) / 60f <= 1f || (x - 100) * (x - 100) / 150f + (y - 12) * (y - 12) / 60f <= 1f))
                return Zone.Paw;
            float bx = (x - 84) / 60f, by = (y - 34) / 30f;
            if (bx * bx + by * by <= 1f && y >= Ground) return Zone.Back;
            return Zone.None;
        }

        struct Px { public byte part; public float z, light, u, v; }
        const byte None = 0;
        static Color32 C(int r, int g, int b, int a = 255) => new((byte)r, (byte)g, (byte)b, (byte)a);
        static readonly Color32 EyeDark = C(52, 40, 44), Shine = C(255, 252, 245), Pink = C(226, 140, 150), PinkLight = C(246, 186, 192);
        static readonly Vector3 L = new Vector3(-0.45f, 0.6f, 0.66f).normalized;

        static float Shade(float u, float v)
        {
            float w = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u - v * v));
            return Mathf.Clamp01(Vector3.Dot(new Vector3(u, v, w), L) * 0.85f + 0.32f);
        }

        static void Put(Px[] b, int x, int y, CatRig.CoatPart part, float z, float u, float v, float lightBias = 0f)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            ref var q = ref b[y * W + x];
            if (q.part != None && q.z > z) return;
            q.part = (byte)part; q.z = z; q.u = u; q.v = v; q.light = Shade(u, v) + lightBias;
        }

        static void Ellipse(Px[] b, float cx, float cy, float rx, float ry, float ang, float z, CatRig.CoatPart part, float bias = 0f)
        {
            float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
            int R = (int)Mathf.Ceil(Mathf.Max(rx, ry)) + 1;
            for (int y = (int)cy - R; y <= (int)cy + R; y++)
                for (int x = (int)cx - R; x <= (int)cx + R; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float lx = (dx * ca + dy * sa) / rx, ly = (-dx * sa + dy * ca) / ry;
                    if (lx * lx + ly * ly > 1f) continue;
                    Put(b, x, y, part, z + (1f - lx * lx - ly * ly) * 0.5f, lx * ca - ly * sa, lx * sa + ly * ca, bias);
                }
        }

        static void Capsule(Px[] b, Vector2 a, Vector2 c, float r0, float r1, float z, CatRig.CoatPart part, float v0, float v1)
        {
            float minx = Mathf.Min(a.x, c.x) - Mathf.Max(r0, r1) - 1, maxx = Mathf.Max(a.x, c.x) + Mathf.Max(r0, r1) + 1;
            float miny = Mathf.Min(a.y, c.y) - Mathf.Max(r0, r1) - 1, maxy = Mathf.Max(a.y, c.y) + Mathf.Max(r0, r1) + 1;
            var ab = c - a; float len2 = ab.sqrMagnitude + 1e-4f;
            for (int y = (int)miny; y <= (int)maxy; y++)
                for (int x = (int)minx; x <= (int)maxx; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f) - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p, ab) / len2);
                    var q = p - ab * t; float r = Mathf.Lerp(r0, r1, t);
                    if (q.sqrMagnitude > r * r) continue;
                    var n = new Vector2(-ab.y, ab.x).normalized;
                    float s = Vector2.Dot(q, n) / r;
                    float zz = z + (1f - s * s) * 0.3f;
                    Put(b, x, y, part, zz, s * n.x * 0.9f, s * n.y * 0.9f + 0.1f);
                    ref var got = ref b[y * W + x];
                    if (got.part == (byte)part && got.z == zz) got.v = Mathf.Lerp(v0, v1, t);   // 꼬리 무늬용 길이 좌표
                }
        }

        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float s2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float s3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = s1 < 0 || s2 < 0 || s3 < 0, pos = s1 > 0 || s2 > 0 || s3 > 0;
            return !(neg && pos);
        }

        public static PixelCanvas Draw(string id, State s)
        {
            var buf = new Px[W * H];
            var c = new PixelCanvas(W, H);
            float hx = 84 + s.lean * 4f, hy = 74f, tilt = -s.lean * 0.07f;

            // 그림자(카운터 위)
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x - 84f) / 70f, dy = (y - Ground) / 5f;
                    if (dx * dx + dy * dy <= 1f) c.Set(x, y, C(74, 54, 47, 60));
                }
            var shadow = (Color32[])c.Pixels.Clone();

            // 몸(식빵) → 가슴 → 꼬리(앞으로 감쌈) → 앞발 → 머리
            Ellipse(buf, 84, 34, 60, 30, 0, 1f, CatRig.CoatPart.Body);
            Ellipse(buf, 84, 38, 31, 26, 0, 2f, CatRig.CoatPart.Chest, 0.04f);
            {
                float lift = s.tail * 4f;
                var pts = new[] { new Vector2(142, 24), new Vector2(136, 13), new Vector2(120, 9.5f), new Vector2(104, 10), new Vector2(94, 12 + lift * 0.5f), new Vector2(88, 15 + lift) };
                for (int i = 0; i < pts.Length - 1; i++)
                    Capsule(buf, pts[i], pts[i + 1], 6.5f - i * 0.5f, 6.5f - (i + 1) * 0.5f, 3.2f + i * 0.01f, CatRig.CoatPart.Tail, 1f - i / 5f, 1f - (i + 1) / 5f);
            }
            Ellipse(buf, 68, 13, 12, 7.5f, 0.05f, 3.5f, CatRig.CoatPart.Leg, 0.05f);
            Ellipse(buf, 100, 13, 12, 7.5f, -0.05f, 3.5f, CatRig.CoatPart.Leg, 0.05f);

            // 귀(뒤) → 머리
            var head = new Vector2(hx, hy);
            Vector2 Rot(float dx, float dy) { float ca = Mathf.Cos(tilt), sa = Mathf.Sin(tilt); return head + new Vector2(dx * ca - dy * sa, dx * sa + dy * ca); }
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 a = Rot(side * 31, 16), b = Rot(side * 8, 27);
                Vector2 tip = s.earsFlat ? Rot(side * 46, 26) : Rot(side * 31, 49);
                Vector2 cen = (a + b + tip) / 3f;
                Vector2 ia = Vector2.Lerp(a, cen, 0.35f), ib = Vector2.Lerp(b, cen, 0.35f), it = Vector2.Lerp(tip, cen, 0.3f);
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        if (!InTri(p, a, b, tip)) continue;
                        bool inner = InTri(p, ia, ib, it);
                        Put(buf, x, y, CatRig.CoatPart.Ear, 4.5f, side * 0.35f, 0.5f);
                        if (inner) buf[y * W + x].u = 9f;                    // 안쪽 분홍 표시
                    }
            }
            Ellipse(buf, hx, hy, 38, 32, tilt, 5f, CatRig.CoatPart.Head);
            Ellipse(buf, Rot(0, -12).x, Rot(0, -12).y, 14, 9, tilt, 5.6f, CatRig.CoatPart.Muzzle, 0.06f);

            // 땅 아래 자르기
            for (int y = 0; y < Ground; y++) for (int x = 0; x < W; x++) buf[y * W + x].part = None;

            // 색칠
            bool tabby = CatRig.IsTabby(id);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    var q = buf[y * W + x];
                    if (q.part == None) continue;
                    var part = (CatRig.CoatPart)q.part;
                    if (part == CatRig.CoatPart.Ear && q.u > 8f) { c.Set(x, y, PinkLight); continue; }
                    float mx, my, u = q.u, v = q.v;
                    switch (part)
                    {
                        case CatRig.CoatPart.Head: case CatRig.CoatPart.Muzzle: case CatRig.CoatPart.Ear:
                            mx = (x - hx) / 2.4f + 40f; my = (y - hy) / 2.4f + 30f; u = q.u + 0.32f; break;   // 정면 얼굴 → 리그(3/4) 무늬 기준으로 옮김
                        case CatRig.CoatPart.Leg: mx = (x - 84) / 2.2f + 30f; my = 8f; v = -0.6f; break;
                        case CatRig.CoatPart.Chest: mx = (x - 84) / 2.2f + 30f; my = (y - 34) / 2.2f + 15f; u = q.u * 0.4f + 0.2f; break;
                        case CatRig.CoatPart.Tail: mx = 12f + q.v * 9f; my = 24f; break;
                        default: mx = (x - 84) / 2.2f + 30f; my = (y - 34) / 2.2f + 15f; break;
                    }
                    float li = q.light;
                    if (tabby && (part == CatRig.CoatPart.Body || part == CatRig.CoatPart.Tail))
                    {
                        float sn = Mathf.Sin(x * 0.36f + q.v * 2.2f);
                        if (sn > 0.6f && q.v > -0.5f) li -= 0.28f;
                    }
                    if (tabby && part == CatRig.CoatPart.Head && q.v > 0.42f && Mathf.Abs(q.u) < 0.42f && Mathf.Sin(q.u * 11f + 1.57f) > 0.55f) li -= 0.28f;
                    if (tabby && part == CatRig.CoatPart.Head && Mathf.Abs(q.u) > 0.55f && q.v > -0.3f && q.v < 0.2f && Mathf.Sin(q.v * 14f) > 0.5f) li -= 0.22f;   // 볼 줄무늬
                    c.Set(x, y, CatRig.CoatTone(id, part, mx, my, u, v, li));
                }

            // 앞쪽 부위 경계선(머리-몸, 앞발-몸, 꼬리-몸)
            var snap = (Color32[])c.Pixels.Clone();
            for (int y = 1; y < H - 1; y++)
                for (int x = 1; x < W - 1; x++)
                {
                    var q = buf[y * W + x];
                    if (q.part == None) continue;
                    bool front = q.part == (byte)CatRig.CoatPart.Head || q.part == (byte)CatRig.CoatPart.Leg || q.part == (byte)CatRig.CoatPart.Tail;
                    if (!front) continue;
                    if (Edge(buf, x + 1, y, q) || Edge(buf, x - 1, y, q) || Edge(buf, x, y + 1, q) || Edge(buf, x, y - 1, q))
                        c.Set(x, y, Mul(snap[y * W + x], 0.78f));
                }

            // 앞발 발가락 홈
            foreach (var px in new[] { 68f, 100f })
                for (int k = -1; k <= 1; k++)
                {
                    int gx = Mathf.RoundToInt(px + k * 4.5f);
                    for (int gy = 13; gy <= 19; gy++) { var p = c.Get(gx, gy); if (p.a == 255 && buf[gy * W + gx].part == (byte)CatRig.CoatPart.Leg) c.Set(gx, gy, Mul(p, 0.8f)); }
                }

            DrawFace(c, id, s, Rot);

            for (int y = 0; y < Ground; y++) for (int x = 0; x < W; x++) if (c.Pixels[y * W + x].a == 255) c.Pixels[y * W + x] = shadow[y * W + x];

            // 외곽선
            snap = (Color32[])c.Pixels.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (snap[y * W + x].a == 255) continue;
                    Color32? nb = null;
                    if (x + 1 < W && snap[y * W + x + 1].a == 255) nb = snap[y * W + x + 1];
                    else if (x > 0 && snap[y * W + x - 1].a == 255) nb = snap[y * W + x - 1];
                    else if (y + 1 < H && snap[(y + 1) * W + x].a == 255) nb = snap[(y + 1) * W + x];
                    else if (y > 0 && snap[(y - 1) * W + x].a == 255) nb = snap[(y - 1) * W + x];
                    if (nb.HasValue) c.Set(x, y, C((int)(nb.Value.r * 0.32f + 50), (int)(nb.Value.g * 0.28f + 34), (int)(nb.Value.b * 0.28f + 30)));
                }
            return c;
        }

        static bool Edge(Px[] b, int x, int y, Px q)
        {
            var n = b[y * W + x];
            return n.part != None && n.part != q.part && q.z > n.z + 0.9f;
        }

        static Color32 Mul(Color32 c, float k) => new((byte)(c.r * k), (byte)(c.g * k), (byte)(c.b * k), 255);

        static void DrawFace(PixelCanvas c, string id, State s, System.Func<float, float, Vector2> Rot)
        {
            int I(float f) => Mathf.RoundToInt(f);
            var iris = CatRig.EyeOf(id);
            for (int side = -1; side <= 1; side += 2)
            {
                var e = Rot(side * 16f, 2f);
                // 달냥: 오드아이
                var col = id == "cat_moon" && side < 0 ? C(120, 176, 232) : iris;
                Eye(c, I(e.x), I(e.y), s.eyes, col, side);
            }
            // 볼 홍조
            for (int side = -1; side <= 1; side += 2)
            {
                var b = Rot(side * 24f, -10f);
                for (int y = -2; y <= 2; y++) for (int x = -5; x <= 5; x++)
                        if (x * x / 25f + y * y / 4f <= 1f) c.Blend(I(b.x) + x, I(b.y) + y, C(240, 130, 140, 90));
            }
            // 코
            var n = Rot(0, -7);
            int nx = I(n.x), ny = I(n.y);
            for (int r = 0; r < 4; r++) for (int k = -3 + r; k <= 3 - r; k++) c.Set(nx + k, ny - r, r == 0 && k < 0 ? PinkLight : Pink);
            // 입
            if (s.mouthOpen)
            {
                for (int y = -5; y <= 0; y++)
                    for (int x = -5; x <= 5; x++)
                        if (x * x / 25f + (y + 2.5f) * (y + 2.5f) / 7f <= 1f) c.Set(nx + x, ny - 6 + y, y <= -3 && Mathf.Abs(x) <= 2 ? Pink : EyeDark);
                c.Set(nx - 3, ny - 4, Shine); c.Set(nx + 3, ny - 4, Shine);
            }
            else
            {
                for (int y = 1; y <= 3; y++) c.Set(nx, ny - 3 - y, EyeDark);
                // ω
                for (int k = 1; k <= 4; k++)
                {
                    int dy = k <= 2 ? k : 5 - k;
                    c.Set(nx - k, ny - 6 - dy, EyeDark); c.Set(nx + k, ny - 6 - dy, EyeDark);
                }
                c.Set(nx - 5, ny - 6, EyeDark); c.Set(nx + 5, ny - 6, EyeDark);
            }
            // 수염(양옆 3가닥씩)
            for (int side = -1; side <= 1; side += 2)
                for (int w = 0; w < 3; w++)
                {
                    var a = Rot(side * 15f, -9f - w * 3f);
                    for (int k = 0; k < 16; k++)
                    {
                        float x = a.x + side * k, y = a.y + (w - 1) * k * 0.22f;
                        c.Blend(I(x), I(y), C(255, 250, 240, k < 10 ? 170 : 100));
                    }
                }
        }

        // 큰 눈(11×13): 표정별
        static void Eye(PixelCanvas c, int x, int y, Eyes f, Color32 iris, int side)
        {
            switch (f)
            {
                case Eyes.Closed:     // ‿ (편안히 감음)
                    for (int k = -6; k <= 6; k++) { int d = Mathf.Abs(k) >= 5 ? 1 : Mathf.Abs(k) <= 2 ? -1 : 0; c.Set(x + k, y - 1 + d, EyeDark); c.Set(x + k, y + d, EyeDark); }
                    return;
                case Eyes.Happy:      // ^ ^
                    for (int k = -6; k <= 6; k++) { int h = 5 - Mathf.Abs(k) * 5 / 6; c.Set(x + k, y + h - 2, EyeDark); c.Set(x + k, y + h - 3, EyeDark); }
                    return;
            }
            int w = 13, hgt = f == Eyes.Wide ? 17 : 15;
            int top = y + hgt / 2, left = x - w / 2;
            for (int r = 0; r < hgt; r++)
                for (int col = 0; col < w; col++)
                {
                    float dx = (col + 0.5f - w / 2f) / (w / 2f), dy = (r + 0.5f - hgt / 2f) / (hgt / 2f);
                    if (dx * dx + dy * dy > 1f) continue;
                    int py = top - r;
                    // 눈꺼풀: 반쯤(Half)은 위 절반, 화남(Angry)은 바깥쪽이 올라간 사선
                    if (f == Eyes.Half && r < hgt * 0.5f) { if (r >= hgt * 0.5f - 1.5f) c.Set(left + col, py, EyeDark); continue; }
                    if (f == Eyes.Angry)
                    {
                        float cut = (side < 0 ? col : (w - 1 - col)) * 0.6f + 3f;     // 코 쪽이 내려간 V자
                        if (r < cut) { if (r >= cut - 1.2f) c.Set(left + col, py, EyeDark); continue; }
                    }
                    bool rim = dx * dx + dy * dy > 0.62f;
                    Color32 px = rim ? EyeDark : iris;
                    float pupilW = f == Eyes.Wide ? 0.55f : 0.22f;
                    if (Mathf.Abs(dx) < pupilW && !rim) px = EyeDark;
                    if (!rim && dy > 0.35f && Mathf.Abs(dx) >= pupilW) px = C(Mathf.Min(255, iris.r + 28), Mathf.Min(255, iris.g + 28), Mathf.Min(255, iris.b + 22));
                    c.Set(left + col, py, px);
                }
            if (f != Eyes.Half && f != Eyes.Angry)
            {
                for (int a = 3; a <= 5; a++) for (int b = 3; b <= 5; b++) if (!(a == 5 && b == 5)) c.Set(left + a, top - b, Shine);
                c.Set(left + 8, top - 10, Shine); c.Set(left + 9, top - 10, Shine);
            }
        }
    }
}
