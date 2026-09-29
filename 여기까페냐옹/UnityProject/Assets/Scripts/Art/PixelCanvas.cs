using UnityEngine;

namespace YeogiCafe.Art
{
    // 공용 픽셀 드로잉 버퍼. 모든 스프라이트가 같은 프리미티브·같은 셰이딩 규칙을 쓰게 하는 '단일 붓'.
    // 순수 로직(Color32[])이라 에디터/런타임/테스트 모두에서 동작.
    public class PixelCanvas
    {
        public readonly int W, H;
        readonly Color32[] px;

        public PixelCanvas(int w, int h)
        {
            W = w; H = h;
            px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = PixelPalette.Transparent;
        }

        public Color32[] Pixels => px;
        public bool InBounds(int x, int y) => x >= 0 && x < W && y >= 0 && y < H;
        public Color32 Get(int x, int y) => InBounds(x, y) ? px[y * W + x] : PixelPalette.Transparent;

        public void Set(int x, int y, Color32 c)
        {
            if (!InBounds(x, y)) return;
            px[y * W + x] = c;
        }

        // 알파 블렌드(그림자 등 반투명용)
        public void Blend(int x, int y, Color32 c)
        {
            if (!InBounds(x, y)) return;
            if (c.a == 255) { px[y * W + x] = c; return; }
            if (c.a == 0) return;
            var b = px[y * W + x];
            float a = c.a / 255f, ia = 1 - a;
            px[y * W + x] = new Color32(
                (byte)(c.r * a + b.r * ia),
                (byte)(c.g * a + b.g * ia),
                (byte)(c.b * a + b.b * ia),
                (byte)Mathf.Min(255, c.a + b.a * ia));
        }

        public void HLine(int x0, int x1, int y, Color32 c)
        { if (x0 > x1) (x0, x1) = (x1, x0); for (int x = x0; x <= x1; x++) Set(x, y, c); }

        public void VLine(int x, int y0, int y1, Color32 c)
        { if (y0 > y1) (y0, y1) = (y1, y0); for (int y = y0; y <= y1; y++) Set(x, y, c); }

        public void FillRect(int x0, int y0, int x1, int y1, Color32 c)
        { for (int y = y0; y <= y1; y++) HLine(x0, x1, y, c); }

        public void RectOutline(int x0, int y0, int x1, int y1, Color32 c)
        { HLine(x0, x1, y0, c); HLine(x0, x1, y1, c); VLine(x0, y0, y1, c); VLine(x1, y0, y1, c); }

        // 채워진 원(디스크) — 부드러운 몸통·머리·쿠션 등에.
        public void Disc(int cx, int cy, int r, Color32 c)
        {
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                    if (x * x + y * y <= r * r + r * 0.5f) Set(cx + x, cy + y, c);
        }

        // 좌우 대칭 복사(왼쪽 절반 → 오른쪽). 고양이·가구 대칭 실루엣용.
        public void MirrorLeftToRight()
        {
            int mid = W / 2;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < mid; x++)
                    Set(W - 1 - x, y, Get(x, y));
        }

        // 불투명 픽셀 둘레에 아웃라인 자동 부착(통일된 짙은 갈색). 도트 룩의 핵심 통일 요소.
        public void AutoOutline(Color32 outline)
        {
            var snapshot = (Color32[])px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (snapshot[y * W + x].a != 0) continue;         // 이미 채워진 곳 제외
                    if (HasOpaqueNeighbor(snapshot, x, y)) Set(x, y, outline);
                }
        }

        bool HasOpaqueNeighbor(Color32[] buf, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                    if (buf[ny * W + nx].a == 255) return true;       // 불투명 이웃
                }
            return false;
        }

        // 바닥 접지 그림자(타원형 반투명) — 모든 오브젝트 통일 요소.
        public void GroundShadow(int cx, int cy, int rx)
        {
            for (int x = -rx; x <= rx; x++)
            {
                float t = 1f - (x * (float)x) / (rx * rx);
                if (t <= 0) continue;
                int ry = Mathf.Max(1, Mathf.RoundToInt(rx * 0.32f * Mathf.Sqrt(t)));
                for (int y = -ry; y <= ry; y++) Blend(cx + x, cy + y, PixelPalette.Shadow);
            }
        }
    }
}
