# 컷아웃 걷기: AI 원화 1장에서 몸통·다리 조각을 잘라, 다리를 어깨/엉덩이 축으로 회전시켜 N프레임 보행을 만든다.
# 회전은 원화 해상도(고해상)에서 하고, 마지막에 게임 크기로 줄인다 → 다리 그림체가 원화와 같다.
import numpy as np, math, sys, pickle
from PIL import Image
from scipy import ndimage
from seg import fg_mask
import convert as C, convert2 as C2, build_v2 as V

def first_frame(src):
    a, fg0 = fg_mask(src)
    lab, n = ndimage.label(ndimage.binary_closing(fg0, iterations=2))
    objs = []
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        m = (lab == i) & fg0
        if m.sum() < 2000: continue
        objs.append((sl[1].start, sl, m[sl]))
    objs.sort(key=lambda o: o[0]); _, sl, m = objs[0]
    rgba = np.zeros(m.shape + (4,), np.uint8); rgba[..., :3] = a[sl]; rgba[..., 3] = m * 255
    return a, fg0, rgba

def split(rgba, belly=None):
    m = rgba[..., 3] > 0; H, W = m.shape
    if belly is None:   # 몸통 아랫선: 아래에서 위로 덩어리가 1개가 되는 첫 줄
        for y in range(H - 1, 0, -1):
            xs = np.where(m[y])[0]
            if len(xs) and (np.diff(xs) > 1).sum() == 0: belly = y + 1; break
    cutY = belly + 6                         # 다리 윗부분 약간은 몸 쪽에 남겨 이음매를 숨김
    yref = min(H - 3, belly + (H - belly) // 2)
    xs = np.where(m[yref])[0]; runs = [r for r in np.split(xs, np.where(np.diff(xs) > 1)[0] + 1) if len(r) > 4]
    legs = []
    for r in runs:
        x0, x1 = max(0, r[0] - 8), min(W, r[-1] + 9)
        y0 = cutY - 14
        piece = rgba[y0:, x0:x1].copy(); pm = m[y0:, x0:x1].copy()
        # 이웃 다리와 겹친 부분 배제: 이 구간 안에서 아래쪽 덩어리만
        lab, n = ndimage.label(pm[14:]); 
        if n > 1:
            sizes = ndimage.sum(pm[14:], lab, range(1, n + 1)); keep = lab == (int(np.argmax(sizes)) + 1)
            pm[14:] = keep
        piece[..., 3] = pm * 255
        legs.append(dict(x0=x0, y0=y0, img=piece, cx=(r[0] + r[-1]) / 2))
    legs.sort(key=lambda L: L['cx'])
    body = rgba.copy(); body[cutY:] = 0
    return body, legs, belly, cutY

def rot(img, ang, pivot):
    im = Image.fromarray(img)
    return np.array(im.rotate(ang, resample=Image.BICUBIC, expand=False, center=pivot))

def darken(img, k=0.68):
    o = img.copy(); o[..., :3] = (o[..., :3] * k).astype(np.uint8); return o

def make(rgba, n=8, swing=24, lift=10, bob=4):
    body, legs, belly, cutY = split(rgba)
    print('legs', len(legs), [int(L['cx']) for L in legs], 'belly', belly)
    # 왼→오: [뒷다리 쪽 2개, 앞다리 쪽 2개] — 각 쪽에서 더 큰 것을 '가까운 다리' 모양으로 사용
    hind = legs[0]; front = legs[-1]          # 가장 바깥 = 가까운 쪽(밝은 발끝) 다리
    H, W = rgba.shape[:2]; pad = 40
    hx = np.mean([L['cx'] for L in legs[:2]]); fx = np.mean([L['cx'] for L in legs[-2:]])
    frames = []
    for i in range(n):
        t = 2 * math.pi * i / n
        canvas = np.zeros((H + 2 * pad, W + 2 * pad, 4), np.uint8)
        dy = round(bob * (0.5 - 0.5 * math.cos(2 * t)))          # 한 걸음마다 한 번 살짝 위로
        def paste(img, x, y):
            h, w = img.shape[:2]; y = int(y) + pad; x = int(x)
            sub = canvas[y:y + h, x:x + w]; al = img[:sub.shape[0], :sub.shape[1], 3:4] > 0
            canvas[y:y + h, x:x + w] = np.where(al, img[:sub.shape[0], :sub.shape[1]], sub)
        def leg(tmpl, cx, phase, far):
            ang = swing * math.sin(t + phase)                    # +: 발이 앞으로(오른쪽)
            up = max(0.0, math.cos(t + phase)) * lift            # 앞으로 가는 동안 발 들기
            img = tmpl['img']; h, w = img.shape[:2]
            big = np.zeros((h + 40, w + 60, 4), np.uint8); big[0:h, 30:30 + w] = img
            r = rot(big, ang, (30 + w / 2, 14))                  # 뿌리(몸통 안) 기준 회전
            if far: r = darken(r)
            x = pad + cx - (30 + w / 2) + (-6 if far else 0)
            paste(r, x, tmpl['y0'] - dy * 0 - up + (-3 if far else 0))
        # 대각선 짝: 가까운 앞(LF)+먼 뒤(RH) 같은 위상, 먼 앞(RF)+가까운 뒤(LH) 반대 위상
        leg(front, fx + 4, math.pi, True)    # RF
        leg(hind, hx - 4, 0.0, True)         # RH
        paste(body, pad, -dy)
        leg(hind, hx, math.pi, False)        # LH
        leg(front, fx, 0.0, False)           # LF
        frames.append(canvas)
    return frames

if __name__ == '__main__':
    src = sys.argv[1]; n = int(sys.argv[2]) if len(sys.argv) > 2 else 8
    a, fg0, rgba = first_frame(src)
    hi = make(rgba, n)
    # 게임 크기로: 모든 프레임 같은 배율·같은 팔레트, 캔버스 그대로 줄여 정렬 유지
    allpx = np.vstack([f[f[..., 3] > 0][:, :3] for f in hi])
    big = allpx.reshape(1, -1, 3).astype(np.uint8)
    pal_img = Image.fromarray(big).quantize(colors=64, method=Image.Quantize.MEDIANCUT)
    pal = np.array(pal_img.getpalette()[:192]).reshape(-1, 3)
    fake = np.zeros((1, len(allpx), 3), np.uint8); fake[0] = allpx
    pal = np.vstack([pal, V.accent_palette(fake.reshape(1, -1, 3)[0].reshape(1, -1, 3).repeat(1, 0).reshape(1, -1, 3)[0].reshape(-1, 3)[None, :, :].reshape(1, -1, 3)[0].reshape(1, -1, 3), np.ones((1, len(allpx)), bool))]).astype(np.float32)
    s = 46.0 / (rgba.shape[1] * 0.92)
    out = []
    for f in hi:
        m = f[..., 3] > 0
        g = C2.down2(f[..., :3], pal, np.ones_like(m), s, sharpen=False)   # 캔버스 전체를 같은 배율로
        # down2는 bbox로 자르므로 캔버스 전체 마스크를 쓰고, 알파는 다시 계산
        al = np.array(Image.fromarray((m * 255).astype(np.uint8)).resize((g.shape[1], g.shape[0]), Image.BOX)) > 127
        g[~al] = 0
        out.append(g)
    pickle.dump(out, open('cutout_frames.pkl', 'wb'))
    Hh, Ww = out[0].shape[:2]
    S = Image.new('RGBA', (Ww * n, Hh), (214, 190, 150, 255))
    for j, g in enumerate(out): S.alpha_composite(Image.fromarray(g), (j * Ww, 0))
    S.resize((S.width * 4, S.height * 4), Image.NEAREST).save('cutout_walk_strip.png')
    ims = []
    for g in out:
        bg = Image.new('RGBA', (Ww, Hh), (214, 190, 150, 255)); bg.alpha_composite(Image.fromarray(g))
        ims.append(bg.resize((Ww * 4, Hh * 4), Image.NEAREST).convert('P', palette=Image.ADAPTIVE))
    ims[0].save('cutout_walk.gif', save_all=True, append_images=ims[1:], duration=int(1000 / 10), loop=0)
    # 고해상 미리보기(원화 그대로)
    fw, fh = hi[0].shape[1] // 2, hi[0].shape[0] // 2
    HS = Image.new('RGBA', (fw * n, fh), (214, 190, 150, 255))
    for j, f in enumerate(hi): HS.alpha_composite(Image.fromarray(f).resize((fw, fh)), (j * fw, 0))
    HS.save('cutout_walk_hires.png')
    print('ok', Ww, Hh)
