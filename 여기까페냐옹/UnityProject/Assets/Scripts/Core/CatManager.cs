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
            var brain = go.GetComponent<CatBrain>() ?? go.AddComponent<CatBrain>();
            // 이동: 프리팹에 ICatMover(NavMeshCatMover 등)가 있으면 사용, 없으면 SimpleLerpMover 부착
            var mover = go.GetComponent<ICatMover>() ?? go.AddComponent<SimpleLerpMover>();

            // 비주얼: 도트 스프라이트 표시(없으면 안 보이지만 로직은 정상)
            if (data.worldSprite != null)
            {
                var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
                sr.sprite = data.worldSprite;
                sr.sortingOrder = 10;            // 고양이는 가구 위에
            }

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
            // [프리플라이트] 단골 고정자리 주입 제거(favoriteSeat는 포스트 MVP)

            go.transform.position = cafe.Entrance;
            active.Add(brain);
            OnCatSpawned?.Invoke(brain);
            return brain;
        }

        public void Despawn(CatBrain brain, float satisfaction)
        {
            active.Remove(brain);
            OnCatLeft?.Invoke(brain, satisfaction);
            if (brain != null) Destroy(brain.gameObject);
        }

        public bool HasActiveCats => active.Count > 0;
    }
}
