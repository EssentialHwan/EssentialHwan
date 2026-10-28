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
        # 흰 털(달냥): 털 안쪽이 배경색과 같아서 외곽선 틈으로 '배경'으로 새어 나가 속이 빈 부품이 된다
        # → 외곽선을 닫은 뒤 안쪽 구멍을 채운다(어두운 털 고양이는 원래 구멍이 없어 결과 같음)
        mp = np.pad(m, 10)
        m = ndimage.binary_fill_holes(ndimage.binary_closing(mp, iterations=8))[10:-10, 10:-10] | m
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
    # 먼 쪽 다리: 털만 어둡게, 밝은 발끝(분홍)은 조금만 → 회색 발끝 방지
    o = img.copy(); rgb = o[..., :3].astype(float); lum = rgb.mean(2)
    kk = np.where(lum > 150, 0.9, k)[..., None]
    o[..., :3] = (rgb * kk).astype(np.uint8); return o

def belly_y(body, x):
    col = np.where(body[:, int(x), 3] > 0)[0]
    return col.max() if len(col) else body.shape[0]

def clip_outside(layer, body, pad, dy, ground, visible):
    # 몸통 옆(엉덩이 뒤·가슴 앞)으로 다리 윗동이 삐져나와 생기던 조각을 지운다: 몸이 없는 칸에서는 보이는 다리 길이 위쪽을 잘라 냄
    H, W = layer.shape[:2]
    top = int(ground - visible * 1.02)
    for x in np.where((layer[..., 3] > 0).any(0))[0]:
        bx = x - pad
        has = 0 <= bx < body.shape[1] and (body[:, bx, 3] > 0).any()
        if not has: layer[:max(0, top), x] = 0

def seam(c, L, body, pad, dy, bh):
    # 이음매 메우기: 가까운 쪽 다리 둘레(조금 넓힌 영역)에서 실루엣 안쪽에 남은 진한 선(배·엉덩이 외곽선, 다리 윗동 외곽선)을
    # 주변 털색으로 채운다. 바깥 테두리 선은 그대로 → 다리가 몸에서 이어져 나온 것처럼
    union = c[..., 3] > 0
    edge = union & ~ndimage.binary_erosion(union, iterations=max(2, int(bh * 0.014)))
    lum = c[..., :3].astype(float).mean(2)
    near = ndimage.binary_dilation(L[..., 3] > 0, iterations=max(2, int(bh * 0.012)))
    # 다리 윗부분 근처만(발끝·발가락 선은 건드리지 않게): 각 칸에서 몸통 아랫단 ± 띠
    band = np.zeros_like(near)
    k = max(4, int(bh * 0.08))
    for x in np.where(near.any(0))[0]:
        bx = x - pad
        if 0 <= bx < body.shape[1] and (body[:, bx, 3] > 0).any():
            by = int(np.where(body[:, bx, 3] > 0)[0].max() - dy)
            band[max(0, by - k * 2):by + max(2, int(bh * 0.012)) + 1, x] = True   # 몸통 아랫단까지만(다리 자체 옆선은 남김)
    rgb = c[..., :3].astype(float); sat = rgb.max(2) - rgb.min(2)
    # 털이 검은 고양이도 있으니 '선'의 기준은 다리 털 밝기에 맞춘다(털보다 확실히 어두운 것만 선)
    Lm = L[..., 3] > 0
    fur_lum = np.median(lum[Lm & (lum > 20)]) if (Lm & (lum > 20)).any() else 200.0
    dark = min(115.0, fur_lum * 0.6)
    gray = ((sat < 28) & (lum < 215) & (lum > fur_lum * 1.3)) if fur_lum < 120 else ((sat < 28) & (lum < 215))
    M = near & band & union & ~edge & ((lum < dark) | gray)   # 진한 선 + 원화 가장자리의 회색 번짐
    if not M.any(): return
    src = union & ~M & (lum >= dark) & ~gray & (near | ~ndimage.binary_dilation(near, iterations=6))   # 먼 쪽 다리 색(회색빛)은 끌어오지 않게
    src &= ~((L[..., 3] == 0) & (lum < 160) & (lum > dark) & near & (fur_lum > 120))
    _, (iy, ix) = ndimage.distance_transform_edt(~src, return_indices=True)
    c[M, :3] = c[iy[M], ix[M], :3]

def gait_angle(u, swing, lift, lh, R):
    # 걷기 v2(10/28): 땅 짚은 동안(stance)은 각도가 '일정한 속도로' 줄어 발이 바닥에 붙은 채 뒤로 밀리고,
    # 공중(swing)에서는 부드럽게 앞으로 돌아오며 발을 든다. 짚는 동안 회전으로 발끝이 뜨는 만큼(R(1-cos)) 내려서 바닥에 붙인다.
    u %= 1.0
    if 0.25 <= u < 0.75:
        ang = swing * (1 - 4 * (u - 0.25)); up = -R * (1 - math.cos(math.radians(ang)))
        return ang, up, -1.0
    else:
        w = ((u - 0.75) % 1.0) * 2
        ang = -swing * math.cos(math.pi * w); up = math.sin(math.pi * w) * lift * lh - R * (1 - math.cos(math.radians(ang)))
    return ang, up, w

# ── 걷기 v3(10/28): '다리만 움직이는 인형' 느낌을 없애는 몸의 반응 ──
# 1) 어깨·엉덩이 근육: 그 쪽 다리가 몸 밑에서 짚고 설 때(짚은 가운데) 어깨뼈/골반이 솟고, 다리가 떠 있을 때 내려앉는다.
#    → 등선이 앞·뒤 번갈아 물결친다(앞뒤 반 박자 차이). 몸통 세로 열을 가우스 모양으로 살짝 밀어 올림.
# 2) 꼬리: 한 걸음 주기에 맞춰 끝이 좌우로 살랑(뿌리는 고정, 끝으로 갈수록 크게 = 휘어짐).
# 3) 관절: 다리를 공중에서 앞으로 가져올 때 무릎/손목이 접혀 발끝이 뒤로 끌리다가 착지 직전에 펴진다.
def warp_body(body, t, fx, hx, low_min, bottom, roll, tail_amp):
    bh, bw = body.shape[:2]
    out = np.zeros_like(body)
    sig = bw * 0.13
    uF = (t / (2 * math.pi)) % 1.0; uH = ((t + math.pi) / (2 * math.pi)) % 1.0   # 가까운 앞다리 위상 0, 가까운 뒷다리 위상 π
    sF = math.cos(2 * math.pi * (uF - 0.5)); sH = math.cos(2 * math.pi * (uH - 0.5))
    # 머리·가슴은 어깨와 한 덩어리로, 엉덩이·꼬리는 골반과 한 덩어리로 움직이고 그 사이 등은 부드럽게 이어진다
    # (열마다 따로 밀면 얼굴이 찢어져서, 앞/뒤를 통째로 + 사이만 보간 = 몸이 앞뒤로 살짝 끄덕이는 '피치')
    xs = np.arange(bw)
    kF, kH = roll * sF, roll * sH
    u = np.clip((xs - hx) / max(1.0, fx - hx), 0, 1); u = u * u * (3 - 2 * u)
    k = np.round(kH + (kF - kH) * u).astype(int)
    for x in range(bw):
        sh = k[x]
        if sh > 0: out[:bh - sh, x] = body[sh:, x]
        elif sh < 0: out[-sh:, x] = body[:bh + sh, x]
        else: out[:, x] = body[:, x]
    if tail_amp > 0:   # 꼬리: 뿌리를 축으로 통째로 살짝 흔든다(옆에서 보면 좌우 살랑 = 앞뒤로 까딱)
        base_y = bottom - bh * 0.25 - kH; tx = low_min + bw * 0.02
        m = out[..., 3] > 0
        yy, xx = np.nonzero(m)
        sel = (xx < tx) & (yy < base_y)
        if sel.any():
            tl = np.zeros_like(out); tl[yy[sel], xx[sel]] = out[yy[sel], xx[sel]]
            mv = sel & (yy < base_y - bh * 0.07)          # 뿌리 쪽 토막은 제자리에 남겨 이음매가 안 벌어지게
            out[yy[mv], xx[mv]] = 0
            r = rot(tl, tail_amp * math.sin(t), (tx, base_y))
            r[..., 3] = np.where(r[..., 3] > 127, 255, 0)
            al = r[..., 3:4] > 0
            out[:] = np.where(al, r, out)
    return out

def make(body, front, hind, n=8, swing=17, lift=0.10, bob=0.012, sink=0.55, front_fwd=0.10, gait=False, roll=0.0, tail=0.0, flex=0.0):
    bh, bw = body.shape[:2]
    if front.shape[0] != hind.shape[0]:   # 앞다리 길이를 뒷다리 기준으로(폭은 그대로)
        front = np.array(Image.fromarray(front).resize((front.shape[1], hind.shape[0]), Image.BICUBIC))
        front[..., 3] = np.where(front[..., 3] > 127, 255, 0)
    fl_h, hl_h = front.shape[0], hind.shape[0]
    # 다리 뿌리 위치: 앞다리 = 몸 앞쪽 아래(가슴), 뒷다리 = 몸 뒤쪽 아래(엉덩이)
    m = body[..., 3] > 0; cols = np.where(m.any(0))[0]
    ys_bottom = np.array([belly_y(body, x) for x in cols])
    low = cols[ys_bottom >= ys_bottom.max() - bh * 0.25]          # 몸통 아랫부분 구간(꼬리·턱 제외) — 고양이마다 일정하게 잡힘
    fx = low.max() - bw * 0.28 + bw * front_fwd; hx = low.min() + bw * 0.115   # 까망냥에서 맞춘 위치를 기준으로 고정(뒷다리 조금 더 뒤로)
    legH = max(fl_h, hl_h); pad = int(legH * 1.2)
    # 몸 속에 묻는 길이: 기본 55%, 다리가 짧은 고양이(먼치·아기)는 최소 25%만 묻어 다리가 사라지지 않게
    hidden = min(hl_h * sink, max(hl_h * 0.25, hl_h - bh * 0.30))
    ground = ys_bottom.max() + (hl_h - hidden)
    H = bh + int(hl_h - hidden) + pad; W = bw + pad * 2
    frames = []
    body0 = body
    for i in range(n):
        t = 2 * math.pi * i / n
        c = np.zeros((H, W, 4), np.uint8)
        dy = bob * bh * ((0.5 + 0.5 * math.cos(2 * t)) if gait else (0.5 - 0.5 * math.cos(2 * t)))   # gait: 다리가 수직(짚은 가운데)일 때 몸이 가장 높다
        def leg(img, rx, phase, far, target=None):
            lh, lw = img.shape[:2]
            w = -1.0
            if gait:
                ang, up, w = gait_angle((t + phase) / (2 * math.pi), swing, lift, lh, lh - lw * 0.45)
            else:
                ang = swing * math.sin(t + phase)
                up = max(0.0, math.cos(t + phase)) * lift * lh
            big = np.zeros((lh + 2 * lw, lw * 3, 4), np.uint8); big[lw:lw + lh, lw:2 * lw] = img
            pv = (lw * 1.5, lw + lw * 0.45)                        # 어깨/엉덩이 둥근 윗부분 중심
            if flex > 0 and w >= 0:                                 # 관절 접기: 아랫마디(55%~)를 무릎/손목 축으로 뒤로
                kr = int(lh * 0.55); ex = -flex * math.sin(math.pi * min(1.0, w * 1.15))
                if abs(ex) > 0.5:
                    lower = np.zeros_like(big); lower[lw + kr:] = big[lw + kr:]
                    upper = big.copy(); upper[lw + kr:] = 0
                    lower = rot(lower, ex, (lw * 1.5, lw + kr))
                    big = np.where(upper[..., 3:4] > 0, upper, lower)
            r = rot(big, ang, pv)
            if far: r = darken(r)
            top = ground - lh                                       # 발끝이 모두 같은 바닥선에(앞·뒤 다리 보이는 길이 같게)
            x = pad + rx - lw * 1.5 + (-bw * 0.05 if far else 0)
            y = top - lw - up + dy * 0 + (-lh * 0.03 if far else 0)
            paste(c if target is None else target, r, x, y + pad * 0)
        # 대각선 짝: LF(가까운 앞)+RH(먼 뒤) 위상 0, RF(먼 앞)+LH(가까운 뒤) 위상 π
        if roll > 0 or tail > 0:
            body = warp_body(body0, t, fx, hx, low.min(), ys_bottom.max(), roll * bh, tail)
        Fl = np.zeros_like(c)
        leg(front, fx, math.pi, True, Fl)
        leg(hind, hx, 0.0, True, Fl)
        clip_outside(Fl, body, pad, dy, ground, hl_h - hidden)
        c[:] = np.where(Fl[..., 3:4] > 0, Fl, c)
        paste(c, body, pad, -dy)
        # 가까운 쪽 다리: 따로 그린 뒤, 몸통 안쪽(배 외곽선 바로 위까지)에 들어간 부분은 지우고 덮는다
        # → 배 외곽선이 다리를 가로지르지 않고, 다리가 몸에서 이어져 나온 것처럼 보임
        L = np.zeros_like(c)
        leg(hind, hx, math.pi, False, L)
        leg(front, fx, 0.0, False, L)
        bodyin = np.zeros(c.shape[:2], bool)
        inner = ndimage.binary_erosion(body[..., 3] > 0, iterations=max(2, int(bh * 0.02)))
        iy, ix = np.nonzero(inner); bodyin[(iy - round(dy)).clip(0, c.shape[0] - 1), ix + pad] = True
        L[bodyin] = 0
        clip_outside(L, body, pad, dy, ground, hl_h - hidden)
        al = L[..., 3:4] > 0
        c[:] = np.where(al, L, c)
        # 이음매 메우기: 배 외곽선·다리 윗동 외곽선이 다리와 몸 사이에 선으로 남지 않게,
        # 실루엣 안쪽에 있는 진한 선 픽셀을 그 다리의 털색으로 칠한다(바깥 테두리 선은 그대로)
        seam(c, L, body, pad, dy, bh)
        frames.append(c)
    return frames

def to_game(hi, width_px=48, scale=None):
    allpx = np.vstack([f[f[..., 3] > 0][:, :3] for f in hi]).astype(np.uint8)
    q = Image.fromarray(allpx.reshape(1, -1, 3)).quantize(colors=64, method=Image.Quantize.MEDIANCUT)
    pal = np.array(q.getpalette()[:192]).reshape(-1, 3)
    pal = np.vstack([pal, V.accent_palette(allpx.reshape(1, -1, 3), np.ones((1, len(allpx)), bool))]).astype(np.float32)
    # 모든 프레임 공통 bbox로 잘라 같은 배율
    union = np.zeros(hi[0].shape[:2], bool)
    for f in hi: union |= f[..., 3] > 0
    ys, xs = np.nonzero(union); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    s = width_px / (x1 - x0)
    if scale is not None:   # 기준(옛 걷기) 배율 고정: 다리를 크게 흔들어 bbox가 넓어져도 고양이 크기는 그대로
        s = scale
    return _down(hi, pal, (y0, y1, x0, x1), s)

def _down(hi, pal, box, s):
    y0, y1, x0, x1 = box
    out = []
    for f in hi:
        g = f[y0:y1, x0:x1]; m = g[..., 3] > 0
        full = np.ones(m.shape, bool)
        # 투명한 칸의 색(검정)이 줄일 때 가장자리에 섞여 들어가면 가는 다리가 거뭇해진다(흰 고양이 달냥에서 드러남, 10/28)
        # → 투명 칸을 가장 가까운 불투명 칸 색으로 채운 뒤 줄인다(알파는 따로 계산하므로 모양은 그대로)
        rgb = g[..., :3].copy()
        if (~m).any() and m.any():
            _, (iy, ix) = ndimage.distance_transform_edt(~m, return_indices=True)
            rgb = rgb[iy, ix]
        d = C2.down2(rgb, pal, full, s, sharpen=True)
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
