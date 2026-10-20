# 앞발 원화(4×3, 마젠타 배경) → 같은 크기 칸 12개 시트(paws.png) — 발 바닥선 맞춤
import numpy as np
from PIL import Image
from scipy import ndimage
a = np.array(Image.open('src.png').convert('RGB')).astype(int)
H, W = a.shape[:2]
r, g, b = a[..., 0], a[..., 1], a[..., 2]
# 배경 = 순수 마젠타(core)와 이어진 마젠타 계열 픽셀만. 젤리·혀 테두리(진분홍)는 털 안쪽에 갇혀 있어 남는다.
core = (r > 200) & (b > 200) & (g < 90)
cand = (((r - g) > 55) & ((b - g) > 45)) | core
clab, _ = ndimage.label(cand)
bgids = np.unique(clab[core]); bgids = bgids[bgids > 0]
bg = np.isin(clab, bgids)
fg = ~bg
fg = ndimage.binary_opening(fg, np.ones((2, 2)))
# 배경과 맞닿은 가장자리의 보랏빛 번짐만 지우고, 남는 가장자리는 파랑을 눌러 준다
near = ndimage.binary_dilation(~fg, iterations=2) & fg
purple = near & ((b - g) > 25) & ((r - g) > 25)
fg &= ~purple
near = ndimage.binary_dilation(~fg, iterations=2) & fg
a[near, 2] = np.minimum(a[near, 2], np.maximum(a[near, 1], (a[near, 0] + a[near, 1]) // 2))
lab, n = ndimage.label(fg)
rows = [(0, H * 0.335), (H * 0.335, H * 0.665), (H * 0.665, H)]
cells = {}
for i, sl in enumerate(ndimage.find_objects(lab), 1):
    m = lab[sl] == i
    if m.sum() < 40: continue
    ys, xs = np.nonzero(m)
    cy, cx = ys.mean() + sl[0].start, xs.mean() + sl[1].start
    ri = next(k for k, (y0, y1) in enumerate(rows) if y0 <= cy < y1)
    ci = min(3, int(cx // (W / 4)))
    cells.setdefault(ri * 4 + ci, np.zeros(fg.shape, bool))[sl] |= m
def crop(m):
    ys, xs = np.nonzero(m)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    rgba = np.zeros((y1 - y0, x1 - x0, 4), np.uint8)
    rgba[..., :3] = a[y0:y1, x0:x1]; rgba[..., 3] = m[y0:y1, x0:x1] * 255
    return rgba
frames = {}
for k, m in cells.items():
    if k >= 8:
        # 쭙쭙이 줄: 왼쪽 작은 발과 오른발이 붙어 있다 → 둘 사이 가장 가는 세로줄에서 잘라 오른발만
        ys, xs = np.nonzero(m)
        x0, x1 = xs.min(), xs.max()
        cnt = m[:, x0:x1 + 1].sum(0)
        lo, hi = int((x1 - x0) * 0.25), int((x1 - x0) * 0.55)
        cut = x0 + lo + int(np.argmin(cnt[lo:hi]))
        m = m.copy(); m[:, :cut + 1] = False
        colc = m.sum(0); peak = colc.max()
        c = cut + 1
        while c < cut + 14 and colc[c] < 0.35 * peak: m[:, c] = False; c += 1   # 잘린 선 옆 얇은 세로 조각(왼발 외곽선)
        # 잘린 선에 붙은 왼발 부스러기 제거: 가장 큰 덩어리(오른발)와 선에 안 닿는 장식(하트·떨림선)만 남김
        cl, cn = ndimage.label(m)
        sizes = ndimage.sum(m, cl, range(1, cn + 1))
        main = int(np.argmax(sizes)) + 1
        for j in range(1, cn + 1):
            if j == main: continue
            if (cl[:, cut + 1:cut + 4] == j).any(): m[cl == j] = False
    if k == 10:
        # 이 칸은 왼발이 오른발 위로 겹쳐 그려져 있다 → 왼쪽 14px을 더 자르고 새 가장자리에 외곽선
        xs = np.nonzero(m.any(0))[0]
        m = m.copy(); m[:, :xs.min() + 14] = False
        e = m & ~ndimage.binary_erosion(m, structure=[[0, 1, 0], [1, 1, 1], [0, 1, 0]])
        e[:, xs.min() + 16:] = False
        a[e] = (58, 35, 40)
    frames[k] = crop(m)
for k in sorted(frames): print(k, frames[k].shape[:2])
CW = max(f.shape[1] for f in frames.values()) + 4
CH = max(f.shape[0] for f in frames.values()) + 2
sheet = Image.new('RGBA', (CW * 4, CH * 3), (0, 0, 0, 0))
for k, f in frames.items():
    h, w = f.shape[:2]
    sheet.alpha_composite(Image.fromarray(f), ((k % 4) * CW + (CW - w) // 2, (k // 4) * CH + CH - h))
sheet.save('paws.png')
print('cell', CW, CH)
bg = Image.new('RGBA', sheet.size, (60, 56, 64, 255)); bg.alpha_composite(sheet); bg.convert('RGB').resize((sheet.width // 2, sheet.height // 2), Image.NEAREST).save('paws_prev.png')
