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
        // 벽 액자(사진을 걸 수 있는 가구) 목록 — 포토 모드
        public const string PhotoFrameId = "furn_deco_frame";
        public List<(GameObject go, PlacedFurnitureDTO dto)> PhotoFrames()
        {
            var list = new List<(GameObject, PlacedFurnitureDTO)>();
            foreach (var it in items) if (it.go != null && it.dto != null && it.data != null && it.data.furnitureId == PhotoFrameId) list.Add((it.go, it.dto));
            return list;
        }
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
            var placedGo = Place(data, slot, dto);
            if (dto != null) Store(dto, placedGo.transform.position);      // 다음에 불러와도 같은 자리
            items.Add(new PlacedItem { data = data, dto = dto, slot = slot, go = placedGo });
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
                // 옮겨 둔(또는 산 자리) 좌표가 있으면 그 자리에 그대로 — 칸 수와 상관없이 복원한다
                var slot = dto.hasPos ? VirtualSlot(data, new Vector3(dto.gridX / 100f, dto.gridY / 100f, 0f)) : FindSlotFor(data);
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

        // ── 자유 배치: 정해진 칸이 다 차도, 바닥(벽)에 겹치지 않는 빈 곳이 있으면 거기에 놓는다 ──
        // 칸(slots)은 '처음 놓일 보기 좋은 자리' 역할만 하고, 개수 제한은 실제 빈 공간이 정한다.
        // 카페 레벨: Lv1은 바닥 뒤쪽 두 줄, Lv2부터 앞줄, Lv3부터 양 끝까지.
        static bool IsFlat(FurnitureData d) => d != null && d.furnitureId == "furn_deco_rug";

        SlotEntry VirtualSlot(FurnitureData data, Vector3 pos)
        {
            var go = new GameObject("Slot_free");
            go.transform.position = pos;
            var e = new SlotEntry { slot = go.transform, kind = data.wallMounted ? SlotKind.Wall : SlotKind.Floor, minCafeLevel = 1 };
            slots.Add(e);
            return e;
        }

        // 지금 놓여 있는 것들의 자리(러그는 다른 가구와 겹쳐도 된다)
        System.Collections.Generic.List<Vector3> Occupied(SlotKind kind, bool flat)
        {
            var list = new System.Collections.Generic.List<Vector3>();
            foreach (var it in items)
            {
                if (it.go == null || it.data == null) continue;
                bool wall = it.data.wallMounted;
                if ((kind == SlotKind.Wall) != wall) continue;
                if (kind == SlotKind.Floor && IsFlat(it.data) != flat) continue;
                list.Add(it.go.transform.position);
            }
            if (kind == SlotKind.Floor && !flat)
            {
                foreach (var st in starters) if (st.go != null) list.Add(st.go.transform.position);
                foreach (var f in FixtureManager.All) if (f.kind == FixtureKind.Plant) list.Add(f.transform.position);   // 큰 화분
            }
            return list;
        }

        static bool Clear(Vector3 p, System.Collections.Generic.List<Vector3> occ, SlotKind kind)
        {
            float rx = kind == SlotKind.Wall ? 1.0f : 1.25f, ry = kind == SlotKind.Wall ? 0.9f : 0.75f;
            foreach (var o in occ) if (Mathf.Abs(o.x - p.x) < rx && Mathf.Abs(o.y - p.y) < ry) return false;
            return true;
        }

        bool Allowed(Vector3 p, SlotKind kind)
        {
            int lv = save != null ? save.cafeLevel : 1;
            if (kind == SlotKind.Wall)
                return !FixtureManager.WallBlockedAt(p) && p.x <= CafeLayout.WallMaxX(lv);   // 통유리·칠판·명판·문 자리는 비운다(옮겨도 따라감)
            if (p.x < 2.4f && p.y > 2.6f) return false;                           // 문 앞 통로
            if (lv < 2 && p.y < 1.3f) return false;                               // 앞줄은 Lv2부터
            if (p.x > CafeLayout.FloorMaxX(lv)) return false;                     // 오른쪽 공간은 확장하면 드러난다
            if (lv < 3 && p.x < 1.6f) return false;                               // 왼쪽 끝은 Lv3부터
            return true;
        }

        // 뒤쪽 줄·가운데부터 차례로 훑어 처음 나오는 빈 곳
        bool FindFreeSpot(SlotKind kind, bool flat, System.Collections.Generic.List<Vector3> occ, out Vector3 spot)
        {
            spot = Vector3.zero;
            float x0, x1, y0, y1, sx, sy;
            int lvl = save != null ? save.cafeLevel : 1;
            if (kind == SlotKind.Wall) { x0 = 1.0f; x1 = CafeLayout.WallMaxX(lvl); y0 = CafeLayout.FloorTop + 1.6f; y1 = CafeLayout.WallMaxY(lvl) - 0.1f; sx = 0.6f; sy = 0.5f; }
            else { x0 = 1.0f; x1 = CafeLayout.FloorMaxX(lvl); y0 = 0.4f; y1 = CafeLayout.FloorTop - 0.7f; sx = 0.5f; sy = 0.4f; }
            float cx = (x0 + x1) * 0.5f;
            for (float y = y1; y >= y0 - 0.001f; y -= sy)
            {
                int n = Mathf.FloorToInt((x1 - x0) / sx) + 1;
                for (int k = 0; k < n; k++)
                {
                    // 가운데에서 바깥으로 번갈아
                    int off = (k + 1) / 2 * (k % 2 == 0 ? 1 : -1);
                    float x = cx + off * sx;
                    if (x < x0 || x > x1) continue;
                    var p = new Vector3(Mathf.Round(x * 10f) / 10f, Mathf.Round(y * 10f) / 10f, 0f);
                    if (!Allowed(p, kind) || !Clear(p, occ, kind)) continue;
                    spot = p; return true;
                }
            }
            return false;
        }

        // 기본 사물(큰 화분)용: 바닥 빈 곳 하나
        public bool TryFindFloorSpot(out Vector3 p) => FindFreeSpot(SlotKind.Floor, false, Occupied(SlotKind.Floor, false), out p);

        // 상점 표시용: 지금 더 놓을 수 있는 자리 수(겹치지 않게 차례로 채워 본 개수, 최대 20)
        public (int free, int total) SlotCount(SlotKind kind)
        {
            if (kind == SlotKind.Wall)
            {
                int f = 0, t = 0;
                var wocc = Occupied(SlotKind.Wall, false);
                foreach (var s in slots)
                {
                    if (s.kind != SlotKind.Wall || s.minCafeLevel > CafeLevel) continue;
                    t++; if (!s.occupied && (s.slot == null || (Clear(s.slot.position, wocc, SlotKind.Wall) && Allowed(s.slot.position, SlotKind.Wall)))) f++;
                }
                return (f, t);
            }
            var occ = Occupied(kind, false);
            int n = 0;
            while (n < 20 && FindFreeSpot(kind, false, occ, out var p)) { occ.Add(p); n++; }
            return (n, n);
        }

        SlotEntry FindSlotFor(FurnitureData data)
        {
            {
                var kind = data.wallMounted ? SlotKind.Wall : SlotKind.Floor;
                var occ = Occupied(kind, IsFlat(data));
                // 1) 미리 정해 둔 보기 좋은 칸(비어 있고, 누가 그 위로 옮겨 두지 않았으면)
                var pre = FindPresetSlot(data, occ);
                if (pre != null) return pre;
                // 2) 그 외 바닥 빈 공간(벽은 문·명판·칠판·통유리 때문에 정해 둔 칸만 쓴다)
                if (kind == SlotKind.Floor && FindFreeSpot(kind, IsFlat(data), occ, out var spot)) return VirtualSlot(data, spot);
                return null;
            }
        }

        SlotEntry FindPresetSlot(FurnitureData data, System.Collections.Generic.List<Vector3> occ)
        {
            var want = data.wallMounted ? SlotKind.Wall : SlotKind.Floor;
            bool windowSeat = false;
            if (data.type == FurnitureType.Seat && data.seatTags != null)
                foreach (var t in data.seatTags) if (t == SeatTag.Window || t == SeatTag.OutsideView) windowSeat = true;

            SlotEntry fallback = null;
            foreach (var s in slots)
            {
                if (s.occupied || s.kind != want || s.minCafeLevel > CafeLevel) continue;
                if (s.slot != null && !Clear(s.slot.position, occ, want)) continue;     // 다른 가구를 그 위로 옮겨 둔 칸
                if (want == SlotKind.Wall && s.slot != null && !Allowed(s.slot.position, SlotKind.Wall)) continue;   // 창·칠판을 그 앞으로 옮겨 둔 칸
                if (want == SlotKind.Floor && s.nearWindow != windowSeat) { if (fallback == null) fallback = s; continue; }
                return s;
            }
            return fallback;
        }
    }
}
