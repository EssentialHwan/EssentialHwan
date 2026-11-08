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
    rnd = random.Random(hash(version) & 0xffff)
    swing = version == 'evening'
    def note(t, ch, n, v, d):
        if swing and abs((t % 1) - 0.5) < 1e-6: t += 0.08                 # 저녁: 뒷박을 살짝 늦춰 스윙
        jitter = 0 if ch == 9 and version == 'noon' and t % 1 == 0 else rnd.randint(-8, 8)
        bar = int(t // 4) % BARS
        sec = 0.88 if bar < 16 else (1.12 if bar < 24 else 1.0)
        if ch == 0 and bar in (5, 21, 28): sec *= 1.08                       # 프레이즈 정점
        v = max(1, min(127, int(v * sec * (0.92 + rnd.random() * 0.16))))
        ev.append((max(0, T(t) + jitter), ch, n, v, T(d)))
    progs = {}
    # v3(earshot 진단: 전부 '오르골'로 들림·스테레오 좁음·다이내믹 2LU) → 종소리 비중↓, 리드 악기 분리, 패드·카운터라인 추가
    if version == 'morning':  progs = {0:73, 1:24, 2:32, 3:9, 4:48}     # 플루트 리드 · 나일론 아르페지오 · 베이스 · 글로켄(강박만) · 현 패드
    if version == 'noon':     progs = {0:12, 1:25, 2:32, 3:24, 4:9}     # 마림바 · 스틸기타 스트럼 · 베이스 · 나일론 카운터 · 글로켄(B만)
    if version == 'evening':  progs = {0:0, 1:4, 2:32, 3:48, 4:66}      # 피아노 · 일렉피아노 컴핑 · 콘트라베이스 · 현 패드 · 테너색소(A' 카운터)
    if version == 'rain':     progs = {0:10, 1:4, 2:89, 3:0}            # 오르골 · 일렉피아노 · 웜패드 · 피아노 저음
    for L in range(loops):
        o = L * BARS * 4
        for b in range(BARS):
            t0 = o + b * 4; ch = PROG[b]; tones = CH[ch]; r = ROOT[ch]
            # 멜로디
            t = t0
            for n, d in MEL[b]:
                if n is not None:
                    if version == 'morning': note(t, 0, n, 74, d * 0.92)
                    elif version == 'noon': note(t, 0, n, 92, d * 0.85)
                    elif version == 'evening': note(t, 0, n, 70 if d < 1 else 76, d * 0.95)
                    else: note(t, 0, n + 12, 64, d)
                t += d
            # 반주
            if version == 'morning':
                arp = [tones[0], tones[1], tones[2], tones[1]] * 2
                for i, n in enumerate(arp): note(t0 + i * .5, 1, n + 12, 58 if i % 2 else 66, .5)
                note(t0, 2, r, 62, 1.8); note(t0 + 2, 2, r + 7 if r + 7 < 55 else r - 5, 54, 1.8)
                if b % 4 == 0: note(t0, 3, tones[-1] + 24, 42, 1)                      # 반짝임은 4마디에 한 번
                if b >= 16:
                    for n in tones: note(t0, 4, n + 12, 34, 3.9)                        # B부터 현 패드
            if version == 'noon':
                for k, bt in enumerate([0, 1, 1.5, 2.5, 3]):
                    for j, n in enumerate(tones): note(t0 + bt + j * .025, 1, n + 12, 74 if k == 0 else 60, .4)
                if b >= 24:                                                         # A': 나일론 카운터(3도 아래 길게)
                    note(t0, 3, tones[1] + 12, 52, 1.9); note(t0 + 2, 3, tones[2] + 12, 48, 1.9)
                walk = [r, r + 4 if ch not in ('Am','Dm','Gm','Gm7') else r + 3, r + 7, r + 5]
                for k, n in enumerate(walk): note(t0 + k, 2, n, 70, .9)
                if 16 <= b < 24 and b % 2 == 1:                                    # B: 글로켄이 멜로디에 대답
                    for k, n in enumerate([tones[2] + 24, tones[1] + 24, tones[0] + 24]): note(t0 + 2.5 + k * .5, 4, n, 46, .45)
                if b in (7, 15, 23, 31):                                           # 드럼 필
                    for k, n in enumerate([45, 45, 43, 43]): note(t0 + 3 + k * .25, 9, n, 48 + k * 4, .2)
                for k in range(8): note(t0 + k * .5, 9, 42 if b < 16 else 69, 40 if k % 2 else 54, .25)   # A 하이햇 / B 캐바사
                if b >= 16:
                    for k in range(16): note(t0 + k * .25, 9, 70, 30 if k % 2 else 40, .1)   # B부터 셰이커
                note(t0, 9, 36, 68, .5); note(t0 + 2, 9, 36, 58, .5)                        # 킥
                note(t0 + 1, 9, 37, 50, .25); note(t0 + 3, 9, 37, 50, .25)                  # 사이드스틱
            if version == 'evening':
                vo = [n + 12 for n in tones]
                for j, n in enumerate(vo): note(t0 + j * .03, 1, n, 50, 1.4)
                for j, n in enumerate(vo): note(t0 + 2.5 + j * .03, 1, n, 44, 1.2)
                if b >= 16:
                    for n in tones: note(t0, 3, n, 30, 3.9)
                if b >= 24 and b % 2 == 0: note(t0 + 1, 4, tones[1] + 12, 46, 2.6)        # A': 색소 긴 음
                walk = [r, r + 2, r + 4 if ch not in ('Am','Dm','Gm','Gm7') else r + 3, r + 7]
                for k, n in enumerate(walk): note(t0 + k, 2, n, 64, .95)
                note(t0 + 1, 9, 44, 30, .25); note(t0 + 3, 9, 44, 30, .25)                  # 페달 하이햇만
            if version == 'rain':
                for j, n in enumerate(tones): note(t0 + j * .25, 1, n + 12, 46, 3.6 - j * .25)
                for n in tones: note(t0, 2, n, 36, 3.95)
                note(t0, 3, r, 40, 3.5)
    mid = mido.MidiFile(ticks_per_beat=TPB)
    tr = mido.MidiTrack(); mid.tracks.append(tr)
    tr.append(mido.MetaMessage('set_tempo', tempo=mido.bpm2tempo(BPM)))
    for c, p in progs.items(): tr.append(mido.Message('program_change', channel=c, program=p, time=0))
    for c, pan, rev, cho in [(0, 66, 58, 10), (1, 28, 46, 30), (2, 64, 18, 0), (3, 100, 64, 25), (4, 40, 80, 40), (9, 72, 34, 0)]:
        tr.append(mido.Message('control_change', channel=c, control=10, value=pan, time=0))
        tr.append(mido.Message('control_change', channel=c, control=91, value=rev, time=0))
        tr.append(mido.Message('control_change', channel=c, control=93, value=cho, time=0))
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
