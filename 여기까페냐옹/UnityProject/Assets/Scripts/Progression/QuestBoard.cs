using System;
using System.Collections.Generic;
using YeogiCafe.AI;
using YeogiCafe.Core;
using YeogiCafe.Data;
using YeogiCafe.Save;

namespace YeogiCafe.Progression
{
    // 일일 의뢰 게시판(스타듀밸리 게시판 참고). 매일 아침 3개를 뽑고, GameEvents로 진행도를 센다.
    // 의뢰는 '만들어 둔 행동을 일부러 찾아보게' 하는 장치 — 지금 카페에서 실제로 볼 수 있는 것만 뽑는다.
    public class QuestBoard
    {
        public struct Template
        {
            public string id, eventId; public int goal, reward;
            public Func<SaveGame, IReadOnlyList<CatData>, bool> available;
        }

        static bool Any(IReadOnlyList<CatData> cats, Func<CatData, bool> f) { foreach (var c in cats) if (c != null && f(c)) return true; return false; }

        public static readonly Template[] Templates =
        {
            new Template { id = "q_petlove", eventId = GameEvents.PetLove, goal = 3, reward = 40, available = (s, c) => true },
            new Template { id = "q_serve", eventId = GameEvents.Serve, goal = 5, reward = 40, available = (s, c) => true },
            new Template { id = "q_zoomies", eventId = GameEvents.Zoomies, goal = 1, reward = 30, available = (s, c) => true },
            new Template { id = "q_rub", eventId = GameEvents.Rub, goal = 1, reward = 30, available = (s, c) => c.Count >= 2 },
            new Template { id = "q_groom", eventId = GameEvents.Groom, goal = 1, reward = 50, available = (s, c) => HasFriends(s) },
            new Template { id = "q_mindread", eventId = GameEvents.MindRead, goal = 1, reward = 50, available = (s, c) => HasRegular(s) },
            new Template { id = "q_gift", eventId = GameEvents.Gift, goal = 1, reward = 20, available = (s, c) => s.pendingGifts.Count > 0 },
            new Template { id = "q_fight", eventId = GameEvents.Fight, goal = 1, reward = 30, available = (s, c) => Any(c, CatTemperament.IsBoss) || Any(c, x => CatTemperament.Style(x) == TouchStyle.Fickle) },
            new Template { id = "q_ambush", eventId = GameEvents.Ambush, goal = 1, reward = 30, available = (s, c) => Any(c, CatTemperament.IsKid) },
            new Template { id = "q_steal", eventId = GameEvents.Steal, goal = 1, reward = 30, available = (s, c) => Any(c, CatTemperament.IsBoss) },
            new Template { id = "q_guess", eventId = GameEvents.GuessCorrect, goal = 1, reward = 60, available = (s, c) => true },
        };

        static bool HasRegular(SaveGame s) { foreach (var c in s.cats) if (c.regularStage >= 2) return true; return false; }
        static bool HasFriends(SaveGame s) { foreach (var r in s.relations) if (r.stage >= 2 || r.bond >= 2) return true; return false; }

        readonly Func<SaveGame> getSave;
        readonly Func<IReadOnlyList<CatData>> getCats;
        public Action<int> addGold;
        public Action<string> toast;
        public event Action OnChanged;
        readonly Random rng = new();

        public QuestBoard(Func<SaveGame> getSave, Func<IReadOnlyList<CatData>> getCats)
        {
            this.getSave = getSave; this.getCats = getCats;
            GameEvents.OnEvent += Handle;
        }

        public void Dispose() => GameEvents.OnEvent -= Handle;

        // 하루에 한 번 3개 뽑기(이미 오늘 뽑았으면 그대로)
        public void EnsureToday()
        {
            var s = getSave(); if (s == null) return;
            if (s.questDay == s.currentDay && s.quests.Count > 0) return;
            s.questDay = s.currentDay;
            s.quests.Clear();
            var pool = new List<Template>();
            var cats = getCats();
            foreach (var t in Templates) if (t.available(s, cats)) pool.Add(t);
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int k = rng.Next(pool.Count);
                var t = pool[k]; pool.RemoveAt(k);
                s.quests.Add(new QuestDTO { id = t.id, eventId = t.eventId, goal = t.goal, reward = t.reward });
            }
            OnChanged?.Invoke();
        }

        void Handle(string eventId, string catId)
        {
            var s = getSave(); if (s == null) return;
            foreach (var q in s.quests)
            {
                if (q.done || q.eventId != eventId) continue;
                q.progress++;
                if (q.progress >= q.goal)
                {
                    q.done = true;
                    addGold?.Invoke(q.reward);
                    toast?.Invoke(YeogiCafe.Loc.L.Get("quest.done", ("q", YeogiCafe.Loc.L.Get(q.id)), ("g", q.reward.ToString())));
                }
                OnChanged?.Invoke();
            }
        }
    }
}
