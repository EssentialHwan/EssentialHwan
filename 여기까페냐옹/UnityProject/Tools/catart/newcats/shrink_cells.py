# 시트의 일반 칸(0~51)을 k배로 줄인다(꼬물냥=아기 고양이, 11/02). 발 위치(칸 아래·가운데)는 그대로. 걷기(52~59)는 rewalk2.py --scale로 따로.
#   python3 shrink_cells.py <catId> <k>
import sys, numpy as np
from PIL import Image
F = 56; cat = sys.argv[1]; k = float(sys.argv[2])
p = f'../../../Assets/Resources/CatSheets/cat_{cat}.bytes'
S = Image.open(p).convert('RGBA')
for c in range(52):
    box = ((c % 4) * F, (c // 4) * F, (c % 4) * F + F, (c // 4) * F + F)
    cell = S.crop(box); a = np.array(cell)[..., 3]
    ys, xs = np.nonzero(a)
    if len(xs) == 0: continue
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    part = cell.crop((x0, y0, x1, y1))
    w, h = max(1, round(part.width * k)), max(1, round(part.height * k))
    part = part.resize((w, h), Image.NEAREST)
    new = Image.new('RGBA', (F, F), (0, 0, 0, 0))
    cx = (x0 + x1) / 2
    new.alpha_composite(part, (int(round(cx - w / 2)), y1 - h))
    S.paste(Image.new('RGBA', (F, F), (0, 0, 0, 0)), box); S.paste(new, box)
S.save(p, 'PNG'); print('shrunk', cat, k)
