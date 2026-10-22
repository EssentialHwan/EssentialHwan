using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Cafe;
using YeogiCafe.Core;
using YeogiCafe.Data;
using YeogiCafe.Economy;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 상점 구매→배치→관찰 채널 개방 배선 검증.
    // 가구 구매가 실제로 CafeContext에 좌석/시설을 등록해 CatBrain 후보가 되는지, 골드가 차감되는지.
    public class ShopFlowTests
    {
        BalanceConfig cfg;
        CafeContext cafe;
        EconomyManager economy;
        FurnitureManager furniture;

        [SetUp]
        public void Setup()
        {
            cfg = ScriptableObject.CreateInstance<BalanceConfig>();
            cfg.startGold = 300;

            var root = new GameObject("root");
            cafe = root.AddComponent<CafeContext>();
            economy = root.AddComponent<EconomyManager>();
            economy.cfg = cfg;
            economy.Init();

            furniture = root.AddComponent<FurnitureManager>();
            furniture.cafe = cafe;
            furniture.economy = economy;
            furniture.save = new SaveGame();

            // 배치 슬롯 1개
            var slotGo = new GameObject("slot");
            furniture.slots.Add(new FurnitureManager.SlotEntry { slot = slotGo.transform });
        }

        static FurnitureData Seat(string id, int price)
        {
            var f = ScriptableObject.CreateInstance<FurnitureData>();
            f.furnitureId = id;
            f.displayNameKey = "furn." + id;
            f.type = FurnitureType.Seat;
            f.seatTags = new[] { SeatTag.Window };
            f.price = price;
            return f;
        }

        [Test]
        public void Buy_Seat_RegistersInCafe_AndSpendsGold()
        {
            Assert.AreEqual(0, cafe.SeatCount, "구매 전에는 좌석 없음");
            int before = economy.Gold;

            bool ok = furniture.BuyAndPlace(Seat("furn_seat_window", 100));

            Assert.IsTrue(ok, "잔액·슬롯 충분하면 구매 성공");
            Assert.AreEqual(1, cafe.SeatCount, "구매한 좌석이 CafeContext에 등록되어 후보가 됨");
            Assert.AreEqual(before - 100, economy.Gold, "가격만큼 골드 차감");
        }

        [Test]
        public void Buy_Fails_WhenInsufficientGold()
        {
            bool ok = furniture.BuyAndPlace(Seat("furn_seat_window", 9999));
            Assert.IsFalse(ok, "잔액 부족 시 구매 실패");
            Assert.AreEqual(0, cafe.SeatCount, "실패 시 좌석 등록되지 않음");
            Assert.AreEqual(300, economy.Gold, "실패 시 골드 미차감");
        }

        [Test]
        public void Buy_Fails_WhenNoFreeSlot()
        {
            // 바닥 가구는 정해 둔 칸이 차면 빈 바닥을 찾아 놓는다 → '공간 부족'은 정해 둔 칸만 쓰는 벽 가구로 검사.
            furniture.slots.Clear();
            var wallGo = new GameObject("wallslot");
            furniture.slots.Add(new FurnitureManager.SlotEntry { slot = wallGo.transform, kind = FurnitureManager.SlotKind.Wall });
            Assert.IsTrue(furniture.BuyAndPlace(Wall("furn_deco_frame", 10)));
            int gold = economy.Gold;
            bool second = furniture.BuyAndPlace(Wall("furn_deco_clock", 10));
            Assert.IsFalse(second, "빈 벽 칸이 없으면 구매 실패");
            Assert.AreEqual(FurnitureManager.FailReason.NoSpace, furniture.LastFail);
            Assert.AreEqual(gold, economy.Gold, "실패하면 골드는 안 빠진다");
        }

        static FurnitureData Wall(string id, int price)
        {
            var f = ScriptableObject.CreateInstance<FurnitureData>();
            f.furnitureId = id;
            f.displayNameKey = "furn." + id;
            f.type = FurnitureType.Decoration;
            f.wallMounted = true;
            f.price = price;
            return f;
        }
    }
}
