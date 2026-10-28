# 걷기 전용 한 줄 원화 → 프레임 분리·몸통 일관성 측정·머리 기준 정렬 → 게임 크기 GIF
import numpy as np, sys, pickle
from PIL import Image
from scipy import ndimage
from seg import fg_mask
import convert as C, convert2 as C2, build_v2 as V
src = sys.argv[1]
a, fg0 = fg_mask(src)
lab, n = ndimage.label(ndimage.binary_closing(fg0, iterations=2))
objs = []
for i, sl in enumerate(ndimage.find_objects(lab), 1):
    m = (lab == i) & fg0
    if m.sum() < 2000: continue
    ys, xs = np.nonzero(m); objs.append((xs.min(), m))
objs.sort(key=lambda o: o[0]); masks = [o[1] for o in objs]
print('frames', len(masks))
def bb(m): ys, xs = np.nonzero(m); return xs.min(), ys.min(), xs.max(), ys.max()
# 머리 기준점: 가장 오른쪽(코) x, 귀 꼭대기 y
info = []
for m in masks:
    x0, y0, x1, y1 = bb(m); info.append((x1 - x0, y1 - y0, y0, y1))
    print('w,h,top,bottom', x1 - x0, y1 - y0, y0, y1)
allfg = np.zeros(fg0.shape, bool)
for m in masks: allfg |= m
pal, _ = C.quantize(a, allfg, 64); pal = np.vstack([pal, V.accent_palette(a, allfg)]).astype(np.float32)
# 한 배율로(앉기 기준과 같은 크기: 몸길이 ≈ 46px)
s = 46.0 / np.median([i[0] for i in info]) * 0.97
frames = []
for m in masks:
    x0, y0, x1, y1 = bb(m)
    # down2는 마스크 bbox로 자르므로, 공통 박스(같은 바닥·같은 코 위치)로 맞추기 위해 원점 정보를 따로 계산
    fr = C2.down2(a, pal, m, s)
    # 눈(노랑) 중심 x — 가로 정렬 기준(앞발이 코보다 앞으로 나와도 몸이 밀리지 않게)
    px = a.astype(int); eye = m & (px[..., 0] > 170) & (px[..., 1] > 110) & (px[..., 2] < 110)
    ex = np.nonzero(eye)[1].mean() if eye.any() else x1 - 40
    frames.append((fr, x0, ex))
# 정렬: 코 x(오른쪽 끝)와 발바닥 y(아래) 기준
W, H = 64, 52
out = []
gx = max(f[1] for f in frames); gy = max(f[2] for f in frames)
for fr, x0, ex in frames:
    h, w = fr.shape[:2]
    c = np.zeros((H, W, 4), np.uint8)
    by = H - 2; lx = 46 - round((ex - x0) * s)          # 눈이 항상 x=46에 오게
    c[by - h + 1:by + 1, max(0, lx):lx + w] = fr[:, max(0, -lx):][:, :W - max(0, lx)]
    out.append(c)
pickle.dump(out, open('walkW_frames.pkl', 'wb'))
def gif(seq, name, fps, k):
    ims = []
    for f in seq:
        bg = Image.new('RGBA', (W, H), (214, 190, 150, 255)); bg.alpha_composite(Image.fromarray(f))
        ims.append(bg.resize((W * k, H * k), Image.NEAREST).convert('P', palette=Image.ADAPTIVE))
    ims[0].save(name, save_all=True, append_images=ims[1:], duration=int(1000 / fps), loop=0)
gif(out, 'walkW_game_x4.gif', 9, 4)
S = Image.new('RGBA', (W * len(out), H), (214, 190, 150, 255))
for j, f in enumerate(out): S.alpha_composite(Image.fromarray(f), (j * W, 0))
S.resize((S.width * 4, S.height * 4), Image.NEAREST).save('walkW_strip.png')
# 몸통 일관성: 머리 영역(위 50%) 프레임 간 차이
heads = [f[:H // 2, :, 3] > 0 for f in out]
print('head diff vs f0', [round((heads[0] ^ h).mean(), 3) for h in heads])
legs = [f[-12:, :, 3] > 0 for f in out]
print('leg diff consecutive', [round((legs[i] ^ legs[(i + 1) % len(legs)]).mean(), 3) for i in range(len(legs))])
