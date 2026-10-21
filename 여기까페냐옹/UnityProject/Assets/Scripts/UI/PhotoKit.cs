using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YeogiCafe.UI
{
    // 포토 모드 도구: 사진 파일 저장/불러오기 + 필터(CPU, 찍은 한 장에만 적용) + 프레임 목록.
    public static class PhotoKit
    {
        // ── 파일 ──
        public static string Dir => Path.Combine(Application.persistentDataPath, "Photos");
        static readonly Dictionary<string, Texture2D> cache = new();

        public static string SavePng(byte[] png, string suffix = "")
        {
            Directory.CreateDirectory(Dir);
            string name = "photo_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + suffix + ".png";
            File.WriteAllBytes(Path.Combine(Dir, name), png);
            return name;
        }

        public static Texture2D Load(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (cache.TryGetValue(file, out var t) && t != null) return t;
            string p = Path.Combine(Dir, file);
            if (!File.Exists(p)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (!tex.LoadImage(File.ReadAllBytes(p))) { Object.Destroy(tex); return null; }
            tex.filterMode = FilterMode.Point;
            cache[file] = tex;
            return tex;
        }

        public static void Delete(string file)
        {
            if (cache.TryGetValue(file, out var t) && t != null) Object.Destroy(t);
            cache.Remove(file);
            try { File.Delete(Path.Combine(Dir, file)); } catch { }
        }

        // ── 프레임(사진 테두리) — 폴라로이드는 기본, 나머지는 골드로 산다 ──
        public struct FrameDef { public string id; public int price; }
        public static readonly FrameDef[] Frames =
        {
            new FrameDef { id = "polaroid", price = 0 },
            new FrameDef { id = "wood", price = 150 },
            new FrameDef { id = "postcard", price = 200 },
            new FrameDef { id = "film", price = 250 },
            new FrameDef { id = "gold", price = 500 },
        };

        // ── 필터 ──
        public static readonly string[] Filters = { "none", "warm", "sepia", "mono", "night", "film" };

        // 화면 캡처를 사진 크기로 줄인다(최근접 — 도트가 뭉개지지 않게)
        public static Texture2D Resize(Texture2D src, int w, int h)
        {
            var s = src.GetPixels32(); int sw = src.width, sh = src.height;
            var d = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int sy = Mathf.Min(sh - 1, (int)((y + 0.5f) * sh / h));
                for (int x = 0; x < w; x++)
                {
                    int sx = Mathf.Min(sw - 1, (int)((x + 0.5f) * sw / w));
                    d[y * w + x] = s[sy * sw + sx];
                }
            }
            var t = new Texture2D(w, h, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(d); t.Apply();
            return t;
        }

        public static Texture2D Apply(Texture2D src, string filter)
        {
            int w = src.width, h = src.height;
            var s = src.GetPixels32();
            var d = new Color32[s.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float r = s[i].r / 255f, g = s[i].g / 255f, b = s[i].b / 255f;
                    float lum = r * 0.299f + g * 0.587f + b * 0.114f;
                    float dx = (x / (float)w - 0.5f) * 2f, dy = (y / (float)h - 0.5f) * 2f;
                    float vig = 1f - Mathf.Clamp01((dx * dx + dy * dy - 0.45f) * 0.35f);   // 가장자리 살짝 어둡게
                    switch (filter)
                    {
                        case "warm":
                            r = r * 1.06f + 0.04f; g = g * 1.01f + 0.015f; b = b * 0.88f; break;
                        case "sepia":
                            r = lum * 1.07f + 0.08f; g = lum * 0.92f + 0.05f; b = lum * 0.72f + 0.02f;
                            r *= vig; g *= vig; b *= vig; break;
                        case "mono":
                            r = g = b = Mathf.Clamp01((lum - 0.5f) * 1.15f + 0.5f); break;
                        case "night":
                            r = r * 0.55f; g = g * 0.62f + 0.02f; b = b * 0.8f + 0.1f;
                            r *= vig; g *= vig; b *= vig; break;
                        case "film":
                        {
                            float k = 0.75f;                                  // 채도 낮춤
                            r = Mathf.Lerp(lum, r, k) * 1.02f + 0.03f; g = Mathf.Lerp(lum, g, k) + 0.02f; b = Mathf.Lerp(lum, b, k) * 0.95f + 0.04f;
                            float n = (Hash(x, y) - 0.5f) * 0.08f;            // 입자(같은 사진이면 늘 같은 무늬)
                            r = (r + n) * vig; g = (g + n) * vig; b = (b + n) * vig; break;
                        }
                    }
                    d[i] = new Color32((byte)(Mathf.Clamp01(r) * 255f), (byte)(Mathf.Clamp01(g) * 255f), (byte)(Mathf.Clamp01(b) * 255f), 255);
                }
            var t = new Texture2D(w, h, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(d); t.Apply();
            return t;
        }

        static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0xFFFF) / 65535f;
        }
    }
}
