#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using YeogiCafe.Data;

namespace YeogiCafe.EditorTools
{
    // 메뉴 "여기까페냐옹/1.0 콘텐츠 에셋 생성"으로 8마리·8메뉴·18가구 SO를 자동 생성.
    // CONTENT_AUTHORING.md 데이터표와 일치. 반복 수작업 제거.
    public static class ContentGenerator
    {
        const string CatDir = "Assets/Data/Cats";
        const string MenuDir = "Assets/Data/Menus";
        const string FurnDir = "Assets/Data/Furniture";

        [MenuItem("여기까페냐옹/1.0 콘텐츠 에셋 생성")]
        public static void GenerateAll()
        {
            EnsureDir(CatDir); EnsureDir(MenuDir); EnsureDir(FurnDir);
            GenerateMenus();
            GenerateFurniture();
            GenerateCats();
            GenerateEvents();
            GenerateCafeLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ContentGenerator] 1.0 콘텐츠 에셋 생성 완료");
        }

        static void GenerateMenus()
        {
            // 기본 5종 Always, 확장 3종은 레벨 게이트(문서 06/32)
            CreateMenu("menu_americano", "아메리카노", 30, 8, 3f, new FoodTag[0], U(UnlockType.Always, 0));
            CreateMenu("menu_milk", "우유", 30, 10, 3f, new[] { FoodTag.Warm }, U(UnlockType.Always, 0));
            CreateMenu("menu_fishcake", "생선케이크", 60, 20, 5f, new[] { FoodTag.Fish, FoodTag.Sweet }, U(UnlockType.Always, 0));
            CreateMenu("menu_parfait", "딸기파르페", 70, 25, 6f, new[] { FoodTag.Fruit, FoodTag.Sweet }, U(UnlockType.Always, 0));
            CreateMenu("menu_tuna_sand", "참치샌드위치", 80, 30, 6f, new[] { FoodTag.Fish, FoodTag.Meal }, U(UnlockType.Always, 0));
            CreateMenu("menu_catnip_tea", "캣닢차", 40, 12, 4f, new[] { FoodTag.Warm }, U(UnlockType.CafeLevel, 2));
            CreateMenu("menu_pudding", "푸딩", 50, 18, 5f, new[] { FoodTag.Sweet }, U(UnlockType.CafeLevel, 2));
            CreateMenu("menu_grilled_fish", "구운생선", 90, 35, 8f, new[] { FoodTag.Fish, FoodTag.Meal, FoodTag.Warm }, U(UnlockType.CafeLevel, 3));
        }

        static void GenerateFurniture()
        {
            // 좌석 (문서 06 분위기 기여 반영)
            CreateSeat("furn_seat_normal", "일반석", 80, new[] { SeatTag.Normal }, A());
            CreateSeat("furn_seat_window", "창가석", 200, new[] { SeatTag.Window, SeatTag.Quiet, SeatTag.OutsideView },
                       A((AtmosphereAxis.Natural, 3), (AtmosphereAxis.Quiet, 2)));
            CreateSeat("furn_seat_corner", "구석석", 150, new[] { SeatTag.Corner, SeatTag.Quiet },
                       A((AtmosphereAxis.Quiet, 3)));
            CreateSeat("furn_seat_two", "2인석", 220, new[] { SeatTag.TwoSeat },
                       A((AtmosphereAxis.Lively, 2)));
            CreateSeat("furn_seat_sofa", "소파", 300, new[] { SeatTag.Sofa },
                       A((AtmosphereAxis.Warm, 3), (AtmosphereAxis.Luxury, 1)));
            CreateSeat("furn_seat_bar", "바테이블", 260, new[] { SeatTag.BarTable },
                       A((AtmosphereAxis.Luxury, 2)));
            // 시설
            CreateFacility("furn_fac_tower", "캣타워", 400, new[] { FacilityTag.Play, FacilityTag.Height, FacilityTag.Active },
                           A((AtmosphereAxis.Lively, 4)));
            CreateFacility("furn_fac_cushion", "쿠션", 200, new[] { FacilityTag.Soft, FacilityTag.Relax, FacilityTag.Quiet },
                           A((AtmosphereAxis.Warm, 3), (AtmosphereAxis.Quiet, 2)));
            CreateFacility("furn_fac_toybox", "장난감바구니", 350, new[] { FacilityTag.Play, FacilityTag.Social, FacilityTag.Active },
                           A((AtmosphereAxis.Lively, 3)));
            CreateFacility("furn_fac_scratch", "스크래처", 250, new[] { FacilityTag.Play, FacilityTag.Active },
                           A((AtmosphereAxis.Lively, 1)));
            CreateFacility("furn_fac_plant", "화분", 180, new[] { FacilityTag.Quiet, FacilityTag.Natural },
                           A((AtmosphereAxis.Natural, 3), (AtmosphereAxis.Quiet, 1)));
            CreateFacility("furn_fac_window", "창문", 150, new[] { FacilityTag.Natural },
                           A((AtmosphereAxis.Natural, 2)));
            // 장식 6종 (분위기 기여만)
            CreateDeco("furn_deco_clock", "벽시계", 80, A((AtmosphereAxis.Luxury, 1)));
            CreateDeco("furn_deco_frame", "액자", 60, A((AtmosphereAxis.Natural, 1)));
            CreateDeco("furn_deco_rug", "러그", 120, A((AtmosphereAxis.Warm, 2)));
            CreateDeco("furn_deco_lamp", "조명", 150, A((AtmosphereAxis.Warm, 1), (AtmosphereAxis.Luxury, 1)));
            CreateDeco("furn_deco_curtain", "커튼", 90, A((AtmosphereAxis.Quiet, 1)));
            CreateDeco("furn_deco_vase", "화병", 110, A((AtmosphereAxis.Natural, 2)));
        }

        // 분위기 기여 헬퍼
        static FurnitureData.AtmoContribution[] A(params (AtmosphereAxis, int)[] items)
        {
            var arr = new FurnitureData.AtmoContribution[items.Length];
            for (int i = 0; i < items.Length; i++)
                arr[i] = new FurnitureData.AtmoContribution { axis = items[i].Item1, value = items[i].Item2 };
            return arr;
        }

        static void GenerateCats()
        {
            // 해금 순서(출시스코프 §C): 1 시작 → 2·3 발견1 → 4·5 Lv2 → 6·7 Lv3 → 8 도감진척
            CreateCat("cat_cheese", "치즈냥", PersonalityTag.Relaxed, PersonalityTag.Glutton,
                      "menu_fishcake", SeatTag.Window, FacilityTag.Soft, AtmosphereAxis.Quiet, U(UnlockType.Always, 0));
            CreateCat("cat_calico", "삼색냥", PersonalityTag.Curious, PersonalityTag.Fickle,
                      "menu_parfait", SeatTag.Corner, FacilityTag.Play, AtmosphereAxis.Lively, U(UnlockType.DiscoveredCats, 1));
            CreateCat("cat_cow", "젖소냥", PersonalityTag.Social, PersonalityTag.Lonely,
                      "menu_tuna_sand", SeatTag.TwoSeat, FacilityTag.Social, AtmosphereAxis.Lively, U(UnlockType.DiscoveredCats, 1));
            CreateCat("cat_black", "까망냥", PersonalityTag.Introvert, PersonalityTag.Lonely,
                      "menu_milk", SeatTag.Corner, FacilityTag.Natural, AtmosphereAxis.Quiet, U(UnlockType.CafeLevel, 2));
            CreateCat("cat_tuxedo", "턱시도냥", PersonalityTag.Active, PersonalityTag.Playful,
                      "menu_americano", SeatTag.Sofa, FacilityTag.Play, AtmosphereAxis.Lively, U(UnlockType.CafeLevel, 2));
            CreateCat("cat_mackerel", "고등어냥", PersonalityTag.Glutton, PersonalityTag.Relaxed,
                      "menu_fishcake", SeatTag.Sofa, FacilityTag.Soft, AtmosphereAxis.Warm, U(UnlockType.CafeLevel, 3));
            CreateCat("cat_siamese", "샴냥", PersonalityTag.Fickle, PersonalityTag.Curious,
                      "menu_parfait", SeatTag.Window, FacilityTag.Play, AtmosphereAxis.Luxury, U(UnlockType.CafeLevel, 3));
            CreateCat("cat_gray", "회색냥", PersonalityTag.Lonely, PersonalityTag.Social,
                      "menu_tuna_sand", SeatTag.TwoSeat, FacilityTag.Social, AtmosphereAxis.Warm, U(UnlockType.RecordedAxesTotal, 6));
        }

        static UnlockCondition U(UnlockType t, int v) => new UnlockCondition { type = t, value = v };

        const string EventDir = "Assets/Data/Events";
        const string ConfigDir = "Assets/Data/Config";

        static void GenerateCafeLevels()
        {
            EnsureDir(ConfigDir);
            var so = GetOrCreate<CafeLevelConfig>($"{ConfigDir}/CafeLevels.asset");
            // 밸런스 문서 06: Lv2 800G(발견2), Lv3 2000G(3축 OR 가구5)
            so.levels = new[]
            {
                new CafeLevelConfig.LevelReq {
                    level = 2, goldCost = 800,
                    minDiscoveredCats = 2, minRecordedAxesTotal = 0, minRegulars = 0,
                    orAltFurnitureCount = 3
                },
                new CafeLevelConfig.LevelReq {
                    level = 3, goldCost = 2000,
                    minDiscoveredCats = 3, minRecordedAxesTotal = 3, minRegulars = 0,
                    orAltFurnitureCount = 5
                },
            };
            EditorUtility.SetDirty(so);
        }

        static void GenerateEvents()
        {
            EnsureDir(EventDir);
            // 문서 01: 단골 3단계 에피소드 8편 (고양이당 1, 로컬키 2줄 + 보상)
            CreateEpisode("ep_cheese", "cat_cheese", 50);
            CreateEpisode("ep_calico", "cat_calico", 50);
            CreateEpisode("ep_cow", "cat_cow", 60);
            CreateEpisode("ep_black", "cat_black", 50);
            CreateEpisode("ep_tuxedo", "cat_tuxedo", 60);
            CreateEpisode("ep_mackerel", "cat_mackerel", 60);
            CreateEpisode("ep_siamese", "cat_siamese", 70);
            CreateEpisode("ep_gray", "cat_gray", 70);
        }

        static void CreateEpisode(string epId, string catId, int gold)
        {
            var so = GetOrCreate<CatEventData>($"{EventDir}/{epId}.asset");
            so.eventId = epId; so.catId = catId;
            so.trigger = EventTrigger.RegularStage; so.triggerValue = 3;
            so.scriptLineKeys = new[] { $"ep.{catId}.regular3.01", $"ep.{catId}.regular3.02" };
            so.rewardGold = gold; so.rewardCatalogStamp = true;
            EditorUtility.SetDirty(so);

            // 관계 이벤트(친구=2단계 도달 시). 문서 01 복선 짝.
            CreateRelationEvents();
        }

        static bool relEventsDone;
        static void CreateRelationEvents()
        {
            if (relEventsDone) return; relEventsDone = true;
            CreateRelationEvent("rel_gray_cow", "cat_gray", "cat_cow", 100);
            CreateRelationEvent("rel_calico_tuxedo", "cat_calico", "cat_tuxedo", 100);
        }

        static void CreateRelationEvent(string id, string a, string b, int gold)
        {
            var so = GetOrCreate<CatEventData>($"{EventDir}/{id}.asset");
            so.eventId = id; so.catId = a; so.catIdB = b;
            so.trigger = EventTrigger.RelationStage; so.triggerValue = 2; // 친구
            so.scriptLineKeys = new[] { $"{id}.01", $"{id}.02" };
            so.rewardGold = gold; so.rewardCatalogStamp = true;
            EditorUtility.SetDirty(so);
        }

        // ── helpers ──
        static void CreateMenu(string id, string name, int price, int cost, float cook, FoodTag[] tags, UnlockCondition unlock)
        {
            var so = GetOrCreate<MenuData>($"{MenuDir}/{id}.asset");
            so.menuId = id; so.displayNameKey = name; so.price = price; so.cost = cost;
            so.cookTime = cook; so.foodTags = tags; so.unlockCondition = unlock;
            EditorUtility.SetDirty(so);
        }
        static void CreateSeat(string id, string name, int price, SeatTag[] tags, FurnitureData.AtmoContribution[] atmo)
        {
            var so = GetOrCreate<FurnitureData>($"{FurnDir}/{id}.asset");
            so.furnitureId = id; so.displayNameKey = name; so.type = FurnitureType.Seat;
            so.seatTags = tags; so.price = price; so.atmosphere = atmo;
            EditorUtility.SetDirty(so);
        }
        static void CreateFacility(string id, string name, int price, FacilityTag[] tags, FurnitureData.AtmoContribution[] atmo)
        {
            var so = GetOrCreate<FurnitureData>($"{FurnDir}/{id}.asset");
            so.furnitureId = id; so.displayNameKey = name; so.type = FurnitureType.Facility;
            so.facilityTags = tags; so.price = price; so.atmosphere = atmo;
            EditorUtility.SetDirty(so);
        }
        static void CreateDeco(string id, string name, int price, FurnitureData.AtmoContribution[] atmo)
        {
            var so = GetOrCreate<FurnitureData>($"{FurnDir}/{id}.asset");
            so.furnitureId = id; so.displayNameKey = name; so.type = FurnitureType.Decoration;
            so.price = price; so.atmosphere = atmo;
            EditorUtility.SetDirty(so);
        }
        static void CreateCat(string id, string name, PersonalityTag p1, PersonalityTag p2,
                              string fav, SeatTag seat, FacilityTag fac, AtmosphereAxis atmo,
                              UnlockCondition unlock)
        {
            var so = GetOrCreate<CatData>($"{CatDir}/{id}.asset");
            so.catId = id; so.displayNameKey = name;
            so.personalityTags = new[] { p1, p2 };
            so.foodFavoriteMenuId = fav; so.seatPreference = seat;
            so.facilityPreference = fac; so.atmospherePreference = atmo;
            so.unlockCondition = unlock;
            so.relationshipHintCatIds = RelationHints(id);
            EditorUtility.SetDirty(so);
        }

        // 관계 잠재 짝(문서 01): 회색냥↔젖소냥(쌍둥이 취향), 삼색냥↔턱시도냥(활발 콤비)
        static string[] RelationHints(string id) => id switch
        {
            "cat_gray" => new[] { "cat_cow" },
            "cat_cow" => new[] { "cat_gray" },
            "cat_calico" => new[] { "cat_tuxedo" },
            "cat_tuxedo" => new[] { "cat_calico" },
            _ => new string[0]
        };

        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        static void EnsureDir(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parent = System.IO.Path.GetDirectoryName(dir).Replace("\\", "/");
                var leaf = System.IO.Path.GetFileName(dir);
                if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }
}
#endif
