# 냥슐랭 심사원: 크림/회갈색 + 남색 베레모 + 금테 모노클
import numpy as np
from PIL import Image
import recolor2 as R
R.CATS['inspector'] = dict(name='심사원', fur=R.ramp((196, 178, 160)), white=R.WHITE, whiteFaceOnly=True,
                           X=(46, 36, 34), iris=(150, 110, 60), line=(90, 62, 50))
NAVY, NAVYD, NAVYL = (52, 58, 104), (32, 34, 70), (84, 92, 150)
GOLD, GOLDD = (246, 204, 80), (170, 120, 40)

def head(i):
    if i in R.OPEN:
        xs = [x0 + len(t[0]) / 2 for t, x0, y0 in R.OPEN[i]]; ys = [y0 for t, x0, y0 in R.OPEN[i]]
        return np.mean(xs), min(ys), R.OPEN[i]
    if i in R.FACE: cx, cy, rx, ry = R.FACE[i]; return cx, cy - 4, []
    e = R.EYES.get(i)
    if e: return np.mean([(a + c) / 2 for a, b, c, d in e]), min(b for a, b, c, d in e), []
    return None

def deco(img, i):
    h = head(i) if i != 17 else None
    if h is None: return
    cx, ey, opens = h
    a = img[:, :, 3] > 0
    cols = [x for x in range(int(cx) - 6, int(cx) + 7) if 0 <= x < R.F and a[:, x].any()]
    if not cols: return
    top = min(np.where(a[:, x])[0].min() for x in cols)
    # 베레모: 머리 꼭대기 위 납작한 반타원 + 꼭지
    w = 7 if len(opens) != 1 else 5
    for y in range(top - 3, top + 3):
        for x in range(int(cx) - w - 1, int(cx) + w + 2):
            u = (x + 0.5 - cx - 1) / (w + 1.2); v = (y + 0.5 - (top + 1)) / 3.2
            if u * u + v * v <= 1 and 0 <= x < R.F and 0 <= y < R.F:
                img[y, x] = (*(NAVYL if v < -0.4 and u < 0 else NAVYD if v > 0.5 else NAVY), 255)
    for y, x in ((top - 4, int(cx) + 1), (top - 5, int(cx) + 1)):
        if y >= 0: img[y, x] = (*NAVYD, 255)
    # 모노클: 오른쪽(두 번째) 눈 둘레 금테 + 줄
    if opens:
        t, x0, y0 = opens[-1]
        x1, y1 = x0 + len(t[0]), y0 + len(t)
        for x in range(x0 - 1, x1 + 1):
            for y in (y0 - 1, y1):
                if 0 <= y < R.F: img[y, x] = (*GOLD, 255)
        for y in range(y0 - 1, y1 + 1):
            for x in (x0 - 1, x1):
                if 0 <= x < R.F: img[y, x] = (*GOLD, 255)
        for k in range(1, 6):
            y, x = y1 + k, x1 - 1 + (k // 2)
            if y < R.F and x < R.F and img[y, x, 3] > 0: img[y, x] = (*GOLDD, 255)

if __name__ == '__main__':
    F = R.F
    sh = Image.new('RGBA', (F * 4, F * 7), (0, 0, 0, 0))
    for i in range(25):
        im = R.render('inspector', i)
        a = np.array(im); deco(a, i); im = Image.fromarray(a)
        sh.alpha_composite(R.align(im, i), ((i % 4) * F, (i // 4) * F))
    sh.save('out/cat_inspector.png')
    big = Image.new('RGBA', sh.size, (60, 56, 64, 255)); big.alpha_composite(sh)
    big.resize((sh.width * 4, sh.height * 4), Image.NEAREST).convert('RGB').save('inspector_preview.png')
    print('ok')
