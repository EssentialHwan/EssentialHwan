# 옆모습 프레임(걷기 4~7·점프 18)이 정면보다 작게 그려져 있다(머리 높이 23.5px vs 28px).
# 발 기준으로 키워 정면과 머리 크기를 맞춘다. recolor2를 import한 직후 apply()를 부른다.
import numpy as np
F = 56; BOT = 54
SCALE = {4: 1.19, 5: 1.19, 6: 1.19, 7: 1.19, 18: 1.19}

def _scale_grid(g, f, OUT):
    ys, xs = np.where(g >= 0)
    cx = (xs.min() + xs.max()) / 2.0
    G = -np.ones_like(g)
    for y in range(F):
        for x in range(F):
            sx = int(round(cx + (x - cx) / f)); sy = int(round(BOT - (BOT - y) / f))
            if 0 <= sx < F and 0 <= sy < F: G[y, x] = g[sy, sx]
    # 늘어나며 2px가 된 외곽선을 1px로
    tr = G < 0
    def at(a, y, x): return a[y, x] if 0 <= y < F and 0 <= x < F else True
    edge = np.zeros_like(tr)
    for y in range(F):
        for x in range(F):
            if G[y, x] == OUT and (at(tr, y-1, x) or at(tr, y+1, x) or at(tr, y, x-1) or at(tr, y, x+1)): edge[y, x] = True
    H = G.copy()
    for y in range(F):
        for x in range(F):
            if G[y, x] != OUT or edge[y, x]: continue
            if any(at(tr, y+dy, x+dx) for dy in (-1, 0, 1) for dx in (-1, 0, 1)): continue
            for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                yy, xx = y + dy, x + dx
                if 0 <= yy < F and 0 <= xx < F and edge[yy, xx]:
                    oy, ox = y - dy, x - dx
                    if 0 <= oy < F and 0 <= ox < F and G[oy, ox] >= 0 and G[oy, ox] != OUT:
                        H[y, x] = G[oy, ox]; break
    return H, cx

def apply(R):
    for i, f in SCALE.items():
        g = R.frames[i]
        H, cx = _scale_grid(g, f, R.OUT)
        R.frames[i] = H
        T = lambda x, y: (int(round(cx + (x - cx) * f)), int(round(BOT - (BOT - y) * f)))
        if i in R.OPEN:
            new = []
            for tpl, x0, y0 in R.OPEN[i]:
                # 옛 눈(늘어난 검은 덩어리)을 주변 털로 지우고 새 눈 도장을 찍는다
                ax, ay = T(x0 - 2, y0 - 2); bx, by = T(x0 + len(tpl[0]) + 2, y0 + len(tpl) + 2)
                tr = H < 0
                for y in range(max(0, ay), min(F, by + 1)):
                    for x in range(max(0, ax), min(F, bx + 1)):
                        if H[y, x] != R.OUT: continue
                        if any(0 <= y+dy < F and 0 <= x+dx < F and tr[y+dy, x+dx] for dy in (-1,0,1) for dx in (-1,0,1)): continue
                        nb = [H[y+dy, x+dx] for dy in (-2,-1,0,1,2) for dx in (-2,-1,0,1,2)
                              if 0 <= y+dy < F and 0 <= x+dx < F and H[y+dy, x+dx] >= 0 and H[y+dy, x+dx] != R.OUT and H[y+dy, x+dx] not in R.PINK]
                        if nb: H[y, x] = max(set(nb), key=nb.count)
                nx, ny = T(x0 + 0.5, y0 + 0.5)
                new.append((R.EYE6, nx, ny))
            R.OPEN[i] = new
            R.frames[i] = H
        if i in R.ANCH:
            ax, ay, hw, d = R.ANCH[i]; nx, ny = T(ax, ay); R.ANCH[i] = (nx, ny, hw, d)
        if i in R.REFX:
            R.REFX[i] = cx + (R.REFX[i] - cx) * f
