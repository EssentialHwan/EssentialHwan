# 릴스용 소품(게임 코드 FurnitureDrawing 그대로 옮김): 캣휠 받침+돌아가는 바퀴, 2층 선반.
import math
from PIL import Image
WL, WM, WD = (214, 178, 138, 255), (184, 142, 100, 255), (150, 110, 74, 255)
PINK, YEL, YELL, METAL, OUT = (230, 160, 172, 255), (232, 198, 112, 255), (248, 224, 156, 255), (120, 118, 126, 255), (74, 54, 47, 255)
W, H, CX, CY, R = 60, 62, 30, 34, 26
def _canvas(): return [[(0, 0, 0, 0)] * W for _ in range(H)]
def _img(px):   # 게임 캔버스는 y=0이 아래 → 뒤집어서 이미지로
    im = Image.new('RGBA', (W, H))
    for y in range(H):
        for x in range(W): im.putpixel((x, H - 1 - y), px[y][x])
    return im
def _set(px, x, y, c):
    if 0 <= x < W and 0 <= y < H: px[y][x] = c
def outline(im):
    a = im.split()[3]; o = im.copy()
    for y in range(im.height):
        for x in range(im.width):
            if a.getpixel((x, y)) == 0 and any(0 <= x + dx < im.width and 0 <= y + dy < im.height and a.getpixel((x + dx, y + dy)) > 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o.putpixel((x, y), OUT)
    return o
def wheel_base():
    px = _canvas()
    for y in range(4, 9):
        for x in range(6, 54): _set(px, x, y, WM)
    for x in range(7, 53): _set(px, x, 8, WL)
    for x in range(6, 54): _set(px, x, 4, WD)
    for i in range(27):
        u = i / 26; lx = round(10 + (CX - 2 - 10) * u); rx = round(49 + (CX + 2 - 49) * u); y = round(8 + (CY - 8) * u)
        for x in range(lx - 1, lx + 2): _set(px, x, y, WM)
        _set(px, lx - 1, y, WL)
        for x in range(rx - 1, rx + 2): _set(px, x, y, WM)
        _set(px, rx + 1, y, WD)
    return outline(_img(px))
def wheel_spin(frame):
    px = _canvas()
    rimO, rim, rimL, track, back, spoke = (118, 182, 172, 255), (156, 214, 202, 255), (204, 238, 228, 255), (250, 240, 222, 255), (246, 236, 220, 200), (130, 192, 182, 255)
    for y in range(CY - R - 1, CY + R + 2):
        for x in range(CX - R - 1, CX + R + 2):
            d = math.hypot(x - CX, y - CY)
            if d <= R - 4: _set(px, x, y, back)
            elif d <= R - 3: _set(px, x, y, track)
            elif d <= R - 1: _set(px, x, y, rimL if y > CY else rim)
            elif d <= R: _set(px, x, y, rimO)
    base = -frame * math.radians(11.25)
    for kk in range(8):
        a = base + kk * math.pi / 4; ca, sa = math.cos(a), math.sin(a); r = 3.0
        while r <= R - 4: _set(px, round(CX + ca * r), round(CY + sa * r), spoke); r += 0.5
        _set(px, round(CX + ca * (R - 2)), round(CY + sa * (R - 2)), PINK if kk % 2 == 0 else YEL)
    for y in range(CY - 3, CY + 4):
        for x in range(CX - 3, CX + 4):
            if math.hypot(x - CX, y - CY) <= 3: _set(px, x, y, YEL)
    _set(px, CX - 1, CY + 1, YELL); _set(px, CX, CY, METAL)
    return _img(px)
def shelf(w=64, h=10):
    im = Image.new('RGBA', (w, h + 14), (0, 0, 0, 0))
    for y in range(h):
        for x in range(w): im.putpixel((x, y), WL if y < 2 else (WM if y < h - 2 else WD))
    for bx in (6, w - 9):   # 받침 쇠붙이
        for y in range(h, h + 12):
            for x in range(bx, bx + 3): im.putpixel((x, y), WD)
    return outline(im)
