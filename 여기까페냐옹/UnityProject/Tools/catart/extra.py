# 시트에 없는 4포즈를 기존 포즈에서 손으로 고쳐 만든다(군집 격자 단위). 16 하악 · 17 뒹굴 · 18 점프 · 19 냥펀치
import pickle, numpy as np
from scipy import ndimage
cent, frames = pickle.load(open('frames.pkl', 'rb'))
frames = frames[:16]
F = 56; OUT = 3; WH = 1; WS = 7; PK = 5; PD = 8

def reoutline(g):
    """실루엣 가장자리(투명과 맞닿은 털)를 외곽선으로."""
    sil = g >= 0
    edge = sil & ndimage.binary_dilation(~sil)
    g = g.copy(); g[edge & (g != OUT)] = OUT
    return g

def bottom_align(g):
    ys, xs = np.where(g >= 0)
    out = -np.ones((F, F), int)
    dy = (F - 2) - ys.max()
    for y, x in zip(ys, xs):
        if 0 <= y + dy < F: out[y + dy, x] = g[y, x]
    return out

def stamp(g, x0, y0, rows, keymap):
    for r, row in enumerate(rows):
        for c, ch in enumerate(row):
            if ch in keymap:
                y, x = y0 + r, x0 + c
                if 0 <= y < F and 0 <= x < F: g[y, x] = keymap[ch]

KM = {'#': OUT, 'w': WH, 'v': WS, 'p': PK, 'P': PD, 'o': 0, 'O': 10, 'B': 9, 'D': 2, '_': -1}

# ── 16 하악: 귀 젖히기 · 성난 눈 · 송곳니 · 털 부풀리기 ──
g = frames[3].copy()
g[:18, :] = -1                                   # 귀 끝 잘라내기
# 옆으로 젖힌 귀
stamp(g, 2, 17, ["..####", ".#ppoo#", "#pppoo#", ".#ppo#", "..##"], KM)
stamp(g, 37, 17, ["####", "#BBpp#", "#BBppp#", "#Bpp#", "##"], KM)
for x in range(8, 38): g[18, x] = OUT if g[18, x] >= 0 else g[18, x]
# 눈: 안쪽으로 처진 눈꺼풀
for y, a, b in ((30, 13, 19), (31, 13, 19), (32, 15, 19), (33, 17, 19)): g[y, a:b] = OUT
for y, a, b in ((30, 28, 35), (31, 28, 35), (32, 28, 32), (33, 28, 30)): g[y, a:b] = OUT
g[29, 12:16] = OUT; g[29, 33:37] = OUT                                     # 찌푸린 눈썹 끝
# 입 크게 + 송곳니
stamp(g, 19, 36, ["##PP##", "#PPPP#", "#wPPw#", "#PPPP#", ".####"], KM)
# 등·옆 털 부풀리기(삐죽)
sil = g >= 0
for y in range(41, 54, 2):
    xs = np.where(sil[y])[0]
    if len(xs) == 0: continue
    for x, d in ((xs.min(), -1), (xs.max(), 1)):
        if 0 <= x + d < F: g[y, x + d] = OUT; g[y, x] = 9 if d > 0 else 0
g = reoutline(g)
frames.append(bottom_align(g))

# ── 17 뒹굴: 눈웃음(2)을 반시계 90° 눕히고 앞발 하나를 위로 ──
g = np.rot90(frames[2], 1).copy()                # 머리 왼쪽, 발 오른쪽
g = bottom_align(g)
ys, xs = np.where(g >= 0)
top = ys.min()
# 허공 앞발(위로 꼬부린)
px = int(np.median(xs)) + 4
frames.append(bottom_align(reoutline(g)))

# ── 18 점프: 걷기(5)를 코가 위로 20° 기울이고 다리 쭉 ──
src = frames[5]
lab = np.where(src >= 0, src + 1, 0)
r = ndimage.rotate(lab, 20, reshape=False, order=0, mode='constant', cval=0)
g = r - 1
# 뒷다리 뒤로 쭉: 아래쪽 왼편 덩어리를 왼쪽으로 2칸 끌기
ys, xs = np.where(g >= 0)
low = ys.max() - 6
for y in range(low, F):
    row = g[y].copy()
    left = xs.min() + 14
    for x in range(F):
        if row[x] >= 0 and x < left and x - 2 >= 0: g[y, x - 2] = row[x]
g = reoutline(g)
frames.append(bottom_align(g))

# ── 19 냥펀치: 앉기(0)에서 오른 앞발 번쩍 ──
g = frames[0].copy()
paw = ["..####..",
       ".#wwww#.",
       "#wpwpww#",
       "#wwpPPw#",
       "#wwPPPw#",
       ".#wwww#.",
       "..#ww#..",
       "..#ww#..",
       "..#vw#..",
       "..#vw#.."]
stamp(g, 43, 33, paw, KM)
# 원래 오른 앞발 자리 → 몸통
for y in range(47, 54):
    for x in range(33, 40):
        if g[y, x] >= 0: g[y, x] = WH if y < 52 else OUT
g = reoutline(g)
# 활짝 웃는 입 대신 '앙' 입
stamp(g, 31, 37, ["#PP#", ".##."], KM)
frames.append(bottom_align(g))

pickle.dump((cent, frames), open('frames20.pkl', 'wb'))

# 미리보기
from PIL import Image
S = 8
img = Image.new('RGB', (F * S * 4, F * S), (40, 40, 48))
for n, g in enumerate(frames[16:]):
    for y in range(F):
        for x in range(F):
            k = g[y, x]
            if k >= 0:
                img.paste(tuple(cent[k].astype(int)), (n * F * S + x * S, y * S, n * F * S + x * S + S, y * S + S))
img.save('extra_prev.png')
print(len(frames))
