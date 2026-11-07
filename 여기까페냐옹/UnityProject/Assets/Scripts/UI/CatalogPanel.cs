using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YeogiCafe.Art;
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
        ScrollRect scroll;
        const float WheelStep = 40f;            // 휠 한 칸 = 한 줄

        // 휠 스크롤: 한 칸에 한 줄씩(도감이 열려 있고 목록 위에 마우스가 있을 때)
        public void Tick()
        {
            if (!IsOpen || listView == null || !listView.gameObject.activeInHierarchy) return;
            float d = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(d, 0f)) return;
            if (!RectTransformUtility.RectangleContainsScreenPoint(listView, Input.mousePosition, null)) return;
            float max = Mathf.Max(0f, listContent.rect.height - listView.rect.height);
            var p = listContent.anchoredPosition;
            p.y = Mathf.Clamp(p.y - Mathf.Sign(d) * WheelStep, 0f, max);
            listContent.anchoredPosition = p;
            if (scroll != null) scroll.StopMovement();
        }
        readonly GameObject rootGo;
        Text header;
        RectTransform listContent;
        RectTransform relRoot; Text tabLabel; bool showRel, showReview, showStory;
        Text reviewTabLabel, storyTabLabel;
        GameObject storyCardGo;

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
            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(view, false);
            listContent = (RectTransform)listGo.transform;
            listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = new Vector2(1, 1); listContent.pivot = new Vector2(0.5f, 1);
            listContent.anchoredPosition = Vector2.zero; listContent.sizeDelta = new Vector2(0, 0);
            var sr = viewGo.GetComponent<ScrollRect>();
            sr.content = listContent; sr.viewport = view; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 0f;          // 휠은 Tick에서 직접(에디터·기기마다 휠 값이 0.1~1로 달라 너무 느리던 문제)
            // 오른쪽 스크롤바: 더 내려 볼 게 있다는 걸 보여 준다
            var barGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            barGo.transform.SetParent(panel.transform, false);
            var bar = (RectTransform)barGo.transform;
            UiFactory.SetRect(bar, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-14, -22), new Vector2(8, -178));
            barGo.GetComponent<Image>().color = new Color(0.55f, 0.42f, 0.32f, 0.25f);
            var handle = UiFactory.Panel("Handle", bar, UiFactory.WoodDark);
            UiFactory.Anchor(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var sb = barGo.GetComponent<Scrollbar>();
            sb.handleRect = handle; sb.targetGraphic = handle.GetComponent<Image>(); sb.direction = Scrollbar.Direction.BottomToTop;
            sr.verticalScrollbar = sb; sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll = sr;
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
            tab.onClick.AddListener(() => { showRel = !showRel; showReview = false; showStory = false; Render(); });
            // 탭: 리뷰(고양이 손님 한 줄 평 · 평판 · 냥슐랭)
            var rtab = UiFactory.Button("ReviewTab", panel.transform, L.Get("ui.catalog.reviewtab"), UiFactory.Wood, UiFactory.Cream, out reviewTabLabel);
            reviewTabLabel.fontSize = 16;
            UiFactory.SetRect((RectTransform)rtab.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, -10), new Vector2(96, 40));
            rtab.onClick.AddListener(() => { showReview = !showReview; showRel = false; showStory = false; Render(); });
            // 탭: 이야기(지금까지 본 단골 편지·기억·졸업 편지·옛 사장님의 노트 다시 보기) — 오른쪽, 닫기 버튼 옆
            var stab = UiFactory.Button("StoryTab", panel.transform, L.Get("ui.catalog.storytab"), UiFactory.Wood, UiFactory.Cream, out storyTabLabel);
            storyTabLabel.fontSize = 16;
            UiFactory.SetRect((RectTransform)stab.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-66, -10), new Vector2(96, 40));
            stab.onClick.AddListener(() => { showStory = !showStory; showRel = false; showReview = false; Render(); });
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
            bool any = false;
            if (s != null) foreach (var rv in s.reviews) if (YeogiCafe.Food.ReviewBook.Visible(rv)) { any = true; break; }
            if (!any)
            {
                var e = UiFactory.Label("Empty", listContent, L.Get("ui.review.empty"), 17, TextAnchor.MiddleCenter, UiFactory.WoodDark);
                ((RectTransform)e.transform).sizeDelta = new Vector2(0, 60);
                FitList();
                return;
            }
            int shown = 0;
            for (int i = s.reviews.Count - 1; i >= 0 && shown < 30; i--)
            {
                var r = s.reviews[i];
                if (!YeogiCafe.Food.ReviewBook.Visible(r)) continue;
                shown++;
                var row = UiFactory.Panel("Review", listContent, UiFactory.Cream);
                UiFactory.Frame(row, UiFactory.FrameStyle.Card, UiFactory.Cream);
                ((RectTransform)row.transform).sizeDelta = new Vector2(0, 38);
                string who = L.Get($"cat.{r.catId}.name");
                var paws = UiFactory.Label("Paws", row.transform, YeogiCafe.Food.ReviewBook.Paws(r.paws), 15, TextAnchor.MiddleLeft, UiFactory.PinkDeep);
                UiFactory.SetRect((RectTransform)paws.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(84, 0));
                var lbl = UiFactory.Label("Text", row.transform, $"{who} · {L.Get("ui.cafe.day", ("n", r.day.ToString()))}   {YeogiCafe.Food.ReviewBook.Text(r)}", 15, TextAnchor.MiddleLeft, UiFactory.Ink);
                lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 10; lbl.resizeTextMaxSize = 15;
                lbl.verticalOverflow = VerticalWrapMode.Truncate;
                UiFactory.SetRect((RectTransform)lbl.transform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), new Vector2(104, 0), new Vector2(-120, 0));
            }
            FitList();
        }

        void RenderStories()
        {
            listView.gameObject.SetActive(true);
            relRoot.gameObject.SetActive(false);
            tabLabel.text = L.Get("ui.catalog.reltab");
            var s = owner.Save;
            var all = owner.AllCats;
            int mem = 0, cats = 0;
            if (s != null) foreach (var d in all) if (d != null) { cats++; var c = s.cats.Find(x => x != null && x.catId == d.catId); if (c != null && c.memorySeen) mem++; }
            header.text = L.Get("ui.story.header", ("a", (s != null ? s.notesFound.Count : 0).ToString()), ("b", YeogiCafe.Progression.StoryBook.NoteCount.ToString()),
                                ("c", mem.ToString()), ("d", cats.ToString()));
            var list = YeogiCafe.Progression.StoryBook.Seen(s, all);
            if (list.Count == 0)
            {
                var e = UiFactory.Label("Empty", listContent, L.Get("ui.story.empty"), 17, TextAnchor.MiddleCenter, UiFactory.WoodDark);
                ((RectTransform)e.transform).sizeDelta = new Vector2(0, 60);
                FitList();
                return;
            }
            var stamp = RuntimeSprites.Get("ui_pawstamp", PawDrawing.Stamp);
            foreach (var st in list)
            {
                bool note = st.kind == YeogiCafe.Progression.StoryBook.KindNote;
                bool memory = st.kind == YeogiCafe.Progression.StoryBook.KindMemory;
                Color bg = note ? new Color(0.99f, 0.97f, 0.9f, 1f) : memory ? new Color(0.93f, 0.86f, 0.74f, 1f) : UiFactory.Cream;
                var row = UiFactory.Panel("Story", listContent, bg);
                UiFactory.Frame(row, UiFactory.FrameStyle.Card, bg);
                ((RectTransform)row.transform).sizeDelta = new Vector2(0, 38);
                if (note) RowIcon(row, RuntimeSprites.Get("ui_note_icon", StoryCard.NoteIcon), 12, 30);
                else RowIcon(row, YeogiCafe.Art.CatSpriteBank.Portrait(st.catId), 10, 34);
                string name = st.catId != null ? L.Get($"cat.{st.catId}.name") : "";
                string t = note ? L.Get("story.title.note", ("n", st.note.ToString()), ("t", YeogiCafe.Progression.StoryBook.NoteCount.ToString()))
                                : L.Get(YeogiCafe.Progression.StoryBook.TitleKey(st), ("name", name));
                var lbl = UiFactory.Label("Text", row.transform, t, 16, TextAnchor.MiddleLeft, memory ? new Color(0.45f, 0.32f, 0.22f, 1f) : UiFactory.Ink);
                lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 10; lbl.resizeTextMaxSize = 16;
                lbl.verticalOverflow = VerticalWrapMode.Truncate;
                UiFactory.SetRect((RectTransform)lbl.transform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), new Vector2(56, 0), new Vector2(-72, 0));
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = row.GetComponent<Image>();
                var dto = st;
                btn.onClick.AddListener(() =>
                {
                    if (storyCardGo != null) Object.Destroy(storyCardGo);
                    storyCardGo = StoryCard.Open(rootGo.transform, dto, stamp, () => { Object.Destroy(storyCardGo); storyCardGo = null; }, L.Get("ui.story.close"));
                });
            }
            FitList();
        }

        void ClearRows()
        {
            // 지울 줄은 바로 떼어 낸다(Destroy는 프레임 끝이라 남아 있으면 목록 높이 계산에 섞인다)
            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                var c = listContent.GetChild(i);
                c.SetParent(null, false);
                Object.Destroy(c.gameObject);
            }
        }

        // 스크롤 범위 = 줄 높이 합(자동 맞춤에 기대지 않고 직접 잰다)
        void FitList()
        {
            var vlg = listContent.GetComponent<VerticalLayoutGroup>();
            float h = 0; int n = 0;
            for (int i = 0; i < listContent.childCount; i++)
            {
                var c = (RectTransform)listContent.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                h += c.sizeDelta.y; n++;
            }
            if (n > 1) h += vlg.spacing * (n - 1);
            h += vlg.padding.top + vlg.padding.bottom;
            listContent.sizeDelta = new Vector2(listContent.sizeDelta.x, h);
            listContent.anchoredPosition = Vector2.zero;
        }

        void Render()
        {
            ClearRows();
            listContent.anchoredPosition = Vector2.zero;          // 탭을 바꾸면 맨 위부터
            reviewTabLabel.text = L.Get(showReview ? "ui.catalog.listtab" : "ui.catalog.reviewtab");
            storyTabLabel.text = L.Get(showStory ? "ui.catalog.listtab" : "ui.catalog.storytab");
            if (showStory) { RenderStories(); return; }
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
                // 이름(아이콘만으로는 비슷한 무늬끼리 헷갈린다) — 윗줄 이름, 아랫줄 대장·졸업 표시
                var nm = UiFactory.Label("Name", row.transform, visited ? L.Get($"cat.{data.catId}.name") : "???", 13,
                                         marks != "" ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft, UiFactory.Ink);
                UiFactory.SetRect((RectTransform)nm.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(48, -4));
                UiFactory.FitOneLine(nm, 46, 9);
                if (marks != "")
                {
                    var ml = UiFactory.Label("Marks", row.transform, marks.Trim(), 11, TextAnchor.LowerLeft, UiFactory.PinkDeep);
                    UiFactory.SetRect((RectTransform)ml.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(48, -4));
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
            FitList();
        }

        // ── 관계도(11/02 v2): 위에 만난 고양이 버튼 줄 → 누른 고양이를 가운데, 그 고양이와 얽힌 고양이만 빙 둘러 놓고 선으로 잇는다.
        //  예전(전원을 둘레에 놓고 모든 선을 그림)은 고양이가 늘면 선이 얽혀 누가 누구와 무슨 사이인지 읽을 수 없었다.
        string relFocus;
        void RenderRelations()
        {
            for (int i = relRoot.childCount - 1; i >= 0; i--) Object.Destroy(relRoot.GetChild(i).gameObject);
            var save = owner.Save; if (save == null) return;
            var met = new List<CatData>();
            foreach (var d in owner.AllCats) if (d != null && save.GetOrCreateCat(d.catId).visitCount > 0) met.Add(d);
            if (met.Count == 0) return;

            // 고양이별 관계 목록
            var links = new Dictionary<string, List<(string other, int kind)>>();
            foreach (var d in met) links[d.catId] = new List<(string, int)>();
            foreach (var r in save.relations)
            {
                if (!links.ContainsKey(r.catA) || !links.ContainsKey(r.catB)) continue;
                int kind = RelKind(r); if (kind < 0) continue;
                links[r.catA].Add((r.catB, kind)); links[r.catB].Add((r.catA, kind));
            }
            if (relFocus == null || !links.ContainsKey(relFocus))
            {   // 처음엔 관계가 가장 많은 고양이
                relFocus = met[0].catId;
                foreach (var d in met) if (links[d.catId].Count > links[relFocus].Count) relFocus = d.catId;
            }

            // ① 위: 고양이 버튼 줄(누르면 그 고양이 중심으로)
            float bw = Mathf.Min(40f, 640f / met.Count), bx0 = -(met.Count - 1) * bw * 0.5f;
            for (int i = 0; i < met.Count; i++)
            {
                string id = met[i].catId; bool on = id == relFocus;
                var b = UiFactory.Button("Pick_" + id, relRoot, "", on ? UiFactory.Pink : UiFactory.Cream, UiFactory.Ink, out var bl);
                bl.text = "";
                var brt = (RectTransform)b.transform; brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f); brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(bx0 + i * bw, 178f); brt.sizeDelta = new Vector2(bw - 3f, 38f);
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                ic.transform.SetParent(b.transform, false);
                var im = ic.GetComponent<Image>(); im.sprite = YeogiCafe.Art.CatSpriteBank.Portrait(id); im.preserveAspect = true; im.raycastTarget = false;
                UiFactory.Anchor((RectTransform)ic.transform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
                b.onClick.AddListener(() => { relFocus = id; RenderRelations(); });
            }

            // ② 가운데 고양이 + 둘레의 관계 고양이(단짝·친구 → 아는 사이 → 티격태격·앙숙 순)
            //  정확히 아래(가운데 이름표와 겹침)엔 놓지 않는다: 홀수면 맨 위에서, 짝수면 반 칸 돌려서 시작.
            //  위쪽 고양이는 이름·관계를 그림 '위'에 써서, 가운데서 올라오는 선이 글자를 지나가지 않게 한다.
            Vector2 c0 = new Vector2(0f, -22f);
            var around = links[relFocus];
            around.Sort((x, y) => Order(x.kind).CompareTo(Order(y.kind)));
            static int Order(int q) => q == 2 ? 0 : q == 3 ? 1 : q == 4 ? 2 : q == 1 ? 3 : 4;
            int n = around.Count;
            float k = n > 10 ? 0.82f : 1f;
            for (int i = 0; i < n; i++)
            {
                float ang = n <= 2 ? (i == 0 ? 0f : Mathf.PI) : Mathf.PI * 0.5f - i * Mathf.PI * 2f / n + (n % 2 == 0 ? Mathf.PI / n : 0f);
                float sy = Mathf.Sin(ang);
                Vector2 p = c0 + new Vector2(Mathf.Cos(ang) * 250f, sy * (sy > 0f ? 100f : 92f));
                bool up = p.y > c0.y + 30f;
                int kind = around[i].kind; Color col = RelColor(kind);
                float th = kind == 0 || kind == 2 ? 7f : kind == 4 ? 4f : 5f;
                Vector2 d = p - c0, dir = d.normalized;
                float ax = Mathf.Abs(dir.x), ay = Mathf.Abs(dir.y);
                Vector2 pa = c0 + dir * (ax * 50f + ay * (dir.y > 0f ? 42f : 66f));     // 가운데: 그림 + (아래쪽이면) 이름표
                Vector2 pb = p - dir * ((ax * 36f + ay * 28f) * k + (up ? 0f : ay * 4f));
                float len = Mathf.Max(0f, Vector2.Dot(pb - pa, dir)), deg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                RelLine(relRoot, (pa + pb) * 0.5f, len + 2f, th + 2f, deg, new Color(0.25f, 0.18f, 0.14f, kind == 4 ? 0.18f : 0.32f));
                RelLine(relRoot, (pa + pb) * 0.5f, len, th, deg, kind == 4 ? new Color(col.r, col.g, col.b, 0.8f) : col);
                RelCat(around[i].other, p, 64f * k, 0.9f, up);
                var t = UiFactory.Label("Kind", relRoot, L.Get(RelKey(kind)), 13, TextAnchor.MiddleCenter, Color.Lerp(col, Color.black, 0.25f));
                var trt = (RectTransform)t.transform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.anchoredPosition = p + new Vector2(0, up ? 64f * k * 0.42f + 34f : -64f * k * 0.42f - 34f); trt.sizeDelta = new Vector2(110, 18);
            }
            RelCat(relFocus, c0, 96f, 1f, false);
            if (n == 0)
            {
                var t = UiFactory.Label("None", relRoot, L.Get("ui.rel.none", ("name", L.Get($"cat.{relFocus}.name"))), 15, TextAnchor.MiddleCenter, UiFactory.WoodDark);
                var trt = (RectTransform)t.transform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.anchoredPosition = c0 + new Vector2(0, -80f); trt.sizeDelta = new Vector2(520, 24);
            }

            // ③ 범례(아래쪽)
            {
                int[] kinds = { 2, 3, 4, 1, 0 };
                float lx = -((kinds.Length - 1) * 92f) / 2f;
                for (int i = 0; i < kinds.Length; i++)
                {
                    var sw = UiFactory.Panel("Legend", relRoot, RelColor(kinds[i]));
                    sw.anchorMin = sw.anchorMax = new Vector2(0.5f, 0f); sw.pivot = new Vector2(0.5f, 0.5f);
                    sw.anchoredPosition = new Vector2(lx + i * 92f - 30f, 10f); sw.sizeDelta = new Vector2(18, kinds[i] == 4 ? 4 : kinds[i] == 0 || kinds[i] == 2 ? 7 : 5);
                    var lt = UiFactory.Label("LegendT", relRoot, L.Get(RelKey(kinds[i])), 13, TextAnchor.MiddleLeft, UiFactory.WoodDark);
                    var lrt = (RectTransform)lt.transform; lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f); lrt.pivot = new Vector2(0f, 0.5f);
                    lrt.anchoredPosition = new Vector2(lx + i * 92f - 20f, 10f); lrt.sizeDelta = new Vector2(70, 18);
                }
            }
        }

        // 관계도의 고양이 하나(그림 + 이름)
        void RelCat(string id, Vector2 at, float size, float nameScale, bool nameAbove)
        {
            var g = new GameObject("Cat_" + id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            g.transform.SetParent(relRoot, false);
            var img = g.GetComponent<Image>(); img.sprite = YeogiCafe.Art.CatSpriteBank.Portrait(id); img.preserveAspect = true; img.raycastTarget = false;
            var rt = (RectTransform)g.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = at; rt.sizeDelta = new Vector2(size, size * 0.84f);
            var nm = UiFactory.Label("N", relRoot, L.Get($"cat.{id}.name"), Mathf.RoundToInt(15 * nameScale), TextAnchor.UpperCenter, UiFactory.Ink);
            var nrt = (RectTransform)nm.transform; nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0.5f);
            nrt.anchoredPosition = at + new Vector2(0, nameAbove ? size * 0.42f + 18f : -size * 0.42f - 12f); nrt.sizeDelta = new Vector2(110, 20);   // 11/04: 그림과 이름 사이를 조금 띄움
            UiFactory.FitOneLine(nm, 110, 11);
        }

        // 둥근 사각형(반폭 hw, 반높이 hh) 둘레 위 t(0~1) 지점. 위 가운데에서 시작해 시계 방향. 모서리는 반지름 r로 깎는다.
        static Vector2 Perimeter(float t, float hw, float hh)
        {
            float r = Mathf.Min(hw, hh) * 0.45f;
            float sw = 2f * (hw - r), sh = 2f * (hh - r), arc = Mathf.PI * 0.5f * r;
            float total = 2f * sw + 2f * sh + 4f * arc;
            float d = Mathf.Repeat(t, 1f) * total;
            // 구간: 위(오른쪽 반) → 우상 모서리 → 오른쪽 → 우하 → 아래 → 좌하 → 왼쪽 → 좌상 → 위(왼쪽 반)
            float[] len = { sw * 0.5f, arc, sh, arc, sw, arc, sh, arc, sw * 0.5f };
            int seg = 0; while (seg < len.Length - 1 && d > len[seg]) { d -= len[seg]; seg++; }
            float u = len[seg] > 0f ? d / len[seg] : 0f;
            float cx = hw - r, cy = hh - r;
            Vector2 Corner(float x, float y, float a0) { float a = a0 - u * Mathf.PI * 0.5f; return new Vector2(x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r); }
            switch (seg)
            {
                case 0: return new Vector2(u * cx, hh);
                case 1: return Corner(cx, cy, Mathf.PI * 0.5f);
                case 2: return new Vector2(hw, cy - u * sh);
                case 3: return Corner(cx, -cy, 0f);
                case 4: return new Vector2(cx - u * sw, -hh);
                case 5: return Corner(-cx, -cy, -Mathf.PI * 0.5f);
                case 6: return new Vector2(-hw, -cy + u * sh);
                case 7: return Corner(-cx, cy, Mathf.PI);
                default: return new Vector2(-cx + u * cx, hh);
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
            UiFactory.SetRect((RectTransform)lab.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(52, 20));   // 'スポット'(4자)도 13px 그대로
            lab.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rec = cs != null ? cs.observation.Get(axis) : null;
            float cx = x + 52;                                       // 아이콘 30px → 다음 칸(96px 간격)과 안 겹침
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
        static void RelLine(RectTransform parent, Vector2 mid, float len, float th, float angle, Color c)
        {
            var line = UiFactory.Panel("L", parent, c);
            line.pivot = new Vector2(0.5f, 0.5f); line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = mid; line.sizeDelta = new Vector2(len, th);
            line.localEulerAngles = new Vector3(0, 0, angle);
            line.GetComponent<Image>().raycastTarget = false;
        }

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
        public void Hide() { if (storyCardGo != null) { Object.Destroy(storyCardGo); storyCardGo = null; } rootGo.SetActive(false); }
        public void DebugTab(bool rel, bool review) { if (!IsOpen) Toggle(); showRel = rel; showReview = review; Render(); }
        public bool IsOpen => rootGo != null && rootGo.activeSelf;
    }
}
