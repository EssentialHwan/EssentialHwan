# 세트1 → 게임 파일 10개(bgm_{시간대}_{날씨}.ogg + bgm_night.ogg).
#  맑음 = 시간대 편곡 그대로 / 흐림 = 타악·반짝이 빼고 살짝 어둡게 / 비 = 오르골 비 버전(시간대 공통)
#  밤 정산 = 저녁 피아노 둘만, 84 BPM.
# 9곡 모두 길이(76.8초, 샘플 수)가 같아서 SoundManager가 듣던 자리에서 이어 튼다 → 시간대·날씨가 바뀌어도 노래가 끊기지 않는다.
# 사용: python3 render_set1.py <compose_set1.py가 만든 .mid 폴더> <sf2> <출력 폴더>
import sys, os, subprocess, mido
MID, SF2, OUT = sys.argv[1:4]
os.makedirs(OUT, exist_ok=True)
LOOP = 76.8

def filtered(src, dst, drop=(), vel=1.0, bpm=None):
    m = mido.MidiFile(os.path.join(MID, src)); out = mido.MidiFile(ticks_per_beat=m.ticks_per_beat)
    for tr in m.tracks:
        nt = mido.MidiTrack(); carry = 0
        for msg in tr:
            if hasattr(msg, 'channel') and msg.channel in drop: carry += msg.time; continue
            msg = msg.copy(time=msg.time + carry); carry = 0
            if msg.type == 'note_on' and msg.velocity > 0: msg = msg.copy(velocity=max(1, int(msg.velocity * vel)))
            if bpm and msg.type == 'set_tempo': msg = msg.copy(tempo=mido.bpm2tempo(bpm))
            nt.append(msg)
        out.tracks.append(nt)
    out.save(dst)

def render(mid, name, loop, af=''):
    wav = mid.replace('.mid', '.wav')
    subprocess.run(['fluidsynth', '-ni', '-g', '0.6', '-r', '44100', '-R', '1', '-C', '1', '-F', wav, SF2, mid], capture_output=True)
    tmp = wav.replace('.wav', '_c.wav')
    chain = 'highpass=f=35,extrastereo=m=1.35,' + (af + ',' if af else '') + 'acompressor=threshold=-20dB:ratio=2.5:attack=15:release=200'
    # 두 바퀴 렌더 후 두 번째 바퀴만 잘라 이음매 없는 루프(리버브 꼬리까지 이어짐)
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', wav, '-ss', str(loop), '-t', str(loop), '-af', chain, '-c:a', 'pcm_s16le', tmp])
    r = subprocess.run(['ffmpeg', '-nostats', '-i', tmp, '-af', 'ebur128', '-f', 'null', '-'], capture_output=True, text=True).stderr
    I = float([l for l in r.splitlines() if l.strip().startswith('I:')][-1].split()[1])
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', tmp, '-af', f'volume={-18 - I}dB,alimiter=limit=0.85',
                    '-c:a', 'libvorbis', '-q:a', '6', os.path.join(OUT, name + '.ogg')])
    print(name, 'LUFS before', I)

T = os.path.join(OUT, '_tmp'); os.makedirs(T, exist_ok=True)
for p, v, cloudy_drop in [(0, 'morning', (3,)), (1, 'noon', (9, 4)), (2, 'evening', (9, 4))]:
    filtered(f'{v}.mid', f'{T}/{v}_0.mid'); render(f'{T}/{v}_0.mid', f'bgm_{p}_0', LOOP)
    filtered(f'{v}.mid', f'{T}/{v}_1.mid', drop=cloudy_drop, vel=0.88)
    render(f'{T}/{v}_1.mid', f'bgm_{p}_1', LOOP, af='lowpass=f=5200')
filtered('rain.mid', f'{T}/rain.mid'); render(f'{T}/rain.mid', 'bgm_rain', LOOP)
for p in range(3):
    import shutil; shutil.copy(os.path.join(OUT, 'bgm_rain.ogg'), os.path.join(OUT, f'bgm_{p}_2.ogg'))
os.remove(os.path.join(OUT, 'bgm_rain.ogg'))
filtered('evening.mid', f'{T}/night.mid', drop=(2, 3, 4, 9), vel=0.9, bpm=84)
render(f'{T}/night.mid', 'bgm_night', 32 * 4 * 60 / 84, af='lowpass=f=6000')
shutil.rmtree(T)
