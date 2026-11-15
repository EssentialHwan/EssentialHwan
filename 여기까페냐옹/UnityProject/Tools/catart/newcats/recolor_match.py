# 색이 다르게 온 원화(예: 페르냥 생활 동작이 크림색으로 옴)를 기본 그림의 털색으로 바꾼다.
#   python3 recolor_match.py <잘못온.png> <기준(기본 그림).png> <out.png>
# 털 픽셀만(외곽선·분홍 귀/볼·흰 진주/반짝이·눈 제외) 밝기 순위를 기준 그림 털의 같은 순위 색으로 옮긴다(히스토그램 매칭).
import sys, numpy as np
from PIL import Image
from seg import fg_mask
def fur_mask(a, fg):
    c = a.astype(int); r, g, b = c[..., 0], c[..., 1], c[..., 2]
    lum = (r * 3 + g * 6 + b) / 10; sat = c.max(2) - c.min(2)
    outline = lum < 70
    pink = (r - g > 45) & (r > 170) & (b > 100)
    white = (lum > 228) & (sat < 12)          # 진주·반짝이(무채색)만 — 크림 털의 밝은 곳은 털로 본다
    eye = (sat > 90) & (lum < 140)
    return fg & ~outline & ~pink & ~white & ~eye, lum
src, ref, out = sys.argv[1:4]
a, fa = fg_mask(src); b, fb = fg_mask(ref)
ma, la = fur_mask(a, fa); mb, lb = fur_mask(b, fb)
# 밝기 순위 매칭(기준 그림 털의 밝기 분포·색을 그대로) — 단, 대상 밝기를 먼저 살짝 흐려서(σ=SIG)
# 크림 털의 잔 노이즈가 기준의 큰 명암차로 부풀어 얼룩지지 않게 한다. 배·얼굴·발처럼 원래 밝은 곳은 밝은 갈색이 된다.
from scipy import ndimage
SIG = float(sys.argv[4]) if len(sys.argv) > 4 else 3.0
ob = np.argsort(lb[mb]); cols = b[mb][ob].astype(float)
N = 256; idx = np.linspace(0, len(cols), N + 1).astype(int)
lut = np.array([cols[idx[i]:max(idx[i] + 1, idx[i + 1])].mean(0) for i in range(N)])
lsm = ndimage.gaussian_filter(np.where(ma, la, np.median(la[ma])), SIG)
va = lsm[ma] + (la[ma] - lsm[ma]) * 0.25          # 흐린 밝기 + 원래 결 25%
rank = np.argsort(np.argsort(va)) / max(1, len(va) - 1)
o = a.copy().astype(float); o[ma] = lut[np.minimum(N - 1, (rank * N).astype(int))]
Image.fromarray(o.astype(np.uint8)).save(out); print('ok', out, ma.sum())
