using System;
using System.Linq;
using NUnit.Framework;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // T12/T13 보강 — 세이브 직렬화 계약. JsonUtility는 [Serializable] + public 필드만 저장한다.
    // 실제 파일 왕복(T12)은 PlayMode 필요하지만, "무엇이 저장되고 무엇이 안 되는가"는 구조로 보장.
    public class SaveContractTests
    {
        [Test]
        public void CoreSaveTypes_AreSerializable()
        {
            Assert.IsTrue(HasSerializable(typeof(SaveGame)));
            Assert.IsTrue(HasSerializable(typeof(CatSaveData)));
            Assert.IsTrue(HasSerializable(typeof(ObservationRecord)));
            Assert.IsTrue(HasSerializable(typeof(AxisRecord)));
            Assert.IsTrue(HasSerializable(typeof(ClueTally)));
        }

        [Test]
        public void ObservationRecord_IsSaved_ButTruthIsNot()
        {
            // 필수 저장: 관찰 기록
            Assert.IsNotNull(typeof(CatSaveData).GetField("observation"));
            // Truth 금지: 취향 필드가 세이브에 없어야
            Assert.IsNull(typeof(CatSaveData).GetField("seatPreference"));
            Assert.IsNull(typeof(CatSaveData).GetField("foodFavoriteMenuId"));
            Assert.IsNull(typeof(ObservationRecord).GetField("atmospherePreference"));
        }

        [Test]
        public void SatisfactionHistory_IsField_ButDerivedAreProperties()
        {
            // 단일 소스(P5): history는 필드(저장), last/avg는 프로퍼티(미저장)
            Assert.IsNotNull(typeof(CatSaveData).GetField("satisfactionHistory"));
            Assert.IsNull(typeof(CatSaveData).GetField("LastSatisfaction"), "파생값은 필드가 아니어야(미저장)");
            Assert.IsNotNull(typeof(CatSaveData).GetProperty("LastSatisfaction"));
            Assert.IsNotNull(typeof(CatSaveData).GetProperty("AvgSatisfaction"));
        }

        [Test]
        public void SatisfactionHistory_CapsAtMaxLength()
        {
            var cat = new CatSaveData { catId = "c" };
            for (int i = 0; i < 10; i++) cat.PushSatisfaction(i);
            Assert.LessOrEqual(cat.satisfactionHistory.Count, 5, "최근 N=5만 유지");
            Assert.AreEqual(9f, cat.LastSatisfaction, "마지막 값 유지");
        }

        static bool HasSerializable(Type t)
            => t.GetCustomAttributes(typeof(SerializableAttribute), false).Any();
    }
}
