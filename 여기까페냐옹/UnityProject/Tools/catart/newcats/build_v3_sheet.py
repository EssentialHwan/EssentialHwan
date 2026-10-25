# 리디자인 시트(4×13) 조립: v3_check.py가 만든 프레임(v3_frames.pkl) → cat_{name}.png
import pickle, sys, numpy as np
from PIL import Image
from scipy import ndimage
import convert as C
name = sys.argv[1] if len(sys.argv) > 1 else 'black'
fr = pickle.load(open('v3_frames.pkl', 'rb'))
def clean(f):   # 효과선(~)·잡티: 가장 큰 덩어리와 떨어진 작은 조각 제거
    m = f[..., 3] > 0; lab, n = ndimage.label(m, structure=np.ones((3, 3)))
    if n <= 1: return f
    sizes = ndimage.sum(m, lab, range(1, n + 1)); keep = np.zeros(n + 1, bool)
    for i, sz in enumerate(sizes, 1): keep[i] = sz >= 12
    g = f.copy(); g[~keep[lab]] = 0; return g
F = C.F
sheet = Image.new('RGBA', (F * 4, F * 13), (0, 0, 0, 0))
def put(cellno, f):
    f = clean(f); h, w = f.shape[:2]
    c = Image.new('RGBA', (F, F), (0, 0, 0, 0)); c.alpha_composite(Image.fromarray(f), ((F - w) // 2, 55 - h))
    sheet.alpha_composite(c, ((cellno % 4) * F, (cellno // 4) * F))
for g in range(25): put(g, fr[C.MAP[g]])
for j, src in enumerate(range(32, 56)): put(28 + j, fr[src])
sheet.save(f'cat_{name}_v3.png'); print('ok', sheet.size)
