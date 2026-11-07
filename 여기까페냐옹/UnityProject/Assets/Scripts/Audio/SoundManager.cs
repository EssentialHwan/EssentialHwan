using System.Collections.Generic;
using UnityEngine;
using YeogiCafe.Core;
using YeogiCafe.Data;

namespace YeogiCafe.Audio
{
    // 배경음악(시간대 × 날씨 9곡 + 밤 정산 1곡, 3초 크로스페이드) + 빗소리 환경음 + 효과음.
    // 음원: Resources/Audio/bgm_{시간대}_{날씨}.ogg (0 아침·1 점심·2 저녁 / 0 맑음·1 흐림·2 비), bgm_night, amb_rain, sfx_*.
    // 지금 음원은 Tools/synth.py·sfx.py로 직접 합성한 임시음(저작권 문제 없음). 같은 이름으로 덮어쓰면 정식 음원으로 교체된다.
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager I { get; private set; }
        public static float BgmVolume = 0.5f, SfxVolume = 0.8f, AmbVolume = 0.35f;

        AudioSource bgmA, bgmB, amb;
        readonly List<AudioSource> pool = new();
        readonly Dictionary<string, AudioClip> clips = new();
        readonly Dictionary<string, float> lastPlayed = new();
        string bgmKey; float xfade = 1f; bool aActive = true;
        DayManager day;
        public static bool Muted;

        void Awake()
        {
            I = this;
            bgmA = NewSource(true); bgmB = NewSource(true); amb = NewSource(true);
            for (int i = 0; i < 10; i++) pool.Add(NewSource(false));
        }

        void Start()
        {
            if (FindObjectOfType<AudioListener>() == null)
            {
                var cam = Camera.main;
                (cam != null ? cam.gameObject : gameObject).AddComponent<AudioListener>();
            }
        }

        AudioSource NewSource(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f; s.volume = 0f;
            return s;
        }

        AudioClip Clip(string id)
        {
            if (clips.TryGetValue(id, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + id);
            clips[id] = c;
            return c;
        }

        string WantedBgm()
        {
            if (day == null) day = FindObjectOfType<DayManager>();
            if (day == null) return "bgm_0_0";
            if (day.Phase == DayPhase.Settlement) return "bgm_night";
            int p = day.Phase == DayPhase.Lunch ? 1 : day.Phase == DayPhase.Evening ? 2 : 0;
            return $"bgm_{p}_{(int)Weather.Current}";
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) { Muted = !Muted; PlayerPrefs.SetInt("opt.mute", Muted ? 1 : 0); }     // M: 소리 끄기/켜기
            AudioListener.volume = Muted ? 0f : 1f;

            string want = WantedBgm();
            if (want != bgmKey)
            {
                var clip = Clip(want);
                if (clip != null)
                {
                    var from = aActive ? bgmA : bgmB;
                    bool sameSong = bgmKey != null && want.Length == bgmKey.Length && want.StartsWith(bgmKey.Substring(0, bgmKey.Length - 1));
                    bgmKey = want;
                    aActive = !aActive;
                    var to = aActive ? bgmA : bgmB;
                    to.clip = clip; to.volume = 0f;
                    // 같은 시간대에서 날씨만 바뀌면(bgm_1_0 → bgm_1_2) 같은 노래의 다른 편곡이라 길이가 같다 → 듣던 자리에서 이어 튼다
                    to.Play();
                    if (sameSong && from.isPlaying && from.clip != null && from.clip.samples == clip.samples) to.timeSamples = from.timeSamples;
                    xfade = 0f;
                }
                else bgmKey = want;
            }
            xfade = Mathf.Min(1f, xfade + Time.unscaledDeltaTime / 3f);
            var on = aActive ? bgmA : bgmB; var off = aActive ? bgmB : bgmA;
            on.volume = BgmVolume * xfade;
            off.volume = BgmVolume * (1f - xfade);
            if (xfade >= 1f && off.isPlaying) off.Stop();

            // 환경음: 비 오는 날 빗소리 / 비 안 오는 여름 낮 매미(살짝, 저녁엔 더 작게). 바뀔 땐 줄였다가 갈아 끼운다.
            string ambWant = null; float ambVol = 0f;
            bool open = day != null && day.Phase != DayPhase.Settlement;
            if (open && Weather.Current == WeatherKind.Rain) { ambWant = "amb_rain"; ambVol = AmbVolume; }
            else if (open && YeogiCafe.Data.SeasonBook.Season(day.CurrentDay) == 1)
            { ambWant = "amb_cicada"; ambVol = AmbVolume * (day.Phase == DayPhase.Evening ? 0.15f : 0.35f); }
            bool swap = amb.clip != null && ambWant != null && amb.clip.name != ambWant;
            float target = swap ? 0f : ambVol;
            amb.volume = Mathf.MoveTowards(amb.volume, target, Time.unscaledDeltaTime * 0.3f);
            if (amb.volume <= 0f && (swap || ambWant == null) && amb.isPlaying) amb.Stop();
            if (ambWant != null && !amb.isPlaying)
            {
                amb.clip = Clip(ambWant);
                if (amb.clip != null) amb.Play();
            }
        }

        // 효과음. variants>1이면 id_0..id_{n-1} 중 무작위. throttle: 같은 키로 cooldown초 안에 다시 울리지 않음(고양이별 도배 방지).
        public static void Sfx(string id, float vol = 1f, float pitchJitter = 0.06f, string throttleKey = null, float cooldown = 0f, int variants = 1)
        {
            var m = I; if (m == null) return;
            string key = throttleKey ?? id;
            float now = Time.unscaledTime;
            if (cooldown > 0f && m.lastPlayed.TryGetValue(key, out var last) && now - last < cooldown) return;
            m.lastPlayed[key] = now;
            string clipId = variants > 1 ? $"{id}_{Random.Range(0, variants)}" : id;
            var clip = m.Clip(clipId); if (clip == null) return;
            AudioSource src = null;
            foreach (var s in m.pool) if (!s.isPlaying) { src = s; break; }
            if (src == null) src = m.pool[0];
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.clip = clip; src.volume = vol * SfxVolume; src.Play();
        }

        // 계속 울리는 소리(골골송 등) — 켜고 끄기
        AudioSource loopSrc; string loopId;
        public static void Loop(string id, bool on, float vol = 0.6f)
        {
            var m = I; if (m == null) return;
            if (m.loopSrc == null) m.loopSrc = m.NewSource(true);
            if (on)
            {
                if (m.loopId != id) { m.loopSrc.clip = m.Clip(id); m.loopId = id; m.loopSrc.Play(); }
                m.loopSrc.volume = vol * SfxVolume;
            }
            else if (m.loopId == id) { m.loopSrc.Stop(); m.loopId = null; }
        }
    }
}
