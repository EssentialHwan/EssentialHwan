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
        [MenuItem("여기까페냐옹/M1 플레이 씬 생성", priority = 100)]
        public static void BuildScene()
        {
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

            // 새 씬
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라 — 2D 정면 직교(스프라이트가 정면으로 보이게)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.96f, 0.93f, 0.87f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(3f, 3f, -10f);   // 정면(z 뒤에서)
            camGo.transform.rotation = Quaternion.identity;

            // 바닥 타일(2D 스프라이트로 카펫처럼 깔기) — floor 스프라이트 있으면 사용
            var floorSprite = LoadSprite("Assets/Art/Generated/bg/floor.png");
            var floorRoot = new GameObject("Floor").transform;
            for (int x = -1; x <= 7; x++)
                for (int y = -1; y <= 6; y++)
                {
                    var t = new GameObject($"floor_{x}_{y}");
                    t.transform.SetParent(floorRoot);
                    t.transform.position = new Vector3(x, y, 0f);
                    if (floorSprite != null)
                    {
                        var sr = t.AddComponent<SpriteRenderer>();
                        sr.sprite = floorSprite;
                        sr.sortingOrder = -10;
                    }
                }

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
            // 좌석을 카메라 화면 중앙 근처에 배치
            boot.seatSpacing = 2.2f;
            boot.seatsPerRow = 3;

            // 씬 저장
            const string dir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, $"{dir}/Cafe.unity");

            Selection.activeGameObject = root;
            Debug.Log("[SceneBuilder] M1 플레이 씬 생성 완료 → Assets/Scenes/Cafe.unity. Play 버튼으로 실행하세요.\n" +
                      "(상점 UI는 아직 없으니, 창가석 구매는 [GameRoot] 인스펙터에서 우클릭 → BuyWindowSeat() 로 테스트하거나 DevConsole(F1) 사용)");
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
            // 일반석 위주 + 좌석 타입 2~3개(창가석 제외: 상점 구매로 발견 유도)
            var seats = all.Where(f => f.type == FurnitureType.Seat && f.furnitureId != "furn_seat_window").ToList();
            var normal = seats.Where(f => f.furnitureId == "furn_seat_normal").ToList();
            var others = seats.Where(f => f.furnitureId != "furn_seat_normal").Take(2).ToList();
            var result = new List<FurnitureData>();
            result.AddRange(normal);
            result.AddRange(others);
            if (result.Count == 0) result = seats.Take(3).ToList();
            return result;
        }

        // 생성된 PNG(Assets/Art/Generated)에서 스프라이트를 찾아 데이터에 연결·저장
        static void LinkSprites(List<CatData> cats, List<FurnitureData> furn)
        {
            bool dirty = false;
            foreach (var c in cats)
            {
                if (c.worldSprite != null) continue;
                var s = LoadSprite($"Assets/Art/Generated/cats/{c.catId}.png");
                if (s != null) { c.worldSprite = s; EditorUtility.SetDirty(c); dirty = true; }
            }
            foreach (var f in furn)
            {
                if (f.worldSprite != null) continue;
                var s = LoadSprite($"Assets/Art/Generated/furniture/{f.furnitureId}.png");
                if (s != null) { f.worldSprite = s; EditorUtility.SetDirty(f); dirty = true; }
            }
            if (dirty) AssetDatabase.SaveAssets();
        }

        static Sprite LoadSprite(string path)
            => AssetDatabase.LoadAssetAtPath<Sprite>(path);

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
