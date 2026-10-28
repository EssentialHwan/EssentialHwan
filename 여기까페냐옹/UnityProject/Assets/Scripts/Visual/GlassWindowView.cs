using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Art;
using YeogiCafe.Core;

namespace YeogiCafe.Visual
{
    // 통유리 창 한 개(옮길 수 있는 기본 사물). 바깥 풍경(날씨×시간대×계절) + 창틀 + 비 오는 날 유리 빗줄기.
    // 기준점 = 유리 아랫단 가운데. 같은 창이 여러 개여도 각자 그린다(가게 통유리 8유닛, 큰 통창 4유닛).
    public class GlassWindowView : MonoBehaviour
    {
        public int glassPx = GlassWindowDrawing.GW;          // 유리 가로 픽셀(256 = 8유닛, 128 = 4유닛)
        public float Width => glassPx / 32f;
        public const float Height = GlassWindowDrawing.GH / 32f;

        SpriteRenderer outside, frame;
        readonly List<Transform> drops = new();
        int shownKey = -1; WeatherKind shownWeather = (WeatherKind)(-1);
        DayManager day;
        static Sprite white;

        public SpriteRenderer FrameRenderer => frame;

        void Awake()
        {
            var o = new GameObject("GlassOutside"); o.transform.SetParent(transform, false);
            outside = o.AddComponent<SpriteRenderer>(); outside.sortingOrder = YSort.WallProps - 3;   // 그 위에 거리 움직임(StreetLife) -2~0
            var f = new GameObject("GlassFrame"); f.transform.SetParent(transform, false);
            f.transform.localPosition = new Vector3(0f, -(GlassWindowDrawing.Border + 3) / 32f, 0f);
            frame = f.AddComponent<SpriteRenderer>(); frame.sortingOrder = YSort.WallProps + 2;
        }

        void Start()
        {
            int gw = glassPx;
            var life = gameObject.AddComponent<StreetLife>();          // 창밖 행인·차(10/29)
            life.widthPx = gw; life.outside = outside;
            frame.sprite = gw == GlassWindowDrawing.GW ? RuntimeSprites.Get("glass_frame", GlassWindowDrawing.Frame)
                                                       : RuntimeSprites.Get("glass_frame_" + gw, () => GlassWindowDrawing.Frame(gw));
        }

        int Season() { if (day == null) day = FindObjectOfType<DayManager>(); return day != null ? YeogiCafe.Data.SeasonBook.Season(day.CurrentDay) : 1; }
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
            int season = Season(), w = (int)Weather.Current, p = PhaseIndex();
            int key = (w * 3 + p) * 4 + season;
            if (key != shownKey)
            {
                shownKey = key;
                int gw = glassPx;
                outside.sprite = gw == GlassWindowDrawing.GW
                    ? RuntimeSprites.Get($"glass_out_{w}_{p}_{season}", () => GlassWindowDrawing.Outside(w, p, season))
                    : RuntimeSprites.Get($"glass_out_{gw}_{w}_{p}_{season}", () => GlassWindowDrawing.OutsidePart(gw, w, p, season));
            }
            if (Weather.Current != shownWeather) Rain(Weather.Current == WeatherKind.Rain);
            if (drops.Count == 0) return;
            float dt = Time.deltaTime, half = Width * 0.5f;
            Vector3 c = transform.position;
            float left = c.x - half + 0.05f, right = c.x + half - 0.05f, bottom = c.y + 0.05f, top = c.y + Height - 0.1f;
            for (int i = 0; i < drops.Count; i++)
            {
                var d = drops[i];
                var q = d.position; q.y -= dt * (3.2f + (i % 3) * 0.6f); q.x -= dt * 0.35f;
                if (q.y < bottom || q.x < left || q.x > right) { q.y = top; q.x = Random.Range(left, right); }
                d.position = q;
            }
        }

        void Rain(bool on)
        {
            shownWeather = Weather.Current;
            foreach (var d in drops) if (d != null) Destroy(d.gameObject);
            drops.Clear();
            if (!on) return;
            if (white == null)
            {
                var t = new Texture2D(1, 1); t.SetPixel(0, 0, Color.white); t.Apply(); t.filterMode = FilterMode.Point;
                white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            int n = Mathf.RoundToInt(70 * Width / 8f);
            float half = Width * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Rain");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = white;
                sr.color = new Color(0.85f, 0.92f, 1f, 0.8f);
                sr.sortingOrder = YSort.WallProps + 1;
                go.transform.localScale = new Vector3(1f / 32f, 5f / 32f, 1f);   // 1×5 픽셀 빗줄기
                go.transform.localEulerAngles = new Vector3(0, 0, -12f);
                go.transform.localPosition = new Vector3(Random.Range(-half, half), Random.Range(0.1f, Height - 0.1f), 0f);
                drops.Add(go.transform);
            }
        }
    }
}
