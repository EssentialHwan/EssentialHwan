using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Core;

namespace YeogiCafe.Visual
{
    // 날씨 연출: 비 오는 날 창문 유리에 빗줄기, 흐린 날·비 오는 날 방 전체를 살짝 어둡게.
    public class WeatherView : MonoBehaviour
    {
        SpriteRenderer tint;
        readonly List<Transform> drops = new();
        static Sprite white;
        WeatherKind shown = (WeatherKind)(-1);

        static Sprite White()
        {
            if (white != null) return white;
            var t = new Texture2D(1, 1); t.SetPixel(0, 0, Color.white); t.Apply(); t.filterMode = FilterMode.Point;
            white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return white;
        }

        void Update()
        {
            if (Weather.Current != shown) Apply(Weather.Current);
            if (Weather.Current != WeatherKind.Rain) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < drops.Count; i++)
            {
                var d = drops[i];
                var p = d.position; p.y -= dt * (3.2f + (i % 3) * 0.6f); p.x -= dt * 0.35f;
                int w = i % CafeLayout.WindowX.Length;
                float wx = CafeLayout.WindowX[w], bottom = CafeLayout.WindowBottom + 0.22f, top = CafeLayout.WindowBottom + 1.38f;
                if (p.y < bottom) { p.y = top; p.x = wx + Random.Range(-0.8f, 0.8f); }
                if (p.x < wx - 0.85f) p.x = wx + 0.8f;
                d.position = p;
            }
        }

        void Apply(WeatherKind k)
        {
            shown = k;
            if (tint == null)
            {
                var go = new GameObject("WeatherTint");
                go.transform.SetParent(transform, false);
                tint = go.AddComponent<SpriteRenderer>(); tint.sprite = White();
                go.transform.position = new Vector3(CafeLayout.CameraPos.x, 4f, 0f);
                go.transform.localScale = new Vector3(40f, 20f, 1f);
                tint.sortingOrder = 30000;
            }
            tint.color = k == WeatherKind.Rain ? new Color(0.25f, 0.3f, 0.45f, 0.13f)
                       : k == WeatherKind.Cloudy ? new Color(0.4f, 0.4f, 0.45f, 0.07f)
                       : new Color(1f, 0.9f, 0.7f, 0.04f);                      // 맑음: 아주 살짝 따뜻하게
            foreach (var d in drops) if (d != null) Destroy(d.gameObject);
            drops.Clear();
            if (k != WeatherKind.Rain) return;
            for (int i = 0; i < 26; i++)
            {
                var go = new GameObject("Rain");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = White();
                sr.color = new Color(0.85f, 0.92f, 1f, 0.8f);
                sr.sortingOrder = YSort.WallProps + 1;
                go.transform.localScale = new Vector3(1f / 32f, 5f / 32f, 1f);   // 1×5 픽셀 빗줄기
                go.transform.localEulerAngles = new Vector3(0, 0, -12f);
                int w = i % CafeLayout.WindowX.Length;
                go.transform.position = new Vector3(CafeLayout.WindowX[w] + Random.Range(-0.8f, 0.8f), CafeLayout.WindowBottom + Random.Range(0.25f, 1.35f), 0f);
                drops.Add(go.transform);
            }
        }
    }
}
