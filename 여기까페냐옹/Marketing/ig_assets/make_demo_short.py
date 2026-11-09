# 체험판 출시 공지 쇼츠(세로) — 실제 플레이 화면으로 "이 게임이 뭔지" 15초 안에 보여 주기.
# 구성: 손님이 온다 → 앞발로 요리 → 행동 관찰 → 취향 추리 → 다 맞힘 → 단골 → 출시 카드
# 사용(여기까페냐옹/ 폴더에서): python3 Marketing/ig_assets/make_demo_short.py <seg 폴더(seg_1~6.mp4, 1000x900)> <out.mp4>
import sys, os, subprocess, tempfile
from PIL import Image, ImageDraw, ImageFont

SEG, OUT = sys.argv[1], sys.argv[2]
ROOT = 'UnityProject/Assets/Resources'; FONT = f'{ROOT}/Fonts/Galmuri11.ttf'; AUD = f'{ROOT}/Audio'
W, H = 1080, 1920
CREAM, INK, SOFT, PINK = (250, 240, 222), (92, 64, 52), (150, 112, 86), (196, 92, 110)
tmp = tempfile.mkdtemp()
fk = ImageFont.truetype(FONT, 62); fe = ImageFont.truetype(FONT, 34); fs = ImageFont.truetype(FONT, 40); fx = ImageFont.truetype(FONT, 30)

# (seg, 길이, 한국어, English)
SHOTS = [
    (1, 2.8, '고양이 손님이 찾아오는 카페', 'A cafe where cats are the customers'),
    (2, 2.6, '앞발로 톡! 요리해서 내주고', 'Tap with your paw to cook & serve'),
    (3, 2.4, '무엇을 좋아하는지 지켜보고', 'Watch what each cat loves'),
    (4, 2.6, '취향을 추리해요', 'Then deduce their tastes'),
    (5, 1.8, '전부 맞히면…', 'Get them all right…'),
    (6, 3.0, '단골이 돼요!', '…and they become regulars!'),
]
CLIP_W, CLIP_H = 900, 810          # 1000x900 → 0.9배
CY = 560

def frame(ko, en, path):
    bg = Image.new('RGB', (W, H), CREAM); d = ImageDraw.Draw(bg)
    y = 340
    w = d.textlength(ko, font=fk); d.text(((W - w) / 2, y), ko, font=fk, fill=INK)
    w = d.textlength(en, font=fe); d.text(((W - w) / 2, y + 86), en, font=fe, fill=SOFT)
    cx = (W - CLIP_W) // 2
    d.rounded_rectangle((cx - 8, CY - 8, cx + CLIP_W + 8, CY + CLIP_H + 8), radius=18, fill=INK)
    yb = CY + CLIP_H + 40
    for t, f, c in (('Purrsonal Taste · 여기카페냐옹', fs, PINK), ('11.16(월) 19시 · STOVE 체험판 무료', fx, SOFT)):
        w = d.textlength(t, font=f); d.text(((W - w) / 2, yb), t, font=f, fill=c); yb += 56
    bg.save(path)

parts = []
for i, (s, dur, ko, en) in enumerate(SHOTS):
    bgp = os.path.join(tmp, f'bg{i}.png'); frame(ko, en, bgp)
    out = os.path.join(tmp, f'p{i}.mp4')
    off = {5: 0.25}.get(s, 0)                                    # 체크 표시가 떠 있는 1초만 쓰고 멈춰 보여 준다
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-loop', '1', '-i', bgp, '-ss', str(off), '-i', os.path.join(SEG, f'seg_{s}.mp4'),
                    '-filter_complex', ('[1:v]trim=0:0.95,setpts=PTS-STARTPTS,tpad=stop_mode=clone:stop_duration=1.5,' if s == 5 else '[1:v]') + f'scale={CLIP_W}:{CLIP_H}:flags=lanczos[c];[0:v][c]overlay={(W - CLIP_W) // 2}:{CY},fps=30,format=yuv420p[v]',
                    '-map', '[v]', '-t', str(dur), '-c:v', 'libx264', '-crf', '16', '-preset', 'fast', out], check=True)
    parts.append(out)

# 출시 카드
END = 3.0
card = Image.new('RGB', (W, H), CREAM); d = ImageDraw.Draw(card)
fl = ImageFont.truetype(FONT, 96); fd = ImageFont.truetype(FONT, 64); fm = ImageFont.truetype(FONT, 40)
y = 640
for t, f, c, gap in (('여기카페냐옹', fl, PINK, 130), ('Purrsonal Taste', fm, SOFT, 150), ('11.16 (월) 저녁 7시', fd, INK, 100),
                     ('STOVE 체험판 무료 출시', fd, INK, 110), ('Free demo on STOVE · Nov 16', fm, SOFT, 0)):
    w = d.textlength(t, font=f); d.text(((W - w) / 2, y), t, font=f, fill=c); y += gap
# 고양이 발바닥 한 줄
for k in range(5):
    x = 330 + k * 105; yy = 1420 + (k % 2) * 30
    d.ellipse((x, yy, x + 46, yy + 40), fill=(240, 196, 204))
    for dx, dy in ((-8, -26), (12, -34), (34, -26)): d.ellipse((x + dx + 6, yy + dy, x + dx + 22, yy + dy + 16), fill=(240, 196, 204))
cp = os.path.join(tmp, 'card.png'); card.save(cp)
out = os.path.join(tmp, 'end.mp4')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-loop', '1', '-i', cp, '-vf', 'fps=30,format=yuv420p',
                '-t', str(END), '-c:v', 'libx264', '-crf', '16', out], check=True)
parts.append(out)

lst = os.path.join(tmp, 'l.txt'); open(lst, 'w').write(''.join(f"file '{p}'\n" for p in parts))
vid = os.path.join(tmp, 'v.mp4')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-f', 'concat', '-safe', '0', '-i', lst, '-c', 'copy', vid], check=True)
total = sum(s[1] for s in SHOTS) + END

# 소리: 점심 BGM + 장면마다 게임 효과음 한두 개
t0 = [sum(s[1] for s in SHOTS[:i]) for i in range(len(SHOTS))]
SFX = [('sfx_enter', t0[0] + 0.2, 0.5), ('sfx_serve', t0[1] + 1.9, 0.8), ('sfx_meow_0', t0[2] + 0.5, 0.7),
       ('sfx_click', t0[3] + 0.8, 0.6), ('sfx_correct', t0[4] + 0.3, 0.8), ('sfx_purr', t0[5] + 0.3, 0.6), ('sfx_story', total - END + 0.1, 0.8)]
inputs = ['-i', vid, '-stream_loop', '-1', '-i', f'{AUD}/bgm_1_0.ogg']
fc = [f'[1:a]volume=0.45,afade=t=in:d=0.5,afade=t=out:st={total - 1.2}:d=1.2[a0]']; mix = ['[a0]']
for i, (n, at, v) in enumerate(SFX):
    inputs += ['-i', f'{AUD}/{n}.ogg']; ms = int(at * 1000)
    fc.append(f'[{2 + i}:a]adelay={ms}|{ms},volume={v}[s{i}]'); mix.append(f'[s{i}]')
fc.append(''.join(mix) + f'amix=inputs={len(mix)}:duration=first:normalize=0[a]')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', *inputs, '-filter_complex', ';'.join(fc), '-map', '0:v', '-map', '[a]',
                '-t', str(total), '-c:v', 'copy', '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', OUT], check=True)
print('ok', OUT, round(total, 1), 's', os.path.getsize(OUT) // 1024, 'KB')
