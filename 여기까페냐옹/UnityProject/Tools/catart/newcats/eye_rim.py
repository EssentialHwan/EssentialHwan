# 젖소냥·턱시도냥 마감 클로즈업: 눈 위쪽이 검은 털에 바로 붙어 눈이 '흰자 없는 구멍'처럼 보이는 문제(10/28 형 피드백).
# 원화 그림체는 그대로 두고, 눈 테두리 바깥이 검은 털에 닿는 곳에만 2px 크림색 띠를 둘러 눈과 털을 떼어 놓는다.
#   python3 eye_rim.py <in.png> <out.png>   (closeup_poses.py가 젖소·턱시도에 자동 적용)
import sys, numpy as np
from PIL import Image
from scipy import ndimage

RING = 2
WARM = 0.42
def rim(a):
    a = a.copy(); c = a[..., :3].astype(float); al = a[..., 3] > 0
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    lum = (r * 3 + g * 6 + b) / 10
    ys, xs = np.nonzero(al); y0, y1 = ys.min(), ys.max()
    amber = al & (r > 120) & (r - b > 80) & (b < 95) & (g > 65) & (g < 185)   # 호박색(빨간 나비넥타이 제외)
    lab, n = ndimage.label(ndimage.binary_closing(ndimage.binary_dilation(amber, iterations=2), iterations=2))   # 디더링으로 쪼개진 홍채를 한 덩어리로
    comps = []
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        sz = int(((lab[sl] == i) & amber[sl]).sum())
        if sz < 12 or (sl[0].start + sl[0].stop) / 2 > y0 + (y1 - y0) * 0.75: continue
        comps.append((sz, sl))
    comps.sort(key=lambda e: -e[0]); comps = [x[1] for x in comps[:2]]
    H, W = al.shape; Y, X = np.mgrid[0:H, 0:W]
    eye = np.zeros_like(al)
    for sl in comps:
        t, bt, l, rt = sl[0].start, sl[0].stop, sl[1].start, sl[1].stop    # (홍채를 넓힌 덩어리 = 거의 눈 전체)
        cy, cx = (t + bt) / 2, (l + rt) / 2; ry, rx = (bt - t) / 2 + 0.5, (rt - l) / 2 + 0.5
        eye |= ((Y - cy) / ry) ** 2 + ((X - cx) / rx) ** 2 <= 1
    # 눈 안쪽(테두리 2px 안): ① 짙은 눈동자·홍채 윗부분을 밝은 밤색 쪽으로(눈알이 '색 있는 구슬'로 보이게)
    #                         ② 반짝이를 1px 키움(작은 반짝이 때문에 눈이 죽어 보임)
    inner = np.zeros_like(eye)
    for sl in comps:
        t, bt, l, rt = sl[0].start, sl[0].stop, sl[1].start, sl[1].stop
        cy, cx = (t + bt) / 2, (l + rt) / 2; ry, rx = (bt - t) / 2 - 2.5, (rt - l) / 2 - 2.5
        inner |= ((Y - cy) / ry) ** 2 + ((X - cx) / rx) ** 2 <= 1
    hl = inner & (lum > 235)
    big = ndimage.binary_dilation(hl, iterations=1) & inner
    dk = inner & (lum < 90) & ~big
    k = WARM
    tgt = np.array([128, 66, 32], float)
    a[dk, :3] = (c[dk] * (1 - k) + tgt * k).astype(np.uint8)
    a[big, :3] = (255, 253, 248)
    # 눈 테두리 바깥이 검은 털에 바로 닿는 곳: 2px 크림색 띠 → 눈과 털이 붙어 '구멍'처럼 보이지 않게
    fur = al & (r - g < 22) & (lum < 125) & (lum > 25)
    ring = ndimage.binary_dilation(eye, iterations=RING) & ~eye
    ring &= fur | (al & (lum < 25) & ndimage.binary_dilation(fur, iterations=1))
    a[ring, :3] = (240, 231, 220)
    return a, len(comps)

if __name__ == '__main__':
    im = np.array(Image.open(sys.argv[1]).convert('RGBA'))
    o, n = rim(im); Image.fromarray(o).save(sys.argv[2]); print('eyes', n)
