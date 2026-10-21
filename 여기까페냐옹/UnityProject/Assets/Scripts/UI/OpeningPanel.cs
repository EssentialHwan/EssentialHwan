using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
using YeogiCafe.Loc;

namespace YeogiCafe.UI
{
    // 오프닝 그림책(새 게임 1일차 전 한 번 · 설정에서 다시 보기).
    // 2027년 혜성 → 사람이 사라짐 → 고양이만 남아 똑똑해짐(NeuroNya) → 고양이 마을 → 오늘도 카페에 출근.
    // 그림: Resources/Opening/op_1~5.bytes(PNG)가 있으면 그걸, 없으면 지금 있는 그림으로 임시 장면을 만든다.
    // 클릭/스페이스 = 글 다 보이기 → 다음 장 · ESC/[건너뛰기] = 끝. 보는 동안 게임 시간은 멈춘다.
    public class OpeningPanel
    {
        public const int Pages = 5;
        public static bool Pending;               // 새 게임으로 씬을 다시 불러온 직후 한 번 보여 줄지

        readonly GameObject rootGo;
        readonly CanvasGroup group;
        readonly RectTransform picture;
        readonly Text body, pageLabel;
        readonly System.Action onDone;
        int page = -1;
        string full = "";
        float shown, fade = -1f, inT;
        const float CharsPerSec = 26f;

        public bool IsOpen => rootGo != null;

        public OpeningPanel(Transform canvas, System.Action onDone)
        {
            this.onDone = onDone;
            var bg = UiFactory.Panel("Opening", canvas, new Color(0.07f, 0.06f, 0.1f, 1f));
            rootGo = bg.gameObject;
            UiFactory.Anchor(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            group = rootGo.AddComponent<CanvasGroup>();
            bg.SetAsLastSibling();
            // 화면 아무 곳이나 누르면 넘김
            var click = rootGo.AddComponent<Button>(); click.transition = Selectable.Transition.None;
            click.onClick.AddListener(Advance);

            // 그림 액자(16:9)
            var frame = UiFactory.Panel("Frame", bg, new Color(1f, 0.96f, 0.86f, 1f));
            UiFactory.SetRect(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 74), new Vector2(752, 431));
            UiFactory.Frame(frame, UiFactory.FrameStyle.Paper, null, 3f);
            frame.GetComponent<Image>().raycastTarget = false;
            var picGo = new GameObject("Picture", typeof(RectTransform), typeof(RectMask2D));
            picGo.transform.SetParent(frame, false);
            picture = (RectTransform)picGo.transform;
            UiFactory.SetRect(picture, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-32, -31));

            // 글
            body = UiFactory.Label("Body", bg, "", 24, TextAnchor.UpperCenter, new Color(1f, 0.96f, 0.88f, 1f));
            body.lineSpacing = 1.2f; body.raycastTarget = false;
            UiFactory.SetRect((RectTransform)body.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(900, 120));

            pageLabel = UiFactory.Label("Page", bg, "", 15, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.45f));
            pageLabel.raycastTarget = false;
            UiFactory.SetRect((RectTransform)pageLabel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(400, 22));

            var skip = UiFactory.Button("Skip", bg, L.Get("opening.skip"), new Color(1f, 1f, 1f, 0.12f), new Color(1f, 0.96f, 0.88f, 0.85f), out var sl);
            sl.fontSize = 16;
            UiFactory.SetRect((RectTransform)skip.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -18), new Vector2(130, 40));
            skip.onClick.AddListener(Finish);

            Time.timeScale = 0f;
            Next();
        }

        void Advance()
        {
            if (fade >= 0f) return;
            if (shown < full.Length) { shown = full.Length; body.text = full; return; }   // 글 먼저 다 보이기
            Next();
        }

        void Next()
        {
            page++;
            if (page >= Pages) { Finish(); return; }
            for (int i = picture.childCount - 1; i >= 0; i--) Object.Destroy(picture.GetChild(i).gameObject);
            BuildPicture(page);
            full = L.Get($"opening.{page + 1}");
            shown = 0; body.text = ""; inT = 0f;
            pageLabel.text = $"{page + 1} / {Pages}   ·   {L.Get("opening.hint")}";
            YeogiCafe.Audio.SoundManager.Sfx("sfx_boop", 0.3f, 0.05f);
        }

        void Finish()
        {
            if (fade >= 0f) return;
            fade = 1f;
        }

        public void Tick(float dt)
        {
            if (rootGo == null) return;
            Time.timeScale = 0f;
            rootGo.transform.SetAsLastSibling();
            if (Input.GetKeyDown(KeyCode.Escape)) Finish();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Advance();
            if (fade >= 0f)
            {
                fade -= dt / 0.6f;
                group.alpha = Mathf.Clamp01(fade);
                if (fade <= 0f)
                {
                    Object.Destroy(rootGo);
                    Time.timeScale = 1f;
                    onDone?.Invoke();
                }
                return;
            }
            // 장면이 천천히 밝아지고, 글이 한 자씩
            inT += dt;
            float a = Mathf.Clamp01(inT / 0.6f);
            foreach (var g in picture.GetComponentsInChildren<Graphic>()) g.canvasRenderer.SetAlpha(a);
            if (shown < full.Length)
            {
                shown = Mathf.Min(full.Length, shown + dt * CharsPerSec);
                body.text = full.Substring(0, (int)shown);
            }
        }

        // ── 장면 ──
        void BuildPicture(int i)
        {
            var art = Resources.Load<TextAsset>($"Opening/op_{i + 1}");
            if (art != null)
            {
                var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
                if (tex.LoadImage(art.bytes))
                {
                    // 액자를 꽉 채우고 넘치는 가장자리는 잘라 낸다(3:2 그림 → 16:9 액자)
                    var im = Img(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.white, false);
                    var fit = im.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    fit.aspectRatio = tex.width / (float)tex.height;
                    return;
                }
            }
            var town = RuntimeSprites.Get("title_bg", TitleDrawing.Background);
            switch (i)
            {
                case 0:   // 밤하늘을 가르는 혜성
                    Img(Night(), Vector2.zero, Vector2.one, Color.white, false);
                    Img(Comet(), new Vector2(0.18f, 0.35f), new Vector2(0.9f, 0.95f), Color.white, false);
                    break;
                case 1:   // 텅 빈 마을(푸르스름하게)
                    Img(town, Vector2.zero, Vector2.one, new Color(0.45f, 0.5f, 0.62f, 1f), false);
                    break;
                case 2:   // 어두운 방에서 눈을 뜬 고양이
                    Img(null, Vector2.zero, Vector2.one, new Color(0.1f, 0.09f, 0.14f, 1f), false);
                    Img(CatSpriteBank.Portrait("cat_cheese"), new Vector2(0.36f, 0.08f), new Vector2(0.64f, 0.82f), new Color(0.85f, 0.85f, 1f, 1f), true);
                    break;
                case 3:   // 고양이 마을(여러 마리)
                    Img(town, Vector2.zero, Vector2.one, Color.white, false);
                    string[] ids = { "cat_calico", "cat_black", "cat_fold", "cat_munchkin", "cat_siamese" };
                    for (int k = 0; k < ids.Length; k++)
                    {
                        float x0 = 0.06f + k * 0.18f;
                        Img(CatSpriteBank.Portrait(ids[k]), new Vector2(x0, 0.02f), new Vector2(x0 + 0.16f, 0.36f), Color.white, true);
                    }
                    break;
                default:  // 앞발로 카페 문을 연다
                    Img(town, Vector2.zero, Vector2.one, new Color(1f, 0.93f, 0.8f, 1f), false);
                    if (PawArt.Has) Img(PawArt.Get(PawArt.Spread, false), new Vector2(0.32f, -0.25f), new Vector2(0.68f, 0.7f), Color.white, true);
                    break;
            }
        }

        Image Img(Sprite sp, Vector2 min, Vector2 max, Color col, bool keepAspect)
        {
            var go = new GameObject("I", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(picture, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var im = go.GetComponent<Image>(); im.sprite = sp; im.color = col; im.preserveAspect = keepAspect; im.raycastTarget = false;
            im.canvasRenderer.SetAlpha(0f);
            return im;
        }

        // 임시 그림: 별 밤하늘 / 혜성(도트)
        static Sprite night, comet;
        static Sprite Night()
        {
            if (night != null) return night;
            const int W = 192, H = 108;
            var t = new Texture2D(W, H) { filterMode = FilterMode.Point };
            var rnd = new System.Random(7);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float k = y / (float)H;
                    var c = Color.Lerp(new Color(0.16f, 0.12f, 0.26f), new Color(0.03f, 0.03f, 0.08f), k);
                    if (rnd.NextDouble() < 0.012) c = new Color(1f, 0.95f, 0.8f);
                    t.SetPixel(x, y, c);
                }
            for (int x = 0; x < W; x++) for (int y = 0; y < 10 + (int)(4 * Mathf.Sin(x * 0.07f)); y++) t.SetPixel(x, y, new Color(0.05f, 0.05f, 0.09f));   // 지붕선
            t.Apply();
            return night = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
        }
        static Sprite Comet()
        {
            if (comet != null) return comet;
            const int W = 96, H = 48;
            var t = new Texture2D(W, H) { filterMode = FilterMode.Point };
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) t.SetPixel(x, y, clear);
            // 왼쪽 아래 머리, 오른쪽 위로 꼬리
            for (int i = 0; i < 80; i++)
            {
                float u = i / 80f;
                int cx = 8 + (int)(u * 84), cy = 8 + (int)(u * 36);
                int r = Mathf.Max(0, (int)(4 * (1f - u)));
                var col = Color.Lerp(new Color(1f, 0.95f, 0.75f, 1f), new Color(0.55f, 0.75f, 1f, 0f), u);
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r * r && cx + dx >= 0 && cx + dx < W && cy + dy >= 0 && cy + dy < H) t.SetPixel(cx + dx, cy + dy, col);
                if (r == 0 && cx < W && cy < H) t.SetPixel(cx, cy, col);
            }
            for (int dy = -5; dy <= 5; dy++) for (int dx = -5; dx <= 5; dx++)
                if (dx * dx + dy * dy <= 25) t.SetPixel(8 + dx, 8 + dy, dx * dx + dy * dy <= 9 ? Color.white : new Color(1f, 0.85f, 0.5f, 1f));
            t.Apply();
            return comet = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
        }
    }
}
