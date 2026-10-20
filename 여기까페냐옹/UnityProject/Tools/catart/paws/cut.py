# 앞발 원화(4×3, 마젠타 배경) → 같은 크기 칸 12개 시트(paws.png) — 발 바닥선 맞춤
import numpy as np
from PIL import Image
from scipy import ndimage
a = np.array(Image.open('src.png').convert('RGB')).astype(int)
H, W = a.shape[:2]
r, g, b = a[..., 0], a[..., 1], a[..., 2]
mag = (r > 150) & (b > 150) & (g < 0.62 * np.minimum(r, b))          # 마젠타 + 가장자리 번짐
fg = ~mag
fg = ndimage.binary_opening(fg, np.ones((2, 2)))
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
