using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YeogiCafe.Art
{
    // 2D 조명용 노멀맵을 그림에서 자동으로 만든다(따로 그릴 필요 없음).
    // 높이 = 가장자리에서 안쪽으로 둥글게 솟는 '부풀림' + 밝기(밝은 털이 살짝 앞으로) → 기울기로 노멀 계산.
    // URP 2D 조명이 켜져 있을 때만 쓰인다(꺼져 있으면 노멀맵 없이 예전과 똑같이).
    public static class LitSprites
    {
        public const string NormalMapName = "_NormalMap";
        public static bool Enabled => GraphicsSettings.currentRenderPipeline != null;

        static readonly Dictionary<Texture2D, Texture2D> normals = new();
        static readonly Dictionary<int, Sprite> litCache = new();
        static readonly Dictionary<Texture2D, Color32[]> pixels = new();
        static readonly HashSet<int> flat = new();     // 꽉 찬 타일(바닥·벽지 등): 노멀맵 없이 평평하게

        static Color32[] Pixels(Texture2D tex)
        {
            if (pixels.TryGetValue(tex, out var p)) return p;
            p = ReadPixels(tex); pixels[tex] = p; return p;
        }

        // 스프라이트 영역에 투명 픽셀이 있나? 없으면 타일 → 노멀맵을 붙이면 이음매마다 테두리 음영이 생겨 줄무늬가 된다
        public static bool HasTransparency(Texture2D tex, Rect r)
        {
            var px = Pixels(tex); if (px == null) return true;
            int W = tex.width;
            int x0 = Mathf.Max(0, (int)r.x), y0 = Mathf.Max(0, (int)r.y);
            int x1 = Mathf.Min(W, (int)(r.x + r.width)), y1 = Mathf.Min(tex.height, (int)(r.y + r.height));
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    if (px[y * W + x].a <= 8) return true;
            return false;
        }

        // 텍스처 전체의 노멀맵(시트면 칸 사이 투명 띠 덕분에 칸끼리 섞이지 않음)
        public static Texture2D NormalFor(Texture2D src, float bevelPx = 3f, float lumaWeight = 0.35f)
        {
            if (src == null) return null;
            if (normals.TryGetValue(src, out var n) && n != null) return n;
            Color32[] px = Pixels(src);
            if (px == null) return null;
            int W = src.width, H = src.height;
            // 1) 투명까지의 거리(맨해튼, 2패스) → 부풀림
            var dist = new float[W * H];
            const float INF = 9999f;
            for (int i = 0; i < px.Length; i++) dist[i] = px[i].a > 8 ? INF : 0f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x; if (dist[i] == 0f) continue;
                    float d = dist[i];
                    if (x > 0) d = Mathf.Min(d, dist[i - 1] + 1f); else d = Mathf.Min(d, 1f);
                    if (y > 0) d = Mathf.Min(d, dist[i - W] + 1f); else d = Mathf.Min(d, 1f);
                    dist[i] = d;
                }
            for (int y = H - 1; y >= 0; y--)
                for (int x = W - 1; x >= 0; x--)
                {
                    int i = y * W + x; if (dist[i] == 0f) continue;
                    float d = dist[i];
                    if (x < W - 1) d = Mathf.Min(d, dist[i + 1] + 1f); else d = Mathf.Min(d, 1f);
                    if (y < H - 1) d = Mathf.Min(d, dist[i + W] + 1f); else d = Mathf.Min(d, 1f);
                    dist[i] = d;
                }
            var h = new float[W * H];
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a <= 8) { h[i] = 0f; continue; }
                float t = Mathf.Clamp01(dist[i] / bevelPx);
                float bulge = Mathf.Sqrt(1f - (1f - t) * (1f - t));          // 가장자리에서 둥글게
                float luma = (px[i].r * 0.299f + px[i].g * 0.587f + px[i].b * 0.114f) / 255f;
                h[i] = bulge * (1f - lumaWeight) + luma * lumaWeight * bulge;
            }
            // 2) 소벨 기울기 → 노멀(접선 공간, 0.5 중심 인코딩)
            var outPx = new Color32[W * H];
            const float strength = 2.2f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (px[i].a <= 8) { outPx[i] = new Color32(128, 128, 255, 0); continue; }
                    float H_(int xx, int yy) { xx = Mathf.Clamp(xx, 0, W - 1); yy = Mathf.Clamp(yy, 0, H - 1); return h[yy * W + xx]; }
                    float dx = (H_(x + 1, y - 1) + 2f * H_(x + 1, y) + H_(x + 1, y + 1)) - (H_(x - 1, y - 1) + 2f * H_(x - 1, y) + H_(x - 1, y + 1));
                    float dy = (H_(x - 1, y + 1) + 2f * H_(x, y + 1) + H_(x + 1, y + 1)) - (H_(x - 1, y - 1) + 2f * H_(x, y - 1) + H_(x + 1, y - 1));
                    var nrm = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                    outPx[i] = new Color32((byte)((nrm.x * 0.5f + 0.5f) * 255f), (byte)((nrm.y * 0.5f + 0.5f) * 255f), (byte)((nrm.z * 0.5f + 0.5f) * 255f), 255);
                }
            n = new Texture2D(W, H, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = src.name + "_n" };
            n.SetPixels32(outPx); n.Apply(false, false);
            normals[src] = n;
            return n;
        }

        // 읽기 불가 텍스처(임포트된 PNG)도 GPU로 복사해서 읽는다
        static Color32[] ReadPixels(Texture2D src)
        {
            if (src.isReadable) { try { return src.GetPixels32(); } catch { } }
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); copy.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            var px = copy.GetPixels32(); Object.Destroy(copy);
            return px;
        }

        // 노멀맵을 붙인 스프라이트 생성(조명이 꺼져 있으면 보통 스프라이트)
        public static Sprite Create(Texture2D tex, Rect rect, Vector2 pivot, float ppu)
        {
            if (!Enabled || !HasTransparency(tex, rect)) return Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
            var n = NormalFor(tex);
            if (n == null) return Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
            return Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect, Vector4.zero, false,
                                 new[] { new SecondarySpriteTexture { name = NormalMapName, texture = n } });
        }

        // 이미 있는 스프라이트(씬에 놓인 임포트 아트)를 노멀맵 붙은 복제로 바꿔 끼운다
        public static void Upgrade(SpriteRenderer sr)
        {
            if (!Enabled || sr == null || sr.sprite == null) return;
            var s = sr.sprite;
            if (s.GetSecondaryTextureCount() > 0) return;
            int id = s.GetInstanceID();
            if (flat.Contains(id)) return;
            if (s.texture != null && !HasTransparency(s.texture, s.rect)) { flat.Add(id); return; }
            if (!litCache.TryGetValue(id, out var lit) || lit == null)
            {
                var tex = s.texture; if (tex == null) return;
                var n = NormalFor(tex); if (n == null) return;
                var pivot = new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height);
                lit = Sprite.Create(tex, s.rect, pivot, s.pixelsPerUnit, 0, SpriteMeshType.FullRect, s.border, false,
                                    new[] { new SecondarySpriteTexture { name = NormalMapName, texture = n } });
                lit.name = s.name;
                litCache[id] = lit;
                litCache[lit.GetInstanceID()] = lit;
            }
            sr.sprite = lit;
        }
    }
}
