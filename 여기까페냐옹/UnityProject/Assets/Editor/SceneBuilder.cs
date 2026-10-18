#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YeogiCafe.Core;
using YeogiCafe.Data;

namespace YeogiCafe.EditorTools
{
    // 원클릭 씬 배선기. "여기까페냐옹 ▸ M1 플레이 씬 생성"을 누르면
    // 카메라·바닥·GameRoot(S1Bootstrap)를 만들고, 생성된 SO 에셋을 자동으로 찾아 연결한다.
    // 전제: 먼저 "1.0 콘텐츠 에셋 생성"을 실행해 Assets/Data/에 SO가 있어야 함.
    public static class SceneBuilder
    {
        // 처음 받은 PC에서 한 번에: 콘텐츠 SO → 픽셀 아트 → 씬. (각 단계는 아래 개별 메뉴로도 실행 가능)
        [MenuItem("여기까페냐옹/★ 전체 셋업 (콘텐츠+아트+씬)", priority = 0)]
        public static void SetupAll()
        {
            ContentGenerator.GenerateAll();
            PixelArtGenerator.GenerateAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            BuildScene();
        }

        [MenuItem("여기까페냐옹/M1 플레이 씬 생성", priority = 100)]
        public static void BuildScene()
        {
            // ⚠ 새 씬(Single)을 먼저 만든다. NewScene은 이전 씬과 '참조 없는 에셋'을 언로드하므로,
            //   SO를 먼저 로드해 두면 그 참조가 죽어(null 판정) 상점 목록이 0~1개만 남는 버그가 있었다.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 콘텐츠 SO 존재 확인
            var cats = LoadAll<CatData>("Assets/Data/Cats");
            var menus = LoadAll<MenuData>("Assets/Data/Menus");
            var furn = LoadAll<FurnitureData>("Assets/Data/Furniture");
            var events = LoadAll<CatEventData>("Assets/Data/Events");
            var balance = LoadFirst<BalanceConfig>("Assets/Data");
            var levels = LoadFirst<CafeLevelConfig>("Assets/Data/Config");

            if (cats.Count == 0 || menus.Count == 0 || furn.Count == 0)
            {
                EditorUtility.DisplayDialog("콘텐츠 없음",
                    "먼저 메뉴 '여기까페냐옹 ▸ 1.0 콘텐츠 에셋 생성'을 실행하세요.\n(Assets/Data 에 SO가 있어야 합니다)", "확인");
                return;
            }
            if (balance == null)
            {
                // BalanceConfig가 없으면 자동 생성
                balance = ScriptableObject.CreateInstance<BalanceConfig>();
                AssetDatabase.CreateAsset(balance, "Assets/Data/Balance.asset");
                AssetDatabase.SaveAssets();
            }

            // 생성된 도트 스프라이트를 데이터에 자동 연결(있으면). 없으면 프리미티브로만 보임.
            LinkSprites(cats, furn);

            // 로컬라이즈 CSV를 Resources로 동기화(런타임 L.EnsureLoaded()가 Resources.Load<TextAsset>("strings") 사용).
            SyncStringsToResources();

            // 카메라 — 2D 정면 직교. 프레이밍은 CafeLayout(공간 배치표) 기준.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CafeLayout.CameraSize;
            cam.backgroundColor = new Color(0.24f, 0.19f, 0.17f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = CafeLayout.CameraPos;
            camGo.transform.rotation = Quaternion.identity;

            BuildRoom();

            // GameRoot + S1Bootstrap
            var root = new GameObject("[GameRoot]");
            var boot = root.AddComponent<S1Bootstrap>();

            // 데이터 자동 연결
            boot.balance = balance;
            boot.cafeLevelConfig = levels;
            boot.starterMenus = menus;
            boot.allCats = cats;
            boot.allEvents = events;
            boot.starterCats = PickStarters(cats);
            boot.starterSeats = PickStarterSeats(furn);
            boot.windowSeatData = furn.FirstOrDefault(f => f.furnitureId == "furn_seat_window");
            // 상점 판매 목록: 시작 좌석 외 모든 가구(창가석·시설·데코 포함)
            boot.allFurniture = furn;
            boot.shopFurniture = PickShopItems(furn, boot.starterSeats);
            Debug.Log($"[SceneBuilder] 가구 {furn.Count}종 · 시작 좌석 {boot.starterSeats.Count} · 상점 {boot.shopFurniture.Count}\n" +
                      $"all=[{string.Join(",", furn.Select(f => f.furnitureId))}]\nstarter=[{string.Join(",", boot.starterSeats.Select(f => f.furnitureId))}]\n" +
                      $"shop=[{string.Join(",", boot.shopFurniture.Select(f => f.furnitureId))}]");

            // UI 총괄 컨트롤러(런타임에 Canvas/HUD/상점/정산/도감 생성)
            var ui = root.AddComponent<YeogiCafe.UI.GameUiController>();
            ui.bootstrap = boot;

            // DevConsole(F1) 자동 배치 — 플레이테스트 가속(배속·골드 치트)
            var dev = root.AddComponent<YeogiCafe.DevTools.DevConsole>();

            // 씬 저장
            const string dir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Scenes");

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(boot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, $"{dir}/Cafe.unity");
            AddSceneToBuild($"{dir}/Cafe.unity");
            Debug.Log("[SceneBuilder] 플레이 씬 생성 완료 → Assets/Scenes/Cafe.unity. Play로 실행. (F1: 개발 콘솔)");
        }

        static List<CatData> PickStarters(List<CatData> all)
        {
            string[] pref = { "cat_cheese", "cat_calico", "cat_cow" };
            var picked = all.Where(c => pref.Contains(c.catId)).ToList();
            if (picked.Count == 0) picked = all.Take(3).ToList();
            return picked;
        }

        static List<FurnitureData> PickStarterSeats(List<FurnitureData> all)
        {
            // 시작은 일반석 3개만(출시스코프 G.6: "창가석 없이 치즈냥 → 일반석 fallback").
            // 구석석·바테이블을 처음부터 깔아두면 삼색냥(구석) 등의 취향이 공짜로 드러나 관찰 루프가 무너진다.
            var normal = all.FirstOrDefault(f => f.furnitureId == "furn_seat_normal");
            if (normal != null) return new List<FurnitureData> { normal, normal, normal };
            return all.Where(f => f.type == FurnitureType.Seat && f.furnitureId != "furn_seat_window").Take(3).ToList();
        }

        // 상점 판매 목록: 시작 시 이미 놓인 좌석을 제외한 모든 가구(창가석·시설·데코 포함)
        static List<FurnitureData> PickShopItems(List<FurnitureData> all, List<FurnitureData> starterSeats)
        {
            // 일반석도 추가 구매 가능하게 전체 18종을 판다(좌석 수 = 동시 손님 수).
            return all.Where(f => f != null).OrderBy(f => f.price).ToList();
        }

        // 생성된 PNG(Assets/Art/Generated)에서 스프라이트를 찾아 데이터에 연결·저장
        static void LinkSprites(List<CatData> cats, List<FurnitureData> furn)
        {
            bool dirty = false;
            // 항상 다시 연결(아트를 재생성/교체해도 최신 스프라이트를 가리키도록).
            // 정식 아트로 바꿀 때: Assets/Art/Override/ 에 같은 파일명을 두면 그것을 우선 사용한다.
            foreach (var c in cats)
            {
                var s = LoadArt($"cats/{c.catId}.png");
                if (s != null && c.worldSprite != s) { c.worldSprite = s; dirty = true; }
                var frames = LoadSheetFrames(c.catId);
                if (frames != null) { c.animFrames = frames; dirty = true; }
                EditorUtility.SetDirty(c);
            }
            foreach (var f in furn)
            {
                var s = LoadArt($"furniture/{f.furnitureId}.png");
                if (s != null && f.worldSprite != s) { f.worldSprite = s; EditorUtility.SetDirty(f); dirty = true; }
            }
            if (dirty) AssetDatabase.SaveAssets();
        }

        // Localization/strings.csv → Resources/strings.csv 복사(단일 소스 유지).
        static void SyncStringsToResources()
        {
            const string src = "Assets/Localization/strings.csv";
            const string dstDir = "Assets/Resources";
            const string dst = "Assets/Resources/strings.csv";
            if (!System.IO.File.Exists(src)) return;
            if (!AssetDatabase.IsValidFolder(dstDir)) AssetDatabase.CreateFolder("Assets", "Resources");
            System.IO.File.Copy(System.IO.Path.GetFullPath(src), System.IO.Path.GetFullPath(dst), overwrite: true);
            AssetDatabase.ImportAsset(dst);
        }

        static Sprite LoadSprite(string path)
            => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        // 정식 아트 교체 지점: Override 폴더 우선 → 없으면 절차 생성물.
        const string OverrideRoot = "Assets/Art/Override";
        const string GeneratedRoot = "Assets/Art/Generated";
        static Sprite LoadArt(string relPath)
            => LoadSprite($"{OverrideRoot}/{relPath}") ?? LoadSprite($"{GeneratedRoot}/{relPath}");

        static readonly string[] FrameNames = { "idle0", "idle1", "walk0", "walk1", "eat0", "eat1", "sleep", "groom", "stretch", "order" };

        static Sprite[] LoadSheetFrames(string catId)
        {
            foreach (var root in new[] { OverrideRoot, GeneratedRoot })
            {
                var path = $"{root}/cats_sheet/{catId}_sheet.png";
                var subs = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
                if (subs.Count == 0) continue;
                var result = new Sprite[FrameNames.Length];
                for (int i = 0; i < FrameNames.Length; i++)
                    result[i] = subs.FirstOrDefault(sp => sp.name == FrameNames[i]);
                return result;
            }
            return null;
        }

        // ── 공간(배경) 조립: 바닥·징두리·벽 타일 + 문·창·카운터·조명·칠판·큰 화분 ──
        static void BuildRoom()
        {
            var room = new GameObject("Room").transform;
            var floor = LoadArt("bg/floor.png");
            var wall = LoadArt("bg/wall.png");
            var wainscot = LoadArt("bg/wainscot.png");
            float top = CafeLayout.FloorTop;
            for (int x = (int)CafeLayout.RoomLeft; x < (int)CafeLayout.RoomRight; x++)
            {
                for (int y = -2; y < 5; y++) Tile(room, floor, x, top - 5 + y, YeogiCafe.Visual.YSort.FloorTiles);
                Tile(room, wainscot, x, top, YeogiCafe.Visual.YSort.WallTiles + 1);
                for (int y = 1; y < 7; y++) Tile(room, wall, x, top + y, YeogiCafe.Visual.YSort.WallTiles);
            }
            Prop(room, "Door", LoadArt("bg/door.png"), CafeLayout.Door, YeogiCafe.Visual.YSort.WallProps);
            // 창: 통유리 창은 런타임 WeatherView가 날씨·시간대에 맞춰 그린다
            Prop(room, "MenuBoard", LoadArt("bg/menu_board.png"), CafeLayout.MenuBoard, YeogiCafe.Visual.YSort.WallProps);
            foreach (var lx in CafeLayout.LampX)
                Prop(room, "Lamp", LoadArt("bg/pendant_lamp.png"), new Vector3(lx, CafeLayout.CeilingY, 0), YeogiCafe.Visual.YSort.WallProps + 3);
            Prop(room, "Counter", LoadArt("bg/counter.png"), CafeLayout.Counter, YeogiCafe.Visual.YSort.Order(CafeLayout.Counter.y));
            Prop(room, "BigPlant", LoadArt("bg/big_plant.png"), CafeLayout.BigPlant, YeogiCafe.Visual.YSort.Order(CafeLayout.BigPlant.y));
        }

        static void Tile(Transform parent, Sprite s, float x, float y, int order)
        {
            if (s == null) return;
            var go = new GameObject($"tile_{x}_{y}");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingOrder = order;
        }

        static void Prop(Transform parent, string name, Sprite s, Vector3 pos, int order)
        {
            if (s == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingOrder = order;
        }

        static void AddSceneToBuild(string path)
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == path)) return;
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static List<T> LoadAll<T>(string folder) where T : Object
        {
            var list = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder)) return list;
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<T>(path);
                if (a != null) list.Add(a);
            }
            return list;
        }

        static T LoadFirst<T>(string folder) where T : Object
        {
            var all = LoadAll<T>(folder);
            return all.Count > 0 ? all[0] : null;
        }
    }
}
#endif
