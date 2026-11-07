# 새 고양이 4종(스코티시폴드·먼치킨·페르시안·카오스) AI 원화 → 게임 시트. 기존 고양이와 크기를 맞춘다(앉기 높이≈42, 걷기 폭≈48).
import numpy as np
from PIL import Image
import convert as C, convert2 as C2

def fit_scale(cells):
    def bb(m):
        ys, xs = np.nonzero(m); return xs.max() - xs.min() + 1, ys.max() - ys.min() + 1
    sw, sh = bb(cells[0]); ww, wh = bb(cells[4])
    s = min(42.0 / sh, 48.0 / ww)
    for k, m in cells.items():                          # 어떤 포즈도 칸(54px)을 넘지 않게
        w, h = bb(m); s = min(s, 55.0 / w, 53.0 / h)
    return s

def build(name):
    a, cells = C.poses(f'{name}_src.png')
    missing = [k for k in range(16) if k not in cells]
    if missing: print('MISSING', name, missing)
    allfg = np.zeros(a.shape[:2], bool)
    for m in cells.values(): allfg |= m
    pal, _ = C.quantize(a, allfg, 64)
    s = fit_scale(cells)
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
    out = [build(n) for n in ('fold', 'munchkin', 'persian', 'tortie')]
    F = C.F
    c = Image.new('RGBA', (F * 4 * 4 + 30, F * 7), (60, 56, 64, 255))
    for i, sh in enumerate(out): c.alpha_composite(sh, (i * (F * 4 + 10), 0))
    c.resize((c.width * 2, c.height * 2), Image.NEAREST).save('new4_prev.png')
