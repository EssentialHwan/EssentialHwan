using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Cafe;
using YeogiCafe.Core;
using YeogiCafe.Loc;

namespace YeogiCafe.UI
{
    // 가구 배치 모드: 시간을 멈추고 가구를 끌어서 옮긴다. 다른 가구와 겹치면 빨갛게 → 놓으면 원위치.
    // 그 가구에 앉아/올라가 있던 고양이는 같이 따라 옮겨진다. 위치는 세이브에 저장(다음 날에도 유지).
    public class FurnitureEditMode
    {
        readonly GameUiController owner;
        readonly GameObject bannerGo;
        public bool IsOn { get; private set; }

        float prevTimeScale = 1f;
        MovableFurniture dragging; Vector3 grabOffset, startPos; int startOrder; bool overlapping;
        readonly List<MovableFurniture> all = new();
        float pulse;

        public FurnitureEditMode(GameUiController owner, Transform parent)
        {
            this.owner = owner;
            var bar = UiFactory.Panel("EditBanner", parent, new Color(0.29f, 0.21f, 0.18f, 0.88f));
            bannerGo = bar.gameObject;
            UiFactory.Frame(bar, UiFactory.FrameStyle.Window);
            UiFactory.SetRect(bar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(820, 64));
            var t = UiFactory.Label("Hint", bar.transform, L.Get("ui.edit.hint"), 20, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(-170, 0));
            var done = UiFactory.Button("Done", bar.transform, L.Get("ui.edit.done"), UiFactory.Green, UiFactory.Ink, out _);
            UiFactory.SetRect((RectTransform)done.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(140, 48));
            done.onClick.AddListener(Exit);
            bannerGo.SetActive(false);
        }

        public void Toggle() { if (IsOn) Exit(); else Enter(); }

        public void Enter()
        {
            if (IsOn) return;
            IsOn = true;
            prevTimeScale = Time.timeScale; Time.timeScale = 0f;              // 배치 중엔 시간 정지
            YeogiCafe.Visual.CatInteractionDirector.Suspended = true;
            bannerGo.SetActive(true); bannerGo.transform.SetAsLastSibling();
            all.Clear();
            all.AddRange(Object.FindObjectsOfType<MovableFurniture>());
        }

        public void Exit()
        {
            if (!IsOn) return;
            if (dragging != null) Drop();
            IsOn = false;
            Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
            YeogiCafe.Visual.CatInteractionDirector.Suspended = false;
            bannerGo.SetActive(false);
            foreach (var m in all) if (m != null && m.Renderer != null) m.Renderer.color = Color.white;
        }

        public void Tick(float dt)
        {
            if (!IsOn) return;
            var cam = Camera.main; if (cam == null) return;
            Vector3 w = cam.ScreenToWorldPoint(Input.mousePosition); w.z = 0f;

            // 옮길 수 있는 가구를 은은하게 깜빡여 표시
            pulse += dt * 4f;
            float glow = 0.88f + Mathf.Sin(pulse) * 0.08f;
            foreach (var m in all)
            {
                if (m == null || m.Renderer == null || m == dragging) continue;
                m.Renderer.color = new Color(1f, glow + 0.08f, glow, 1f);
            }

            if (dragging == null)
            {
                if (Input.GetMouseButtonDown(0) && !OverUi())
                {
                    var hit = Pick(w);
                    if (hit != null)
                    {
                        dragging = hit; startPos = hit.transform.position; grabOffset = startPos - w;
                        startOrder = hit.Renderer != null ? hit.Renderer.sortingOrder : 0;
                        if (hit.Renderer != null) hit.Renderer.sortingOrder = 30000;
                    }
                }
                return;
            }

            // 드래그 중
            var p = dragging.Clamp(w + grabOffset);
            Vector3 delta = p - dragging.transform.position;
            dragging.transform.position = p;
            CarryCats(dragging.transform.position - delta, delta);
            overlapping = Overlaps(dragging);
            if (dragging.Renderer != null)
            {
                dragging.Renderer.color = overlapping ? new Color(1f, 0.55f, 0.55f, 0.9f) : new Color(1f, 1f, 1f, 0.92f);
                dragging.transform.localScale = Vector3.one * 1.05f;
            }
            if (Input.GetMouseButtonUp(0)) Drop();
        }

        void Drop()
        {
            var m = dragging; dragging = null;
            m.transform.localScale = Vector3.one;
            if (m.Renderer != null) { m.Renderer.color = Color.white; m.Renderer.sortingOrder = startOrder; }
            if (overlapping)
            {
                // 겹치면 원래 자리로
                Vector3 back = startPos - m.transform.position;
                CarryCats(m.transform.position, back);
                m.transform.position = startPos;
            }
            else m.onMoved?.Invoke(m.transform.position);
            m.RefreshVisual();
            overlapping = false;
        }

        // 가구 위(근처)에 있던 고양이를 같이 옮긴다
        void CarryCats(Vector3 oldPos, Vector3 delta)
        {
            if (delta.sqrMagnitude < 1e-8f || owner.Cats == null) return;
            foreach (var b in owner.Cats.Active)
            {
                if (b == null) continue;
                var bp = b.transform.position;
                if ((bp - oldPos).sqrMagnitude < 0.04f) b.transform.position = bp + delta;
            }
        }

        MovableFurniture Pick(Vector3 w)
        {
            MovableFurniture best = null; int bestOrder = int.MinValue;
            foreach (var m in all)
            {
                if (m == null || m.Renderer == null || m.Renderer.sprite == null) continue;
                var b = m.Renderer.bounds;
                if (!b.Contains(new Vector3(w.x, w.y, b.center.z))) continue;
                // 러그(바닥에 깔림)는 위에 다른 가구가 있으면 그쪽을 먼저 집는다
                int order = m.area == MovableFurniture.Area.FloorFlat ? int.MinValue + 1 : m.Renderer.sortingOrder;
                if (order > bestOrder) { bestOrder = order; best = m; }
            }
            return best;
        }

        bool Overlaps(MovableFurniture m)
        {
            if (m.area == MovableFurniture.Area.FloorFlat) return false;
            foreach (var o in all)
            {
                if (o == null || o == m || o.area != m.area) continue;
                Vector3 d = o.transform.position - m.transform.position;
                if (Mathf.Abs(d.x) < 0.75f && Mathf.Abs(d.y) < (m.area == MovableFurniture.Area.Wall ? 0.9f : 0.45f)) return true;
            }
            return false;
        }

        static bool OverUi()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }
    }
}
