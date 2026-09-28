using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Cafe;
using YeogiCafe.Data;

namespace YeogiCafe.Core
{
    // 카페 런타임 컨텍스트 — 좌석/시설/메뉴/입구를 한 곳에서 제공.
    // CatBrain이 씬 구조를 직접 뒤지지 않도록 하는 배선 허브.
    public class CafeContext : MonoBehaviour
    {
        public Transform entrance;

        readonly List<SeatBehaviour> seats = new();
        readonly List<FacilityBehaviour> facilities = new();
        readonly List<MenuData> availableMenus = new();

        // ── 등록 (FurnitureManager가 배치/제거 시 호출) ──
        public void RegisterSeat(SeatBehaviour s)     { if (!seats.Contains(s)) seats.Add(s); }
        public void UnregisterSeat(SeatBehaviour s)   { seats.Remove(s); }
        public void RegisterFacility(FacilityBehaviour f)   { if (!facilities.Contains(f)) facilities.Add(f); }
        public void UnregisterFacility(FacilityBehaviour f) { facilities.Remove(f); }
        public void SetMenu(IEnumerable<MenuData> menus) { availableMenus.Clear(); availableMenus.AddRange(menus); }
        public void AddMenu(MenuData m) { if (!availableMenus.Contains(m)) availableMenus.Add(m); }

        // ── 조회 (CatBrain 주입용) ──
        public IReadOnlyList<ISeat> GetSeats() => seats.ConvertAll(s => (ISeat)s);
        public IReadOnlyList<IFacility> GetFacilities() => facilities.ConvertAll(f => (IFacility)f);
        public IReadOnlyList<MenuData> GetAvailableMenus() => availableMenus;
        public Vector3 Entrance => entrance != null ? entrance.position : Vector3.zero;

        public int SeatCount => seats.Count;
        public int FreeSeatCount { get { int n = 0; foreach (var s in seats) if (s.IsFree) n++; return n; } }

        // 관계 조회용(정식). null이면 친구 0(S1 동작).
        public YeogiCafe.Progression.RelationshipManager relationships;
        // 착석 고양이 → catId 조회용(SeatBehaviour에 점유자 저장). 간이 매핑.
        readonly Dictionary<ISeat, string> seatOccupantCatId = new();

        public void SetSeatOccupant(ISeat seat, string catId)
        {
            if (catId == null) seatOccupantCatId.Remove(seat);
            else seatOccupantCatId[seat] = catId;
        }

        // 근접 친구 수: 관계 매니저가 있으면 인접 착석 고양이 중 친구를 카운트.
        public int NearbyFriends(ISeat seat)
        {
            if (relationships == null) return 0;
            // 이 좌석에 앉으려는 고양이의 친구가 다른 좌석에 있는지는 호출 시점에
            // 알 수 없으므로, 여기서는 "착석 중인 고양이들 간 친구 쌍" 근사치를 제공.
            // 실사용: CatBrain이 자신 catId로 CountFriendsSeated(myId) 호출 권장.
            return 0;
        }

        public int CountFriendsSeated(string myCatId)
        {
            if (relationships == null || string.IsNullOrEmpty(myCatId)) return 0;
            int n = 0;
            foreach (var kv in seatOccupantCatId)
                if (kv.Value != myCatId && relationships.AreFriends(myCatId, kv.Value)) n++;
            return n;
        }

        public int NearbyStrangers(ISeat seat)
        {
            int n = 0;
            foreach (var s in seats) if (!s.IsFree && s != (object)seat) n++;
            return n;
        }
    }
}
