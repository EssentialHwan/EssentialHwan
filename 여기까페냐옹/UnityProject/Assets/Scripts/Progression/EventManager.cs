using System;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Progression
{
    // 사양서 §30 / 문서 01 — 이벤트/에피소드 재생. 트리거 충족 시 큐잉, 1회성(completed 저장).
    public class EventManager : MonoBehaviour
    {
        public List<CatEventData> allEvents = new();   // ContentGenerator 생성분 연결
        public SaveGame save;

        // 뷰가 구독: (catId, 대사키 배열) → 대사 UI 재생
        public event Action<string, string[]> OnPlayEpisode;
        // 보상 지급 요청: (gold, furnitureId)
        public event Action<int, string> OnGrantReward;

        readonly Queue<CatEventData> pending = new();

        // 단골 승급 시 호출 (RegularManager.OnRegularStageUp 구독)
        public void OnRegularStageUp(string catId, int stage)
        {
            foreach (var ev in allEvents)
            {
                if (ev.trigger != EventTrigger.RegularStage) continue;
                if (ev.catId != catId) continue;
                if (stage < ev.triggerValue) continue;
                if (save.completedEventIds.Contains(ev.eventId)) continue;
                pending.Enqueue(ev);
            }
        }

        // 관계 승급 시 호출 (RelationshipManager.OnRelationStageUp 구독). 짝은 순서 무관.
        public void OnRelationStageUp(string a, string b, int stage)
        {
            foreach (var ev in allEvents)
            {
                if (ev.trigger != EventTrigger.RelationStage) continue;
                if (!IsSamePair(ev.catId, ev.catIdB, a, b)) continue;
                if (stage < ev.triggerValue) continue;
                if (save.completedEventIds.Contains(ev.eventId)) continue;
                pending.Enqueue(ev);
            }
        }

        static bool IsSamePair(string a1, string b1, string a2, string b2)
            => (a1 == a2 && b1 == b2) || (a1 == b2 && b1 == a2);

        // 다음 방문/정산 시 큐 소진 (한 번에 하나)
        public bool TryPlayNext()
        {
            if (pending.Count == 0) return false;
            var ev = pending.Dequeue();
            if (save.completedEventIds.Contains(ev.eventId)) return TryPlayNext();

            OnPlayEpisode?.Invoke(ev.catId, ev.scriptLineKeys);
            if (ev.rewardGold > 0 || !string.IsNullOrEmpty(ev.rewardFurnitureId))
                OnGrantReward?.Invoke(ev.rewardGold, ev.rewardFurnitureId);

            save.completedEventIds.Add(ev.eventId);   // 1회성
            return true;
        }

        public int PendingCount => pending.Count;
    }
}
