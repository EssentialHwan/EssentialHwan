using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Data;
using YeogiCafe.Loc;
using YeogiCafe.Observation;
using YeogiCafe.Save;

namespace YeogiCafe.UI
{
    // 간단 관찰 도감 — 발견 진행도 + 고양이별 3축 상태(?/…/★/✓)를 나열.
    // Truth 미노출: 확정(Recorded)된 축만 실제 값(recordedValue) 노출.
    public class CatalogPanel
    {
        readonly GameUiController owner;
        readonly GameObject rootGo;
        Text header;
        RectTransform listContent;
        RectTransform relRoot; Text tabLabel; bool showRel;

        public CatalogPanel(GameUiController owner, Transform parent)
        {
            this.owner = owner;
            var scrim = UiFactory.Panel("CatalogScrim", parent, UiFactory.Scrim);
            rootGo = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = UiFactory.Panel("CatalogPanel", scrim.transform, UiFactory.PanelBg);
            UiFactory.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              Vector2.zero, new Vector2(720, 560));
            UiFactory.Frame(panel, UiFactory.FrameStyle.Window);

            var title = UiFactory.Label("Title", panel.transform, L.Get("ui.catalog.title"), 26,
                                        TextAnchor.MiddleCenter, UiFactory.Ink);
            UiFactory.SetRect((RectTransform)title.transform, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(0, 40));     // 창 테두리(≈24px) 아래로

            header = UiFactory.Label("Header", panel.transform, "", 22, TextAnchor.UpperCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)header.transform, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(-60, 40));

            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panel.transform, false);
            listContent = (RectTransform)listGo.transform;
            UiFactory.SetRect(listContent, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -22), new Vector2(-48, -178));
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4; vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            var closeBtn = UiFactory.Button("Close", panel.transform, "×", UiFactory.Pink, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)closeBtn.transform, new Vector2(1, 1), new Vector2(1, 1),
                              new Vector2(1, 1), new Vector2(-10, -10), new Vector2(48, 48));
            closeBtn.onClick.AddListener(Hide);

            // 탭: 목록 ↔ 관계도
            var tab = UiFactory.Button("RelTab", panel.transform, L.Get("ui.catalog.reltab"), UiFactory.Wood, UiFactory.Cream, out tabLabel);
            tabLabel.fontSize = 16;
            UiFactory.SetRect((RectTransform)tab.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -10), new Vector2(110, 40));
            tab.onClick.AddListener(() => { showRel = !showRel; Render(); });
            var relGo = new GameObject("Relations", typeof(RectTransform));
            relGo.transform.SetParent(panel.transform, false);
            relRoot = (RectTransform)relGo.transform;
            UiFactory.SetRect(relRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(660, 400));

            rootGo.SetActive(false);
        }

        void ClearRows()
        {
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Object.Destroy(listContent.GetChild(i).gameObject);
        }

        void Render()
        {
            ClearRows();
            listContent.gameObject.SetActive(!showRel);
            relRoot.gameObject.SetActive(showRel);
            tabLabel.text = L.Get(showRel ? "ui.catalog.listtab" : "ui.catalog.reltab");
            if (showRel) RenderRelations();
            var save = owner.Save;
            var all = owner.AllCats;
            int total = all.Count;
            int discovered = 0;

            foreach (var data in all)
            {
                if (data == null) continue;
                CatSaveData cs = save?.GetOrCreateCat(data.catId);
                bool visited = cs != null && cs.visitCount > 0;
                if (cs != null && cs.observation.RecordedAxisCount() > 0) discovered++;

                var row = UiFactory.Panel("Cat_" + data.catId, listContent, UiFactory.Cream);
                UiFactory.Frame(row, UiFactory.FrameStyle.Card, UiFactory.Cream);
                ((RectTransform)row.transform).sizeDelta = new Vector2(0, 38);

                string catName = visited ? L.Get($"cat.{data.catId}.name") : "???";
                if (cs != null && cs.bossSeen) catName += $" ({L.Get("ui.catalog.boss")})";
                if (cs != null && cs.graduated) catName += $" ({L.Get("ui.catalog.grad")})";
                string axes = cs == null ? "?  ?  ?" :
                    $"{L.Get("ui.axis.food")} {ObservationCardBuilder.AxisText(cs.observation.food)}   " +
                    $"{L.Get("ui.axis.seat")} {ObservationCardBuilder.AxisText(cs.observation.seat)}   " +
                    $"{L.Get("ui.axis.facility")} {ObservationCardBuilder.AxisText(cs.observation.facility)}";

                string touchKey = cs != null ? YeogiCafe.AI.CatTemperament.ObservedKey(cs.touchSeen) : null;
                string touch = $"{L.Get("ui.axis.touch")} {(touchKey != null ? L.Get(touchKey) : "?")}";
                if (cs != null && cs.likeZones != 0) touch += " " + L.Get("ui.catalog.likes", ("z", ClosingPlayPanel.Zones(cs.likeZones)));
                if (cs != null && cs.hateZones != 0) touch += " " + L.Get("ui.catalog.hates", ("z", ClosingPlayPanel.Zones(cs.hateZones)));
                if (cs != null && cs.suckleSeen) touch += " " + L.Get("ui.catalog.suckle");
                string rowText = $"{catName}    {axes}    {touch}";
                if (!visited && data.unlockCondition != null && data.unlockCondition.type == UnlockType.FurnitureSet)
                    rowText = "???    " + L.Get("ui.catalog.rumor");               // 숨은 고양이: 소문만
                bool rumor = rowText.StartsWith("???    ") && rowText.Length > 40;
                if (rumor) ((RectTransform)row.transform).sizeDelta = new Vector2(0, 54);   // 소문은 두 줄
                var lbl = UiFactory.Label("Row", row.transform, rowText, 16,
                                          TextAnchor.MiddleLeft, UiFactory.Ink);
                lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 10; lbl.resizeTextMaxSize = 16;   // 길어지면 글자를 줄여 한 칸 안에
                lbl.verticalOverflow = VerticalWrapMode.Truncate;
                UiFactory.SetRect((RectTransform)lbl.transform, Vector2.zero, Vector2.one,
                                  new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(-20, 0));
            }

            int stamps = save != null ? save.pawStamps : 0;
            header.text = L.Get("ui.catalog.discovered", ("a", discovered.ToString()), ("b", total.ToString()))
                          + "    " + L.Get("ui.catalog.stamps", ("n", stamps.ToString()))
                          + "    " + L.Get("ui.catalog.gifts", ("n", (save != null ? save.giftsCollected.Count : 0).ToString()),
                                           ("t", YeogiCafe.Data.GiftCatalog.All.Length.ToString()));
            RenderStamps(stamps);
        }

        // ── 관계도: 만난 고양이를 원으로 놓고 사이를 선으로(친구=분홍 굵게, 아는 사이=옅게, 앙숙=빨강) ──
        void RenderRelations()
        {
            for (int i = relRoot.childCount - 1; i >= 0; i--) Object.Destroy(relRoot.GetChild(i).gameObject);
            var save = owner.Save; if (save == null) return;
            var met = new List<CatData>();
            foreach (var d in owner.AllCats) if (d != null && save.GetOrCreateCat(d.catId).visitCount > 0) met.Add(d);
            if (met.Count == 0) return;
            var pos = new Dictionary<string, Vector2>();
            for (int i = 0; i < met.Count; i++)
            {
                float a = Mathf.PI * 0.5f - i * Mathf.PI * 2f / met.Count;
                pos[met[i].catId] = new Vector2(Mathf.Cos(a) * 250f, Mathf.Sin(a) * 160f);
            }
            // 선 먼저(아이콘 아래)
            foreach (var r in save.relations)
            {
                if (!pos.ContainsKey(r.catA) || !pos.ContainsKey(r.catB)) continue;
                bool rival = r.fights >= 2 || r.bond <= -2;
                bool friend = r.stage >= 2 || r.bond >= 3;
                bool known = r.stage >= 1 || r.bond >= 1 || r.fights >= 1;
                if (!rival && !friend && !known) continue;
                Color col = rival ? new Color(0.84f, 0.32f, 0.33f) : friend ? UiFactory.PinkDeep : new Color(0.72f, 0.6f, 0.5f);
                float th = friend || rival ? 6f : 3f;
                Vector2 a = pos[r.catA], b = pos[r.catB], d = b - a;
                var line = UiFactory.Panel("L", relRoot, col);
                line.pivot = new Vector2(0.5f, 0.5f); line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
                line.anchoredPosition = (a + b) * 0.5f; line.sizeDelta = new Vector2(d.magnitude - 70f, th);
                line.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                line.GetComponent<Image>().raycastTarget = false;
                string tag = rival ? L.Get("ui.rel.rival") : friend ? L.Get("ui.rel.friend") : "";
                if (tag != "")
                {
                    var t = UiFactory.Label("Tag", relRoot, tag, 14, TextAnchor.MiddleCenter, col);
                    var trt = (RectTransform)t.transform;
                    trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f); trt.anchoredPosition = (a + b) * 0.5f + new Vector2(0, 12); trt.sizeDelta = new Vector2(80, 20);
                }
            }
            foreach (var d in met)
            {
                var g = new GameObject("Cat_" + d.catId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                g.transform.SetParent(relRoot, false);
                var img = g.GetComponent<Image>(); img.sprite = YeogiCafe.Art.CatSpriteBank.Portrait(d.catId); img.preserveAspect = true; img.raycastTarget = false;
                var rt = (RectTransform)g.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos[d.catId]; rt.sizeDelta = new Vector2(84, 70);
                var n = UiFactory.Label("N", relRoot, L.Get($"cat.{d.catId}.name"), 14, TextAnchor.UpperCenter, UiFactory.Ink);
                var nrt = (RectTransform)n.transform; nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0.5f);
                nrt.anchoredPosition = pos[d.catId] + new Vector2(0, -44); nrt.sizeDelta = new Vector2(100, 20);
            }
        }

        // 하루를 마칠 때마다 찍은 손도장(최근 12개까지 표시)
        RectTransform stampRow;
        void RenderStamps(int n)
        {
            if (stampRow == null)
            {
                var go = new GameObject("Stamps", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                go.transform.SetParent(header.transform.parent, false);
                stampRow = (RectTransform)go.transform;
                UiFactory.SetRect(stampRow, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(-60, 34));
                var h = go.GetComponent<HorizontalLayoutGroup>();
                h.spacing = 6; h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlWidth = false; h.childControlHeight = false; h.childForceExpandWidth = false;
            }
            for (int i = stampRow.childCount - 1; i >= 0; i--) Object.Destroy(stampRow.GetChild(i).gameObject);
            var sp = YeogiCafe.Art.RuntimeSprites.Get("ui_pawstamp", YeogiCafe.Art.PawDrawing.Stamp);
            for (int i = 0; i < Mathf.Min(n, 12); i++)
            {
                var g = new GameObject("S" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                g.transform.SetParent(stampRow, false);
                var img = g.GetComponent<Image>(); img.sprite = sp; img.raycastTarget = false;
                ((RectTransform)g.transform).sizeDelta = new Vector2(30, 30);
                g.transform.localEulerAngles = new Vector3(0, 0, ((i * 37) % 21) - 10);
            }
        }

        public void Toggle()
        {
            bool next = !rootGo.activeSelf;
            rootGo.SetActive(next);
            if (next) { Render(); rootGo.transform.SetAsLastSibling(); }   // 주방 화면 위로(겹침 방지)
        }
        public void Hide() => rootGo.SetActive(false);
    }
}
