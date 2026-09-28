using System;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Progression
{
    // 사양서 §28 — 고양이 관계(정식). 같은 시간대 근접 체류 → 점수 누적(일1회 상한) → 지인→친구.
    // relationshipHints로 잠재 짝 제한(조합 폭발 방지). 핵심 취향 시스템을 방해하지 않는 보조 축.
    public class RelationshipManager : MonoBehaviour
    {
        public SaveGame save;

        [Header("밸런스(가정값)")]
        public int scorePerMeeting = 1;      // 일 1회 상한
        public int acquaintanceThreshold = 2; // 지인
        public int friendThreshold = 5;       // 친구

        public event Action<string, string, int> OnRelationStageUp; // (a, b, stage)

        // 하루 중 이미 점수를 준 페어(중복 방지). 하루 시작 시 초기화.
        readonly HashSet<string> todayCounted = new();

        public void OnDayStart() => todayCounted.Clear();

        // 두 고양이가 같은 시간대·근접 좌석에 있을 때 호출. relationshipHints에 있는 짝만 유효.
        public void RecordProximity(CatData a, CatData b)
        {
            if (a == null || b == null || a.catId == b.catId) return;
            if (!IsPotentialPair(a, b)) return;

            string key = PairKey(a.catId, b.catId);
            if (todayCounted.Contains(key)) return; // 일 1회 상한
            todayCounted.Add(key);

            var rel = save.GetOrCreateRelation(a.catId, b.catId);
            rel.score += scorePerMeeting;
            PromoteIfNeeded(rel);
        }

        void PromoteIfNeeded(RelationSaveData rel)
        {
            int newStage = EvaluateStage(rel.score, rel.stage, acquaintanceThreshold, friendThreshold);
            if (newStage != rel.stage)
            {
                rel.stage = newStage;
                OnRelationStageUp?.Invoke(rel.catA, rel.catB, newStage);
            }
        }

        // 순수 승급 판정(테스트 용이). 강등 없음.
        public static int EvaluateStage(int score, int currentStage, int acqThreshold, int friendThreshold)
        {
            int s = currentStage;
            if (s < 1 && score >= acqThreshold) s = 1;
            if (s < 2 && score >= friendThreshold) s = 2;
            return s;
        }

        // 잠재 짝 판정 노출(테스트용)
        public static bool IsPotentialPairPublic(CatData a, CatData b) => IsPotentialPair(a, b);

        public bool AreFriends(string a, string b)
        {
            var rel = FindRelation(a, b);
            return rel != null && rel.stage >= 2;
        }

        public int StageOf(string a, string b) => FindRelation(a, b)?.stage ?? 0;

        RelationSaveData FindRelation(string a, string b)
        {
            string key = PairKey(a, b);
            foreach (var r in save.relations)
                if (PairKey(r.catA, r.catB) == key) return r;
            return null;
        }

        static bool IsPotentialPair(CatData a, CatData b)
        {
            // a.hints에 b 있거나 b.hints에 a 있으면 잠재 짝
            if (Contains(a.relationshipHintCatIds, b.catId)) return true;
            if (Contains(b.relationshipHintCatIds, a.catId)) return true;
            return false;
        }

        static bool Contains(string[] arr, string id)
        {
            if (arr == null) return false;
            foreach (var s in arr) if (s == id) return true;
            return false;
        }

        static string PairKey(string a, string b)
            => string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
    }
}
