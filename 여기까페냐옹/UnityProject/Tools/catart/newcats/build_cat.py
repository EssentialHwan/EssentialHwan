# 고양이 1마리 = 원화 3장 → 게임 시트(4×15) 한 번에.
#   python3 build_cat.py <catId> <sheetA.png> <sheetC.png|-> <parts.png> [--install]
#   A: 기본 16포즈(4×4)  C: 생활 동작 16(4×4, 없으면 '-')  parts: 몸통·앞다리·뒷다리(왼→오)
# 칸: 0~24 기본(MAP) · 28~31 뛰기(부품) · 32~35 앉아 대기 · 36~51 시트 C · 52~59 걷기(부품 컷아웃)
import sys, shutil, numpy as np
from PIL import Image
from scipy import ndimage
import convert as C, convert2 as C2, build_v2 as V, build_new4 as B, cutout_parts as P
F = C.F

def grid_frames(src, sit_h=42.0):
    a, cells = C.poses(src)
    miss = [k for k in range(16) if k not in cells]
    if miss: print('  !! 빠진 칸', src, miss)
    allfg = np.zeros(a.shape[:2], bool)
    for m in cells.values(): allfg |= m
    pal, _ = C.quantize(a, allfg, 64); pal = np.vstack([pal, V.accent_palette(a, allfg)]).astype(np.float32)
    return a, cells, pal

def clean(f):
    m = f[..., 3] > 0; lab, n = ndimage.label(m, structure=np.ones((3, 3)))
    if n <= 1: return f
    sizes = ndimage.sum(m, lab, range(1, n + 1)); keep = np.r_[False, sizes >= 12]
    g = f.copy(); g[~keep[lab]] = 0; return g

def main():
    cat, srcA, srcC, srcP = sys.argv[1:5]
    sheet = Image.new('RGBA', (F * 4, F * 15), (0, 0, 0, 0))
    def put(cell, f):
        f = clean(f); h, w = f.shape[:2]
        c = Image.new('RGBA', (F, F), (0, 0, 0, 0)); c.alpha_composite(Image.fromarray(f), ((F - w) // 2, 55 - h))
        sheet.alpha_composite(c, ((cell % 4) * F, (cell // 4) * F))
    # A
    a, cells, pal = grid_frames(srcA)
    s = B.fit_scale(cells)
    A = {k: C2.down2(a, pal, cells[k], s) for k in cells}
    for g in range(25): put(g, A[C.MAP[g]] if C.MAP[g] in A else A[0])
    sitH = A[0].shape[0]
    for j, k in enumerate((0, 0, 2, 0)): put(32 + j, A[k])          # 앉아 대기: 기본·기본·눈감기·기본
    # C
    if srcC != '-':
        c_a, c_cells, c_pal = grid_frames(srcC)
        def bbh(m): ys = np.nonzero(m)[0]; return ys.max() - ys.min() + 1
        cs = sitH * 0.95 / np.median([bbh(c_cells[k]) for k in range(4) if k in c_cells])
        for k in range(16):
            if k in c_cells: put(36 + k, C2.down2(c_a, c_pal, c_cells[k], cs))
    else:   # 시트 C가 없으면 기본 포즈로 대신
        for j, k in enumerate((10, 10, 10, 10, 11, 11, 15, 15, 9, 9, 8, 8, 12, 12, 13, 12)): put(36 + j, A[k])
    # 부품 → 걷기 8 + 뛰기 4
    pa, body, front, hind = P.parts(srcP)
    walk = P.to_game(P.make(body, front, hind, 8))
    for j, f in enumerate(walk): put(52 + j, f)
    run_hi = P.make(body, front, hind, 4, swing=32, lift=0.22, bob=0.04)
    run = P.to_game(run_hi, width_px=50)
    for j, f in enumerate(run): put(28 + j, f)
    out = f'cat_{cat}_v4.png'; sheet.save(out); print('ok', out, sheet.size)
    # 확인용 미리보기(4배)
    pv = Image.new('RGBA', sheet.size, (214, 190, 150, 255)); pv.alpha_composite(sheet)
    pv.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(f'cat_{cat}_v4_preview.png')
    ims = []
    for f in walk:
        bg = Image.new('RGBA', (f.shape[1], f.shape[0]), (214, 190, 150, 255)); bg.alpha_composite(Image.fromarray(f))
        ims.append(bg.resize((f.shape[1] * 5, f.shape[0] * 5), Image.NEAREST).convert('P', palette=Image.ADAPTIVE))
    ims[0].save(f'cat_{cat}_walk.gif', save_all=True, append_images=ims[1:], duration=100, loop=0)
    if '--install' in sys.argv:
        shutil.copy(out, f'../../../Assets/Resources/CatSheets/cat_{cat}.bytes'); print('installed')

if __name__ == '__main__': main()
