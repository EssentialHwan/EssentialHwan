using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
using YeogiCafe.Loc;

namespace YeogiCafe.UI
{
    // 시작 화면. 게임은 뒤에서 준비만 하고 멈춰 있다(timeScale 0) → [이어하기/시작하기]로 페이드아웃하며 영업 시작.
    // 배경: TitleDrawing(창밖 마을·간판·나무 바닥). 고양이: 도트 시트로 앉고·자고·그루밍하고, 한 마리는 바닥을 가로질러 걷는다.
    public class TitleScreen
    {
        public static bool SkipOnce;               // '새 게임' 후 씬을 다시 불러올 때는 시작 화면 없이 바로 영업

        readonly GameUiController owner;
        readonly GameObject rootGo;
        readonly CanvasGroup group;
        readonly RectTransform rootRt;
        readonly Text newLabel;
        Text contLabel; int shownDay = -1;
        readonly bool hasSave;
        bool confirmNew, closing;
        float fade = 1f, t;

        class Cat { public Image img; public Sprite[] frames; public float fps; public RectTransform rt; }
        readonly List<Cat> cats = new();
        Cat walker; float walkX = -220f, walkDir = 1f, walkWait;
        static readonly string[] WalkerIds = { "cat_tuxedo", "cat_mackerel", "cat_gray", "cat_cow" };
        int walkerIdx;

        public bool IsOpen => rootGo != null && rootGo.activeSelf;

        public TitleScreen(GameUiController owner, Transform canvas, bool hasSave, int day)
        {
            this.owner = owner; this.hasSave = hasSave;
            var bg = UiFactory.Panel("TitleScreen", canvas, Color.white);
            rootGo = bg.gameObject; rootRt = bg;
            UiFactory.Anchor(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = RuntimeSprites.Get("title_bg", TitleDrawing.Background);
            group = rootGo.AddComponent<CanvasGroup>();

            // 고양이들(바닥·창턱)
            AddCat("cat_calico", "sit", new Vector2(150, 34), false);
            AddCat("cat_cheese", "loaf", new Vector2(360, 60), false);
            AddCat("cat_black", "groom", new Vector2(860, 26), true);
            AddCat("cat_siamese", "sit", new Vector2(1070, 52), true);
            AddCat("cat_moon", "sleep", new Vector2(1000, 212), true);       // 창턱에서 꾸벅
            walker = AddCat(WalkerIds[0], "walk", new Vector2(walkX, 8), false);

            // 간판 + 로고
            var sign = new GameObject("Sign", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            sign.SetParent(bg, false);
            UiFactory.SetRect(sign, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(600, 160));
            sign.GetComponent<Image>().sprite = RuntimeSprites.Get("title_sign", TitleDrawing.Sign);
            var logo = UiFactory.Label("Logo", sign, L.Get("title.logo"), 64, TextAnchor.MiddleCenter, UiFactory.Ink);
            logo.fontStyle = FontStyle.Bold;
            UiFactory.FitOneLine(logo, 500, 34);                       // 일본어·중국어 제목은 길어서 간판 안에 맞게 줄임
            UiFactory.SetRect((RectTransform)logo.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(-80, 80));
            var sh = logo.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0.86f, 0.6f, 0.62f, 0.9f); sh.effectDistance = new Vector2(3, -3);
            var sub = UiFactory.Label("Sub", sign, L.Get("title.sub"), 19, TextAnchor.MiddleCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)sub.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(-80, 26));
            // 세계관 한 줄(왼쪽 아래 — 오른쪽 아래 단축키 안내와 짝)
            var era = UiFactory.Label("Era", bg, L.Get("title.era"), 15, TextAnchor.LowerLeft, new Color(0.45f, 0.33f, 0.27f, 0.85f));
            UiFactory.SetRect((RectTransform)era.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 8), new Vector2(360, 24));
            era.raycastTarget = false;
            if (YeogiCafe.DevTools.PlaytestLog.Enabled)
            {
                // 친구 테스트 빌드 표시(오른쪽 위) — 버전과 '사운드 미적용'
                var pt = UiFactory.Label("Playtest", bg, L.Get("title.playtest", ("v", Application.version)), 15, TextAnchor.UpperRight, new Color(0.62f, 0.3f, 0.34f, 0.95f));
                UiFactory.SetRect((RectTransform)pt.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -10), new Vector2(420, 22));
                pt.raycastTarget = false;
            }

            // 버튼(창 앞, 가운데 세로로)
            float y = 120;
            var cont = Btn(ContinueText(day), UiFactory.Green, UiFactory.Ink, ref y, out contLabel);
            cont.onClick.AddListener(Begin);
            var nb = Btn(L.Get("ui.title.new"), UiFactory.Cream, UiFactory.Ink, ref y, out newLabel);
            nb.onClick.AddListener(NewGame);
            if (!hasSave) nb.gameObject.SetActive(false);
            var sb = Btn(L.Get("ui.title.settings"), UiFactory.Cream, UiFactory.Ink, ref y, out _);
            sb.onClick.AddListener(() => owner.OpenSettings());
            var qb = Btn(L.Get("ui.title.quit"), UiFactory.Wood, UiFactory.Cream, ref y, out _);
            qb.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            var hint = UiFactory.Label("Hint", bg, L.Get("title.hint"), 15, TextAnchor.LowerRight, new Color(0.45f, 0.33f, 0.27f, 0.85f));
            UiFactory.SetRect((RectTransform)hint.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 8), new Vector2(520, 24));

            if (SkipOnce) { SkipOnce = false; rootGo.SetActive(false); return; }
            rootGo.transform.SetAsLastSibling();
            Time.timeScale = 0f;
        }

        string ContinueText(int day) => hasSave ? L.Get("ui.title.continue") + "  ·  " + L.Get("title.day", ("n", day.ToString())) : L.Get("title.start");

        Button Btn(string label, Color bg, Color fg, ref float y, out Text lbl)
        {
            var b = UiFactory.Button("Btn", rootRt, label, bg, fg, out lbl);
            lbl.fontSize = 22;
            UiFactory.SetRect((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(300, 54));
            y -= 64;
            return b;
        }

        Cat AddCat(string id, string clip, Vector2 pos, bool flip)
        {
            var go = new GameObject(id, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform; rt.SetParent(rootRt, false);
            UiFactory.SetRect(rt, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0), pos, new Vector2(CatSpriteBank.FW * 3, CatSpriteBank.FH * 3));
            rt.localScale = new Vector3(flip ? -1 : 1, 1, 1);
            var img = go.GetComponent<Image>(); img.raycastTarget = false;
            var c = new Cat { img = img, rt = rt };
            SetClip(c, id, clip);
            cats.Add(c);
            return c;
        }

        void SetClip(Cat c, string id, string clip)
        {
            c.frames = CatSpriteBank.Clip(id, clip);
            c.fps = CatClips.Find(clip).fps;
            if (c.frames != null && c.frames.Length > 0) c.img.sprite = c.frames[0];
        }

        public void Tick(float dt)
        {
            if (!IsOpen) return;
            t += dt;
            if (!closing) Time.timeScale = 0f;        // 설정 창이 닫히며 시간을 되돌려도 시작 화면 동안은 멈춤
            int d = owner.Day != null ? owner.Day.CurrentDay : 1;   // 세이브 복원이 시작 화면보다 늦게 끝나므로 매 프레임 확인
            if (d != shownDay) { shownDay = d; contLabel.text = ContinueText(d); }

            foreach (var c in cats)
                if (c.frames != null && c.frames.Length > 0)
                    c.img.sprite = c.frames[(int)(t * c.fps) % c.frames.Length];

            // 바닥을 가로지르는 고양이: 끝에 닿으면 잠깐 쉬고 다른 고양이가 반대로
            if (walkWait > 0f) walkWait -= dt;
            else
            {
                walkX += walkDir * 95f * dt;
                walker.rt.anchoredPosition = new Vector2(walkX, 8);
                if (walkX > 1500f || walkX < -220f)
                {
                    walkDir = -walkDir; walkWait = Random.Range(2.5f, 5f);
                    walkerIdx = (walkerIdx + 1) % WalkerIds.Length;
                    SetClip(walker, WalkerIds[walkerIdx], "walk");
                    walker.rt.localScale = new Vector3(walkDir, 1, 1);
                }
            }

            if (closing)
            {
                fade -= dt / 0.5f;
                group.alpha = Mathf.Clamp01(fade);
                if (fade <= 0f) rootGo.SetActive(false);
            }
        }

        void Begin()
        {
            if (closing) return;
            closing = true; group.blocksRaycasts = false;
            Time.timeScale = 1f;
            YeogiCafe.Audio.SoundManager.Sfx("sfx_trill", 0.45f);
            if (!hasSave) owner.PlayOpening();           // 처음 시작: 1일차 전에 오프닝 그림책
        }

        void NewGame()
        {
            if (!confirmNew) { confirmNew = true; newLabel.text = L.Get("opt.newgame.confirm"); return; }
            var sm = Object.FindObjectOfType<YeogiCafe.Save.SaveManager>();
            if (sm != null) sm.DeleteSave();
            SkipOnce = true;
            OpeningPanel.Pending = true;
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
