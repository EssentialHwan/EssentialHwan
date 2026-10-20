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
        // 판매: 가구를 톡 누르면(끌지 않고) 선택 → [팔기 +NG] 두 번 눌러 판매
        MovableFurniture selected; bool sellArmed;
        Text hintText; Button sellBtn; Text sellLabel;

        public FurnitureEditMode(GameUiController owner, Transform parent)
        {
            this.owner = owner;
            var bar = UiFactory.Panel("EditBanner", parent, new Color(0.29f, 0.21f, 0.18f, 0.88f));
            bannerGo = bar.gameObject;
            UiFactory.Frame(bar, UiFactory.FrameStyle.Window);
            UiFactory.SetRect(bar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(820, 64));
            var t = UiFactory.Label("Hint", bar.transform, L.Get("ui.edit.hint"), 18, TextAnchor.MiddleLeft, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(-380, 0));
            t.verticalOverflow = VerticalWrapMode.Truncate; t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = 18;
            hintText = t;
            sellBtn = UiFactory.Button("Sell", bar.transform, "", UiFactory.Pink, UiFactory.Cream, out sellLabel);
            sellLabel.fontSize = 17; sellLabel.resizeTextForBestFit = true; sellLabel.resizeTextMinSize = 11; sellLabel.resizeTextMaxSize = 17;
            UiFactory.SetRect((RectTransform)sellBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-162, 0), new Vector2(200, 48));
            sellBtn.onClick.AddListener(OnSell);
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
            Select(null);
            hintText.text = L.Get("ui.edit.hint");
        }

        void Select(MovableFurniture m)
        {
            selected = m; sellArmed = false;
            var fm = owner.Furniture;
            var d = m != null && fm != null ? fm.DataOf(m.gameObject) : null;
            sellBtn.gameObject.SetActive(m != null);
            if (m == null) return;
            if (d == null && fm != null && fm.IsStarter(m.gameObject))
            {
                if (!fm.CanSellStarter) { sellLabel.text = L.Get("ui.edit.lastseat"); sellBtn.interactable = false; return; }
                sellBtn.interactable = true;
                sellLabel.text = L.Get("ui.edit.sell", ("name", ShopPanel.FurnitureName(fm.StarterData(m.gameObject))), ("g", "0"));
                return;
            }
            if (d == null) { sellLabel.text = L.Get("ui.edit.cantsell"); sellBtn.interactable = false; return; }
            sellBtn.interactable = true;
            sellLabel.text = L.Get("ui.edit.sell", ("name", ShopPanel.FurnitureName(d)), ("g", FurnitureManager.SellPrice(d).ToString()));
        }

        void OnSell()
        {
            if (selected == null) return;
            var fm = owner.Furniture; if (fm == null) return;
            var d = fm.DataOf(selected.gameObject);
            bool starter = d == null && fm.IsStarter(selected.gameObject);
            if (starter) d = fm.StarterData(selected.gameObject);
            if (d == null) return;
            int price = starter ? 0 : FurnitureManager.SellPrice(d);
            if (!sellArmed) { sellArmed = true; sellLabel.text = L.Get("ui.edit.sellconfirm", ("g", price.ToString())); return; }   // 실수 방지: 한 번 더
            var go = selected.gameObject;
            if (fm.Sell(go, out int gold, out var why))
            {
                all.Remove(selected);
                hintText.text = L.Get("ui.edit.sold", ("name", ShopPanel.FurnitureName(d)), ("g", gold.ToString()));
                YeogiCafe.Audio.SoundManager.Sfx("sfx_coin", 0.5f, 0.02f);
                Select(null);
                owner.RefreshHud();
            }
            else
            {
                hintText.text = L.Get(why == FurnitureManager.SellFail.InUse ? "ui.edit.inuse" : why == FurnitureManager.SellFail.LastStarter ? "ui.edit.lastseat" : "ui.edit.cantsell");
                sellArmed = false;
                Select(selected);
            }
        }

        public void Exit()
        {
            if (!IsOn) return;
            if (dragging != null) Drop();
            IsOn = false;
            Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
            YeogiCafe.Visual.CatInteractionDirector.Suspended = false;
            bannerGo.SetActive(false);
            Select(null);
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
                m.Renderer.color = m == selected ? new Color(1f, 0.85f + Mathf.Sin(pulse * 1.5f) * 0.1f, 0.45f, 1f)   // 선택: 노랗게
                                                 : new Color(1f, glow + 0.08f, glow, 1f);
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
                    else Select(null);
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
            if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0)) Drop();   // 떼는 순간을 놓쳐도(아주 빠른 클릭) 버튼이 안 눌려 있으면 내려놓기
        }

        void Drop()
        {
            var m = dragging; dragging = null;
            bool click = (m.transform.position - startPos).sqrMagnitude < 0.0025f;   // 거의 안 움직였으면 '선택'
            if (click) Select(m == selected ? null : m);
            else if (m != selected) Select(null);
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
