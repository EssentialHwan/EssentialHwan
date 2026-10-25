# 부품(몸통·앞다리·뒷다리) → 컷아웃 걷기 N프레임 → 게임 크기 프레임 + GIF
import numpy as np, math, sys, pickle
from PIL import Image
from scipy import ndimage
from seg import fg_mask
import convert2 as C2, build_v2 as V

def parts(src):
    a, fg = fg_mask(src)
    lab, n = ndimage.label(ndimage.binary_closing(fg, iterations=2))
    objs = []
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        m = (lab[sl] == i) & fg[sl]
        if m.sum() < 1500: continue
        rgba = np.zeros(m.shape + (4,), np.uint8); rgba[..., :3] = a[sl]; rgba[..., 3] = m * 255
        objs.append((sl[1].start, rgba))
    objs.sort(key=lambda o: o[0])
    body, front, hind = objs[0][1], objs[1][1], objs[2][1]
    return a, body, front, hind

def paste(canvas, img, x, y):
    h, w = img.shape[:2]; x, y = int(round(x)), int(round(y))
    H, W = canvas.shape[:2]
    x0, y0 = max(0, x), max(0, y); x1, y1 = min(W, x + w), min(H, y + h)
    if x1 <= x0 or y1 <= y0: return
    sub = img[y0 - y:y1 - y, x0 - x:x1 - x]; al = sub[..., 3:4] > 0
    canvas[y0:y1, x0:x1] = np.where(al, sub, canvas[y0:y1, x0:x1])

def rot(img, ang, pivot):
    return np.array(Image.fromarray(img).rotate(ang, resample=Image.BICUBIC, center=pivot))

def darken(img, k=0.74):
    o = img.copy(); o[..., :3] = (o[..., :3].astype(float) * k).astype(np.uint8); return o

def belly_y(body, x):
    col = np.where(body[:, int(x), 3] > 0)[0]
    return col.max() if len(col) else body.shape[0]

def make(body, front, hind, n=8, swing=17, lift=0.10, bob=0.012, sink=0.55):
    bh, bw = body.shape[:2]
    fl_h, hl_h = front.shape[0], hind.shape[0]
    # 다리 뿌리 위치: 앞다리 = 몸 앞쪽 아래(가슴), 뒷다리 = 몸 뒤쪽 아래(엉덩이)
    m = body[..., 3] > 0; cols = np.where(m.any(0))[0]
    ys_bottom = np.array([belly_y(body, x) for x in cols])
    low = cols[ys_bottom >= ys_bottom.max() - bh * 0.08]          # 배 아랫선 구간
    fx = low.max() - bw * 0.10; hx = low.min() + bw * 0.12
    legH = max(fl_h, hl_h); pad = int(legH * 1.2)
    H = bh + int(legH * (1 - sink)) + pad; W = bw + pad * 2
    frames = []
    for i in range(n):
        t = 2 * math.pi * i / n
        c = np.zeros((H, W, 4), np.uint8)
        dy = bob * bh * (0.5 - 0.5 * math.cos(2 * t))
        def leg(img, rx, phase, far):
            lh, lw = img.shape[:2]
            ang = swing * math.sin(t + phase)
            up = max(0.0, math.cos(t + phase)) * lift * lh
            big = np.zeros((lh + 2 * lw, lw * 3, 4), np.uint8); big[lw:lw + lh, lw:2 * lw] = img
            pv = (lw * 1.5, lw + lw * 0.45)                        # 어깨/엉덩이 둥근 윗부분 중심
            r = rot(big, ang, pv)
            if far: r = darken(r)
            top = belly_y(body, rx) - lh * sink                     # 다리 윗부분을 배 속에 묻음
            x = pad + rx - lw * 1.5 + (-bw * 0.05 if far else 0)
            y = top - lw - up + dy * 0 + (-lh * 0.03 if far else 0)
            paste(c, r, x, y + pad * 0)
        # 대각선 짝: LF(가까운 앞)+RH(먼 뒤) 위상 0, RF(먼 앞)+LH(가까운 뒤) 위상 π
        leg(front, fx, math.pi, True)
        leg(hind, hx, 0.0, True)
        paste(c, body, pad, -dy)
        leg(hind, hx, math.pi, False)
        leg(front, fx, 0.0, False)
        # 가까운 다리 윗부분(몸 안쪽)은 몸으로 다시 덮어 어깨·엉덩이 외곽선이 몸 위에 안 보이게
        bm = np.zeros(c.shape[:2], bool); bh_, bw_ = body.shape[:2]
        yy, xx = np.nonzero(body[..., 3] > 0); yy = (yy - round(dy)).clip(0, c.shape[0] - 1); xx = xx + pad
        inner = ndimage.binary_erosion(body[..., 3] > 0, iterations=3); iy, ix = np.nonzero(inner)
        iy2 = (iy - round(dy)).clip(0, c.shape[0] - 1)
        c[iy2, ix + pad] = body[iy, ix]
        frames.append(c)
    return frames

def to_game(hi, width_px=48):
    allpx = np.vstack([f[f[..., 3] > 0][:, :3] for f in hi]).astype(np.uint8)
    q = Image.fromarray(allpx.reshape(1, -1, 3)).quantize(colors=64, method=Image.Quantize.MEDIANCUT)
    pal = np.array(q.getpalette()[:192]).reshape(-1, 3)
    pal = np.vstack([pal, V.accent_palette(allpx.reshape(1, -1, 3), np.ones((1, len(allpx)), bool))]).astype(np.float32)
    # 모든 프레임 공통 bbox로 잘라 같은 배율
    union = np.zeros(hi[0].shape[:2], bool)
    for f in hi: union |= f[..., 3] > 0
    ys, xs = np.nonzero(union); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    s = width_px / (x1 - x0)
    out = []
    for f in hi:
        g = f[y0:y1, x0:x1]; m = g[..., 3] > 0
        full = np.ones(m.shape, bool)
        d = C2.down2(g[..., :3], pal, full, s, sharpen=True)
        al = np.array(Image.fromarray((m * 255).astype(np.uint8)).resize((d.shape[1], d.shape[0]), Image.BOX)) > 110
        d[~al] = 0
        # 외곽선 다시: 알파 경계
        e = al & ~ndimage.binary_erosion(al, structure=[[0, 1, 0], [1, 1, 1], [0, 1, 0]])
        d[e, :3] = pal[pal.sum(1).argmin()]; d[..., 3] = al * 255
        out.append(d)
    return out

if __name__ == '__main__':
    src = sys.argv[1]; n = int(sys.argv[2]) if len(sys.argv) > 2 else 8; tag = sys.argv[3] if len(sys.argv) > 3 else 'parts'
    a, body, front, hind = parts(src)
    print('body', body.shape[:2], 'front', front.shape[:2], 'hind', hind.shape[:2])
    hi = make(body, front, hind, n)
    out = to_game(hi)
    pickle.dump(out, open(f'{tag}_frames.pkl', 'wb'))
    h, w = out[0].shape[:2]; print('game frame', w, h)
    S = Image.new('RGBA', (w * n, h), (214, 190, 150, 255))
    for j, g in enumerate(out): S.alpha_composite(Image.fromarray(g), (j * w, 0))
    S.resize((S.width * 4, S.height * 4), Image.NEAREST).save(f'{tag}_walk_strip.png')
    ims = []
    for g in out:
        bg = Image.new('RGBA', (w, h), (214, 190, 150, 255)); bg.alpha_composite(Image.fromarray(g))
        ims.append(bg.resize((w * 5, h * 5), Image.NEAREST).convert('P', palette=Image.ADAPTIVE))
    ims[0].save(f'{tag}_walk.gif', save_all=True, append_images=ims[1:], duration=int(1000 / 10), loop=0)
    fw = hi[0].shape[1] // 3; fh = hi[0].shape[0] // 3
    HS = Image.new('RGBA', (fw * n, fh), (214, 190, 150, 255))
    for j, f in enumerate(hi): HS.alpha_composite(Image.fromarray(f).resize((fw, fh)), (j * fw, 0))
    HS.save(f'{tag}_walk_hires.png')
