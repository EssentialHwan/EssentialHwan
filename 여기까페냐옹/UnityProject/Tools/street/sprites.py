# 창밖 거리 행인·탈것 도트 템플릿 미리보기 (C# StreetArt.cs와 같은 문자열을 쓴다)
# 문자: . 투명  o 외곽선  F 털  L 밝은 털(배·얼굴)  e 눈  p 코·볼  B 몸체색(차/옷)  b 몸체 어두운색  w 유리창  k 바퀴  s 은색  r 빨강  y 노랑(라이트)  u 우산  U 우산 어두운
import sys
from PIL import Image

T = {}
# 네 발 고양이 행인(오른쪽 보기) 16×11, 2프레임
T['walk4_0'] = [
"...........o..o.",
"..........oFooFo",
"o.........oFFFFo",
"oFo.......oFeFeo",
".oFo......oLFpLo",
"..oFooooooFFLLo.",
"..oFFFFFFFFFFo..",
"..oFFFFFFFFLLo..",
"...oFFFFFFFLLo..",
"...oFo.oFo.oFo..",
"...oo..oo..oo...",
]
T['walk4_1'] = [
"...........o..o.",
"..........oFooFo",
"o.........oFFFFo",
"oFo.......oFeFeo",
".oFo......oLFpLo",
"..oFooooooFFLLo.",
"..oFFFFFFFFFFo..",
"..oFFFFFFFFLLo..",
"...oFFFFFFFLLo..",
"..oFo..oFo..oFo.",
"..oo...oo...oo..",
]
# 두 발로 걷는 우산 고양이 13×17
T['umb_0'] = [
"......o......",
"...ooUuuoo...",
".ooUUuuuuuoo.",
"oUUUuuuuuuuuo",
"ooooooooooooo",
"......o......",
"....oo.o.o...",
"....oFoFFo...",
"....oFeFeo...",
"....oLFpLo...",
"....ooBBBoo..",
"....oBBBBBo..",
"....oBBBBo...",
"....oBBBBo...",
"....oLo.oLo..",
"....oo..oo...",
".............",
]
T['umb_1'] = [
"......o......",
"...ooUuuoo...",
".ooUUuuuuuoo.",
"oUUUuuuuuuuuo",
"ooooooooooooo",
"......o......",
"....oo.o.o...",
"....oFoFFo...",
"....oFeFeo...",
"....oLFpLo...",
"....ooBBBoo..",
"....oBBBBBo..",
"....oBBBBo...",
"....oBBBBo...",
"...oLo..oLo..",
"...oo....oo..",
".............",
]
# 장바구니 든 고양이(두 발) 12×14
T['bag_0'] = [
"....o..o....",
"...oFooFo...",
"...oFFFFo...",
"...oFeFeo...",
"...oLFpLo...",
"....oBBo....",
"...oBBBBo...",
"..oFBBBBoo..",
"..o.oBBBorro",
"....oBBBorro",
"....oLooLorr",
"....oo..oo..",
]
T['bag_1'] = [
"....o..o....",
"...oFooFo...",
"...oFFFFo...",
"...oFeFeo...",
"...oLFpLo...",
"....oBBo....",
"...oBBBBo...",
"..oFBBBBoo..",
"..o.oBBBorro",
"....oBBBorro",
"...oLo.oLorr",
"...oo...oo..",
]
# 고양이가 운전하는 자동차(오른쪽) 28×13
T['car_0'] = [
"........oooooooooo..........",
".......oBwwwwoowwwo.........",
"......oBwwo.owowwwwo........",
".....oBBwoFooFowwwwwo.......",
"....oBBBwoeFeFowwwwwwoooo...",
"..ooBBBBBoLpLooBBBBBBBBBBoo.",
".oBBBBBBBBBBBBBBBBBBBBBBBByo",
".oBBBBBBBBbBBBBBBBBbBBBBBByo",
".obbbbbbbbbbbbbbbbbbbbbbbbbo",
".ooookkkooooooooooookkkooooo",
"....kkskk..........kkskk....",
"....kksk...........kksk.....",
".....kk.............kk......",
]
T['car_1'] = [
"........oooooooooo..........",
".......oBwwwwoowwwo.........",
"......oBwwo.owowwwwo........",
".....oBBwoFooFowwwwwo.......",
"....oBBBwoeFeFowwwwwwoooo...",
"..ooBBBBBoLpLooBBBBBBBBBBoo.",
".oBBBBBBBBBBBBBBBBBBBBBBBByo",
".oBBBBBBBBbBBBBBBBBbBBBBBByo",
".obbbbbbbbbbbbbbbbbbbbbbbbbo",
".ooookkkooooooooooookkkooooo",
"....kskkk..........kskkk....",
"....kkks...........kkks.....",
".....kk.............kk......",
]
# 자전거 고양이 18×15
T['bike_0'] = [
"..........o..o....",
".........oFooFo...",
".........oFFFFo...",
".........oFeFeo...",
".........oLFpLo...",
"........ooBBBo....",
".......oBBBBBoss..",
"......oBBBBoo..s..",
".....oBBBoLo...s..",
"....sssssoo.sss...",
"..kkk.s.s...s.kkk.",
".k...k.s.s.s.k...k",
".k.s.k..sss..k.s.k",
".k...k.......k...k",
"..kkk.........kkk.",
]
T['bike_1'] = [
"..........o..o....",
".........oFooFo...",
".........oFFFFo...",
".........oFeFeo...",
".........oLFpLo...",
"........ooBBBo....",
".......oBBBBBoss..",
"......oBBBBoo..s..",
".....oBBBoLo...s..",
"....sssssoo.sss...",
"..kkk.s.s...s.kkk.",
".k...k.s.s.s.k...k",
".ks..k..sss..ks..k",
".k...k.......k...k",
"..kkk.........kkk.",
]
# 배달 오토바이(빨간 헬멧 고양이 + 상자) 22×15
T['moto_0'] = [
"............oooo......",
"...........orrrro.....",
"..oooooo..orrrrrro....",
"..oyyyyo..oFeFeFo.....",
"..oyyyyo..oLFpLo......",
"..oyyyyo...oBBo.......",
"..oooooo..oBBBBo..ss..",
"...ooooo.oBBBoo..s....",
"..orrrrrrrrrrrrrrrrro..",
".orrrrrrrrrrrrrrrrrryo",
"..oooooooooooooooooo..",
"..kkk............kkk..",
".k.s.k..........k.s.k.",
".k...k..........k...k.",
"..kkk............kkk..",
]
T['moto_1'] = [
"............oooo......",
"...........orrrro.....",
"..oooooo..orrrrrro....",
"..oyyyyo..oFeFeFo.....",
"..oyyyyo..oLFpLo......",
"..oyyyyo...oBBo.......",
"..oooooo..oBBBBo..ss..",
"...ooooo.oBBBoo..s....",
"..orrrrrrrrrrrrrrrrro..",
".orrrrrrrrrrrrrrrrrryo",
"..oooooooooooooooooo..",
"..kkk............kkk..",
".ks..k..........ks..k.",
".k...k..........k...k.",
"..kkk............kkk..",
]
# 버스(창마다 고양이 손님) 48×19
def bus(fr):
    top = "..oooooooooooooooooooooooooooooooooooooooooooo.."
    rows = [top,
    ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBo.",
    ".oBwwwwwwoBwwwwwwoBwwwwwwoBwwwwwwoBwwwwwwwwwwBo.",
    ".oBwoFoFwoBwwwwwwoBwoFoFwoBwwoFoFoBwwwwwwwwwwBo.",
    ".oBwFeFewoBwwwwwwoBwFeFewoBwwFeFeoBwwwwwwwwwwBo.",
    ".oBwLpLwwoBwwwwwwoBwLpLwwoBwwLpLwoBwwwwwwwwwwBo.",
    ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBo",
    ".oyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyBo",
    ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBByo",
    ".oBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBByo",
    ".obbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbo",
    ".ooooookkkkooooooooooooooooooooooooookkkkoooooo.",
    ".....kkssskk......................kkssskk.......",
    ".....kksskkk......................kksskkk.......",
    "......kkkk.........................kkkk........",
    ]
    if fr: rows[12] = ".....kkskskk......................kkskskk......."; rows[13] = ".....kkkskkk......................kkkskkk......."
    return rows
T['bus_0'] = bus(0); T['bus_1'] = bus(1)
# 건너편 작은 행인 7×6
T['tiny_0'] = ["....o.o", "o..oFFo", "oFFFFeF", ".oFFFFo", ".o.o.o.", "......."]
T['tiny_1'] = ["....o.o", "o..oFFo", "oFFFFeF", ".oFFFFo", "o.o.o..", "......."]
# 새 5×3
T['bird_0'] = ["o...o", ".o.o.", "..o.."]
T['bird_1'] = [".....", "oo.oo", "..o.."]

PAL = {
 'o': (54, 40, 44), 'e': (30, 24, 30), 'p': (240, 150, 160), 'w': (178, 214, 236), 'k': (40, 38, 46), 's': (190, 196, 206),
 'r': (222, 70, 70), 'y': (255, 226, 120), 'u': (120, 170, 230), 'U': (86, 128, 196),
}
FUR = {'orange': ((240, 156, 70), (252, 214, 170)), 'gray': ((150, 152, 166), (226, 228, 236)), 'black': ((66, 60, 72), (130, 120, 130)),
       'white': ((244, 240, 234), (255, 255, 255)), 'cream': ((236, 206, 160), (252, 238, 214))}
BODY = [((230, 92, 92), (176, 60, 70)), ((86, 150, 214), (60, 106, 170)), ((250, 200, 80), (200, 150, 50)), ((120, 190, 130), (80, 140, 96)), ((238, 238, 240), (170, 176, 190))]

def render(rows, fur='orange', body=0):
    h = len(rows); w = max(len(r) for r in rows)
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    F, L = FUR[fur]; B, b = BODY[body]
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == '.': continue
            col = {'F': F, 'L': L, 'B': B, 'b': b}.get(ch) or PAL.get(ch)
            im.putpixel((x, y), col + (255,))
    return im

if __name__ == '__main__':
    bad = [(k, len(set(len(r) for r in v))) for k, v in T.items() if len(set(len(r) for r in v)) > 1]
    print('ragged:', bad)
    W = 300; H = 140
    sheet = Image.new('RGBA', (W, H), (150, 200, 236, 255))
    x = 4; y = 4
    for i, (k, fur, body) in enumerate([('walk4_0', 'orange', 0), ('walk4_1', 'gray', 0), ('umb_0', 'white', 1), ('bag_0', 'black', 2),
                                        ('car_0', 'cream', 0), ('car_1', 'gray', 1), ('bike_0', 'orange', 3), ('moto_0', 'cream', 4),
                                        ('bus_0', 'orange', 3), ('tiny_0', 'gray', 0), ('tiny_1', 'black', 0), ('bird_0', 'black', 0), ('bird_1', 'black', 0)]):
        im = render(T[k], fur, body)
        if x + im.width > W: x = 4; y += 26
        sheet.alpha_composite(im, (x, y)); x += im.width + 6
    sheet.resize((W * 4, H * 4), Image.NEAREST).save(sys.argv[1] if len(sys.argv) > 1 else '/tmp/street.png')
