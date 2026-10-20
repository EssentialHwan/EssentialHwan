using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.AI;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Data;
using YeogiCafe.Loc;
using YeogiCafe.Save;
using YeogiCafe.Visual;

namespace YeogiCafe.UI
{
    // 마감 직전 '마지막 손님 놀아주기' — 그 고양이를 크게 확대해 직접 만져 준다.
    //  · 드래그 = 쓰다듬기(머리·볼·턱·등·앞발), 등 위를 톡톡 = 꾹꾹이(내 앞발로 꾹·꾹)
    //  · 반응은 기질(TouchStyle)마다 다르고, 본 반응은 도감 '손길'과 좋아하는/싫어하는 곳으로 기록된다.
    //  · 기분이 가득 차면 골골송 피날레(쭙쭙이 버릇이 있는 고양이는 내 앞발을 물고 쭙쭙…) + 팁.
    //  · 싫어하는 곳을 계속 만지면 "흥!" 하고 가버린다.
    public class ClosingPlayPanel
    {
        const float Scale = 3.6f, Duration = 22f, StrokeStep = 24f;

        readonly GameUiController owner;
        readonly GameObject rootGo;
        readonly RectTransform root, catRt, gaugeFill, fxLayer;
        readonly Image catImg;
        readonly Text title, timerText, sub;
        readonly Button byeBtn;

        CatData cat; CatSaveData cs; TouchStyle style;
        System.Action onDone;
        float mood, timeLeft, t, blinkAt, faceHold, shake;
        int strikes; bool open, finale; float finaleT; bool suckle;
        CatCloseup.State face, baseFace;
        readonly Dictionary<PetZone, int> zoneCount = new();
        // 입력
        bool down; Vector2 downPos, lastPos; float downT, strokeAcc, lastEventT;
        int leanDir; float leanHold;
        Image suckPaw;

        public bool IsOpen => open;

        public ClosingPlayPanel(GameUiController owner, Transform canvas)
        {
            this.owner = owner;
            var scrim = UiFactory.Panel("ClosingPlay", canvas, new Color(0.13f, 0.09f, 0.11f, 0.86f));
            root = scrim; rootGo = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 가게 조명(고양이 뒤 따뜻한 빛 원)
            var glow = new GameObject("Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glow.transform.SetParent(root, false);
            var gimg = glow.GetComponent<Image>(); gimg.raycastTarget = false;
            gimg.sprite = RuntimeSprites.Get("closing_glow", GlowCircle); gimg.color = new Color(1f, 0.86f, 0.6f, 0.55f);
            UiFactory.SetRect((RectTransform)glow.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(900, 640));

            // 카운터 상판
            var counter = UiFactory.Panel("Counter", root, Color.white);
            UiFactory.Frame(counter, UiFactory.FrameStyle.Plank, new Color(0.86f, 0.66f, 0.46f, 1f), 3f);
            UiFactory.SetRect(counter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, -20), new Vector2(0, 150));
            counter.GetComponent<Image>().raycastTarget = false;

            // 고양이
            var cg = new GameObject("Cat", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cg.transform.SetParent(root, false);
            catRt = (RectTransform)cg.transform; catImg = cg.GetComponent<Image>();
            catImg.raycastTarget = false;
            UiFactory.SetRect(catRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(CatCloseup.W, CatCloseup.H) * Scale);

            fxLayer = (RectTransform)new GameObject("FX", typeof(RectTransform)).transform;
            fxLayer.SetParent(root, false);
            UiFactory.Anchor(fxLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 제목(종이) + 설명
            var paper = UiFactory.Panel("Head", root, Color.white);
            UiFactory.Frame(paper, UiFactory.FrameStyle.Paper);
            UiFactory.SetRect(paper, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(760, 92));
            title = UiFactory.Label("T", paper, "", 24, TextAnchor.UpperCenter, UiFactory.Ink);
            UiFactory.Anchor((RectTransform)title.transform, Vector2.zero, Vector2.one, new Vector2(16, 8), new Vector2(-16, -12));
            sub = UiFactory.Label("S", paper, L.Get("closing.sub"), 16, TextAnchor.LowerCenter, UiFactory.WoodDark);
            UiFactory.Anchor((RectTransform)sub.transform, Vector2.zero, Vector2.one, new Vector2(16, 10), new Vector2(-16, -8));

            // 기분 게이지
            var gauge = UiFactory.Panel("Gauge", root, Color.white);
            UiFactory.Frame(gauge, UiFactory.FrameStyle.Card, UiFactory.Cream);
            UiFactory.SetRect(gauge, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -118), new Vector2(420, 34));
            var gl = UiFactory.Label("L", gauge, L.Get("closing.mood"), 16, TextAnchor.MiddleLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)gl.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(60, 0));
            var gbg = UiFactory.Panel("Bg", gauge, new Color(0.9f, 0.84f, 0.8f, 1f));
            UiFactory.Anchor(gbg, Vector2.zero, Vector2.one, new Vector2(70, 9), new Vector2(-70, -9));
            gaugeFill = UiFactory.Panel("Fill", gbg, UiFactory.Pink);
            UiFactory.Anchor(gaugeFill, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            timerText = UiFactory.Label("Time", gauge, "", 16, TextAnchor.MiddleRight, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)timerText.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(60, 0));

            byeBtn = UiFactory.Button("Bye", root, L.Get("closing.bye"), UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)byeBtn.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(200, 54));
            byeBtn.onClick.AddListener(() => Finish(false));

            rootGo.SetActive(false);
        }

        static PixelCanvas GlowCircle()
        {
            var c = new PixelCanvas(64, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 32f;
                    if (d >= 1f) continue;
                    c.Set(x, y, new Color32(255, 255, 255, (byte)(255 * (1f - d) * (1f - d))));
                }
            return c;
        }

        public void Show(CatData data, CatSaveData save, System.Action done)
        {
            cat = data; cs = save; onDone = done;
            style = CatTemperament.Style(data);
            mood = 10f; timeLeft = Duration; t = 0f; strikes = 0; finale = false; suckle = false; finaleT = 0f;
            zoneCount.Clear(); down = false; shake = 0f; faceHold = 0f; leanDir = 0;
            if (suckPaw != null) { Object.Destroy(suckPaw.gameObject); suckPaw = null; }
            for (int i = fxLayer.childCount - 1; i >= 0; i--) Object.Destroy(fxLayer.GetChild(i).gameObject);
            baseFace = new CatCloseup.State { eyes = CatCloseup.Eyes.Open, tail = 1 };
            if (style == TouchStyle.Grumpy) baseFace.eyes = CatCloseup.Eyes.Half;     // 까칠: 시큰둥한 눈
            face = baseFace;
            blinkAt = Random.Range(2f, 4f);
            title.text = L.Get("closing.title", ("name", Name));
            sub.text = L.Get("closing.sub");
            byeBtn.interactable = true;
            open = true;
            rootGo.SetActive(true);
            rootGo.transform.SetAsLastSibling();
            CatInteractionDirector.Suspended = true;
            ApplyFace();
        }

        string Name => cat != null ? L.Get($"cat.{cat.catId}.name") : "";

        void ApplyFace()
        {
            string id = cat.catId; var s = face;
            catImg.sprite = RuntimeSprites.Get($"close_{id}_{s.Key}", () => CatCloseup.Draw(id, s));
        }

        public void Tick(float dt)
        {
            if (!open) return;
            t += dt;
            UpdateFx(dt);
            if (finale) { TickFinale(dt); return; }
            timeLeft -= dt;
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, timeLeft)).ToString();
            if (timeLeft <= 0f) { Finish(false); return; }
            HandleInput();

            // 표정 복귀·눈 깜빡임·고개 기울기·꼬리
            var want = baseFace;
            if (faceHold > 0f) { faceHold -= dt; want = face; }
            else
            {
                if (mood >= 70f) want.eyes = CatCloseup.Eyes.Half;
                blinkAt -= dt;
                if (blinkAt <= 0f) { want.eyes = CatCloseup.Eyes.Closed; if (blinkAt < -0.14f) blinkAt = Random.Range(2.5f, 5f); }
            }
            leanHold -= dt;
            want.lean = leanHold > 0f ? leanDir : 0;
            float tailSpeed = mood < 25f ? 7f : mood >= 70f ? 2f : 3.2f;
            int tp = (int)(t * tailSpeed) % 4; want.tail = tp == 3 ? 1 : tp;
            if (!want.Key.Equals(face.Key) || faceHold <= 0f) { face = want; ApplyFace(); }

            // 숨쉬기 + 화나면 부르르
            float br = 1f + Mathf.Sin(t * 2.4f) * 0.012f;
            catRt.localScale = new Vector3(1f, br, 1f);
            shake = Mathf.Max(0f, shake - dt);
            catRt.anchoredPosition = new Vector2(shake > 0f ? Mathf.Sin(t * 70f) * 6f * shake / 0.4f : 0f, 100f);
            gaugeFill.anchorMax = new Vector2(Mathf.Clamp01(mood / 100f), 1f);
        }

        // ── 입력: 드래그=쓰다듬기, 짧게 톡=꾹꾹이 ──
        void HandleInput()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            bool overButton = es != null && es.IsPointerOverGameObject() && IsOverButton();
            if (Input.GetMouseButtonDown(0) && !overButton)
            {
                if (ToCat(Input.mousePosition, out var p)) { down = true; downPos = lastPos = p; downT = t; strokeAcc = 0f; }
            }
            if (down && Input.GetMouseButton(0))
            {
                if (ToCat(Input.mousePosition, out var p))
                {
                    float d = (p - lastPos).magnitude;
                    strokeAcc += d;
                    if (Mathf.Abs(p.x - lastPos.x) > 0.5f) { leanDir = p.x > lastPos.x ? 1 : -1; }
                    lastPos = p;
                    if (strokeAcc >= StrokeStep && t - lastEventT > 0.16f)
                    {
                        strokeAcc = 0f;
                        var z = CatCloseup.ZoneAt(p.x, p.y, face.lean);
                        if (z != CatCloseup.Zone.None) Touch(z, PetAct.Stroke, p);
                    }
                }
            }
            if (down && Input.GetMouseButtonUp(0))
            {
                down = false;
                if (t - downT < 0.28f && (lastPos - downPos).magnitude < 8f)
                {
                    var z = CatCloseup.ZoneAt(downPos.x, downPos.y, face.lean);
                    if (z != CatCloseup.Zone.None) Touch(z, z == CatCloseup.Zone.Back || z == CatCloseup.Zone.Head ? PetAct.Knead : PetAct.Stroke, downPos);
                }
            }
        }

        bool IsOverButton()
        {
            var rt = (RectTransform)byeBtn.transform;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);
        }

        // 화면 좌표 → 클로즈업 캔버스 픽셀 좌표
        bool ToCat(Vector2 screen, out Vector2 p)
        {
            p = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(catRt, screen, null, out var local)) return false;
            var r = catRt.rect;
            p = new Vector2((local.x - r.xMin) / r.width * CatCloseup.W, (local.y - r.yMin) / r.height * CatCloseup.H);
            return p.x >= 0 && p.y >= 0 && p.x < CatCloseup.W && p.y < CatCloseup.H;
        }

        Vector2 CatToUi(Vector2 p)
        {
            var r = catRt.rect;
            Vector2 local = new Vector2(r.xMin + p.x / CatCloseup.W * r.width, r.yMin + p.y / CatCloseup.H * r.height);
            Vector3 world = catRt.TransformPoint(local);
            return fxLayer.InverseTransformPoint(world);
        }

        static PetZone Map(CatCloseup.Zone z) => z switch
        {
            CatCloseup.Zone.Head => PetZone.Head, CatCloseup.Zone.Cheek => PetZone.Cheek, CatCloseup.Zone.Chin => PetZone.Chin,
            CatCloseup.Zone.Paw => PetZone.Paw, _ => PetZone.Back
        };

        void Touch(CatCloseup.Zone z, PetAct act, Vector2 at)
        {
            lastEventT = t;
            var pz = Map(z);
            zoneCount.TryGetValue(pz, out int n); n++; zoneCount[pz] = n;
            var feel = CatTemperament.ReactClose(style, pz, act, n, cs != null ? cs.regularStage : 1, Random.value);
            mood = Mathf.Clamp(mood + CatTemperament.CloseMood(feel), 0f, 100f);
            Record(pz, feel);
            if (act == PetAct.Knead) { KneadPaw(at); YeogiCafe.Audio.SoundManager.Sfx("sfx_knead", 0.5f); }
            if (act == PetAct.Stroke && (pz == PetZone.Head || pz == PetZone.Cheek)) leanHold = 0.7f;

            var f = baseFace; faceHold = 0.9f;
            switch (feel)
            {
                case PetFeel.Love:
                    YeogiCafe.Audio.SoundManager.Sfx("sfx_trill", 0.5f, 0.1f, "close_love", 0.9f);
                    f.eyes = Random.value < 0.5f ? CatCloseup.Eyes.Happy : CatCloseup.Eyes.Closed;
                    Float(at, L.Get("closing.love." + Random.Range(1, 4)), UiFactory.PinkDeep); Heart(at); break;
                case PetFeel.Play:
                    YeogiCafe.Audio.SoundManager.Sfx("sfx_meow_small", 0.5f, 0.1f, "close_play", 0.8f);
                    f.eyes = CatCloseup.Eyes.Wide; f.mouthOpen = true;
                    Float(at, L.Get("closing.play." + Random.Range(1, 3)), UiFactory.WoodDark); break;
                case PetFeel.Ok:
                    f.eyes = CatCloseup.Eyes.Open;
                    Float(at, L.Get("closing.ok." + Random.Range(1, 3)), UiFactory.WoodDark); break;
                case PetFeel.Annoyed:
                    f.eyes = CatCloseup.Eyes.Angry; f.earsFlat = true; shake = 0.2f;
                    Float(at, L.Get("closing.annoyed." + Random.Range(1, 3)), UiFactory.WoodDark); break;
                default:
                    YeogiCafe.Audio.SoundManager.Sfx("sfx_hiss", 0.7f);
                    f.eyes = CatCloseup.Eyes.Angry; f.earsFlat = true; f.mouthOpen = true; shake = 0.4f; faceHold = 1.3f; strikes++;
                    Float(at, L.Get("closing.hate." + Random.Range(1, 3)), UiFactory.PinkDeep); break;
            }
            if (act == PetAct.Knead && feel != PetFeel.Hate) Float(at + new Vector2(0, 14), L.Get("closing.knead"), UiFactory.Wood);
            face = f; ApplyFace();

            if (strikes >= 2) { Leave(); return; }
            if (mood >= 100f) StartFinale();
        }

        void Record(PetZone z, PetFeel f)
        {
            if (cs == null) return;
            if (cs.touchSeen == null || cs.touchSeen.Length < CatTemperament.ResultCount) cs.touchSeen = new int[CatTemperament.ResultCount];
            cs.touchSeen[(int)CatTemperament.ToTouch(f)]++;
            int bit = 1 << (int)z;
            if (f == PetFeel.Love) cs.likeZones |= bit;
            if (f == PetFeel.Hate || f == PetFeel.Annoyed) cs.hateZones |= bit;
            GameEvents.Raise(f == PetFeel.Love ? GameEvents.PetLove : GameEvents.Pet, cat.catId);
        }

        // ── 피날레: 골골송(+ 쭙쭙이) ──
        void StartFinale()
        {
            finale = true; finaleT = 0f;
            byeBtn.interactable = false;
            suckle = CatTemperament.IsSuckler(cat);
            face = new CatCloseup.State { eyes = CatCloseup.Eyes.Closed, tail = 0 };
            ApplyFace();
            YeogiCafe.Audio.SoundManager.Loop("sfx_purr", true, 0.6f);
            sub.text = L.Get("closing.finale");
            if (suckle)
            {
                // 내 앞발을 물고 쭙쭙…(앞발 뒷모습을 고양이 입가에)
                var g = new GameObject("SuckPaw", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                g.transform.SetParent(root, false);
                suckPaw = g.GetComponent<Image>(); suckPaw.raycastTarget = false;
                suckPaw.sprite = PawArt.Has ? PawArt.Get(PawArt.Idle, false) : RuntimeSprites.Get("paw_right", CookingDrawing.PawRight);
                var rt = (RectTransform)g.transform;
                rt.pivot = new Vector2(0.5f, PawArt.Has ? PawArt.TipY(PawArt.Idle) - 0.08f : 0.85f);
                rt.sizeDelta = PawArt.Has ? PawArt.CellSize * 0.85f : new Vector2(PawDrawing.W, PawDrawing.H) * 2.2f;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(70, -200);
                rt.localEulerAngles = new Vector3(0, 0, 38f);
                sub.text = L.Get("closing.suckle", ("name", Name));
                if (cs != null) cs.suckleSeen = true;
                GameEvents.Raise(GameEvents.Suckle, cat.catId);
            }
        }

        void TickFinale(float dt)
        {
            finaleT += dt;
            gaugeFill.anchorMax = new Vector2(1f, 1f);
            float purr = 1f + Mathf.Sin(t * 30f) * 0.004f + Mathf.Sin(t * 2.4f) * 0.012f;   // 골골 진동
            catRt.localScale = new Vector3(1f, purr, 1f);
            catRt.anchoredPosition = new Vector2(0, 100f);
            if (((int)(finaleT * 2.2f)) != ((int)((finaleT - dt) * 2.2f)))
                Float(new Vector2(84 + Random.Range(-40f, 40f), 96), L.Get("closing.purr"), UiFactory.PinkDeep);
            if (suckPaw != null)
            {
                // 입가(캔버스 84,52)로 앞발을 들이밀고, 쭙쭙 리듬으로 살짝 당겨짐
                var target = CatToUi(new Vector2(84 + 9, 50));
                var prt = (RectTransform)suckPaw.transform;
                var parentPos = (Vector2)root.InverseTransformPoint(fxLayer.TransformPoint(target));
                float k = Mathf.Clamp01(finaleT / 0.6f);
                float suck = Mathf.Sin(finaleT * 9f) * 5f;
                Vector2 start = parentPos + new Vector2(160, -260);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.anchoredPosition = Vector2.Lerp(start, parentPos + new Vector2(30, -40 + suck), k);
                face.eyes = CatCloseup.Eyes.Closed; face.mouthOpen = false;
                if (((int)(finaleT * 1.6f)) != ((int)((finaleT - dt) * 1.6f)))
                { Float(new Vector2(110, 52), L.Get("closing.suck"), UiFactory.PinkDeep); YeogiCafe.Audio.SoundManager.Sfx("sfx_suck", 0.55f); }
            }
            if (finaleT >= (suckle ? 3.6f : 2.6f)) Finish(true);
        }

        void Leave()
        {
            sub.text = L.Get("closing.leave", ("name", Name));
            finale = false;
            open = false;
            CatInteractionDirector.Suspended = false;
            owner.ShowToast(L.Get("closing.leave", ("name", Name)));
            Close();
        }

        void Finish(bool happy)
        {
            if (!open) return;
            open = false;
            CatInteractionDirector.Suspended = false;
            if (cs != null) cs.closingPlays++;
            if (happy)
            {
                int tip = 10 + 5 * (cs != null ? cs.regularStage : 1);
                if (owner.Economy != null) owner.Economy.AddGold(tip);
                GameEvents.Raise(GameEvents.ClosingPlay, cat.catId);
                owner.ShowToast(L.Get("closing.done", ("name", Name), ("g", tip.ToString())) + "  " + ZoneSummary());
            }
            else if (mood >= 50f) owner.ShowToast(L.Get("closing.bye.ok", ("name", Name)) + "  " + ZoneSummary());
            Close();
        }

        string ZoneSummary()
        {
            if (cs == null || cs.likeZones == 0) return "";
            return L.Get("closing.likes", ("z", Zones(cs.likeZones)));
        }

        public static string Zones(int bits)
        {
            var parts = new List<string>();
            for (int i = 0; i < 5; i++) if ((bits & (1 << i)) != 0) parts.Add(L.Get("closing.zone." + i));
            return string.Join("·", parts);
        }

        void Close()
        {
            YeogiCafe.Audio.SoundManager.Loop("sfx_purr", false);
            if (suckPaw != null) { Object.Destroy(suckPaw.gameObject); suckPaw = null; }
            rootGo.SetActive(false);
            var d = onDone; onDone = null;
            d?.Invoke();
        }

        // ── 연출: 떠오르는 글자·하트·꾹꾹 앞발 ──
        class Fx { public RectTransform rt; public Graphic g; public float life, max; public Vector2 vel; }
        readonly List<Fx> fx = new();

        void Float(Vector2 catPos, string text, Color col)
        {
            var lbl = UiFactory.Label("F", fxLayer, text, 20, TextAnchor.MiddleCenter, col);
            lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            var sh = lbl.gameObject.AddComponent<Outline>(); sh.effectColor = new Color(1f, 0.98f, 0.93f, 0.9f); sh.effectDistance = new Vector2(2, -2);
            lbl.raycastTarget = false;
            var rt = (RectTransform)lbl.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(300, 30);
            rt.anchoredPosition = CatToUi(catPos) + new Vector2(Random.Range(-20f, 20f), 30f);
            fx.Add(new Fx { rt = rt, g = lbl, life = 1.3f, max = 1.3f, vel = new Vector2(0, 60) });
        }

        void Heart(Vector2 catPos)
        {
            var g = new GameObject("Heart", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            g.transform.SetParent(fxLayer, false);
            var img = g.GetComponent<Image>(); img.raycastTarget = false;
            img.sprite = RuntimeSprites.Get("emote_Heart", () => EmoteDrawing.Draw(EmoteDrawing.Kind.Heart));
            var rt = (RectTransform)g.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(40, 40);
            rt.anchoredPosition = CatToUi(catPos) + new Vector2(Random.Range(-30f, 30f), 10f);
            fx.Add(new Fx { rt = rt, g = img, life = 1.1f, max = 1.1f, vel = new Vector2(Random.Range(-20f, 20f), 90) });
        }

        void KneadPaw(Vector2 catPos)
        {
            var g = new GameObject("Knead", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            g.transform.SetParent(fxLayer, false);
            var img = g.GetComponent<Image>(); img.raycastTarget = false;
            bool left = catPos.x < 84;
            img.sprite = PawArt.Has ? PawArt.Get(PawArt.Press, left)
                       : left ? RuntimeSprites.Get("paw_left_press", CookingDrawing.PawLeftPressed) : RuntimeSprites.Get("paw_right_press", CookingDrawing.PawRightPressed);
            var rt = (RectTransform)g.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.8f);
            rt.sizeDelta = PawArt.Has ? PawArt.CellSize * 0.55f : new Vector2(PawDrawing.W, PawDrawing.H) * 1.5f;
            rt.anchoredPosition = CatToUi(catPos);
            fx.Add(new Fx { rt = rt, g = img, life = 0.35f, max = 0.35f, vel = Vector2.zero });
        }

        void UpdateFx(float dt)
        {
            for (int i = fx.Count - 1; i >= 0; i--)
            {
                var f = fx[i];
                f.life -= dt;
                if (f.rt == null || f.life <= 0f) { if (f.rt != null) Object.Destroy(f.rt.gameObject); fx.RemoveAt(i); continue; }
                f.rt.anchoredPosition += f.vel * dt;
                var c = f.g.color; c.a = Mathf.Clamp01(f.life / (f.max * 0.5f)); f.g.color = c;
                if (f.vel == Vector2.zero) f.rt.localScale = new Vector3(1f + (1f - f.life / f.max) * 0.1f, 1f - (1f - f.life / f.max) * 0.15f, 1f);
            }
        }
    }
}
