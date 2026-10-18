# 정리된 삼색냥 16포즈(frames.pkl, 56x56) → 9마리 시트.
# 털 역할: W 흰털 / O 주황 / B 갈색 / X 외곽선 / P 분홍. 단색 고양이는 역할 대신 '명암 단계'로 칠해 얼룩 흔적을 없앤다.
import pickle, numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

cent, frames = pickle.load(open('frames20.pkl', 'rb'))
F = 56
FONT = "/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc"
W = [1, 7, 11, 6]; O = [0, 10]; B = [4, 9, 2]; OUT = 3; PINK = [5, 8]
# 명암 단계(0 밝음 ~ 3 어두움)
LVL = {1: 0, 7: 1, 11: 2, 6: 3, 0: 1, 10: 2, 4: 1, 9: 1, 2: 2}
ROLE = {}
for k in W: ROLE[k] = 'W'
for k in O: ROLE[k] = 'O'
for k in B: ROLE[k] = 'B'

def eye_boxes(g):
    dark = g == OUT
    lab, n = ndimage.label(dark)
    near = ndimage.binary_dilation(g < 0)
    out = []
    for i in range(1, n + 1):
        c = lab == i
        if (c & near).any(): continue
        s = c.sum()
        if s < 4 or s > 30: continue
        ys, xs = np.where(c)
        if xs.max() - xs.min() > 6 or ys.max() - ys.min() > 6: continue
        out.append((xs.min(), ys.min(), xs.max(), ys.max()))
    out=sorted(out)
    if len(out)>=2:
        top=min(b[1] for b in out); out=[b for b in out if b[1]<=top+2]
    return out

def ramp(base, dark=0.62, light=1.12):
    b = np.array(base, float)
    return [tuple(np.clip(b * light, 0, 255).astype(int)), tuple(b.astype(int)),
            tuple((b * 0.84).astype(int)), tuple((b * dark).astype(int))]

WHITE = [(252, 244, 236), (238, 222, 212), (214, 196, 190), (176, 160, 166)]

CATS = {
    'calico':  None,
    'cheese':  dict(name='치즈냥', fur=ramp((240, 150, 70)), white=WHITE, X=(60, 26, 16), iris=(104, 170, 70), stripes=True),
    'cow':     dict(name='젖소냥', fur=ramp((64, 58, 72)), white=WHITE, keepPatch=True, X=(22, 12, 20), iris=(110, 160, 90)),
    'black':   dict(name='까망냥', fur=[(88, 82, 106), (62, 56, 78), (48, 42, 62), (36, 30, 46)], X=(14, 8, 16), iris=(244, 180, 56), acc='bandana'),
    'tuxedo':  dict(name='턱시도냥', fur=[(84, 78, 98), (60, 54, 74), (46, 40, 58), (34, 28, 44)], white=WHITE, X=(16, 8, 16), iris=(236, 180, 70), acc='bowtie'),
    'mackerel':dict(name='고등어냥', fur=ramp((150, 152, 164)), white=WHITE, X=(32, 26, 34), iris=(214, 172, 70), stripes=True),
    'siamese': dict(name='샴냥', fur=ramp((240, 226, 202), dark=0.8, light=1.04), point=ramp((104, 74, 60)), X=(48, 30, 26), iris=(70, 140, 232)),
    'gray':    dict(name='회색냥', fur=ramp((148, 148, 162)), white=WHITE, whiteFaceOnly=True, X=(32, 28, 36), iris=(126, 170, 96), acc='collar'),
    'moon':    dict(name='달냥', fur=[(255, 255, 255), (240, 240, 250), (220, 222, 238), (188, 192, 214)], X=(84, 86, 112), iris=(80, 150, 236), iris2=(236, 186, 64)),
}

def point_mask(g, eyes):
    """샴: 귀·얼굴·발·꼬리(가는 부위)를 진하게. 0~1 세기."""
    sil = g >= 0
    core = ndimage.binary_opening(sil, structure=np.ones((7, 7)))
    thin = sil & ~ndimage.binary_dilation(core, iterations=1)
    m = thin.astype(float) * 0.9
    m[np.isin(g, PINK)] = 0
    if len(eyes) >= 1:
        cx = np.mean([(a + c) / 2 for a, _, c, _ in eyes]); cy = np.mean([(b + d) / 2 for _, b, _, d in eyes]) + 2.5
        rx = 7.5 if len(eyes) >= 2 else 6; ry = 6
        yy, xx = np.mgrid[0:F, 0:F]
        d = ((xx - cx) / (rx + (abs(eyes[-1][0] - eyes[0][0]) / 2 if len(eyes) >= 2 else 0))) ** 2 + ((yy - cy) / ry) ** 2
        m = np.maximum(m, np.clip(1.25 - d, 0, 1))
    ys = np.where(sil.any(1))[0]
    if len(ys):                                   # 발끝(맨 아래 3줄)
        m[ys.max() - 3:, :] = np.maximum(m[ys.max() - 3:, :], 0.8 * sil[ys.max() - 3:, :])
    return ndimage.gaussian_filter(m, 0.6)

def mix(a, b, t): return tuple(int(a[i] * (1 - t) + b[i] * t) for i in range(3))

# ── 소품(목 위치: 프레임별 수동 앵커) ──
# (cx, cy, 반폭, 방향) 방향 f=정면, s=옆(오른쪽 보기)
ANCH = {0: (32, 43, 6, 'f'), 1: (29, 43, 6, 'f'), 2: (25, 44, 6, 'f'), 3: (24, 43, 6, 'f'),
        4: (44, 41, 2, 's'), 5: (41, 41, 2, 's'), 6: (39, 41, 2, 's'), 7: (36, 41, 2, 's'),
        8: (33, 47, 6, 'f'), 15: (22, 45, 6, 'f'), 16: (24, 43, 6, 'f'), 19: (32, 43, 6, 'f')}
RED, REDD, GOLD, GOLDD = (214, 58, 66), (150, 34, 48), (250, 204, 72), (176, 126, 40)

def accessory(img, g, i, kind):
    if kind is None or i not in ANCH: return
    cx, cy, hw, d = ANCH[i]
    sil = g >= 0
    def put(x, y, c, force=False):
        if 0 <= x < F and 0 <= y < F and (force or sil[y, x]): img[y, x] = (*c, 255)
    if d == 'f':
        if kind in ('collar', 'bandana'):
            for x in range(cx - hw, cx + hw + 1):
                put(x, cy, RED); put(x, cy + 1, REDD)
        if kind == 'collar':                      # 방울
            for dx in (-1, 0, 1):
                for dy in (2, 3, 4): put(cx + dx, cy + dy, GOLD, True)
            put(cx, cy + 4, GOLDD, True); put(cx - 1, cy + 2, (255, 240, 170), True)
            for x, y in ((cx - 2, cy + 3), (cx + 2, cy + 3), (cx - 1, cy + 5), (cx + 1, cy + 5), (cx, cy + 5)): put(x, y, (70, 40, 20), True)
        if kind == 'bandana':                     # 아래로 늘어진 삼각 + 흰 점
            for r in range(1, 5):
                for x in range(cx - (5 - r), cx + (5 - r) + 1): put(x, cy + 1 + r, RED if r < 4 else REDD, True)
                put(cx - (5 - r) - 1, cy + 1 + r, (40, 14, 20), True); put(cx + (5 - r) + 1, cy + 1 + r, (40, 14, 20), True)
            put(cx, cy + 6, (40, 14, 20), True)
            for x, y in ((cx - 2, cy + 2), (cx + 2, cy + 3), (cx, cy + 4)): put(x, y, (255, 236, 236), True)
        if kind == 'bowtie':
            y = cy + 1
            bow = ["##.....##", "#R#...#R#", "#RR#K#RR#", "#R#...#R#", "##.....##"]
            for r, row in enumerate(bow):
                for c, ch in enumerate(row):
                    x = cx - 4 + c; yy = y - 2 + r
                    if ch == '#': put(x, yy, (60, 14, 22), True)
                    elif ch == 'R': put(x, yy, RED, True)
                    elif ch == 'K': put(x, yy, REDD, True)
            for c in range(9):
                for r in range(5):
                    if bow[r][c] == '.' and r in (1, 2, 3) and 2 <= c <= 6:
                        put(cx - 4 + c, y - 2 + r, RED if r == 2 else REDD, True)
    else:   # 옆모습: 세로 띠
        for t in range(0, 6):
            x = cx - (t // 3); y = cy - 3 + t
            put(x, y, RED); put(x + 1, y, REDD)
        if kind == 'collar':
            for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)): put(cx + 1 + dx, cy + 3 + dy, GOLD, True)
        if kind == 'bandana':
            for r in range(3):
                for x in range(cx - 1 - r, cx + 2): put(x, cy + 3 + r, RED, True)
        if kind == 'bowtie':
            for dx, dy, c in ((1, 2, RED), (2, 2, RED), (1, 3, REDD), (2, 1, RED), (2, 3, RED)): put(cx + dx, cy + dy, c, True)

EYES = {1: [(20, 34, 24, 35), (34, 30, 39, 35)], 8: [(25, 36, 28, 40), (38, 36, 41, 39)], 9: [],
        14: [(13, 39, 16, 42), (23, 39, 26, 41)], 15: [(15, 37, 18, 37), (26, 37, 29, 37)], 16: [(14, 30, 18, 35), (28, 30, 33, 34)]}
def render(key, i):
    g = frames[i]
    spec = CATS[key]
    img = np.zeros((F, F, 4), np.uint8)
    eyes = EYES[i] if i in EYES else eye_boxes(g)
    em = np.zeros(g.shape, bool)
    for x0, y0, x1, y1 in eyes: em[y0:y1 + 2, x0:x1 + 1] = True
    pm = point_mask(g, eyes) if spec and 'point' in spec else None
    rightEyeX = eyes[-1][0] if len(eyes) >= 2 else 999
    for y in range(F):
        for x in range(F):
            k = g[y, x]
            if k < 0: continue
            col = tuple(cent[k].astype(int))
            if spec is not None:
                r = ROLE.get(k)
                if em[y, x] and k not in PINK:
                    if k == OUT: col = (24, 14, 22)
                    elif k == 1: col = (255, 255, 255)
                    elif r == 'W':
                        col = (spec['white'] if 'white' in spec and not spec.get('whiteFaceOnly') else spec['fur'])[LVL[k]]
                    else:
                        ic = spec['iris2'] if 'iris2' in spec and x >= rightEyeX else spec['iris']
                        col = ic if k in (10, 0) else tuple(int(v * 0.75) for v in ic)
                elif k == OUT: col = spec['X']
                elif r is not None:
                    lv = LVL[k]
                    if r == 'W' and 'white' in spec and not spec.get('whiteFaceOnly'): col = spec['white'][lv]
                    elif r == 'W' and spec.get('whiteFaceOnly') and y < 46 and lv <= 1: col = spec['white'][lv]
                    elif spec.get('keepPatch') and r == 'W': col = spec['white'][lv]
                    else:
                        col = spec['fur'][lv]
                        if spec.get('stripes') and r != 'W' and lv <= 1 and (y + (x // 5)) % 4 == 0:
                            col = spec['fur'][min(3, lv + 1)]
                    if pm is not None: col = mix(col, spec['point'][lv], float(np.clip(pm[y, x], 0, 1)))
            img[y, x] = (*col, 255)
    if spec is not None: accessory(img, g, i, spec.get('acc'))
    return Image.fromarray(img)

REFX = {0: 32.5, 1: 29, 2: 25, 3: 23.5, 4: 48, 5: 44.5, 6: 42, 7: 40, 8: 33, 15: 22, 16: 23.5, 19: 32.5}
def align(im, i):
    a = np.array(im)
    if i in REFX: dx = round((44 if 4 <= i <= 7 else 28) - REFX[i])
    else:
        xs = np.where(a[:, :, 3].any(0))[0]; dx = round(28 - (xs.min() + xs.max()) / 2)
    out = Image.new('RGBA', (F, F), (0, 0, 0, 0)); out.alpha_composite(im, (0, 0)) if dx == 0 else out.paste(im, (dx, 0))
    xs = np.where(a[:, :, 3].any(0))[0]
    if xs.min() + dx < 0 or xs.max() + dx >= F: print('edge!', i, dx)
    return out

if __name__ == '__main__':
    order = ['calico', 'cheese', 'cow', 'black', 'tuxedo', 'mackerel', 'siamese', 'gray', 'moon']
    names = {'calico': '삼색냥'}
    S = 3; cell = F * S; pad = 6
    sw = cell * 4 + pad * 3
    big = Image.new('RGBA', ((sw + 40) * 3 + 40, (cell * 5 + pad * 4 + 70) * 3 + 40), (34, 32, 40, 255))
    d = ImageDraw.Draw(big); f = ImageFont.truetype(FONT, 26)
    import os; os.makedirs('out', exist_ok=True)
    for n, key in enumerate(order):
        ox = 40 + (n % 3) * (sw + 40); oy = 40 + (n // 3) * (cell * 5 + pad * 4 + 70)
        d.text((ox, oy + 4), names.get(key) or CATS[key]['name'], font=f, fill=(240, 232, 220))
        sh = Image.new('RGBA', (F * 4, F * 5), (0, 0, 0, 0))
        for i in range(20):
            im = align(render(key, i), i)
            sh.alpha_composite(im, ((i % 4) * F, (i // 4) * F))
            big.alpha_composite(im.resize((cell, cell), Image.NEAREST), (ox + (i % 4) * (cell + pad), oy + 50 + (i // 4) * (cell + pad)))
        sh.save(f'out/cat_{key}.png')
    big.convert('RGB').save('cat_art_I_all.png')
    print('ok')
