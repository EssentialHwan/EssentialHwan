using System;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Core;
using YeogiCafe.Data;
using YeogiCafe.Economy;
using YeogiCafe.Save;

namespace YeogiCafe.Cafe
{
    // 사양서 §6/§15 — 가구 구매·배치. S1 단순화: 그리드 대신 지정 슬롯(Transform).
    public class FurnitureManager : MonoBehaviour
    {
        public CafeContext cafe;
        public EconomyManager economy;
        public SaveGame save;                 // GameFlowController가 주입

        public enum SlotKind { Floor, Wall }

        // 배치 슬롯. 벽걸이 가구는 Wall, 나머지는 Floor. nearWindow 슬롯은 창가 좌석이 우선 차지한다.
        [Serializable] public class SlotEntry
        {
            public Transform slot; public bool occupied;
            public SlotKind kind = SlotKind.Floor;
            public bool nearWindow;
            public int minCafeLevel;          // 카페 성장(같은 방 확장) 레벨에 도달해야 열리는 자리
        }

        public enum FailReason { None, NoSpace, NoGold }
        public FailReason LastFail { get; private set; }

        int CafeLevel => save != null ? save.cafeLevel : int.MaxValue;
        public List<SlotEntry> slots = new();

        [Header("프리팹(없으면 빈 GO)")]
        public GameObject seatPrefab, facilityPrefab;

        public event Action<FurnitureData> OnFurniturePlaced;
        public event Action<AtmosphereSnapshot> OnAtmosphereRecalculated;

        readonly List<FurnitureData> placedData = new();
        public AtmosphereSnapshot CurrentAtmosphere { get; private set; }

        // 배치된 가구 기록(판매용)
        class PlacedItem { public FurnitureData data; public PlacedFurnitureDTO dto; public SlotEntry slot; public GameObject go; }
        readonly List<PlacedItem> items = new();

        // ── 판매: 산 값의 50%(10G 단위) 환급. 손님이 앉아 있거나 쓰는 중이면 못 판다. 시작 좌석은 판매 불가 ──
        public const float SellRate = 0.5f;
        public static int SellPrice(FurnitureData d) => d == null ? 0 : Mathf.Max(10, Mathf.RoundToInt(d.price * SellRate / 10f) * 10);
        public enum SellFail { None, NotSellable, InUse, LastStarter }
        public FurnitureData DataOf(GameObject go) { var it = items.Find(i => i.go == go); return it != null ? it.data : null; }

        // 기본 좌석: 0G에 팔 수 있지만 최소 1개는 남겨야 한다(3개 중 2개까지)
        public bool IsStarter(GameObject go) => starters.Exists(s => s.go == go);
        public bool CanSellStarter => starters.Count > 1;
        public FurnitureData StarterData(GameObject go) { var sb = go != null ? go.GetComponent<SeatBehaviour>() : null; return sb != null ? sb.source : null; }
        void RemoveStarter(int index)
        {
            int k = starters.FindIndex(s => s.index == index);
            if (k < 0) return;
            var go = starters[k].go;
            starters.RemoveAt(k);
            if (go == null) return;
            var sb = go.GetComponent<SeatBehaviour>();
            if (sb != null) cafe.UnregisterSeat(sb);
            // 판 기본 좌석 자리는 바닥 가구 1칸으로 돌려준다(어떤 바닥 가구든 놓을 수 있게)
            var slotGo = new GameObject("Slot_starter#" + index);
            slotGo.transform.position = go.transform.position;
            slots.Add(new SlotEntry { slot = slotGo.transform, kind = SlotKind.Floor, nearWindow = false, minCafeLevel = 1 });
            Destroy(go);
        }

        public bool Sell(GameObject go, out int gold, out SellFail why)
        {
            gold = 0; why = SellFail.None;
            var it = items.Find(i => i.go == go);
            if (it == null && IsStarter(go))
            {
                if (!CanSellStarter) { why = SellFail.LastStarter; return false; }
                var ssb = go.GetComponent<SeatBehaviour>();
                if (ssb != null && !ssb.IsFree) { why = SellFail.InUse; return false; }
                int idx = starters.Find(s => s.go == go).index;
                if (save != null && !save.soldStarters.Contains(idx)) save.soldStarters.Add(idx);
                RemoveStarter(idx);
                return true;                                  // 0G
            }
            if (it == null) { why = SellFail.NotSellable; return false; }
            var sb = go.GetComponent<SeatBehaviour>();
            var fb = go.GetComponent<FacilityBehaviour>();
            if ((sb != null && !sb.IsFree) || (fb != null && !fb.IsFree)) { why = SellFail.InUse; return false; }
            if (sb != null) cafe.UnregisterSeat(sb);
            if (fb != null) cafe.UnregisterFacility(fb);
            if (save != null && it.dto != null) save.placedFurniture.Remove(it.dto);
            if (it.slot != null) it.slot.occupied = false;
            placedData.Remove(it.data);
            items.Remove(it);
            gold = SellPrice(it.data);
            economy.Refund(gold);
            Destroy(go);
            RecalcAtmosphere();
            return true;
        }

        // 구매+배치 (관찰 채널 개방)
        public bool BuyAndPlace(FurnitureData data)
        {
            LastFail = FailReason.None;
            var slot = FindSlotFor(data);
            if (slot == null) { LastFail = FailReason.NoSpace; return false; }               // 공간 부족
            if (!economy.TrySpend(data.price)) { LastFail = FailReason.NoGold; return false; } // 잔액 부족

            PlacedFurnitureDTO dto = null;
            if (save != null) { dto = new PlacedFurnitureDTO { furnitureId = data.furnitureId }; save.placedFurniture.Add(dto); }
            items.Add(new PlacedItem { data = data, dto = dto, slot = slot, go = Place(data, slot, dto) });
            placedData.Add(data);
            OnFurniturePlaced?.Invoke(data);
            RecalcAtmosphere();
            return true;
        }

        // 이어하기: 세이브에 기록된 가구를 비용 없이 다시 배치(세이브 목록은 그대로 둔다).
        public void RestoreFromSave(Func<string, FurnitureData> lookup)
        {
            if (save == null || lookup == null) return;
            // 팔아 버린 기본 좌석을 먼저 치워야 그 자리에 놓았던 가구도 칸을 얻는다
            if (save.soldStarters != null)
                foreach (int idx in save.soldStarters.ToArray()) if (starters.Count > 1) RemoveStarter(idx);
            foreach (var dto in save.placedFurniture)
            {
                var data = lookup(dto.furnitureId);
                if (data == null) continue;
                var slot = FindSlotFor(data);
                if (slot == null) continue;
                items.Add(new PlacedItem { data = data, dto = dto, slot = slot, go = Place(data, slot, dto) });
                placedData.Add(data);
            }
            // 남은 기본 좌석은 옮겨 둔 위치 복원
            foreach (var st in starters)
            {
                var dto = FindStarterDto(st.index, false);
                if (dto != null && dto.hasPos) st.go.transform.position = new Vector3(dto.gridX / 100f, dto.gridY / 100f, 0f);
                var mvf = st.go.GetComponent<MovableFurniture>(); if (mvf != null) mvf.RefreshVisual();
            }
            RecalcAtmosphere();
        }

        void RecalcAtmosphere()
        {
            CurrentAtmosphere = AtmosphereSystem.Calculate(placedData);
            OnAtmosphereRecalculated?.Invoke(CurrentAtmosphere);
        }

        // ── 배치 모드(옮기기) ──
        struct Starter { public GameObject go; public int index; }
        readonly List<Starter> starters = new();

        // S1Bootstrap이 만든 시작 좌석도 옮길 수 있게 등록
        public void RegisterStarter(GameObject go, int index)
        {
            starters.Add(new Starter { go = go, index = index });
            var mv = go.AddComponent<MovableFurniture>();
            mv.area = MovableFurniture.Area.Floor;
            mv.onMoved = p => { var d = FindStarterDto(index, true); if (d != null) Store(d, p); };
        }

        PlacedFurnitureDTO FindStarterDto(int index, bool create)
        {
            if (save == null) return null;
            string key = "starter#" + index;
            foreach (var d in save.starterSeatPos) if (d.furnitureId == key) return d;
            if (!create) return null;
            var nd = new PlacedFurnitureDTO { furnitureId = key };
            save.starterSeatPos.Add(nd);
            return nd;
        }

        static void Store(PlacedFurnitureDTO d, Vector3 p)
        {
            d.hasPos = true; d.gridX = Mathf.RoundToInt(p.x * 100f); d.gridY = Mathf.RoundToInt(p.y * 100f);
        }

        void MakeMovable(GameObject go, FurnitureData data, PlacedFurnitureDTO dto)
        {
            var mv = go.AddComponent<MovableFurniture>();
            mv.area = data.wallMounted ? MovableFurniture.Area.Wall
                    : data.furnitureId == "furn_deco_rug" ? MovableFurniture.Area.FloorFlat
                    : MovableFurniture.Area.Floor;
            if (dto != null)
            {
                if (dto.hasPos) go.transform.position = new Vector3(dto.gridX / 100f, dto.gridY / 100f, 0f);
                mv.onMoved = p => Store(dto, p);
            }
        }

        GameObject Place(FurnitureData data, SlotEntry slot, PlacedFurnitureDTO dto)
        {
            slot.occupied = true;
            GameObject placed;
            if (data.type == FurnitureType.Seat)
            {
                var go = seatPrefab != null ? Instantiate(seatPrefab) : new GameObject("Seat_" + data.furnitureId);
                var sb = go.GetComponent<SeatBehaviour>(); if (sb == null) sb = go.AddComponent<SeatBehaviour>();
                sb.source = data;
                go.transform.position = slot.slot != null ? slot.slot.position : Vector3.zero;
                cafe.RegisterSeat(sb);
                placed = go;
            }
            else if (data.type == FurnitureType.Facility)
            {
                var go = facilityPrefab != null ? Instantiate(facilityPrefab) : new GameObject("Fac_" + data.furnitureId);
                var fb = go.GetComponent<FacilityBehaviour>(); if (fb == null) fb = go.AddComponent<FacilityBehaviour>();
                fb.source = data;
                go.transform.position = slot.slot != null ? slot.slot.position : Vector3.zero;
                cafe.RegisterFacility(fb);
                placed = go;
            }
            else
            {
                // Decoration: 관찰 채널은 아니지만 화면에 보여야 구매가 의미 있다(분위기 기여).
                var go = new GameObject("Deco_" + data.furnitureId);
                var db = go.AddComponent<DecorationBehaviour>();
                db.source = data;
                go.transform.position = slot.slot != null ? slot.slot.position : Vector3.zero;
                placed = go;
            }
            MakeMovable(placed, data, dto);
            return placed;
        }

        // 슬롯 선택: 벽걸이→벽 슬롯 / 창가 좌석→창가 슬롯 우선 / 그 외→창가가 아닌 바닥 슬롯 우선.
        SlotEntry FindSlotFor(FurnitureData data)
        {
            var want = data.wallMounted ? SlotKind.Wall : SlotKind.Floor;
            bool windowSeat = false;
            if (data.type == FurnitureType.Seat && data.seatTags != null)
                foreach (var t in data.seatTags) if (t == SeatTag.Window || t == SeatTag.OutsideView) windowSeat = true;

            SlotEntry fallback = null;
            foreach (var s in slots)
            {
                if (s.occupied || s.kind != want || s.minCafeLevel > CafeLevel) continue;
                if (want == SlotKind.Floor && s.nearWindow != windowSeat) { if (fallback == null) fallback = s; continue; }
                return s;
            }
            return fallback;
        }
    }
}
