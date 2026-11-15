# 모든 고양이 시트 재생성(옆모습 크기 보정 포함) → out/cat_*.png
import recolor2 as R, scalefix, inspector as I
from PIL import Image
import numpy as np
scalefix.apply(R)
F = R.F
for key in ['calico', 'cheese', 'cow', 'black', 'tuxedo', 'mackerel', 'siamese', 'gray', 'moon']:   # 심사원·배달냥은 원화 변환(newcats/convert2.py)
    sh = Image.new('RGBA', (F * 4, F * 7), (0, 0, 0, 0))
    for i in range(25):
        im = R.render(key, i)
        if key == 'inspector':
            a = np.array(im); I.deco(a, i); im = Image.fromarray(a)
        sh.alpha_composite(R.align(im, i), ((i % 4) * F, (i // 4) * F))
    sh.save(f'out/cat_{key}.png')
big = Image.new('RGBA', (F * 4 * 5 + 40, F * 7 * 2 + 20), (60, 56, 64, 255))
for n, key in enumerate(['calico', 'cheese', 'black', 'siamese', 'inspector', 'cow', 'tuxedo', 'mackerel', 'gray', 'moon']):
    big.alpha_composite(Image.open(f'out/cat_{key}.png'), ((n % 5) * (F * 4 + 10), (n // 5) * (F * 7 + 20)))
big.resize((big.width * 2, big.height * 2), Image.NEAREST).save('all_preview.png')
print('ok')
