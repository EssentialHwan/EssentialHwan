using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Data;

namespace YeogiCafe.Visual
{
    // 창밖 거리의 움직임(10/29): 고양이 행인·운전하는 고양이·자전거·배달 오토바이·버스·건너편 작은 행인·새.
    //  GlassWindowView가 붙인다. 유리 밖으로 삐져나오지 않게 SpriteMask로 자르고, 위치는 1px(1/32유닛) 단위로 맞춰 도트를 선명하게.
    //  날씨: 비 → 우산 행인, 자전거 없음, 차는 그대로 · 흐림 → 조금 적게 · 밤(정산) → 아주 드물게, 전조등·창문 노란 불
    //  깊이(그리기 순서): 하늘 새 < 건너편 행인 < 길(차) < 가까운 보도(행인)  — 모두 창틀·빗줄기 아래
    public class StreetLife : MonoBehaviour
    {
        class Mover
        {
            public Transform t; public SpriteRenderer sr; public Sprite a, b;
            public float x, y, speed, anim, frameDur, bob;
        }

        public float widthPx = GlassWindowDrawing.GW;
        public SpriteRenderer outside;                  // 재질(조명) 맞춤용
        readonly List<Mover> movers = new();
        float tNear, tRoad, tFar, tSky;
        DayManager day;
        static Sprite white;

        // 그리기 순서(GlassWindowView: 바깥 풍경 = WallProps-3, 빗줄기 +1, 창틀 +2)
        const int OrdSky = YSort.WallProps - 2, OrdFar = YSort.WallProps - 2, OrdRoad = YSort.WallProps - 1, OrdNear = YSort.WallProps;

        void Start()
        {
            if (white == null)
            {
                var t = new Texture2D(1, 1); t.SetPixel(0, 0, Color.white); t.Apply(); t.filterMode = FilterMode.Point;
                white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            var m = new GameObject("StreetMask"); m.transform.SetParent(transform, false);
            var mask = m.AddComponent<SpriteMask>(); mask.sprite = white;
            m.transform.localScale = new Vector3(widthPx / 32f, GlassWindowDrawing.GH / 32f, 1f);
            m.transform.localPosition = new Vector3(0f, GlassWindowDrawing.GH / 64f, 0f);
            day = FindObjectOfType<DayManager>();
            // 처음 들어왔을 때 거리가 비어 보이지 않게 몇 개 미리
            tNear = Random.Range(0.5f, 2f); tRoad = Random.Range(0.2f, 1.5f); tFar = Random.Range(0f, 1f); tSky = Random.Range(3f, 8f);
        }

        int Phase()
        {
            if (day == null) day = FindObjectOfType<DayManager>();
            if (day == null) return 1;
            return day.Phase == DayPhase.Lunch ? 1 : day.Phase == DayPhase.Settlement ? 3 : day.Phase == DayPhase.Evening ? 2 : 0;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var w = Weather.Current; int ph = Phase();
            bool rain = w == WeatherKind.Rain, night = ph == 3;
            float busy = night ? 0.25f : rain ? 0.7f : w == WeatherKind.Cloudy ? 0.85f : ph == 1 ? 1.25f : 1f;   // 점심이 제일 북적
            int dim = rain ? 74 : night ? 70 : w == WeatherKind.Cloudy ? 90 : 100;
            bool lights = night || (ph == 2 && rain);

            if ((tNear -= dt * busy) <= 0f) { tNear = Random.Range(3.5f, 8f); SpawnNear(rain, dim, ph); }
            if ((tRoad -= dt * busy) <= 0f) { tRoad = Random.Range(3.5f, 7.5f); SpawnRoad(rain, dim, lights); }
            if ((tFar -= dt * busy) <= 0f) { tFar = Random.Range(5f, 10f); SpawnFar(dim); }
            if (!rain && !night && (tSky -= dt) <= 0f) { tSky = Random.Range(9f, 20f); SpawnBirds(); }

            for (int i = movers.Count - 1; i >= 0; i--)
            {
                var m = movers[i];
                m.x += m.speed * dt;
                if (m.x < -70f || m.x > widthPx + 70f) { Destroy(m.t.gameObject); movers.RemoveAt(i); continue; }
                m.anim += dt;
                if (m.b != null) m.sr.sprite = ((int)(m.anim / m.frameDur) & 1) == 0 ? m.a : m.b;
                float by = m.bob > 0f && ((int)(m.anim / m.frameDur) & 1) == 1 ? m.bob : 0f;
                m.t.localPosition = new Vector3((Mathf.Round(m.x) - widthPx * 0.5f) / 32f, Mathf.Round(m.y + by) / 32f, 0f);
                if (outside != null && m.sr.sharedMaterial != outside.sharedMaterial) m.sr.sharedMaterial = outside.sharedMaterial;
            }
        }

        // 가까운 보도(유리 아래쪽 y 0~8): 네 발 행인 · 장바구니 · 비 오는 날엔 우산
        void SpawnNear(bool rain, int dim, int ph)
        {
            int fur = Random.Range(0, StreetArt.FurCount), body = Random.Range(0, StreetArt.BodyCount);
            string k; float sp;
            if (rain) { k = "umb"; sp = 9f; }
            else
            {
                float r = Random.value;
                if (r < 0.55f) { k = "walk4"; sp = 12f; }
                else if (r < 0.85f) { k = "bag"; sp = 8f; }
                else { k = "walk4"; sp = 24f; }                      // 가끔 뛰어가는 고양이
            }
            float dir = Random.value < 0.5f ? 1f : -1f;
            Add(k, fur, body, dim, false, OrdNear, dir, sp, k == "walk4" ? 0.16f * 12f / sp : 0.22f, 0f);
            // 점심엔 가끔 둘이 나란히(뒤따라오는 친구)
            if (ph == 1 && !rain && Random.value < 0.3f) Add("walk4", Random.Range(0, StreetArt.FurCount), 0, dim, false, OrdNear, dir, sp, 0.16f * 12f / sp, 0f, -18f);
        }

        // 길(y 9~21): 위 차선(건너편 쪽) ← · 아래 차선 →
        void SpawnRoad(bool rain, int dim, bool lights)
        {
            float r = Random.value; string k; float sp, bob = 0f;
            if (r < 0.5f) { k = "car"; sp = 34f; }
            else if (r < 0.68f) { k = "moto"; sp = 44f; bob = 1f; }
            else if (r < 0.82f && !rain) { k = "bike"; sp = 20f; }
            else if (!movers.Exists(m => m.t != null && m.t.name == "Street_bus")) { k = "bus"; sp = 26f; }   // 버스는 한 대씩
            else { k = "car"; sp = 34f; }
            bool far = Random.value < 0.5f;
            int fur = Random.Range(0, StreetArt.FurCount), body = k == "moto" ? 4 : Random.Range(0, StreetArt.BodyCount);
            if (k == "bus") body = Random.value < 0.5f ? 3 : 1;      // 초록·파랑 버스
            float y = far ? 14f : 9f;
            Add(k, fur, body, dim, lights, OrdRoad, far ? -1f : 1f, sp, k == "bike" ? 0.2f : 0.12f, bob, 0f, y);
        }

        // 건너편 보도(집 앞 y 22): 아주 작은 행인
        void SpawnFar(int dim)
        {
            Add("tiny", Random.Range(0, StreetArt.FurCount), 0, Mathf.Min(dim, 92), false, OrdFar, Random.value < 0.5f ? 1f : -1f, 5f, 0.3f, 0f, 0f, 22f);
        }

        void SpawnBirds()
        {
            float dir = Random.value < 0.5f ? 1f : -1f, y = Random.Range(46f, 62f);
            int n = Random.Range(1, 4);
            for (int i = 0; i < n; i++) Add("bird", 2, 0, 100, false, OrdSky, dir, 18f + i, 0.18f + i * 0.03f, 0f, -i * 9f, y + (i % 2) * 3f);
        }

        // dir: +1 오른쪽으로, -1 왼쪽으로, 0 = +1과 같음. offset: 줄 지어 올 때 뒤로 미는 거리(px)
        void Add(string k, int fur, int body, int dim, bool lights, int order, float dir, float speed, float frameDur, float bob,
                 float offset = 0f, float y = 1f)
        {
            if (dir == 0f) dir = 1f;
            var go = new GameObject("Street_" + k);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order; sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            if (outside != null) sr.sharedMaterial = outside.sharedMaterial;
            var a = StreetArt.Get(k + "_0", fur, body, dim, lights);
            var b = StreetArt.Has(k + "_1") ? StreetArt.Get(k + "_1", fur, body, dim, lights) : null;
            sr.sprite = a; sr.flipX = dir < 0f;                       // 그림은 모두 오른쪽을 본다
            float half = a.rect.width * 0.5f;
            float x = dir > 0f ? -half - 2f + offset : widthPx + half + 2f - offset;
            var m = new Mover { t = go.transform, sr = sr, a = a, b = b, x = x, y = y, speed = speed * dir, frameDur = frameDur, bob = bob, anim = Random.value };
            movers.Add(m);
            go.transform.localPosition = new Vector3((Mathf.Round(x) - widthPx * 0.5f) / 32f, Mathf.Round(y) / 32f, 0f);
        }
    }
}
