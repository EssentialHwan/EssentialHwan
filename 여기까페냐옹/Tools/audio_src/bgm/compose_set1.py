# 고양이 카페 BGM 시제품: 같은 멜로디·화성·템포를 아침/점심/저녁/비 버전으로 편곡 → 시간대 전환 때 크로스페이드해도 자연스럽다.
import mido, sys
BPM, TPB = 100, 480
def T(b): return int(b * TPB)
CH = { 'F':[53,57,60], 'Am':[57,60,64], 'Bb':[58,62,65], 'C':[48,52,55], 'Dm':[50,53,57], 'Gm7':[55,58,62,65], 'C7':[48,52,55,58], 'Gm':[55,58,62] }
PROG_A = ['F','Am','Bb','C','F','Dm','Gm7','C7','Bb','C','Am','Dm','Gm','C','F','C7']
PROG_B = ['Dm','Bb','F','C','Dm','Gm','C','C7']
PROG = PROG_A + PROG_B + PROG_A[8:]
ROOT = {'F':41,'Am':45,'Bb':46,'C':48,'Dm':38,'Gm7':43,'C7':48,'Gm':43}
MEL = [[(72,1),(69,.5),(72,.5),(77,1),(76,1)],[(74,1.5),(72,.5),(69,2)],[(70,1),(74,.5),(77,.5),(74,1),(70,1)],[(72,3),(None,1)],
       [(69,.5),(72,.5),(77,1),(79,.5),(77,.5),(76,1)],[(74,1),(77,1),(81,2)],[(79,1),(77,.5),(74,.5),(70,1),(74,1)],[(72,2),(70,1),(67,1)],
       [(74,1.5),(72,.5),(70,1),(74,1)],[(76,1.5),(74,.5),(72,2)],[(72,1),(76,1),(81,1),(79,1)],[(77,2),(74,2)],
       [(70,1),(74,1),(79,1),(77,1)],[(76,1),(74,1),(72,1),(70,1)],[(69,1),(72,1),(77,2)],[(76,1),(74,1),(72,2)]]
MEL_B = [[(74,1),(76,.5),(77,.5),(81,2)],[(79,1),(77,1),(74,2)],[(72,.5),(74,.5),(77,1),(76,1),(72,1)],[(74,2),(72,1),(70,1)],
         [(69,1),(74,1),(77,1),(76,1)],[(74,1.5),(70,.5),(67,2)],[(72,1),(74,1),(76,1),(79,1)],[(77,1),(76,1),(74,1),(72,1)]]
MEL = MEL + MEL_B + MEL[8:]
BARS = 32
import random
def build(version, loops=2):
    ev = []   # (tick, ch, note, vel, dur_ticks)
    rnd = random.Random(sum(map(ord, version)))
    swing = version == 'evening'
    def note(t, ch, n, v, d):
        if swing and abs((t % 1) - 0.5) < 1e-6: t += 0.08                 # 저녁: 뒷박을 살짝 늦춰 스윙
        jitter = 0 if ch == 9 and version == 'noon' and t % 1 == 0 else rnd.randint(-8, 8)
        v = max(1, min(127, int(v * (0.92 + rnd.random() * 0.16))))
        ev.append((max(0, T(t) + jitter), ch, n, v, T(d)))
    progs = {}
    if version == 'morning':  progs = {0:9, 1:24, 2:32}            # 글로켄슈필 멜로디 · 나일론 기타 아르페지오 · 어쿠스틱 베이스
    if version == 'noon':     progs = {0:12, 1:24, 2:32, 3:9}      # 마림바 멜로디 · 기타 스트럼 · 베이스 · 글로켄 반짝
    if version == 'evening':  progs = {0:0, 1:0, 2:32}             # 피아노 멜로디 · 피아노 컴핑 · 콘트라베이스
    if version == 'rain':     progs = {0:10, 1:0}                  # 오르골 멜로디 · 피아노 긴 화음
    for L in range(loops):
        o = L * BARS * 4
        for b in range(BARS):
            t0 = o + b * 4; ch = PROG[b]; tones = CH[ch]; r = ROOT[ch]
            # 멜로디
            t = t0
            for n, d in MEL[b]:
                if n is not None:
                    if version == 'morning': note(t, 0, n + 12, 78, d * 0.9)
                    elif version == 'noon': note(t, 0, n, 92, d * 0.85)
                    elif version == 'evening': note(t, 0, n, 70 if d < 1 else 76, d * 0.95)
                    else: note(t, 0, n + 12, 72, d)
                t += d
            # 반주
            if version == 'morning':
                arp = [tones[0], tones[1], tones[2], tones[1]] * 2
                for i, n in enumerate(arp): note(t0 + i * .5, 1, n + 12, 58 if i % 2 else 66, .5)
                note(t0, 2, r, 62, 1.8); note(t0 + 2, 2, r + 7 if r + 7 < 55 else r - 5, 54, 1.8)
            if version == 'noon':
                for k, bt in enumerate([0, 1.5, 2, 3]):
                    for j, n in enumerate(tones): note(t0 + bt + j * .02, 1, n + 12, 70 if k == 0 else 58, .45)
                walk = [r, r + 4 if ch not in ('Am','Dm','Gm','Gm7') else r + 3, r + 7, r + 5]
                for k, n in enumerate(walk): note(t0 + k, 2, n, 70, .9)
                if b % 2 == 0: note(t0, 3, tones[-1] + 24, 50, 1)
                if 16 <= b < 24 and b % 2 == 1:                                    # B: 글로켄이 멜로디에 대답
                    for k, n in enumerate([tones[2] + 24, tones[1] + 24, tones[0] + 24]): note(t0 + 2.5 + k * .5, 3, n, 56, .45)
                if b in (7, 15, 23, 31):                                           # 드럼 필
                    for k, n in enumerate([45, 45, 43, 43]): note(t0 + 3 + k * .25, 9, n, 48 + k * 4, .2)
                for k in range(8): note(t0 + k * .5, 9, 42, 38 if k % 2 else 50, .25)       # 하이햇
                note(t0, 9, 36, 60, .5); note(t0 + 2, 9, 36, 52, .5)                        # 킥
                note(t0 + 1, 9, 37, 40, .25); note(t0 + 3, 9, 37, 40, .25)                  # 사이드스틱
            if version == 'evening':
                vo = [n + 12 for n in tones]
                for j, n in enumerate(vo): note(t0 + j * .03, 1, n, 46, 1.4)
                for j, n in enumerate(vo): note(t0 + 2.5 + j * .03, 1, n, 40, 1.2)
                walk = [r, r + 2, r + 4 if ch not in ('Am','Dm','Gm','Gm7') else r + 3, r + 7]
                for k, n in enumerate(walk): note(t0 + k, 2, n, 64, .95)
                note(t0 + 1, 9, 44, 30, .25); note(t0 + 3, 9, 44, 30, .25)                  # 페달 하이햇만
            if version == 'rain':
                for j, n in enumerate(tones): note(t0 + j * .25, 1, n, 44, 3.6 - j * .25)
    mid = mido.MidiFile(ticks_per_beat=TPB)
    tr = mido.MidiTrack(); mid.tracks.append(tr)
    tr.append(mido.MetaMessage('set_tempo', tempo=mido.bpm2tempo(BPM)))
    for c, p in progs.items(): tr.append(mido.Message('program_change', channel=c, program=p, time=0))
    for c, pan, rev, cho in [(0, 70, 62, 0), (1, 42, 48, 18), (2, 64, 20, 0), (3, 92, 70, 0), (9, 64, 30, 0)]:
        tr.append(mido.Message('control_change', channel=c, control=10, value=pan, time=0))
        tr.append(mido.Message('control_change', channel=c, control=91, value=rev, time=0))
        tr.append(mido.Message('control_change', channel=c, control=93, value=cho, time=0))
    tr.append(mido.Message('control_change', channel=0, control=91, value=62, time=0))
    tr.append(mido.Message('control_change', channel=1, control=91, value=40, time=0))
    tr.append(mido.Message('control_change', channel=2, control=7, value=95, time=0))
    msgs = []
    for t, c, n, v, d in ev:
        msgs.append((t, 1, mido.Message('note_on', channel=c, note=n, velocity=v)))
        msgs.append((t + max(1, d), 0, mido.Message('note_off', channel=c, note=n, velocity=0)))
    msgs.sort(key=lambda x: (x[0], x[1]))
    last = 0
    for t, _, m in msgs: m.time = t - last; last = t; tr.append(m)
    end = T(loops * BARS * 4 + 4)
    tr.append(mido.MetaMessage('end_of_track', time=max(0, end - last)))
    mid.save(f'{version}.mid')
for v in ['morning', 'noon', 'evening', 'rain']: build(v)
print('loop seconds', BARS * 4 * 60 / BPM)
