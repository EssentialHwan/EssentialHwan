using System.IO;
using System.Linq;
using NUnit.Framework;
using YeogiCafe.Data;
using YeogiCafe.Progression;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 기억(사연)·옛 사장님의 노트 큐잉 규칙
    public class StoryBookTests
    {
        static CatSaveData Solved(string id)
        {
            var c = new CatSaveData { catId = id };
            c.observation.food.state = ObsState.Recorded;
            c.observation.seat.state = ObsState.Recorded;
            c.observation.facility.state = ObsState.Recorded;
            return c;
        }

        [Test]
        public void Memory_QueuedOnce_WhenAllThreeRecorded()
        {
            var s = new SaveGame { currentDay = 1 };
            s.cats.Add(Solved("cat_cheese"));
            s.cats.Add(new CatSaveData { catId = "cat_black" });
            StoryBook.QueueNight(s);
            Assert.AreEqual(1, s.storyQueue.Count(q => q.kind == StoryBook.KindMemory));
            Assert.IsTrue(s.cats[0].memorySeen);
            Assert.IsFalse(s.cats[1].memorySeen);
            s.storyQueue.Clear();
            StoryBook.QueueNight(s);
            Assert.AreEqual(0, s.storyQueue.Count(q => q.kind == StoryBook.KindMemory));
        }

        [Test]
        public void Memory_AtMostTwoPerNight()
        {
            var s = new SaveGame { currentDay = 1 };
            for (int i = 0; i < 5; i++) s.cats.Add(Solved("cat_" + i));
            StoryBook.QueueNight(s);
            Assert.AreEqual(StoryBook.MemoriesPerNight, s.storyQueue.Count(q => q.kind == StoryBook.KindMemory));
        }

        [Test]
        public void Notes_OnePerNight_InOrderOfReadiness()
        {
            var s = new SaveGame { currentDay = 1, cafeLevel = 3 };
            StoryBook.QueueNight(s);
            Assert.AreEqual(0, s.notesFound.Count, "1일차 밤엔 노트 없음");
            s.currentDay = 2;
            StoryBook.QueueNight(s);
            CollectionAssert.AreEqual(new[] { 1 }, s.notesFound);
            StoryBook.QueueNight(s);
            CollectionAssert.AreEqual(new[] { 1, 2 }, s.notesFound);
            StoryBook.QueueNight(s);                       // 3(첫 기억) 아직 → Lv3 다락 노트 4
            CollectionAssert.AreEqual(new[] { 1, 2, 4 }, s.notesFound);
            StoryBook.QueueNight(s);
            Assert.AreEqual(3, s.notesFound.Count, "남은 노트는 조건 전");
        }

        [Test]
        public void Note3_WaitsUntilNightAfterFirstMemory()
        {
            var s = new SaveGame { currentDay = 5 };
            s.notesFound.Add(1);
            s.cats.Add(Solved("cat_cheese"));
            StoryBook.QueueNight(s);                       // 기억은 오늘 밤, 노트 3은 다음 밤
            Assert.IsFalse(s.notesFound.Contains(3));
            StoryBook.QueueNight(s);
            Assert.IsTrue(s.notesFound.Contains(3));
        }

        [Test]
        public void EveryStoryKey_ExistsInCsv()
        {
            var csv = File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/strings.csv"));
            string[] cats = { "cat_cheese", "cat_calico", "cat_cow", "cat_black", "cat_tuxedo", "cat_mackerel", "cat_siamese", "cat_gray",
                              "cat_moon", "cat_fold", "cat_munchkin", "cat_persian", "cat_tortie", "cat_sphynx", "cat_kitten", "cat_senior" };
            foreach (var c in cats)
                foreach (var k in StoryBook.LineKeys(new StoryDTO { catId = c, kind = StoryBook.KindMemory }))
                    StringAssert.Contains("\n" + k + ",", csv, k);
            for (int n = 1; n <= StoryBook.NoteCount; n++)
            {
                StringAssert.Contains($"\nnote.{n}.where,", csv);
                foreach (var k in StoryBook.LineKeys(new StoryDTO { kind = StoryBook.KindNote, note = n }))
                    StringAssert.Contains("\n" + k + ",", csv, k);
            }
        }
    }
}
