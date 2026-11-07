# 인스타 릴스(세로 1080x1920) 만들기. 여기까페냐옹/ 폴더에서:
#   python3 Marketing/ig_assets/make_reel.py <clip.mp4|newcats> <out.mp4> <seconds> "<한국어 큰 글씨>" "<English small>" <sfx1@sec,sfx2@sec,...> [bgm]
# - 게임 화면 클립(가로 1000px)을 가운데에, 위에 한·영 문구, 아래에 게임 이름. 글꼴은 게임과 같은 Galmuri11.
# - 소리: 게임 안 BGM(작게) + 고양이 효과음(합성음 · 골골은 BigSoundBank CC0).
import sys, os, subprocess, tempfile, math
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
elif src.startswith('intro:') or src.startswith('acts:') or src.startswith('acts2:'):
    cw, ch = 1000, 860
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

# 오늘의 손님(고양이 소개 시리즈, 10/29): 마감 클로즈업(고해상도 원화 포즈)으로 표정 바꾸기 + 아래에서 걷기 칸으로 실제로 걸어 지나가기
#   intro:<catId>[:한 줄]  — 취향(정답)은 절대 안 보여 준다(게임의 추리 재미). 한 줄은 그 고양이의 평소 모습 정도만.
if src.startswith('intro:'):
    parts = src.split(':'); cat = parts[1]; line = parts[2] if len(parts) > 2 else ''
    F, k, FPS = 56, 4, 12
    step = 3.2 * k
    CU = f'{ROOT}/CatCloseups/close_cat_{cat}_%s.bytes'
    faces = {e: Image.open(CU % e).convert('RGBA') for e in ('open', 'happy', 'wide', 'half')}
    sheet = Image.open(f'{ROOT}/CatSheets/cat_{cat}.bytes').convert('RGBA')
    fl = ImageFont.truetype(FONT, 32)
    N = int(secs * FPS) + 2
    # 표정 순서(초): 보통 → 눈웃음 → 보통 → 동그란 눈 → 졸린 눈 → 보통
    seq = [(0.0, 'open'), (1.6, 'happy'), (2.3, 'open'), (3.6, 'wide'), (4.6, 'open'), (5.8, 'half'), (6.8, 'happy'), (7.6, 'open')]
    for t in range(N):
        sec = t / FPS
        e = [f for s0, f in seq if s0 <= sec][-1]
        can = Image.new('RGBA', (cw, ch), (255, 248, 236, 255)); dd = ImageDraw.Draw(can)
        face = faces[e]; fk3 = face.resize((int(face.width * 2.5), int(face.height * 2.5)), Image.NEAREST)
        can.alpha_composite(fk3, ((cw - fk3.width) // 2, 20))
        if line:
            w = dd.textlength(line, font=fl); dd.text(((cw - w) / 2, 20 + fk3.height + 14), line, font=fl, fill=SOFT)
        gy = ch - 24
        dd.line((0, gy + 2, cw, gy + 2), fill=(232, 214, 190, 255), width=4)
        span = cw + F * k
        cell = 52 + t % 8
        img = sheet.crop(((cell % 4) * F, (cell // 4) * F, (cell % 4 + 1) * F, (cell // 4 + 1) * F)).resize((F * k, F * k), Image.NEAREST)
        x = (t * step) % span - F * k
        can.alpha_composite(img, (int(round(x)), gy - F * k + 3 * k))
        # 11/02: 끝 1.4초에 '다음 손님' 안내 — 조회는 되는데 팔로우가 0이라, 시리즈라는 신호를 준다
        if sec >= secs - 1.4:
            fe2 = ImageFont.truetype(FONT, 40); fs2 = ImageFont.truetype(FONT, 26)
            t1 = '팔로우하고 다음 손님 만나기'; t2 = 'Follow to meet the next guest'
            bw = max(dd.textlength(t1, font=fe2), dd.textlength(t2, font=fs2)) + 60
            bx0 = (cw - bw) / 2; by0 = 20 + fk3.height + 60
            dd.rounded_rectangle((bx0, by0, bx0 + bw, by0 + 110), radius=22, fill=PINK)
            dd.text(((cw - dd.textlength(t1, font=fe2)) / 2, by0 + 14), t1, font=fe2, fill=(255, 250, 244))
            dd.text(((cw - dd.textlength(t2, font=fs2)) / 2, by0 + 66), t2, font=fs2, fill=(255, 236, 236))
        can.convert('RGB').save(os.path.join(tmp, f'n{t}.png'))
    src = os.path.join(tmp, 'intro.mp4')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', os.path.join(tmp, 'n%d.png'),
                    '-c:v', 'libx264', '-crf', '12', '-pix_fmt', 'yuv420p', src], check=True)

# 오늘의 손님 v2(11/05): 클로즈업(누끼 테두리가 거슬림) 대신 도트 시트로 행동 여러 개를 크게 보여 준다
#   acts:<catId>[:한 줄] — 걸어 들어와 앉기 → 야옹 → 그루밍 → 식빵 → 꾹꾹이 → 덮치기 → 잠. 칸 번호는 Scripts/Art/CatSheet.cs Map2 기준.
#   ACT_TIMES(초)를 효과음 타이밍에 같이 쓴다.
ACT_PLAN = [  # (길이초, 칸들, 초당칸, 한국어, English, 말풍선)
    (2.4, list(range(52, 60)), 12, '', '', ''),                        # 걸어 들어오기(이동)
    (1.6, [0, 0, 0, 0, 34, 0, 32, 33], 5, '얌전히 앉아서', 'Sitting pretty', ''),
    (1.1, [3, 3, 3, 0], 4, '', '', '냐아~'),
    (1.9, [36, 37, 37, 38, 38, 37, 36, 39], 8, '그루밍', 'Grooming', ''),
    (1.3, [8, 8, 46, 46, 8, 47], 5, '식빵 굽기', 'Loafing', ''),
    (1.6, [42, 43], 6, '꾹꾹이', 'Kneading', ''),
    (1.2, [48, 49, 48, 49, 50, 50, 51, 51], 8, '사냥 놀이!', 'Pounce!', ''),
    (2.4, [44, 44, 44, 45, 45, 45], 4, '새근새근', 'Nap time', 'z z z'),
]
ACT_TIMES = []
_t = 0.0
for _a in ACT_PLAN: ACT_TIMES.append(_t); _t += _a[0]
ACT_TOTAL = _t
if src.startswith('acts:'):
    parts = src.split(':'); cat = parts[1]; line = parts[2] if len(parts) > 2 else ''
    F, k, FPS = 56, 9, 12
    sheet = Image.open(f'{ROOT}/CatSheets/cat_{cat}.bytes').convert('RGBA')
    cells = [sheet.crop(((i % 4) * F, (i // 4) * F, (i % 4 + 1) * F, (i // 4 + 1) * F)).resize((F * k, F * k), Image.NEAREST) for i in range(sheet.height // F * 4)]
    fl = ImageFont.truetype(FONT, 34); fa = ImageFont.truetype(FONT, 48); fae = ImageFont.truetype(FONT, 28); fb = ImageFont.truetype(FONT, 40)
    N = int(secs * FPS) + 2
    gy = ch - 150                                   # 바닥선
    cxm = cw // 2
    for t in range(N):
        sec = t / FPS
        idx = max(i for i, s0 in enumerate(ACT_TIMES) if s0 <= sec) if sec < ACT_TOTAL else len(ACT_PLAN) - 1
        dur, fr, rate, lk, le, bub = ACT_PLAN[idx]
        lt = sec - ACT_TIMES[idx]
        can = Image.new('RGBA', (cw, ch), (255, 248, 236, 255)); dd = ImageDraw.Draw(can)
        dd.rectangle((0, gy, cw, ch), fill=(238, 222, 198, 255))
        dd.line((0, gy, cw, gy), fill=(214, 192, 164, 255), width=4)
        if line and sec < secs - 1.6:
            w = dd.textlength(line, font=fl); dd.text(((cw - w) / 2, 40), line, font=fl, fill=SOFT)
        img = cells[fr[int(lt * rate) % len(fr)]]
        if idx == 0:   # 왼쪽에서 걸어 들어와 가운데에 멈춘다(보폭 3.2px×k/칸)
            x = cxm - F * k / 2 - (dur - lt) * rate * 3.2 * k
        else:
            x = cxm - F * k / 2
        dd.ellipse((x + F * k * 0.18, gy - 14, x + F * k * 0.82, gy + 14), fill=(222, 202, 176, 255))
        can.alpha_composite(img, (int(round(x)), gy - F * k + 3 * k))
        if bub:
            bw = dd.textlength(bub, font=fb) + 48; bx = cxm + 150; by = gy - F * k + 10
            dd.rounded_rectangle((bx, by, bx + bw, by + 70), radius=20, fill=(255, 255, 255, 255), outline=INK, width=4)
            dd.polygon([(bx + 24, by + 66), (bx + 50, by + 66), (bx + 14, by + 100)], fill=(255, 255, 255, 255), outline=INK)
            dd.line((bx + 26, by + 66, bx + 48, by + 66), fill=(255, 255, 255, 255), width=6)
            dd.text((bx + 24, by + 12), bub, font=fb, fill=INK)
        if lk:
            w = dd.textlength(lk, font=fa); dd.text(((cw - w) / 2, gy + 26), lk, font=fa, fill=INK)
            w = dd.textlength(le, font=fae); dd.text(((cw - w) / 2, gy + 86), le, font=fae, fill=SOFT)
        if sec >= secs - 1.6:
            fe2 = ImageFont.truetype(FONT, 40); fs2 = ImageFont.truetype(FONT, 26)
            t1 = '팔로우하고 다음 손님 만나기'; t2 = 'Follow to meet the next guest'
            bw = max(dd.textlength(t1, font=fe2), dd.textlength(t2, font=fs2)) + 60
            bx0 = (cw - bw) / 2; by0 = 34
            dd.rounded_rectangle((bx0, by0, bx0 + bw, by0 + 110), radius=22, fill=PINK)
            dd.text(((cw - dd.textlength(t1, font=fe2)) / 2, by0 + 14), t1, font=fe2, fill=(255, 250, 244))
            dd.text(((cw - dd.textlength(t2, font=fs2)) / 2, by0 + 66), t2, font=fs2, fill=(255, 236, 236))
        can.convert('RGB').save(os.path.join(tmp, f'n{t}.png'))
    src = os.path.join(tmp, 'acts.mp4')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', os.path.join(tmp, 'n%d.png'),
                    '-c:v', 'libx264', '-crf', '12', '-pix_fmt', 'yuv420p', src], check=True)

# 오늘의 손님 v3(11/05 환 요청): 잠 대신 캣휠 달리기 + 2층 점프. acts2:<catId>[:한 줄]
#   장면마다 고양이 크기(k)가 달라 컷이 바뀌는 느낌 — 캣휠·선반이 화면에 들어오게 작게 잡는다.
ACT2 = [  # (길이초, 칸들, 초당칸, 한국어, English, 말풍선, 장면)
    (2.4, list(range(52, 60)), 12, '', '', '', 'walkin'),
    (1.4, [0, 0, 0, 0, 34, 0, 32, 33], 5, '얌전히 앉아서', 'Sitting pretty', '', 'floor'),
    (1.0, [3, 3, 3, 0], 4, '', '', '냐아~', 'floor'),
    (1.6, [36, 37, 37, 38, 38, 37, 36, 39], 8, '그루밍', 'Grooming', '', 'floor'),
    (2.8, [28, 29, 30, 31], 12, '캣휠 달리기!', 'Wheel run!', '', 'wheel'),
    (1.8, [14], 0, '2층으로 점프!', 'Jump to the loft!', '', 'jump'),
    (2.2, [42, 43], 6, '2층에서 꾹꾹이', 'Kneading up top', '', 'loft'),
]
# 첫 화면 실험(11/07): 걸어 들어오기 없이 캣휠 달리기 + 0초 울음으로 바로 시작. acts2:<catId>:<한 줄>:hook
if src.startswith('acts2:') and src.endswith(':hook'):
    ACT2 = [
        (2.6, [28, 29, 30, 31], 12, '캣휠 달리기!', 'Wheel run!', '냐아~', 'wheel'),
        (1.8, [14], 0, '2층으로 점프!', 'Jump to the loft!', '', 'jump'),
        (1.6, [36, 37, 37, 38, 38, 37, 36, 39], 8, '2층에서 그루밍', 'Grooming up top', '', 'loft'),
        (2.0, [42, 43], 6, '꾹꾹이', 'Kneading', '', 'loft'),
    ]
    src = src[:-5]
ACT2_T = []
_t = 0.0
for _a in ACT2: ACT2_T.append(_t); _t += _a[0]
ACT2_TOTAL = _t
if src.startswith('acts2:'):
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import reel_props as RP
    parts = src.split(':'); cat = parts[1]; line = parts[2] if len(parts) > 2 else ''
    F, FPS = 56, 12
    sheet = Image.open(f'{ROOT}/CatSheets/cat_{cat}.bytes').convert('RGBA')
    raw = [sheet.crop(((i % 4) * F, (i // 4) * F, (i % 4 + 1) * F, (i // 4 + 1) * F)) for i in range(sheet.height // F * 4)]
    cache = {}
    def cell(i, k):
        if (i, k) not in cache: cache[(i, k)] = raw[i].resize((F * k, F * k), Image.NEAREST)
        return cache[(i, k)]
    wbase = RP.wheel_base(); wspin = [RP.wheel_spin(i) for i in range(4)]; shelf0 = RP.shelf()
    fl = ImageFont.truetype(FONT, 32); fa = ImageFont.truetype(FONT, 48); fae = ImageFont.truetype(FONT, 28); fb = ImageFont.truetype(FONT, 40)
    N = int(secs * FPS) + 2
    gy = ch - 150; cxm = cw // 2
    for t in range(N):
        sec = t / FPS
        idx = max(i for i, s0 in enumerate(ACT2_T) if s0 <= sec) if sec < ACT2_TOTAL else len(ACT2) - 1
        dur, fr, rate, lk, le, bub, scene = ACT2[idx]
        lt = min(sec - ACT2_T[idx], dur)
        can = Image.new('RGBA', (cw, ch), (255, 248, 236, 255)); dd = ImageDraw.Draw(can)
        dd.rectangle((0, gy, cw, ch), fill=(238, 222, 198, 255)); dd.line((0, gy, cw, gy), fill=(214, 192, 164, 255), width=4)
        if line and sec < secs - 1.6:
            w = dd.textlength(line, font=fl); dd.text(((cw - w) / 2, 34), line, font=fl, fill=SOFT)
        def shadow(x0, w, y): dd.ellipse((x0 + w * 0.18, y - 12, x0 + w * 0.82, y + 12), fill=(222, 202, 176, 255))
        if scene in ('walkin', 'floor'):
            k = 9; img = cell(fr[int(lt * rate) % len(fr)], k)
            x = cxm - F * k / 2 - ((dur - lt) * rate * 3.2 * k if scene == 'walkin' else 0)
            shadow(x, F * k, gy); can.alpha_composite(img, (int(round(x)), gy - F * k + 3 * k)); bub_y = gy - F * k + 10
        elif scene == 'wheel':
            kw, k = 8, 6                                        # 바퀴 60×62 ×8, 고양이 56×6
            wb = wbase.resize((60 * kw, 62 * kw), Image.NEAREST)
            spin = min(1.0, lt / 0.6)                           # 서서히 빨라짐
            ws = wspin[int(lt * 14 * spin) % 4].resize((60 * kw, 62 * kw), Image.NEAREST)
            wx = cxm - 30 * kw; wy = gy - 62 * kw + 4 * kw
            can.alpha_composite(wb, (wx, wy)); can.alpha_composite(ws, (wx, wy))
            img = cell(fr[int(lt * rate) % len(fr)], k)
            ccy = wy + (62 - 1 - 34) * kw                       # 바퀴 중심(이미지 좌표)
            bottom = ccy + (26 - 4) * kw                        # 달리는 면
            can.alpha_composite(img, (int(cxm - F * k / 2), int(bottom - F * k + 3 * k)))
            bub_y = wy
        else:
            k = 6; shelf_w = 64 * 6; sx = cxm + 20; sy = 330    # 선반 윗면(캔버스 y)
            sh = shelf0.resize((shelf0.width * 6, shelf0.height * 6), Image.NEAREST)
            for px0 in (sx + 24, sx + shelf_w - 44):            # 선반 기둥
                dd.rectangle((px0, sy + 60, px0 + 20, gy), fill=(184, 142, 100, 255), outline=INK, width=3)
            can.alpha_composite(sh, (sx, sy))
            x0, y0 = cxm - 360, gy                              # 바닥 출발점(고양이 발)
            x1, y1 = sx + shelf_w / 2 - F * k / 2, sy + 4       # 선반 위 착지점
            if scene == 'jump':
                u = min(1.0, max(0.0, (lt - 0.6) / 0.7))          # 0.6초 웅크림 → 0.7초 비행 → 착지
                fi = 14 if lt < 0.6 else (18 if u < 1 else 0)
                x = x0 + (x1 - x0) * u; y = y0 + (y1 - y0) * u - math.sin(u * math.pi) * 170
                img = cell(fi, k)
                if u <= 0: shadow(x, F * k, gy)
            else:
                x, y = x1, y1; img = cell(fr[int(lt * rate) % len(fr)], k)
            can.alpha_composite(img, (int(round(x)), int(round(y - F * k + 3 * k))))
            bub_y = sy - F * k
        if bub:
            bw = dd.textlength(bub, font=fb) + 48; bx = cxm + 150; by = bub_y
            dd.rounded_rectangle((bx, by, bx + bw, by + 70), radius=20, fill=(255, 255, 255, 255), outline=INK, width=4)
            dd.text((bx + 24, by + 12), bub, font=fb, fill=INK)
        if lk:
            w = dd.textlength(lk, font=fa); dd.text(((cw - w) / 2, gy + 26), lk, font=fa, fill=INK)
            w = dd.textlength(le, font=fae); dd.text(((cw - w) / 2, gy + 86), le, font=fae, fill=SOFT)
        if sec >= secs - 1.6:
            fe2 = ImageFont.truetype(FONT, 40); fs2 = ImageFont.truetype(FONT, 26)
            t1 = '팔로우하고 다음 손님 만나기'; t2 = 'Follow to meet the next guest'
            bw = max(dd.textlength(t1, font=fe2), dd.textlength(t2, font=fs2)) + 60
            bx0 = (cw - bw) / 2; by0 = 34
            dd.rounded_rectangle((bx0, by0, bx0 + bw, by0 + 110), radius=22, fill=PINK)
            dd.text(((cw - dd.textlength(t1, font=fe2)) / 2, by0 + 14), t1, font=fe2, fill=(255, 250, 244))
            dd.text(((cw - dd.textlength(t2, font=fs2)) / 2, by0 + 66), t2, font=fs2, fill=(255, 236, 236))
        can.convert('RGB').save(os.path.join(tmp, f'n{t}.png'))
    src = os.path.join(tmp, 'acts2.mp4')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', os.path.join(tmp, 'n%d.png'),
                    '-c:v', 'libx264', '-crf', '12', '-pix_fmt', 'yuv420p', src], check=True)

# 소리
inputs =['-loop', '1', '-i', bgp, '-stream_loop', '-1', '-i', src, '-stream_loop', '-1', '-i', f'{AUD}/{bgm}.ogg']
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
