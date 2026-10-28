# ※ 폐기(10/28): 덧그린 표정이 부자연·무섭다는 피드백 → closeup_poses.py(원화 포즈 고해상도)로 교체. 참고용으로만 남김.
# 마감 클로즈업 표정 6개: 고해상도 식빵 그림(closeup.py) 위에서 눈·입만 다시 그린다 → 자세가 절대 안 바뀜.
#   python3 closeup_faces.py <closeup_<cat>_4.5.png> <catId> [out_dir]
# 표정: open(기본) · happy(^ ^) · closed(눈 감음) · angry(화남 💢) · wide(놀람 o) · half(시큰둥)
# 눈 찾기: 얼굴 가운데 위쪽의 '진한 색 + 눈동자 색(초록·노랑·파랑)' 덩어리 두 개 → 감싸는 상자.
# 지우기: OpenCV 인페인트로 털색을 채운 뒤, 원래 그림에 있던 색으로만 다시 맞춘다(도트 색 유지).
import sys, os, numpy as np, cv2
from PIL import Image
from scipy import ndimage

def load(p):
    a = np.array(Image.open(p).convert('RGBA')); return a

def palette(a):
    px = a[a[..., 3] > 0][:, :3]
    return np.unique(px, axis=0).astype(np.float32)

def snap(rgb, pal):
    d = ((rgb[:, None, :].astype(np.float32) - pal[None]) ** 2).sum(-1)
    return pal[d.argmin(1)].astype(np.uint8)

# 털이 검은 고양이는 눈 테두리가 털과 붙어 자동으로 못 잡는다 → 눈 상자를 직접 적어 둠(4.5배 그림 기준)
EYE_OVERRIDE = {
    'cow':    [[82, 102, 110, 131], [141, 102, 168, 131]],
    'tuxedo': [[77, 93, 108, 121], [138, 94, 171, 121]],
}

def find_eyes(a, cat=None):
    # 눈 = 얼굴 안쪽(외곽선에서 떨어진 곳)의 가장 진한 덩어리 두 개(눈 테두리·동공). 좌우 하나씩.
    if cat in EYE_OVERRIDE: return [list(b) for b in EYE_OVERRIDE[cat]]
    op = a[..., 3] > 0
    ys, xs = np.nonzero(op); top, bot, left, right = ys.min(), ys.max(), xs.min(), xs.max()
    r, g, b = [a[..., i].astype(int) for i in range(3)]
    lum = (r * 3 + g * 6 + b) / 10
    edge = ndimage.binary_dilation(~op, iterations=4)
    zone = np.zeros_like(op)
    zone[int(top + (bot - top) * 0.30):int(top + (bot - top) * 0.75), int(left + (right - left) * 0.10):int(right - (right - left) * 0.10)] = True
    dk = op & (lum < 60) & ~edge & zone
    lab, n = ndimage.label(dk, structure=np.ones((3, 3)))
    cx = (left + right) / 2
    cand = []
    for k in range(1, n + 1):
        yy, xx = np.nonzero(lab == k)
        if len(yy) < 25: continue
        bx = [xx.min(), yy.min(), xx.max(), yy.max()]
        if bx[2] - bx[0] < 8 or bx[3] - bx[1] < 8: continue
        cand.append((len(yy), bx))
    L = sorted([c for c in cand if (c[1][0] + c[1][2]) / 2 < cx], key=lambda c: -c[0])
    R = sorted([c for c in cand if (c[1][0] + c[1][2]) / 2 >= cx], key=lambda c: -c[0])
    if not L or not R: raise SystemExit('눈을 못 찾음')
    out = []
    for bx in (L[0][1], R[0][1]):
        h = bx[3] - bx[1]
        # 진한 테두리는 눈 위쪽이 두껍고 아래쪽 눈동자(호박·갈색)는 밝아서 빠진다 → 아래로 35% 늘려 눈동자까지
        bot2 = bx[3] + int(round(h * 0.35))
        out.append([bx[0] - 1, bx[1], bx[2] + 1, bot2])
    return out

def find_mouth(a, eyes):
    (lx0, ly0, lx1, ly1), (rx0, ry0, rx1, ry1) = eyes
    cx = (lx1 + rx0) // 2
    y0 = max(ly1, ry1) - 4; y1 = y0 + 22
    r, g, b = [a[..., i].astype(int) for i in range(3)]
    lum = (r * 3 + g * 6 + b) / 10
    m = (a[..., 3] > 0) & (lum < 110)
    sub = np.zeros_like(m); sub[y0:y1, cx - 12:cx + 12] = True
    yy, xx = np.nonzero(m & sub)
    if len(yy) == 0: return [cx - 6, y0 + 6, cx + 6, y0 + 12]
    return [xx.min(), yy.min(), xx.max(), yy.max()]

def erase(a, boxes, pal, pad=2):
    # 상자 둘레의 털색(가장 흔한 색들)이 아닌 픽셀 = 눈(테두리·눈동자·반짝이) → 가장 가까운 털 픽셀 색으로 채움
    H, W = a.shape[:2]; op = a[..., 3] > 0
    res = a.copy()
    for x0, y0, x1, y1 in boxes:
        x0, y0, x1, y1 = x0 - pad, y0 - pad, x1 + pad, y1 + pad
        box = np.zeros((H, W), bool); box[max(0, y0):y1 + 1, max(0, x0):x1 + 1] = True
        ring = np.zeros((H, W), bool); ring[max(0, y0 - 5):y1 + 6, max(0, x0 - 5):x1 + 6] = True
        rr = ring & ~box & op
        if not rr.any(): continue
        cols, cnt = np.unique(a[rr][:, :3], axis=0, return_counts=True)
        order = np.argsort(cnt)[::-1]; keep = set(); tot = 0
        for i in order:
            keep.add(tuple(int(v) for v in cols[i])); tot += cnt[i]
            if tot >= 0.88 * cnt.sum(): break
        sub = res[box][:, :3]
        isfur = np.array([tuple(int(v) for v in c) in keep for c in sub])
        m = np.zeros((H, W), bool); m[box] = ~isfur
        m &= op
        if not m.any(): continue
        src = op & ~m & ring
        _, (iy, ix) = ndimage.distance_transform_edt(~src, return_indices=True)
        res[m] = res[iy[m], ix[m]]
    return res

def put(a, x, y, col, t=1):
    H, W = a.shape[:2]
    for dy in range(t):
        for dx in range(t):
            xx, yy = int(round(x)) + dx - t // 2, int(round(y)) + dy - t // 2
            if 0 <= xx < W and 0 <= yy < H: a[yy, xx, :3] = col; a[yy, xx, 3] = 255

def line(a, p, q, col, t):
    n = int(max(abs(q[0] - p[0]), abs(q[1] - p[1]))) * 2 + 1
    for i in range(n + 1):
        u = i / n; put(a, p[0] + (q[0] - p[0]) * u, p[1] + (q[1] - p[1]) * u, col, t)

def arc(a, box, col, t, up=True, depth=0.45):
    x0, y0, x1, y1 = box; cx = (x0 + x1) / 2; w = (x1 - x0) / 2; cy = (y0 + y1) / 2
    h = (y1 - y0) * depth
    prev = None
    for i in range(int(w * 4) + 1):
        x = x0 + i * (x1 - x0) / (w * 4)
        u = (x - cx) / w
        y = cy - h * (1 - u * u) * (1 if up else -1) + (h * 0.4 if up else -h * 0.4)
        if prev: line(a, prev, (x, y), col, t)
        prev = (x, y)

def ellipse(a, cx, cy, rx, ry, col, fill=None, t=2):
    H, W = a.shape[:2]
    yy, xx = np.mgrid[0:H, 0:W]
    d = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2
    if fill is not None:
        m = d <= 1; a[m, :3] = fill; a[m, 3] = 255
    ring = (d <= 1) & (d >= (1 - t / min(rx, ry)) ** 2)
    a[ring, :3] = col; a[ring, 3] = 255

def anger_mark(a, cx, cy, s, col):
    # 💢 화남 표시: 가운데를 비워 두고 네 귀퉁이에 바깥으로 벌어지는 꺾쇠
    g = s * 0.35
    for sx in (-1, 1):
        for sy in (-1, 1):
            c = (cx + sx * g, cy + sy * g)
            line(a, c, (cx + sx * s, cy + sy * g), col, 2)
            line(a, c, (cx + sx * g, cy + sy * s), col, 2)

def lids(a, boxes, grow=2):
    # 눈을 '눈꺼풀'로 덮는다: 눈 상자를 감싸는 타원 안을, 타원 밖에서 가장 가까운 털 픽셀 색으로 채운다
    #  (눈 테두리처럼 아주 진한 색은 빼고 — 검은 털은 테두리보다 밝아서 그대로 쓰인다)
    H, W = a.shape[:2]; op = a[..., 3] > 0
    r, g, b = [a[..., i].astype(int) for i in range(3)]
    lum = (r * 3 + g * 6 + b) / 10
    yy, xx = np.mgrid[0:H, 0:W]
    m = np.zeros((H, W), bool)
    for x0, y0, x1, y1 in boxes:
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2; rx, ry = (x1 - x0) / 2 + grow, (y1 - y0) / 2 + grow
        m |= ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1
    m &= op
    near = np.zeros((H, W), bool)
    for x0, y0, x1, y1 in boxes: near[max(0, y0 - 3):y1 + 4, max(0, x0 - 3):x1 + 4] = True
    pinkish = (r > 200) & (r - g > 40) & (r - b > 25)   # 볼터치 분홍이 눈꺼풀로 번져 올라가지 않게
    src = op & ~near & (lum >= 38) & ~pinkish          # 눈 상자 근처(눈동자 색이 남은 귀퉁이)는 재료로 안 씀
    _, (iy, ix) = ndimage.distance_transform_edt(~src, return_indices=True)
    res = a.copy(); res[m] = a[iy[m], ix[m]]
    return res

def erase_mouth(a, base, mouth, pal):
    # 입·코를 얼굴 털색으로 지운 뒤(가장 가까운 털 픽셀로 채움 — 얼룩 없이) 작은 분홍 코를 새로 그린다
    x0, y0, x1, y1 = mouth
    H, W = a.shape[:2]; op = a[..., 3] > 0
    ring = np.zeros((H, W), bool); ring[y0 - 6:y1 + 6, x0 - 7:x1 + 8] = True
    box = np.zeros((H, W), bool); box[y0 - 4:y1 + 2, x0 - 1:x1 + 2] = True
    rr = ring & ~box & op
    cols, cnt = np.unique(a[rr][:, :3], axis=0, return_counts=True)
    order = np.argsort(cnt)[::-1]; keep = []; tot = 0
    for i in order:
        keep.append(tuple(cols[i])); tot += cnt[i]
        if tot >= 0.9 * cnt.sum(): break
    keep = set(keep)
    m = box & op & ~np.array([[tuple(a[y, x, :3]) in keep for x in range(W)] for y in range(H)])
    res = a.copy()
    if m.any():
        src = op & ~m & ring
        _, (iy, ix) = ndimage.distance_transform_edt(~src, return_indices=True)
        res[m] = a[iy[m], ix[m]]
    # 원래 코 자리(분홍 픽셀 중심)에 작은 코: 분홍 역삼각형 + 진한 테두리
    r, g, b = [base[..., i].astype(int) for i in range(3)]
    pk = box & (r > 200) & (r - b > 40) & (g < 210)
    ny, nx = (np.nonzero(pk) if pk.any() else ([y0 - 2], [(x0 + x1) // 2]))
    cx, cy = int(round(np.mean(nx))), int(round(np.min(ny)))
    ink = pal[pal.sum(1).argmin()].astype(np.uint8); pink = np.array([240, 140, 150], np.uint8)
    for dy, half in enumerate((3, 2, 1)):
        for dx in range(-half, half + 1):
            res[cy + dy, cx + dx, :3] = ink if abs(dx) == half or dy == 0 and abs(dx) == half else pink
            res[cy + dy, cx + dx, 3] = 255
    res[cy + 3, cx, :3] = ink; res[cy + 3, cx, 3] = 255
    return res

def make_all(src, cat, out_dir):
    base = load(src); pal = palette(base)
    eyes = find_eyes(base, cat); mouth = find_mouth(base, eyes)
    lum = pal.sum(1); ink = pal[lum.argmin()].astype(np.uint8)              # 테두리(가장 진한 색)
    white = np.array([255, 255, 255], np.uint8)
    pink = np.array([236, 120, 132], np.uint8); red = np.array([214, 58, 64], np.uint8)
    ew = eyes[0][2] - eyes[0][0]; t = max(2, round(ew / 11))              # 선 굵기(눈 너비 비례)
    noeye = lids(base, eyes)
    res = {'open': base.copy()}

    def halo_arc(a, b, up, depth):
        # 검은 털 위에서도 보이게: 선 밑에 밝은 테두리를 먼저 깔고(어두운 곳에만) 그 위에 진한 선
        lite = a.copy(); arc(lite, b, np.array([248, 238, 226], np.uint8), t + 2, up=up, depth=depth)
        r_, g_, b_ = [a[..., i].astype(int) for i in range(3)]
        darkbg = (r_ * 3 + g_ * 6 + b_) / 10 < 75
        changed = (lite != a).any(-1) & darkbg
        a[changed] = lite[changed]
        arc(a, b, ink, t, up=up, depth=depth)

    a = noeye.copy()                                                        # 행복: ∩ ∩
    for b in eyes: halo_arc(a, b, True, 0.55)
    res['happy'] = a

    a = noeye.copy()                                                        # 눈 감음: ‿ ‿ (편안)
    for b in eyes: halo_arc(a, b, False, 0.35)
    res['closed'] = a

    a = base.copy()                                                         # 화남: 눈 위쪽을 사선으로 덮고 💢
    for i, (x0, y0, x1, y1) in enumerate(eyes):
        inner_left = i == 1                                                 # 오른쪽 눈은 왼쪽이 안쪽
        for x in range(x0 - 1, x1 + 2):
            u = (x - x0) / max(1, x1 - x0)
            k = (1 - u) if inner_left else u                                # 안쪽일수록 많이 덮음
            cut = y0 + (y1 - y0) * (0.15 + 0.5 * k)
            for y in range(y0 - 2, int(cut)):
                if 0 <= y < a.shape[0]: a[y, x] = noeye[y, x]
            put(a, x, cut, ink, t)
    mx = (mouth[0] + mouth[2]) / 2; my = mouth[3] + 1
    a = erase_mouth(a, base, mouth, pal)
    ellipse(a, mx, my + 1, max(3, ew * 0.16), max(2, ew * 0.11), ink, fill=pink, t=1)
    put(a, mx - ew * 0.07, my, white, 2)   # 작은 송곳니
    ys, xs = np.nonzero(base[..., 3] > 0)
    anger_mark(a, xs.max() - ew * 1.3, ys.min() + ew * 1.4, ew * 0.32, red)
    res['angry'] = a

    a = erase_mouth(noeye, base, mouth, pal)                                    # 놀람: 동그란 흰 눈 + 작은 눈동자 + o 입
    for x0, y0, x1, y1 in eyes:
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2 + 1
        ellipse(a, cx, cy, (x1 - x0) / 2, (y1 - y0) / 2, ink, fill=white, t=t)
        ellipse(a, cx, cy + 1, (x1 - x0) * 0.16, (y1 - y0) * 0.18, ink, fill=ink, t=1)
        put(a, cx - (x1 - x0) * 0.08, cy - (y1 - y0) * 0.06, white, 2)
    ellipse(a, mx, my + 2, max(2, ew * 0.12), max(3, ew * 0.15), ink, fill=pink, t=1)
    res['wide'] = a

    a = base.copy()                                                         # 시큰둥: 눈 위 절반을 눈꺼풀로 + 일자 입
    for x0, y0, x1, y1 in eyes:
        cut = y0 + (y1 - y0) * 0.48
        a[y0 - 2:int(cut), x0 - 1:x1 + 2] = noeye[y0 - 2:int(cut), x0 - 1:x1 + 2]
        line(a, (x0 - 1, cut), (x1 + 1, cut), ink, t)
    a = erase_mouth(a, base, mouth, pal)
    line(a, (mx - ew * 0.18, my), (mx + ew * 0.18, my), ink, max(1, t - 1))
    res['half'] = a

    os.makedirs(out_dir, exist_ok=True)
    for k, v in res.items():
        Image.fromarray(v).save(os.path.join(out_dir, f'close_{cat}_{k}.png'))
    return res, eyes, mouth

if __name__ == '__main__':
    src, cat = sys.argv[1], sys.argv[2]
    out = sys.argv[3] if len(sys.argv) > 3 else 'closeups'
    res, eyes, mouth = make_all(src, cat, out)
    print('eyes', eyes, 'mouth', mouth)
