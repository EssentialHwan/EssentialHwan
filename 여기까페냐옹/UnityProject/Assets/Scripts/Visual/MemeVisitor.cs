using UnityEngine;
using YeogiCafe.Art;
using YeogiCafe.Core;

namespace YeogiCafe.Visual
{
    // 밈 고양이 손님(주문 안 함, 잠깐 놀러 옴). 문으로 들어와 카페 가운데에서 한바탕 하고 나간다.
    //  빙글냥: "우이이아이우~" 하며 빙글빙글 → 카페 고양이들이 다 같이 따라 돈다
    //  방방냥: 제자리에서 방방 뛴다 → 다들 따라 방방
    public class MemeVisitor : MonoBehaviour
    {
        public enum Kind { Spin, Bounce }
        public Kind kind;
        public string Id => kind == Kind.Spin ? "cat_spin" : "cat_bounce";
        public Transform Anchor => visual;
        public System.Action onShowtime;            // 가운데 도착해 시작할 때(디렉터가 다른 고양이들을 합류시킴)

        Transform visual; SpriteRenderer sr;
        Vector3 target, door;
        enum Phase { In, Show, Out }
        Phase phase; float t;
        string clip = ""; Sprite[] frames; CatClips.Def def; float clipT;
        const float Speed = 1.8f, ShowTime = 6.5f;

        public void Init(Kind k, Vector3 doorPos, Vector3 stage)
        {
            kind = k; door = doorPos; target = stage;
            transform.position = doorPos;
            visual = new GameObject("Visual").transform; visual.SetParent(transform, false);
            sr = visual.gameObject.AddComponent<SpriteRenderer>();
            Play("walk");
        }

        void Play(string name)
        {
            if (clip == name) return;
            clip = name; clipT = 0f; def = CatClips.Find(name); frames = CatSpriteBank.Clip(Id, name);
        }

        void Update()
        {
            float dt = Time.deltaTime; t += dt; clipT += dt;
            switch (phase)
            {
                case Phase.In:
                    if (Move(target, dt)) { phase = Phase.Show; t = 0f; onShowtime?.Invoke(); }
                    break;
                case Phase.Show:
                    if (kind == Kind.Spin) { Play("stand"); Spin(visual, sr, t); }
                    else { Play("stand"); visual.localPosition = new Vector3(0, Bounce(t), 0); visual.localScale = Squash(t); }
                    if (t > ShowTime) { phase = Phase.Out; visual.localScale = Vector3.one; visual.localPosition = Vector3.zero; Play("walk"); }
                    break;
                case Phase.Out:
                    if (Move(door, dt)) { Destroy(gameObject); return; }
                    break;
            }
            if (frames != null && frames.Length > 0)
            {
                int i = (int)(clipT * def.fps);
                sr.sprite = frames[def.loop ? i % frames.Length : Mathf.Min(i, frames.Length - 1)];
            }
            sr.sortingOrder = YSort.Order(transform.position.y) + YSort.CatAboveSeat;
        }

        bool Move(Vector3 to, float dt)
        {
            Play("walk");
            var d = to - transform.position;
            if (d.magnitude < 0.05f) return true;
            if (Mathf.Abs(d.x) > 0.01f) sr.flipX = d.x < 0;
            transform.position += d.normalized * Mathf.Min(d.magnitude, Speed * dt);
            return false;
        }

        // ── 공용 밈 모션(카페 고양이도 CatView에서 같은 함수 사용) ──
        // 빙글: 옆모습 스프라이트를 가로로 접었다 펴며 좌우를 뒤집어 '도는' 것처럼. 점점 빨라졌다가 마지막에 감속.
        public static void Spin(Transform vis, SpriteRenderer r, float time)
        {
            float speed = Mathf.Lerp(3f, 11f, Mathf.Clamp01(time / 2f));
            float a = time * speed;
            float c = Mathf.Cos(a);
            r.flipX = c < 0f;
            var s = vis.localScale; s.x = Mathf.Max(0.12f, Mathf.Abs(c)); s.y = 1f; vis.localScale = s;
        }

        public static float Bounce(float time) => Mathf.Abs(Mathf.Sin(time * 7f)) * 0.42f;
        public static Vector3 Squash(float time)
        {
            float k = Mathf.Abs(Mathf.Sin(time * 7f));
            return k < 0.18f ? new Vector3(1.15f, 0.85f, 1f) : new Vector3(0.95f, 1.06f, 1f);   // 착지 순간 납작
        }
    }
}
