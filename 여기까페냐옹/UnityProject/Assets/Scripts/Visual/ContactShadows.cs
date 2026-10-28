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
        class Entry { public Transform target; public SpriteRenderer src, sh; public MeshRenderer cast; public Mesh mesh; public MaterialPropertyBlock mpb; public CatBrain cat; }

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
            bool night = ph == DayPhase.Evening || ph == DayPhase.Settlement || YeogiCafe.Cafe.FixtureManager.PrimaryGlass() == null;
            var w = Weather.Current;
            bool sunny = w == WeatherKind.Sunny && !night;
            float baseA = night ? 0.6f : sunny ? 0.52f : 0.38f;
            bool sunOk = w == WeatherKind.Sunny && ph != DayPhase.Settlement && YeogiCafe.Cafe.FixtureManager.PrimaryGlass() != null;   // 창이 있어야 해가 든다(통유리를 보관함에 넣으면 조명 그림자)

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e.target == null || e.src == null) { if (e.sh != null) Destroy(e.sh.gameObject); if (e.cast != null) { Destroy(e.cast.gameObject); Destroy(e.mesh); } list.RemoveAt(i); continue; }
                bool hide = !e.src.enabled || e.src.sprite == null || !e.target.gameObject.activeInHierarchy;
                // 10/28 v5: 의자·시설에 앉은 고양이도 그림자를 만든다 — 그 가구의 바닥 줄을 땅으로 삼는다(높이가 있으니 그림자가 가구 그림자에 이어 붙는다)
                float? seatBase = null;
                if (!hide && e.cat != null && (e.cat.CurrentSeat != null || e.cat.CurrentFacility != null))
                {
                    var comp = (e.cat.CurrentSeat as Component) ?? (e.cat.CurrentFacility as Component);
                    var csr = comp != null ? comp.GetComponent<SpriteRenderer>() : null;
                    if (csr == null || csr.sprite == null || e.cat.FacilityWall || comp.transform.position.y > CafeLayout.FloorTop + 0.2f) hide = true;   // 벽 선반·다락 위는 바닥에 안 비친다
                    else seatBase = csr.transform.position.y + BottomY(csr.sprite) * csr.transform.lossyScale.y;
                }
                if (hide) { e.sh.enabled = false; e.cast.enabled = false; continue; }
                e.cast.enabled = false;

                var b = e.src.bounds;
                Vector3 foot = new Vector3(b.center.x, e.target.position.y, 0f);
                Vector3 ground = foot;                                           // 바닥(점프 판정용)
                {   // 발밑 타원도 그림의 실제 발바닥 줄에 붙인다(칸 아래 투명 여백만큼 떠 보이던 것 보정)
                    float baseY = e.src.transform.position.y + BottomY(e.src.sprite) * e.src.transform.lossyScale.y;
                    if (Mathf.Abs(baseY - foot.y) < 0.12f) foot.y = baseY;
                }
                float width = Mathf.Clamp(b.size.x * 0.75f, 0.45f, 2.4f);
                e.sh.enabled = seatBase == null;                                 // 앉아 있으면 발밑 타원은 가구가 대신
                float a = 0.34f;

                if (sunOk)
                {
                    // 해: 방 안 모든 것에 같은 방향 — 바닥 햇살 모양(SunbeamSpot)의 기울기·길이에서 그대로 가져온다
                    //  아침 = 해가 낮고 왼쪽으로 길게 · 점심 = 높아서 짧고 바로 앞 · 저녁 = 낮고 오른쪽으로 더 길게
                    SunShadow(day != null ? day.Phase : DayPhase.Morning, out float ky, out float kx);
                    bool lit = InBeam(foot);
                    Cast(e, ground, ky, kx, lit ? 0.34f : 0.17f, seatBase);       // 햇살 밖은 창빛이 퍼져서 연하게
                    a = lit ? 0.42f : 0.32f;
                }
                else if (night)
                {
                    // 조명: 가장 가까운 천장 조명에서 멀어지는 쪽으로. 조명과 가로로 멀수록 옆으로 더 눕는다(kx = 가로거리/높이차)
                    Vector2 lamp = NearestLamp(foot.x);
                    float kx = Mathf.Clamp((foot.x - lamp.x) / Mathf.Max(1f, lamp.y - foot.y), -1.2f, 1.2f) * 0.45f;
                    Cast(e, ground, 0.24f, kx, 0.26f, seatBase);
                    a = 0.5f;
                }
                else
                {
                    // 흐림·비 낮: 빛이 사방에서 퍼진다 → 짧고 연하게 바로 아래
                    Cast(e, ground, 0.14f, 0f, 0.16f, seatBase);
                    a = 0.36f;
                }
                e.sh.transform.position = foot + new Vector3(0f, -0.02f, 0f);   // 발밑 접지(짧고 진하게)
                e.sh.transform.localScale = new Vector3(width * 0.85f, Mathf.Max(0.45f, width * 0.42f), 1f);
                e.sh.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, a);
            }
        }

        // 자기 모양 그림자(10/28 v2, Graveyard Keeper 방식): 그림자는 '접지점 B'를 축으로만 돈다 → 어떤 각도에서도 물체와 붙어 있다.
        //  B = 그림의 가장 아래 불투명 줄(발바닥) · 스프라이트 기준점 x.  물체의 점 P(높이 h = P.y − B.y)는
        //  S = B + R(tilt)·(Δx, −h·squash)  — 위아래로 뒤집어(−) 납작하게(squash) 눌러 B에 매단 뒤 B를 축으로 기울인다.
        //  squash = 그림자 길이/키(해가 낮을수록 길다), tilt = 빛 반대 방향(도, +면 오른쪽 아래로).
        static readonly Dictionary<Sprite, float> bottomCache = new();
        // 가구 PNG처럼 '읽기 불가'로 들여온 그림도 픽셀을 읽는다(GPU에서 복사). 실패하면 바닥 계산이 칸 아래 변으로 밀려 그림자가 떠 보였다
        static readonly Dictionary<Texture2D, Color32[]> pxCache = new();
        static Color32[] Pixels(Texture2D t)
        {
            if (pxCache.TryGetValue(t, out var c)) return c;
            if (t.isReadable) c = t.GetPixels32();
            else
            {
                var rt = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                var prev = RenderTexture.active;
                Graphics.Blit(t, rt); RenderTexture.active = rt;
                var cp = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false);
                cp.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0); cp.Apply();
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                c = cp.GetPixels32(); Object.Destroy(cp);
            }
            pxCache[t] = c; return c;
        }
        // 스프라이트 기준점에서 가장 아래 불투명 줄까지의 거리(스프라이트 단위). 그림 칸 아래 투명 여백만큼 그림자가 떠 보이던 원인
        static float BottomY(Sprite sp)
        {
            if (bottomCache.TryGetValue(sp, out var v)) return v;
            var r = sp.textureRect; int row = 0;
            try
            {
                var px = Pixels(sp.texture);
                int W = sp.texture.width; bool found = false;
                for (int y = 0; y < (int)r.height && !found; y++)
                    for (int x = 0; x < (int)r.width; x++)
                        if (px[((int)r.y + y) * W + (int)r.x + x].a > 8) { row = y; found = true; break; }
            }
            catch { row = 0; }                                       // 읽을 수 없는 그림이면 칸 아래 변
            v = (row - sp.pivot.y) / sp.pixelsPerUnit;
            bottomCache[sp] = v; return v;
        }

        // 그림자용 '꽉 찬 실루엣': 열마다 맨 위 불투명 칸부터 바닥 줄까지 채운다.
        //  의자 다리 사이·고양이 배 밑 빈칸까지 채워서, 눌러 놓은 그림자가 바닥 줄 전체로 물체와 이어진다(다리만 가늘게 붙고 덩어리가 떠 보이던 것 해결)
        static readonly Dictionary<Sprite, Sprite> solidCache = new();
        static Sprite Solid(Sprite sp)
        {
            if (solidCache.TryGetValue(sp, out var v) && v != null) return v;
            var r = sp.textureRect; int W = (int)r.width, H = (int)r.height;
            var outPx = new Color32[W * H];
            try
            {
                var px = Pixels(sp.texture); int TW = sp.texture.width;
                int baseRow = H;
                for (int y = 0; y < H && baseRow == H; y++)
                    for (int x = 0; x < W; x++) if (px[((int)r.y + y) * TW + (int)r.x + x].a > 8) { baseRow = y; break; }
                for (int x = 0; x < W; x++)
                {
                    int top = -1;
                    for (int y = H - 1; y >= 0; y--) if (px[((int)r.y + y) * TW + (int)r.x + x].a > 8) { top = y; break; }
                    for (int y = baseRow; y <= top; y++) outPx[y * W + x] = new Color32(255, 255, 255, 255);
                }
            }
            catch { return sp; }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "shadow_solid" };
            tex.SetPixels32(outPx); tex.Apply();
            v = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(sp.pivot.x / W, sp.pivot.y / H), sp.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            solidCache[sp] = v; return v;
        }

        // 반환: 그렸으면 true(공중에 뜬 순간 등엔 false → 타원만)
        // v4(10/28): 회전 대신 '기울임(shear)'. 회전하면 바닥 변도 같이 기울어 한쪽 끝이 물체에서 떨어졌다(형 빨간 표시).
        //  물체의 점 (X, Y), 높이 h = Y − B.y  →  그림자 S = (X + kx·h,  B.y − ky·h)
        //  h = 0인 바닥 줄은 그대로 제자리 → 바닥 변 전체가 항상 물체에 붙는다. ky = 길이/키, kx = 옆으로 밀림/키(빛 반대쪽).
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        bool Cast(Entry e, Vector3 groundFoot, float ky, float kx, float alpha, float? baseOverride = null)
        {
            var c = e.cast; var sp = e.src.sprite;
            if (sp == null) return false;
            var st = e.src.transform; var ls = st.lossyScale; var pos = st.position;
            float yb = BottomY(sp);
            float baseY = pos.y + yb * ls.y;
            if (baseOverride.HasValue) baseY = baseOverride.Value;                     // 가구 위: 가구 바닥이 땅
            else if (baseY - groundFoot.y > 0.12f) { c.enabled = false; return false; }   // 점프 중: 그림자를 띄우지 않는다
            var solid = Solid(sp);
            var r = sp.textureRect; float ppu = sp.pixelsPerUnit;
            float x0 = -sp.pivot.x / ppu, x1 = (r.width - sp.pivot.x) / ppu;
            float y0 = -sp.pivot.y / ppu, y1 = (r.height - sp.pivot.y) / ppu;
            bool fx = e.src.flipX;
            var v = new Vector3[4]; var uv = new Vector2[4];
            float[] lx = { x0, x1, x0, x1 }, ly = { y0, y0, y1, y1 };
            for (int i = 0; i < 4; i++)
            {
                float X = pos.x + (fx ? -lx[i] : lx[i]) * ls.x, Y = pos.y + ly[i] * ls.y;
                float h = Y - baseY;
                v[i] = new Vector3(X + kx * h, baseY - ky * h, 0f);
                uv[i] = new Vector2(i % 2 == 0 ? 0f : 1f, i < 2 ? 0f : 1f);
            }
            var m = e.mesh;
            m.vertices = v; m.uv = uv;
            var col = new Color(ShadowColor.r * 0.6f, ShadowColor.g * 0.6f, ShadowColor.b * 0.6f, alpha);
            m.colors = new[] { col, col, col, col };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            m.RecalculateBounds();
            e.mpb.SetTexture(MainTexId, solid.texture);
            c.SetPropertyBlock(e.mpb);
            c.enabled = true;
            return true;
        }

        // 해 그림자 길이(ky = 그림자 길이/키)·옆 밀림(kx). 바닥 햇살의 skew/depth와 같은 비율(아침 −1.6/2.3, 저녁 +1.9/2.5)
        static void SunShadow(DayPhase ph, out float ky, out float kx)
        {
            switch (ph)
            {
                case DayPhase.Lunch:   ky = 0.16f; kx = 0f; break;                       // 해가 높다 → 짧게
                case DayPhase.Evening: ky = 0.5f;  kx = 0.5f * 0.76f; break;             // 낮은 해 → 오른쪽 앞으로 길게
                default:               ky = 0.44f; kx = 0.44f * -0.7f; break;            // 아침(준비 포함) → 왼쪽 앞으로
            }
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
            var cgo = new GameObject("CastShadow");                         // 세계 좌표 메시(기울인 사각형 1장)
            cgo.transform.SetParent(transform, false);
            var mesh = new Mesh { name = "cast_shadow" }; mesh.MarkDynamic();
            cgo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var cast = cgo.AddComponent<MeshRenderer>();
            if (mat != null) cast.sharedMaterial = mat;
            cast.sortingOrder = YSort.FlatOnFloor + 5; cast.enabled = false;
            list.Add(new Entry { target = t, src = src, sh = sh, cast = cast, mesh = mesh, mpb = new MaterialPropertyBlock(), cat = cat });
        }
    }
}
