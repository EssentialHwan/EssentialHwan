# 효과음 합성(고양이 행동·주방·UI). 모두 직접 합성 → 정식 사운드로 교체 전까지 쓰는 임시음.
import numpy as np, scipy.signal as sg, scipy.io.wavfile as wf, subprocess, os, sys
SR = 32000
OUT = sys.argv[1] if len(sys.argv) > 1 else "sfx"
os.makedirs(OUT, exist_ok=True)

def T(sec): return np.arange(int(sec * SR)) / SR
def nz(n, seed=0): return np.random.default_rng(seed).standard_normal(n)
def bp(x, lo, hi, order=2): b, a = sg.butter(order, [lo / (SR / 2), min(0.98, hi / (SR / 2))], 'band'); return sg.lfilter(b, a, x)
def lp(x, f, order=2): b, a = sg.butter(order, f / (SR / 2)); return sg.lfilter(b, a, x)
def hp(x, f, order=2): b, a = sg.butter(order, f / (SR / 2), 'high'); return sg.lfilter(b, a, x)
def fade(x, a=0.005, r=0.03):
    n = len(x); ai = int(a * SR); ri = int(r * SR)
    if ai: x[:ai] *= np.linspace(0, 1, ai)
    if ri: x[-ri:] *= np.linspace(1, 0, ri)
    return x

def meow(seed, dur=0.55, f_lo=520, f_hi=820, end=480):
    # 성대(배음열) + 시간에 따라 움직이는 포먼트(이→아→우) = '미야옹'
    r = np.random.default_rng(seed)
    t = T(dur); n = len(t); u = t / dur
    f0 = np.interp(u, [0, 0.25, 0.6, 1], [f_lo, f_hi, f_hi * 0.96, end]) * (1 + 0.012 * np.sin(2 * np.pi * 6 * t))
    ph = 2 * np.pi * np.cumsum(f0) / SR
    F1 = np.interp(u, [0, 0.3, 0.7, 1], [450, 950, 900, 520])
    F2 = np.interp(u, [0, 0.3, 0.7, 1], [2300, 1500, 1300, 950])
    x = np.zeros(n)
    for k in range(1, 14):
        fk = f0 * k
        w = np.exp(-((fk - F1) / 260) ** 2) + 0.7 * np.exp(-((fk - F2) / 380) ** 2) + 0.05
        x += np.sin(k * ph) * w / k ** 0.3
    x += hp(nz(n, seed), 2500) * 0.02                      # 숨결
    env = np.interp(u, [0, 0.08, 0.7, 1], [0, 1, 0.8, 0])
    return x * env

def purr(sec=1.6):
    t = T(sec); n = len(t)
    rate = 26
    pulses = (0.5 + 0.5 * np.sin(2 * np.pi * rate * t)) ** 3
    x = lp(nz(n, 3), 400) * 3 + np.sin(2 * np.pi * 52 * t) * 0.4
    x *= pulses * (0.75 + 0.25 * np.sin(2 * np.pi * 0.6 * t))
    return fade(x, 0.08, 0.15)

def hiss():
    t = T(0.7); n = len(t)
    x = bp(nz(n, 4), 2500, 9000, 3)
    env = np.interp(t / 0.7, [0, 0.05, 0.6, 1], [0, 1, 0.7, 0])
    return x * env

def trill():
    # 기분 좋은 '브르릉~'(위로 올라가는 짧은 트릴)
    t = T(0.35); u = t / 0.35
    f0 = np.interp(u, [0, 1], [380, 620])
    ph = 2 * np.pi * np.cumsum(f0) / SR
    x = sum(np.sin(k * ph) / k for k in range(1, 7))
    x *= 0.6 + 0.4 * np.sign(np.sin(2 * np.pi * 32 * t))   # 혀 굴림
    return lp(x, 3000) * np.interp(u, [0, 0.1, 0.8, 1], [0, 1, 0.8, 0])

def crunch(seed):
    n = int(0.35 * SR); x = np.zeros(n); r = np.random.default_rng(seed)
    for _ in range(5):
        i = r.integers(0, n - 2000); L = r.integers(300, 1200)
        x[i:i + L] += bp(nz(L, r.integers(1e6)), 1200, 6000) * np.exp(-np.arange(L) / SR * 60) * r.uniform(0.4, 1)
    return x

def lick(seed):
    n = int(0.12 * SR); t = np.arange(n) / SR
    return bp(nz(n, seed), 800, 3000) * np.exp(-t * 50) * (1 + np.sin(2 * np.pi * 40 * t))

def whoosh(sec=0.25, lo=300, hi=2500):
    t = T(sec); u = t / sec
    x = nz(len(t), 7)
    out = np.zeros_like(x)
    for i, f in enumerate(np.linspace(lo, hi, 8)):
        seg = slice(int(i * len(t) / 8), int((i + 1) * len(t) / 8))
        out[seg] = bp(x, f * 0.7, f * 1.3)[seg]
    return out * np.sin(np.pi * u)

def thump(f=90, sec=0.18):
    t = T(sec)
    return (np.sin(2 * np.pi * f * t * (1 - t)) + lp(nz(len(t), 8), 600) * 0.4) * np.exp(-t * 28)

def patpat():
    t = T(0.11)
    x = lp(nz(len(t), 11), 900) * 2 + np.sin(2 * np.pi * 140 * t)
    return x * np.exp(-t * 40)

def suck():
    t = T(0.14)
    x = bp(nz(len(t), 12), 300, 1800) * np.exp(-t * 30)
    x += np.sin(2 * np.pi * np.interp(t, [0, 0.14], [900, 300]) * t) * np.exp(-t * 40) * 0.5   # '쭙'
    return x

def scratch():
    n = int(0.6 * SR); x = np.zeros(n)
    for k in range(4):
        i = int(k * 0.14 * SR); L = int(0.1 * SR)
        x[i:i + L] += bp(nz(L, 20 + k), 1500, 7000) * np.sin(np.pi * np.arange(L) / L)
    return x

def squeak(seed, n_ch=2):
    r = np.random.default_rng(seed); parts = []
    for _ in range(n_ch):
        t = T(r.uniform(0.05, 0.09)); u = t / t[-1]
        f = np.interp(u, [0, 0.5, 1], [3200, 4300, 3600]) * r.uniform(0.9, 1.1)
        parts.append(np.sin(2 * np.pi * np.cumsum(f) / SR) * np.sin(np.pi * u)); parts.append(np.zeros(int(0.04 * SR)))
    return np.concatenate(parts)

def box():
    n = int(0.4 * SR); x = np.zeros(n); r = np.random.default_rng(30)
    for _ in range(9):
        i = r.integers(0, n - 1500); L = r.integers(400, 1400)
        x[i:i + L] += bp(nz(L, r.integers(1e6)), 400, 3500) * np.exp(-np.arange(L) / SR * 25)
    return x + np.pad(thump(70, 0.15), (0, n - int(0.15 * SR)))[:n] * 0.6

def bell(f=1320, sec=0.9, partials=(1, 2.76, 5.4)):
    t = T(sec)
    return sum(np.sin(2 * np.pi * f * p * t) * np.exp(-t * (2.5 + 3 * i)) / (i + 1) for i, p in enumerate(partials))

def click():
    t = T(0.05)
    return (np.sin(2 * np.pi * 1700 * t) * 0.5 + bp(nz(len(t), 40), 2000, 6000) * 0.4) * np.exp(-t * 120)

def boop():
    t = T(0.12)
    return np.sin(2 * np.pi * np.interp(t, [0, 0.12], [660, 990]) * t) * np.exp(-t * 25)

def coin():
    a = bell(1975, 0.18, (1, 2.4)); b = bell(2637, 0.5, (1, 2.4))
    return np.concatenate([a, b])

def sizzle():
    t = T(0.9)
    return hp(nz(len(t), 50), 3000) * (0.5 + 0.5 * np.abs(np.sin(2 * np.pi * 7 * t))) * np.interp(t, [0, 0.1, 0.7, 0.9], [0, 1, 0.8, 0]) * 0.5

def pour():
    t = T(0.8)
    x = bp(nz(len(t), 51), 500, 2500) * (1 + 0.5 * np.sin(2 * np.pi * 13 * t))
    return x * np.interp(t, [0, 0.1, 0.6, 0.8], [0, 1, 0.8, 0]) * 0.6

def yawn():
    t = T(0.8); u = t / 0.8
    f0 = np.interp(u, [0, 0.4, 1], [420, 600, 300])
    ph = 2 * np.pi * np.cumsum(f0) / SR
    x = sum(np.sin(k * ph) * np.exp(-((k * f0 - 800) / 400) ** 2) for k in range(1, 10))
    return lp(x, 2500) * np.interp(u, [0, 0.2, 0.8, 1], [0, 1, 0.6, 0]) * 0.6

def stamp():
    return thump(70, 0.25) + np.pad(click(), (0, int(0.25 * SR) - int(0.05 * SR)))[:int(0.25 * SR)] * 0.4

def save(name, y, gain=0.8):
    y = np.asarray(y, float)
    y = y / (np.max(np.abs(y)) + 1e-9) * gain
    wav = os.path.join(OUT, name + ".wav")
    wf.write(wav, SR, (np.clip(y, -1, 1) * 32767).astype(np.int16))
    ogg = os.path.join(OUT, name + ".ogg")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "4", ogg], check=True)
    os.remove(wav)

save("sfx_meow_0", meow(1)); save("sfx_meow_1", meow(2, 0.42, 600, 900, 620)); save("sfx_meow_2", meow(3, 0.7, 480, 760, 420))
save("sfx_meow_small", meow(4, 0.28, 650, 820, 700), 0.6)
save("sfx_meow_demand", np.concatenate([meow(5, 0.45, 560, 900, 600), np.zeros(int(0.08 * SR)), meow(6, 0.6, 600, 980, 520)]))
save("sfx_purr", purr(), 0.6)
save("sfx_hiss", hiss(), 0.7)
save("sfx_trill", trill(), 0.7)
save("sfx_eat_0", crunch(1), 0.6); save("sfx_eat_1", crunch(2), 0.6)
save("sfx_lick", lick(3), 0.5)
save("sfx_jump", whoosh(0.22), 0.5)
save("sfx_land", thump(80), 0.6)
save("sfx_pounce", np.concatenate([whoosh(0.15, 600, 3000), thump(100, 0.12)]), 0.6)
save("sfx_swat", whoosh(0.12, 1000, 4000), 0.6)
save("sfx_knead", patpat(), 0.5)
save("sfx_suck", suck(), 0.5)
save("sfx_scratch", scratch(), 0.5)
save("sfx_squeak", squeak(1), 0.5); save("sfx_squeak_2", squeak(2, 3), 0.5)
save("sfx_catch", np.concatenate([whoosh(0.1, 800, 3000), thump(120, 0.1), squeak(3, 1)]), 0.7)
save("sfx_box", box(), 0.6)
save("sfx_yawn", yawn(), 0.5)
save("sfx_click", click(), 0.5)
save("sfx_boop", boop(), 0.5)
save("sfx_coin", coin(), 0.5)
save("sfx_bell", bell(), 0.5)
save("sfx_sizzle", sizzle(), 0.5)
save("sfx_pour", pour(), 0.5)
save("sfx_stamp", stamp(), 0.7)
save("sfx_chime", np.concatenate([bell(1568, 0.25), bell(2093, 0.8)]), 0.45)
print("ok", len(os.listdir(OUT)))
