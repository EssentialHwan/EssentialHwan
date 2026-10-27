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
        // 천장 조명은 옮기고 빼고 더 살 수 있다(기본 사물) → 조명마다 빛 세트를 붙여 따라가게 한다
        class LampRig { public Transform lamp; public Light2D light, pool; public SpriteRenderer halo, cord; }
        readonly List<LampRig> rigs = new();
        float rigScanT;
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

            // 창빛: 유리 아래쪽 가운데에서 넓고 부드럽게(노멀맵 X — 창 주변이 은은하게 밝아지는 정도). 가장 큰 창을 따라간다
            window = Make("WindowGlow", Light2D.LightType.Point,
                new Vector3(CafeLayout.GlassCenterX, CafeLayout.GlassBottom + CafeLayout.GlassHeight * 0.45f, 0f), false);
            window.pointLightInnerRadius = 2.2f; window.pointLightOuterRadius = 7.5f; window.falloffIntensity = 0.7f;
            SyncRigs();
            ContactShadows.Ensure(litMat);

            Apply(1f);
            ScanSprites();
        }

        // 조명(기본 사물 'Lamp')마다 빛·바닥 웅덩이·번짐을 붙이고, 사라진 조명의 것은 치운다. 창빛은 가장 큰 창으로.
        void SyncRigs()
        {
            var lamps = new List<Transform>(YeogiCafe.Cafe.FixtureManager.Lamps());
            for (int i = rigs.Count - 1; i >= 0; i--)
            {
                var r = rigs[i];
                if (r.lamp != null && lamps.Contains(r.lamp)) continue;
                if (r.light != null) Destroy(r.light.gameObject);
                if (r.pool != null) Destroy(r.pool.gameObject);
                if (r.halo != null) Destroy(r.halo.gameObject);
                if (r.cord != null) Destroy(r.cord.gameObject);
                rigs.RemoveAt(i);
            }
            foreach (var t in lamps)
            {
                if (rigs.Exists(r => r.lamp == t)) continue;
                var r = new LampRig { lamp = t };
                // 노멀맵 O — 고양이·가구 윗면이 위에서 빛을 받는다
                r.light = Make("LampLight", Light2D.LightType.Point, Vector3.zero, true);
                r.light.pointLightInnerRadius = 0.4f; r.light.pointLightOuterRadius = 5.2f; r.light.falloffIntensity = 0.8f;
                r.pool = Make("LampPool", Light2D.LightType.Point, Vector3.zero, false);
                r.pool.pointLightInnerRadius = 0.6f; r.pool.pointLightOuterRadius = 2.9f; r.pool.falloffIntensity = 0.6f;
                r.halo = Halo(Vector3.zero, t.GetComponent<SpriteRenderer>());
                r.cord = Cord(t.GetComponent<SpriteRenderer>());
                rigs.Add(r);
            }
            foreach (var r in rigs)
            {
                var sr = r.lamp.GetComponent<SpriteRenderer>();
                float x = r.lamp.position.x, sy = sr != null ? sr.bounds.min.y : CafeLayout.CeilingY - 1.0f;
                r.light.transform.position = new Vector3(x, sy - 0.1f, 0f);
                r.pool.transform.position = new Vector3(x, 2.9f, 0f);
                r.halo.transform.position = new Vector3(x, sy - 0.05f, 0f);
                // Lv3(위로 넓힘): 캣워크·다락 아래가 아니면 매달 데가 없어 갓이 허공에 떠 보였다 → 화면 위 천장까지 줄을 늘인다
                bool hangsFree = CafeLayout.ViewLevel >= 3 && sr != null
                                 && (x < YeogiCafe.Visual.LoftView.CatwalkLeft || x > CafeLayout.DeckRight);
                r.cord.enabled = hangsFree;
                if (hangsFree)
                {
                    var cam = Camera.main;
                    float top = cam != null ? cam.transform.position.y + cam.orthographicSize + 0.5f : CafeLayout.CeilingY + 3f;
                    float from = sr.bounds.max.y - 0.05f;
                    r.cord.transform.position = new Vector3(x + 0.5f / 32f, from, 0f);   // 그림 속 줄(16px 중 8번째 칸)과 같은 x
                    r.cord.transform.localScale = new Vector3(1f, Mathf.Max(0f, top - from) * 32f, 1f);
                    r.cord.sortingOrder = sr.sortingOrder - 1;
                }
            }
            var g = YeogiCafe.Cafe.FixtureManager.PrimaryGlass();
            if (window != null)
            {
                window.gameObject.SetActive(g != null);
                if (g != null) window.transform.position = new Vector3(g.position.x, CafeLayout.GlassBottom + CafeLayout.GlassHeight * 0.45f, 0f);
            }
        }

        // 천장 줄: 조명 그림 속 줄과 같은 색 1px 세로선(아래 끝이 기준점 — 위로 늘인다)
        SpriteRenderer Cord(SpriteRenderer shade)
        {
            var go = new GameObject("LampCord"); go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (cordSprite == null)
            {
                // 조명 그림 속 줄과 같은 모양: 테두리-갈색-테두리 3px
                var tex = new Texture2D(3, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "lamp_cord" };
                tex.SetPixels32(new Color32[] { YeogiCafe.Art.PixelPalette.Outline, YeogiCafe.Art.PixelPalette.WoodDeep, YeogiCafe.Art.PixelPalette.Outline }); tex.Apply();
                cordSprite = Sprite.Create(tex, new Rect(0, 0, 3, 1), new Vector2(0.5f, 0f), 32f);
            }
            sr.sprite = cordSprite;
            sr.enabled = false;
            return sr;
        }
        static Sprite cordSprite;

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
            rigScanT -= Time.unscaledDeltaTime;
            if (rigScanT <= 0f) { rigScanT = 0.1f; SyncRigs(); }     // 배치 모드에서 끄는 동안에도 빛이 따라오게
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
            float ha = Mathf.Clamp01((t.li - 0.25f) / 0.8f) * 0.9f;        // 불이 켜질수록 번짐이 보인다
            foreach (var r in rigs)
            {
                Tween(r.light, new Color(1f, 0.78f, 0.5f), t.li, k);
                Tween(r.pool, new Color(1f, 0.74f, 0.45f), Mathf.Max(0f, t.li - 0.25f) * 0.45f, k);   // 낮엔 꺼짐, 저녁·밤에만
                var h = r.halo;
                if (h != null) { var c = h.color; c.a = Mathf.Lerp(c.a, ha, Mathf.Clamp01(k)); h.color = c; }
            }
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
