using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Save;
using YeogiCafe.UI;

namespace YeogiCafe.Tests
{
    // 엔딩 두 단계: 일반(보호소 제외) → 진엔딩(보호소 포함). 순서는 항상 일반 → 진엔딩.
    public class TwoStageEndingTests
    {
        static List<CatData> Cats()
        {
            var ids = new[] { "cat_cheese", "cat_calico", "cat_sphynx", "cat_kitten", "cat_senior" };
            var list = new List<CatData>();
            foreach (var id in ids) { var c = ScriptableObject.CreateInstance<CatData>(); c.catId = id; list.Add(c); }
            return list;
        }

        static void Complete(SaveGame s, string catId)
        {
            var o = s.GetOrCreateCat(catId).observation;
            foreach (PrefAxis a in new[] { PrefAxis.Food, PrefAxis.Seat, PrefAxis.Facility }) o.Get(a).state = ObsState.Recorded;
        }

        [Test]
        public void 보호소를_먼저_끝내도_진엔딩은_일반_엔딩_뒤에만_열린다()
        {
            var cats = Cats(); var s = new SaveGame();
            Complete(s, "cat_sphynx"); Complete(s, "cat_kitten"); Complete(s, "cat_senior");
            Assert.IsFalse(EndingPanel.Ready(s, cats));
            Assert.IsFalse(EndingPanel.TrueReady(s, cats), "보호소만 끝낸 상태로 진엔딩이 먼저 뜨면 안 된다");
        }

        [Test]
        public void 같은_밤에_둘_다_채우면_일반_다음_진엔딩()
        {
            var cats = Cats(); var s = new SaveGame();
            foreach (var c in cats) Complete(s, c.catId);
            Assert.IsTrue(EndingPanel.Ready(s, cats));
            Assert.IsFalse(EndingPanel.TrueReady(s, cats), "일반 엔딩을 보기 전에는 진엔딩 아님");
            s.endingSeen = true;                                   // 일반 엔딩 [계속 영업하기]
            Assert.IsFalse(EndingPanel.Ready(s, cats));
            Assert.IsTrue(EndingPanel.TrueReady(s, cats), "같은 밤 바로 이어서 진엔딩");
            s.trueEndingSeen = true;
            Assert.IsFalse(EndingPanel.TrueReady(s, cats), "진엔딩은 한 번만");
        }

        [Test]
        public void 일반_엔딩은_보호소_없이도_열리고_진엔딩은_보호소를_다_알아야_열린다()
        {
            var cats = Cats(); var s = new SaveGame();
            Complete(s, "cat_cheese"); Complete(s, "cat_calico");
            Assert.IsTrue(EndingPanel.Ready(s, cats));
            Assert.AreEqual((6, 6), EndingPanel.Progress(s, cats));
            s.endingSeen = true;
            Complete(s, "cat_sphynx"); Complete(s, "cat_kitten");
            Assert.IsFalse(EndingPanel.TrueReady(s, cats));
            Assert.AreEqual((12, 15), EndingPanel.TrueProgress(s, cats));
            Complete(s, "cat_senior");
            Assert.IsTrue(EndingPanel.TrueReady(s, cats));
        }
    }
}
