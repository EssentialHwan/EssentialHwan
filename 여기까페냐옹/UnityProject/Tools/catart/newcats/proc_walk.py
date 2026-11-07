# 걷기: AI 몸통(다리 위) 1장 고정 + 다리 4개는 코드로 그려 8프레임 보행 주기를 만든다(프레임마다 생김새가 흔들리지 않게).
import numpy as np, math
from PIL import Image
def make_walk(base, cut=None, n=8, roots=None, legw=6, bob=True):
    m = base[..., 3] > 0; H, W = m.shape
    if cut is None:   # 몸통 아랫선: 아래에서 올라가며 덩어리가 1개가 되는 첫 줄
        for y in range(H - 1, 0, -1):
            xs = np.where(m[y])[0]
            if len(xs) and (np.diff(xs) > 1).sum() == 0: cut = y + 1; break
    legH = H - cut
    body = base.copy(); body[cut:] = 0
    # 색: 다리 영역 픽셀에서 몸 색(가장 흔한 색)과 외곽선(가장 어두운 색)
    allc = base[base[..., 3] > 0][:, :3].astype(int)
    dark = allc[allc.sum(1).argmin()]
    reg = base[cut - 6:cut][base[cut - 6:cut, :, 3] > 0][:, :3].astype(int)   # 배 아래쪽 몸색
    reg = reg[reg.sum(1) > dark.sum() + 25]
    cols, cnt = np.unique(reg, axis=0, return_counts=True)
    fill = cols[cnt.argmax()]
    far = (fill * 0.72 + dark * 0.28).astype(int)
    pad = np.clip(fill + 18, 0, 255)
    if roots is None:   # 다리 뿌리: 다리 줄 첫 행의 덩어리 중심
        y = cut + 2; xs = np.where(m[min(y, H - 1)])[0]
        runs = np.split(xs, np.where(np.diff(xs) > 1)[0] + 1)
        cs = [int(r.mean()) for r in runs if len(r) >= 3]
        cs = sorted(cs)
        if len(cs) < 4: cs = [cs[0], cs[0] + 6, cs[-1] - 6, cs[-1]] if len(cs) >= 2 else [8, 16, 28, 36]
        roots = cs[:2] + cs[-2:]          # 뒷다리 2, 앞다리 2 (왼→오)
    # 다리: (root x, 위상, 먼쪽?)  대각선 짝: 먼 뒷다리 + 가까운 앞다리 같은 위상
    legs = [(roots[0], 0.0, True), (roots[1], math.pi, False), (roots[2], math.pi, True), (roots[3], 0.0, False)]
    frames = []
    for i in range(n):
        t = 2 * math.pi * i / n
        o = np.zeros((H + 1, W + 4, 4), np.uint8)
        dy = 1 if (bob and i % (n // 2) in (1, 2)) else 0      # 발이 모두 땅에 있을 때 1px 내려앉기
        def draw_leg(rx, ph, isfar):
            sw = math.sin(t + ph)                 # -1..1 앞뒤
            lift = max(0.0, math.cos(t + ph))     # 앞으로 내딛는 동안 들림
            fx = rx + 2 + sw * 2.6; fy = H - 1 - round(lift * 2.0)
            hx, hy = rx + 2, cut - 3 + dy
            lm = np.zeros(o.shape[:2], bool)
            for yy in range(int(hy), int(fy) + 1):
                a_ = (yy - hy) / max(1, fy - hy); x = hx + (fx - hx) * a_
                x0 = int(round(x - legw / 2)); lm[yy, max(0, x0):max(0, x0 + legw)] = True
            # 외곽선 = 다리 마스크 경계, 속 = 몸색
            from scipy import ndimage
            inner = ndimage.binary_erosion(lm, structure=[[0,1,0],[1,1,1],[0,1,0]])
            c = far if isfar else fill
            top = np.zeros_like(lm); top[:int(hy) + 2] = True
            o[lm & ~inner & ~top, :3] = dark; o[(inner | (lm & top)), :3] = c; o[lm, 3] = 255
            if not isfar:   # 발끝 밝은 점
                ys_, xs_ = np.nonzero(inner)
                if len(ys_):
                    yb = ys_.max(); o[yb, xs_[ys_ == yb], :3] = pad
        for L in legs:
            if L[2]: draw_leg(*L)
        # 몸통(1px 내려앉기)
        bm = body[..., 3] > 0
        ys, xs = np.nonzero(bm)
        o[ys + dy, xs + 2] = body[ys, xs]
        for L in legs:
            if not L[2]: draw_leg(*L)
        frames.append(o)
    return frames
if __name__ == '__main__':
    import pickle
    fr = pickle.load(open('v3_frames.pkl', 'rb'))
    out = make_walk(fr[4])
    S = Image.new('RGBA', (64 * 8, 48), (214, 190, 150, 255))
    for j, f in enumerate(out): S.alpha_composite(Image.fromarray(f), (j * 64 + 4, 2))
    S.resize((S.width * 4, S.height * 4), Image.NEAREST).save('/tmp/claude-0/-home-claude/b7e3a153-a708-54d7-9b26-8c25b99f754e/scratchpad/proc_walk_strip.png')
    ims = []
    for f in out:
        bg = Image.new('RGBA', (64, 48), (214, 190, 150, 255)); bg.alpha_composite(Image.fromarray(f), (4, 2))
        ims.append(bg.resize((256, 192), Image.NEAREST).convert('P', palette=Image.ADAPTIVE))
    ims[0].save('v3_walk_proc.gif', save_all=True, append_images=ims[1:], duration=int(1000 / 11), loop=0)
