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

        // 구매+배치 (관찰 채널 개방)
        public bool BuyAndPlace(FurnitureData data)
        {
            LastFail = FailReason.None;
            var slot = FindSlotFor(data);
            if (slot == null) { LastFail = FailReason.NoSpace; return false; }               // 공간 부족
            if (!economy.TrySpend(data.price)) { LastFail = FailReason.NoGold; return false; } // 잔액 부족

            Place(data, slot);
            placedData.Add(data);
            save?.placedFurniture.Add(new PlacedFurnitureDTO { furnitureId = data.furnitureId });
            OnFurniturePlaced?.Invoke(data);
            RecalcAtmosphere();
            return true;
        }

        // 이어하기: 세이브에 기록된 가구를 비용 없이 다시 배치(세이브 목록은 그대로 둔다).
        public void RestoreFromSave(Func<string, FurnitureData> lookup)
        {
            if (save == null || lookup == null) return;
            foreach (var dto in save.placedFurniture)
            {
                var data = lookup(dto.furnitureId);
                if (data == null) continue;
                var slot = FindSlotFor(data);
                if (slot == null) continue;
                Place(data, slot);
                placedData.Add(data);
            }
            RecalcAtmosphere();
        }

        void RecalcAtmosphere()
        {
            CurrentAtmosphere = AtmosphereSystem.Calculate(placedData);
            OnAtmosphereRecalculated?.Invoke(CurrentAtmosphere);
        }

        void Place(FurnitureData data, SlotEntry slot)
        {
            slot.occupied = true;
            if (data.type == FurnitureType.Seat)
            {
                var go = seatPrefab != null ? Instantiate(seatPrefab) : new GameObject("Seat_" + data.furnitureId);
                var sb = go.GetComponent<SeatBehaviour>(); if (sb == null) sb = go.AddComponent<SeatBehaviour>();
                sb.source = data;
                go.transform.position = slot.slot != null ? slot.slot.position : Vector3.zero;
                cafe.RegisterSeat(sb);
            }
            else if (data.type == FurnitureType.Facility)
            {
                var go = facilityPrefab != null ? Instantiate(facilityPrefab) : new GameObject("Fac_" + data.furnitureId);
                var fb = go.GetComponent<FacilityBehaviour>(); if (fb == null) fb = go.AddComponent<FacilityBehaviour>();
                fb.source = data;
                go.transform.position = slot.slot != null ? slot.slot.position : Vector3.zero;
                cafe.RegisterFacility(fb);
            }
            else
            {
                // Decoration: 관찰 채널은 아니지만 화면에 보여야 구매가 의미 있다(분위기 기여).
                var go = new GameObject("Deco_" + data.furnitureId);
                var db = go.AddComponent<DecorationBehaviour>();
                db.source = data;
                go.transform.position = slot.slot != null ? slot.slot.position : Vector3.zero;
            }
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
