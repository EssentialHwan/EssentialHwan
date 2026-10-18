# 여기까페냐옹 BGM 합성기 — 시간대(아침/점심/저녁) × 날씨(맑음/흐림/비) 9곡 + 밤(정산) 1곡 + 빗소리 루프.
# 전부 직접 합성(외부 샘플 없음) → 저작권 걱정 없음. 16마디 심리스 루프.
import numpy as np, scipy.signal as sg, subprocess, os, sys

SR = 32000
OUT = sys.argv[1] if len(sys.argv) > 1 else "out"
os.makedirs(OUT, exist_ok=True)

def midi(n): return 440.0 * 2 ** ((n - 69) / 12)

def env_adsr(n, a, d, s, r, sr=SR):
    t = np.arange(n) / sr
    e = np.ones(n) * s
    ai = int(a * sr); di = int(d * sr); ri = int(r * sr)
    if ai > 0: e[:ai] = np.linspace(0, 1, ai)
    if di > 0: e[ai:ai + di] = np.linspace(1, s, min(di, max(0, n - ai)))[: max(0, min(di, n - ai))]
    if ri > 0 and n > ri: e[-ri:] *= np.linspace(1, 0, ri)
    return e

# ── 악기 ──
def epiano(f, dur, vel=1.0, trem=0.0):
    n = int(dur * SR); t = np.arange(n) / SR
    idx = 1.8 * np.exp(-t * 5)
    mod = np.sin(2 * np.pi * f * t) * idx
    x = np.sin(2 * np.pi * f * t + mod) * 0.8 + 0.18 * np.sin(2 * np.pi * 2 * f * t) * np.exp(-t * 3)
    x += 0.06 * np.sin(2 * np.pi * 7.0 * f * t) * np.exp(-t * 30)       # 타건 '띵'
    e = np.exp(-t * 1.6) * env_adsr(n, 0.004, 0, 1, 0.08)
    if trem: x *= 1 - trem * (0.5 + 0.5 * np.sin(2 * np.pi * 4.2 * t))
    return x * e * vel

def marimba(f, dur, vel=1.0):
    n = int(dur * SR); t = np.arange(n) / SR
    x = np.sin(2 * np.pi * f * t) + 0.28 * np.sin(2 * np.pi * 3.93 * f * t) * np.exp(-t * 18)
    return x * np.exp(-t * 5.5) * env_adsr(n, 0.002, 0, 1, 0.03) * vel

def glock(f, dur, vel=1.0):
    n = int(dur * SR); t = np.arange(n) / SR
    x = np.sin(2 * np.pi * f * t) + 0.4 * np.sin(2 * np.pi * 2.76 * f * t) * np.exp(-t * 6) + 0.2 * np.sin(2 * np.pi * 5.4 * f * t) * np.exp(-t * 12)
    return x * np.exp(-t * 2.8) * env_adsr(n, 0.001, 0, 1, 0.05) * vel

def flute(f, dur, vel=1.0):
    n = int(dur * SR); t = np.arange(n) / SR
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.2 * t) * np.clip(t / 0.25, 0, 1)
    ph = 2 * np.pi * np.cumsum(f * vib) / SR
    x = np.sin(ph) + 0.12 * np.sin(2 * ph) + 0.04 * np.sin(3 * ph)
    br = np.random.default_rng(int(f)).standard_normal(n)
    b, a = sg.butter(2, [f * 0.9 / (SR / 2), min(0.95, f * 1.15 / (SR / 2))], 'band')
    x += 0.025 * sg.lfilter(b, a, br) * np.exp(-t * 4)   # 숨소리는 첫 순간만 살짝
    return x * env_adsr(n, 0.06, 0.1, 0.8, 0.12) * vel

def pluck(f, dur, vel=1.0, bright=0.5):
    n = int(dur * SR); N = max(2, int(SR / f))
    rng = np.random.default_rng(int(f * 10))
    buf = rng.uniform(-1, 1, N)
    b, a = sg.butter(1, 0.1 + bright * 0.6)
    buf = sg.lfilter(b, a, buf)
    out = np.zeros(n)
    for i in range(n):
        out[i] = buf[i % N]
        buf[i % N] = 0.497 * (buf[i % N] + buf[(i + 1) % N])
    return out * vel * env_adsr(n, 0.001, 0, 1, 0.05)

def bass(f, dur, vel=1.0):
    n = int(dur * SR); t = np.arange(n) / SR
    x = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(2 * np.pi * 2 * f * t) + 0.08 * np.sin(2 * np.pi * 3 * f * t)
    return x * np.exp(-t * 1.2) * env_adsr(n, 0.008, 0, 1, 0.06) * vel

def musicbox(f, dur, vel=1.0):
    n = int(dur * SR); t = np.arange(n) / SR
    x = np.sin(2 * np.pi * f * t) + 0.3 * np.sin(2 * np.pi * 4.0 * f * t) * np.exp(-t * 8) + 0.15 * np.sin(2 * np.pi * 6.1 * f * t) * np.exp(-t * 14)
    return x * np.exp(-t * 2.2) * env_adsr(n, 0.001, 0, 1, 0.05) * vel

def pad(freqs, dur, vel=1.0):
    n = int(dur * SR); t = np.arange(n) / SR
    x = np.zeros(n)
    for f in freqs:
        for d in (-0.15, 0.15):
            x += sg.sawtooth(2 * np.pi * f * (1 + d / 100) * t)
    b, a = sg.butter(2, 1400 / (SR / 2))
    x = sg.lfilter(b, a, x) / (len(freqs) * 2)
    return x * env_adsr(n, dur * 0.3, 0, 1, dur * 0.3) * vel

def noise(n, seed):
    return np.random.default_rng(seed).standard_normal(n)

def kick(vel=1.0):
    n = int(0.25 * SR); t = np.arange(n) / SR
    f = 45 + 70 * np.exp(-t * 30)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 14) * vel

def brush(vel=1.0, seed=1):
    n = int(0.18 * SR); t = np.arange(n) / SR
    b, a = sg.butter(2, [1500 / (SR / 2), 7000 / (SR / 2)], 'band')
    return sg.lfilter(b, a, noise(n, seed)) * np.exp(-t * 22) * vel * 0.6

def shaker(vel=1.0, seed=2):
    n = int(0.06 * SR); t = np.arange(n) / SR
    b, a = sg.butter(2, 6000 / (SR / 2), 'high')
    return sg.lfilter(b, a, noise(n, seed)) * np.exp(-t * 60) * vel * 0.35

def rim(vel=1.0):
    n = int(0.05 * SR); t = np.arange(n) / SR
    return (np.sin(2 * np.pi * 1800 * t) * 0.6 + noise(n, 9) * 0.3) * np.exp(-t * 90) * vel

# ── 곡 ──
class Song:
    def __init__(self, bpm, bars=16):
        self.bpm = bpm; self.beat = 60 / bpm; self.bars = bars
        self.len = int(bars * 4 * self.beat * SR)
        self.buf = np.zeros(self.len + SR * 4)       # 꼬리 여유(나중에 앞으로 감음 → 심리스)
    def add(self, x, beat_pos, gain=1.0):
        i = int(beat_pos * self.beat * SR)
        self.buf[i:i + len(x)] += x * gain
    def render(self):
        y = self.buf[: self.len].copy()
        tail = self.buf[self.len:]
        y[: len(tail)] += tail                        # 루프 끝을 넘친 잔향을 처음에 더함
        return y

def chord_tones(root, quality):
    q = {"maj": [0, 4, 7], "min": [0, 3, 7], "maj7": [0, 4, 7, 11], "min7": [0, 3, 7, 10], "dom7": [0, 4, 7, 10], "add9": [0, 4, 7, 14], "m9": [0, 3, 7, 10, 14]}[quality]
    return [root + i for i in q]

def make_melody(rng, prog, scale, low, high, density, bars=16):
    # 프레이즈 AABA(4마디씩): A를 만들고 반복, B는 새로. 강박엔 코드톤, 약박엔 음계 경과음.
    def phrase(seed_rng):
        notes = []
        prev = None
        for bar in range(4):
            root, q = prog[bar % len(prog)]
            ct = [n for n in range(low, high + 1) if (n - root) % 12 in [(x - root) % 12 for x in chord_tones(root, q)]]
            sc = [n for n in range(low, high + 1) if (n % 12) in scale]
            beat = 0.0
            while beat < 4:
                strong = beat in (0.0, 2.0)
                pool = ct if strong else sc
                if prev is None: cand = pool
                else:
                    cand = [n for n in pool if abs(n - prev) <= (5 if strong else 3)] or pool
                n = int(seed_rng.choice(cand))
                durs = [0.5, 1.0, 1.0, 1.5, 2.0] if density < 0.5 else [0.5, 0.5, 1.0, 1.0, 1.5]
                d = float(seed_rng.choice(durs))
                d = min(d, 4 - beat)
                if seed_rng.random() > 0.15 or strong:
                    notes.append((bar * 4 + beat, n, d))
                    prev = n
                beat += d
        return notes
    A = phrase(rng); B = phrase(rng)
    out = []
    for k, ph in enumerate([A, A, B, A]):
        for (b, n, d) in ph:
            # A 반복 마지막 마디는 으뜸음으로 해결
            out.append((k * 16 + b, n, d))
    return out

def mix_master(y, lowpass=None, crackle=0.0, seed=0):
    if lowpass:
        b, a = sg.butter(2, lowpass / (SR / 2)); y = sg.filtfilt(b, a, y)
    if crackle:
        n = len(y); r = np.random.default_rng(seed)
        c = np.zeros(n); idx = r.integers(0, n, int(n / SR * 6)); c[idx] = r.uniform(-1, 1, len(idx))
        b, a = sg.butter(2, 3000 / (SR / 2), 'high'); c = sg.lfilter(b, a, c)
        y = y + c * crackle + noise(n, seed + 1) * crackle * 0.03
    # 부드러운 리미터
    peak = np.max(np.abs(y)) + 1e-9
    y = y / peak * 0.9
    y = np.tanh(y * 1.2) / np.tanh(1.2)
    return y * 0.85

def build(phase, weather):
    rng = np.random.default_rng(100 + phase * 7)      # 같은 시간대는 같은 멜로디 → 날씨만 편곡이 바뀐다
    if phase == 0:   # 아침: F장조, 산뜻
        bpm, key = 92, 65
        prog = [(65, "add9"), (72, "maj"), (62, "min7"), (70, "maj7")]          # F – C – Dm7 – Bbmaj7
        scale = [5, 7, 9, 0, 2]
    elif phase == 1: # 점심: C장조, 통통
        bpm, key = 108, 60
        prog = [(60, "maj"), (57, "min7"), (62, "min7"), (67, "dom7")]          # C – Am7 – Dm7 – G7
        scale = [0, 2, 4, 7, 9]
    else:            # 저녁: Bb장조, 재지
        bpm, key = 76, 58
        prog = [(58, "maj7"), (55, "min7"), (60, "min7"), (65, "dom7")]         # Bbmaj7 – Gm7 – Cm7 – F7
        scale = [10, 0, 2, 5, 7]
    if weather == 2: bpm = int(bpm * 0.9)
    s = Song(bpm)
    mel = make_melody(rng, prog, scale, key + 7, key + 21, 0.5 if phase != 1 else 0.7)

    for bar in range(16):
        root, q = prog[bar % 4]
        tones = chord_tones(root, q)
        voicing = [t - 12 for t in tones[:4]]
        # 반주
        if weather == 2:
            for v in voicing: s.add(epiano(midi(v), 4 * s.beat + 0.5, 0.22, trem=0.35), bar * 4)
            s.add(pad([midi(v) for v in voicing[:3]], 4 * s.beat, 0.12), bar * 4)
        elif phase == 1:
            for beat in (0, 1.5, 2, 3.5):
                for v in voicing: s.add(epiano(midi(v), 0.6, 0.16), bar * 4 + beat)
        else:
            for v in voicing: s.add(epiano(midi(v), 4 * s.beat, 0.2), bar * 4)
            for v in voicing[1:3]: s.add(epiano(midi(v + 12), 1.0, 0.07), bar * 4 + 2.5)
        # 베이스
        bvel = 0.5 if weather != 2 else 0.35
        if phase == 1 and weather != 2:
            walk = [root - 24, root - 24 + 7, root - 24 + 12, root - 24 + 7]
            for i, nn in enumerate(walk): s.add(bass(midi(nn), s.beat * 0.95, bvel), bar * 4 + i)
        else:
            s.add(bass(midi(root - 24), 2 * s.beat, bvel), bar * 4)
            s.add(bass(midi(root - 24 + 7), 2 * s.beat, bvel * 0.8), bar * 4 + 2)
        # 드럼
        if weather == 0:
            for b in range(4):
                if phase != 2: s.add(shaker(0.8, bar * 4 + b), bar * 4 + b + 0.5)
                s.add(shaker(0.4, bar * 9 + b), bar * 4 + b)
            s.add(kick(0.45), bar * 4); s.add(kick(0.3), bar * 4 + 2.5 if phase == 1 else bar * 4 + 2)
            s.add(brush(0.5, bar), bar * 4 + 1); s.add(brush(0.5, bar + 50), bar * 4 + 3)
        elif weather == 1:
            s.add(kick(0.3), bar * 4); s.add(brush(0.35, bar), bar * 4 + 2)
            if bar % 2: s.add(shaker(0.4, bar), bar * 4 + 3.5)
        else:
            if bar % 2 == 0: s.add(rim(0.25), bar * 4 + 2)
            s.add(kick(0.18), bar * 4)

    # 멜로디: 날씨별 악기
    for (b, n, d) in mel:
        dur = d * s.beat
        if weather == 0:
            inst = marimba if phase != 2 else epiano
            s.add(inst(midi(n), dur + 0.4, 0.42), b)
            if phase == 0 and (b % 4 == 0): s.add(glock(midi(n + 12), 1.2, 0.12), b)
        elif weather == 1:
            s.add(flute(midi(n), dur * 0.95, 0.3), b)
        else:
            if (b % 2 == 0) or d >= 1.5:   # 비: 음을 솎아 듬성듬성
                s.add(epiano(midi(n), dur + 0.6, 0.28, trem=0.2), b)

    y = s.render()
    y = mix_master(y, lowpass=2600 if weather == 2 else (6000 if weather == 1 else None),
                   crackle=0.04 if weather == 2 else 0.0, seed=phase * 10 + weather)
    return y

def night():
    # 밤(도감·정산): 오르골 자장가, 느린 3/4 느낌을 4/4로
    rng = np.random.default_rng(777)
    s = Song(66)
    prog = [(65, "maj"), (69, "min"), (70, "maj"), (72, "dom7")]
    scale = [5, 7, 9, 0, 2]
    mel = make_melody(rng, prog, scale, 77, 89, 0.3)
    for bar in range(16):
        root, q = prog[bar % 4]
        tones = chord_tones(root, q)
        for i, v in enumerate([tones[0], tones[1], tones[2], tones[1]]):
            s.add(musicbox(midi(v), 1.2, 0.18), bar * 4 + i)
        s.add(bass(midi(root - 24), 4 * s.beat, 0.25), bar * 4)
    for (b, n, d) in mel: s.add(musicbox(midi(n), d * s.beat + 0.8, 0.4), b)
    return mix_master(s.render(), lowpass=7000)

def rain_loop(sec=16):
    n = int(sec * SR)
    w = noise(n + SR, 5)
    b, a = sg.butter(2, [400 / (SR / 2), 5000 / (SR / 2)], 'band')
    y = sg.lfilter(b, a, w)[SR:] * 0.35
    r = np.random.default_rng(6)
    for _ in range(int(sec * 30)):                    # 빗방울 톡톡
        i = r.integers(0, n - 2000); L = r.integers(200, 900)
        t = np.arange(L) / SR
        f = r.uniform(1800, 5000)
        y[i:i + L] += np.sin(2 * np.pi * f * t) * np.exp(-t * r.uniform(150, 400)) * r.uniform(0.05, 0.25)
    # 루프 이음새: 끝 1초를 처음과 크로스페이드
    m = SR
    fade = np.linspace(0, 1, m)
    y[:m] = y[:m] * fade + y[-m:] * (1 - fade)
    y = y[:-m]
    return y / np.max(np.abs(y)) * 0.6

def save(name, y):
    wav = os.path.join(OUT, name + ".wav")
    import scipy.io.wavfile as wf
    wf.write(wav, SR, (np.clip(y, -1, 1) * 32767).astype(np.int16))
    ogg = os.path.join(OUT, name + ".ogg")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "4", ogg], check=True)
    os.remove(wav)
    print(name, f"{len(y)/SR:.1f}s")

if __name__ == "__main__":
    which = sys.argv[2] if len(sys.argv) > 2 else "all"
    if which in ("all", "bgm"):
        for p in range(3):
            for w in range(3):
                save(f"bgm_{p}_{w}", build(p, w))
        save("bgm_night", night())
        save("amb_rain", rain_loop())
