using UnityEngine;
using YeogiCafe.Art;

namespace YeogiCafe.Visual
{
    // 벽 액자 가구에 건 사진. 사진이 걸리면 원래 그림(작은 풍경화) 대신 가로로 긴 사진 액자를 그린다.
    // 사진은 세상 도트의 2배 밀도로 인화(인쇄된 사진 느낌), 테두리는 세상과 같은 도트 크기.
    public class PhotoFrameView : MonoBehaviour
    {
        const int Scale = 2;                  // 텍스처 px / 세상 px
        const int W = 44, H = 28, Border = 4; // 세상 px(PPU 32 기준 1.375×0.875 유닛)
        SpriteRenderer baseSr, sr;
        Texture2D tex;
        public string Shown { get; private set; }

        public void Show(Texture2D photo, string file)
        {
            if (photo == null) { Clear(); return; }
            if (Shown == file && sr != null) return;
            Shown = file;
            baseSr = GetComponent<SpriteRenderer>();
            if (baseSr != null) baseSr.enabled = false;
            if (sr == null)
            {
                var go = new GameObject("Photo");
                go.transform.SetParent(transform, false);
                sr = go.AddComponent<SpriteRenderer>();
            }
            sr.sortingOrder = (baseSr != null ? baseSr.sortingOrder : YSort.WallProps) + 1;
            if (tex != null) Destroy(tex);
            tex = Build(photo);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32 * Scale);
        }

        public void Clear()
        {
            Shown = null;
            if (sr != null) { Destroy(sr.gameObject); sr = null; }
            if (tex != null) { Destroy(tex); tex = null; }
            if (baseSr == null) baseSr = GetComponent<SpriteRenderer>();
            if (baseSr != null) baseSr.enabled = true;
        }

        static Texture2D Build(Texture2D photo)
        {
            int tw = W * Scale, th = H * Scale, b = Border * Scale;
            var px = new Color32[tw * th];
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    int wx = x / Scale, wy = y / Scale;                       // 세상 도트 단위
                    Color32 c;
                    bool edge = wx == 0 || wy == 0 || wx == W - 1 || wy == H - 1;
                    bool inner = wx == Border - 1 || wy == Border - 1 || wx == W - Border || wy == H - Border;
                    if (edge || inner) c = PixelPalette.Outline;
                    else if (x < b || y < b || x >= tw - b || y >= th - b)
                        c = (wy == H - 2 || wx == 1) ? PixelPalette.WoodLight : (wy == 1 || wx == W - 2) ? PixelPalette.WoodDark : PixelPalette.WoodMid;
                    else
                    {
                        // 사진 비율 유지(남는 곳은 크림색 매트)
                        float iw = tw - 2 * b, ih = th - 2 * b;
                        float k = Mathf.Min(iw / photo.width, ih / photo.height);
                        float pw = photo.width * k, ph = photo.height * k;
                        float u = (x - b + 0.5f - (iw - pw) * 0.5f) / pw, v = (y - b + 0.5f - (ih - ph) * 0.5f) / ph;
                        if (u < 0f || u >= 1f || v < 0f || v >= 1f) c = PixelPalette.CreamLight;
                        else c = photo.GetPixel((int)(u * photo.width), (int)(v * photo.height));
                    }
                    px[y * tw + x] = c;
                }
            var t = new Texture2D(tw, th, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px); t.Apply();
            return t;
        }

        void OnDestroy() { if (tex != null) Destroy(tex); }
    }
}
