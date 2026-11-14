# 11/13 '맞혀 보세요' 퀴즈 쇼츠(세로 1080x1920) — 고양이로 붙잡고(0초) → 질문 → 단서 3개 → 3·2·1 → 영상 안에서 정답 공개 → 출시 카드.
#   댓글 참여에 기대지 않는다(이전 회색냥 퀴즈 11/04: 댓글 0). 답이 궁금해서 끝까지 보게 만드는 게 목적.
#   정답은 게임 안 도감 '소문'에 이미 공개된 내용만 쓴다(달냥: 창가·높은 곳·불맛 생선) → 새 스포일러 없음.
# 사용(여기까페냐옹/ 폴더에서): python3 Marketing/ig_assets/make_quiz_short.py <out.mp4>
import sys, os, subprocess, tempfile, math
from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1]
ROOT = 'UnityProject/Assets/Resources'; FONT = f'{ROOT}/Fonts/Galmuri11.ttf'; AUD = f'{ROOT}/Audio'
W, H, FPS = 1080, 1920, 30
CREAM, PANEL, INK, SOFT, PINK, LINE = (250, 240, 222), (255, 248, 236), (92, 64, 52), (150, 112, 86), (196, 92, 110), (232, 214, 190)
F = lambda s: ImageFont.truetype(FONT, s)
tmp = tempfile.mkdtemp()

CAT = 'moon'
Q_KO, Q_EN = 'Q. 달냥이 기다리는 냄새는?', 'Q. What smell is Luna waiting for?'
OPTS = [('menu_pudding', '푸딩', 'Pudding'), ('menu_grilled_fish', '구운 생선', 'Grilled fish'), ('menu_milk', '우유', 'Milk')]
ANS = 1
CLUES = [('단서 ① 창가 자리를 골라 앉아요', 'Clue 1 · Always picks the window seat'),
         ('단서 ② 높은 곳을 한참 올려다봐요', 'Clue 2 · Stares up at high places'),
         ('단서 ③ 불맛 나는 냄새를 기다린대요', 'Clue 3 · Rumor: waiting for a smoky smell')]
T_Q, T_OPT, T_CLUE, T_CNT, T_ANS, T_HID, T_END, TOTAL = 0.4, 1.1, 2.0, 8.0, 9.5, 11.8, 13.8, 16.8

# ── 게임 메뉴 아이콘(Scripts/Art/CookingDrawing.Dish 그대로 옮김, 24x24 · 유니티는 아래가 y=0) ──
P = dict(Outline=(74, 54, 47), White=(250, 246, 238), WoodMid=(184, 142, 100), WoodDark=(150, 110, 74), PinkMid=(230, 160, 172),
         BlueLight=(178, 208, 226), BlueMid=(132, 176, 202), YellowLight=(248, 224, 156), WoodDeep=(116, 82, 58),
         CreamDark=(212, 186, 152), SkyLow=(196, 226, 238))
def dish(mid):
    px = {}
    def s(x, y, c):
        if 0 <= x < 24 and 0 <= y < 24: px[(x, y)] = P[c]
    def hl(x0, x1, y, c): [s(x, y, c) for x in range(min(x0, x1), max(x0, x1) + 1)]
    def vl(x, y0, y1, c): [s(x, y, c) for y in range(min(y0, y1), max(y0, y1) + 1)]
    def rr(x0, y0, x1, y1, r, c):
        for y in range(y0, y1 + 1):
            dy = min(y - y0, y1 - y); ins = r - dy if dy < r else 0; hl(x0 + ins, x1 - ins, y, c)
    def disc(cx, cy, r, c):
        for y in range(-r, r + 1):
            for x in range(-r, r + 1):
                if x * x + y * y <= r * r + r * 0.5: s(cx + x, cy + y, c)
    def plate(): rr(2, 2, 21, 6, 2, 'White'); hl(4, 19, 2, 'CreamDark')
    if mid == 'menu_pudding':
        plate()
        for y in range(6, 15): h = 6 - (y - 6) // 3; hl(12 - h, 11 + h, y, 'YellowLight')
        hl(9, 14, 15, 'WoodMid'); hl(10, 13, 16, 'WoodDark'); disc(12, 18, 1, 'PinkMid')
    elif mid == 'menu_grilled_fish':
        plate(); rr(4, 6, 16, 10, 2, 'BlueMid')
        for i in range(3): vl(17 + i, 6 + i, 10 - i, 'BlueMid')
        for x in range(7, 16, 3): vl(x, 7, 9, 'WoodDeep')
        s(5, 8, 'Outline')
    elif mid == 'menu_milk':
        for y in range(3, 18): hl(8, 15, y, 'SkyLow')
        for y in range(4, 15): hl(9, 14, y, 'White')
        hl(8, 15, 17, 'BlueLight')
    snap = dict(px)                                                    # AutoOutline
    for y in range(24):
        for x in range(24):
            if (x, y) in snap: continue
            if any((x + dx, y + dy) in snap for dx in (-1, 0, 1) for dy in (-1, 0, 1)): px[(x, y)] = P['Outline']
    im = Image.new('RGBA', (24, 24))
    for (x, y), c in px.items(): im.putpixel((x, 23 - y), c + (255,))
    return im
ICONS = {m: dish(m).resize((24 * 6, 24 * 6), Image.NEAREST) for m, _, _ in OPTS}

faces = {e: Image.open(f'{ROOT}/CatCloseups/close_cat_{CAT}_{e}.bytes').convert('RGBA') for e in ('open', 'half', 'closed', 'happy', 'wide')}
FS = 2.05
faces = {e: im.resize((int(im.width * FS), int(im.height * FS)), Image.NEAREST) for e, im in faces.items()}

# 발도장(정산 화면 도장 느낌): 분홍 발바닥
def paw(size, col=(214, 96, 118, 230)):
    im = Image.new('RGBA', (size, size)); d = ImageDraw.Draw(im); u = size / 10
    d.ellipse((2.6 * u, 4.4 * u, 7.4 * u, 8.8 * u), fill=col)
    for cx, cy in ((2.0, 3.6), (4.0, 1.9), (6.0, 1.9), (8.0, 3.6)): d.ellipse(((cx - 1.0) * u, (cy - 1.15) * u, (cx + 1.0) * u, (cy + 1.15) * u), fill=col)
    return im
PAW = paw(110)

def ctext(d, y, t, f, c):
    w = d.textlength(t, font=f); d.text(((W - w) / 2, y), t, font=f, fill=c)

def ease(x): x = max(0.0, min(1.0, x)); return 1 - (1 - x) ** 3

STAGE = (120, 500, 960, 1060)            # 고양이 무대
OPT_Y = 1090; OPT_W, OPT_H, OPT_GAP = 260, 250, 30

def face_at(t):
    if t >= T_ANS: return 'closed' if T_HID + 0.9 <= t < T_HID + 1.5 else 'happy'
    if t < 1.0: return 'wide'
    blink = (t % 2.6) > 2.45
    if blink: return 'closed'
    return 'half' if T_CLUE + 2 <= t < T_CLUE + 4 else 'open'     # 높은 곳 올려다보는 단서 때는 지그시

def frame(n):
    t = n / FPS
    im = Image.new('RGBA', (W, H), CREAM + (255,)); d = ImageDraw.Draw(im)
    if t >= T_END: return endcard(t, im, d)
    # 질문
    if t >= T_Q:
        a = ease((t - T_Q) / 0.25); y = 340 + int((1 - a) * 30)
        ctext(d, y, Q_KO, F(60), INK); ctext(d, y + 84, Q_EN, F(34), SOFT)
    # 무대
    x0, y0, x1, y1 = STAGE
    d.rounded_rectangle((x0 - 8, y0 - 8, x1 + 8, y1 + 8), radius=22, fill=INK)
    d.rounded_rectangle((x0, y0, x1, y1), radius=16, fill=PANEL)
    d.rectangle((x0, y1 - 70, x1, y1 - 16), fill=LINE)
    fc = faces[face_at(t)]
    bob = int(round(math.sin(t * 2.4) * 3)) if t < T_CNT or t >= T_ANS else 0
    fx = (W - fc.width) // 2; fy = y1 - 70 - fc.height + 18 + bob
    im.alpha_composite(fc, (fx, fy))
    # 단서 자막(무대 위쪽)
    if T_CLUE <= t < T_CNT:
        i = int((t - T_CLUE) // 2); ko, en = CLUES[i]
        a = ease((t - T_CLUE - i * 2) / 0.2)
        bw = 800; bx = (W - bw) // 2; by = y0 + 22 - int((1 - a) * 16)
        d.rounded_rectangle((bx, by, bx + bw, by + 104), radius=18, fill=(255, 255, 255), outline=LINE, width=3)
        ctext(d, by + 14, ko, F(36), INK); ctext(d, by + 64, en, F(26), SOFT)
    # 3·2·1
    if T_CNT <= t < T_ANS:
        k = 3 - int((t - T_CNT) / 0.5); fr = ((t - T_CNT) % 0.5) / 0.5
        f = F(int(220 - fr * 40)); s = str(k); w = d.textlength(s, font=f)
        ov = Image.new('RGBA', (W, H)); od = ImageDraw.Draw(ov)
        od.rounded_rectangle((x0, y0, x1, y1), radius=16, fill=(255, 248, 236, 150)); im.alpha_composite(ov); d = ImageDraw.Draw(im)
        d.text(((W - w) / 2, (y0 + y1) / 2 - 130), s, font=f, fill=PINK)
    # 정답 · 숨은 손님 자막
    if T_ANS <= t:
        msg = ('정답! 구운 생선', 'Correct! Grilled fish') if t < T_HID else ('조건을 맞춰야 찾아오는 숨은 손님', 'A hidden guest who comes only for the right café')
        st = T_ANS if t < T_HID else T_HID; a = ease((t - st) / 0.2)
        bw = 800; bx = (W - bw) // 2; by = y0 + 22 - int((1 - a) * 16)
        d.rounded_rectangle((bx, by, bx + bw, by + 104), radius=18, fill=PINK)
        ctext(d, by + 14, msg[0], F(40 if t < T_HID else 34), (255, 250, 244)); ctext(d, by + 64, msg[1], F(26), (255, 232, 236))
    # 보기 3개
    tot = 3 * OPT_W + 2 * OPT_GAP
    for i, (mid, ko, en) in enumerate(OPTS):
        a = ease((t - T_OPT - i * 0.12) / 0.3)
        if a <= 0: continue
        ox = (W - tot) // 2 + i * (OPT_W + OPT_GAP); oy = OPT_Y + int((1 - a) * 60)
        win = t >= T_ANS and i == ANS; lose = t >= T_ANS and i != ANS
        card = Image.new('RGBA', (OPT_W, OPT_H)); cd = ImageDraw.Draw(card)
        cd.rounded_rectangle((0, 0, OPT_W - 1, OPT_H - 1), radius=20, fill=(255, 255, 255) if not win else (255, 236, 240), outline=PINK if win else INK, width=6 if win else 4)
        cd.text((16, 10), '①②③'[i], font=F(34), fill=PINK if win else SOFT)
        card.alpha_composite(ICONS[mid], ((OPT_W - 144) // 2, 26))
        w = cd.textlength(ko, font=F(36)); cd.text(((OPT_W - w) / 2, 172), ko, font=F(36), fill=INK)
        w = cd.textlength(en, font=F(22)); cd.text(((OPT_W - w) / 2, 214), en, font=F(22), fill=SOFT)
        if lose:
            fade = Image.new('RGBA', card.size, (250, 240, 222, int(160 * ease((t - T_ANS) / 0.3)))); card.alpha_composite(fade)
        if win:
            sc = 1 + 0.08 * math.sin(min(1, (t - T_ANS) / 0.3) * math.pi)
            card = card.resize((int(OPT_W * sc), int(OPT_H * sc)), Image.LANCZOS); ox -= (card.width - OPT_W) // 2; oy -= (card.height - OPT_H) // 2
        im.alpha_composite(card, (ox, oy))
        if win and t >= T_ANS + 0.25:                                  # 발도장 쾅
            k = ease((t - T_ANS - 0.25) / 0.12); sz = int(110 * (1.6 - 0.6 * k))
            p = PAW.resize((sz, sz), Image.LANCZOS).rotate(-14, expand=True)
            im.alpha_composite(p, (ox + OPT_W - 30 - p.width // 2, oy + 6 - p.height // 2))
    # 게임 이름
    ctext(d, 1372, 'Purrsonal Taste · 여기카페냐옹', F(40), PINK)
    return im

def endcard(t, im, d):
    a = ease((t - T_END) / 0.35)
    f2 = faces['happy']; im.alpha_composite(f2, ((W - f2.width) // 2, 360 - int((1 - a) * 30)))
    y = 360 + f2.height + 30
    for txt, f, c, gap in (('고양이마다 숨은 취향이 있어요', F(52), INK, 74), ('Every cat hides its own taste', F(32), SOFT, 90),
                           ('11.16 (월) 저녁 7시', F(64), INK, 92), ('STOVE 체험판 무료 출시', F(64), PINK, 92), ('Free demo on STOVE · Nov 16', F(34), SOFT, 70)):
        ctext(d, y, txt, f, c); y += gap
    ctext(d, 1372, 'Purrsonal Taste · 여기카페냐옹', F(40), PINK)
    return im

N = int(TOTAL * FPS)
for n in range(N): frame(n).convert('RGB').save(os.path.join(tmp, f'f{n:04d}.png'))
vid = os.path.join(tmp, 'v.mp4')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', os.path.join(tmp, 'f%04d.png'),
                '-c:v', 'libx264', '-crf', '17', '-preset', 'medium', '-pix_fmt', 'yuv420p', vid], check=True)

SFX = [('sfx_meow_0', 0.05, 0.9), ('sfx_toast', T_Q, 0.5), ('sfx_click', T_OPT, 0.5), ('sfx_click', T_OPT + 0.12, 0.5), ('sfx_click', T_OPT + 0.24, 0.5),
       ('sfx_page', T_CLUE, 0.6), ('sfx_page', T_CLUE + 2, 0.6), ('sfx_page', T_CLUE + 4, 0.6),
       ('sfx_click', T_CNT, 0.8), ('sfx_click', T_CNT + 0.5, 0.8), ('sfx_click', T_CNT + 1.0, 0.8),
       ('sfx_correct', T_ANS, 0.9), ('sfx_stamp', T_ANS + 0.3, 0.9), ('sfx_trill', T_ANS + 0.6, 0.8),
       ('sfx_purr', T_HID, 0.5), ('sfx_story', T_END, 0.8)]
inputs = ['-i', vid, '-stream_loop', '-1', '-i', f'{AUD}/bgm_night.ogg']
fc = [f'[1:a]volume=0.35,afade=t=in:d=0.4,volume=enable=\'between(t,{T_CNT},{T_ANS})\':volume=0.4,afade=t=out:st={TOTAL - 1.2}:d=1.2[a0]']; mix = ['[a0]']
for i, (nm, at, v) in enumerate(SFX):
    inputs += ['-i', f'{AUD}/{nm}.ogg']; ms = int(at * 1000)
    fc.append(f'[{2 + i}:a]adelay={ms}|{ms},volume={v}[s{i}]'); mix.append(f'[s{i}]')
fc.append(''.join(mix) + f'amix=inputs={len(mix)}:duration=first:normalize=0,alimiter=limit=0.95[a]')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', *inputs, '-filter_complex', ';'.join(fc), '-map', '0:v', '-map', '[a]',
                '-t', str(TOTAL), '-c:v', 'copy', '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', OUT], check=True)
print('ok', OUT, TOTAL, 's', os.path.getsize(OUT) // 1024, 'KB')
