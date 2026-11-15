using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
using YeogiCafe.Loc;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 11/16 '로이에게' — 진엔딩(모든 엔딩)을 본 뒤 한 번 나오는 헌정 화면.
    //  이 게임의 주인공(하얀 고양이)은 만든 사람의 가장 친한 친구였던 하얀 고양이 로이에서 왔다.
    //  게임 내내 도트였던 화면에 마지막으로 진짜 사진(Resources/Tribute/roy_0~3)이 한 장씩 놓이고, 주인공의 하얀 앞발이 옆에 머문다.
    //  사진은 촬영 정보(EXIF)를 지우고 480×600으로 줄여 넣었다.
    public class TributePanel
    {
        public static bool IsOpen { get; private set; }
        const int PhotoCount = 4;

        public TributePanel(Transform parent, SaveGame save, System.Action onClose, bool preview = false)
        {
            IsOpen = true;
            Time.timeScale = 0f;
            var scrim = UiFactory.Panel("Tribute", parent, new Color(0.10f, 0.08f, 0.10f, 0f));
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scrim.SetAsLastSibling();
            var anim = scrim.gameObject.AddComponent<TributeAnim>();
            anim.scrim = scrim.GetComponent<Image>();

            // 사진: 폴라로이드처럼 흰 테두리, 조금씩 기울여 한 줄로
            var photos = new List<RectTransform>();
            float[] tilt = { -5f, 3f, -2f, 4f };
            for (int i = 0; i < PhotoCount; i++)
            {
                var ta = Resources.Load<TextAsset>($"Tribute/roy_{i}");
                if (ta == null) continue;
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                if (!tex.LoadImage(ta.bytes)) continue;
                var frame = UiFactory.Panel("Photo" + i, scrim, new Color(1f, 0.99f, 0.96f, 1f));
                float x = (i - (PhotoCount - 1) * 0.5f) * 236f;
                UiFactory.SetRect(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 92f), new Vector2(212, 252));
                frame.localRotation = Quaternion.Euler(0, 0, tilt[i % tilt.Length]);
                var g = new GameObject("Img", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                g.transform.SetParent(frame, false);
                var ri = g.GetComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false;
                UiFactory.SetRect((RectTransform)g.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(192, 200));
                photos.Add(frame);
            }
            anim.photos = photos;

            var title = UiFactory.Label("Title", scrim, L.Get("tribute.title"), 34, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.82f, 1f));
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(-80, 50));
            var body = UiFactory.Label("Body", scrim, L.Get("tribute.body"), 20, TextAnchor.UpperCenter, new Color(1f, 0.96f, 0.9f, 1f));
            body.lineSpacing = 1.3f;
            UiFactory.SetRect((RectTransform)body.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 98), new Vector2(-160, 160));
            anim.title = title; anim.body = body;

            // 주인공의 하얀 앞발(= 로이): 사진 줄 오른쪽 아래에 살며시
            if (PawArt.Has)
            {
                var pg = new GameObject("Paw", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pg.transform.SetParent(scrim, false);
                var pi = pg.GetComponent<Image>(); pi.sprite = PawArt.Get(0, false); pi.preserveAspect = true; pi.raycastTarget = false;
                UiFactory.SetRect((RectTransform)pg.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, -10), new Vector2(170, 170));
                anim.paw = pi;
            }

            var btn = UiFactory.Button("Close", scrim, L.Get("tribute.close"), UiFactory.Cream, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(240, 48));
            anim.button = btn;
            btn.onClick.AddListener(() =>
            {
                if (save != null && !preview)
                {
                    save.tributeSeen = true;
                    var sm = Object.FindObjectOfType<SaveManager>(); if (sm != null) sm.Save(save);
                }
                Object.Destroy(scrim.gameObject);
                IsOpen = false; Time.timeScale = 1f;
                onClose?.Invoke();
            });
            anim.Init();
        }

        // 순서: 어두워짐(0~1.2초) → 사진 한 장씩(1.2초부터 0.9초 간격) → 글(사진 다 놓인 뒤) → 앞발 → 버튼
        class TributeAnim : MonoBehaviour
        {
            public Image scrim, paw; public Text title, body; public Button button; public List<RectTransform> photos;
            float t;
            CanvasGroup[] pg; CanvasGroup tg, bg, btg;
            Vector2[] basePos;

            public void Init()
            {
                pg = new CanvasGroup[photos.Count]; basePos = new Vector2[photos.Count];
                for (int i = 0; i < photos.Count; i++) { pg[i] = photos[i].gameObject.AddComponent<CanvasGroup>(); pg[i].alpha = 0f; basePos[i] = photos[i].anchoredPosition; }
                tg = title.gameObject.AddComponent<CanvasGroup>(); tg.alpha = 0f;
                bg = body.gameObject.AddComponent<CanvasGroup>(); bg.alpha = 0f;
                btg = button.gameObject.AddComponent<CanvasGroup>(); btg.alpha = 0f; btg.interactable = false;
                if (paw != null) paw.color = new Color(1, 1, 1, 0);
                YeogiCafe.Audio.SoundManager.Sfx("sfx_purr", 0.35f, 0f);
            }

            void Update()
            {
                t += Time.unscaledDeltaTime;
                scrim.color = new Color(0.10f, 0.08f, 0.10f, Mathf.Clamp01(t / 1.2f) * 0.94f);
                for (int i = 0; i < pg.Length; i++)
                {
                    float k = Mathf.Clamp01((t - 1.2f - i * 0.9f) / 0.7f);
                    float e = 1f - (1f - k) * (1f - k);
                    pg[i].alpha = e;
                    photos[i].anchoredPosition = basePos[i] + new Vector2(0, (1f - e) * 24f);
                }
                float after = 1.2f + pg.Length * 0.9f;
                tg.alpha = Mathf.Clamp01((t - after) / 1.0f);
                bg.alpha = Mathf.Clamp01((t - after - 0.8f) / 1.4f);
                if (paw != null) paw.color = new Color(1, 1, 1, Mathf.Clamp01((t - after - 2.0f) / 1.2f));
                float b = Mathf.Clamp01((t - after - 3.0f) / 0.6f);
                btg.alpha = b; btg.interactable = b > 0.9f;
            }
        }
    }
}
