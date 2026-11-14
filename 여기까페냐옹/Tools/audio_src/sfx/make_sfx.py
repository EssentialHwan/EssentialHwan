# 손맛 효과음 라인업(11/09): 정답·오답(일부정답)·서빙·요리완성·입장·하트·골드.
# 음정 있는 소리는 BGM과 같은 F장조(F·A·C)로 맞춰 배경음 위에서 튀지 않게 한다.
# 재료: fluidsynth + GeneralUser GS(상업 무료) 악기음 + earshot 내장 효과음 뱅크(코드 합성, 저작권 없음).
# 사용: python3 make_sfx.py <sf2 경로> <출력 폴더>
import sys, os, subprocess, tempfile
import numpy as np, soundfile as sf, mido

SR = 44100
SF2, OUT = sys.argv[1], sys.argv[2]
TMP = tempfile.mkdtemp()
os.makedirs(OUT, exist_ok=True)
NOTE = {'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11}
def midi(n):  # 'A5' → 81, 'Bb4' → 70
    k = NOTE[n[0]] + (1 if '#' in n else -1 if 'b' in n[1:] else 0)
    return 12 * (int(n[-1]) + 1) + k

def inst(program, notes, length, vel=90, ch=0):
    """notes: [(start_s, 'F5', dur_s, vel_scale)] → numpy stereo"""
    mid = mido.MidiFile(ticks_per_beat=480); tr = mido.MidiTrack(); mid.tracks.append(tr)
    tr.append(mido.MetaMessage('set_tempo', tempo=500000))  # 1박 = 0.5초 → 1초 = 960틱
    tr.append(mido.Message('program_change', channel=ch, program=program, time=0))
    tr.append(mido.Message('control_change', channel=ch, control=91, value=40, time=0))
    ev = []
    for s, n, d, vs in notes:
        ev.append((int(s * 960), 1, mido.Message('note_on', channel=ch, note=midi(n), velocity=int(min(127, vel * vs)))))
        ev.append((int((s + d) * 960), 0, mido.Message('note_off', channel=ch, note=midi(n), velocity=0)))
    ev.sort(key=lambda e: (e[0], e[1])); last = 0
    for t, _, m in ev: m.time = t - last; last = t; tr.append(m)
    tr.append(mido.MetaMessage('end_of_track', time=int(length * 960) - last + 960))
    p = os.path.join(TMP, 'm.mid'); w = os.path.join(TMP, 'm.wav'); mid.save(p)
    subprocess.run(['fluidsynth', '-ni', '-C', '0', '-g', '0.8', '-r', str(SR), '-F', w, SF2, p], capture_output=True)
    a, _ = sf.read(w, always_2d=True)
    return fit(a, length)

def bank(sound, length, pitch=None, seed=None):
    w = os.path.join(TMP, 'b.wav')
    cmd = ['earshot', 'sfx', 'render', sound, '-o', w, '--sr', str(SR)]
    if pitch: cmd += ['--pitch', pitch]
    if seed is not None: cmd += ['--seed', str(seed)]
    subprocess.run(cmd, capture_output=True, env={**os.environ, 'PATH': os.path.expanduser('~/.local/bin') + ':' + os.environ['PATH']})
    a, _ = sf.read(w, always_2d=True)
    if a.shape[1] == 1: a = np.repeat(a, 2, axis=1)
    return fit(a, length)

def fit(a, length):
    n = int(length * SR)
    return a[:n] if len(a) >= n else np.vstack([a, np.zeros((n - len(a), a.shape[1]))])

def at(a, t, length):  # t초 뒤에 놓기
    z = np.zeros((int(length * SR), 2)); i = int(t * SR); k = min(len(a), len(z) - i)
    z[i:i + k] = a[:k]; return z

def hp(a, fc):  # 1차 하이패스
    from math import pi
    rc = 1 / (2 * pi * fc); dt = 1 / SR; al = rc / (rc + dt); y = np.zeros_like(a)
    for i in range(1, len(a)): y[i] = al * (y[i - 1] + a[i] - a[i - 1])
    return y

def finish(name, mix, peak_db=-3.0, fade=0.06):
    m = mix.copy()
    nz = np.where(np.abs(m).max(axis=1) > 1e-4)[0]
    m = m[nz[0]:] if len(nz) else m                        # 앞 무음 제거 → 반응 즉시
    f = int(fade * SR); m[-f:] *= np.linspace(1, 0, f)[:, None]
    g = int(0.003 * SR); m[:g] *= np.linspace(0, 1, g)[:, None]        # 시작 클릭 방지
    m *= 10 ** (peak_db / 20) / max(1e-9, np.abs(m).max())
    wav = os.path.join(TMP, name + '.wav'); sf.write(wav, m, SR)
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', wav, '-c:a', 'libvorbis', '-q:a', '6', os.path.join(OUT, name + '.ogg')])
    print(name, round(len(m) / SR, 2), 's')

# 1) 정답(전부 맞힘): 칼림바+첼레스타가 F-A-C-F로 올라가고 끝에 반짝
L = 1.5
arp = [(0.00, 'F5', 0.9, .9), (0.07, 'A5', 0.9, .95), (0.14, 'C6', 0.9, 1.0), (0.22, 'F6', 1.1, 1.05)]
finish('sfx_correct', inst(108, arp, L) * 0.9 + inst(8, arp, L, vel=70) * 0.6
       + at(inst(9, [(0, 'A6', .6, 1), (0.05, 'C7', .6, .8)], 1.0, vel=55), 0.30, L) * 0.5, fade=0.3)

# 2) 일부만 맞음(연필 ○): 짧게 끊은 현 피치카토 D4→C4 + 나무 톡톡. 벌주는 느낌 없이 '음… 아직'
#    (처음엔 마림바 두 음이었는데 earshot이 '초인종 딩동'으로 읽어서 짧고 둔하게 바꿈)
L = 0.6
finish('sfx_wrong', inst(45, [(0, 'D4', .12, 1), (0.13, 'C4', .14, .9)], L, vel=95)
       + bank('knock/double', L) * 0.3, peak_db=-5, fade=0.2)

# 3) 서빙: 접시 놓는 '딸깍' + 받침 나무 '톡' + 아주 작은 마림바 C6
L = 0.5
#    (처음 버전은 '킥드럼'으로 읽혀서 저음 탭을 빼고 높은 딸깍으로)
srv = bank('knock/snap', L) * 0.7 + bank('pro_click/glass', L) * 0.6 + at(inst(12, [(0, 'C6', .25, 1)], L, vel=60), 0.02, L) * 0.5
srv = hp(srv, 350)
finish('sfx_serve', srv, peak_db=-4)

# 4) 요리 완성: 주방 '띵!' — 맑은 딩(C7) + 칼림바 C6·G6 화음
L = 1.2
finish('sfx_cookdone', bank('pro_notify/ding_2045', L) * 0.7
       + inst(108, [(0, 'C6', 1, 1), (0.01, 'G6', 1, .8)], L, vel=85) * 0.8, peak_db=-3)

# 5) 고양이 입장: 문 종 '딸랑딸랑' — 틴클벨 트레몰로 + 작은 종
L = 1.1
bell = [(0.00, 'A6', .5, 1), (0.08, 'C7', .5, .8), (0.16, 'A6', .5, .7), (0.25, 'F6', .7, .6)]
finish('sfx_enter', inst(112, bell, L, vel=80) + at(bank('fit/bell/small_bell', 0.9), 0.0, L) * 0.25, peak_db=-6)

# 6) 하트(취향 발견): 뽁 + 첼레스타 A6→C7 앞꾸밈음
L = 0.6
finish('sfx_heart', bank('pro_pop/bloop', L) * 0.6
       + at(inst(8, [(0, 'A6', .15, .8), (0.06, 'C7', .4, 1)], L, vel=75), 0.02, L) * 0.8, peak_db=-5)

# 7) 골드: 동전 두 번 '찰캉' — 높은 글로켄 E7·A7 + 금속 틱
L = 0.6
finish('sfx_coin', inst(9, [(0, 'E7', .25, .9), (0.07, 'A7', .4, 1)], L, vel=80)
       + bank('pro_click/tink', L) * 0.5 + at(bank('pro_click/tink', L, seed=7), 0.07, L) * 0.4, peak_db=-5)

# ───────── 2차(11/09): 나머지 UI·주방 소리도 같은 결로 ─────────
rng = np.random.default_rng(11)
# 8) 구매(도구·테마·시즌 메뉴 연구): 동전 + 글로켄 C6→F6 '찰칵-딩'
L = 0.9
finish('sfx_buy', bank('pro_click/tink', L) * 0.5
       + at(inst(9, [(0, 'C6', .3, .85), (0.08, 'F6', .6, 1)], L, vel=78), 0.03, L), peak_db=-4, fade=0.25)

# 9) 강화(레시피·가구 별, 후원 단계): 마림바 F-A-C 빠르게 + 끝 글로켄 F7 반짝
L = 1.1
up = [(0, 'F5', .2, .8), (0.06, 'A5', .2, .9), (0.12, 'C6', .2, 1), (0.18, 'F6', .5, 1.05)]
finish('sfx_upgrade', inst(12, up, L, vel=85) + at(inst(9, [(0, 'F7', .5, 1)], 0.8, vel=60), 0.2, L) * 0.6, peak_db=-3, fade=0.3)

# 10) 카페 확장(공간이 넓어짐): 하프 글리산도 F장조 두 옥타브 + 은은한 쉬머
L = 2.2
gl = [(i * 0.045, n, 1.2, 0.7 + i * 0.03) for i, n in enumerate(['F4', 'A4', 'C5', 'F5', 'A5', 'C6', 'F6', 'A6', 'C7'])]
finish('sfx_reveal', inst(46, gl, L, vel=88) + at(inst(9, [(0, 'F6', 1, 1), (0.02, 'C7', 1, .8)], 1.4, vel=55), 0.45, L) * 0.5
       + bank('pro_shimmer/m4', L) * 0.18, peak_db=-3, fade=0.6)

# 11) 이야기 카드: 오르골이 BGM 첫 소절(C·A·C·F)을 살짝
L = 1.8
finish('sfx_story', inst(10, [(0, 'C6', .4, .9), (0.18, 'A5', .4, .8), (0.36, 'C6', .4, .9), (0.6, 'F6', 1, 1)], L, vel=80),
       peak_db=-5, fade=0.5)

# 12) 엔딩·체험판 끝: 피아노 F장조 펼친화음 + 현 패드 + 멜로디 꼬리
L = 3.2
pno = [(0, 'F3', 2.8, .8), (0.08, 'C4', 2.7, .7), (0.16, 'A4', 2.6, .75), (0.24, 'F5', 2.5, .8), (0.6, 'A5', .5, .9), (1.0, 'C6', 2.0, 1)]
finish('sfx_finale', inst(0, pno, L, vel=80) + inst(48, [(0, 'F4', 3, 1), (0, 'A4', 3, 1), (0, 'C5', 3, 1)], L, vel=45) * 0.6,
       peak_db=-3, fade=0.8)

# 13) 사진 찍기: 카메라 셔터
L = 0.35
finish('sfx_shutter', bank('shutter/film', L), peak_db=-4)

# 14) 사진 저장: 칼림바 A5·C6 두 음
L = 0.9
finish('sfx_saved', inst(108, [(0, 'A5', .5, .9), (0.09, 'C6', .7, 1)], L, vel=80), peak_db=-5, fade=0.3)

# 15) 버튼 클릭: 부드러운 딸깍(음정 없음 — 자주 나니까 귀에 안 걸리게)
L = 0.12
finish('sfx_click', bank('pro_click/soft', L) * 0.8 + bank('pro_pop/thup', L) * 0.3, peak_db=-6, fade=0.04)

# 16) 알림 토스트: 뽁 + 첼레스타 F6 한 음(작게)
L = 0.5
finish('sfx_toast', bank('pro_pop/tink', L) * 0.5 + inst(8, [(0, 'F6', .3, 1)], L, vel=60) * 0.7, peak_db=-7, fade=0.2)

# 17) 안 됨(바쁨·도구 없음·받을 손님 없음): 둔한 톡톡, 음정 낮게
L = 0.45
finish('sfx_no', bank('pro_pop/thup', L) * 0.7 + at(bank('pro_pop/thup', L, seed=3), 0.09, L) * 0.5
       + inst(12, [(0, 'F3', .12, 1)], L, vel=70) * 0.4, peak_db=-7, fade=0.08)

# 18~21) 종이 넘김·발도장·붓기·지글도 코드로 만들어 봤지만 earshot이 '글리치/스네어/휘익'으로 읽어서 버림.
#   생활 소리(foley)는 합성으로는 한계 → 기존 소리 유지, 나중에 CC0 녹음(freesound 등)을 받아 교체할 것.
