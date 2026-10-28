using UnityEngine;
using UnityEngine.UI;

namespace YeogiCafe.UI
{
    // 세로 목록 + 휠 스크롤(한 칸 = 한 줄). 에디터·기기마다 휠 값이 0.1~1로 달라 ScrollRect 기본 휠이 너무 느려서 직접 처리한다.
    // 줄은 content 아래에 sizeDelta.y로 높이를 정해 넣고, 다 넣은 뒤 Fit()을 부른다.
    public class WheelList : MonoBehaviour
    {
        public RectTransform view, content;
        public float step = 44f;
        ScrollRect scroll;

        public static WheelList Create(Transform parent, string name, float spacing, out RectTransform content)
        {
            var viewGo = new GameObject(name, typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image), typeof(WheelList));
            viewGo.transform.SetParent(parent, false);
            viewGo.GetComponent<Image>().color = new Color(1, 1, 1, 0);      // 빈 곳에서도 드래그가 먹게
            var view = (RectTransform)viewGo.transform;
            var listGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(view, false);
            content = (RectTransform)listGo.transform;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing; vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            var sr = viewGo.GetComponent<ScrollRect>();
            sr.content = content; sr.viewport = view; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 0f;
            var w = viewGo.GetComponent<WheelList>();
            w.view = view; w.content = content; w.scroll = sr;
            return w;
        }

        // 지울 줄은 바로 떼어 낸다(Destroy는 프레임 끝이라 남아 있으면 높이 계산에 섞인다)
        public void Clear()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var c = content.GetChild(i);
                c.SetParent(null, false);
                Destroy(c.gameObject);
            }
        }

        public void Fit(bool toTop = true)
        {
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            float h = 0; int n = 0;
            for (int i = 0; i < content.childCount; i++)
            {
                var c = (RectTransform)content.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                h += c.sizeDelta.y; n++;
            }
            if (n > 1) h += vlg.spacing * (n - 1);
            h += vlg.padding.top + vlg.padding.bottom;
            content.sizeDelta = new Vector2(content.sizeDelta.x, h);
            if (toTop) content.anchoredPosition = Vector2.zero;
            else Clamp();
        }

        void Clamp()
        {
            float max = Mathf.Max(0f, content.rect.height - view.rect.height);
            var p = content.anchoredPosition; p.y = Mathf.Clamp(p.y, 0f, max); content.anchoredPosition = p;
        }

        void Update()
        {
            float d = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(d, 0f)) return;
            if (!RectTransformUtility.RectangleContainsScreenPoint(view, Input.mousePosition, null)) return;
            var p = content.anchoredPosition; p.y -= Mathf.Sign(d) * step; content.anchoredPosition = p;
            Clamp();
            if (scroll != null) scroll.StopMovement();
        }
    }
}
