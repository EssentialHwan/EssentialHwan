# 11/16 출시일 쇼츠 '오픈 1초 전': 시계 18:59:58 → 19:00 땡! CLOSED 팻말이 OPEN으로 뒤집히고, 문으로 고양이 손님 10마리가 우르르 → 하트 → 출시 카드.
#   기존 '오늘의 손님' 한 마리 소개와 다른 그림(가게 앞·시계·팻말·단체 입장)으로, 첫 화면부터 '무슨 일이 일어나려는지'를 보여 준다.
# 사용(여기까페냐옹/ 폴더에서): python3 Marketing/ig_assets/make_open_short.py <out.mp4>
import sys, os, csv, math, random, subprocess, tempfile
from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1]
ROOT = 'UnityProject/Assets/Resources'; FONT = f'{ROOT}/Fonts/Galmuri11.ttf'; AUD = f'{ROOT}/Audio'
W, H, FPS = 1080, 1920, 30
CREAM, INK, SOFT, PINK = (250, 240, 222), (92, 64, 52), (150, 112, 86), (196, 92, 110)
WALL, FLOOR, LINE, WOOD, WOODD = (255, 246, 232), (238, 222, 198), (222, 200, 170), (176, 128, 86), (128, 88, 58)
F = lambda s: ImageFont.truetype(FONT, s)
tmp = tempfile.mkdtemp(); random.seed(7)

CATS = ['moon', 'calico', 'kitten', 'black', 'cheese', 'siamese', 'fold', 'persian', 'munchkin', 'cow']
names = {r[0]: r[1] for r in csv.reader(open(f'{ROOT}/strings.csv', encoding='utf-8')) if r and r[0].startswith('cat.cat_') and r[0].endswith('.name')}
CS, K = 56, 3
sheets = {c: Image.open(f'{ROOT}/CatSheets/cat_{c}.bytes').convert('RGBA') for c in CATS}
_cache = {}
def cell(c, i, flip):
    key = (c, i, flip)
    if key not in _cache:
        im = sheets[c].crop(((i % 4) * CS, (i // 4) * CS, (i % 4 + 1) * CS, (i // 4 + 1) * CS)).resize((CS * K, CS * K), Image.NEAREST)
        _cache[key] = im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im
    return _cache[key]

T_OPEN, T_IN0, T_GAP, SPEED, T_HEART, T_END, TOTAL = 1.3, 1.65, 0.48, 430.0, 8.3, 9.6, 12.8
SX0, SY0, SX1, SY1 = 90, 470, 990, 1230                 # 가게 안 무대
ROWS = [SY1 - 250, SY1 - 70]                            # 뒷줄·앞줄 바닥선
DOOR_X = SX1 - 120
# 자리: 먼저 들어온 손님일수록 문에서 먼 자리(왼쪽)부터 — 뒷줄·앞줄 번갈아
SPOTS = []
for i in range(10):
    row = i % 2; col = i // 2
    SPOTS.append((SX0 + 100 + col * 140 + row * 70, ROWS[row]))
ENTER = [T_IN0 + i * T_GAP for i in range(10)]

def ease(x): x = max(0.0, min(1.0, x)); return 1 - (1 - x) ** 3
def ctext(d, y, t, f, c):
    w = d.textlength(t, font=f); d.text(((W - w) / 2, y), t, font=f, fill=c)

def heart(d, cx, cy, s, col=(232, 96, 120)):
    d.ellipse((cx - s, cy - s * 0.6, cx, cy + s * 0.3), fill=col); d.ellipse((cx, cy - s * 0.6, cx + s, cy + s * 0.3), fill=col)
    d.polygon([(cx - s, cy - 0.05 * s), (cx + s, cy - 0.05 * s), (cx, cy + s)], fill=col)

def stage(im, t):
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((SX0 - 8, SY0 - 8, SX1 + 8, SY1 + 8), radius=22, fill=INK)
    d.rectangle((SX0, SY0, SX1, SY1), fill=WALL)
    d.rectangle((SX0, ROWS[0] - 40, SX1, SY1), fill=FLOOR)
    for y in range(ROWS[0] - 40, SY1, 46): d.line((SX0, y, SX1, y), fill=LINE, width=2)
    # 창(왼쪽 위) — 저녁 노을
    wx0, wy0 = SX0 + 40, SY0 + 120
    d.rectangle((wx0, wy0, wx0 + 220, wy0 + 150), fill=(250, 196, 150)); d.rectangle((wx0, wy0 + 90, wx0 + 220, wy0 + 150), fill=(236, 150, 128))
    d.rectangle((wx0, wy0, wx0 + 220, wy0 + 150), outline=WOODD, width=8); d.line((wx0 + 110, wy0, wx0 + 110, wy0 + 150), fill=WOODD, width=6)
    # 문(오른쪽) — 열리면 안쪽이 밝아진다
    opened = t >= T_OPEN
    dx0, dy0, dx1 = DOOR_X, SY0 + 90, SX1 - 6
    d.rectangle((dx0 - 10, dy0 - 10, dx1 + 4, ROWS[0] - 36), fill=WOODD)
    if opened:
        d.rectangle((dx0, dy0, dx1, ROWS[0] - 40), fill=(255, 236, 190))
        d.polygon([(dx1, dy0), (dx1 + 6, dy0 - 6), (dx1 + 6, ROWS[0] - 30), (dx1, ROWS[0] - 40)], fill=WOOD)
    else:
        d.rectangle((dx0, dy0, dx1, ROWS[0] - 40), fill=WOOD)
        d.ellipse((dx0 + 12, dy0 + 180, dx0 + 26, dy0 + 194), fill=(240, 200, 120))
    # 문 팻말(문 앞에 걸림): CLOSED → 뒤집히며 OPEN
    flip = (t - T_OPEN) / 0.25
    sw = 150 * abs(math.cos(min(1.0, max(0.0, flip)) * math.pi)) if 0 <= flip <= 1 else 150
    sx = (dx0 + dx1) / 2 - 75 - 20; sy = dy0 + 40
    lab = 'OPEN' if flip >= 0.5 else 'CLOSED'; col = (120, 176, 104) if lab == 'OPEN' else (204, 104, 104)
    d.line((sx + 30, sy - 26, sx + 75, sy - 46), fill=WOODD, width=3); d.line((sx + 120, sy - 26, sx + 75, sy - 46), fill=WOODD, width=3)
    cx = sx + 75
    d.rounded_rectangle((cx - sw / 2, sy - 26, cx + sw / 2, sy + 30), radius=8, fill=col, outline=INK, width=3)
    if sw > 60:
        f = F(28); w = d.textlength(lab, font=f); d.text((cx - w / 2, sy - 18), lab, font=f, fill=(255, 250, 240))
    # 시계(가운데 위 벽)
    secs = 58 + min(2, int(t / (T_OPEN / 2.0))) if t < T_OPEN else 0
    hh, mm = (18, 59) if t < T_OPEN else (19, 0)
    clock = f'{hh:02d}:{mm:02d}:{secs:02d}'
    bx = (SX0 + DOOR_X) / 2 + 40
    d.rounded_rectangle((bx - 150, SY0 + 22, bx + 150, SY0 + 96), radius=14, fill=(60, 44, 40))
    f = F(52); w = d.textlength(clock, font=f)
    d.text((bx - w / 2, SY0 + 32), clock, font=f, fill=(255, 214, 120) if t < T_OPEN else (150, 230, 140))
    return d

def frame(n):
    t = n / FPS
    im = Image.new('RGBA', (W, H), CREAM + (255,))
    if t >= T_END: return endcard(t, im)
    d = ImageDraw.Draw(im)
    # 위 글
    if t < T_OPEN:
        ctext(d, 340, '오픈 1초 전…', F(62), INK); ctext(d, 420, 'One second to opening…', F(32), SOFT)
    else:
        a = ease((t - T_OPEN) / 0.2); f = F(int(64 + 10 * (1 - a)))
        ctext(d, 336, '드디어 OPEN!', f, PINK); ctext(d, 420, 'Purrsonal Taste is open!', F(32), SOFT)
    stage(im, t); d = ImageDraw.Draw(im)
    # 손님 수
    n_in = sum(1 for e in ENTER if t >= e + 0.05)
    if n_in:
        s = f'손님 {n_in}마리'; f = F(34); w = d.textlength(s, font=f)
        d.rounded_rectangle((SX0 + 24, SY0 + 22, SX0 + 44 + w, SY0 + 76), radius=12, fill=(255, 255, 255), outline=LINE, width=3)
        d.text((SX0 + 34, SY0 + 30), s, font=f, fill=INK)
    # 고양이(뒷줄 먼저)
    order = sorted(range(10), key=lambda i: SPOTS[i][1])
    for i in order:
        c = CATS[i]; t0 = ENTER[i]
        if t < t0: continue
        tx, ty = SPOTS[i]
        x0 = DOOR_X + 10; dist = x0 - tx; dur = dist / SPEED
        lt = t - t0
        if lt < dur:                                       # 걷기(왼쪽으로)
            x = x0 - lt * SPEED; y = ty
            img = cell(c, 52 + int(lt * 16) % 8, True)
            bob = 0
        else:                                              # 앉기 + 가끔 눈 깜빡
            x, y = tx, ty
            st = lt - dur
            idx = 34 if (st + i * 0.37) % 2.4 > 2.25 else 0
            if t >= T_HEART: idx = 0
            img = cell(c, idx, True); bob = 0
        im.alpha_composite(img, (int(x - CS * K / 2 + CS * K / 2 - 84), int(y - CS * K + 9 + bob)))
        if lt >= dur:
            nm = names.get(f'cat.cat_{c}.name', c); f = F(24); w = d.textlength(nm, font=f)
            a = ease((lt - dur) / 0.2)
            nx = x - w / 2 + 0; ny = y + 4 + (1 - a) * 8
            d.rounded_rectangle((nx - 8, ny - 2, nx + w + 8, ny + 30), radius=8, fill=(255, 255, 255))
            d.text((nx, ny), nm, font=f, fill=INK)
        if t >= T_HEART + i * 0.05:
            k = ease((t - T_HEART - i * 0.05) / 0.25); bump = math.sin(min(1, (t - T_HEART - i * 0.05) / 0.25) * math.pi) * 6
            heart(d, x, y - CS * K + 10 - 20 * k - bump, 16 * k)
    ctext(d, 1262, 'Purrsonal Taste · 여기카페냐옹', F(40), PINK)
    ctext(d, 1318, '11.16 (월) 19시 · STOVE 무료 체험판', F(32), SOFT)
    return im

def endcard(t, im):
    d = ImageDraw.Draw(im); a = ease((t - T_END) / 0.35)
    y = 470 - int((1 - a) * 30)
    for txt, f, c, gap in (('고양이 손님 10마리가', F(52), INK, 74), ('기다리고 있어요', F(52), INK, 92), ('Ten cat guests are waiting', F(32), SOFT, 120),
                           ('지금 STOVE에서', F(64), INK, 92), ('무료 체험판 OPEN', F(64), PINK, 100), ('Free demo now on STOVE', F(34), SOFT, 0)):
        ctext(d, y, txt, f, c); y += gap
    for j, c in enumerate(['moon', 'calico', 'kitten', 'black', 'cheese']):
        img = cell(c, 0, j % 2 == 1); im.alpha_composite(img, (90 + j * 186, 1110 - int((1 - a) * 20)))
    ctext(d, 1318, 'Purrsonal Taste · 여기카페냐옹', F(40), PINK)
    return im

N = int(TOTAL * FPS)
for n in range(N): frame(n).convert('RGB').save(os.path.join(tmp, f'f{n:04d}.png'))
vid = os.path.join(tmp, 'v.mp4')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', os.path.join(tmp, 'f%04d.png'),
                '-c:v', 'libx264', '-crf', '17', '-preset', 'medium', '-pix_fmt', 'yuv420p', vid], check=True)

meows = ['sfx_meow_0', 'sfx_meow_1', 'sfx_meow_2', 'sfx_meow_small']
SFX = [('sfx_click', 0.02, 0.6), ('sfx_click', T_OPEN / 2, 0.6), ('sfx_enter', T_OPEN, 1.0), ('sfx_reveal', T_OPEN + 0.05, 0.6)]
SFX += [(meows[i % 4], ENTER[i] + 0.1, 0.55) for i in (0, 2, 4, 7, 9)]
SFX += [('sfx_heart', T_HEART, 0.8), ('sfx_heart', T_HEART + 0.25, 0.6), ('sfx_story', T_END, 0.8)]
inputs = ['-i', vid, '-stream_loop', '-1', '-i', f'{AUD}/bgm_1_0.ogg']
fc = [f"[1:a]volume=0.0,volume=enable='gte(t,{T_OPEN})':volume=0.42,afade=t=out:st={TOTAL - 1.2}:d=1.2[a0]"]; mix = ['[a0]']
for i, (nm, at, v) in enumerate(SFX):
    inputs += ['-i', f'{AUD}/{nm}.ogg']; ms = int(at * 1000)
    fc.append(f'[{2 + i}:a]adelay={ms}|{ms},volume={v}[s{i}]'); mix.append(f'[s{i}]')
fc.append(''.join(mix) + f'amix=inputs={len(mix)}:duration=first:normalize=0,alimiter=limit=0.95[a]')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', *inputs, '-filter_complex', ';'.join(fc), '-map', '0:v', '-map', '[a]',
                '-t', str(TOTAL), '-c:v', 'copy', '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', OUT], check=True)
print('ok', OUT, TOTAL, 's', os.path.getsize(OUT) // 1024, 'KB')
