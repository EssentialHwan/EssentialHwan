using System;
using System.Collections.Generic;
using System.Linq;
using YeogiCafe.Data;

namespace YeogiCafe.Save
{
    // 사양서 §1.3 — 세이브 DTO. Truth(CatData 취향)는 어떤 DTO에도 존재하지 않는다(QA T13).

    [Serializable]
    public class SaveGame
    {
        public int version = 1;
        public int gold;
        public int currentDay = 1;
        public int cafeLevel = 1;
        public List<PlacedFurnitureDTO> placedFurniture = new();
        public List<string> unlockedMenuIds = new();
        public List<CatSaveData> cats = new();
        public List<string> completedEventIds = new();
        public List<RelationSaveData> relations = new();   // 정식: 고양이 관계

        public RelationSaveData GetOrCreateRelation(string a, string b)
        {
            // 순서 무관 키(정렬)
            string x = string.CompareOrdinal(a, b) <= 0 ? a : b;
            string y = string.CompareOrdinal(a, b) <= 0 ? b : a;
            var r = relations.FirstOrDefault(t => t.catA == x && t.catB == y);
            if (r == null) { r = new RelationSaveData { catA = x, catB = y }; relations.Add(r); }
            return r;
        }

        public CatSaveData GetOrCreateCat(string catId)
        {
            var c = cats.FirstOrDefault(x => x.catId == catId);
            if (c == null) { c = new CatSaveData { catId = catId }; cats.Add(c); }
            return c;
        }
    }

    [Serializable]
    public class PlacedFurnitureDTO
    {
        public string furnitureId;
        public int gridX, gridY;
        public int rotation;
    }

    [Serializable]
    public class CatSaveData
    {
        public string catId;
        public int visitCount;
        public int lastVisitDay = -1;
        public List<float> satisfactionHistory = new();  // 단일 소스 (P5)
        public int regularStage = 1;
        public bool personality2Revealed;                 // 부록 J-C
        // [프리플라이트] 재방문 판정용 "지난 방문 실제 결과"(Recorded 상태가 아니라 실제 이용 여부)
        public bool lastServedFavorite;
        public bool lastUsedPreferredSeat;
        public bool lastUsedPreferredFacility;
        public ObservationRecord observation = new();     // 필수 저장, Truth 미포함

        // ── 만족도 파생값 (저장 안 함) ──
        public float LastSatisfaction => satisfactionHistory.Count > 0 ? satisfactionHistory[^1] : 0f;
        public float AvgSatisfaction  => satisfactionHistory.Count > 0 ? satisfactionHistory.Average() : 0f;

        public void PushSatisfaction(float value, int maxLen = 5)
        {
            satisfactionHistory.Add(value);
            while (satisfactionHistory.Count > maxLen) satisfactionHistory.RemoveAt(0);
        }
    }

    [Serializable]
    public class RelationSaveData
    {
        public string catA, catB;
        public int score;        // 누적 관계 점수
        public int stage;        // 0=모름, 1=지인, 2=친구
    }

    [Serializable]
    public class ObservationRecord   // 3축 (P2)
    {
        public AxisRecord food = new() { axis = PrefAxis.Food };
        public AxisRecord seat = new() { axis = PrefAxis.Seat };
        public AxisRecord facility = new() { axis = PrefAxis.Facility };

        public AxisRecord Get(PrefAxis axis) => axis switch
        {
            PrefAxis.Food => food,
            PrefAxis.Seat => seat,
            _ => facility
        };

        public int RecordedAxisCount()
        {
            int n = 0;
            if (food.state == ObsState.Recorded) n++;
            if (seat.state == ObsState.Recorded) n++;
            if (facility.state == ObsState.Recorded) n++;
            return n;
        }
    }

    [Serializable]
    public class AxisRecord
    {
        public PrefAxis axis;
        public ObsState state = ObsState.Unknown;   // ? / … / ★ / ✓
        public List<ClueTally> clueWeights = new();
        public string recordedValue;                // Recorded 시 확정된 태그/메뉴 id

        public ClueTally GetOrCreate(string target)
        {
            var t = clueWeights.FirstOrDefault(x => x.target == target);
            if (t == null) { t = new ClueTally { target = target }; clueWeights.Add(t); }
            return t;
        }
        public ClueTally Top()    => clueWeights.OrderByDescending(t => t.total).FirstOrDefault();
        public ClueTally Second() => clueWeights.OrderByDescending(t => t.total).Skip(1).FirstOrDefault();
    }

    [Serializable]
    public class ClueTally
    {
        public string target;        // SeatTag/FacilityTag 이름 또는 menuId
        public float total;          // 승격 판정용 누적 (fallback cap 반영)
        public float fallbackSum;    // P1: fallback 기여 누계 (cap 체크용)
    }
}
