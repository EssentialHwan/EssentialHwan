# 고양이 목소리·행동 소리 합성 v2 (더 실제 같고 더 조용하게).
# 성대 펄스(배음 기울기 + 떨림) → 시간에 따라 바뀌는 포먼트 공명(입 모양) → 숨소리.
# 사용: python3 catvoice.py <출력폴더>   (Resources/Audio 에 같은 이름으로 덮어쓰기)
import numpy as np, scipy.signal as sg, scipy.io.wavfile as wf, subprocess, os, sys
SR = 32000
OUT = sys.argv[1] if len(sys.argv) > 1 else "catvoice"
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(7)

def T(sec): return np.arange(int(sec * SR)) / SR
def nz(n, seed=None): return (np.random.default_rng(seed) if seed is not None else rng).standard_normal(n)
def bp(x, lo, hi, order=2): b, a = sg.butter(order, [lo / (SR / 2), min(0.98, hi / (SR / 2))], 'band'); return sg.lfilter(b, a, x)
def lp(x, f, order=2): b, a = sg.butter(order, f / (SR / 2)); return sg.lfilter(b, a, x)
def hp(x, f, order=2): b, a = sg.butter(order, f / (SR / 2), 'high'); return sg.lfilter(b, a, x)
def env(n, pts, vals): u = np.linspace(0, 1, n); return np.interp(u, pts, vals)
def sil(sec): return np.zeros(int(sec * SR))

def glottal(f0):
    """f0(샘플별) → 성대 펄스열. 배음이 위로 갈수록 약해지고(-12dB/oct 근처) 미세한 떨림·흔들림."""
    n = len(f0)
    jit = 1 + 0.008 * lp(nz(n), 30) * 8                    # 지터(주기 흔들림)
    ph = np.cumsum(f0 * jit) / SR
    x = np.zeros(n)
    for k in range(1, 30):
        fk = f0 * k
        ok = fk < SR / 2 - 500
        x += np.where(ok, np.sin(2 * np.pi * k * ph) / k ** 1.25, 0)
    shim = 1 + 0.06 * lp(nz(n), 40) * 6                     # 시머(크기 흔들림)
    return x * shim, ph

def resonate(x, F, bw):
    """시간 변화 2차 공명기 하나(대역 통과)."""
    out = np.zeros_like(x); z1 = z2 = 0.0; blk = 32
    for s in range(0, len(x), blk):
        f = F[min(s, len(F) - 1)]
        r = np.exp(-np.pi * bw / SR); th = 2 * np.pi * f / SR
        a1, a2 = -2 * r * np.cos(th), r * r
        g = (1 - r * r) * 0.5
        for i in range(s, min(s + blk, len(x))):
            v = g * x[i] - a1 * z1 - a2 * z2
            out[i] = v; z2 = z1; z1 = v
    return out

def formants(x, tracks, bws, gains=(1.0, 0.8, 0.5)):
    """포먼트 공명기를 병렬로 더함(배음이 살아 있는 목소리) + 원음 조금."""
    y = sum(g * resonate(x, F, bw) for F, bw, g in zip(tracks, bws, gains))
    y = y + 0.22 * x
    return y / (np.max(np.abs(y)) + 1e-9)

def voice(dur, f0pts, F1, F2, F3=2900, amp=((0, .1, .8, 1), (0, 1, .8, 0)), breath=0.08, nasal=0.0, roll=0.0, roll_hz=27):
    """f0pts/F1/F2/F3: (시점들, 값들) 또는 상수. nasal: 0~1 입 닫힘(콧소리). roll: 혀 굴림(트릴) 깊이."""
    n = int(dur * SR)
    def tr(p):
        if isinstance(p, (int, float)): return np.full(n, float(p))
        return env(n, p[0], p[1])
    f0 = tr(f0pts) * (1 + 0.01 * np.sin(2 * np.pi * 5.5 * np.arange(n) / SR))   # 살짝 비브라토
    src, ph = glottal(f0)
    asp = hp(nz(n), 1800) * breath * (0.6 + 0.4 * np.cos(2 * np.pi * ph))          # 성대 주기에 맞춘 숨소리
    y = formants(src + asp, [tr(F1), tr(F2), tr(F3)], [130, 180, 280])
    if nasal > 0: y = (1 - nasal) * y + nasal * lp(y, 900, 3) * 2.2
    if roll > 0:
        m = 0.5 + 0.5 * np.sin(2 * np.pi * roll_hz * np.arange(n) / SR)
        y *= (1 - roll) + roll * m ** 1.5
    a = env(n, *amp)
    y = y * a
    y += hp(nz(n), 3000) * 0.015 * a                                                  # 아주 옅은 바람소리
    return y

# ── 목소리 ──
def meow(dur=0.6, lo=480, hi=720, end=420, open_=1.0, seed=0):
    # m(입 닫힘) → i/e(벌어짐) → a(활짝) → u/w(오므림)
    return voice(dur, ((0, .18, .5, 1), (lo * .9, hi, hi * .95, end)),
                 F1=((0, .15, .45, .8, 1), (320, 650 * open_, 950 * open_, 700, 450)),
                 F2=((0, .15, .45, .8, 1), (1500, 2100, 1500, 1050, 850)),
                 F3=((0, 1), (3000, 2700)),
                 amp=((0, .12, .55, .85, 1), (0, .9, 1, .6, 0)), breath=0.1)

def mew(dur=0.3, f=820):            # 아기 고양이처럼 작고 높은 '먀'
    return voice(dur, ((0, .3, 1), (f * .95, f * 1.1, f * .9)),
                 F1=((0, .3, 1), (500, 900, 700)), F2=((0, .3, 1), (2200, 1800, 1300)),
                 amp=((0, .15, .7, 1), (0, 1, .7, 0)), breath=0.12)

def trill(dur=0.32, lo=340, hi=520):  # 입 다물고 '브르릅↗' (반가움)
    return voice(dur, ((0, 1), (lo, hi)), F1=330, F2=((0, 1), (1000, 1250)), F3=2600,
                 amp=((0, .12, .8, 1), (0, 1, .85, 0)), breath=0.05, nasal=0.75, roll=0.55, roll_hz=29)

def mrrng(dur=0.42):                 # 캣타워에서 내려올 때 '으응~' (힘주는 콧소리)
    return voice(dur, ((0, .35, 1), (250, 310, 215)), F1=((0, 1), (300, 360)), F2=((0, 1), (950, 820)), F3=2400,
                 amp=((0, .15, .7, 1), (0, 1, .75, 0)), breath=0.06, nasal=0.85, roll=0.2, roll_hz=22)

def mrrow(dur=0.5):                  # 인사 '므르아앙' (트릴로 시작해 입을 엶)
    a = voice(dur, ((0, .3, .7, 1), (360, 470, 560, 430)),
              F1=((0, .3, .6, 1), (320, 360, 850, 520)), F2=((0, .3, .6, 1), (1050, 1200, 1500, 950)),
              amp=((0, .1, .8, 1), (0, 1, .9, 0)), breath=0.08, nasal=0.0)
    r = 0.5 + 0.5 * np.sin(2 * np.pi * 28 * np.arange(len(a)) / SR)
    k = env(len(a), (0, .3, .45, 1), (0.6, 0.6, 0, 0))
    return a * ((1 - k) + k * r ** 1.5)

def chatter():                       # 창밖 새 보고 '깍깍깍' 채터링
    parts = []
    for i in range(6):
        parts.append(voice(0.055, ((0, 1), (620, 560)), F1=((0, .5, 1), (400, 800, 400)), F2=1600,
                           amp=((0, .2, .6, 1), (0, 1, .6, 0)), breath=0.25))
        parts.append(sil(0.045 + 0.01 * (i % 2)))
    return np.concatenate(parts)

def hiss():                          # '하악' — 짧고 낮게
    n = int(0.55 * SR)
    x = bp(nz(n, 3), 1800, 7500, 3) + bp(nz(n, 4), 600, 1500) * 0.25
    return x * env(n, (0, .04, .25, 1), (0, 1, .7, 0))

def purr(sec=3.0):
    """그르릉: 성대 근육이 초당 ~26번 떨림. 내쉼(크고 낮게)·들이쉼(작고 살짝 높게) 반복. 처음과 끝이 이어지게."""
    n = int(sec * SR); t = np.arange(n) / SR
    cyc = 1.5                                                    # 내쉼+들이쉼 한 주기(초)
    phase = (t % cyc) / cyc
    exh = phase < 0.58
    rate = np.where(exh, 25.5, 27.5)
    ph = np.cumsum(rate) / SR
    pulse = (0.5 + 0.5 * np.cos(2 * np.pi * ph)) ** 6            # 떨림 펄스
    body = lp(nz(n, 11), 700, 3) * 2.5 + np.sin(2 * np.pi * np.cumsum(rate * 4) / SR) * 0.3
    x = body * pulse
    x = formants(x, [np.full(n, 160.0), np.full(n, 520.0)], [120, 300], (1.0, 0.5)) * 0.7 + lp(x, 400) * 0.6
    breath = np.where(exh, 1.0, 0.55)
    breath = lp(breath, 6)                                       # 경계 부드럽게
    gap = np.minimum(1, np.abs(phase - 0.58) * 40) * np.minimum(1, np.minimum(phase, 1 - phase) * 40)
    return x * breath * (0.3 + 0.7 * gap)

# ── 몸 소리 ──
def thud(f=70, sec=0.14, soft=1.0, seed=0):
    t = T(sec)
    x = np.sin(2 * np.pi * f * t * (1 - 1.5 * t)) * np.exp(-t * 35) + lp(nz(len(t), seed), 400) * 0.35 * np.exp(-t * 50)
    return lp(x, 900 * soft)

def zoom():                          # 우다다: 작게 '도도도독'
    r = np.random.default_rng(5); out = sil(0.75)
    pos = 0.0
    while pos < 0.62:
        i = int(pos * SR); s = thud(r.uniform(65, 95), 0.08, 0.6, int(r.integers(1e6))) * r.uniform(0.5, 1)
        out[i:i + len(s)] += s[:len(out) - i]
        pos += r.uniform(0.055, 0.09)
    return out * env(len(out), (0, .1, .8, 1), (0.6, 1, 1, 0.5))

def claw(seed, sec=0.09):            # 발톱이 기둥(삼줄)을 긁는 '득'
    n = int(sec * SR); r = np.random.default_rng(seed)
    grain = (r.random(n) < 0.08) * r.uniform(0.5, 1, n)              # 섬유에 걸리는 알갱이
    x = bp(grain + nz(n, seed) * 0.15, 1500, 5000, 2)
    return x * np.sin(np.pi * np.arange(n) / n)

def climb():                         # 캣타워 오르기: 박 차고 + 발톱 득득 + 툭
    out = sil(0.7)
    def put(at, s, g=1.0):
        i = int(at * SR); out[i:i + len(s)] += s[:len(out) - i] * g
    put(0.0, thud(90, 0.1, 0.8, 1), 0.6)
    put(0.12, claw(1), 0.25); put(0.21, claw(2), 0.3); put(0.3, claw(3, 0.07), 0.2)
    put(0.45, thud(75, 0.14, 0.7, 2), 0.8)
    return out

def descend():                       # 내려오기: 살짝 '휙' + '툭'
    n = int(0.12 * SR)
    w = bp(nz(n, 9), 400, 2200) * np.sin(np.pi * np.arange(n) / n) * 0.25
    return np.concatenate([w, thud(65, 0.18, 0.7, 3)])

def land(): return thud(70, 0.16, 0.7, 4)

def squeak(seed, count=2):           # 쥐 '찍찍'
    r = np.random.default_rng(seed); parts = []
    for _ in range(count):
        d = r.uniform(0.04, 0.07); n = int(d * SR); u = np.linspace(0, 1, n)
        f = np.interp(u, [0, .3, 1], [4200, 5600, 4600]) * r.uniform(0.92, 1.08)
        ph = 2 * np.pi * np.cumsum(f) / SR
        s = (np.sin(ph) + 0.35 * np.sin(2 * ph) + 0.15 * np.sin(3 * ph)) * np.sin(np.pi * u) ** 0.7
        s += bp(nz(n), 3000, 8000) * 0.12 * np.sin(np.pi * u)
        parts += [s, sil(r.uniform(0.035, 0.06))]
    return np.concatenate(parts)

def scurry():                        # 쥐 발소리 '사사삭'
    out = sil(0.45); r = np.random.default_rng(12)
    for k in range(10):
        i = int(k * 0.04 * SR); L = int(0.018 * SR)
        out[i:i + L] += bp(nz(L, k), 2500, 8000) * np.sin(np.pi * np.arange(L) / L) * r.uniform(0.4, 1)
    return out

def wheel(sec=2.4):                  # 캣휠: 굴러가는 '우우웅' + 받침 삐걱/딸깍 (루프)
    n = int(sec * SR); t = np.arange(n) / SR
    rot = 1.25                                                   # 초당 회전
    rumble = lp(nz(n, 21), 160, 3) * 3 * (0.8 + 0.2 * np.sin(2 * np.pi * rot * t))
    clicks = np.zeros(n)
    for k in range(int(sec * rot * 8)):                          # 바퀴살 지날 때 딸깍
        i = int(k / (rot * 8) * SR); L = int(0.01 * SR)
        if i + L < n: clicks[i:i + L] += bp(nz(L, k), 1500, 5000) * np.exp(-np.arange(L) / L * 4) * 0.25
    paws = np.zeros(n)
    for k in range(int(sec / 0.11)):                             # 안에서 뛰는 발
        i = int(k * 0.11 * SR); s = thud(80, 0.06, 0.5, k) * 0.35
        paws[i:i + len(s)] += s[:n - i]
    x = rumble + clicks + paws
    return x

def save(name, y, peak=0.5):
    """peak: 최대 크기(0~1). 고양이 소리는 이전(0.8)보다 확 낮게."""
    y = np.asarray(y, float)
    y = y - np.mean(y)
    y = y / (np.max(np.abs(y)) + 1e-9) * peak
    fl = int(0.004 * SR); y[:fl] *= np.linspace(0, 1, fl); y[-fl:] *= np.linspace(1, 0, fl)
    wav = os.path.join(OUT, name + ".wav")
    wf.write(wav, SR, (np.clip(y, -1, 1) * 32767).astype(np.int16))
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "5", os.path.join(OUT, name + ".ogg")], check=True)
    os.remove(wav)

if __name__ == '__main__':
    save("sfx_meow_0", meow(0.55, 470, 700, 420), 0.42)
    save("sfx_meow_1", meow(0.42, 560, 780, 560, 0.9), 0.4)
    save("sfx_meow_2", meow(0.7, 430, 640, 380, 1.05), 0.42)
    save("sfx_meow_small", mew(), 0.3)
    save("sfx_meow_demand", np.concatenate([meow(0.45, 520, 760, 560), sil(0.12), meow(0.62, 540, 820, 470, 1.1)]), 0.45)
    save("sfx_trill", trill(), 0.38)
    save("sfx_trill_1", trill(0.26, 380, 600), 0.36)
    save("sfx_mrrow", mrrow(), 0.4)
    save("sfx_mrrng", mrrng(), 0.34)
    save("sfx_chatter", chatter(), 0.3)
    save("sfx_hiss", hiss(), 0.38)
    save("sfx_purr", purr(), 0.32)
    save("sfx_zoom", zoom(), 0.28)
    save("sfx_climb", climb(), 0.36)
    save("sfx_descend", descend(), 0.34)
    save("sfx_land", land(), 0.32)
    save("sfx_squeak", squeak(1), 0.3); save("sfx_squeak_2", squeak(2, 3), 0.3)
    save("sfx_scurry", scurry(), 0.22)
    save("sfx_wheel", wheel(), 0.28)
    print("ok", sorted(os.listdir(OUT)))
