# 보호소 고양이 AI 원화 → 게임 시트. 아기 고양이는 어른보다 작게(앉기 높이 ≈ 33px).
import numpy as np
from PIL import Image
import convert as C, convert2 as C2
import build_new4 as B

SIZE = {'kitten': 0.78, 'sphynx': 1.0, 'senior': 1.0}

def build(name):
    a, cells = C.poses(f'{name}_src.png')
    missing = [k for k in range(16) if k not in cells]
    if missing: print('MISSING', name, missing)
    allfg = np.zeros(a.shape[:2], bool)
    for m in cells.values(): allfg |= m
    pal, _ = C.quantize(a, allfg, 64)
    s = B.fit_scale(cells) * SIZE.get(name, 1.0)
    frames = {k: C2.down2(a, pal.astype(np.float32), cells[k], s) for k in range(16)}
    F = C.F
    sheet = Image.new('RGBA', (F * 4, F * 7), (0, 0, 0, 0))
    for g in range(25):
        fr = frames[C.MAP[g]]; h, w = fr.shape[:2]
        cell = Image.new('RGBA', (F, F), (0, 0, 0, 0))
        cell.alpha_composite(Image.fromarray(fr), ((F - w) // 2, 55 - h))
        sheet.alpha_composite(cell, ((g % 4) * F, (g // 4) * F))
    sheet.save(f'cat_{name}.png')
    print(name, 'scale', round(s, 4), 'sit', frames[0].shape[:2], 'walk', frames[4].shape[:2])
    return sheet

if __name__ == '__main__':
    import sys
    for n in sys.argv[1:]: build(n)
