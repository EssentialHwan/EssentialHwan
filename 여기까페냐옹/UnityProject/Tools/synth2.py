# 여기카페냐옹 BGM v2 — 주제곡 하나(직접 작곡)를 시간대·날씨별로 편곡.
#   python3 synth2.py <출력폴더> [bgm|night|all]
# v1(synth.py)과 달라진 점
#  - 멜로디를 무작위로 뽑지 않고 직접 쓴 주제곡(A 8마디 + B 8마디)을 쓴다 → 아침·점심·저녁·밤이 같은 노래의 다른 편곡
#  - 스테레오(악기별 좌우 배치) + 잔향(방 울림) + 사람 손 느낌(타이밍·세기 미세 흔들림)
#  - 같은 시간대의 맑음·흐림·비는 템포·길이가 같다 → 날씨가 바뀌면 SoundManager가 같은 위치에서 이어 튼다
#  - 32마디 루프(A A' B A'') — v1(16마디)보다 두 배 길어 덜 반복적으로 들림
# 전부 직접 합성 → 외부 샘플·라이선스 없음.
import sys, os, subprocess
import numpy as np, scipy.signal as sg
import scipy.io.wavfile as wf

SR = 32000
OUT = sys.argv[1] if len(sys.argv) > 1 else "out2"
os.makedirs(OUT, exist_ok=True)

def midi(n): return 440.0 * 2 ** ((n - 69) / 12)
def T(n): return np.arange(n) / SR

def adsr(n, a, r):
    e = np.ones(n)
    ai = min(n, max(1, int(a * SR))); ri = min(n, max(1, int(r * SR)))
    e[:ai] *= np.linspace(0, 1, ai); e[-ri:] *= np.linspace(1, 0, ri)
    return e

# ───────── 악기 ─────────
def epiano(f, dur, vel=1.0, bright=1.0):
    n = int(dur * SR); t = T(n)
    idx = 1.6 * bright * np.exp(-t * 6)
    x = np.sin(2 * np.pi * f * t + np.sin(2 * np.pi * f * t) * idx) * 0.8
    x += 0.15 * np.sin(2 * np.pi * 2 * f * t) * np.exp(-t * 3)
    x += 0.05 * bright * np.sin(2 * np.pi * 7 * f * t) * np.exp(-t * 35)
    return x * np.exp(-t * 1.4) * adsr(n, 0.004, 0.12) * vel

def marimba(f, dur, vel=1.0):
    n = int(dur * SR); t = T(n)
    x = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(2 * np.pi * 3.93 * f * t) * np.exp(-t * 20) + 0.08 * np.sin(2 * np.pi * 9.2 * f * t) * np.exp(-t * 60)
    return x * np.exp(-t * 4.5) * adsr(n, 0.002, 0.05) * vel

def glock(f, dur, vel=1.0):
    n = int(dur * SR); t = T(n)
    x = np.sin(2 * np.pi * f * t) + 0.35 * np.sin(2 * np.pi * 2.76 * f * t) * np.exp(-t * 7) + 0.15 * np.sin(2 * np.pi * 5.4 * f * t) * np.exp(-t * 14)
    return x * np.exp(-t * 2.5) * adsr(n, 0.001, 0.08) * vel

def musicbox(f, dur, vel=1.0):
    n = int(dur * SR); t = T(n)
    x = np.sin(2 * np.pi * f * t) + 0.28 * np.sin(2 * np.pi * 4.0 * f * t) * np.exp(-t * 9) + 0.12 * np.sin(2 * np.pi * 6.1 * f * t) * np.exp(-t * 16)
    return x * np.exp(-t * 1.9) * adsr(n, 0.001, 0.08) * vel

def flute(f, dur, vel=1.0, seed=0):
    n = int(dur * SR); t = T(n)
    vib = 1 + 0.005 * np.sin(2 * np.pi * 5.0 * t) * np.clip((t - 0.15) / 0.3, 0, 1)
    ph = 2 * np.pi * np.cumsum(f * vib) / SR
    x = np.sin(ph) + 0.10 * np.sin(2 * ph) + 0.03 * np.sin(3 * ph)
    br = np.random.default_rng(seed).standard_normal(n)
    b, a = sg.butter(2, [f * 0.9 / (SR / 2), min(0.95, f * 1.2 / (SR / 2))], 'band')
    x += 0.03 * sg.lfilter(b, a, br) * (0.4 + 0.6 * np.exp(-t * 5))
    return x * adsr(n, 0.07, 0.15) * vel

def pluck(f, dur, vel=1.0, dark=0.5, seed=0):
    # 카플러스-스트롱(나일론 기타·콘트라베이스 피치카토) — 지연선 필터로 벡터화
    n = int(dur * SR); N = max(2, int(round(SR / f)))
    rng = np.random.default_rng(seed)
    exc = np.zeros(n); exc[:N] = rng.uniform(-1, 1, N)
    b, a = sg.butter(1, 0.08 + (1 - dark) * 0.5); exc = sg.lfilter(b, a, exc)
    den = np.zeros(N + 2); den[0] = 1; den[N] = -0.498; den[N + 1] = -0.498
    y = sg.lfilter([1.0], den, exc)
    return y / (np.max(np.abs(y[:N * 4])) + 1e-9) * adsr(n, 0.001, 0.06) * vel

def upright(f, dur, vel=1.0, seed=0):
    # 콘트라베이스 피치카토: 줄 튕김(카플러스) + 몸통 울림(사인) — 튕김만 쓰면 저음이 비어서 얇게 들림
    x = pluck(f, dur, 0.7, 0.9, seed) + bass(f, dur, 0.75)
    return x * vel

def pad(freqs, dur, vel=1.0, cutoff=1300):
    n = int(dur * SR); t = T(n); x = np.zeros(n)
    for f in freqs:
        for d in (-0.12, 0.12):
            x += sg.sawtooth(2 * np.pi * f * (1 + d / 100) * t)
    b, a = sg.butter(2, cutoff / (SR / 2)); x = sg.lfilter(b, a, x) / (len(freqs) * 2)
    return x * adsr(n, dur * 0.35, dur * 0.35) * vel

def bass(f, dur, vel=1.0):
    n = int(dur * SR); t = T(n)
    x = np.sin(2 * np.pi * f * t) + 0.22 * np.sin(2 * np.pi * 2 * f * t) + 0.06 * np.sin(2 * np.pi * 3 * f * t)
    return x * np.exp(-t * 1.3) * adsr(n, 0.01, 0.07) * vel

def nz(n, seed): return np.random.default_rng(seed).standard_normal(n)
def kick(vel=1.0):
    n = int(0.25 * SR); t = T(n); f = 48 + 65 * np.exp(-t * 32)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 15) * vel
def brush(vel=1.0, seed=1):
    n = int(0.2 * SR); t = T(n); b, a = sg.butter(2, [1500 / (SR / 2), 6500 / (SR / 2)], 'band')
    return sg.lfilter(b, a, nz(n, seed)) * np.exp(-t * 20) * vel * 0.5
def shaker(vel=1.0, seed=2):
    n = int(0.06 * SR); t = T(n); b, a = sg.butter(2, 6000 / (SR / 2), 'high')
    return sg.lfilter(b, a, nz(n, seed)) * np.exp(-t * 60) * vel * 0.3
def rim(vel=1.0, seed=9):
    n = int(0.05 * SR); t = T(n)
    return (np.sin(2 * np.pi * 1700 * t) * 0.6 + nz(n, seed) * 0.25) * np.exp(-t * 90) * vel

# ───────── 주제곡 (도=0 기준 반음, 마디 안 박자, 길이) ─────────
# A: "어서 오라냥" — 미 솔라 솔 미 / 도' 시 라 …  (I vi IV V I vi ii V)
A = [
    [(0, 4, 1), (1, 7, .5), (1.5, 9, .5), (2, 7, 1), (3, 4, 1)],
    [(0, 12, 1.5), (1.5, 11, .5), (2, 9, 2)],
    [(0, 9, 1), (1, 12, .5), (1.5, 9, .5), (2, 7, 1), (3, 5, 1)],
    [(0, 4, 1), (1, 2, 1), (2, 7, 2)],
    [(0, 4, 1), (1, 7, .5), (1.5, 9, .5), (2, 7, 1), (3, 4, 1)],
    [(0, 12, 1), (1, 14, .5), (1.5, 12, .5), (2, 9, 2)],
    [(0, 9, 1), (1, 7, .5), (1.5, 5, .5), (2, 4, 1), (3, 2, 1)],
    [(0, 2, 1.5), (1.5, 4, .5), (2, 2, 1), (3, -1, 1)],
]
A_END = [(0, 7, 1), (1, 4, 1), (2, 2, 2)]          # 마지막 A의 끝: 솔 미 레~ 하고 숨을 쉬었다가 처음(미)으로 이어짐
# B: 조금 높이 올라가서 숨 고르기 (IV V iii vi ii V I V7)
B = [
    [(0, 5, 1), (1, 9, 1), (2, 12, 1.5), (3.5, 9, .5)],
    [(0, 7, 1.5), (1.5, 5, .5), (2, 4, 1), (3, 2, 1)],
    [(0, 4, 1), (1, 7, 1), (2, 11, 2)],
    [(0, 9, 3)],
    [(0, 14, 1), (1, 12, .5), (1.5, 9, .5), (2, 5, 1), (3, 9, 1)],
    [(0, 7, 1.5), (1.5, 9, .5), (2, 11, 1), (3, 14, 1)],
    [(0, 12, 2), (2, 7, 1), (3, 4, 1)],
    [(0, 2, 1), (1, 5, 1), (2, 7, 1), (3, -1, 1)],
]
# 화음(도=0 기준 근음, 종류)
CA = [(0, 'I'), (9, 'vi'), (5, 'IV'), (7, 'V'), (0, 'I'), (9, 'vi'), (2, 'ii'), (7, 'V')]
CB = [(5, 'IV'), (7, 'V'), (4, 'iii'), (9, 'vi'), (2, 'ii'), (7, 'V'), (0, 'I'), (7, 'V7')]

def chord(root, deg, jazz):
    if deg == 'I':  q = [0, 4, 7, 11] if jazz else [0, 4, 7, 14]
    elif deg == 'IV': q = [0, 4, 7, 11] if jazz else [0, 4, 7, 9]
    elif deg in ('V', 'V7'): q = [0, 4, 7, 10] if (jazz or deg == 'V7') else [0, 4, 7, 9]    # 밝은 곡의 V는 6화음(멜로디의 '미'와 안 부딪힘)
    else: q = [0, 3, 7, 10]      # ii iii vi = 단7
    return [root + i for i in q]

def form(night=False):
    """32마디(밤은 16마디) 순서: (마디 멜로디, 화음, 구간 이름)"""
    secs = [('A', A, CA), ('B', B, CB)] if night else [('A', A, CA), ('A2', A, CA), ('B', B, CB), ('A3', A[:7] + [A_END], CA)]
    out = []
    for name, mel, ch in secs:
        for i in range(8): out.append((mel[i], ch[i], name))
    return out

# ───────── 믹서: 스테레오 버스 + 잔향 ─────────
class Mix:
    def __init__(self, bpm, bars, seed):
        self.beat = 60 / bpm; self.len = int(bars * 4 * self.beat * SR)
        self.dry = np.zeros((self.len + 6 * SR, 2)); self.wet = np.zeros_like(self.dry)
        self.rng = np.random.default_rng(seed); self.swing = 0.0
    def at(self, beat_pos):
        frac = beat_pos % 1.0
        if self.swing and abs(frac - 0.5) < 1e-6: beat_pos += self.swing   # 스윙: 8분 뒷박을 살짝 늦게
        return beat_pos
    def add(self, x, beat_pos, gain=1.0, pan=0.0, send=0.25, human=True):
        bp = self.at(beat_pos)
        jit = self.rng.normal(0, 0.006) if human else 0.0          # ±6ms 정도 손 느낌
        i = max(0, int((bp * self.beat + jit) * SR))
        g = gain * (self.rng.uniform(0.9, 1.08) if human else 1.0)
        L = np.cos((pan + 1) * np.pi / 4); R = np.sin((pan + 1) * np.pi / 4)
        seg = x[: len(self.dry) - i] * g
        self.dry[i:i + len(seg), 0] += seg * L; self.dry[i:i + len(seg), 1] += seg * R
        if send:
            self.wet[i:i + len(seg), 0] += seg * L * send; self.wet[i:i + len(seg), 1] += seg * R * send

def reverb_ir(sec=1.8, seed=5, damp=3500):
    n = int(sec * SR); t = T(n); ir = np.zeros((n, 2))
    for ch in range(2):
        x = nz(n, seed + ch) * np.exp(-t * 6.9 / sec)            # RT60 ≈ sec
        b, a = sg.butter(1, damp / (SR / 2)); x = sg.lfilter(b, a, x)
        x[: int(0.018 * SR)] = 0                                   # 18ms 프리딜레이
        ir[:, ch] = x / np.sqrt(np.sum(x ** 2))
    return ir

def render(m, rev_sec=1.8, rev_gain=0.9, lowpass=None, crackle=0.0, wow=0.0, seed=0):
    ir = reverb_ir(rev_sec, seed + 31)
    wet = np.stack([sg.fftconvolve(m.wet[:, c], ir[:, c])[: len(m.wet)] for c in range(2)], 1)
    y = m.dry + wet * rev_gain
    # 루프: 끝을 넘친 잔향·꼬리를 처음에 겹쳐 이음새 없이
    body = y[: m.len].copy(); tail = y[m.len:]; body[: len(tail)] += tail; y = body
    if wow:   # 테이프가 아주 살짝 늘어지는 느낌(로파이) — 루프 길이에 딱 맞는 주기라 이음새 안 생김
        n = len(y); cyc = max(1, round(n / SR / 2.7))
        d = wow * SR * np.sin(2 * np.pi * cyc * np.arange(n) / n)
        idx = (np.arange(n) + d) % n
        yy = np.vstack([y, y[:1]])
        y = np.stack([np.interp(idx, np.arange(n + 1), yy[:, c]) for c in range(2)], 1)
    # 필터는 루프를 고리처럼 이어 붙인 채로 걸어야 끝과 처음이 똑같이 처리된다(이음새 '틱' 방지)
    P = SR
    def circ(b, a, y):
        z = np.concatenate([y[-P:], y, y[:P]]); z = sg.filtfilt(b, a, z, axis=0); return z[P:-P]
    if lowpass: y = circ(*sg.butter(2, lowpass / (SR / 2)), y)
    y = circ(*sg.butter(1, 35 / (SR / 2), 'high'), y)   # 웅웅거리는 초저역 정리
    if crackle:
        n = len(y); r = np.random.default_rng(seed + 77)
        c = np.zeros(n); idx = r.integers(0, n, int(n / SR * 5)); c[idx] = r.uniform(-1, 1, len(idx))
        b, a = sg.butter(2, 2500 / (SR / 2), 'high'); c = sg.lfilter(b, a, c)
        y += np.stack([c, np.roll(c, 37)], 1) * crackle
    return y

def master(y, target_rms):
    # 부드러운 압축(tanh) 후, 옛 곡과 같은 평균 음량에 맞춤 → 게임 안 음량 균형 유지
    y = y / (np.max(np.abs(y)) + 1e-9)
    y = np.tanh(y * 1.4) / np.tanh(1.4)
    y *= target_rms / (np.sqrt(np.mean(y ** 2)) + 1e-9)
    pk = np.max(np.abs(y))
    if pk > 0.95: y *= 0.95 / pk
    return y

# ───────── 편곡 ─────────
PHASES = {  # 조(으뜸음 MIDI), 템포, 멜로디 옥타브 이동
    0: dict(key=65, bpm=92),    # 아침 F장조
    1: dict(key=60, bpm=108),   # 점심 C장조
    2: dict(key=58, bpm=76),    # 저녁 B♭장조
}

def arrange(phase, weather):
    P = PHASES[phase]; key, bpm = P['key'], P['bpm']
    jazz = phase == 2
    bars = form()
    m = Mix(bpm, len(bars), seed=phase * 10 + weather)
    if jazz: m.swing = 0.12
    mel_oct = key + 12 if phase != 2 else key + 12
    for bi, (mel, (root, deg), sec) in enumerate(bars):
        b0 = bi * 4
        tones = chord(key + root, deg, jazz)
        low = [t - 12 for t in tones]          # 반주 음역
        broot = key + root - 24                # 베이스
        last = bi == len(bars) - 1
        # ── 반주 ──
        if weather == 2:   # 비: 길게 깔리는 로즈 + 패드
            for k, v in enumerate(low): m.add(epiano(midi(v), 4 * m.beat + 0.6, 0.16, 0.6), b0 + k * 0.06, pan=-0.25 + k * 0.17, send=0.45)
            m.add(pad([midi(v) for v in low[:3]], 4 * m.beat, 0.10, 900), b0, send=0.5)
        elif phase == 0:   # 아침: 나일론 기타 아르페지오(8분)
            pat = [low[0], low[1], low[2], low[3], low[2], low[1], low[2], low[3]]
            for k, v in enumerate(pat):
                if weather == 1 and k % 2: continue
                m.add(pluck(midi(v), 1.2, 0.22, 0.55, seed=bi * 8 + k), b0 + k * 0.5, pan=-0.35, send=0.3)
            if sec in ('A2', 'B'): m.add(pad([midi(v) for v in low[:3]], 4 * m.beat, 0.05, 1600), b0, send=0.4)
        elif phase == 1:   # 점심: 통통 튀는 로즈 뒷박(오프비트)
            for beat in ((0.5, 1.5, 2.5, 3.5) if weather == 0 else (1.5, 3.5)):
                for k, v in enumerate(low[1:]): m.add(epiano(midi(v), 0.35, 0.14, 1.1), b0 + beat, pan=-0.3 + k * 0.3, send=0.2)
        else:              # 저녁: 재즈 로즈 컴핑(코러스처럼 좌우 두 겹)
            for k, v in enumerate(low):
                m.add(epiano(midi(v), 2.2 * m.beat, 0.13), b0, pan=-0.45, send=0.35)
                m.add(epiano(midi(v) * 1.003, 2.2 * m.beat, 0.10), b0 + 0.02, pan=0.45, send=0.35)
            for k, v in enumerate(low[1:3]): m.add(epiano(midi(v + 12), 0.9, 0.06), b0 + 2.5, pan=0.2, send=0.4)
        # ── 베이스 ──
        bv = 0.42 if weather != 2 else 0.30
        if phase == 1 and weather != 2:      # 걷는 베이스
            for i, nn in enumerate([broot, broot + 7, broot + 12, broot + 7]): m.add(upright(midi(nn), m.beat * 1.1, bv * 0.85, seed=500 + bi * 4 + i), b0 + i, send=0.05)
        elif phase == 2 and weather != 2:    # 저녁: 콘트라베이스 2분 + 경과음
            m.add(upright(midi(broot), 2 * m.beat, bv, seed=600 + bi), b0, send=0.08)
            m.add(upright(midi(broot + 7), 1.5 * m.beat, bv * 0.8, seed=700 + bi), b0 + 2, send=0.08)
            m.add(upright(midi(broot + 6 if deg in ('V', 'V7') else broot + 10), 0.5 * m.beat, bv * 0.55, seed=800 + bi), b0 + 3.5, send=0.08)
        else:
            m.add(bass(midi(broot), 2 * m.beat, bv), b0, send=0.05)
            m.add(bass(midi(broot + 7), 2 * m.beat, bv * 0.75), b0 + 2, send=0.05)
        # ── 드럼(살살) ──
        if weather == 0:
            m.add(kick(0.42), b0, send=0.02); m.add(kick(0.28), b0 + (2.5 if phase == 1 else 2), send=0.02)
            m.add(brush(0.45, bi), b0 + 1, pan=0.2, send=0.2); m.add(brush(0.45, bi + 50), b0 + 3, pan=0.2, send=0.2)
            for b in range(4):
                if phase != 2: m.add(shaker(0.7, bi * 4 + b), b0 + b + 0.5, pan=0.4, send=0.1)
                else: m.add(brush(0.18, bi * 4 + b + 200), b0 + b + 0.5, pan=0.3, send=0.2)
        elif weather == 1:
            m.add(kick(0.28), b0, send=0.02); m.add(brush(0.3, bi), b0 + 2, pan=0.2, send=0.25)
            if bi % 2: m.add(shaker(0.4, bi), b0 + 3.5, pan=0.4, send=0.15)
        else:
            if bi % 2 == 0: m.add(rim(0.22, bi), b0 + 2, pan=0.15, send=0.3)
            m.add(kick(0.16), b0, send=0.02)
        # ── 멜로디 ──
        for (b, n, d) in mel:
            nn = mel_oct + n; dur = d * m.beat
            if weather == 0:
                if phase == 0:
                    m.add(marimba(midi(nn), dur + 0.5, 0.40), b0 + b, pan=0.15, send=0.3)
                    if b in (0, 2) and sec != 'A': m.add(glock(midi(nn + 12), 1.0, 0.10), b0 + b, pan=0.4, send=0.45)
                elif phase == 1:
                    m.add(marimba(midi(nn), dur + 0.3, 0.38), b0 + b, pan=0.1, send=0.2)
                    if sec in ('A2', 'A3'): m.add(pluck(midi(nn + 12), 0.6, 0.12, 0.3, seed=int(b0 * 8 + b * 2)), b0 + b, pan=0.45, send=0.2)
                else:
                    m.add(epiano(midi(nn), dur + 0.6, 0.34, 0.8), b0 + b, pan=0.1, send=0.35)
                    if sec == 'B': m.add(flute(midi(nn + 12), dur * 0.95, 0.10, seed=bi), b0 + b, pan=0.35, send=0.45)
            elif weather == 1:
                m.add(flute(midi(nn), dur * 0.95, 0.28, seed=bi * 7 + int(b * 2)), b0 + b, pan=0.1, send=0.4)
                if b == 0 and sec == 'B': m.add(glock(midi(nn + 12), 1.0, 0.06), b0 + b, pan=0.4, send=0.5)
            else:
                if b in (0, 2) or d >= 1.5:     # 비: 음을 솎아 듬성듬성, 한 옥타브 아래로 차분하게
                    m.add(epiano(midi(nn - 12 if phase == 1 else nn), dur + 0.9, 0.26, 0.5), b0 + b, pan=0.1, send=0.55)
        if last: pass
    lp = {0: None, 1: 6500, 2: 3200}[weather]
    y = render(m, rev_sec={0: 1.5, 1: 1.9, 2: 2.4}[weather], rev_gain={0: 0.7, 1: 0.85, 2: 1.0}[weather],
               lowpass=lp, crackle=0.025 if weather == 2 else (0.012 if phase == 2 else 0.0),
               wow=0.0007 if (phase == 2 or weather == 2) else 0.0, seed=phase * 10 + weather)
    return y

def night():
    key, bpm = 65, 66
    m = Mix(bpm, 16, seed=99)
    for bi, (mel, (root, deg), sec) in enumerate(form(night=True)):
        b0 = bi * 4; tones = chord(key + root, deg, False)
        for i, v in enumerate([tones[0], tones[1], tones[2], tones[1]]):
            m.add(musicbox(midi(v), 1.4, 0.14), b0 + i, pan=-0.3, send=0.5)
        m.add(pad([midi(t - 12) for t in tones[:3]], 4 * m.beat, 0.05, 800), b0, send=0.6)
        m.add(bass(midi(key + root - 24), 4 * m.beat, 0.18), b0, send=0.1)
        for (b, n, d) in mel:
            m.add(musicbox(midi(key + 12 + n), d * m.beat + 1.0, 0.36), b0 + b, pan=0.2, send=0.55)
    return render(m, rev_sec=2.6, rev_gain=1.0, lowpass=7000, seed=999)

def save(name, y, target_rms):
    y = master(y, target_rms)
    wav = os.path.join(OUT, name + ".wav")
    wf.write(wav, SR, (np.clip(y, -1, 1) * 32767).astype(np.int16))
    ogg = os.path.join(OUT, name + ".ogg")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "4", ogg], check=True)
    os.remove(wav)
    print(f"{name}: {len(y)/SR:.1f}s rms={np.sqrt(np.mean(y**2)):.3f} peak={np.max(np.abs(y)):.3f}")

if __name__ == "__main__":
    which = sys.argv[2] if len(sys.argv) > 2 else "all"
    RMS = float(os.environ.get("TARGET_RMS", "0.16"))
    if which in ("all", "bgm"):
        for p in range(3):
            for w in range(3):
                save(f"bgm_{p}_{w}", arrange(p, w), RMS * (0.9 if w == 2 else 1.0))
    if which in ("all", "night"):
        save("bgm_night", night(), RMS * 0.85)
