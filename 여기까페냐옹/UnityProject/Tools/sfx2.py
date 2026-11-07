# 1순위 효과음 v2 (문 종 · 코인 · 클릭 · 알림 · 조리 완성) — 직접 합성, 라이선스 없음.
#   python3 sfx2.py <출력폴더>
# v1(sfx.py)보다: 금속 소리를 실제 종처럼 '안 맞는 배음'으로, 짧은 방 울림, 귀를 찌르는 고음 정리.
import sys, os, subprocess
import numpy as np, scipy.signal as sg, scipy.io.wavfile as wf

SR = 32000
OUT = sys.argv[1] if len(sys.argv) > 1 else "sfx2"
os.makedirs(OUT, exist_ok=True)
def T(sec): return np.arange(int(sec * SR)) / SR
def nz(n, seed): return np.random.default_rng(seed).standard_normal(n)
def lp(x, f): return sg.lfilter(*sg.butter(2, f / (SR / 2)), x)
def hp(x, f): return sg.lfilter(*sg.butter(2, f / (SR / 2), 'high'), x)
def bp(x, lo, hi): return sg.lfilter(*sg.butter(2, [lo / (SR / 2), hi / (SR / 2)], 'band'), x)

def place(dst, x, at):
    i = int(at * SR); dst[i:i + len(x)] += x[: max(0, len(dst) - i)]

def room(x, sec=0.6, mix=0.18, seed=3):
    # 작은 카페 안 울림(짧은 잔향)
    n = int(sec * SR); t = np.arange(n) / SR
    ir = lp(nz(n, seed), 4000) * np.exp(-t * 6.9 / sec); ir[: int(0.012 * SR)] = 0
    ir /= np.sqrt(np.sum(ir ** 2))
    wet = sg.fftconvolve(x, ir)[: len(x)]
    return x + wet * mix

def tine(f, sec, partials, decays, amps, seed=0):
    # 종·쇠붙이: 배음이 정수배가 아니라서 '쇠' 느낌이 난다. 아주 짧은 때리는 소리(노이즈)도 같이.
    t = T(sec); x = np.zeros(len(t)); r = np.random.default_rng(seed)
    for p, d, a in zip(partials, decays, amps):
        x += a * np.sin(2 * np.pi * f * p * t + r.uniform(0, 6.28)) * np.exp(-t * d)
    hit = hp(nz(len(t), seed + 1), 3000) * np.exp(-t * 400) * 0.25
    return (x + hit) * np.clip(t / 0.0008, 0, 1)

def door_bell():
    # 가게 문에 달린 작은 놋쇠 종 2~3개가 '딸랑딸랑' — 문이 열리며 흔들리는 박자(점점 약하게)
    y = np.zeros(int(1.6 * SR))
    bells = [(2093, 0), (2489, 1), (2794, 2)]      # C7 · D#7 · F7 근처, 살짝씩 다른 종
    hits = [(0.00, 0, 1.0), (0.06, 1, 0.8), (0.13, 2, 0.7), (0.22, 0, 0.55), (0.30, 1, 0.45), (0.42, 2, 0.3), (0.55, 0, 0.18)]
    for k, (at, b, v) in enumerate(hits):
        f = bells[b][0] * (1 + np.random.default_rng(k).uniform(-0.004, 0.004))
        place(y, tine(f, 1.0, (1, 2.32, 4.25, 6.63), (4.5, 7, 13, 20), (1, 0.45, 0.22, 0.1), seed=k) * v, at)
    return room(lp(y, 9000), 0.7, 0.22)

def coin():
    # 작은 동전 두 개가 '짤랑' — 두 음(B6 → E7), 두 번째가 길게 남음
    y = np.zeros(int(0.8 * SR))
    place(y, tine(1976, 0.25, (1, 2.71, 5.1), (16, 25, 40), (1, 0.4, 0.15), seed=11) * 0.75, 0.0)
    place(y, tine(2637, 0.7, (1, 2.71, 5.1), (6, 11, 22), (1, 0.4, 0.15), seed=12), 0.075)
    return room(lp(y, 9500), 0.45, 0.15)

def click():
    # 버튼: 귀에 안 거슬리는 나무 '톡' (짧은 공명 + 아주 짧은 때림)
    t = T(0.06)
    body = np.sin(2 * np.pi * 1150 * t) * np.exp(-t * 110) + 0.4 * np.sin(2 * np.pi * 2300 * t) * np.exp(-t * 180)
    tick = bp(nz(len(t), 5), 1500, 5000) * np.exp(-t * 600) * 0.35
    return (body + tick) * np.clip(t / 0.0005, 0, 1)

def boop():
    # 알림: 물방울처럼 '뽁' — 음이 살짝 올라가는 둥근 소리
    t = T(0.14)
    f = 620 + 420 * (1 - np.exp(-t * 45))
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 26) + 0.2 * np.sin(2 * np.pi * np.cumsum(2 * f) / SR) * np.exp(-t * 40)
    return room(x * np.clip(t / 0.002, 0, 1), 0.35, 0.12)

def chime():
    # 요리 완성: 실로폰 도-미-솔 '띠로링' (밝게, 짧게)
    y = np.zeros(int(1.3 * SR))
    for k, (f, at) in enumerate([(1047, 0.0), (1319, 0.085), (1568, 0.17), (2093, 0.255)]):
        v = 0.75 if k < 3 else 1.0
        place(y, tine(f, 1.0 if k == 3 else 0.5, (1, 2.76, 5.4), (5 if k == 3 else 9, 9, 18), (1, 0.35, 0.12), seed=20 + k) * v, at)
    return room(lp(y, 9000), 0.6, 0.2)

def save(name, y, peak):
    y = y / (np.max(np.abs(y)) + 1e-9) * peak
    fade = int(0.01 * SR); y[-fade:] *= np.linspace(1, 0, fade)
    wav = os.path.join(OUT, name + ".wav"); wf.write(wav, SR, (y * 32767).astype(np.int16))
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "5", os.path.join(OUT, name + ".ogg")], check=True)
    os.remove(wav); print(name, f"{len(y)/SR:.2f}s rms={np.sqrt(np.mean(y**2)):.3f}")

# 옛 소리와 비슷한 최대 음량(약 0.5)에 맞춰 게임 안 음량 균형을 유지
save("sfx_bell", door_bell(), 0.55)
save("sfx_coin", coin(), 0.62)
save("sfx_click", click(), 0.45)
save("sfx_boop", boop(), 0.48)
save("sfx_chime", chime(), 0.46)
