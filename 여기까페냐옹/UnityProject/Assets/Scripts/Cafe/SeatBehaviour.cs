using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;

namespace YeogiCafe.Cafe
{
    // ISeat의 씬 구현. FurnitureData(type==Seat)에서 태그를 읽는다.
    public class SeatBehaviour : MonoBehaviour, ISeat
    {
        public FurnitureData source;
        [SerializeField] bool isCentral;
        AtmosphereAxis localDominant = AtmosphereAxis.Quiet;

        string _id;
        public string SeatId => _id ??= (source != null ? source.furnitureId : name) + "#" + GetInstanceID();
        public IReadOnlyList<SeatTag> Tags => source != null ? source.seatTags : System.Array.Empty<SeatTag>();
        public Vector3 WorldPos => transform.position;
        public bool IsFree => occupant == null;
        public bool IsCentral => isCentral;
        public AtmosphereAxis LocalDominantAxis => localDominant;

        object occupant;
        public void Occupy(object cat) => occupant = cat;
        public void Vacate() => occupant = null;
        public void SetLocalDominant(AtmosphereAxis axis) => localDominant = axis;
    }
}
