using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Core;
using YeogiCafe.Art;

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

        // 통유리 창
        SpriteRenderer outside, frame;
        int shownOutside = -1;
        DayManager day;

        void Start()
        {
            // 예전 씬의 작은 창 두 개는 숨긴다(통유리로 대체)
            var room = GameObject.Find("Room");
            if (room != null) foreach (Transform t in room.transform) if (t.name == "Window") t.gameObject.SetActive(false);
            var o = new GameObject("GlassOutside"); o.transform.SetParent(transform, false);
            o.transform.position = new Vector3(CafeLayout.GlassCenterX, CafeLayout.GlassBottom, 0f);
            outside = o.AddComponent<SpriteRenderer>(); outside.sortingOrder = YSort.WallProps;
            var f = new GameObject("GlassFrame"); f.transform.SetParent(transform, false);
            f.transform.position = new Vector3(CafeLayout.GlassCenterX, CafeLayout.GlassBottom - (GlassWindowDrawing.Border + 3) / 32f, 0f);
            frame = f.AddComponent<SpriteRenderer>(); frame.sortingOrder = YSort.WallProps + 2;
            frame.sprite = RuntimeSprites.Get("glass_frame", GlassWindowDrawing.Frame);
        }

        int PhaseIndex()
        {
            if (day == null) day = FindObjectOfType<DayManager>();
            if (day == null) return 1;
            switch (day.Phase)
            {
                case YeogiCafe.Data.DayPhase.Lunch: return 1;
                case YeogiCafe.Data.DayPhase.Evening:
                case YeogiCafe.Data.DayPhase.Settlement: return 2;
                default: return 0;
            }
        }

        void Update()
        {
            if (Weather.Current != shown) Apply(Weather.Current);
            int key = (int)Weather.Current * 3 + PhaseIndex();
            if (outside != null && key != shownOutside)
            {
                shownOutside = key;
                int w = (int)Weather.Current, p = PhaseIndex();
                outside.sprite = RuntimeSprites.Get($"glass_out_{w}_{p}", () => GlassWindowDrawing.Outside(w, p));
            }
            if (Weather.Current != WeatherKind.Rain) return;
            float dt = Time.deltaTime;
            float left = CafeLayout.GlassLeft + 0.05f, right = CafeLayout.GlassRight - 0.05f;
            float bottom = CafeLayout.GlassBottom + 0.05f, top = CafeLayout.GlassBottom + CafeLayout.GlassHeight - 0.1f;
            for (int i = 0; i < drops.Count; i++)
            {
                var d = drops[i];
                var p = d.position; p.y -= dt * (3.2f + (i % 3) * 0.6f); p.x -= dt * 0.35f;
                if (p.y < bottom) { p.y = top; p.x = Random.Range(left, right); }
                if (p.x < left) p.x = right;
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
            for (int i = 0; i < 70; i++)
            {
                var go = new GameObject("Rain");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = White();
                sr.color = new Color(0.85f, 0.92f, 1f, 0.8f);
                sr.sortingOrder = YSort.WallProps + 1;
                go.transform.localScale = new Vector3(1f / 32f, 5f / 32f, 1f);   // 1×5 픽셀 빗줄기
                go.transform.localEulerAngles = new Vector3(0, 0, -12f);
                go.transform.position = new Vector3(Random.Range(CafeLayout.GlassLeft, CafeLayout.GlassRight),
                                                    CafeLayout.GlassBottom + Random.Range(0.1f, CafeLayout.GlassHeight - 0.1f), 0f);
                drops.Add(go.transform);
            }
        }
    }
}
