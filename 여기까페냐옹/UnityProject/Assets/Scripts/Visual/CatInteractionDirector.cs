using System;
using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.AI;
using YeogiCafe.Art;
using YeogiCafe.Core;
using YeogiCafe.Loc;
using YeogiCafe.Save;
using YeogiCafe.Data;

namespace YeogiCafe.Visual
{
    // 고양이 상호작용 연출 총괄.
    //  ① 클릭(쓰다듬기): 기질에 따라 애교/장난/킁킁/움찔/하악. 본 반응은 세이브(touchSeen)에 쌓여 도감 '손길' 칸이 채워진다.
    //     반응은 이번 방문 기분(TouchMood)으로 만족도·팁에 반영 → 누굴 만지면 좋은지 알아가는 재미.
    //  ② 고양이끼리: 가까이 쉬는 둘이 부비기·핥아주기·킁킁·놀이·하악·싸움. 관계 단계(친구)와 기질로 결정.
    public class CatInteractionDirector : MonoBehaviour
    {
        public CatManager cats;
        public SaveGame save;                        // 비어 있으면 relationships.save를 사용
        public YeogiCafe.Progression.RelationshipManager relationships;
        public Action<string, string> say;           // (catId, text) 말풍선
        public Action<string> toast;                 // 화면 위 알림
        public GameFlowController flow;              // 관찰 단서 기록(선물)

        [Header("고양이끼리")]
        public Vector2 socialInterval = new Vector2(7f, 13f);
        public float socialRangeX = 3.4f, socialRangeY = 1.7f;   // 이 안이면 바로 그 자리에서
        public float approachRange = 8f;                           // 이 안이면 한쪽이 걸어가서 어울림(카페 어디서든)

        // 다가가는 중인 쌍
        class Pending { public CatView from, to; public float timeout; }
        readonly List<Pending> pending = new();
        float nextSocial = 6f;

        readonly List<CatView> views = new();
        readonly System.Random rng = new();
        float R() => (float)rng.NextDouble();

        static CatView.Step S(string clip, float sec) => new CatView.Step { clip = clip, seconds = sec };
        static CatView.Step S(string clip, float sec, EmoteDrawing.Kind e) => new CatView.Step { clip = clip, seconds = sec, hasEmote = true, emote = e };

        public static bool Suspended;                 // 가구 배치 모드 등에서 일시 정지

        void OnEnable() { CatBrain.OnSeatStolen += PlaySteal; CatView.OnSuckle += PlaySuckle; }
        void OnDisable() { CatBrain.OnSeatStolen -= PlaySteal; CatView.OnSuckle -= PlaySuckle; }

        // 쭙쭙이 목격: 말풍선 + 도감 '버릇' 기록
        void PlaySuckle(CatView v)
        {
            if (v == null || v.Data == null) return;
            say?.Invoke(v.Data.catId, L.Get("narr.suckle"));
            if (save != null)
            {
                var cs = save.GetOrCreateCat(v.Data.catId);
                if (!cs.suckleSeen) { cs.suckleSeen = true; toast?.Invoke(L.Get("toast.suckle", ("name", L.Get($"cat.{v.Data.catId}.name")))); }
            }
            GameEvents.Raise(GameEvents.Suckle, v.Data.catId);
        }

        // 대장냥이: 쉬고 있던 고양이 자리에 와서 하악 → 상대는 움찔하고 비켜남
        void PlaySteal(CatBrain boss, CatBrain victim)
        {
            if (boss == null || victim == null) return;
            var bv = boss.GetComponent<CatView>(); var vv = victim.GetComponent<CatView>();
            if (bv == null || vv == null) return;
            string bossName = L.Get($"cat.{boss.data.catId}.name"), victimName = L.Get($"cat.{victim.data.catId}.name");
            if ((boss.transform.position - victim.transform.position).sqrMagnitude < 2.5f)
            {
                int dir = victim.transform.position.x >= boss.transform.position.x ? 1 : -1;
                bv.DoAction(new[] { S("hiss", 1.5f, EmoteDrawing.Kind.Anger) }, null, dir);
                var aside = CafeLayout.ClampFloor(victim.transform.position + new Vector3(dir * 0.7f, -0.25f, 0f));
                vv.DoAction(new[] { S("flinch", 1.3f, EmoteDrawing.Kind.Sweat) }, aside, -dir);
            }
            say?.Invoke(victim.data.catId, L.Get("social.steal", ("boss", bossName), ("name", victimName)));
            if (save != null) save.GetOrCreateCat(boss.data.catId).bossSeen = true;
            GameEvents.Raise(GameEvents.Steal, boss.data.catId);
        }

        void Update()
        {
            if (cats == null || Suspended) return;
            if (save == null && relationships != null) save = relationships.save;
            TickGifts();
            if (Input.GetMouseButtonDown(0)) TryClick();
            TickPending();
            TickZoomStartle();
            TickHungry();
            nextSocial -= Time.deltaTime;
            if (nextSocial <= 0f)
            {
                nextSocial = Mathf.Lerp(socialInterval.x, socialInterval.y, R());
                TrySocial();
            }
        }

        void Collect()
        {
            views.Clear();
            foreach (var b in cats.Active)
            {
                if (b == null) continue;
                var v = b.GetComponent<CatView>();
                if (v != null && v.Data != null) views.Add(v);
            }
        }

        // ───────── ① 쓰다듬기 ─────────
        void TryClick()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.IsPointerOverGameObject()) return;     // UI 위 클릭은 무시
            var cam = Camera.main; if (cam == null) return;
            Vector3 w = cam.ScreenToWorldPoint(Input.mousePosition); w.z = 0f;

            if (MouseRaid.TryCatch(w)) return;                          // 쥐 잡기가 쓰다듬기보다 먼저
            if (TryPickGift(w)) return;
            Collect();
            CatView hit = null; int best = int.MinValue;
            foreach (var v in views)
            {
                var r = v.Renderer; if (r == null || r.sprite == null) continue;
                var bnd = r.bounds;
                // 스프라이트 사각형 중 고양이 몸이 있는 가운데 영역만(여백 클릭 방지)
                bnd.Expand(new Vector3(-bnd.size.x * 0.25f, -bnd.size.y * 0.2f, 0f));
                if (!bnd.Contains(new Vector3(w.x, w.y, bnd.center.z))) continue;
                if (r.sortingOrder > best) { best = r.sortingOrder; hit = v; }
            }
            if (hit != null) Pet(hit);
        }

        public void Pet(CatView v)
        {
            if (v == null || !v.CanTouch) return;
            var style = CatTemperament.Style(v.Data);
            var cs = save != null ? save.GetOrCreateCat(v.Data.catId) : null;
            int streak = v.RegisterPet();
            var res = CatTemperament.React(style, streak, cs != null ? cs.regularStage : 1, R());

            CatView.Step[] seq; string line;
            switch (res)
            {
                case TouchResult.Love:
                    if (streak >= 3) { seq = new[] { S("purr", 2.4f, EmoteDrawing.Kind.Heart) }; line = "touch.say.love"; }
                    else if (style == TouchStyle.Affectionate && streak == 1)
                    { seq = new[] { S("rollover", 2.6f, EmoteDrawing.Kind.Heart) }; line = "touch.say.rollover"; }
                    else { seq = new[] { S("rub", 2.2f, EmoteDrawing.Kind.Heart) }; line = "touch.say.love"; }
                    break;
                case TouchResult.Play:  seq = new[] { S("pounce", 1.8f, EmoteDrawing.Kind.Exclaim) }; line = "touch.say.play"; break;
                case TouchResult.Sniff: seq = new[] { S("sniff", 1.8f, EmoteDrawing.Kind.Question) }; line = "touch.say.sniff"; break;
                case TouchResult.Flinch: seq = new[] { S("flinch", 1.8f, EmoteDrawing.Kind.Sweat) }; line = "touch.say.flinch"; break;
                default:
                    if (style == TouchStyle.Fickle) { seq = new[] { S("swat", 1.0f, EmoteDrawing.Kind.Anger), S("hiss", 0.8f) }; line = "touch.say.swat"; }
                    else { seq = new[] { S("hiss", 1.8f, EmoteDrawing.Kind.Anger) }; line = "touch.say.hiss"; }
                    break;
            }
            v.DoAction(seq, null, 0);
            v.Brain.AddTouchMood(CatTemperament.MoodDelta(res));
            GameEvents.Raise(GameEvents.Pet, v.Data.catId);
            if (res == TouchResult.Love) GameEvents.Raise(GameEvents.PetLove, v.Data.catId);

            // 관찰 기록 → 새로 알아낸 성향이면 그 문장을, 아니면 반응 대사를 말풍선으로
            string text = L.Get(line);
            if (cs != null)
            {
                if (cs.touchSeen == null || cs.touchSeen.Length < CatTemperament.ResultCount) cs.touchSeen = new int[CatTemperament.ResultCount];
                string before = CatTemperament.ObservedKey(cs.touchSeen);
                cs.touchSeen[(int)res]++;
                string after = CatTemperament.ObservedKey(cs.touchSeen);
                if (after != null && after != before)
                    text = L.Get(after.Replace("touch.", "touch.learn."), ("name", L.Get($"cat.{v.Data.catId}.name")));
            }
            say?.Invoke(v.Data.catId, text);
        }

        // ───────── 밥 달라고 야옹 ─────────
        float hungryTimer;
        void TickHungry()
        {
            hungryTimer -= Time.deltaTime;
            if (hungryTimer > 0f) return;
            hungryTimer = 3.5f;
            Collect();
            foreach (var v in views)
            {
                if (v.Brain.State != CatState.WaitFood || v.Brain.OrderPatienceRatio >= 0.5f) continue;
                if (R() < 0.6f) say?.Invoke(v.Data.catId, YeogiCafe.Food.DeliveryCat.IsDelivery(v.Data.catId)
                    ? L.Get("delivery.hurry") : L.Get("meme.hungry." + (rng.Next(3) + 1)));
            }
        }

        // ───────── 선물(문 앞) ─────────
        readonly List<(GameObject go, GiftDTO dto)> giftObjs = new();
        float giftCheck;

        void TickGifts()
        {
            giftCheck -= Time.deltaTime;
            for (int i = 0; i < giftObjs.Count; i++)   // 반짝반짝 살짝 통통
            {
                var g = giftObjs[i].go; if (g == null) continue;
                float t = Time.time * 3f + i;
                g.transform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(t)) * 0.08f);
            }
            if (giftCheck > 0f || save == null) return;
            giftCheck = 1f;
            int slot = giftObjs.Count;
            foreach (var dto in save.pendingGifts)
            {
                if (dto.day >= save.currentDay) continue;                       // 받은 다음 날 아침부터
                bool shown = false;
                foreach (var p in giftObjs) if (p.dto == dto) shown = true;
                if (shown) continue;
                var go = new GameObject("Gift_" + dto.giftId);
                go.transform.position = new Vector3(CafeLayout.Door.x - 0.5f + slot * 0.45f, CafeLayout.FloorTop - 0.55f - (slot % 2) * 0.25f, 0f);
                var srd = go.AddComponent<SpriteRenderer>();
                srd.sprite = RuntimeSprites.Get("gift_" + dto.giftId, () => GiftDrawing.Draw(dto.giftId));
                srd.sortingOrder = YSort.Order(go.transform.position.y) + 2;
                giftObjs.Add((go, dto));
                slot++;
            }
        }

        bool TryPickGift(Vector3 w)
        {
            for (int i = 0; i < giftObjs.Count; i++)
            {
                var (go, dto) = giftObjs[i];
                if (go == null) continue;
                if ((go.transform.position + new Vector3(0, 0.25f, 0) - w).sqrMagnitude > 0.16f) continue;
                Destroy(go); giftObjs.RemoveAt(i);
                save.pendingGifts.Remove(dto);
                if (!save.giftsCollected.Contains(dto.giftId)) save.giftsCollected.Add(dto.giftId);
                if (GiftCatalog.TryGet(dto.giftId, out var g) && flow != null && flow.Observation != null)
                {
                    var type = g.axis == PrefAxis.Food ? ClueType.FoodEat : g.axis == PrefAxis.Seat ? ClueType.SeatUse : ClueType.FacilityUse;
                    flow.Observation.RecordClue(dto.catId, type, g.target, false, null);      // 선물은 진짜 취향을 가리키는 단서
                }
                string catName = L.Get($"cat.{dto.catId}.name");
                toast?.Invoke(L.Get("gift.found", ("name", catName), ("gift", L.Get($"gift.{dto.giftId}.name")))
                              + "\n" + L.Get($"gift.{dto.giftId}.desc"));
                GameEvents.Raise(GameEvents.Gift, dto.catId);
                return true;
            }
            return false;
        }

        // ───────── ② 고양이끼리 ─────────
        void TrySocial()
        {
            if (pending.Count > 0) return;
            Collect();
            var near = new List<(CatView a, CatView b)>();
            var far = new List<(CatView a, CatView b)>();
            for (int i = 0; i < views.Count; i++)
                for (int j = i + 1; j < views.Count; j++)
                {
                    var a = views[i]; var b = views[j];
                    if (!a.CanSocialize || !b.CanSocialize) continue;
                    Vector3 d = b.SeatPos - a.SeatPos;
                    float ax = Mathf.Abs(d.x), ay = Mathf.Abs(d.y);
                    if (ax >= 0.6f && ax <= socialRangeX && ay <= socialRangeY) near.Add((a, b));
                    else if (d.magnitude <= approachRange) far.Add((a, b));
                }

            if (near.Count > 0 && (far.Count == 0 || R() < 0.6f))
            {
                var (x, y) = near[rng.Next(near.Count)];
                Begin(x, y);
                return;
            }
            if (far.Count == 0) return;

            // 멀리 있는 둘: 더 활발한 쪽이 상대 옆으로 걸어간다
            var (p, q) = far[rng.Next(far.Count)];
            var from = Eagerness(p) >= Eagerness(q) ? p : q;
            var to = from == p ? q : p;
            float side = from.SeatPos.x <= to.SeatPos.x ? -1f : 1f;
            var spot = Core.CafeLayout.ClampFloor(to.SeatPos + new Vector3(side * 0.95f, -0.05f, 0f));
            if (Mathf.Abs(spot.x - to.SeatPos.x) < 0.6f)                       // 벽에 막히면 반대편
                spot = Core.CafeLayout.ClampFloor(to.SeatPos + new Vector3(-side * 0.95f, -0.05f, 0f));
            if (from.Brain.RequestApproach(spot)) pending.Add(new Pending { from = from, to = to, timeout = 9f });
        }

        // 우다다 하는 고양이가 옆을 휙 지나가면 쉬던 고양이가 깜짝(가끔 하악)
        float startleTimer;
        void TickZoomStartle()
        {
            startleTimer -= Time.deltaTime;
            if (startleTimer > 0f) return;
            startleTimer = 0.25f;
            Collect();
            foreach (var z in views)
            {
                if (!z.Brain.IsZooming || !z.Brain.IsMoving) continue;
                foreach (var v in views)
                {
                    if (v == z || !v.CanSocialize) continue;
                    if ((v.SeatPos - z.SeatPos).sqrMagnitude > 0.5f) continue;
                    var st = CatTemperament.Style(v.Data);
                    int face = z.SeatPos.x >= v.SeatPos.x ? 1 : -1;
                    if (st == TouchStyle.Grumpy || (st == TouchStyle.Fickle && R() < 0.5f))
                        v.DoAction(new[] { S("hiss", 1.2f, EmoteDrawing.Kind.Anger) }, null, face);
                    else
                        v.DoAction(new[] { S("flinch", 0.9f, EmoteDrawing.Kind.Exclaim) }, null, face);
                }
            }
        }

        float Eagerness(CatView v)
        {
            var st = CatTemperament.Style(v.Data);
            return st switch
            {
                TouchStyle.Playful => 3f, TouchStyle.Affectionate => 2.5f, TouchStyle.Curious => 2f,
                TouchStyle.Fickle => 1.5f, TouchStyle.Grumpy => 1f, _ => 0.5f
            } + R();
        }

        void TickPending()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                p.timeout -= Time.deltaTime;
                bool gone = p.from == null || p.to == null || p.from.IsExiting || p.to.IsExiting;
                if (gone || p.timeout <= 0f) { pending.RemoveAt(i); continue; }
                if (p.from.Brain.IsMoving) continue;
                if (!p.from.CanSocialize || !p.to.CanSocialize) continue;      // 앉는 중 등 잠깐 기다림
                pending.RemoveAt(i);
                Begin(p.from, p.to);
            }
        }

        void Begin(CatView x, CatView y)
        {
            if (x.SeatPos.x > y.SeatPos.x) (x, y) = (y, x);           // x = 왼쪽, y = 오른쪽
            int stage = RelationStage(x.Data.catId, y.Data.catId);
            var sx = CatTemperament.Style(x.Data); var sy = CatTemperament.Style(y.Data);
            var kind = CatTemperament.Pair(sx, sy, stage, R());
            Play(kind, x, y, sx, sy);
        }

        // QA용: 자리에 있는 아무 둘에게 지정한 상호작용을 강제로 재생(거리·상태 무시)
        public bool DebugForce(SocialKind kind)
        {
            Collect();
            CatView x = null, y = null;
            foreach (var v in views)
            {
                if (!v.IsSeatedNow || v.IsActing) continue;
                if (x == null) x = v; else { y = v; break; }
            }
            if (x == null || y == null) return false;
            if (x.SeatPos.x > y.SeatPos.x) (x, y) = (y, x);
            Play(kind, x, y, CatTemperament.Style(x.Data), CatTemperament.Style(y.Data));
            return true;
        }

        public void DebugZoomies()
        {
            Collect();
            foreach (var v in views) if (v.Brain.State == YeogiCafe.Data.CatState.Idle && !v.Brain.IsMoving) { v.Brain.StartZoomies(); return; }
        }

        int RelationStage(string a, string b)
        {
            if (save == null || save.relations == null) return 0;
            string p = string.CompareOrdinal(a, b) <= 0 ? a : b, q = string.CompareOrdinal(a, b) <= 0 ? b : a;
            foreach (var r in save.relations) if (r.catA == p && r.catB == q) return r.stage;
            return 0;
        }

        void Play(SocialKind kind, CatView left, CatView right, TouchStyle sl, TouchStyle sr)
        {
            // 관계도용 기록 + 의뢰 이벤트
            if (save != null)
            {
                var rel = save.GetOrCreateRelation(left.Data.catId, right.Data.catId);
                if (kind == SocialKind.Rub || kind == SocialKind.Groom || kind == SocialKind.Play) rel.bond++;
                else if (kind == SocialKind.Fight) { rel.bond--; rel.fights++; }
                else if (kind == SocialKind.Hiss) rel.bond--;
            }
            string ev = kind switch
            {
                SocialKind.Rub => GameEvents.Rub, SocialKind.Groom => GameEvents.Groom,
                SocialKind.Fight => GameEvents.Fight, SocialKind.Ambush => GameEvents.Ambush, _ => null
            };
            if (ev != null) GameEvents.Raise(ev, left.Data.catId);

            // 둘 사이 바닥에서 마주 본다
            Vector3 mid = (left.SeatPos + right.SeatPos) * 0.5f;
            float gap = kind == SocialKind.Hiss || kind == SocialKind.Fight ? 0.62f : 0.42f;
            Vector3 ml = mid + Vector3.left * gap, mr = mid + Vector3.right * gap;
            ml.y = mr.y = Mathf.Min(left.SeatPos.y, right.SeatPos.y) - 0.05f;

            switch (kind)
            {
                case SocialKind.Rub:
                    left.DoAction(new[] { S("rub", 3.0f, EmoteDrawing.Kind.Heart) }, ml, +1);
                    right.DoAction(new[] { S("rub", 3.0f, EmoteDrawing.Kind.Heart) }, mr, -1);
                    say?.Invoke(left.Data.catId, L.Get("social.rub"));
                    break;
                case SocialKind.Groom:
                    left.DoAction(new[] { S("lick", 3.2f, EmoteDrawing.Kind.Heart) }, ml, +1);
                    right.DoAction(new[] { S("purr", 3.2f, EmoteDrawing.Kind.Note) }, mr, -1);
                    say?.Invoke(left.Data.catId, L.Get("social.groom"));
                    break;
                case SocialKind.Play:
                    left.DoAction(new[] { S("pounce", 1.4f, EmoteDrawing.Kind.Exclaim), S("pounce", 1.4f) }, ml, +1);
                    right.DoAction(new[] { S("pounce", 1.4f), S("rollover", 1.4f, EmoteDrawing.Kind.Note) }, mr, -1);
                    say?.Invoke(right.Data.catId, L.Get("social.play"));
                    break;
                case SocialKind.Hiss:
                {
                    // 까칠한 쪽이 하악, 상대는 움찔
                    bool leftAngry = sl == TouchStyle.Grumpy || (sr != TouchStyle.Grumpy && sl == TouchStyle.Fickle);
                    var angry = leftAngry ? left : right; var other = leftAngry ? right : left;
                    angry.DoAction(new[] { S("hiss", 2.2f, EmoteDrawing.Kind.Anger) }, null, leftAngry ? +1 : -1);
                    other.DoAction(new[] { S("flinch", 2.2f, EmoteDrawing.Kind.Sweat) }, null, leftAngry ? -1 : +1);
                    say?.Invoke(angry.Data.catId, L.Get("social.hiss"));
                    break;
                }
                case SocialKind.Fight:
                    left.DoAction(new[] { S("hiss", 0.9f, EmoteDrawing.Kind.Anger), S("swat", 1.8f), S("hiss", 0.6f) }, ml, +1);
                    right.DoAction(new[] { S("hiss", 0.9f, EmoteDrawing.Kind.Anger), S("swat", 1.8f), S("flinch", 0.6f, EmoteDrawing.Kind.Sweat) }, mr, -1);
                    SpawnFightCloud(mid + new Vector3(0f, 0.45f, 0f), 0.45f + 0.9f, 1.8f, Mathf.Min(left.SeatPos.y, right.SeatPos.y));
                    say?.Invoke(left.Data.catId, L.Get("social.fight"));
                    break;
                case SocialKind.Ambush:
                {
                    // 캣초딩이 엉덩이 씰룩 → 덮침. 상대 반응은 기질대로(까칠·변덕은 반격, 장난은 같이 놀기, 나머지는 깜짝)
                    bool leftKid = sl == TouchStyle.Playful;
                    var kid = leftKid ? left : right; var other = leftKid ? right : left;
                    var so = leftKid ? sr : sl;
                    int kf = leftKid ? +1 : -1;
                    kid.DoAction(new[] { S("pounce", 1.3f, EmoteDrawing.Kind.Exclaim), S("swat", 0.8f) }, leftKid ? ml : mr, kf);
                    if (so == TouchStyle.Grumpy || so == TouchStyle.Fickle)
                    {
                        other.DoAction(new[] { S("flinch", 1.2f, EmoteDrawing.Kind.Exclaim), S("swat", 1.0f, EmoteDrawing.Kind.Anger) }, leftKid ? mr : ml, -kf);
                        say?.Invoke(other.Data.catId, L.Get("social.fight"));
                    }
                    else if (so == TouchStyle.Playful)
                        other.DoAction(new[] { S("flinch", 1.0f, EmoteDrawing.Kind.Exclaim), S("pounce", 1.2f, EmoteDrawing.Kind.Note) }, leftKid ? mr : ml, -kf);
                    else
                        other.DoAction(new[] { S("flinch", 2.0f, EmoteDrawing.Kind.Sweat) }, leftKid ? mr : ml, -kf);
                    say?.Invoke(kid.Data.catId, L.Get("social.ambush"));
                    break;
                }
                default:
                    left.DoAction(new[] { S("sniff", 2.2f, EmoteDrawing.Kind.Question) }, ml, +1);
                    right.DoAction(new[] { S("sniff", 2.2f) }, mr, -1);
                    break;
            }
        }

        // 싸움 먼지구름: 잠깐 뒤에 나타나 들썩이다 사라짐
        void SpawnFightCloud(Vector3 at, float delay, float life, float sortY)
        {
            var go = new GameObject("FightCloud");
            go.transform.position = at;
            var srd = go.AddComponent<SpriteRenderer>();
            srd.sprite = RuntimeSprites.Get("emote_Fight", () => EmoteDrawing.Draw(EmoteDrawing.Kind.Fight));
            srd.sortingOrder = YSort.Order(sortY - 0.2f) + YSort.CatAboveSeat + 3;
            srd.enabled = false;
            go.AddComponent<FightCloud>().Init(delay, life);
        }

        class FightCloud : MonoBehaviour
        {
            float delay, life, t; SpriteRenderer srd; Vector3 basePos;
            public void Init(float d, float l) { delay = d; life = l; srd = GetComponent<SpriteRenderer>(); basePos = transform.position; }
            void Update()
            {
                t += Time.deltaTime;
                if (t < delay) return;
                float k = t - delay;
                if (k > life) { Destroy(gameObject); return; }
                srd.enabled = true;
                float j = Mathf.Sin(k * 40f) * 0.04f;
                transform.position = basePos + new Vector3(j, Mathf.Abs(Mathf.Sin(k * 23f)) * 0.06f, 0f);
                transform.localScale = Vector3.one * (2.0f + Mathf.Sin(k * 31f) * 0.15f);
            }
        }
    }
}
