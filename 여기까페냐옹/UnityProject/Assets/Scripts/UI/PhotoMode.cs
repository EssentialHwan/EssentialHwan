using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Core;
using YeogiCafe.Loc;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 포토 모드(P): 시간이 멈추고 UI가 사라진 카페를 찍는다.
    //  · 촬영 화면: 방향키/드래그 이동 · 휠 확대 · Space 찰칵
    //  · 꾸미기: 필터 6종 · 프레임 5종(폴라로이드 외엔 골드로 구매) · 날짜 도장 · 이름표 → 앨범에 PNG 저장
    //  · 앨범: 넘겨 보기 · 액자에 걸기(인화비) · 삭제 · 칸 늘리기(골드) · 폴더 열기
    public class PhotoMode
    {
        public const int HangPrice = 50, AlbumStep = 12, AlbumMax = 60;
        public static int ExpandPrice(int cap) => 200 + 100 * ((cap - 12) / AlbumStep);

        readonly GameUiController owner;
        readonly Canvas canvas;
        readonly GameObject liveGo, editGo, albumGo;
        readonly Image flash;
        float flashT;
        readonly Text toast; readonly RectTransform toastBg; float toastT;

        // 메인 UI(알림 포함)는 숨겨져 있으니 포토 모드 위에 따로 띄운다
        void Toast(string msg)
        {
            toast.text = msg;
            toastBg.sizeDelta = new Vector2(Mathf.Min(900f, toast.preferredWidth + 48f), 48f);
            toastBg.gameObject.SetActive(true); toastBg.SetAsLastSibling();
            toastT = 2.6f;
        }

        enum Mode { Off, Live, Edit, Album }
        Mode mode = Mode.Off;
        public bool IsOpen => mode != Mode.Off;

        Camera cam; Vector3 camPos0; float camSize0, prevTimeScale = 1f;
        bool dragging; Vector3 lastMouse;
        bool busy;

        // 꾸미기 상태
        Texture2D raw, filtered;
        readonly List<string> shotCats = new();
        string filter = "none", frame = "polaroid";
        bool stamp = true, names = true;
        RectTransform card, optRoot;

        // 앨범 상태
        int page; string viewing; bool deleteArmed;
        RectTransform albumPaper;

        SaveGame Save => owner.Save;

        public PhotoMode(GameUiController owner)
        {
            this.owner = owner;
            var go = new GameObject("[PhotoCanvas]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(owner.transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1280, 720);
            sc.matchWidthOrHeight = 0.5f;

            liveGo = BuildLive(go.transform);
            editGo = Full("Edit", go.transform, new Color(0.08f, 0.06f, 0.1f, 0.82f));
            albumGo = Full("Album", go.transform, new Color(0.08f, 0.06f, 0.1f, 0.7f));
            flash = Full("Flash", go.transform, new Color(1, 1, 1, 0)).GetComponent<Image>();
            flash.raycastTarget = false;
            flash.gameObject.SetActive(true);
            toastBg = UiFactory.Panel("Toast", go.transform, new Color(1f, 0.97f, 0.88f, 0.97f));
            UiFactory.Frame(toastBg, UiFactory.FrameStyle.Paper);
            UiFactory.SetRect(toastBg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(400, 48));
            toast = UiFactory.Label("T", toastBg, "", 18, TextAnchor.MiddleCenter, UiFactory.Ink);
            toast.horizontalOverflow = HorizontalWrapMode.Overflow; toast.raycastTarget = false;
            UiFactory.Anchor((RectTransform)toast.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            toastBg.GetComponent<Image>().raycastTarget = false;
            toastBg.gameObject.SetActive(false);
            canvas.gameObject.SetActive(false);
        }

        static GameObject Full(string name, Transform parent, Color c)
        {
            var p = UiFactory.Panel(name, parent, c);
            UiFactory.Anchor(p, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        static Button Btn(Transform parent, string text, Color bg, Color fg, Vector2 pos, Vector2 size, System.Action onClick, int font = 18, Vector2? anchor = null)
        {
            var b = UiFactory.Button("Btn", parent, text, bg, fg, out var lbl);
            lbl.fontSize = font; lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            var a = anchor ?? new Vector2(0.5f, 0.5f);
            UiFactory.SetRect((RectTransform)b.transform, a, a, new Vector2(0.5f, 0.5f), pos, size);
            b.onClick.AddListener(() => onClick());
            return b;
        }

        // ── 촬영 화면 ──
        GameObject BuildLive(Transform parent)
        {
            var root = new GameObject("Live", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            UiFactory.Anchor((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bar = UiFactory.Panel("Bar", root.transform, new Color(0.29f, 0.21f, 0.18f, 0.85f));
            UiFactory.SetRect(bar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(660, 106));
            UiFactory.Frame(bar, UiFactory.FrameStyle.Window);
            var hint = UiFactory.Label("Hint", bar, L.Get("photo.hint"), 15, TextAnchor.MiddleCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)hint.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(-60, 22));   // 창 테두리(위 ~14px) 아래
            hint.horizontalOverflow = HorizontalWrapMode.Overflow;
            Btn(bar, L.Get("photo.shoot"), UiFactory.PinkDeep, UiFactory.Cream, new Vector2(-200, -18), new Vector2(200, 44), Shoot, 20);
            Btn(bar, L.Get("photo.album"), UiFactory.Wood, UiFactory.Cream, new Vector2(10, -18), new Vector2(170, 44), OpenAlbum);
            Btn(bar, L.Get("photo.exit"), UiFactory.CreamDim, UiFactory.Ink, new Vector2(202, -18), new Vector2(170, 44), Exit);
            return root;
        }

        public void Enter()
        {
            if (IsOpen) return;
            cam = Camera.main; if (cam == null) return;
            camPos0 = cam.transform.position; camSize0 = cam.orthographicSize;
            prevTimeScale = Time.timeScale; Time.timeScale = 0f;
            YeogiCafe.Visual.CatInteractionDirector.Suspended = true;
            owner.SetMainUiVisible(false);
            canvas.gameObject.SetActive(true);
            SetMode(Mode.Live);
            YeogiCafe.Audio.SoundManager.Sfx("sfx_click", 0.5f);
        }

        public void Exit()
        {
            if (!IsOpen) return;
            SetMode(Mode.Off);
            canvas.gameObject.SetActive(false);
            if (cam != null) { cam.transform.position = camPos0; cam.orthographicSize = camSize0; }
            Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
            YeogiCafe.Visual.CatInteractionDirector.Suspended = false;
            owner.SetMainUiVisible(true);
            owner.RefreshHud();
        }

        void SetMode(Mode m)
        {
            mode = m;
            liveGo.SetActive(m == Mode.Live);
            editGo.SetActive(m == Mode.Edit);
            albumGo.SetActive(m == Mode.Album);
            flash.transform.SetAsLastSibling();
        }

        public void Tick(float dt)
        {
            if (!IsOpen) return;
            if (toastT > 0f) { toastT -= dt; if (toastT <= 0f) toastBg.gameObject.SetActive(false); }
            if (flashT > 0f) { flashT -= dt; flash.color = new Color(1, 1, 1, Mathf.Clamp01(flashT / 0.35f) * 0.85f); }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (mode == Mode.Edit) SetMode(Mode.Live);
                else if (mode == Mode.Album) { if (viewing != null) { viewing = null; RenderAlbum(); } else SetMode(Mode.Live); }
                else Exit();
                return;
            }
            if (mode != Mode.Live || busy) return;
            if (Input.GetKeyDown(KeyCode.Space)) { Shoot(); return; }
            if (Input.GetKeyDown(KeyCode.P)) { Exit(); return; }
            MoveCamera(dt);
        }

        void MoveCamera(float dt)
        {
            if (cam == null) return;
            float size = cam.orthographicSize;
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f) size = Mathf.Clamp(size - wheel * 0.3f, 2.0f, CafeLayout.CameraSize);
            var p = cam.transform.position;
            float spd = 5f * size / CafeLayout.CameraSize * dt;
            float h = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? 1 : 0);
            float v = (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) ? 1 : 0);
            p += new Vector3(h, v, 0) * spd;
            // 드래그(바 위가 아닐 때)
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (Input.GetMouseButtonDown(0) && (es == null || !es.IsPointerOverGameObject())) { dragging = true; lastMouse = Input.mousePosition; }
            if (!Input.GetMouseButton(0)) dragging = false;
            if (dragging)
            {
                var d = Input.mousePosition - lastMouse; lastMouse = Input.mousePosition;
                float k = 2f * size / Screen.height;
                p -= new Vector3(d.x * k, d.y * k, 0);
            }
            // 방 밖이 보이지 않게(기본 화면 범위 안)
            float S = CafeLayout.CameraSize, aspect = cam.aspect;
            var c0 = CafeLayout.CameraPos;
            float mx = (S - size) * aspect, my = S - size;
            p.x = Mathf.Clamp(p.x, c0.x - mx, c0.x + mx);
            p.y = Mathf.Clamp(p.y, c0.y - my, c0.y + my);
            p.z = c0.z;
            cam.orthographicSize = size;
            cam.transform.position = p;
        }

        // ── 찰칵 ──
        void Shoot() { if (!busy && mode == Mode.Live) owner.StartCoroutine(ShootCo()); }
        public void DebugShoot() { if (!IsOpen) Enter(); Shoot(); }   // UI 점검용

        IEnumerator ShootCo()
        {
            busy = true;
            liveGo.SetActive(false);
            yield return new WaitForEndOfFrame();
            var full = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            full.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); full.Apply();
            if (raw != null) Object.Destroy(raw);
            raw = PhotoKit.Resize(full, 960, 540);
            Object.Destroy(full);
            // 찍힌 고양이(화면 안)
            shotCats.Clear();
            var cats = owner.Cats;
            if (cats != null && cam != null)
                foreach (var b in cats.Active)
                {
                    if (b == null || b.IsSpecialGuest || b.data == null) continue;
                    var vp = cam.WorldToViewportPoint(b.transform.position + Vector3.up * 0.4f);
                    if (vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f && !shotCats.Contains(b.data.catId)) shotCats.Add(b.data.catId);
                }
            flashT = 0.35f;
            YeogiCafe.Audio.SoundManager.Sfx("sfx_click", 0.9f, 0f);
            busy = false;
            filter = "none";
            OpenEditor();
        }

        // ── 꾸미기 ──
        void OpenEditor()
        {
            SetMode(Mode.Edit);
            ApplyFilter();
            BuildOptions();
        }

        void ApplyFilter()
        {
            if (filtered != null) Object.Destroy(filtered);
            filtered = filter == "none" ? null : PhotoKit.Apply(raw, filter);
            BuildCard();
        }

        Texture PhotoTex => filtered != null ? (Texture)filtered : raw;

        static readonly Color Paper = new(0.98f, 0.97f, 0.94f, 1f);
        static readonly Color DateOrange = new(1f, 0.62f, 0.22f, 1f);

        void BuildCard()
        {
            if (card != null) Object.Destroy(card.gameObject);
            const float pw = 576, ph = 324;
            float l, r, t, b; Color bg;
            switch (frame)
            {
                case "wood": l = r = t = b = 26; bg = UiFactory.Wood; break;
                case "postcard": l = r = t = 20; b = 58; bg = new Color(0.96f, 0.92f, 0.84f, 1f); break;
                case "film": l = r = 18; t = b = 40; bg = new Color(0.12f, 0.11f, 0.12f, 1f); break;
                case "gold": l = r = t = b = 30; bg = new Color(0.86f, 0.69f, 0.3f, 1f); break;
                case "heart": l = r = t = 24; b = 78; bg = new Color(1f, 0.89f, 0.9f, 1f); break;   // 보호소 하트(후원 보상)
                default: l = r = t = 20; b = 78; bg = Paper; break;     // 폴라로이드
            }
            card = UiFactory.Panel("Card", editGo.transform, bg);
            float cw = pw + l + r, ch = ph + t + b;
            UiFactory.SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 360 - 24 - ch * 0.5f), new Vector2(cw, ch));

            // 테두리 장식
            if (frame == "wood" || frame == "gold")
            {
                var dark = frame == "wood" ? UiFactory.WoodDark : new Color(0.62f, 0.46f, 0.15f, 1f);
                var line = UiFactory.Panel("Inner", card, dark);
                UiFactory.SetRect(line, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(l - 6, b - 6), new Vector2(pw + 12, ph + 12));
                var hi = UiFactory.Panel("Hi", card, frame == "wood" ? new Color(0.84f, 0.7f, 0.54f, 1f) : new Color(1f, 0.88f, 0.55f, 1f));
                UiFactory.SetRect(hi, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(-12, 4));
            }
            if (frame == "film")
                for (float x = 14; x < cw - 14; x += 34)
                    foreach (float y in new[] { b * 0.5f, ch - t * 0.5f })
                    {
                        var hole = UiFactory.Panel("Hole", card, new Color(0.93f, 0.9f, 0.84f, 1f));
                        UiFactory.SetRect(hole, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(x + 7, y), new Vector2(14, 10));
                    }

            var photoGo = new GameObject("Photo", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            photoGo.transform.SetParent(card, false);
            var ri = photoGo.GetComponent<RawImage>(); ri.texture = PhotoTex; ri.raycastTarget = false;
            UiFactory.SetRect((RectTransform)photoGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(l, b), new Vector2(pw, ph));

            if (frame == "heart")
                foreach (var corner in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) })
                {
                    var h = UiFactory.Label("Heart", card, "♥", 22, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
                    UiFactory.SetRect((RectTransform)h.transform, corner, corner, new Vector2(0.5f, 0.5f),
                                      new Vector2(corner.x < 0.5f ? 12 : -12, corner.y < 0.5f ? 12 : -12), new Vector2(24, 24));
                }
            if (frame == "postcard")
            {
                var st = UiFactory.Panel("PostStamp", card, UiFactory.Pink);
                UiFactory.SetRect(st, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(54, 62));
                var stIn = UiFactory.Panel("In", st, UiFactory.Cream);
                UiFactory.Anchor(stIn, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));
                var paw = UiFactory.Label("Paw", stIn, "♥", 26, TextAnchor.MiddleCenter, UiFactory.PinkDeep);
                UiFactory.Anchor((RectTransform)paw.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            string dateText = DateText();
            string nameText = NamesText();
            bool captionUnder = frame == "polaroid" || frame == "postcard" || frame == "heart";
            if (captionUnder)
            {
                var cap = UiFactory.Label("Caption", card, names ? nameText : "", 22, TextAnchor.MiddleLeft, UiFactory.Ink);
                UiFactory.SetRect((RectTransform)cap.transform, Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(l + 6, 0), new Vector2(-(l + r + 12), b));
                cap.horizontalOverflow = HorizontalWrapMode.Overflow;
                var date = UiFactory.Label("Date", card, stamp ? dateText : "", 18, TextAnchor.MiddleRight, UiFactory.WoodDark);
                UiFactory.SetRect((RectTransform)date.transform, Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(l + 6, 0), new Vector2(-(l + r + 12), b));
                date.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (names && stamp) UiFactory.FitOneLine(cap, pw - date.preferredWidth - 24, 14);
            }
            else
            {
                // 사진 위 오렌지 날짜(필름 카메라 느낌) + 왼쪽 아래 이름표
                if (stamp)
                {
                    var date = UiFactory.Label("Date", photoGo.transform, dateText, 20, TextAnchor.LowerRight, DateOrange);
                    UiFactory.Anchor((RectTransform)date.transform, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8));
                    var sh = date.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(2, -2);
                }
                if (names && nameText.Length > 0)
                {
                    var tagBg = UiFactory.Panel("NameTag", photoGo.transform, new Color(1f, 0.97f, 0.9f, 0.9f));
                    var tag = UiFactory.Label("T", tagBg, nameText, 16, TextAnchor.MiddleCenter, UiFactory.Ink);
                    tag.horizontalOverflow = HorizontalWrapMode.Overflow;
                    UiFactory.FitOneLine(tag, 340, 12);
                    UiFactory.Anchor((RectTransform)tag.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    UiFactory.SetRect(tagBg, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(10, 10), new Vector2(tag.preferredWidth + 20, 26));
                }
            }
        }

        string DateText()
        {
            var s = Save;
            int day = s != null ? s.currentDay : 1;
            return L.Get("ui.cafe.day", ("n", day.ToString())) + " · " + L.Get("weather." + (int)Weather.Current);
        }

        string NamesText()
        {
            var parts = new List<string>();
            foreach (var id in shotCats) parts.Add(L.Get($"cat.{id}.name"));
            return string.Join(" · ", parts);
        }

        bool FrameOwned(string id) => id == "polaroid" || (Save != null && Save.photoFrames.Contains(id));

        void BuildOptions()
        {
            if (optRoot != null) Object.Destroy(optRoot.gameObject);
            optRoot = UiFactory.Panel("Options", editGo.transform, new Color(1f, 0.97f, 0.9f, 0.97f));
            UiFactory.SetRect(optRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(980, 176));
            UiFactory.Frame(optRoot, UiFactory.FrameStyle.Paper);

            Row(L.Get("photo.filter"), -34);
            float x = -330;
            foreach (var f in PhotoKit.Filters)
            {
                string id = f;
                bool on = filter == id;
                Btn(optRoot, L.Get("photo.filter." + id), on ? UiFactory.PinkDeep : UiFactory.Wood, UiFactory.Cream, new Vector2(x, -34), new Vector2(116, 34),
                    () => { filter = id; ApplyFilter(); BuildOptions(); }, 15, new Vector2(0.5f, 1));
                x += 124;
            }

            Row(L.Get("photo.frame"), -78);
            x = -340;
            foreach (var fd in PhotoKit.Frames)
            {
                var d = fd;
                bool own = FrameOwned(d.id), on = frame == d.id;
                string name = L.Get("photo.frame." + d.id);
                if (d.price < 0 && !own) name = L.Get("photo.frame.locked");
                string label = own || d.price < 0 ? name : L.Get("photo.frame.buy", ("name", name), ("g", d.price.ToString()));
                Btn(optRoot, label, on ? UiFactory.PinkDeep : own ? UiFactory.Wood : UiFactory.CreamDim, on || own ? UiFactory.Cream : UiFactory.WoodDark,
                    new Vector2(x + 31, -78), new Vector2(130, 34), () => PickFrame(d), 14, new Vector2(0.5f, 1));
                x += 136;
            }

            Btn(optRoot, L.Get(stamp ? "photo.stamp.on" : "photo.stamp.off"), stamp ? UiFactory.Green : UiFactory.CreamDim, UiFactory.Ink,
                new Vector2(-330, -128), new Vector2(170, 38), () => { stamp = !stamp; BuildCard(); BuildOptions(); }, 16, new Vector2(0.5f, 1));
            Btn(optRoot, L.Get(names ? "photo.names.on" : "photo.names.off"), names ? UiFactory.Green : UiFactory.CreamDim, UiFactory.Ink,
                new Vector2(-150, -128), new Vector2(170, 38), () => { names = !names; BuildCard(); BuildOptions(); }, 16, new Vector2(0.5f, 1));
            Btn(optRoot, L.Get("photo.retake"), UiFactory.CreamDim, UiFactory.Ink, new Vector2(200, -128), new Vector2(170, 38), () => SetMode(Mode.Live), 17, new Vector2(0.5f, 1));
            Btn(optRoot, L.Get("photo.save"), UiFactory.PinkDeep, UiFactory.Cream, new Vector2(385, -128), new Vector2(180, 38), SaveCard, 18, new Vector2(0.5f, 1));
        }

        void Row(string label, float y)
        {
            var t = UiFactory.Label("Row", optRoot, label, 16, TextAnchor.MiddleLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(20, y), new Vector2(84, 30));
            UiFactory.FitOneLine(t, 84, 12);
        }

        void PickFrame(PhotoKit.FrameDef d)
        {
            if (!FrameOwned(d.id) && d.price < 0) { Toast(L.Get("photo.frame.lockedhint")); return; }
            if (!FrameOwned(d.id))
            {
                var eco = owner.Economy;
                if (eco == null || Save == null || !eco.TrySpend(d.price)) { Toast(L.Get("ui.shop.insufficient")); return; }
                Save.photoFrames.Add(d.id);
                owner.RefreshHud();
                Toast(L.Get("photo.frame.bought", ("name", L.Get("photo.frame." + d.id))));
                YeogiCafe.Audio.SoundManager.Sfx("sfx_coin", 0.6f);
            }
            frame = d.id;
            BuildCard(); BuildOptions();
        }

        void SaveCard()
        {
            if (busy || Save == null) return;
            if (Save.photos.Count >= Save.albumCap) { Toast(L.Get("photo.full")); return; }
            owner.StartCoroutine(SaveCo());
        }

        IEnumerator SaveCo()
        {
            busy = true;
            optRoot.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame();
            var corners = new Vector3[4];
            card.GetWorldCorners(corners);                       // 오버레이 캔버스: 월드 = 화면 픽셀
            int x0 = Mathf.Clamp(Mathf.RoundToInt(corners[0].x), 0, Screen.width - 1), y0 = Mathf.Clamp(Mathf.RoundToInt(corners[0].y), 0, Screen.height - 1);
            int x1 = Mathf.Clamp(Mathf.RoundToInt(corners[2].x), x0 + 1, Screen.width), y1 = Mathf.Clamp(Mathf.RoundToInt(corners[2].y), y0 + 1, Screen.height);
            var tex = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x0, y0, x1 - x0, y1 - y0), 0, 0); tex.Apply();
            string file = PhotoKit.SavePng(tex.EncodeToPNG());
            Object.Destroy(tex);
            optRoot.gameObject.SetActive(true);

            // 벽 액자용 인화본: 필터만 입힌 사진(테두리 없이) 작게
            var src = filtered != null ? filtered : raw;
            var small = PhotoKit.Resize(src, 352, 198);
            string print = PhotoKit.SavePng(small.EncodeToPNG(), "_print");
            Object.Destroy(small);
            var dto = new PhotoDTO { file = file, print = print, day = Save.currentDay, weather = (int)Weather.Current, frame = frame, filter = filter };
            dto.cats.AddRange(shotCats);
            Save.photos.Add(dto);
            if (shotCats.Count >= 1) GameEvents.Raise(GameEvents.Photo, shotCats[0]);
            if (shotCats.Count >= 2) GameEvents.Raise(GameEvents.PhotoGroup, shotCats[0]);
            Toast(L.Get("photo.saved", ("n", Save.photos.Count.ToString()), ("cap", Save.albumCap.ToString())));
            YeogiCafe.Audio.SoundManager.Sfx("sfx_chime", 0.5f);
            busy = false;
            SetMode(Mode.Live);
        }

        // ── 앨범 ──
        const int PerPage = 8;

        public void OpenAlbum()
        {
            if (!IsOpen) Enter();
            viewing = null; deleteArmed = false;
            SetMode(Mode.Album);
            RenderAlbum();
        }

        void RenderAlbum()
        {
            if (albumPaper != null) Object.Destroy(albumPaper.gameObject);
            var s = Save;
            albumPaper = UiFactory.Panel("Paper", albumGo.transform, new Color(1f, 0.97f, 0.9f, 1f));
            UiFactory.SetRect(albumPaper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 620));
            UiFactory.Frame(albumPaper, UiFactory.FrameStyle.Paper, null, 3f);
            if (s == null) return;
            if (viewing != null) { RenderViewer(); return; }

            var title = UiFactory.Label("Title", albumPaper, L.Get("photo.album.title", ("n", s.photos.Count.ToString()), ("cap", s.albumCap.ToString())), 26, TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(-40, 36));

            int pages = Mathf.Max(1, Mathf.CeilToInt(s.photos.Count / (float)PerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            if (s.photos.Count == 0)
            {
                var e = UiFactory.Label("Empty", albumPaper, L.Get("photo.album.empty"), 18, TextAnchor.MiddleCenter, UiFactory.WoodDark);
                UiFactory.Anchor((RectTransform)e.transform, Vector2.zero, Vector2.one, new Vector2(40, 80), new Vector2(-40, -80));
            }
            for (int i = 0; i < PerPage; i++)
            {
                int idx = s.photos.Count - 1 - (page * PerPage + i);        // 최근 것부터
                if (idx < 0) break;
                var p = s.photos[idx];
                int col = i % 4, row = i / 4;
                var cell = UiFactory.Button("Thumb", albumPaper, "", UiFactory.Cream, UiFactory.Ink, out var empty);
                Object.Destroy(empty.gameObject);
                UiFactory.SetRect((RectTransform)cell.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                                  new Vector2(-330 + col * 220, -74 - row * 212), new Vector2(204, 196));
                var ri = new GameObject("Img", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                ri.transform.SetParent(cell.transform, false);
                var img = ri.GetComponent<RawImage>(); img.raycastTarget = false;
                var tex = PhotoKit.Load(p.file); img.texture = tex;
                FitInto((RectTransform)ri.transform, tex, new Vector2(184, 150), new Vector2(0, 14));
                var cap = UiFactory.Label("Day", cell.transform, L.Get("ui.cafe.day", ("n", p.day.ToString())), 14, TextAnchor.LowerCenter, UiFactory.WoodDark);
                UiFactory.Anchor((RectTransform)cap.transform, Vector2.zero, Vector2.one, new Vector2(6, 4), new Vector2(-6, -4));
                string file = p.file;
                cell.onClick.AddListener(() => { viewing = file; deleteArmed = false; RenderAlbum(); });
            }

            if (pages > 1)
            {
                Btn(albumPaper, "<", UiFactory.Wood, UiFactory.Cream, new Vector2(-70, 96), new Vector2(48, 36), () => { page--; RenderAlbum(); }, 20, new Vector2(0.5f, 0));
                var pl = UiFactory.Label("Page", albumPaper, $"{page + 1}/{pages}", 18, TextAnchor.MiddleCenter, UiFactory.Ink);
                UiFactory.SetRect((RectTransform)pl.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 96), new Vector2(80, 30));
                Btn(albumPaper, ">", UiFactory.Wood, UiFactory.Cream, new Vector2(70, 96), new Vector2(48, 36), () => { page++; RenderAlbum(); }, 20, new Vector2(0.5f, 0));
            }

            bool canExpand = s.albumCap < AlbumMax;
            Btn(albumPaper, canExpand ? L.Get("photo.album.expand", ("g", ExpandPrice(s.albumCap).ToString())) : L.Get("photo.album.max"),
                canExpand ? UiFactory.PinkDeep : UiFactory.CreamDim, canExpand ? UiFactory.Cream : UiFactory.WoodDark,
                new Vector2(-270, 44), new Vector2(260, 42), Expand, 17, new Vector2(0.5f, 0));
            Btn(albumPaper, L.Get("photo.album.folder"), UiFactory.Wood, UiFactory.Cream, new Vector2(0, 44), new Vector2(200, 42),
                () => { System.IO.Directory.CreateDirectory(PhotoKit.Dir); Application.OpenURL("file:///" + PhotoKit.Dir.Replace('\\', '/')); }, 17, new Vector2(0.5f, 0));
            Btn(albumPaper, L.Get("photo.close"), UiFactory.Green, UiFactory.Ink, new Vector2(270, 44), new Vector2(200, 42), () => SetMode(Mode.Live), 18, new Vector2(0.5f, 0));
        }

        static void FitInto(RectTransform rt, Texture tex, Vector2 box, Vector2 offset)
        {
            float w = box.x, h = box.y;
            if (tex != null && tex.width > 0) { float k = Mathf.Min(box.x / tex.width, box.y / tex.height); w = tex.width * k; h = tex.height * k; }
            UiFactory.SetRect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, new Vector2(w, h));
        }

        void Expand()
        {
            var s = Save; if (s == null || s.albumCap >= AlbumMax) return;
            int price = ExpandPrice(s.albumCap);
            if (owner.Economy == null || !owner.Economy.TrySpend(price)) { Toast(L.Get("ui.shop.insufficient")); return; }
            s.albumCap = Mathf.Min(AlbumMax, s.albumCap + AlbumStep);
            owner.RefreshHud();
            Toast(L.Get("photo.album.expanded", ("cap", s.albumCap.ToString())));
            YeogiCafe.Audio.SoundManager.Sfx("sfx_coin", 0.6f);
            RenderAlbum();
        }

        void RenderViewer()
        {
            var s = Save;
            var p = s.photos.Find(x => x.file == viewing);
            if (p == null) { viewing = null; RenderAlbum(); return; }
            var ri = new GameObject("Big", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            ri.transform.SetParent(albumPaper, false);
            var img = ri.GetComponent<RawImage>(); img.raycastTarget = false;
            var tex = PhotoKit.Load(p.file); img.texture = tex;
            FitInto((RectTransform)ri.transform, tex, new Vector2(820, 440), new Vector2(0, 50));

            var names = new List<string>();
            foreach (var id in p.cats) names.Add(L.Get($"cat.{id}.name"));
            string info = L.Get("ui.cafe.day", ("n", p.day.ToString())) + " · " + L.Get("weather." + p.weather)
                          + (names.Count > 0 ? "   " + string.Join(" · ", names) : "");
            var il = UiFactory.Label("Info", albumPaper, info, 17, TextAnchor.MiddleCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)il.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(-60, 26));

            bool hung = false;
            foreach (var f in s.placedFurniture) if (f.photo == p.file) hung = true;
            Btn(albumPaper, hung ? L.Get("photo.hung") : L.Get("photo.hang", ("g", HangPrice.ToString())), hung ? UiFactory.CreamDim : UiFactory.PinkDeep, hung ? UiFactory.WoodDark : UiFactory.Cream,
                new Vector2(-250, 36), new Vector2(240, 42), () => { if (!hung) Hang(p); }, 17, new Vector2(0.5f, 0));
            Btn(albumPaper, deleteArmed ? L.Get("photo.delete.confirm") : L.Get("photo.delete"), deleteArmed ? UiFactory.PinkDeep : UiFactory.Wood, UiFactory.Cream,
                new Vector2(0, 36), new Vector2(220, 42), () => Delete(p), 17, new Vector2(0.5f, 0));
            Btn(albumPaper, L.Get("photo.back"), UiFactory.Green, UiFactory.Ink, new Vector2(250, 36), new Vector2(200, 42), () => { viewing = null; RenderAlbum(); }, 18, new Vector2(0.5f, 0));
        }

        void Hang(PhotoDTO p)
        {
            var fm = owner.Furniture;
            var frames = fm != null ? fm.PhotoFrames() : null;
            if (frames == null || frames.Count == 0) { Toast(L.Get("photo.hang.noframe")); return; }
            var target = frames.Find(f => string.IsNullOrEmpty(f.dto.photo));
            if (target.go == null) target = frames[0];                 // 다 차 있으면 첫 액자를 바꿔 건다
            if (owner.Economy == null || !owner.Economy.TrySpend(HangPrice)) { Toast(L.Get("ui.shop.insufficient")); return; }
            target.dto.photo = p.file;
            RefreshFrames(owner);
            owner.RefreshHud();
            GameEvents.Raise(GameEvents.PhotoHang, p.cats.Count > 0 ? p.cats[0] : null);
            Toast(L.Get("photo.hang.done"));
            YeogiCafe.Audio.SoundManager.Sfx("sfx_coin", 0.6f);
            RenderAlbum();
        }

        void Delete(PhotoDTO p)
        {
            if (!deleteArmed) { deleteArmed = true; RenderAlbum(); return; }
            deleteArmed = false;
            Save.photos.Remove(p);
            foreach (var f in Save.placedFurniture) if (f.photo == p.file) f.photo = null;
            PhotoKit.Delete(p.file);
            if (!string.IsNullOrEmpty(p.print)) PhotoKit.Delete(p.print);
            RefreshFrames(owner);
            Toast(L.Get("photo.deleted"));
            viewing = null;
            RenderAlbum();
        }

        // 벽 액자에 걸린 사진을 세이브와 맞춘다(시작·걸기·지우기·주기적으로)
        public static void RefreshFrames(GameUiController owner)
        {
            var fm = owner.Furniture; if (fm == null) return;
            foreach (var (go, dto) in fm.PhotoFrames())
            {
                var view = go.GetComponent<YeogiCafe.Visual.PhotoFrameView>();
                if (string.IsNullOrEmpty(dto.photo)) { if (view != null && view.Shown != null) view.Clear(); continue; }
                if (view == null) view = go.AddComponent<YeogiCafe.Visual.PhotoFrameView>();
                if (view.Shown == dto.photo) continue;
                var pd = owner.Save != null ? owner.Save.photos.Find(x => x.file == dto.photo) : null;
                var tex = pd != null && !string.IsNullOrEmpty(pd.print) ? PhotoKit.Load(pd.print) : null;
                if (tex == null) tex = PhotoKit.Load(dto.photo);
                if (tex == null) { dto.photo = null; view.Clear(); continue; }   // 파일이 지워졌으면 그림으로
                view.Show(tex, dto.photo);
            }
        }
    }
}
