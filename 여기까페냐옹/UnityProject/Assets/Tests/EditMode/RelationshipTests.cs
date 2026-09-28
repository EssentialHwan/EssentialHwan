using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Progression;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 사양서 §28 — 관계 승급·잠재짝·순서무관 키.
    public class RelationshipTests
    {
        [Test]
        public void EvaluateStage_Promotes_NoDemotion()
        {
            Assert.AreEqual(0, RelationshipManager.EvaluateStage(1, 0, 2, 5));
            Assert.AreEqual(1, RelationshipManager.EvaluateStage(2, 0, 2, 5), "지인");
            Assert.AreEqual(2, RelationshipManager.EvaluateStage(5, 1, 2, 5), "친구");
            // 점수 하락해도 강등 없음(현재 단계 유지)
            Assert.AreEqual(2, RelationshipManager.EvaluateStage(0, 2, 2, 5), "강등 없음");
        }

        [Test]
        public void PotentialPair_OnlyWhenHinted()
        {
            var gray = Cat("cat_gray", "cat_cow");
            var cow = Cat("cat_cow", "cat_gray");
            var cheese = Cat("cat_cheese"); // 힌트 없음

            Assert.IsTrue(RelationshipManager.IsPotentialPairPublic(gray, cow), "쌍둥이 취향 짝");
            Assert.IsFalse(RelationshipManager.IsPotentialPairPublic(gray, cheese), "힌트 없으면 짝 아님");
        }

        [Test]
        public void Relation_OrderIndependentKey()
        {
            var save = new SaveGame();
            var r1 = save.GetOrCreateRelation("cat_cow", "cat_gray");
            var r2 = save.GetOrCreateRelation("cat_gray", "cat_cow"); // 순서 바꿔도 같은 레코드
            Assert.AreSame(r1, r2, "순서 무관 동일 관계");
            Assert.AreEqual(1, save.relations.Count);
        }

        static CatData Cat(string id, params string[] hints)
        {
            var c = ScriptableObject.CreateInstance<CatData>();
            c.catId = id;
            c.relationshipHintCatIds = hints;
            return c;
        }
    }
}
