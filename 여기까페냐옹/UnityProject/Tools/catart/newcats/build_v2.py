# 리디자인(캡슐 그림체) 원화 → 게임 시트. build_new4와 같되, 작은 강조색(눈 노랑·하이라이트·분홍)이 팔레트에서 묻히지 않게 따로 뽑아 넣는다.
import numpy as np, sys
from PIL import Image
import convert as C, convert2 as C2, build_new4 as B

def accent_palette(a, fg):
    px = a[fg].astype(int)
    r, g, b = px[:, 0], px[:, 1], px[:, 2]
    sat = px.max(1) - px.min(1)
    groups = [
        (r > 170) & (g > 110) & (b < 110) & (sat > 70),   # 노랑·호박색 눈
        (px.min(1) > 225),                                # 흰 하이라이트
        (r > 190) & (g < 170) & (b > 120) & (r - g > 40), # 분홍 귀·볼
        (b > 150) & (b - r > 40),                         # 파란 눈
    ]
    out = []
    for m in groups:
        if m.sum() < 30: continue
        sub = px[m].astype(np.uint8).reshape(1, -1, 3)
        q = Image.fromarray(sub).quantize(colors=4, method=Image.Quantize.MEDIANCUT)
        out.append(np.array(q.getpalette()[:12]).reshape(-1, 3))
    return np.vstack(out) if out else np.zeros((0, 3))

def build(name, out_name=None):
    a, cells = C.poses(f'{name}_src.png')
    missing = [k for k in range(16) if k not in cells]
    if missing: print('MISSING', name, missing)
    allfg = np.zeros(a.shape[:2], bool)
    for m in cells.values(): allfg |= m
    pal, _ = C.quantize(a, allfg, 64)
    pal = np.vstack([pal, accent_palette(a, allfg)])
    s = B.fit_scale(cells)
    frames = {k: C2.down2(a, pal.astype(np.float32), cells[k], s) for k in range(16)}
    F = C.F
    sheet = Image.new('RGBA', (F * 4, F * 7), (0, 0, 0, 0))
    for g in range(25):
        fr = frames[C.MAP[g]]; h, w = fr.shape[:2]
        cell = Image.new('RGBA', (F, F), (0, 0, 0, 0))
        cell.alpha_composite(Image.fromarray(fr), ((F - w) // 2, 55 - h))
        sheet.alpha_composite(cell, ((g % 4) * F, (g // 4) * F))
    sheet.save(f'cat_{out_name or name}.png')
    print(name, 'scale', round(s, 4))
    return sheet

if __name__ == '__main__':
    build(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
