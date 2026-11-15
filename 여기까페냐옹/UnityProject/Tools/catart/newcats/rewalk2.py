# 걷기 v2(10/28): 보폭을 키우고(다리 흔들기 17°→swing), 짚은 발이 바닥에 붙어 일정하게 밀리는 걸음(gait)으로 52~59칸만 다시 만든다.
#   python3 rewalk2.py <catId> [swing=26] [--install]
# 고양이 크기·앞다리 위치는 지금 시트의 걷기 칸에 자동으로 맞춘다(옛 설정으로 만든 것과 가장 비슷한 쪽 선택).
# 보폭(한 프레임에 짚은 발이 밀리는 px)을 출력 → CatView.WalkPxPerFrame 과 맞출 것.
import sys, shutil, math, numpy as np
from PIL import Image
import cutout_parts as P, build_cat as B
F = 56
cat = sys.argv[1]; swing = float(sys.argv[2]) if len(sys.argv) > 2 and not sys.argv[2].startswith('--') else 26.0
sheet_p = f'../../../Assets/Resources/CatSheets/cat_{cat}.bytes'
sheet = Image.open(sheet_p).convert('RGBA'); S = np.array(sheet)
def cell(k): return S[(k // 4) * F:(k // 4) * F + F, (k % 4) * F:(k % 4) * F + F]
def bw(a):
    xs = np.nonzero((a[..., 3] > 0).any(0))[0]; return xs.max() - xs.min() + 1
inst_w = max(bw(cell(52 + j)) for j in range(8))
a, body, front, hind = P.parts(f'{cat}_parts_src.png')
best = None
FF = [float(x.split('=')[1]) for x in sys.argv if x.startswith('--front=')]
for ff in (FF or [0.10, 0.16]):
    ref_hi = P.make(body, front, hind, 8, front_fwd=ff)
    union = np.zeros(ref_hi[0].shape[:2], bool)
    for f in ref_hi: union |= f[..., 3] > 0
    xs = np.nonzero(union.any(0))[0]; s = inst_w / (xs.max() - xs.min() + 1)
    g = P.to_game(ref_hi, scale=s)
    def pad(f):
        c = np.zeros((F, F, 4), np.uint8); h, w = f.shape[:2]; c[55 - h:55, (F - w) // 2:(F - w) // 2 + w] = f; return c
    err = sum(np.abs(pad(B.clean(g[j]))[..., 3].astype(int) - cell(52 + j)[..., 3].astype(int)).sum() for j in range(8))
    if best is None or err < best[0]: best = (err, ff, s)
err, ff, s = best
SC = [float(x.split('=')[1]) for x in sys.argv if x.startswith('--scale=')]
if SC: s = SC[0]   # 다시 뽑을 때 크기가 조금씩 변하지 않게 고정(회색냥 0.0613)
print(cat, 'front', ff, 'scale', round(s, 4), 'inst_w', inst_w)
def opt(k, d):
    return next((float(x.split('=')[1]) for x in sys.argv if x.startswith('--' + k + '=')), d)
# 걷기 v3 몸 반응(기본 끔 — 10/28 형 판단: 덜컹거려서 v2 유지. 예: --roll=0.04 --tail=7 --flex=20): 어깨·엉덩이 솟음(roll, 몸높이 비), 꼬리 살랑(tail, 도), 무릎·손목 접기(flex, 도)
hi = P.make(body, front, hind, 8, swing=swing, lift=opt('lift', 0.16), bob=opt('bob', 0.03), front_fwd=ff, gait=True,
            roll=opt('roll', 0), tail=opt('tail', 0), flex=opt('flex', 0), blend=opt('blend', 0))
walk = P.to_game(hi, scale=s)
lh = hind.shape[0]; lw = hind.shape[1]; R = (lh - lw * 0.45) * s
print('  stride px/frame ≈', round(2 * R * math.sin(math.radians(swing)) / 4, 2), ' frame', walk[0].shape[1], 'x', walk[0].shape[0])
for j, f in enumerate(walk):
    f = B.clean(f); h, w = f.shape[:2]
    if w > F: f = f[:, (w - F) // 2:(w - F) // 2 + F]; w = F
    c = Image.new('RGBA', (F, F), (0, 0, 0, 0)); c.alpha_composite(Image.fromarray(f), ((F - w) // 2, 55 - h))
    x, y = ((52 + j) % 4) * F, ((52 + j) // 4) * F
    sheet.paste(Image.new('RGBA', (F, F), (0, 0, 0, 0)), (x, y)); sheet.alpha_composite(c, (x, y))
tag = next((x.split('=')[1] for x in sys.argv if x.startswith('--tag=')), 'walk2')
out = f'cat_{cat}_{tag}.png'; sheet.save(out)
ims = []
for j in range(8):
    k = 52 + j; fr = sheet.crop(((k % 4) * F, (k // 4) * F, (k % 4) * F + F, (k // 4) * F + F))
    bg = Image.new('RGBA', (F, F), (214, 190, 150, 255)); bg.alpha_composite(fr)
    ims.append(bg.resize((F * 5, F * 5), Image.NEAREST).convert('P', palette=Image.ADAPTIVE))
ims[0].save(f'cat_{cat}_{tag}.gif', save_all=True, append_images=ims[1:], duration=80, loop=0)
if '--install' in sys.argv: shutil.copy(out, sheet_p); print('  installed')
