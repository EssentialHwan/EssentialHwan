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

        public CatalogPanel(GameUiController owner, Transform parent)
        {
            this.owner = owner;
            var scrim = UiFactory.Panel("CatalogScrim", parent, UiFactory.Scrim);
            rootGo = scrim.gameObject;
            UiFactory.Anchor(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = UiFactory.Panel("CatalogPanel", scrim.transform, UiFactory.PanelBg);
            UiFactory.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                              Vector2.zero, new Vector2(720, 560));

            UiFactory.Label("Title", panel.transform, "📖 " + L.Get("ui.catalog.title"), 30,
                            TextAnchor.UpperCenter, UiFactory.Ink);

            header = UiFactory.Label("Header", panel.transform, "", 22, TextAnchor.UpperCenter, UiFactory.WoodDark);
            UiFactory.SetRect((RectTransform)header.transform, new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(-40, 40));

            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panel.transform, false);
            listContent = (RectTransform)listGo.transform;
            UiFactory.SetRect(listContent, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                              new Vector2(0, -20), new Vector2(-40, -130));
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6; vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            var closeBtn = UiFactory.Button("Close", panel.transform, "✕", UiFactory.Pink, UiFactory.Cream, out _);
            UiFactory.SetRect((RectTransform)closeBtn.transform, new Vector2(1, 1), new Vector2(1, 1),
                              new Vector2(1, 1), new Vector2(-10, -10), new Vector2(48, 48));
            closeBtn.onClick.AddListener(Hide);

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
                ((RectTransform)row.transform).sizeDelta = new Vector2(0, 52);

                string catName = visited ? L.Get($"cat.{data.catId}.name") : "???";
                string axes = cs == null ? "?  ?  ?" :
                    $"{L.Get("ui.axis.food")}{ObservationCardBuilder.StateIcon(cs.observation.food.state)}  " +
                    $"{L.Get("ui.axis.seat")}{ObservationCardBuilder.StateIcon(cs.observation.seat.state)}  " +
                    $"{L.Get("ui.axis.facility")}{ObservationCardBuilder.StateIcon(cs.observation.facility.state)}";

                var lbl = UiFactory.Label("Row", row.transform, $"{catName}      {axes}", 20,
                                          TextAnchor.MiddleLeft, UiFactory.Ink);
                UiFactory.SetRect((RectTransform)lbl.transform, Vector2.zero, Vector2.one,
                                  new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(-20, 0));
            }

            header.text = L.Get("ui.catalog.discovered", ("a", discovered.ToString()), ("b", total.ToString()));
        }

        public void Toggle()
        {
            bool next = !rootGo.activeSelf;
            rootGo.SetActive(next);
            if (next) Render();
        }
        public void Hide() => rootGo.SetActive(false);
    }
}
