using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Visual;

namespace YeogiCafe.UI
{
    // 고양이 머리 위 행동 서술 말풍선. 관찰 게임의 핵심 피드백이라 화면 하단 로그가 아니라
    // "누가" 그랬는지 바로 보이게 해당 고양이를 따라다닌다. 수치는 절대 표시하지 않는다(서술만).
    public class BubbleLayer
    {
        class Bubble { public RectTransform rt; public CanvasGroup cg; public Text text; public Transform target; public float life; }

        const float Life = 3.6f, Fade = 0.5f;
        static float HeadOffset => 1.45f * YeogiCafe.Art.CatSpriteBank.Scale;   // 고양이 발밑 기준 머리 위(유닛)

        readonly RectTransform root;
        readonly Dictionary<string, Bubble> byCat = new();
        readonly List<string> expired = new();

        public BubbleLayer(Transform canvas)
        {
            var go = new GameObject("Bubbles", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            root = (RectTransform)go.transform;
            UiFactory.Anchor(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.SetAsFirstSibling();              // 패널(상점/정산)보다 뒤
        }

        public void Show(string catId, string text)
        {
            var target = FindCat(catId);
            if (target == null) return;
            if (!byCat.TryGetValue(catId, out var b) || b.rt == null) { b = Create(); byCat[catId] = b; }
            b.target = target; b.text.text = text; b.life = Life; b.cg.alpha = 1f;
            b.rt.gameObject.SetActive(true);
        }

        public void Tick(float dt)
        {
            var cam = Camera.main;
            expired.Clear();
            foreach (var kv in byCat)
            {
                var b = kv.Value;
                if (b.rt == null || !b.rt.gameObject.activeSelf) continue;
                b.life -= dt;
                if (b.life <= 0f || b.target == null) { b.rt.gameObject.SetActive(false); continue; }
                b.cg.alpha = Mathf.Clamp01(b.life / Fade);
                if (cam != null)
                {
                    var cv = b.target.GetComponent<YeogiCafe.Visual.CatView>();                               // 올라앉은 고양이(캣타워·다락)는 그만큼 위에
                    Vector3 sp = cam.WorldToScreenPoint(b.target.position + (cv != null ? cv.VisualOffset : Vector3.zero) + Vector3.up * HeadOffset);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out var lp);
                    b.rt.anchoredPosition = lp;
                }
            }
        }

        Bubble Create()
        {
            var bg = UiFactory.Panel("Bubble", root, new Color(1f, 0.99f, 0.96f, 0.96f));
            bg.anchorMin = bg.anchorMax = new Vector2(0.5f, 0.5f);
            bg.pivot = new Vector2(0.5f, 0f);
            UiFactory.Frame(bg, UiFactory.FrameStyle.Bubble);
            var tail = new GameObject("Tail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            tail.transform.SetParent(bg, false);
            tail.GetComponent<LayoutElement>().ignoreLayout = true;
            var timg = tail.GetComponent<Image>(); timg.raycastTarget = false;
            timg.sprite = YeogiCafe.Art.RuntimeSprites.Get("ui_bubble_tail", YeogiCafe.Art.UiFrameDrawing.BubbleTail);
            UiFactory.SetRect((RectTransform)tail.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, 2), new Vector2(20, 14));
            var fitter = bg.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 9, 9);
            layout.childControlWidth = layout.childControlHeight = true;

            var t = UiFactory.Label("Text", bg.transform, "", 16, TextAnchor.MiddleCenter, UiFactory.Ink);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var cg = bg.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false;
            return new Bubble { rt = bg, cg = cg, text = t };
        }

        static Transform FindCat(string catId)
        {
            foreach (var v in Object.FindObjectsOfType<CatView>())
                if (v != null && v.Data != null && v.Data.catId == catId && !v.IsExiting)
                    return v.BubbleAnchor;
            return null;
        }
    }
}
