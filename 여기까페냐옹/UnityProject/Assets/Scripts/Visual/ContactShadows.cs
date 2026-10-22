using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Art;
using YeogiCafe.Cafe;
using YeogiCafe.Core;
using YeogiCafe.Data;

namespace YeogiCafe.Visual
{
    // 접지 그림자: 고양이·바닥 가구 발밑에 도트 타원 그림자를 깔고, 그 방향을 '지금 가장 센 빛'에 맞춘다.
    //  낮(맑음) → 통유리 창이 광원. 창에서 먼 쪽으로 비스듬히 길게.
    //  저녁·밤   → 가장 가까운 천장 조명이 광원. 바로 아래 짧고 진하게.
    //  흐림·비   → 빛이 퍼져서 연하고 둥글게.
    // URP 2D 그림자(벽에 세워지는 다각형)는 이 정면 시점 도트에 어울리지 않아 쓰지 않는다.
    public class ContactShadows : MonoBehaviour
    {
        class Entry { public Transform target; public SpriteRenderer src, sh; public CatBrain cat; }

        readonly List<Entry> list = new();
        readonly HashSet<Transform> known = new();
        DayManager day;
        Material mat;
        float scanT;
        static Sprite blob;
        static readonly Color ShadowColor = new Color(0.2f, 0.13f, 0.1f, 1f);

        public static void Ensure(Material litMat)
        {
            if (FindObjectOfType<ContactShadows>() != null) return;
            var c = new GameObject("[ContactShadows]").AddComponent<ContactShadows>();
            c.mat = litMat;
        }

        void Start() { day = FindObjectOfType<DayManager>(); }

        // 32×10 도트 타원: 가운데 진하고 가장자리 한 단계 연하게(계단식 — 부드러운 번짐 대신 도트 느낌)
        static Sprite Blob()
        {
            if (blob != null) return blob;
            const int W = 32, H = 10;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "contact_shadow" };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x + 0.5f - W / 2f) / (W / 2f), dy = (y + 0.5f - H / 2f) / (H / 2f);
                    float d = dx * dx + dy * dy;
                    byte a = d <= 0.45f ? (byte)255 : d <= 1f ? (byte)140 : (byte)0;
                    px[y * W + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px); tex.Apply();
            blob = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 32f);
            return blob;
        }

        void Update()
        {
            scanT -= Time.unscaledDeltaTime;
            if (scanT <= 0f) { scanT = 0.5f; Scan(); }
            if (day == null) day = FindObjectOfType<DayManager>();

            var ph = day != null ? day.Phase : DayPhase.Morning;
            bool night = ph == DayPhase.Evening || ph == DayPhase.Settlement;
            var w = Weather.Current;
            bool sunny = w == WeatherKind.Sunny && !night;
            float baseA = night ? 0.6f : sunny ? 0.52f : 0.38f;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e.target == null || e.src == null) { if (e.sh != null) Destroy(e.sh.gameObject); list.RemoveAt(i); continue; }
                bool hide = !e.src.enabled || e.src.sprite == null || !e.target.gameObject.activeInHierarchy
                            || (e.cat != null && (e.cat.CurrentSeat != null || e.cat.CurrentFacility != null));
                if (hide) { e.sh.enabled = false; continue; }
                e.sh.enabled = true;

                var b = e.src.bounds;
                Vector3 foot = new Vector3(b.center.x, e.target.position.y, 0f);
                float width = Mathf.Clamp(b.size.x * 0.75f, 0.45f, 2.4f);

                // 빛 위치 → 그림자는 빛 반대쪽으로 기운다(정면 시점이라 가로 방향만 의미 있음)
                Vector2 light = night ? NearestLamp(foot.x) : new Vector2(CafeLayout.GlassCenterX, CafeLayout.GlassBottom);
                float lean = Mathf.Clamp((foot.x - light.x) * (night ? 0.05f : 0.06f), -0.35f, 0.35f);
                if (!sunny && !night) lean *= 0.3f;                          // 흐린 날은 거의 바로 아래
                float stretch = 1f + Mathf.Abs(lean) * (sunny ? 1.4f : 0.8f);

                e.sh.transform.position = foot + new Vector3(lean * width * 0.6f, -0.03f, 0f);
                e.sh.transform.localScale = new Vector3(width * stretch, Mathf.Max(0.55f, width * 0.55f), 1f);
                e.sh.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, baseA);
            }
        }

        static Vector2 NearestLamp(float x)
        {
            float best = CafeLayout.LampX[0];
            foreach (var lx in CafeLayout.LampX) if (Mathf.Abs(lx - x) < Mathf.Abs(best - x)) best = lx;
            return new Vector2(best, CafeLayout.CeilingY - 1.6f);
        }

        void Scan()
        {
            foreach (var cv in FindObjectsOfType<CatView>())
            {
                var vis = cv.transform.Find("Visual");
                Add(cv.transform, vis != null ? vis.GetComponent<SpriteRenderer>() : null, cv.GetComponent<CatBrain>());
            }
            foreach (var s in FindObjectsOfType<SeatBehaviour>()) Add(s.transform, s.GetComponent<SpriteRenderer>(), null);
            foreach (var f in FindObjectsOfType<FacilityBehaviour>())
            {
                if (f.source == null || f.source.wallMounted || f.name == "Sunbeam") continue;
                if (f.source.furnitureId == "furn_fac_heatpad" || f.source.furnitureId == "furn_fac_cushion") continue;   // 바닥에 납작
                Add(f.transform, f.GetComponent<SpriteRenderer>(), null);
            }
        }

        void Add(Transform t, SpriteRenderer src, CatBrain cat)
        {
            if (t == null || src == null || known.Contains(t)) return;
            if (t.position.y > CafeLayout.FloorTop + 0.2f) return;          // 벽에 붙은 것
            known.Add(t);
            var go = new GameObject("ContactShadow");
            go.transform.SetParent(transform, false);
            var sh = go.AddComponent<SpriteRenderer>();
            sh.sprite = Blob();
            if (mat != null) sh.sharedMaterial = mat;                       // 조명을 받아 저녁엔 그림자도 같이 어두워진다
            sh.sortingOrder = YSort.FlatOnFloor + 5;                         // 러그·햇살 위, 모든 물건 아래
            list.Add(new Entry { target = t, src = src, sh = sh, cat = cat });
        }
    }
}
