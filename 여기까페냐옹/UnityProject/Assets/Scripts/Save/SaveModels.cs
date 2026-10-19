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
        public bool initialized;                 // 새 게임 시작 처리 여부(골드 초기값 1회 지급)
        public int gold;
        public int currentDay = 1;
        public int cafeLevel = 1;
        public List<PlacedFurnitureDTO> placedFurniture = new();
        public List<string> unlockedMenuIds = new();
        public List<CatSaveData> cats = new();
        public List<string> completedEventIds = new();
        public List<RelationSaveData> relations = new();   // 정식: 고양이 관계
        public int pawStamps;
        public List<PlacedFurnitureDTO> starterSeatPos = new();
        public List<GiftDTO> pendingGifts = new();         // 다음 날 아침 문 앞에 놓일 선물
        public List<string> giftsCollected = new();        // 주운 선물 종류(수집)
        public List<QuestDTO> quests = new();              // 오늘의 의뢰(게시판)
        public int questDay = -1;                          // 의뢰를 뽑은 날
        public int weather, weatherDay = -1;               // 오늘의 날씨(WeatherKind)
        // ── 요리·평판 ──
        public List<string> ownedRecipes = new();          // 산 레시피(기본 3종 제외)
        public List<MenuStatDTO> menuStats = new();        // 메뉴별 판매 수·업그레이드 단계
        public int nyanStars;                              // 냥슐랭 스타 0~5
        public int inspectorLastDay = -1;                  // 심사원이 마지막으로 온 날
        public int inspectorPlanDay = -1;                  // 심사원이 오기로 정해진 날
        public InspectorReportDTO lastReport = new();      // 가장 최근 심사 결과(정산에서 보여줌)
        public List<ReviewDTO> reviews = new();            // 고양이 손님 한 줄 리뷰(최근 40개)
        public int mouseStateDay = -1, mouseState;         // 오늘 쥐 소동: 0 없음 · 1 잡음 · 2 놓침

        public List<FurnStatDTO> furnStats = new();        // 가구 종류별 별(강화)·사용 수

        public FurnStatDTO GetFurnStat(string furnitureId)
        {
            var f = furnStats.FirstOrDefault(x => x.furnitureId == furnitureId);
            if (f == null) { f = new FurnStatDTO { furnitureId = furnitureId }; furnStats.Add(f); }
            return f;
        }

        public MenuStatDTO GetMenuStat(string menuId)
        {
            var m = menuStats.FirstOrDefault(x => x.menuId == menuId);
            if (m == null) { m = new MenuStatDTO { menuId = menuId }; menuStats.Add(m); }
            return m;
        }

        public List<StoryDTO> storyQueue = new();          // 밤 일지에서 보여줄 이야기(단골·졸업)   // 시작 좌석을 옮긴 위치(furnitureId="starter#i")                              // 하루를 마칠 때 도감에 찍은 손도장 수

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
        public int gridX, gridY;          // 플레이어가 옮긴 위치(1/100 유닛). hasPos일 때만 사용
        public int rotation;
        public bool hasPos;
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
        // 쓰다듬기 반응을 '본' 횟수(TouchResult 순서). 기질(Truth)이 아니라 관찰 결과만 저장.
        public int[] touchSeen = new int[5];
        public bool bossSeen;
        public bool graduated;
        // 마감 놀아주기에서 알아낸 것(관찰 결과만): 좋아한/싫어한 부위 비트(PetZone), 쭙쭙이 버릇을 봤는지
        public int likeZones, hateZones;
        public bool suckleSeen;
        public int closingPlays;                            // 3축을 다 알아내고 단골 3단계 → 졸업(가끔 놀러 옴)                             // 대장냥이가 자리 뺏는 걸 본 적 있음(도감 표시)

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
    public class StoryDTO
    {
        public string catId;
        public int kind;           // 2=단골 2단계, 3=단골 3단계, 9=졸업
    }

    [Serializable]
    public class QuestDTO
    {
        public string id;          // 템플릿 id
        public string eventId;     // 세는 이벤트(GameEvents)
        public int goal, progress, reward;
        public bool done;
    }

    [Serializable]
    public class MenuStatDTO
    {
        public string menuId;
        public int sold;           // 지금까지 판 수
        public int level;          // 업그레이드 단계 0~3
    }

    [Serializable]
    public class FurnStatDTO
    {
        public string furnitureId;
        public int uses;           // 고양이가 앉거나 이용한 횟수
        public int stars = 1;      // 1~3
    }

    [Serializable]
    public class ReviewDTO
    {
        public int day;
        public string catId;
        public int paws;           // 1~5
        public string text;        // 작성 시점 언어로 완성된 문장
    }

    [Serializable]
    public class InspectorReportDTO
    {
        public int day = -1;
        public int dish, speed, furniture, clean, total, starsBefore, starsAfter;
        public bool seen;          // 정산에서 결과 카드를 봤나
    }

    [Serializable]
    public class GiftDTO
    {
        public string catId, giftId;
        public int day;            // 받은 날(이 날 다음부터 문 앞에 나타남)
    }

    [Serializable]
    public class RelationSaveData
    {
        public string catA, catB;
        public int score;        // 누적 관계 점수
        public int stage;        // 0=모름, 1=지인, 2=친구
        public int bond;         // 상호작용으로 쌓는 친밀도(부비·핥기·놀이 +1, 싸움·하악 -1) — 관계도 표시용
        public int fights;       // 싸운 횟수
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
        public string recordedValue;                // Recorded 시 확정된 태그/메뉴 id (추리 기록이면 플레이어가 고른 메뉴/가구 id)
        public List<string> wrongGuesses = new();   // 틀린 추리(다시 고를 때 × 표시)
        public int lastGuessDay = -1;               // 하루 한 번만 추리 가능(찍기 방지)
        public string pendingGuess;                 // 묶음 채점 전 플레이어가 골라 둔 답

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
