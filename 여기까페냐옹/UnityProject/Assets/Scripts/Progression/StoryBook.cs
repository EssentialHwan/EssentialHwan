using System.Collections.Generic;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Progression
{
    // 이야기 묶음: 단골 편지 · 기억(사연) · 졸업 편지 · 옛 사장님의 노트.
    //  - 기억: 세 취향(음식·자리·시설)을 다 맞힌 고양이 → 뉴로냐 이전, 그 취향이 생긴 이야기(ep.<cat>.memory.01~03)
    //  - 노트: 이 가게의 원래 주인(사람)이 남긴 노트. 진행 조건을 채울 때마다 하루에 한 장씩(note.<n>.where / .01~03)
    //  - 졸업은 기억을 본 다음(GameFlowController).
    public static class StoryBook
    {
        public const int KindRegular2 = 2, KindRegular3 = 3, KindNote = 7, KindMemory = 8, KindGrad = 9;
        public const int NoteCount = 6;
        public const int MemoriesPerNight = 2;      // 예전 세이브에서 한꺼번에 쏟아지지 않게

        // 노트 n을 찾을 조건(번호가 낮은 것부터 하루 한 장)
        public static bool NoteReady(SaveGame s, int n)
        {
            if (s == null) return false;
            switch (n)
            {
                case 1: return s.currentDay >= 2;                                  // 둘째 날 마감: 주방 서랍
                case 2: return s.cafeLevel >= 2;                                   // 옆 공간을 텄을 때
                case 3: return s.cats.Exists(c => c != null && c.memorySeen);      // 첫 '기억'을 본 다음 날
                case 4: return s.cafeLevel >= 3;                                   // 다락
                case 5: return ShelterBook.TierOf(s.shelterDonated) >= 1;          // 보호소 후원
                case 6: return s.endingSeen;                                       // 모두의 단골집
                default: return false;
            }
        }

        // 하루 마감(승격·단골 판정 다음, 졸업 판정 전)에 부른다.
        public static void QueueNight(SaveGame s)
        {
            if (s == null) return;
            // 노트는 '어제까지' 채운 조건으로 찾는다(같은 밤에 기억 → 노트 3이 바로 붙지 않게 먼저 본다)
            int note = NextNote(s);
            int shown = 0;
            foreach (var c in s.cats)
            {
                if (c == null || c.memorySeen || c.observation.RecordedAxisCount() < 3) continue;
                if (shown >= MemoriesPerNight) break;
                c.memorySeen = true; shown++;
                s.storyQueue.Add(new StoryDTO { catId = c.catId, kind = KindMemory });
            }
            if (note > 0)
            {
                s.notesFound.Add(note);
                s.storyQueue.Add(new StoryDTO { kind = KindNote, note = note });
            }
        }

        public static int NextNote(SaveGame s)
        {
            // 첫 장(노트를 처음 발견)이 먼저. 나머지는 조건을 채운 낮은 번호부터.
            if (!s.notesFound.Contains(1)) return NoteReady(s, 1) ? 1 : 0;
            for (int n = 2; n <= NoteCount; n++)
                if (!s.notesFound.Contains(n) && NoteReady(s, n)) return n;
            return 0;
        }

        // 도감 '이야기' 탭: 지금까지 본 이야기(노트 → 고양이 순)
        public static List<StoryDTO> Seen(SaveGame s, IReadOnlyList<CatData> cats)
        {
            var list = new List<StoryDTO>();
            if (s == null) return list;
            var notes = new List<int>(s.notesFound); notes.Sort();
            foreach (var n in notes) list.Add(new StoryDTO { kind = KindNote, note = n });
            if (cats == null) return list;
            foreach (var d in cats)
            {
                if (d == null) continue;
                var c = s.cats.Find(x => x != null && x.catId == d.catId);
                if (c == null) continue;
                if (c.regularStage >= 2) list.Add(new StoryDTO { catId = c.catId, kind = KindRegular2 });
                if (c.regularStage >= 3) list.Add(new StoryDTO { catId = c.catId, kind = KindRegular3 });
                if (c.memorySeen) list.Add(new StoryDTO { catId = c.catId, kind = KindMemory });
                if (c.graduated) list.Add(new StoryDTO { catId = c.catId, kind = KindGrad });
            }
            return list;
        }

        public static string TitleKey(StoryDTO st) => st.kind switch
        {
            KindGrad => "story.title.grad",
            KindRegular3 => "story.title.r3",
            KindMemory => "story.title.memory",
            KindNote => "story.title.note",
            _ => "story.title.r2",
        };

        // 본문 줄 키
        public static IEnumerable<string> LineKeys(StoryDTO st)
        {
            switch (st.kind)
            {
                case KindNote: for (int i = 1; i <= 3; i++) yield return $"note.{st.note}.0{i}"; break;
                case KindMemory: for (int i = 1; i <= 3; i++) yield return $"ep.{st.catId}.memory.0{i}"; break;
                case KindGrad: for (int i = 1; i <= 3; i++) yield return $"ep.{st.catId}.grad.0{i}"; break;
                case KindRegular3: for (int i = 1; i <= 2; i++) yield return $"ep.{st.catId}.regular3.0{i}"; break;
                default: for (int i = 1; i <= 2; i++) yield return $"ep.{st.catId}.regular2.0{i}"; break;
            }
        }
    }
}
