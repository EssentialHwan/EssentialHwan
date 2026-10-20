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
        RectTransform listView;
        readonly GameObject rootGo;
        Text header;
        RectTransform listContent;
        RectTransform relRoot; Text tabLabel; bool showRel, showReview;
        Text reviewTabLabel;

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

            // 고양이가 늘어 한 화면을 넘으면 휠/드래그로 내려 본다
            var viewGo = new GameObject("ListView", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image));
            viewGo.transform.SetParent(panel.transform, false);
            var view = (RectTransform)viewGo.transform;
            UiFactory.SetRect(view, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -22), new Vector2(-48, -178));
            viewGo.GetComponent<Image>().color = new Color(1, 1, 1, 0);      // 빈 곳도 휠·드래그가 먹게
            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(view, false);
            listContent = (RectTransform)listGo.transform;
            listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = new Vector2(1, 1); listContent.pivot = new Vector2(0.5f, 1);
            listContent.anchoredPosition = Vector2.zero; listContent.sizeDelta = new Vector2(0, 0);
            listGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = viewGo.GetComponent<ScrollRect>();
            sr.content = listContent; sr.viewport = view; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 24f;
            listView = view;
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
            tab.onClick.AddListener(() => { showRel = !showRel; showReview = false; Render(); });
            // 탭: 리뷰(고양이 손님 한 줄 평 · 평판 · 냥슐랭)
            var rtab = UiFactory.Button("ReviewTab", panel.transform, L.Get("ui.catalog.reviewtab"), UiFactory.Wood, UiFactory.Cream, out reviewTabLabel);
            reviewTabLabel.fontSize = 16;
            UiFactory.SetRect((RectTransform)rtab.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, -10), new Vector2(96, 40));
            rtab.onClick.AddListener(() => { showReview = !showReview; showRel = false; Render(); });
            var relGo = new GameObject("Relations", typeof(RectTransform));
            relGo.transform.SetParent(panel.transform, false);
            relRoot = (RectTransform)relGo.transform;
            UiFactory.SetRect(relRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(660, 400));

            rootGo.SetActive(false);
        }

        void RenderReviews()
        {
            listView.gameObject.SetActive(true);
            relRoot.gameObject.SetActive(false);
            tabLabel.text = L.Get("ui.catalog.reltab");
            var s = owner.Save;
            float rep = YeogiCafe.Food.ReviewBook.Reputation(s);
            int stars = s != null ? s.nyanStars : 0;
            header.text = L.Get("ui.review.header", ("rep", rep.ToString("0.0")), ("stars", YeogiCafe.Food.NyanGuide.Stars(stars)),
                                ("need", YeogiCafe.Food.NyanGuide.NextPassScore(stars).ToString()));
            if (s == null || s.reviews.Count == 0)
            {
                var e = UiFactory.Label("Empty", listContent, L.Get("ui.review.empty"), 17, TextAnchor.MiddleCenter, UiFactory.WoodDark);
                ((RectTransform)e.transform).sizeDelta = new Vector2(0, 60);
                return;
            }
            int shown = 0;
            for (int i = s.reviews.Count - 1; i >= 0 && shown < 9; i--, shown++)
            {
                var r = s.reviews[i];
                var row = UiFactory.Panel("Review", listContent, UiFactory.Cream);
                UiFactory.Frame(row, UiFactory.FrameStyle.Card, UiFactory.Cream);
                ((RectTransform)row.transform).sizeDelta = new Vector2(0, 38);
                string who = L.Get($"cat.{r.catId}.name");
                var paws = UiFactory.Label("Paws", row.transform, YeogiCafe.Food.ReviewBook.Paws(r.paws), 15, TextAnchor.MiddleLeft, UiFactory.PinkDeep);
                UiFactory.SetRect((RectTransform)paws.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(84, 0));
                var lbl = UiFactory.Label("Text", row.transform, $"{who} · {L.Get("ui.cafe.day", ("n", r.day.ToString()))}   {r.text}", 15, TextAnchor.MiddleLeft, UiFactory.Ink);
                lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 10; lbl.resizeTextMaxSize = 15;
                lbl.verticalOverflow = VerticalWrapMode.Truncate;
                UiFactory.SetRect((RectTransform)lbl.transform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), new Vector2(104, 0), new Vector2(-120, 0));
            }
        }

        void ClearRows()
        {
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Object.Destroy(listContent.GetChild(i).gameObject);
        }

        void Render()
        {
            ClearRows();
            listContent.anchoredPosition = Vector2.zero;          // 탭을 바꾸면 맨 위부터
            reviewTabLabel.text = L.Get(showReview ? "ui.catalog.listtab" : "ui.catalog.reviewtab");
            if (showReview) { RenderReviews(); return; }
            listView.gameObject.SetActive(!showRel);
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
                ((RectTransform)row.transform).sizeDelta = new Vector2(0, 36);

                // 숨은 고양이: 소문만(두 줄 글)
                if (!visited && data.unlockCondition != null && data.unlockCondition.type == UnlockType.FurnitureSet)
                {
                    ((RectTransform)row.transform).sizeDelta = new Vector2(0, 48);
                    var rl = UiFactory.Label("Rumor", row.transform, "???    " + L.Get("ui.catalog.rumor"), 15, TextAnchor.MiddleLeft, UiFactory.Ink);
                    rl.resizeTextForBestFit = true; rl.resizeTextMinSize = 10; rl.resizeTextMaxSize = 15; rl.verticalOverflow = VerticalWrapMode.Truncate;
                    UiFactory.SetRect((RectTransform)rl.transform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(-32, -6));
                    continue;
                }

                // 얼굴 아이콘(아직 못 만난 고양이는 ?)
                if (visited) RowIcon(row, YeogiCafe.Art.CatSpriteBank.Portrait(data.catId), 10, 34);
                else RowMark(row, "?", 10, 34, UiFactory.WoodDark);
                string marks = "";
                if (cs != null && cs.bossSeen) marks += L.Get("ui.catalog.boss") + " ";
                if (cs != null && cs.graduated) marks += L.Get("ui.catalog.grad");
                if (marks != "")
                {
                    var ml = UiFactory.Label("Marks", row.transform, marks.Trim(), 11, TextAnchor.LowerLeft, UiFactory.PinkDeep);
                    UiFactory.SetRect((RectTransform)ml.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(52, 0), new Vector2(46, -6));
                    ml.resizeTextForBestFit = true; ml.resizeTextMinSize = 8; ml.resizeTextMaxSize = 11;
                }

                // 음식·좌석·시설: 맞히면 그 메뉴/가구 그림, 거의 확실이면 △, 그 외 ?
                AxisCell(row, data, cs, PrefAxis.Food, 100);
                AxisCell(row, data, cs, PrefAxis.Seat, 196);
                AxisCell(row, data, cs, PrefAxis.Facility, 292);

                string touchKey = cs != null ? YeogiCafe.AI.CatTemperament.ObservedKey(cs.touchSeen) : null;
                string touch = $"{L.Get("ui.axis.touch")} {(touchKey != null ? L.Get(touchKey) : "?")}";
                if (cs != null && cs.likeZones != 0) touch += " " + L.Get("ui.catalog.likes", ("z", ClosingPlayPanel.Zones(cs.likeZones)));
                if (cs != null && cs.hateZones != 0) touch += " " + L.Get("ui.catalog.hates", ("z", ClosingPlayPanel.Zones(cs.hateZones)));
                if (cs != null && cs.suckleSeen) touch += " " + L.Get("ui.catalog.suckle");
                var lbl = UiFactory.Label("Touch", row.transform, touch, 15, TextAnchor.MiddleLeft, UiFactory.Ink);
                lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 10; lbl.resizeTextMaxSize = 15;
                lbl.verticalOverflow = VerticalWrapMode.Truncate;
                UiFactory.SetRect((RectTransform)lbl.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(390, 0), new Vector2(-404, -4));
            }

            int stamps = save != null ? save.pawStamps : 0;
            var goal = EndingPanel.Progress(save, all);
            header.text = L.Get(save != null && save.endingSeen ? "ui.catalog.goaldone" : "ui.catalog.goal", ("a", goal.done.ToString()), ("b", goal.total.ToString()))
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
                pos[met[i].catId] = new Vector2(Mathf.Cos(a) * 250f, Mathf.Sin(a) * 125f + 10f);   // 아래 범례 자리를 비워 둔다
            }
            // 관계 종류(색·이름이 모두 다르게): 앙숙 · 티격태격 · 단짝 · 친구 · 아는 사이
            // 글자 칸끼리, 그리고 고양이 아이콘·이름과 겹치지 않게 선을 따라 자리를 옮겨 둔다
            var taken = new List<Rect>();
            foreach (var kv in pos) { taken.Add(new Rect(kv.Value.x - 44, kv.Value.y - 56, 88, 92)); }   // 아이콘+이름 칸
            var tags = new List<(Vector2 at, string text, Color col)>();
            foreach (var r in save.relations)
            {
                if (!pos.ContainsKey(r.catA) || !pos.ContainsKey(r.catB)) continue;
                var kind = RelKind(r);
                if (kind < 0) continue;
                Color col = RelColor(kind);
                float th = kind == 0 || kind == 2 ? 6f : kind == 4 ? 2f : 4f;
                Vector2 a = pos[r.catA], b = pos[r.catB], d = b - a;
                var line = UiFactory.Panel("L", relRoot, kind == 4 ? new Color(col.r, col.g, col.b, 0.55f) : col);
                line.pivot = new Vector2(0.5f, 0.5f); line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
                line.anchoredPosition = (a + b) * 0.5f; line.sizeDelta = new Vector2(Mathf.Max(0f, d.magnitude - 70f), th);
                line.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                line.GetComponent<Image>().raycastTarget = false;
                // 글자 자리: 가운데부터 양쪽으로 비켜 가며 빈 곳 찾기
                Vector2 at = (a + b) * 0.5f; var tr = new Rect(at.x - 34, at.y - 11, 68, 22);
                foreach (float t in new[] { 0.5f, 0.38f, 0.62f, 0.3f, 0.7f, 0.44f, 0.56f, 0.25f, 0.75f })
                {
                    var c = Vector2.Lerp(a, b, t); var rr = new Rect(c.x - 34, c.y - 11, 68, 22);
                    bool hit = false; foreach (var q in taken) if (q.Overlaps(rr)) { hit = true; break; }
                    at = c; tr = rr;
                    if (!hit) break;
                }
                taken.Add(tr);
                tags.Add((at, L.Get(RelKey(kind)), col));
            }
            // 글자는 선 위에(색 배지 + 흰 글자)
            foreach (var (at, text, col) in tags)
            {
                var pill = UiFactory.Panel("TagBg", relRoot, col);
                pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 0.5f); pill.pivot = new Vector2(0.5f, 0.5f);
                pill.anchoredPosition = at; pill.sizeDelta = new Vector2(64, 20);
                pill.GetComponent<Image>().raycastTarget = false;
                var t = UiFactory.Label("Tag", pill, text, 13, TextAnchor.MiddleCenter, Color.white);
                UiFactory.Anchor((RectTransform)t.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                t.raycastTarget = false;
            }
            // 범례(아래쪽)
            {
                int[] kinds = { 2, 3, 4, 1, 0 };
                float lx = -((kinds.Length - 1) * 92f) / 2f;
                for (int i = 0; i < kinds.Length; i++)
                {
                    var sw = UiFactory.Panel("Legend", relRoot, RelColor(kinds[i]));
                    sw.anchorMin = sw.anchorMax = new Vector2(0.5f, 0f); sw.pivot = new Vector2(0.5f, 0.5f);
                    sw.anchoredPosition = new Vector2(lx + i * 92f - 30f, 10f); sw.sizeDelta = new Vector2(14, 6);
                    var lt = UiFactory.Label("LegendT", relRoot, L.Get(RelKey(kinds[i])), 13, TextAnchor.MiddleLeft, UiFactory.WoodDark);
                    var lrt = (RectTransform)lt.transform; lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f); lrt.pivot = new Vector2(0f, 0.5f);
                    lrt.anchoredPosition = new Vector2(lx + i * 92f - 20f, 10f); lrt.sizeDelta = new Vector2(70, 18);
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

        // ── 도감 줄 칸 도우미 ──
        static void RowIcon(RectTransform row, Sprite sp, float x, float size)
        {
            var g = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            g.transform.SetParent(row, false);
            var im = g.GetComponent<Image>(); im.sprite = sp; im.preserveAspect = true; im.raycastTarget = false;
            UiFactory.SetRect((RectTransform)g.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(size, size));
        }
        static void RowMark(RectTransform row, string mark, float x, float size, Color col)
        {
            var t = UiFactory.Label("Mark", row, mark, 20, TextAnchor.MiddleCenter, col);
            UiFactory.SetRect((RectTransform)t.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(size, size));
        }

        void AxisCell(RectTransform row, CatData data, CatSaveData cs, PrefAxis axis, float x)
        {
            var lab = UiFactory.Label("Axis", row, L.Get(axis == PrefAxis.Food ? "ui.axis.food" : axis == PrefAxis.Seat ? "ui.axis.seat" : "ui.axis.facility"), 13, TextAnchor.MiddleLeft, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)lab.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(40, 20));
            lab.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rec = cs != null ? cs.observation.Get(axis) : null;
            float cx = x + 40;
            if (rec != null && rec.state == ObsState.Recorded)
            {
                var sp = AnswerSprite(data, axis, rec.recordedValue);
                if (sp != null) { RowIcon(row, sp, cx, 30); return; }
                RowMark(row, "○", cx, 30, UiFactory.Green);
                return;
            }
            if (rec != null && rec.state == ObsState.Confirmable) { RowMark(row, "△", cx, 30, UiFactory.PinkDeep); return; }
            RowMark(row, "?", cx, 30, UiFactory.WoodDark);
        }

        // 맞힌 답의 그림: 기록값이 메뉴/가구 id면 그것, 태그면 그 취향의 대표 메뉴/가구
        Sprite AnswerSprite(CatData data, PrefAxis axis, string v)
        {
            if (axis == PrefAxis.Food)
            {
                string id = !string.IsNullOrEmpty(v) && v.StartsWith("menu_") ? v : data.foodFavoriteMenuId;
                return string.IsNullOrEmpty(id) ? null : KitchenView.DishSprite(id);
            }
            if (!string.IsNullOrEmpty(v) && v.StartsWith("furn_"))
            {
                var f = owner.LookupFurniture(v);
                if (f != null && f.worldSprite != null) return f.worldSprite;
            }
            foreach (var f in owner.AllFurniture)
            {
                if (f == null || f.worldSprite == null) continue;
                if (axis == PrefAxis.Seat && f.seatTags != null && System.Array.IndexOf(f.seatTags, data.seatPreference) >= 0) return f.worldSprite;
                if (axis == PrefAxis.Facility && f.facilityTags != null && System.Array.IndexOf(f.facilityTags, data.facilityPreference) >= 0) return f.worldSprite;
            }
            return null;
        }

        // 관계 종류: 0 앙숙 · 1 티격태격 · 2 단짝 · 3 친구 · 4 아는 사이 · -1 표시 안 함
        static int RelKind(RelationSaveData r)
        {
            if (r.fights >= 2 || r.bond <= -2) return 0;
            if (r.fights >= 1 || r.bond < 0) return 1;
            if (r.stage >= 2 || r.bond >= 3) return 2;
            if (r.bond >= 1) return 3;
            if (r.stage >= 1) return 4;
            return -1;
        }
        static string RelKey(int k) => k switch { 0 => "ui.rel.rival", 1 => "ui.rel.bicker", 2 => "ui.rel.friend", 3 => "ui.rel.pal", _ => "ui.rel.known" };
        static Color RelColor(int k) => k switch
        {
            0 => new Color(0.82f, 0.25f, 0.27f),     // 앙숙: 빨강
            1 => new Color(0.93f, 0.55f, 0.2f),      // 티격태격: 주황
            2 => new Color(0.88f, 0.38f, 0.62f),     // 단짝: 분홍
            3 => new Color(0.38f, 0.66f, 0.38f),     // 친구: 초록
            _ => new Color(0.55f, 0.55f, 0.66f),     // 아는 사이: 회보라
        };

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
        public bool IsOpen => rootGo != null && rootGo.activeSelf;
    }
}
