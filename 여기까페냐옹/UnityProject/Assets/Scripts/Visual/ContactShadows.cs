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
        class Entry { public Transform target; public SpriteRenderer src, sh, cast; public CatBrain cat; }

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
            bool sunOk = w == WeatherKind.Sunny && ph != DayPhase.Settlement;   // 저녁에도 햇살 자리엔 해 그림자

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e.target == null || e.src == null) { if (e.sh != null) Destroy(e.sh.gameObject); if (e.cast != null) Destroy(e.cast.gameObject); list.RemoveAt(i); continue; }
                bool hide = !e.src.enabled || e.src.sprite == null || !e.target.gameObject.activeInHierarchy
                            || (e.cat != null && (e.cat.CurrentSeat != null || e.cat.CurrentFacility != null));
                if (hide) { e.sh.enabled = false; e.cast.enabled = false; continue; }
                e.sh.enabled = true; e.cast.enabled = false;

                var b = e.src.bounds;
                Vector3 foot = new Vector3(b.center.x, e.target.position.y, 0f);
                float width = Mathf.Clamp(b.size.x * 0.75f, 0.45f, 2.4f);
                float a = baseA;

                if (sunOk && InBeam(foot))
                {
                    // 햇살 안: 해는 하나라 그림자 방향도 하나 — 바닥 햇살이 기운 쪽(아침 왼쪽 앞, 점심 바로 앞, 저녁 오른쪽 앞)으로
                    //  창이 뒷벽에 있으니 그림자는 화면 앞쪽(아래)으로 조금 내려오고, 키가 클수록 길다
                    float slope = SunSlope(day != null ? day.Phase : DayPhase.Morning);
                    float h = Mathf.Clamp(b.size.y, 0.4f, 2.2f);
                    float len = h * 0.35f;
                    // 10/28: 동그란 타원 대신 '자기 모양' 그림자 — 실루엣을 바닥에 눕혀(뒤집고 납작하게) 해 반대쪽으로 기울인다
                    Cast(e, foot, 0.34f, slope * 22f, 0.3f);
                    e.sh.transform.position = foot + new Vector3(0f, -0.02f, 0f);       // 발밑 접지(짧고 진하게)
                    e.sh.transform.localScale = new Vector3(width * 0.8f, Mathf.Max(0.45f, width * 0.4f), 1f);
                    a = 0.42f;
                }
                else if (night)
                {
                    // 밤: 가장 가까운 천장 조명 반대쪽으로 살짝(조명 바로 아래면 똑바로 짧게)
                    Vector2 lamp = NearestLamp(foot.x);
                    float lean = Mathf.Clamp((foot.x - lamp.x) * 0.05f, -0.35f, 0.35f);
                    Cast(e, foot, 0.22f, lean * 50f, 0.24f);                               // 조명 아래: 짧은 자기 모양 그림자
                    e.sh.transform.position = foot + new Vector3(0f, -0.03f, 0f);
                    e.sh.transform.localScale = new Vector3(width * 0.85f, Mathf.Max(0.5f, width * 0.45f), 1f);
                }
                else
                {
                    // 햇살 밖·흐린 날: 방 전체의 은은한 빛 → 발밑에 동그랗고 연하게
                    e.sh.transform.position = foot + new Vector3(0f, -0.03f, 0f);
                    e.sh.transform.localScale = new Vector3(width, Mathf.Max(0.55f, width * 0.55f), 1f);
                    a = sunny ? 0.34f : 0.38f;
                }
                e.sh.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, a);
            }
        }

        // 자기 모양 그림자: 지금 그림(스프라이트)을 위아래로 뒤집어 납작하게 눌러 발밑에서 화면 앞쪽(아래)으로 눕힌다.
        //  squash = 키 대비 그림자 길이, tilt = 기울기(도, +면 오른쪽 아래로), alpha = 진하기
        void Cast(Entry e, Vector3 foot, float squash, float tilt, float alpha)
        {
            var c = e.cast;
            c.enabled = true;
            c.sprite = e.src.sprite; c.flipX = e.src.flipX;
            var ls = e.src.transform.lossyScale;
            c.transform.rotation = Quaternion.Euler(0f, 0f, tilt);
            c.transform.localScale = new Vector3(ls.x, -Mathf.Abs(ls.y) * squash, 1f);
            c.transform.position = foot;
            var bb = c.bounds;                                          // 뒤집힌 그림의 윗변(=발)이 발 위치에 오게
            c.transform.position += new Vector3(foot.x - bb.center.x + Mathf.Sin(tilt * Mathf.Deg2Rad) * bb.size.y * 0.5f, foot.y - bb.max.y + 0.02f, 0f);
            c.color = new Color(ShadowColor.r * 0.6f, ShadowColor.g * 0.6f, ShadowColor.b * 0.6f, alpha);
        }

        // 바닥 햇살(SunbeamSpot)과 같은 기울기: 아침 -1.6/2.3, 점심 0, 저녁 +1.9/2.5
        static float SunSlope(DayPhase ph) =>
            ph == DayPhase.Lunch ? 0f : (ph == DayPhase.Evening ? 0.76f : -0.7f);

        SpriteRenderer beamSr;
        // 발밑이 바닥 햇살 안에 있나(보일 때만). 햇살 그림의 투명 칸(창살 틈)까지는 안 따지고 둘레 상자로 본다
        bool InBeam(Vector3 foot)
        {
            if (beamSr == null) { var go = GameObject.Find("Sunbeam"); if (go != null) beamSr = go.GetComponent<SpriteRenderer>(); }
            if (beamSr == null || beamSr.sprite == null || beamSr.color.a < 0.3f) return false;
            var bb = beamSr.bounds; bb.Expand(new Vector3(0.2f, 0.25f, 10f));
            return bb.Contains(new Vector3(foot.x, foot.y, bb.center.z));
        }

        static Vector2 NearestLamp(float x)
        {
            float best = x; bool any = false;
            foreach (var t in YeogiCafe.Cafe.FixtureManager.Lamps())
                if (!any || Mathf.Abs(t.position.x - x) < Mathf.Abs(best - x)) { best = t.position.x; any = true; }
            return new Vector2(best, CafeLayout.CeilingY - 1.6f);   // 조명이 없으면 바로 위에서(그림자 똑바로)
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
            sh.sortingOrder = YSort.FlatOnFloor + 6;                         // 러그·햇살 위, 모든 물건 아래
            var cgo = new GameObject("CastShadow");
            cgo.transform.SetParent(transform, false);
            var cast = cgo.AddComponent<SpriteRenderer>();
            if (mat != null) cast.sharedMaterial = mat;
            cast.sortingOrder = YSort.FlatOnFloor + 5; cast.enabled = false;
            list.Add(new Entry { target = t, src = src, sh = sh, cast = cast, cat = cat });
        }
    }
}
