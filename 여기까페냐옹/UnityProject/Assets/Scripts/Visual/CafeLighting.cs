using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using YeogiCafe.Core;
using YeogiCafe.Data;

namespace YeogiCafe.Visual
{
    // URP 2D 조명. 빛의 출처는 딱 두 가지로 정리한다.
    //  1) 통유리 창: 창 주변 벽을 밝히는 창빛 + 바닥에 떨어지는 창 모양 햇살(SunbeamSpot 자리를 그대로 따라감)
    //  2) 천장 조명 3개: 낮엔 거의 꺼져 있다가 저녁·정산 때 따뜻한 빛 웅덩이를 만든다
    // 전역광은 '방의 기본 밝기'만 맡고(낮엔 원래 그림에 가깝게), 시간대·날씨에 따라 천천히 바뀐다.
    // 바깥 풍경·조명 갓·날씨 덧칠은 조명을 받지 않는다(빛을 내는 쪽이니까).
    // URP가 꺼져 있으면(기본 렌더링) 아무것도 하지 않는다.
    public class CafeLighting : MonoBehaviour
    {
        Light2D global, window, beam;
        readonly Light2D[] lamps = new Light2D[3];
        readonly Light2D[] pools = new Light2D[3];      // 조명 아래 바닥에 떨어지는 빛 웅덩이
        readonly SpriteRenderer[] halos = new SpriteRenderer[3];   // 갓 아래 빛 번짐(불 켜진 게 보이게)
        const float ShadeBottomY = 6.55f;                // 갓 아랫단 높이(상단 메뉴 바 아래로 확실히 보이게)
        DayManager day;
        Material litMat, unlitMat;
        SpriteRenderer beamSr;           // SunbeamSpot이 그리는 바닥 햇살
        Sprite beamFor;                  // beam 쿠키를 만든 원본
        float scanT;
        static readonly Dictionary<Sprite, Sprite> cookies = new();

        public static void Ensure()
        {
            if (!YeogiCafe.Art.LitSprites.Enabled) return;
            if (FindObjectOfType<CafeLighting>() != null) return;
            new GameObject("[CafeLighting]").AddComponent<CafeLighting>();
        }

        void Start()
        {
            day = FindObjectOfType<DayManager>();
            litMat = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.default2DMaterial : null;
            var unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlit != null) unlitMat = new Material(unlit) { name = "CafeUnlit" };

            global = Make("Global", Light2D.LightType.Global, Vector3.zero, false);

            // 창빛: 유리 아래쪽 가운데에서 넓고 부드럽게(노멀맵 X — 창 주변이 은은하게 밝아지는 정도)
            window = Make("WindowGlow", Light2D.LightType.Point,
                new Vector3(CafeLayout.GlassCenterX, CafeLayout.GlassBottom + CafeLayout.GlassHeight * 0.45f, 0f), false);
            window.pointLightInnerRadius = 2.2f; window.pointLightOuterRadius = 7.5f; window.falloffIntensity = 0.7f;

            // 천장 조명: 갓이 상단 메뉴 바에 가려 '빛만 있고 광원이 없는' 상태가 되지 않게 갓을 화면 안으로 내린다
            var shades = LowerLamps();
            for (int i = 0; i < lamps.Length; i++)
            {
                float lx = CafeLayout.LampX[i];
                float sy = shades[i] != null ? shades[i].bounds.min.y : CafeLayout.CeilingY - 1.0f;
                // 노멀맵 O — 고양이·가구 윗면이 위에서 빛을 받는다
                var l = Make("Lamp" + i, Light2D.LightType.Point, new Vector3(lx, sy - 0.1f, 0f), true);
                l.pointLightInnerRadius = 0.4f; l.pointLightOuterRadius = 5.2f; l.falloffIntensity = 0.8f;
                lamps[i] = l;
                var p = Make("LampPool" + i, Light2D.LightType.Point, new Vector3(lx, 2.9f, 0f), false);
                p.pointLightInnerRadius = 0.6f; p.pointLightOuterRadius = 2.9f; p.falloffIntensity = 0.6f;
                pools[i] = p;
                halos[i] = Halo(new Vector3(lx, sy - 0.05f, 0f), shades[i]);
            }
            ContactShadows.Ensure(litMat);

            Apply(1f);
            ScanSprites();
        }

        // 씬의 "Lamp"(펜던트 조명 그림)를 갓 아랫단이 ShadeBottomY에 오도록 내린다. 줄 윗부분은 메뉴 바 뒤로 숨는다.
        SpriteRenderer[] LowerLamps()
        {
            var res = new SpriteRenderer[CafeLayout.LampX.Length];
            foreach (var sr in FindObjectsOfType<SpriteRenderer>())
            {
                if (sr.gameObject.name != "Lamp") continue;
                int best = 0;
                for (int i = 1; i < CafeLayout.LampX.Length; i++)
                    if (Mathf.Abs(CafeLayout.LampX[i] - sr.transform.position.x) < Mathf.Abs(CafeLayout.LampX[best] - sr.transform.position.x)) best = i;
                float drop = sr.bounds.min.y - ShadeBottomY;
                if (drop > 0f) sr.transform.position -= new Vector3(0f, drop, 0f);
                res[best] = sr;
            }
            return res;
        }

        // 갓 아래 둥근 빛 번짐(도트 3단 원). 조명을 받지 않는 재질 — 스스로 빛나는 쪽
        SpriteRenderer Halo(Vector3 pos, SpriteRenderer shade)
        {
            var go = new GameObject("LampHalo"); go.transform.SetParent(transform, false); go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = HaloSprite();
            if (unlitMat != null) sr.sharedMaterial = unlitMat;
            sr.sortingOrder = shade != null ? shade.sortingOrder + 1 : YSort.WallProps + 4;
            sr.color = new Color(1f, 0.85f, 0.55f, 0f);
            return sr;
        }

        static Sprite haloSprite;
        static Sprite HaloSprite()
        {
            if (haloSprite != null) return haloSprite;
            const int N = 40;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "lamp_halo" };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f - N / 2f) / (N / 2f), dy = (y + 0.5f - N / 2f) / (N / 2f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = d <= 0.3f ? (byte)150 : d <= 0.6f ? (byte)80 : d <= 1f ? (byte)32 : (byte)0;
                    px[y * N + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px); tex.Apply();
            haloSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.6f), 32f);
            return haloSprite;
        }

        Light2D Make(string n, Light2D.LightType type, Vector3 pos, bool normals, Transform parent = null)
        {
            var go = new GameObject(n); go.transform.SetParent(parent != null ? parent : transform, false);
            if (parent == null) go.transform.position = pos; else go.transform.localPosition = pos;
            var l = go.AddComponent<Light2D>();
            l.lightType = type;
            l.intensity = 0f;
            if (type != Light2D.LightType.Global)
            {
                // URP 14에서는 읽기 전용 프로퍼티라 내부 필드에 직접 넣는다
                SetField(l, "m_NormalMapQuality", normals ? Light2D.NormalMapQuality.Accurate : Light2D.NormalMapQuality.Disabled);
                SetField(l, "m_NormalMapDistance", 2.2f);
            }
            return l;
        }

        static void SetField(object o, string name, object v)
        {
            var f = o.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) f.SetValue(o, v);
            else Debug.LogWarning("[CafeLighting] 필드 없음: " + name);
        }

        void Update()
        {
            TrackBeam();
            Apply(Time.unscaledDeltaTime * 1.5f);
            scanT -= Time.unscaledDeltaTime;
            if (scanT <= 0f) { scanT = 1f; ScanSprites(); }      // 새로 놓인 가구·고양이도 조명을 받게
        }

        // 바닥 햇살: SunbeamSpot의 창 모양 그림을 그대로 '빛 모양(쿠키)'으로 써서 같은 자리에 진짜 빛을 떨어뜨린다
        void TrackBeam()
        {
            if (beamSr == null)
            {
                var go = GameObject.Find("Sunbeam");
                if (go == null) return;
                beamSr = go.GetComponent<SpriteRenderer>();
                if (beamSr == null) return;
                beam = Make("FloorBeam", Light2D.LightType.Sprite, Vector3.zero, false, go.transform);
            }
            if (beamSr.sprite != null && beamSr.sprite != beamFor)
            {
                beamFor = beamSr.sprite;
                SetField(beam, "m_LightCookieSprite", Cookie(beamFor));
            }
        }

        // 햇살 그림의 칸(반투명) → 흰색 불투명 빛 모양. 창살 틈은 그대로 비워 둬서 바닥에 창살 그림자가 생긴다
        static Sprite Cookie(Sprite src)
        {
            if (cookies.TryGetValue(src, out var c) && c != null) return c;
            var t = src.texture;
            Color32[] px;
            try { px = t.GetPixels32(); } catch { return src; }
            var o = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++) o[i] = px[i].a > 0 ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            var tex = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = t.name + "_cookie" };
            tex.SetPixels32(o); tex.Apply();
            var r = src.rect;
            c = Sprite.Create(tex, r, new Vector2(src.pivot.x / r.width, src.pivot.y / r.height), src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            cookies[src] = c;
            return c;
        }

        // 기본 렌더링용으로 만든 씬 스프라이트(Sprites-Default)를 조명 재질로 바꾸고 노멀맵을 붙인다.
        // 빛을 내는 쪽(바깥 풍경·조명 갓)과 화면 덧칠은 조명을 받지 않게 둔다.
        void ScanSprites()
        {
            foreach (var sr in FindObjectsOfType<SpriteRenderer>())
            {
                if (sr.GetComponentInParent<Canvas>() != null) continue;
                string nm = sr.gameObject.name;
                if (nm == "LampHalo" || nm == "ContactShadow") continue;          // 직접 관리하는 것
                var sh = sr.sharedMaterial != null ? sr.sharedMaterial.shader : null;
                bool isDefault = sh == null || sh.name == "Sprites/Default";
                if (IsEmitter(sr))
                {
                    if (unlitMat != null && sr.sharedMaterial != unlitMat) sr.sharedMaterial = unlitMat;
                    continue;
                }
                if (litMat != null && isDefault) sr.sharedMaterial = litMat;
                YeogiCafe.Art.LitSprites.Upgrade(sr);
            }
        }

        static bool IsEmitter(SpriteRenderer sr)
        {
            string n = sr.gameObject.name;
            return n == "GlassOutside" || n == "Lamp" || n == "WeatherTint" || n == "Rain";
        }

        struct Look { public Color g; public float gi; public Color w; public float wi; public Color b; public float bi; public float li; }

        // 시간대별 목표값(낮엔 원래 그림 밝기에 가깝게, 저녁엔 창빛 주황 + 천장 조명, 정산 밤엔 푸른 방에 조명만)
        Look Target()
        {
            var ph = day != null ? day.Phase : DayPhase.Morning;
            Look k;
            switch (ph)
            {
                case DayPhase.Lunch:
                    k.g = new Color(1f, 0.99f, 0.96f); k.gi = 0.95f; k.w = new Color(1f, 0.97f, 0.88f); k.wi = 0.45f;
                    k.b = new Color(1f, 0.95f, 0.8f); k.bi = 0.3f; k.li = 0.1f; break;
                case DayPhase.Evening:
                    k.g = new Color(0.9f, 0.78f, 0.76f); k.gi = 0.66f; k.w = new Color(1f, 0.6f, 0.38f); k.wi = 0.85f;
                    k.b = new Color(1f, 0.64f, 0.38f); k.bi = 0.45f; k.li = 1.05f; break;
                case DayPhase.Settlement:
                    k.g = new Color(0.6f, 0.65f, 0.92f); k.gi = 0.45f; k.w = new Color(0.55f, 0.62f, 1f); k.wi = 0.3f;
                    k.b = Color.white; k.bi = 0f; k.li = 1.15f; break;
                default:
                    k.g = new Color(1f, 0.97f, 0.92f); k.gi = 0.9f; k.w = new Color(1f, 0.92f, 0.76f); k.wi = 0.5f;
                    k.b = new Color(1f, 0.93f, 0.76f); k.bi = 0.32f; k.li = 0.2f; break;
            }
            var w = Weather.Current;
            if (w == WeatherKind.Rain)   { k.gi *= 0.86f; k.w = Color.Lerp(k.w, new Color(0.62f, 0.72f, 0.9f), 0.7f); k.wi *= 0.6f; k.li += 0.3f; }
            if (w == WeatherKind.Cloudy) { k.gi *= 0.94f; k.w = Color.Lerp(k.w, new Color(0.85f, 0.88f, 0.95f), 0.5f); k.wi *= 0.75f; k.li += 0.15f; }
            // 햇살은 SunbeamSpot이 보일 때만(맑은 날·정산 전) — 그 그림의 투명도를 그대로 따른다
            k.bi *= beamSr != null ? beamSr.color.a : 0f;
            return k;
        }

        void Apply(float k)
        {
            var t = Target();
            Tween(global, t.g, t.gi, k);
            Tween(window, t.w, t.wi, k);
            Tween(beam, t.b, t.bi, 1f);                       // 그림 투명도를 이미 천천히 따라가므로 바로 맞춘다
            foreach (var l in lamps) Tween(l, new Color(1f, 0.78f, 0.5f), t.li, k);
            foreach (var p in pools) Tween(p, new Color(1f, 0.74f, 0.45f), Mathf.Max(0f, t.li - 0.25f) * 0.45f, k);   // 낮엔 꺼짐, 저녁·밤에만
            float ha = Mathf.Clamp01((t.li - 0.25f) / 0.8f) * 0.9f;        // 불이 켜질수록 번짐이 보인다
            foreach (var h in halos)
                if (h != null) { var c = h.color; c.a = Mathf.Lerp(c.a, ha, Mathf.Clamp01(k)); h.color = c; }
        }

        static void Tween(Light2D l, Color c, float i, float k)
        {
            if (l == null) return;
            k = Mathf.Clamp01(k);
            l.color = Color.Lerp(l.color, c, k);
            l.intensity = Mathf.Lerp(l.intensity, i, k);
        }
    }
}
