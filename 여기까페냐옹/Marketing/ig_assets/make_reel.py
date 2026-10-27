# 인스타 릴스(세로 1080x1920) 만들기. 여기까페냐옹/ 폴더에서:
#   python3 Marketing/ig_assets/make_reel.py <clip.mp4|newcats> <out.mp4> <seconds> "<한국어 큰 글씨>" "<English small>" <sfx1@sec,sfx2@sec,...> [bgm]
# - 게임 화면 클립(가로 1000px)을 가운데에, 위에 한·영 문구, 아래에 게임 이름. 글꼴은 게임과 같은 Galmuri11.
# - 소리: 게임 안 BGM(작게) + 고양이 효과음(합성음 · 골골은 BigSoundBank CC0).
import sys, os, subprocess, tempfile
from PIL import Image, ImageDraw, ImageFont

ROOT = 'UnityProject/Assets/Resources'
FONT = f'{ROOT}/Fonts/Galmuri11.ttf'
AUD = f'{ROOT}/Audio'
W, H = 1080, 1920
CREAM, INK, SOFT, PINK = (250, 240, 222), (92, 64, 52), (150, 112, 86), (196, 92, 110)

src, out, secs, ko, en, sfx = sys.argv[1:7]
bgm = sys.argv[7] if len(sys.argv) > 7 else 'bgm_0_0'
secs = float(secs)
tmp = tempfile.mkdtemp()

def wrap(d, text, font, maxw):
    if '/' in text: return [t.strip() for t in text.split('/')]   # '/'로 줄바꿈 위치를 직접 정한다
    lines, cur = [], ''
    for ch in text.split(' '):
        t = (cur + ' ' + ch).strip()
        if d.textlength(t, font=font) > maxw and cur: lines.append(cur); cur = ch
        else: cur = t
    lines.append(cur); return lines

# 클립 크기
if src == 'newcats':
    clip_w, clip_h = 1000, 584
else:
    probe = subprocess.run(['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=width,height', '-of', 'csv=p=0', src], capture_output=True, text=True).stdout.strip().split(',')
    cw, ch = map(int, probe); clip_w, clip_h = 1000, round(ch * 1000 / cw) // 2 * 2
cy = 1000 - clip_h // 2                     # 클립 위치(가운데보다 살짝 아래)

# 바탕(문구·테두리)
bg = Image.new('RGB', (W, H), CREAM); d = ImageDraw.Draw(bg)
fk = ImageFont.truetype(FONT, 66); fe = ImageFont.truetype(FONT, 40); fs = ImageFont.truetype(FONT, 44); fx = ImageFont.truetype(FONT, 34)
y = max(150, cy - 330)
for ln in wrap(d, ko, fk, 960):
    w = d.textlength(ln, font=fk); d.text(((W - w) / 2, y), ln, font=fk, fill=INK); y += 86
y += 10
for ln in wrap(d, en, fe, 960):
    w = d.textlength(ln, font=fe); d.text(((W - w) / 2, y), ln, font=fe, fill=SOFT); y += 54
d.rounded_rectangle((40 - 8, cy - 8, 40 + clip_w + 8, cy + clip_h + 8), radius=18, fill=INK)
yb = cy + clip_h + 70
for t, f, c in (('Purrsonal Taste · 여기카페냐옹', fs, PINK), ('Steam 2027 · @winterystudio', fx, SOFT)):
    w = d.textlength(t, font=f); d.text(((W - w) / 2, yb), t, font=f, fill=c); yb += 62
bgp = os.path.join(tmp, 'bg.png'); bg.save(bgp)

# 새 고양이 8마리: 시트에서 걷기 칸으로 직접 클립 만들기
if src == 'newcats':
    import csv
    F, k = 56, 3
    cats = ['cheese', 'calico', 'cow', 'black', 'tuxedo', 'mackerel', 'siamese', 'gray']
    names = {r[0]: r[1] for r in csv.reader(open(f'{ROOT}/strings.csv', encoding='utf-8')) if r and r[0].startswith('cat.cat_') and r[0].endswith('.name')}
    sheets = {c: Image.open(f'{ROOT}/CatSheets/cat_{c}.bytes').convert('RGBA') for c in cats}
    fn = ImageFont.truetype(FONT, 30)
    for t in range(8):
        can = Image.new('RGBA', (clip_w, clip_h), (255, 248, 236, 255)); dd = ImageDraw.Draw(can)
        for j, c in enumerate(cats):
            cell = 52 + t
            img = sheets[c].crop(((cell % 4) * F, (cell // 4) * F, (cell % 4 + 1) * F, (cell // 4 + 1) * F)).resize((F * k, F * k), Image.NEAREST)
            cx = 125 + (j % 4) * 250; top = 6 + (j // 4) * 290
            dd.ellipse((cx - 62, top + 158, cx + 62, top + 174), fill=(232, 214, 190, 255))
            can.alpha_composite(img, (cx - F * k // 2, top))
            n = names.get(f'cat.cat_{c}.name', c); w = dd.textlength(n, font=fn)
            dd.text((cx - w / 2, top + 180), n, font=fn, fill=INK)
        can.convert('RGB').save(os.path.join(tmp, f'n{t}.png'))
    src = os.path.join(tmp, 'newcats.mp4')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', '9', '-i', os.path.join(tmp, 'n%d.png'),
                    '-c:v', 'libx264', '-crf', '12', '-pix_fmt', 'yuv420p', src], check=True)

# 소리
inputs = ['-loop', '1', '-i', bgp, '-stream_loop', '-1', '-i', src, '-stream_loop', '-1', '-i', f'{AUD}/{bgm}.ogg']
fc = [f'[1:v]scale={clip_w}:{clip_h}:flags=neighbor[c]', f'[0:v][c]overlay=40:{cy}:shortest=0,fps=30,format=yuv420p[v]',
      f'[2:a]volume=0.35,afade=t=in:d=0.6,afade=t=out:st={secs - 1.2}:d=1.2[a0]']
mix = ['[a0]']
for i, item in enumerate([s for s in sfx.split(',') if s and s != '-']):
    name, at = item.split('@'); ms = int(float(at) * 1000)
    inputs += ['-i', f'{AUD}/{name}.ogg']
    fc.append(f'[{3 + i}:a]adelay={ms}|{ms},volume=1.0[s{i}]'); mix.append(f'[s{i}]')
fc.append(''.join(mix) + f'amix=inputs={len(mix)}:duration=first:normalize=0[a]')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', *inputs, '-filter_complex', ';'.join(fc), '-map', '[v]', '-map', '[a]',
                '-t', str(secs), '-c:v', 'libx264', '-crf', '18', '-preset', 'medium', '-c:a', 'aac', '-b:a', '160k',
                '-movflags', '+faststart', out], check=True)
print('ok', out, os.path.getsize(out) // 1024, 'KB')
