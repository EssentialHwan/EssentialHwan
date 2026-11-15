# 시트는 그대로 두고 걷기(52~59)·뛰기(28~31) 칸만 새 컷아웃 방식으로 다시 만든다(까망냥처럼 다른 경로로 만든 시트용)
#   python3 rewalk.py <sheet.png> <parts.png> <out.png> [--front=0.16]
import sys, numpy as np
from PIL import Image
import cutout_parts as P, build_cat as B
sheet_p, parts_p, out_p = sys.argv[1:4]
ff = next((float(a.split('=')[1]) for a in sys.argv if a.startswith('--front=')), 0.10)
F = 56
sheet = Image.open(sheet_p).convert('RGBA')
def put(cell, f):
    f = B.clean(f); h, w = f.shape[:2]
    c = Image.new('RGBA', (F, F), (0, 0, 0, 0)); c.alpha_composite(Image.fromarray(f), ((F - w) // 2, 55 - h))
    x, y = (cell % 4) * F, (cell // 4) * F
    sheet.paste(Image.new('RGBA', (F, F), (0, 0, 0, 0)), (x, y)); sheet.alpha_composite(c, (x, y))
a, body, front, hind = P.parts(parts_p)
for j, f in enumerate(P.to_game(P.make(body, front, hind, 8, front_fwd=ff))): put(52 + j, f)
for j, f in enumerate(P.to_game(P.make(body, front, hind, 4, swing=32, lift=0.22, bob=0.04, front_fwd=ff), width_px=50)): put(28 + j, f)
sheet.save(out_p); print('ok', out_p)
