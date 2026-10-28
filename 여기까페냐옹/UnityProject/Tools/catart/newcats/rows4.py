# ChatGPT가 기본 포즈를 5줄로 그려 올 때: 위 4줄만 정사각 4×4 칸에 다시 놓는다
import sys, numpy as np
from PIL import Image
src, dst = sys.argv[1], sys.argv[2]
im = Image.open(src).convert('RGB'); a = np.asarray(im).astype(int)
rows = (a.min(2) < 235).any(1); bands = []; y = 0; H = a.shape[0]
while y < H:
    if rows[y]:
        y0 = y
        while y < H and rows[y]: y += 1
        bands.append((y0, y))
    y += 1
m = []
for b in bands:
    if m and b[0] - m[-1][1] < 12: m[-1] = (m[-1][0], b[1])
    else: m.append(b)
m = [b for b in m if b[1] - b[0] > 40]
print('rows', len(m), m)
W = im.size[0]; out = Image.new('RGB', (W, W), (255, 255, 255)); slot = W / 4
for i, (y0, y1) in enumerate(m[:4]):
    out.paste(im.crop((0, y0, W, y1)), (0, int(i * slot + (slot - (y1 - y0)) / 2)))
out.save(dst)
