# AI 원화(1254px, 4×4 포즈) → 게임 시트(56px 칸, 4×7 = 25프레임)
import numpy as np, sys
from PIL import Image
from scipy import ndimage
from seg import fg_mask
F = 56
# 게임 프레임 번호 → 원화 포즈 번호(0~15)
MAP = {0: 0, 1: 1, 2: 2, 3: 3, 4: 4, 5: 5, 6: 6, 7: 7, 8: 8, 9: 9, 10: 10, 11: 11, 12: 12, 13: 13, 14: 12, 15: 15,
       16: 14, 17: 9, 18: 5, 19: 3, 20: 2, 21: 2, 22: 14, 23: 3, 24: 8}

def poses(path):
    a, fg = fg_mask(path)
    lab, n = ndimage.label(fg)
    H = a.shape[0] / 4
    cells = {}
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        m = lab[sl] == i
        if m.sum() < 60: continue
        ys, xs = np.nonzero(m)
        cy, cx = ys.mean() + sl[0].start, xs.mean() + sl[1].start
        k = int(cy // H) * 4 + int(cx // H)
        cells.setdefault(k, np.zeros(fg.shape, bool))[sl] |= m
    return a, cells

def quantize(a, fg, ncol=48):
    pix = a[fg].astype(np.uint8)
    pal_img = Image.fromarray(pix.reshape(1, -1, 3)).quantize(colors=ncol, method=Image.Quantize.MEDIANCUT)
    pal = np.array(pal_img.getpalette()[:ncol * 3]).reshape(-1, 3)
    idx = np.full(fg.shape, -1, int)
    idx[fg] = np.array(pal_img).reshape(-1)
    return pal, idx

def down(pal, idx, m, s):
    ys, xs = np.nonzero(m)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    W, H = max(1, round((x1 - x0) * s)), max(1, round((y1 - y0) * s))
    out = np.zeros((H, W, 4), np.uint8)
    sub = np.where(m[y0:y1, x0:x1], idx[y0:y1, x0:x1], -1)
    global DARK
    DARK = pal.sum(1) < 200
    for ty in range(H):
        for tx in range(W):
            b = sub[int(ty / s):max(int(ty / s) + 1, int((ty + 1) / s)), int(tx / s):max(int(tx / s) + 1, int((tx + 1) / s))]
            v = b[b >= 0]
            if v.size < b.size * 0.45: continue
            dv = v[DARK[v]]
            c = np.bincount(dv).argmax() if dv.size >= v.size * 0.3 else np.bincount(v).argmax()   # 눈동자·선 같은 진한 점은 살린다
            out[ty, tx, :3] = pal[c]; out[ty, tx, 3] = 255
    # 실루엣 테두리는 진한 외곽선으로(원화의 굵은 외곽선이 축소하며 흐려지는 것 보정)
    al = out[..., 3] > 0
    al = ndimage.binary_fill_holes(al) & al
    edge = al & ~ndimage.binary_erosion(al, structure=[[0, 1, 0], [1, 1, 1], [0, 1, 0]])
    lum = pal.sum(1); dark = pal[lum.argmin()]
    out[edge, :3] = dark
    return out

def build(src, scale, name):
    a, cells = poses(src)
    allfg = np.zeros(a.shape[:2], bool)
    for m in cells.values(): allfg |= m
    pal, idx = quantize(a, allfg)
    frames = {k: down(pal, idx, cells[k], scale) for k in range(16)}
    sheet = Image.new('RGBA', (F * 4, F * 7), (0, 0, 0, 0))
    for g in range(25):
        fr = frames[MAP[g]]
        h, w = fr.shape[:2]
        if h > 54 or w > F: print('TOO BIG', name, g, w, h)
        cell = Image.new('RGBA', (F, F), (0, 0, 0, 0))
        cell.alpha_composite(Image.fromarray(fr), ((F - w) // 2, 55 - h))
        sheet.alpha_composite(cell, ((g % 4) * F, (g // 4) * F))
    sheet.save(f'cat_{name}.png')
    big = Image.new('RGBA', sheet.size, (60, 56, 64, 255)); big.alpha_composite(sheet)
    big.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(f'{name}_prev.png')
    for k in (0, 4): print(name, k, frames[k].shape)

if __name__ == '__main__':
    build('delivery_src.png', 0.169, 'delivery')
    build('chef_src.png', 0.165, 'inspector')
