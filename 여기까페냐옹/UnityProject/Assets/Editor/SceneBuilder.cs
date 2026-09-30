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

            // 새 씬
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라 (탑다운 약간 기울임, 직교)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.96f, 0.93f, 0.87f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(2f, 8f, -6f);
            camGo.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

            // 조명
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 바닥
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = new Vector3(2f, 0f, 2f);
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            // GameRoot + S1Bootstrap
            var root = new GameObject("[GameRoot]");
            var boot = root.AddComponent<S1Bootstrap>();

            // 데이터 자동 연결
            boot.balance = balance;
            boot.cafeLevelConfig = levels;
            boot.starterMenus = menus;
            boot.allCats = cats;
            boot.allEvents = events;
            // 시작 큐: 치즈/삼색/젖소(있으면), 없으면 앞 3마리
            boot.starterCats = PickStarters(cats);
            // 시작 좌석: 일반석 + (있으면) 여분 좌석 몇 개
            boot.starterSeats = PickStarterSeats(furn);
            // 창가석(상점 구매 대상)
            boot.windowSeatData = furn.FirstOrDefault(f => f.furnitureId == "furn_seat_window");

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
