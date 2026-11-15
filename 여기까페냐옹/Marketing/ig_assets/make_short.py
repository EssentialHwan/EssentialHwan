# 쇼츠·틱톡 전용 세로 영상(11/05): 첫 프레임부터 고양이를 크게 + 위에 질문 훅. 인스타용 make_reel(여백 큼)과 따로 둔다.
#   python3 Marketing/ig_assets/make_short.py <catId> <out.mp4> "<손님 번호·이름>" "<English>" "<한 줄>" <sfx울음>
#   근거: 쇼츠 '계속 시청' 비율이 9~20%(첫 화면에서 대부분 넘김) → 첫 0.5초에 볼거리를 둔다.
import sys, os, subprocess, tempfile
from PIL import Image, ImageDraw, ImageFont
ROOT = 'UnityProject/Assets/Resources'; FONT = f'{ROOT}/Fonts/Galmuri11.ttf'; AUD = f'{ROOT}/Audio'
W, H, FPS = 1080, 1920, 12
CREAM, INK, SOFT, PINK, WOOD, WOOD2 = (250, 240, 222), (92, 64, 52), (150, 112, 86), (196, 92, 110), (232, 210, 178), (214, 188, 152)
cat, out, title, en, line, meow = sys.argv[1:7]
F, k = 56, 15                                    # 칸 56px × 15 = 840px — 화면 폭의 78%
PLAN = [  # (길이초, 칸들, 초당칸, 자막, English, 말풍선, 걷기)
    (1.4, [3, 3, 3, 0, 0, 2], 5, '', '', '냐아~', False),
    (2.0, list(range(52, 60)), 12, '종종종…', 'Tiny steps', '', True),
    (1.6, [36, 37, 37, 38, 38, 37, 36, 39], 8, '그루밍', 'Grooming', '', False),
    (1.2, [8, 8, 46, 46, 8, 47], 5, '식빵 굽기', 'Loafing', '', False),
    (1.5, [42, 43], 6, '꾹꾹이', 'Kneading', '', False),
    (1.2, [48, 49, 48, 49, 50, 50, 51, 51], 8, '사냥 놀이!', 'Pounce!', '', False),
    (2.6, [44, 44, 44, 45, 45, 45], 4, '새근새근', 'Nap time', 'z z z', False),
]
T0 = []; t = 0.0
for p in PLAN: T0.append(t); t += p[0]
SECS = t
sheet = Image.open(f'{ROOT}/CatSheets/cat_{cat}.bytes').convert('RGBA')
cells = [sheet.crop(((i % 4) * F, (i // 4) * F, (i % 4 + 1) * F, (i // 4 + 1) * F)).resize((F * k, F * k), Image.NEAREST) for i in range(sheet.height // F * 4)]
f = lambda s: ImageFont.truetype(FONT, s)
fh, fhe, ft, fte, fl, fa, fae, fb = f(84), f(38), f(56), f(34), f(36), f(64), f(36), f(60)
tmp = tempfile.mkdtemp()
GY = 1330                                        # 바닥선 — 아래 25%(쇼츠 캡션·버튼 자리) 위
def ctext(d, y, s, font, fill):
    w = d.textlength(s, font=font); d.text(((W - w) / 2, y), s, font=font, fill=fill)
N = int(SECS * FPS) + 1
for n in range(N):
    sec = n / FPS
    idx = max(i for i, s0 in enumerate(T0) if s0 <= sec)
    dur, fr, rate, lk, le, bub, walk = PLAN[idx]; lt = sec - T0[idx]
    im = Image.new('RGBA', (W, H), CREAM + (255,)); d = ImageDraw.Draw(im)
    # 훅(질문) — 첫 프레임부터
    ctext(d, 190, '이 고양이가', fh, INK); ctext(d, 290, '좋아하는 건 뭘까?', fh, PINK)
    ctext(d, 400, "What does this cat secretly love?", fhe, SOFT)
    # 바닥(나무 판자) — 걷는 동안 판자 줄이 흘러서 걷는 느낌
    d.rectangle((0, GY, W, H), fill=WOOD + (255,))
    off = int((lt * rate * 3.2 * k) % 180) if walk else 0
    for x in range(-180, W + 180, 180):
        d.line((x - off, GY, x - off, H), fill=WOOD2 + (255,), width=4)
    d.line((0, GY, W, GY), fill=(200, 172, 136, 255), width=6)
    img = cells[fr[int(lt * rate) % len(fr)]]
    x = (W - F * k) // 2 - (60 if walk else 0)
    d.ellipse((x + F * k * 0.2, GY - 22, x + F * k * 0.8, GY + 22), fill=(214, 190, 158, 255))
    im.alpha_composite(img, (x, GY - F * k + 3 * k))
    if bub:
        bw = d.textlength(bub, font=fb) + 60; bx = W - bw - 70; by = GY - F * k + (300 if bub.startswith("z") else 40)
        d.rounded_rectangle((bx, by, bx + bw, by + 96), radius=26, fill=(255, 255, 255, 255), outline=INK, width=5)
        d.polygon([(bx + 30, by + 92), (bx + 66, by + 92), (bx + 10, by + 140)], fill=(255, 255, 255, 255))
        d.line((bx + 30, by + 92, bx + 10, by + 140), fill=INK, width=5); d.line((bx + 10, by + 140, bx + 66, by + 94), fill=INK, width=5)
        d.text((bx + 30, by + 14), bub, font=fb, fill=INK)
    if lk:
        ctext(d, GY + 30, lk, fa, INK); ctext(d, GY + 104, le, fae, SOFT)
    # 이름표(아래쪽, 캡션 영역보다 위)
    ctext(d, GY + 170, title, ft, INK); ctext(d, GY + 238, en, fte, SOFT)
    if line and sec < SECS - 1.8: ctext(d, 470, line, fl, SOFT)
    if sec >= SECS - 1.8:
        t1, t2 = '구독하고 다음 손님 만나기', 'Subscribe to meet the next guest'
        bw = max(d.textlength(t1, font=f(46)), d.textlength(t2, font=f(30))) + 70; bx0 = (W - bw) / 2; by0 = 462
        d.rounded_rectangle((bx0, by0, bx0 + bw, by0 + 124), radius=26, fill=PINK)
        ctext(d, by0 + 14, t1, f(46), (255, 250, 244)); ctext(d, by0 + 76, t2, f(30), (255, 236, 236))
    im.convert('RGB').save(os.path.join(tmp, f'n{n}.png'))
vid = os.path.join(tmp, 'v.mp4')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', os.path.join(tmp, 'n%d.png'), '-c:v', 'libx264', '-crf', '14', '-pix_fmt', 'yuv420p', vid], check=True)
sfx = [(meow, 0.15), ('sfx_lick', T0[2] + 0.2), ('sfx_lick', T0[2] + 1.0), ('sfx_knead', T0[4] + 0.05), ('sfx_purr', T0[4] + 0.2), ('sfx_pounce', T0[5] + 0.6), ('sfx_yawn', T0[6] + 0.1)]
inputs = ['-i', vid, '-stream_loop', '-1', '-i', f'{AUD}/bgm_0_0.ogg']
fc = [f'[1:a]volume=0.35,afade=t=out:st={SECS - 1.2}:d=1.2[a0]', '[0:v]fps=30,format=yuv420p[v]']; mix = ['[a0]']
for i, (nm, at) in enumerate(sfx):
    ms = int(at * 1000); inputs += ['-i', f'{AUD}/{nm}.ogg']; fc.append(f'[{2 + i}:a]adelay={ms}|{ms}[s{i}]'); mix.append(f'[s{i}]')
fc.append(''.join(mix) + f'amix=inputs={len(mix)}:duration=first:normalize=0[a]')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', *inputs, '-filter_complex', ';'.join(fc), '-map', '[v]', '-map', '[a]', '-t', str(SECS),
                '-c:v', 'libx264', '-crf', '18', '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', out], check=True)
print('ok', out, round(SECS, 1), 's')
