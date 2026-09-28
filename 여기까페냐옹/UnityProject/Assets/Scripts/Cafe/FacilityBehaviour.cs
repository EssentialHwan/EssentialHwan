using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;

namespace YeogiCafe.Cafe
{
    // IFacility의 씬 구현. FurnitureData(type==Facility)에서 태그를 읽는다.
    public class FacilityBehaviour : MonoBehaviour, IFacility
    {
        public FurnitureData source;

        string _id;
        public string FacilityId => _id ??= (source != null ? source.furnitureId : name) + "#" + GetInstanceID();
        public IReadOnlyList<FacilityTag> Tags => source != null ? source.facilityTags : System.Array.Empty<FacilityTag>();
        public bool IsFree => user == null;

        object user;
        public void Use(object cat) => user = cat;
        public void Release() => user = null;
    }
}
