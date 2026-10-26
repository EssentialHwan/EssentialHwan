using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Economy;
using YeogiCafe.Save;
using YeogiCafe.Visual;

namespace YeogiCafe.Cafe
{
    // 기본 사물: 처음부터 가게에 있던 통유리 창·칠판·천장 조명·냥슐랭 명판·큰 화분.
    // 플레이테스트 요청 — 배치 모드에서 옮기거나 뺄 수 있게. 처음 받은 개수는 무료(빼면 보관함, 다시 놓을 때도 무료).
    // 상점(창·조명 탭)에서 더 살 수 있고, 산 것은 팔 수 있다(반값). 냥슐랭 명판은 상으로 받은 것이라 옮기기만.
    public enum FixtureKind { Glass8, Glass4, MenuBoard, Lamp, Plaque, Plant }

    public class Fixture : MonoBehaviour
    {
        public FixtureKind kind;
        public FixtureDTO dto;
        public bool CanStore => kind != FixtureKind.Plaque;
        public bool Bought => dto != null && dto.bought;
        SpriteRenderer sr;

        void LateUpdate()
        {
            if (kind != FixtureKind.Plant) return;                       // 바닥에 놓이는 화분만 앞뒤 순서를 따라간다
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sortingOrder != 30000) sr.sortingOrder = YSort.Order(transform.position.y);
        }
    }

    public class FixtureManager : MonoBehaviour
    {
        public static FixtureManager Instance { get; private set; }
        SaveGame save; EconomyManager economy; FurnitureManager furniture;
        readonly List<Fixture> live = new();
        Sprite boardSprite, lampSprite, plantSprite;
        const float ShadeBottomY = 6.55f;                 // 조명 갓 아랫단(상단 메뉴 바 아래로 보이게)

        public static IEnumerable<Fixture> All { get { if (Instance != null) foreach (var f in Instance.live) if (f != null) yield return f; } }

        // ── 종류별 정보 ──
        public static int Price(FixtureKind k) => k switch
        {
            FixtureKind.Glass8 => 900, FixtureKind.Glass4 => 500, FixtureKind.MenuBoard => 150,
            FixtureKind.Lamp => 120, FixtureKind.Plant => 200, _ => 0,
        };
        public static int SellPrice(FixtureKind k) => Mathf.Max(10, Mathf.RoundToInt(Price(k) * FurnitureManager.SellRate / 10f) * 10);
        public static readonly FixtureKind[] ForSale = { FixtureKind.Glass4, FixtureKind.Glass8, FixtureKind.Lamp, FixtureKind.MenuBoard, FixtureKind.Plant };
        public static bool IsGlass(FixtureKind k) => k == FixtureKind.Glass8 || k == FixtureKind.Glass4;
        static int GlassPx(FixtureKind k) => k == FixtureKind.Glass4 ? 128 : GlassWindowDrawing.GW;
        static float GlassHalf(FixtureKind k) => (GlassPx(k) + GlassWindowDrawing.Border * 2) / 64f;

        public static FixtureManager Ensure(SaveGame save, EconomyManager economy, FurnitureManager furniture)
        {
            if (Instance == null) Instance = new GameObject("[Fixtures]").AddComponent<FixtureManager>();
            Instance.Init(save, economy, furniture);
            return Instance;
        }

        void Init(SaveGame s, EconomyManager eco, FurnitureManager fm)
        {
            save = s; economy = eco; furniture = fm;
            if (save == null) return;
            CaptureSceneArt();
            EnsureArt();
            if (!save.fixturesInit) MakeDefaults();
            foreach (var f in live) if (f != null && f.kind != FixtureKind.Plaque) Destroy(f.gameObject);
            live.RemoveAll(f => f == null || f.kind != FixtureKind.Plaque);
            foreach (var d in save.fixtures) if (!d.stored) Spawn(d);
        }

        // 씬에 굽혀 있던 칠판·조명·큰 화분 그림을 가져오고 원본은 지운다(이제 기본 사물이 대신 그린다)
        void CaptureSceneArt()
        {
            var room = GameObject.Find("Room");
            if (room == null) return;
            var kill = new List<GameObject>();
            foreach (Transform t in room.transform)
            {
                var sr = t.GetComponent<SpriteRenderer>(); if (sr == null) continue;
                if (t.name == "MenuBoard") { boardSprite ??= sr.sprite; kill.Add(t.gameObject); }
                else if (t.name == "Lamp") { lampSprite ??= sr.sprite; kill.Add(t.gameObject); }
                else if (t.name == "BigPlant") { plantSprite ??= sr.sprite; kill.Add(t.gameObject); }
            }
            foreach (var g in kill) Destroy(g);
        }

        void EnsureArt()
        {
            // 씬에서 못 가져왔으면(이미 지운 뒤 다시 불렀을 때 등) 절차 생성 그림으로
            if (boardSprite == null) boardSprite = RuntimeSprites.Get("bg_menu_board", RoomDrawing.MenuBoard);
            if (lampSprite == null) lampSprite = RuntimeSprites.Get("bg_pendant_lamp", RoomDrawing.PendantLamp);
            if (plantSprite == null) plantSprite = RuntimeSprites.Get("bg_big_plant", RoomDrawing.BigPlant);
        }

        float LampY()
        {
            // 갓 아랫단이 ShadeBottomY에 오도록(그림 기준점이 위쪽이든 아래쪽이든)
            if (lampSprite == null) return CafeLayout.CeilingY - 1f;
            float bottomFromPivot = -lampSprite.pivot.y / lampSprite.pixelsPerUnit;
            return ShadeBottomY - bottomFromPivot;
        }

        void MakeDefaults()
        {
            save.fixtures.Clear();
            Add(FixtureKind.Glass8, new Vector3(CafeLayout.GlassCenterX, CafeLayout.GlassBottom, 0f));
            Add(FixtureKind.MenuBoard, new Vector3(save.cafeLevel <= 1 ? 12.95f : CafeLayout.MenuBoard.x, CafeLayout.MenuBoard.y, 0f));
            foreach (var lx in CafeLayout.LampX) Add(FixtureKind.Lamp, new Vector3(lx, LampY(), 0f));
            Add(FixtureKind.Plaque, NyanPlaque.Pos);
            Add(FixtureKind.Plant, CafeLayout.BigPlant);
            save.fixturesInit = true;
        }

        FixtureDTO Add(FixtureKind k, Vector3 p, bool bought = false)
        {
            var d = new FixtureDTO { kind = k.ToString(), bought = bought };
            Store(d, p);
            save.fixtures.Add(d);
            return d;
        }

        static void Store(FixtureDTO d, Vector3 p) { d.x = Mathf.RoundToInt(p.x * 100f); d.y = Mathf.RoundToInt(p.y * 100f); }
        static Vector3 PosOf(FixtureDTO d) => new Vector3(d.x / 100f, d.y / 100f, 0f);
        public static FixtureKind KindOf(FixtureDTO d) => System.Enum.TryParse<FixtureKind>(d.kind, out var k) ? k : FixtureKind.Plant;

        // ── 화면에 만들기 ──
        Fixture Spawn(FixtureDTO d)
        {
            var k = KindOf(d);
            GameObject go; MovableFurniture mv;
            var pos = PosOf(d);
            if (k == FixtureKind.Plaque)
            {
                var plaque = NyanPlaque.Ensure(save);
                go = plaque.gameObject;
                go.transform.position = pos;
                mv = go.GetComponent<MovableFurniture>(); if (mv == null) mv = go.AddComponent<MovableFurniture>();
                mv.area = MovableFurniture.Area.Wall;
            }
            else if (IsGlass(k))
            {
                go = new GameObject("GlassWindow");
                go.transform.position = new Vector3(pos.x, CafeLayout.GlassBottom, 0f);
                var gv = go.AddComponent<GlassWindowView>();
                gv.glassPx = GlassPx(k);
                mv = go.AddComponent<MovableFurniture>();
                mv.area = MovableFurniture.Area.Wall; mv.lockY = true; mv.lockedY = CafeLayout.GlassBottom;
                mv.halfWidth = GlassHalf(k); mv.rendererOverride = gv.FrameRenderer;
            }
            else
            {
                string name = k == FixtureKind.MenuBoard ? "MenuBoard" : k == FixtureKind.Lamp ? "Lamp" : "BigPlant";
                go = new GameObject(name);
                go.transform.position = k == FixtureKind.Lamp ? new Vector3(pos.x, LampY(), 0f) : pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = k == FixtureKind.MenuBoard ? boardSprite : k == FixtureKind.Lamp ? lampSprite : plantSprite;
                sr.sortingOrder = k == FixtureKind.MenuBoard ? YSort.WallProps : k == FixtureKind.Lamp ? YSort.WallProps + 3 : YSort.Order(pos.y);
                mv = go.AddComponent<MovableFurniture>();
                mv.area = k == FixtureKind.MenuBoard ? MovableFurniture.Area.Wall
                        : k == FixtureKind.Lamp ? MovableFurniture.Area.Ceiling : MovableFurniture.Area.Floor;
                if (k == FixtureKind.Lamp) mv.lockedY = LampY();
            }
            var f = go.GetComponent<Fixture>(); if (f == null) f = go.AddComponent<Fixture>();
            f.kind = k; f.dto = d;
            mv.onMoved = p => Store(d, p);
            if (!live.Contains(f)) live.Add(f);
            return f;
        }

        // ── 보관함 / 꺼내기 / 사기 / 팔기 ──
        public int StoredCount(FixtureKind k)
        {
            int n = 0;
            if (save != null) foreach (var d in save.fixtures) if (d.stored && KindOf(d) == k) n++;
            return n;
        }

        public bool StoreAway(Fixture f)
        {
            if (f == null || !f.CanStore || f.dto == null) return false;
            f.dto.stored = true;
            live.Remove(f);
            Destroy(f.gameObject);
            return true;
        }

        public bool SellBought(Fixture f, out int gold)
        {
            gold = 0;
            if (f == null || !f.Bought) return false;
            gold = SellPrice(f.kind);
            economy?.Refund(gold);
            save.fixtures.Remove(f.dto);
            live.Remove(f);
            Destroy(f.gameObject);
            return true;
        }

        public enum PlaceResult { Ok, NoSpace, NoGold, NoneStored }

        public PlaceResult TakeOut(FixtureKind k)
        {
            FixtureDTO d = null;
            foreach (var x in save.fixtures) if (x.stored && KindOf(x) == k) { d = x; break; }
            if (d == null) return PlaceResult.NoneStored;
            if (!FindSpot(k, out var p)) return PlaceResult.NoSpace;
            Store(d, p); d.stored = false;
            Spawn(d);
            return PlaceResult.Ok;
        }

        public PlaceResult Buy(FixtureKind k)
        {
            if (!FindSpot(k, out var p)) return PlaceResult.NoSpace;
            if (economy == null || !economy.TrySpend(Price(k))) return PlaceResult.NoGold;
            Spawn(Add(k, p, bought: true));
            return PlaceResult.Ok;
        }

        // 빈 자리 찾기: 벽(창·칠판)은 오른쪽부터 겹치지 않는 곳, 천장 조명은 다른 조명과 떨어진 곳, 화분은 바닥 빈 곳
        bool FindSpot(FixtureKind k, out Vector3 p)
        {
            p = Vector3.zero;
            int lv = CafeLayout.ViewLevel;
            if (k == FixtureKind.Plant) return furniture != null && furniture.TryFindFloorSpot(out p);
            if (k == FixtureKind.Lamp)
            {
                for (float x = CafeLayout.WallMaxX(lv); x >= 1.0f; x -= 0.5f)
                {
                    bool ok = true;
                    foreach (var f in live) if (f != null && f.kind == FixtureKind.Lamp && Mathf.Abs(f.transform.position.x - x) < 1.6f) ok = false;
                    if (ok) { p = new Vector3(Mathf.Round(x * 10f) / 10f, LampY(), 0f); return true; }
                }
                return false;
            }
            float half = IsGlass(k) ? GlassHalf(k) : 0.65f;
            float y = IsGlass(k) ? CafeLayout.GlassBottom : CafeLayout.MenuBoard.y;
            float h = IsGlass(k) ? GlassWindowView.Height + 0.3f : 1.1f;
            var obstacles = new List<Rect>(WallObstacles(null));
            for (float x = CafeLayout.WallMaxX(lv) + 0.8f - half; x >= 0.5f + half; x -= 0.1f)
            {
                var r = new Rect(x - half, y - 0.15f, half * 2f, h);
                bool free = true;
                foreach (var b in obstacles) if (b.Overlaps(r)) { free = false; break; }
                if (!free) continue;
                p = new Vector3(Mathf.Round(x * 10f) / 10f, y, 0f); return true;
            }
            return false;
        }

        // 벽의 이 네모가 비어 있나(기본 사물·벽걸이 가구·문과 안 겹치게)
        public static bool WallRectFree(Rect r, Object ignore = null)
        {
            foreach (var b in WallObstacles(ignore)) if (b.Overlaps(r)) return false;
            return true;
        }
        public static bool WallRectFree(Bounds b, Object ignore) =>
            WallRectFree(new Rect(b.min.x + 0.06f, b.min.y + 0.06f, b.size.x - 0.12f, b.size.y - 0.12f), ignore);

        // 벽걸이 가구 자리 판정(FurnitureManager): 이 점 근처에 창·칠판·명판·문이 있으면 못 건다
        public static bool WallBlockedAt(Vector3 p)
        {
            var r = new Rect(p.x - 0.5f, p.y - 0.45f, 1.0f, 0.9f);
            foreach (var b in FixtureRects(null)) if (b.Overlaps(r)) return true;
            var door = DoorRect(); if (door.HasValue && door.Value.Overlaps(r)) return true;
            return false;
        }

        static IEnumerable<Rect> FixtureRects(Object ignore)
        {
            var ig = ignore is Component ic ? ic.gameObject : ignore as GameObject;
            foreach (var f in All)
            {
                if ((ig != null && f.gameObject == ig) || f.kind == FixtureKind.Lamp || f.kind == FixtureKind.Plant) continue;
                var mv = f.GetComponent<MovableFurniture>();
                var sr = mv != null ? mv.Renderer : f.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                var b = sr.bounds;
                if (IsGlass(f.kind)) b.Encapsulate(new Vector3(b.center.x, CafeLayout.GlassBottom + GlassWindowView.Height, 0f));
                yield return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            }
        }

        static IEnumerable<Rect> WallObstacles(Object ignore)
        {
            foreach (var r in FixtureRects(ignore)) yield return r;
            var door = DoorRect(); if (door.HasValue) yield return door.Value;
            foreach (var mv in FindObjectsOfType<MovableFurniture>())
            {
                var ig = ignore is Component ic ? ic.gameObject : ignore as GameObject;
                if ((ig != null && mv.gameObject == ig) || mv.area != MovableFurniture.Area.Wall || mv.GetComponent<Fixture>() != null) continue;
                var sr = mv.Renderer; if (sr == null) continue;
                var b = sr.bounds; yield return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            }
        }

        static Rect? DoorRect()
        {
            var room = GameObject.Find("Room");
            var d = room != null ? room.transform.Find("Door") : null;
            var sr = d != null ? d.GetComponent<SpriteRenderer>() : null;
            if (sr == null) return null;
            var b = sr.bounds; return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
        }

        // 조명·창 위치(조명 효과·그림자·햇살이 따라간다)
        public static IEnumerable<Transform> Lamps() { foreach (var f in All) if (f.kind == FixtureKind.Lamp) yield return f.transform; }
        public static Transform PrimaryGlass()
        {
            Transform best = null; int bestPx = 0;
            foreach (var f in All)
            {
                if (!IsGlass(f.kind)) continue;
                int px = GlassPx(f.kind);
                if (px > bestPx) { best = f.transform; bestPx = px; }
            }
            return best;
        }
        public static float PrimaryGlassWidth()
        {
            var g = PrimaryGlass(); var v = g != null ? g.GetComponent<GlassWindowView>() : null;
            return v != null ? v.Width : GlassWindowDrawing.GW / 32f;
        }

        // 상점 아이콘
        public Sprite Icon(FixtureKind k) => k switch
        {
            FixtureKind.Glass8 => RuntimeSprites.Get("glass_frame", GlassWindowDrawing.Frame),
            FixtureKind.Glass4 => RuntimeSprites.Get("glass_frame_128", () => GlassWindowDrawing.Frame(128)),
            FixtureKind.MenuBoard => boardSprite,
            FixtureKind.Lamp => lampSprite,
            FixtureKind.Plant => plantSprite,
            _ => null,
        };

        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
