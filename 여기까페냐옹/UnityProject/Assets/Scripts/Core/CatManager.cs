using System;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Data;
using YeogiCafe.Observation;

namespace YeogiCafe.Core
{
    // 사양서 §15.2 — 고양이 스폰·풀링·인스턴스 관리.
    // S1: 프리팹 없이도 동작하도록 CatBrain을 런타임 GameObject로 생성.
    public class CatManager : MonoBehaviour
    {
        public BalanceConfig cfg;
        public CafeContext cafe;
        public ObservationManager obs;               // 매니저 조립 시 주입
        public YeogiCafe.Save.SaveGame save;         // 단골 단계·선호자리 주입용
        public YeogiCafe.Order.OrderSystem orderSystem; // [S2] 선택
        public GameObject catPrefab;                 // 없으면 빈 GO 생성(S1)

        readonly List<CatBrain> active = new();
        public IReadOnlyList<CatBrain> Active => active;

        public event Action<CatBrain> OnCatSpawned;
        public event Action<CatBrain, float> OnCatLeft;   // (brain, satisfaction)

        public CatBrain Spawn(CatData data)
        {
            GameObject go = catPrefab != null ? Instantiate(catPrefab) : new GameObject("Cat_" + data.catId);
            // Unity 객체는 ?? 연산자 금지(에디터의 fake-null 때문에 AddComponent가 건너뛰어짐)
            var brain = go.GetComponent<CatBrain>(); if (brain == null) brain = go.AddComponent<CatBrain>();
            // 이동: 프리팹에 ICatMover(NavMeshCatMover 등)가 있으면 사용, 없으면 SimpleLerpMover 부착
            ICatMover mover = go.GetComponent<ICatMover>();
            if (mover == null || (mover as UnityEngine.Object) == null) mover = go.AddComponent<SimpleLerpMover>();

            // 비주얼: 표현 전담 컴포넌트(애니메이션·앉은 높이·시설 이동·퇴장). 로직과 분리.
            var view = go.GetComponent<YeogiCafe.Visual.CatView>();
            if (view == null) view = go.AddComponent<YeogiCafe.Visual.CatView>();

            brain.data = data;
            brain.cfg = cfg;
            brain.Obs = obs;
            brain.Mover = mover;
            brain.OrderSystem = orderSystem;   // null이면 CatBrain이 S1 직행
            brain.Entrance = cafe.Entrance;
            brain.GetSeats = cafe.GetSeats;
            brain.GetFacilities = cafe.GetFacilities;
            brain.GetAvailableMenus = cafe.GetAvailableMenus;
            // 친구 근접: 이 고양이(catId) 기준 착석 친구 수 (정식 관계 반영)
            string myId = data.catId;
            brain.NearbyFriends = _ => cafe.CountFriendsSeated(myId);
            brain.NearbyStrangers = cafe.NearbyStrangers;
            brain.SetOccupant = cafe.SetSeatOccupant;
            brain.RandomFloorPoint = CafeLayout.RandomFloorPoint;   // 식사 후 카페 어디든 돌아다님
            brain.GetOthers = () => active;
            brain.GetRegularStage = () => save != null ? save.GetOrCreateCat(myId).regularStage : 1;
            brain.GetCafeLevel = () => save != null ? save.cafeLevel : 1;                           // 대장냥이 자리 뺏기 후보
            // [프리플라이트] 단골 고정자리 주입 제거(favoriteSeat는 포스트 MVP)

            go.transform.position = cafe.Entrance;
            view.Init(data);
            active.Add(brain);
            OnCatSpawned?.Invoke(brain);
            return brain;
        }

        public void Despawn(CatBrain brain, float satisfaction)
        {
            active.Remove(brain);
            OnCatLeft?.Invoke(brain, satisfaction);
            if (brain == null) return;
            // 로직은 여기서 끝. 화면에서는 문까지 걸어나간 뒤 사라진다(표현).
            var view = brain.GetComponent<YeogiCafe.Visual.CatView>();
            if (view != null) { view.BeginExit(cafe.Entrance); leaving.Add(brain); }
            else Destroy(brain.gameObject);
        }

        public bool HasActiveCats => active.Count > 0;

        // 퇴장 걸음 중인 고양이도 '아직 카페 안'으로 친다(같은 개체가 문 앞에서 엇갈려 둘로 보이던 문제)
        readonly List<CatBrain> leaving = new();

        public bool IsInCafe(string catId)
        {
            foreach (var b in active) if (b != null && b.data != null && b.data.catId == catId) return true;
            leaving.RemoveAll(b => b == null);
            foreach (var b in leaving) if (b.data != null && b.data.catId == catId) return true;
            return false;
        }
    }
}
