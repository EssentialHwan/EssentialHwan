using UnityEngine;
using YeogiCafe.Art;

namespace YeogiCafe.Visual
{
    // 자동 낚싯대 장난감: 모터가 몇 초 돌다 쉬다를 반복하며 낚싯대를 좌우로 휘두르고,
    // 끝에 매달린 깃털은 한 박자 늦게 대롱대롱 따라온다(진자). 고양이가 오면 덮치기(pounce) 놀이.
    public class WandToyView : MonoBehaviour
    {
        Transform rod, feather; SpriteRenderer rodSr, featherSr, baseSr; LineRenderer line;
        float t, amp, featherAng, featherVel;
        const float RodLen = 34f / 32f, StringLen = 0.55f;

        void Start()
        {
            baseSr = GetComponent<SpriteRenderer>();
            var r = new GameObject("Rod"); r.transform.SetParent(transform, false);
            r.transform.localPosition = new Vector3(0f, 9f / 32f, 0f);
            rod = r.transform; rodSr = r.AddComponent<SpriteRenderer>();
            rodSr.sprite = RuntimeSprites.Get("wand_rod", FurnitureDrawing.WandRod);
            var f = new GameObject("Feather"); f.transform.SetParent(transform, false);
            feather = f.transform; featherSr = f.AddComponent<SpriteRenderer>();
            featherSr.sprite = RuntimeSprites.Get("wand_feather", FurnitureDrawing.WandFeather);
            var l = new GameObject("String"); l.transform.SetParent(transform, false);
            line = l.AddComponent<LineRenderer>();
            line.positionCount = 2; line.startWidth = line.endWidth = 1f / 32f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = new Color(0.29f, 0.21f, 0.18f, 0.9f);
            line.useWorldSpace = true;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            t += dt;
            bool on = (t % 11f) < 7f;                             // 7초 휘두르고 4초 쉼
            amp = Mathf.MoveTowards(amp, on ? 1f : 0f, dt * 1.5f);
            float ang = Mathf.Sin(t * 2.1f) * 38f * amp + Mathf.Sin(t * 5.3f) * 6f * amp;   // 좌우 휘둘림 + 잔떨림
            rod.localEulerAngles = new Vector3(0, 0, ang);
            Vector3 tip = rod.position + rod.up * RodLen;

            // 깃털: 끝에 매달린 진자(중력 + 감쇠) — 낚싯대가 휘면 한 박자 늦게 따라 흔들림
            float rodVelX = Mathf.Cos(t * 2.1f) * 2.1f * 38f * amp * Mathf.Deg2Rad;
            featherVel += (-Mathf.Sin(featherAng) * 14f - featherVel * 1.6f - rodVelX * 2.2f) * dt;
            featherAng += featherVel * dt;
            Vector3 hang = new Vector3(Mathf.Sin(featherAng), -Mathf.Cos(featherAng), 0f) * StringLen;
            feather.position = tip + hang;
            feather.localEulerAngles = new Vector3(0, 0, 180f + featherAng * Mathf.Rad2Deg);   // 매단 점(그림 아래쪽)이 줄 끝, 깃털은 아래로 늘어짐
            line.SetPosition(0, tip); line.SetPosition(1, tip + hang * 0.92f);

            int baseOrder = baseSr != null ? baseSr.sortingOrder : 0;
            rodSr.sortingOrder = baseOrder + 1; featherSr.sortingOrder = baseOrder + 12;
            line.sortingOrder = baseOrder + 11;
        }
    }
}
