# 걷기 칸(52~59) 몸통 아래쪽의 튀는 점·가는 줄(살 이어 붙이기에서 남은 얼룩)을 주변 털색으로 메운다.
#   python3 despeckle.py <catId> [--dry]   → 설치된 시트를 직접 고친다(_despeckle_<cat>.png 미리보기)
# 지키는 것: 실루엣 바깥 외곽선, 진한 선(다리 사이 구분선), 얼굴(칸 위쪽 45%)
import sys, numpy as np
from PIL import Image
F = 56
cat = sys.argv[1]; DULL = '--dull' in sys.argv
p = f'../../../Assets/Resources/CatSheets/cat_{cat}.bytes'
S = np.array(Image.open(p).convert('RGBA')).astype(int)

def fix(c):
    a = c[..., 3] > 0
    ys = np.nonzero(a.any(1))[0]
    if ys.size == 0: return c, 0
    y0 = ys.min() + int((ys.max() - ys.min()) * 0.45)
    out = c.copy(); n = 0
    for _ in range(2):
        src = out.copy()
        for y in range(max(1, y0), F - 1):
            for x in range(1, F - 1):
                if not a[y, x] or not (a[y - 1, x] and a[y + 1, x] and a[y, x - 1] and a[y, x + 1]): continue   # 실루엣 테두리 픽셀은 제외
                px = src[y, x, :3]
                if px.sum() < 150: continue                                          # 진한 선은 남긴다
                nb = [src[y + dy, x + dx, :3] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if (dy or dx) and a[y + dy, x + dx] and src[y + dy, x + dx, :3].sum() >= 150]
                if len(nb) < 5: continue
                nb = np.array(nb)
                # 이웃 중 서로 비슷한 다수 무리
                best = None
                for q in nb:
                    grp = nb[np.abs(nb - q).sum(1) < 45]
                    if best is None or len(grp) > len(best): best = grp
                if len(best) >= 5 and np.abs(px - best.mean(0)).sum() > 70:
                    out[y, x, :3] = best[np.abs(best - best.mean(0)).sum(1).argmin()]; n += 1
    if DULL:   # 회색·올리브 얼룩(무채색 중간 밝기) → 둘레 털색으로. 회색 털 고양이에는 쓰지 말 것
        for y in range(max(1, y0), F - 1):
            for x in range(1, F - 1):
                if not a[y, x]: continue
                r, g, b = out[y, x, :3]
                if not (250 < r + g + b < 520 and max(r, g, b) - min(r, g, b) < 70): continue
                nb = [out[y + dy, x + dx, :3] for dy in (-1, 0, 1) for dx in (-1, 0, 1)
                      if (dy or dx) and a[y + dy, x + dx] and out[y + dy, x + dx, :3].sum() >= 150
                      and not (250 < out[y + dy, x + dx, :3].sum() < 520 and np.ptp(out[y + dy, x + dx, :3]) < 70)]
                if len(nb) < 2: continue
                nb = np.array(nb); best = max((nb[np.abs(nb - q).sum(1) < 45] for q in nb), key=len)
                out[y, x, :3] = best[0]; n += 1
    return out, n

tot = 0
for k in range(52, 60):
    r, cc = (k // 4) * F, (k % 4) * F
    S[r:r + F, cc:cc + F], n = fix(S[r:r + F, cc:cc + F]); tot += n
print(cat, 'fixed px', tot)
im = Image.fromarray(S.astype(np.uint8))
if '--dry' not in sys.argv: im.save(p, 'PNG')
prev = im.crop((0, 13 * F, 4 * F, 15 * F)); bg = Image.new('RGBA', prev.size, (60, 56, 64, 255)); bg.alpha_composite(prev)
bg.resize((prev.width * 6, prev.height * 6), Image.NEAREST).save(f'_despeckle_{cat}.png')
