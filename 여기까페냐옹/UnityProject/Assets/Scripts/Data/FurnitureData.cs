using System;
using UnityEngine;

namespace YeogiCafe.Data
{
    // 사양서 §1.2 — 가구는 좌석/시설 실체 + 분위기 기여 + 관찰 채널 개방.
    [CreateAssetMenu(menuName = "YeogiCafe/FurnitureData")]
    public class FurnitureData : ScriptableObject
    {
        public string furnitureId;
        public string displayNameKey;
        public FurnitureType type;

        [Header("Seat (type==Seat)")]
        public SeatTag[] seatTags;

        [Header("Facility (type==Facility)")]
        public FacilityTag[] facilityTags;
        public string[] facilityActionIds;   // ClimbTop, Sleep, PlaySolo...

        [Header("분위기 기여")]
        public AtmoContribution[] atmosphere;

        [Header("경제/배치")]
        public int price;
        public Vector2Int size = Vector2Int.one;

        [Serializable]
        public struct AtmoContribution { public AtmosphereAxis axis; public int value; }
    }
}
