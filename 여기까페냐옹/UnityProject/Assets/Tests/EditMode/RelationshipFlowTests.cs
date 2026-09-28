using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Progression;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 관계 전체 흐름(순수 로직 조립): 여러 날 근접 누적 → 친구 → 관계 이벤트 트리거.
    // MonoBehaviour 없이 RelationSaveData + EvaluateStage + 이벤트 조건으로 재현.
    public class RelationshipFlowTests
    {
        [Test]
        public void CoVisits_LeadTo_Friendship_And_RelationEvent()
        {
            var save = new SaveGame();
            int acq = 2, friend = 5;

            // 5일간 매일 근접(일1회 상한) → 점수 5 → 친구
            var rel = save.GetOrCreateRelation("cat_gray", "cat_cow");
            for (int day = 1; day <= 5; day++)
            {
                rel.score += 1; // 일 1회
                rel.stage = RelationshipManager.EvaluateStage(rel.score, rel.stage, acq, friend);
            }

            Assert.AreEqual(2, rel.stage, "5일 근접 → 친구(2단계)");

            // 관계 이벤트 트리거 조건 확인 (친구 도달 & 미완료)
            var relEvent = ScriptableObject.CreateInstance<CatEventData>();
            relEvent.eventId = "rel_gray_cow"; relEvent.catId = "cat_gray"; relEvent.catIdB = "cat_cow";
            relEvent.trigger = EventTrigger.RelationStage; relEvent.triggerValue = 2;

            bool triggers = relEvent.trigger == EventTrigger.RelationStage
                            && rel.stage >= relEvent.triggerValue
                            && !save.completedEventIds.Contains(relEvent.eventId);
            Assert.IsTrue(triggers, "친구 도달 시 관계 이벤트 발동");

            // 중간 단계 검증: 2일차엔 지인(1단계)
            var rel2 = save.GetOrCreateRelation("cat_calico", "cat_tuxedo");
            rel2.score = 2; rel2.stage = RelationshipManager.EvaluateStage(2, 0, acq, friend);
            Assert.AreEqual(1, rel2.stage, "2점 → 지인");
        }

        [Test]
        public void NonHintedPair_NeverAccumulates()
        {
            // 잠재 짝이 아니면 관계가 생기지 않음(IsPotentialPair가 걸러냄) — 데이터 레벨 확인
            var cheese = ScriptableObject.CreateInstance<CatData>();
            cheese.catId = "cat_cheese"; cheese.relationshipHintCatIds = new string[0];
            var cow = ScriptableObject.CreateInstance<CatData>();
            cow.catId = "cat_cow"; cow.relationshipHintCatIds = new[] { "cat_gray" };

            Assert.IsFalse(RelationshipManager.IsPotentialPairPublic(cheese, cow),
                "힌트 없는 짝은 관계 누적 대상이 아님(조합 폭발 방지)");
        }
    }
}
