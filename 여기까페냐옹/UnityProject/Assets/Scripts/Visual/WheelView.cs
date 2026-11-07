using UnityEngine;
using YeogiCafe.Art;
using YeogiCafe.Cafe;

namespace YeogiCafe.Visual
{
    // 캣휠: 고양이가 올라타면 서서히 빨라지며 시계 방향으로 돌고(바퀴살 프레임), '우우웅·딸깍' 소리가 난다.
    // 내리면 관성으로 조금 더 돌다 멈춘다. 고양이 쪽 표현(안에서 run 클립)은 CatView가 맡는다.
    public class WheelView : MonoBehaviour
    {
        SpriteRenderer baseSr, wheelSr;
        Sprite[] frames;
        FacilityBehaviour fac;
        AudioSource audioSrc;
        float spin, phase, occupiedT;

        void Start()
        {
            baseSr = GetComponent<SpriteRenderer>();
            fac = GetComponent<FacilityBehaviour>();
            frames = new Sprite[4];
            for (int i = 0; i < 4; i++) { int k = i; frames[i] = RuntimeSprites.Get("wheel_spin" + i, () => FurnitureDrawing.WheelSpin(k)); }
            var w = new GameObject("Wheel"); w.transform.SetParent(transform, false);
            w.transform.localPosition = new Vector3(0f, -4f / 32f, 0f);      // 받침 스프라이트(바닥선 4px 피벗)와 맞춤
            wheelSr = w.AddComponent<SpriteRenderer>();
            wheelSr.sprite = frames[0];

            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false; audioSrc.loop = true; audioSrc.spatialBlend = 0f; audioSrc.volume = 0f;
            audioSrc.clip = Resources.Load<AudioClip>("Audio/sfx_wheel");
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            bool busy = fac != null && !fac.IsFree;
            occupiedT = busy ? occupiedT + dt : 0f;
            float target = busy && occupiedT > 0.7f ? 1f : 0f;              // 점프해 들어간 뒤부터 달림
            spin = Mathf.MoveTowards(spin, target, dt * (target > spin ? 0.9f : 0.45f));   // 가속·관성
            phase += spin * dt * 14f;                                       // 초당 최대 14프레임(≈1.6회전/초)
            wheelSr.sprite = frames[((int)phase) % 4];

            int baseOrder = baseSr != null ? baseSr.sortingOrder : 0;
            wheelSr.sortingOrder = baseOrder + 1;                           // 받침 위, 안의 고양이(+5)보다는 뒤

            if (audioSrc.clip != null)
            {
                float vol = spin * 0.55f * YeogiCafe.Audio.SoundManager.SfxVolume;
                if (vol > 0.01f && !audioSrc.isPlaying) audioSrc.Play();
                audioSrc.volume = vol;
                audioSrc.pitch = 0.85f + 0.2f * spin;
                if (vol <= 0.01f && audioSrc.isPlaying) audioSrc.Stop();
            }
        }
    }
}
