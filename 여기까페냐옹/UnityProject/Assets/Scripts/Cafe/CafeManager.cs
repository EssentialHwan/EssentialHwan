using System;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Economy;
using YeogiCafe.Save;

namespace YeogiCafe.Cafe
{
    // 사양서 §14/§31 — 카페 성장. [S2] Gold + 발견/단골 복합조건 + OR 대체 경로(P3).
    // 데이터 주도: CafeLevelConfig SO 우선, 없으면 인라인 리스트(하위호환).
    [Serializable]
    public class CafeLevelReq
    {
        public int level;
        public int goldCost;
        public int minDiscoveredCats;
        public int minRecordedAxesTotal;
        public int minRegulars;
        public int orAltFurnitureCount;
    }

    // 성장 판정 입력(테스트 용이하게 분리)
    public struct GrowthState
    {
        public int gold, discoveredCats, recordedAxesTotal, regulars, furnitureCount;
    }

    public class CafeManager : MonoBehaviour
    {
        public EconomyManager economy;
        public SaveGame save;

        public CafeLevelConfig config;                 // SO 우선
        public List<CafeLevelReq> levels = new();      // 폴백

        public event Action<int> OnCafeUpgraded;

        public bool CanUpgrade(out string blockedHint)
        {
            blockedHint = null;
            var next = NextReq();
            if (next == null) { blockedHint = "최대 레벨"; return false; }

            var state = new GrowthState
            {
                gold = economy.Gold,
                discoveredCats = save.cats.FindAll(c => c.visitCount > 0).Count,
                recordedAxesTotal = TotalRecordedAxes(),
                regulars = save.cats.FindAll(c => c.regularStage >= 2).Count,
                furnitureCount = save.placedFurniture.Count
            };
            return Evaluate(state, next, out blockedHint);
        }

        // 순수 판정 로직 (EditMode 테스트 대상). P3 OR 경로 포함.
        public static bool Evaluate(GrowthState s, CafeLevelReq next, out string blockedHint)
        {
            blockedHint = null;
            if (s.gold < next.goldCost) { blockedHint = $"골드 부족 ({s.gold}/{next.goldCost})"; return false; }

            bool observationPath = s.discoveredCats >= next.minDiscoveredCats
                                   && s.recordedAxesTotal >= next.minRecordedAxesTotal
                                   && s.regulars >= next.minRegulars;
            bool altPath = next.orAltFurnitureCount > 0 && s.furnitureCount >= next.orAltFurnitureCount;

            if (!observationPath && !altPath)
            {
                blockedHint = "취향을 더 발견하거나(관찰) 새로운 가구를 설치해보세요";
                return false;
            }
            return true;
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade(out _)) return false;
            var next = NextReq();
            if (!economy.TrySpend(next.goldCost)) return false;
            save.cafeLevel = next.level;
            OnCafeUpgraded?.Invoke(save.cafeLevel);
            return true;
        }

        int TotalRecordedAxes()
        {
            int t = 0; foreach (var c in save.cats) t += c.observation.RecordedAxisCount(); return t;
        }

        CafeLevelReq NextReq()
        {
            // SO 우선
            if (config != null)
            {
                var r = config.GetReqForNext(save.cafeLevel);
                if (r != null) return new CafeLevelReq {
                    level = r.level, goldCost = r.goldCost,
                    minDiscoveredCats = r.minDiscoveredCats,
                    minRecordedAxesTotal = r.minRecordedAxesTotal,
                    minRegulars = r.minRegulars,
                    orAltFurnitureCount = r.orAltFurnitureCount
                };
            }
            foreach (var l in levels) if (l.level == save.cafeLevel + 1) return l;
            return null;
        }
    }
}
