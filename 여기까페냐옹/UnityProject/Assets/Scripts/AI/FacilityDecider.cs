using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.AI
{
    // 사양서 §2.5 — 시설 선택. 선호 시설이 카페에 없으면 관련 행동/단서 자체가 없음(관찰 채널 규칙).
    public interface IFacility
    {
        string FacilityId { get; }
        IReadOnlyList<FacilityTag> Tags { get; }
        bool IsFree { get; }
    }

    public struct FacilityDecision
    {
        public IFacility facility;   // null이면 Idle
        public bool isIdle;
    }

    public static class FacilityDecider
    {
        // Eat 이후 매 결정 tick. 시설 이용 vs Idle 중 최고 점수.
        public static FacilityDecision Tick(
            CatData cat, IReadOnlyList<IFacility> facilities,
            System.Func<IFacility, float> recentUsePenalty, BalanceConfig cfg)
        {
            float idleScore = 15f + Random.Range(-10f, 10f); // Idle 기본
            IFacility bestFac = null; float bestScore = idleScore; bool idle = true;

            if (facilities != null)
            {
                foreach (var f in facilities)
                {
                    if (!f.IsFree) continue;
                    float chance = UseChance(cat, f, recentUsePenalty(f));
                    float score = chance * 100f + Random.Range(-10f, 10f);
                    if (score > bestScore) { bestScore = score; bestFac = f; idle = false; }
                }
            }
            return new FacilityDecision { facility = bestFac, isIdle = idle };
        }

        public static float UseChance(CatData cat, IFacility f, float recentPenalty)
        {
            float p = 0.15f; // base
            if (SharesTag(f.Tags, cat.facilityPreference)) p += 0.5f;      // 선호 시설
            if (cat.HasPersonality(PersonalityTag.Curious)) p += 0.3f;
            if (cat.HasPersonality(PersonalityTag.Playful) && Contains(f.Tags, FacilityTag.Play)) p += 0.3f;
            p -= recentPenalty;
            return Mathf.Clamp01(p);
        }

        static bool Contains(IReadOnlyList<FacilityTag> tags, FacilityTag t)
        {
            for (int i = 0; i < tags.Count; i++) if (tags[i] == t) return true;
            return false;
        }
        static bool SharesTag(IReadOnlyList<FacilityTag> tags, FacilityTag pref) => Contains(tags, pref);
    }
}
