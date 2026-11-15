# 방식 B: 프리멀티 알파 LANCZOS 축소 → 원화 팔레트로 스냅 → 실루엣 외곽선
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage
import convert as C

def down2(a, pal, m, s, sharpen=True):
    ys, xs = np.nonzero(m)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    W, H = max(1, round((x1 - x0) * s)), max(1, round((y1 - y0) * s))
    rgb = a[y0:y1, x0:x1].astype(np.float32) * m[y0:y1, x0:x1, None]
    al = m[y0:y1, x0:x1].astype(np.float32)
    im = Image.fromarray(np.dstack([rgb, al * 255]).clip(0, 255).astype(np.uint8), 'RGBA')
    if sharpen: im = im.filter(ImageFilter.UnsharpMask(radius=3, percent=120, threshold=2))
    r = np.array(im.resize((W, H), Image.LANCZOS)).astype(np.float32)
    alpha = r[..., 3] / 255.0
    col = r[..., :3] / np.maximum(alpha[..., None], 1e-3)
    out = np.zeros((H, W, 4), np.uint8)
    on = alpha > 0.5
    d = ((col[on][:, None, :] - pal[None, :, :]) ** 2).sum(-1)
    out[on, :3] = pal[d.argmin(1)]; out[on, 3] = 255
    al = out[..., 3] > 0
    edge = al & ~ndimage.binary_erosion(al, structure=[[0, 1, 0], [1, 1, 1], [0, 1, 0]])
    out[edge, :3] = pal[pal.sum(1).argmin()]
    return out

def build(src, scale, name):
    a, cells = C.poses(src)
    allfg = np.zeros(a.shape[:2], bool)
    for m in cells.values(): allfg |= m
    pal, idx = C.quantize(a, allfg, 64)
    frames = {k: down2(a, pal.astype(np.float32), cells[k], scale) for k in range(16)}
    F = C.F
    sheet = Image.new('RGBA', (F * 4, F * 7), (0, 0, 0, 0))
    for g in range(25):
        fr = frames[C.MAP[g]]; h, w = fr.shape[:2]
        cell = Image.new('RGBA', (F, F), (0, 0, 0, 0))
        cell.alpha_composite(Image.fromarray(fr), ((F - w) // 2, 55 - h))
        sheet.alpha_composite(cell, ((g % 4) * F, (g // 4) * F))
    sheet.save(f'cat_{name}_b.png')
    return sheet

if __name__ == '__main__':
    out = []
    for src, sc, nm in (('delivery_src.png', 0.169, 'delivery'), ('chef_src.png', 0.165, 'inspector')):
        out.append(build(src, sc, nm))
    F = C.F
    c = Image.new('RGBA', (F * 4 * 2 + 10, F * 3), (60, 56, 64, 255))
    for i, sh in enumerate(out): c.alpha_composite(sh.crop((0, 0, F * 4, F * 3)), (i * (F * 4 + 10), 0))
    c.resize((c.width * 3, c.height * 3), Image.NEAREST).save('both_b.png')
