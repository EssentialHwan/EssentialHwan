using System;
using UnityEngine;

namespace YeogiCafe.Data
{
    // 사양서 §14/§31 — 카페 성장 레벨 조건(데이터 주도). 밸런스 문서 06 반영.
    // 코드 수정 없이 이 SO만 편집해 성장 곡선 조정. P3 OR 경로 포함.
    [CreateAssetMenu(menuName = "YeogiCafe/CafeLevelConfig")]
    public class CafeLevelConfig : ScriptableObject
    {
        public LevelReq[] levels;

        [Serializable]
        public class LevelReq
        {
            public int level;                  // 목표 레벨(2,3...)
            public int goldCost;
            [Header("관찰 성과 경로 (AND)")]
            public int minDiscoveredCats;
            public int minRecordedAxesTotal;
            public int minRegulars;
            [Header("OR 대체 경로 (P3 데드락 방지)")]
            public int orAltFurnitureCount;    // 0이면 대체경로 없음
        }

        public LevelReq GetReqForNext(int currentLevel)
        {
            if (levels == null) return null;
            foreach (var l in levels) if (l.level == currentLevel + 1) return l;
            return null;
        }
    }
}
