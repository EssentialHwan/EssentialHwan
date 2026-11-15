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
        public Sprite worldSprite;           // 씬 표시용
        public bool wallMounted;             // 벽걸이(창문·시계·액자·커튼) → 벽 슬롯에 배치

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
