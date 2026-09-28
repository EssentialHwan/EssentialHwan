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

        [Serializable] public class SlotEntry { public Transform slot; public bool occupied; }
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
            var slot = FirstFreeSlot();
            if (slot == null) return false;               // 공간 부족
            if (!economy.TrySpend(data.price)) return false; // 잔액 부족

            Place(data, slot);
            placedData.Add(data);
            save?.placedFurniture.Add(new PlacedFurnitureDTO { furnitureId = data.furnitureId });
            OnFurniturePlaced?.Invoke(data);
            RecalcAtmosphere();
            return true;
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
                var sb = go.GetComponent<SeatBehaviour>() ?? go.AddComponent<SeatBehaviour>();
                sb.source = data;
                go.transform.position = slot.slot != null ? slot.slot.position : Vector3.zero;
                cafe.RegisterSeat(sb);
            }
            else if (data.type == FurnitureType.Facility)
            {
                var go = facilityPrefab != null ? Instantiate(facilityPrefab) : new GameObject("Fac_" + data.furnitureId);
                var fb = go.GetComponent<FacilityBehaviour>() ?? go.AddComponent<FacilityBehaviour>();
                fb.source = data;
                go.transform.position = slot.slot != null ? slot.slot.position : Vector3.zero;
                cafe.RegisterFacility(fb);
            }
            // Decoration: 분위기 기여만(정식). S1 생략.
        }

        SlotEntry FirstFreeSlot()
        {
            foreach (var s in slots) if (!s.occupied) return s;
            return null;
        }
    }
}
