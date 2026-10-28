# 마감 놀아주기 클로즈업(정면 식빵 자세)을 원화에서 바로 고해상도 도트로 뽑는다.
#   python3 closeup.py <catId> <sheetA.png> <배율(인게임 대비, 예: 3 / 4.5)> [포즈칸=8]
# 인게임 시트는 원화를 56px 칸으로 줄인 것 → 마감 화면에서 그걸 3배로 키우면 뭉툭했다.
# 여기서는 같은 원화를 '인게임 크기 × 배율'로 바로 줄여서, 같은 고양이를 더 고운 도트로 만든다.
# 출력: closeup_<cat>_<배율>.png — 화면 비율 168:128 캔버스, 바닥선(8/128) 위에 가운데 정렬
import sys, numpy as np
from PIL import Image
import convert as C, convert2 as C2, build_new4 as B
import build_v2 as V
from build_cat import clean

def eye_accents(a, fg):
    # 원화의 초록·노랑·파랑 눈과 흰 반짝이·분홍 볼이 64색으로 줄일 때 탁해지지 않게 따로 색을 남긴다
    px = a[fg].astype(int); r, g, b = px[:, 0], px[:, 1], px[:, 2]
    sat = px.max(1) - px.min(1)
    groups = [(g > r + 15) & (g > b + 10) & (sat > 45),      # 초록 눈
              (r > 170) & (g > 110) & (b < 110) & (sat > 70), # 노랑·호박 눈
              (b > 150) & (b - r > 40),                       # 파란 눈
              (px.min(1) > 225),                              # 흰 반짝이
              (r > 190) & (g < 170) & (b > 120) & (r - g > 40)]  # 분홍
    out = []
    for m in groups:
        if m.sum() < 20: continue
        q = Image.fromarray(px[m].astype(np.uint8).reshape(1, -1, 3)).quantize(colors=4, method=Image.Quantize.MEDIANCUT)
        out += list(np.array(q.getpalette()[:12]).reshape(-1, 3))
    return np.array(out, np.float32) if out else np.zeros((0, 3), np.float32)

def keep_highlights(a, m, f):
    # 눈 반짝이(흰 점)가 줄이면서 뭉개져 눈이 '흰자 없는 검은 구멍'처럼 보이던 문제(10/28 형 피드백: 젖소·턱시도).
    # 원화에서 '어두운 눈 속에 박힌 흰 점'만 골라, 줄인 그림의 같은 자리에 선명한 흰색으로 다시 찍는다.
    from scipy import ndimage
    ys, xs = np.nonzero(m); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    c = a[y0:y1, x0:x1].astype(int); mm = m[y0:y1, x0:x1]
    lum = (c[..., 0] * 3 + c[..., 1] * 6 + c[..., 2]) / 10; sat = c.max(-1) - c.min(-1)
    bright = mm & (lum > 222) & (sat < 40)
    dark = mm & ((lum < 110) | ((c[..., 0] - c[..., 2] > 70) & (lum < 190)))      # 눈동자·홍채
    lab, n = ndimage.label(bright)
    hl = np.zeros_like(bright)
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        comp = lab == i; sz = comp[sl].sum()
        if sz < 4 or sz > mm.sum() * 0.004: continue                                 # 털의 흰 부분(큰 덩어리) 제외
        ring = ndimage.binary_dilation(comp, iterations=3) & ~comp
        if ring.sum() and (dark & ring).sum() / ring.sum() > 0.7: hl |= comp
    H, W = f.shape[:2]
    if (y1 - y0) == 0: return
    sm = np.array(Image.fromarray((hl * 255).astype(np.uint8)).resize((W, H), Image.BOX)) > 70
    sm &= f[..., 3] > 0
    f[sm, :3] = (255, 253, 248)

def make(cat, src, mult, pose=8):
    a, cells = C.poses(src)
    s = B.fit_scale(cells)                          # 인게임 56px 칸 기준 배율
    m = cells[pose]
    pal, _ = C.quantize(a, m, 96)                   # 이 포즈 한 장만 96색 → 더 섬세하게
    pal = np.vstack([pal, eye_accents(a, m)]).astype(np.float32)
    f = clean(C2.down2(a, pal, m, s * mult))
    keep_highlights(a, m, f)
    W, H, G = round(168 * mult / 3), round(128 * mult / 3), round(8 * mult / 3)
    can = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    im = Image.fromarray(f); w, h = im.size
    can.alpha_composite(im, ((W - w) // 2, max(0, H - G - h)))
    return can

if __name__ == '__main__':
    cat, src, mult = sys.argv[1], sys.argv[2], float(sys.argv[3])
    pose = int(sys.argv[4]) if len(sys.argv) > 4 else 8
    out = f'closeup_{cat}_{sys.argv[3]}.png'
    make(cat, src, mult, pose).save(out); print('ok', out)
