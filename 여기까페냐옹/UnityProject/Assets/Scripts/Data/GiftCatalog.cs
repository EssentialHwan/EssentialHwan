using System.Collections.Generic;

namespace YeogiCafe.Data
{
    // 고양이 선물 목록 — 선물의 종류가 그 고양이 취향(음식/좌석/시설)의 힌트가 된다(네코아츠메 '선물' 참고).
    public static class GiftCatalog
    {
        public enum Shape { Bone, Leaf, Feather, Ball, Stone, Disc, Fluff, Bean, Ribbon, Berry, Tail, Candy, Stick }

        public struct Gift
        {
            public string id;          // gift.{id}.name / gift.{id}.desc
            public PrefAxis axis;
            public string target;      // menuId 또는 SeatTag/FacilityTag 이름(관찰 단서 대상과 같은 형식)
            public Shape shape;
            public int color;          // GiftDrawing 팔레트 인덱스
        }

        public static readonly Gift[] All =
        {
            // 음식(최애 메뉴)
            G("coffee_bean", PrefAxis.Food, "menu_americano", Shape.Bean, 0),
            G("milk_cap", PrefAxis.Food, "menu_milk", Shape.Disc, 1),
            G("fish_bone", PrefAxis.Food, "menu_fishcake", Shape.Bone, 1),
            G("berry_top", PrefAxis.Food, "menu_parfait", Shape.Berry, 2),
            G("tuna_lid", PrefAxis.Food, "menu_tuna_sand", Shape.Disc, 3),
            G("catnip_leaf", PrefAxis.Food, "menu_catnip_tea", Shape.Leaf, 4),
            G("caramel", PrefAxis.Food, "menu_pudding", Shape.Candy, 5),
            G("fish_tail", PrefAxis.Food, "menu_grilled_fish", Shape.Tail, 6),
            // 좌석 취향
            G("window_leaf", PrefAxis.Seat, "Window", Shape.Leaf, 7),
            G("dust_bunny", PrefAxis.Seat, "Corner", Shape.Fluff, 8),
            G("two_hairs", PrefAxis.Seat, "TwoSeat", Shape.Stick, 9),
            G("sofa_fluff", PrefAxis.Seat, "Sofa", Shape.Fluff, 10),
            G("bottle_cap", PrefAxis.Seat, "BarTable", Shape.Disc, 11),
            G("pebble", PrefAxis.Seat, "Quiet", Shape.Stone, 8),
            G("sparrow_feather", PrefAxis.Seat, "OutsideView", Shape.Feather, 12),
            G("wood_chip", PrefAxis.Seat, "Normal", Shape.Stick, 13),
            // 시설(놀이) 취향
            G("yarn_ball", PrefAxis.Facility, "Play", Shape.Ball, 2),
            G("feather", PrefAxis.Facility, "Height", Shape.Feather, 7),
            G("bouncy_ball", PrefAxis.Facility, "Active", Shape.Ball, 14),
            G("cotton", PrefAxis.Facility, "Soft", Shape.Fluff, 10),
            G("marble", PrefAxis.Facility, "Relax", Shape.Ball, 15),
            G("pinecone", PrefAxis.Facility, "Quiet", Shape.Stone, 13),
            G("ribbon", PrefAxis.Facility, "Social", Shape.Ribbon, 2),
            G("grass_blade", PrefAxis.Facility, "Natural", Shape.Leaf, 4),
        };

        static Gift G(string id, PrefAxis a, string target, Shape s, int c) => new Gift { id = id, axis = a, target = target, shape = s, color = c };

        public static bool TryGet(string id, out Gift g)
        {
            foreach (var x in All) if (x.id == id) { g = x; return true; }
            g = default; return false;
        }

        public static bool TryFor(PrefAxis axis, string target, out Gift g)
        {
            foreach (var x in All) if (x.axis == axis && x.target == target) { g = x; return true; }
            g = default; return false;
        }

        // 아직 기록하지 않은 축 중 하나의 '진짜 취향'을 가리키는 선물. 다 알아냈으면 null.
        public static string Pick(CatData cat, System.Func<PrefAxis, bool> recorded, System.Random rng)
        {
            var axes = new List<PrefAxis>();
            foreach (PrefAxis a in new[] { PrefAxis.Food, PrefAxis.Seat, PrefAxis.Facility })
                if (!recorded(a)) axes.Add(a);
            if (axes.Count == 0) return null;
            var axis = axes[rng.Next(axes.Count)];
            string target = axis == PrefAxis.Food ? cat.foodFavoriteMenuId
                          : axis == PrefAxis.Seat ? cat.seatPreference.ToString()
                          : cat.facilityPreference.ToString();
            return TryFor(axis, target, out var g) ? g.id : null;
        }
    }
}
