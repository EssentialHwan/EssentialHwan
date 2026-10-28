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

# 안전 영역: 프로필 격자(3:4로 위아래가 잘림)와 릴스 화면(위 계정 이름·아래 캡션·오른쪽 버튼)에 안 가리게
#   가로 120~960(840px) · 세로 330~1440 안에 글·클립·게임 이름을 전부 넣는다
SAFE_X0, SAFE_W, SAFE_Y0, SAFE_Y1 = 120, 840, 330, 1440
bg = Image.new('RGB', (W, H), CREAM); d = ImageDraw.Draw(bg)
fk = ImageFont.truetype(FONT, 58); fe = ImageFont.truetype(FONT, 34); fs = ImageFont.truetype(FONT, 40); fx = ImageFont.truetype(FONT, 30)
ko_l, en_l = wrap(d, ko, fk, SAFE_W), wrap(d, en, fe, SAFE_W)
text_h = len(ko_l) * 74 + 12 + len(en_l) * 46
brand_h = 56 + 44
gap = 36
if src in ('newcats', 'parade'):
    cw, ch = 1000, 584
else:
    probe = subprocess.run(['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=width,height', '-of', 'csv=p=0', src], capture_output=True, text=True).stdout.strip().split(',')
    cw, ch = map(int, probe)
room = (SAFE_Y1 - SAFE_Y0) - text_h - brand_h - gap * 2
clip_w = min(SAFE_W, int(cw * room / ch)); clip_w -= clip_w % 2
clip_h = round(ch * clip_w / cw) // 2 * 2
total = text_h + gap + clip_h + gap + brand_h
y = SAFE_Y0 + ((SAFE_Y1 - SAFE_Y0) - total) // 2
for ln in ko_l:
    w = d.textlength(ln, font=fk); d.text(((W - w) / 2, y), ln, font=fk, fill=INK); y += 74
y += 12
for ln in en_l:
    w = d.textlength(ln, font=fe); d.text(((W - w) / 2, y), ln, font=fe, fill=SOFT); y += 46
cy = y + gap; cx = (W - clip_w) // 2
d.rounded_rectangle((cx - 8, cy - 8, cx + clip_w + 8, cy + clip_h + 8), radius=18, fill=INK)
yb = cy + clip_h + gap
for t, f, c in (('Purrsonal Taste · 여기카페냐옹', fs, PINK), ('Steam 2027 · @winterystudio', fx, SOFT)):
    w = d.textlength(t, font=f); d.text(((W - w) / 2, yb), t, font=f, fill=c); yb += 56
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
        can = Image.new('RGBA', (1000, 584), (255, 248, 236, 255)); dd = ImageDraw.Draw(can)
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

# 새 고양이 8마리 산책: 걷기 v2(짚은 발 3.2px/프레임)를 같은 보폭으로 실제로 이동시킨다 → 발이 바닥에 붙어 걷는 게 보임
#   두 줄 × 4마리, 끝에서 사라지면 반대편에서 다시 나와 이음매 없이 반복(한 바퀴 = 1168px / 9.6px = 약 122프레임)
if src == 'parade':
    import csv
    F, k, FPS = 56, 4, 12
    step = 3.2 * k
    cats = ['cheese', 'calico', 'cow', 'black', 'tuxedo', 'mackerel', 'siamese', 'gray']
    names = {r[0]: r[1] for r in csv.reader(open(f'{ROOT}/strings.csv', encoding='utf-8')) if r and r[0].startswith('cat.cat_') and r[0].endswith('.name')}
    sheets = {c: Image.open(f'{ROOT}/CatSheets/cat_{c}.bytes').convert('RGBA') for c in cats}
    fn = ImageFont.truetype(FONT, 30)
    span = 1000 + F * k
    N = int(round(span / step))
    for t in range(N):
        can = Image.new('RGBA', (1000, 584), (255, 248, 236, 255)); dd = ImageDraw.Draw(can)
        for row in range(2):
            gy = 236 + row * 284                                    # 바닥선
            dd.line((0, gy + 2, 1000, gy + 2), fill=(232, 214, 190, 255), width=4)
        for j, c in enumerate(cats):
            row, idx = j // 4, j % 4
            cell = 52 + (t + j * 3) % 8
            img = sheets[c].crop(((cell % 4) * F, (cell // 4) * F, (cell % 4 + 1) * F, (cell // 4 + 1) * F)).resize((F * k, F * k), Image.NEAREST)
            x = (t * step + idx * span / 4 + row * span / 8) % span - F * k
            gy = 236 + row * 284
            can.alpha_composite(img, (int(round(x)), gy - F * k + 3 * k))
            n = names.get(f'cat.cat_{c}.name', c); w = dd.textlength(n, font=fn)
            dd.text((x + F * k / 2 - w / 2, gy + 10), n, font=fn, fill=INK)
        can.convert('RGB').save(os.path.join(tmp, f'n{t}.png'))
    src = os.path.join(tmp, 'parade.mp4')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', os.path.join(tmp, 'n%d.png'),
                    '-c:v', 'libx264', '-crf', '12', '-pix_fmt', 'yuv420p', src], check=True)

# 소리
inputs = ['-loop', '1', '-i', bgp, '-stream_loop', '-1', '-i', src, '-stream_loop', '-1', '-i', f'{AUD}/{bgm}.ogg']
fc = [f'[1:v]scale={clip_w}:{clip_h}:flags=lanczos[c]', f'[0:v][c]overlay={cx}:{cy}:shortest=0,fps=30,format=yuv420p[v]',
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
