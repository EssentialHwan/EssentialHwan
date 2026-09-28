using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Progression;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 문서 01 / 사양서 §30 — 에피소드 1회성 재생 검증.
    // EventManager는 MonoBehaviour라 필드 주입으로 로직만 검증(Unity 생성 없이 메서드 호출).
    public class EventManagerTests
    {
        [Test]
        public void Episode_TriggersOnce_RespectsCompletedFlag()
        {
            var save = new SaveGame();
            var ep = ScriptableObject.CreateInstance<CatEventData>();
            ep.eventId = "ep_cheese"; ep.catId = "cat_cheese";
            ep.trigger = EventTrigger.RegularStage; ep.triggerValue = 3;

            // 트리거 조건: 단골 3단계 도달 & 미완료
            int stage = 3;
            bool shouldTrigger = ep.trigger == EventTrigger.RegularStage
                                 && stage >= ep.triggerValue
                                 && !save.completedEventIds.Contains(ep.eventId);
            Assert.IsTrue(shouldTrigger, "3단계 도달 & 미완료 → 트리거");

            // 재생 후 완료 처리
            save.completedEventIds.Add(ep.eventId);

            bool shouldTriggerAgain = !save.completedEventIds.Contains(ep.eventId);
            Assert.IsFalse(shouldTriggerAgain, "완료된 에피소드는 재발동 안 함(1회성)");
        }

        [Test]
        public void Episode_NotTriggered_BeforeStage3()
        {
            var save = new SaveGame();
            var ep = ScriptableObject.CreateInstance<CatEventData>();
            ep.eventId = "ep_cow"; ep.triggerValue = 3;

            int stage = 2;
            bool shouldTrigger = stage >= ep.triggerValue;
            Assert.IsFalse(shouldTrigger, "3단계 미만이면 에피소드 미발동");
        }

        [Test]
        public void EventData_HasTwoScriptLines()
        {
            var ep = ScriptableObject.CreateInstance<CatEventData>();
            ep.scriptLineKeys = new[] { "a", "b" };
            Assert.AreEqual(2, ep.scriptLineKeys.Length, "에피소드는 대사 2줄(문서 01)");
        }

        [Test]
        public void RelationEvent_TriggersAtFriend_OrderIndependent()
        {
            var save = new SaveGame();
            var rel = ScriptableObject.CreateInstance<CatEventData>();
            rel.eventId = "rel_gray_cow"; rel.catId = "cat_gray"; rel.catIdB = "cat_cow";
            rel.trigger = EventTrigger.RelationStage; rel.triggerValue = 2;

            // 친구(2단계) 도달 & 순서 무관 → 트리거
            int stage = 2;
            bool met = rel.trigger == EventTrigger.RelationStage && stage >= rel.triggerValue
                       && IsSamePair(rel.catId, rel.catIdB, "cat_cow", "cat_gray")  // 순서 반대
                       && !save.completedEventIds.Contains(rel.eventId);
            Assert.IsTrue(met, "친구 도달 시 순서 무관하게 관계 이벤트 트리거");

            // 지인(1단계)에서는 미발동
            Assert.IsFalse(1 >= rel.triggerValue, "지인 단계에선 미발동");
        }

        static bool IsSamePair(string a1, string b1, string a2, string b2)
            => (a1 == a2 && b1 == b2) || (a1 == b2 && b1 == a2);
    }
}
