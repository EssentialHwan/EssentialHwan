# BigSoundBank CC0 녹음(이 폴더 bsb_*.ogg) → 게임 효과음/환경음. 출처는 SOURCES.md.
# 사용: python3 build_foley.py <출력 폴더>
import sys, os, subprocess
import numpy as np, soundfile as sf

SR = 44100; OUT = sys.argv[1]; HERE = os.path.dirname(os.path.abspath(__file__))
os.makedirs(OUT, exist_ok=True)

def load(id_, start=0.0, dur=None):
    tmp = os.path.join(OUT, '_t.wav')
    cmd = ['ffmpeg', '-y', '-loglevel', 'error', '-ss', str(start), '-i', os.path.join(HERE, f'bsb_{id_}.ogg')]
    if dur: cmd += ['-t', str(dur)]
    subprocess.run(cmd + ['-ar', str(SR), '-ac', '2', tmp]); a, _ = sf.read(tmp, always_2d=True); os.remove(tmp); return a

def trim(a, thr_db=-42, pad=0.01):  # 앞뒤 무음 자르기
    lv = np.abs(a).max(axis=1); thr = lv.max() * 10 ** (thr_db / 20); nz = np.where(lv > thr)[0]
    i0 = max(0, nz[0] - int(pad * SR)); i1 = min(len(a), nz[-1] + int(0.05 * SR)); return a[i0:i1]

def hp(a, fc):
    from math import pi
    rc = 1 / (2 * pi * fc); al = rc / (rc + 1 / SR); y = np.zeros_like(a)
    for i in range(1, len(a)): y[i] = al * (y[i - 1] + a[i] - a[i - 1])
    return y

def fades(a, fin=0.004, fout=0.05):
    a = a.copy(); i = int(fin * SR); o = int(fout * SR)
    a[:i] *= np.linspace(0, 1, i)[:, None]; a[-o:] *= np.linspace(1, 0, o)[:, None]; return a

def loop(a, xf):  # 끝 xf초를 앞에 겹쳐 이음매 없는 루프
    n = int(xf * SR); body = a[:-n].copy(); tail = a[-n:]
    w = np.linspace(0, 1, n)[:, None]; body[:n] = body[:n] * np.sqrt(w) + tail * np.sqrt(1 - w); return body

def save(name, a, peak_db=None, lufs=None):
    if peak_db is not None: a = a * 10 ** (peak_db / 20) / max(1e-9, np.abs(a).max())
    wav = os.path.join(OUT, name + '.wav'); sf.write(wav, a, SR)
    af = []
    if lufs is not None:
        r = subprocess.run(['ffmpeg', '-nostats', '-i', wav, '-af', 'ebur128', '-f', 'null', '-'], capture_output=True, text=True).stderr
        I = float([l for l in r.splitlines() if l.strip().startswith('I:')][-1].split()[1])
        af = ['-af', f'volume={lufs - I}dB,alimiter=limit=0.9']
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', wav] + af + ['-c:a', 'libvorbis', '-q:a', '6', os.path.join(OUT, name + '.ogg')])
    os.remove(wav); print(name, round(len(a) / SR, 2), 's')

# ── 고양이 ── (짧은 건 피크 -4dB로 통일, 저역 웅웅거림은 100Hz 하이패스로)
# (11/09 환 요청으로 되돌림 — 기존 합성 울음 유지)
# save('sfx_meow_0',     fades(hp(trim(load('1478', 0, 0.80)), 100)), peak_db=-4)
# (11/09 환 요청으로 되돌림 — 기존 합성 울음 유지)
# save('sfx_meow_1',     fades(hp(trim(load('1475', 0, 0.82)), 100)), peak_db=-4)
# (11/09 환 요청으로 되돌림 — 기존 합성 울음 유지)
# save('sfx_meow_2',     fades(hp(trim(load('1479', 0, 0.85)), 100)), peak_db=-4)
# (11/09 환 요청으로 되돌림 — 기존 합성 울음 유지)
# save('sfx_meow_small', fades(hp(trim(load('0494', 0, 0.65)), 120)), peak_db=-5)
# (11/09 환 요청으로 되돌림 — 기존 합성 울음 유지)
# save('sfx_meow_demand', fades(hp(trim(load('1890', 0, 2.05)), 100), fout=0.12), peak_db=-4)
# 골골: 4초 루프(마감 놀이에서 Loop으로 돈다)
save('sfx_purr', fades(loop(hp(load('1010', 20, 4.6), 40), 0.6), fin=0.005, fout=0.005), lufs=-20)   # 양끝 5ms: 한 번 재생 시 딸깍 방지

# ── 주방 ──
save('sfx_pour',   fades(hp(load('1155', 0.3, 1.6), 150), fin=0.08, fout=0.55), peak_db=-6)
save('sfx_sizzle', fades(hp(load('0144', 2.0, 1.5), 400), fin=0.05, fout=0.6), peak_db=-8)

# ── 오프닝 페이지 넘김 ──
save('sfx_page', fades(hp(trim(load('0164', 0, 0.7)), 150), fout=0.08), peak_db=-8)

# ── 환경음(루프) ──
save('amb_rain',   loop(hp(load('1290', 10, 42), 60), 2.0), lufs=-26)
save('amb_cicada', loop(hp(load('2247', 0, 16.5), 200), 1.5), lufs=-28)
