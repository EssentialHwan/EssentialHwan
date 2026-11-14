# ⑤ 인스타 카드뉴스: "나랑 닮은 고양이 손님은?" 1080×1350 × 6장
import os
from PIL import Image, ImageDraw, ImageFont
R = os.path.dirname(os.path.abspath(__file__)) + '/../../UnityProject/Assets/Resources'
FONT = f'{R}/Fonts/Galmuri11.ttf'; OUT = os.path.dirname(os.path.abspath(__file__)) + '/carousel_cattype'
CREAM, INK, SOFT, PINK, PAPER = (250, 240, 222), (92, 64, 52), (150, 112, 86), (196, 92, 110), (255, 250, 242)
W, H = 1080, 1350
def F(n): return ImageFont.truetype(FONT, n)
def close(cat, face='happy', k=3):
    im = Image.open(f'{R}/CatCloseups/close_cat_{cat}_{face}.bytes').convert('RGBA')
    return im.resize((im.width * k, im.height * k), Image.NEAREST)   # 도트가 고르게: 정수 배율만
def base():
    im = Image.new('RGB', (W, H), CREAM); d = ImageDraw.Draw(im)
    d.rectangle((24, 24, W - 24, H - 24), outline=INK, width=6)
    for x in range(60, W - 40, 70): d.text((x, H - 92), '🐾' if False else '·', font=F(30), fill=(225, 205, 180))
    d.text((W - 60 - d.textlength('@winterystudio', font=F(26)), H - 80), '@winterystudio', font=F(26), fill=SOFT)
    return im, d
def ctext(d, t, y, f, col):
    for i, ln in enumerate(t.split('\n')):
        d.text(((W - d.textlength(ln, font=f)) / 2, y + i * (f.size + 16)), ln, font=f, fill=col)
def page(n): 
    return f'{n} / 6'

# 1 표지
im, d = base()
ctext(d, '나랑 닮은\n고양이 손님은?', 150, F(84), INK)
ctext(d, '질문 3개 · 가장 많이 고른 알파벳을 세어 보세요', 400, F(32), SOFT)
for i, c in enumerate(['cheese', 'black']):
    p = close(c, 'happy', 2); im.paste(p, (30 + i * 516, 560), p)
ctext(d, '넘겨서 시작 →', 1150, F(44), PINK)
im.save(f'{OUT}/01_cover.png')

Q = [
    ('Q1. 쉬는 날, 나는?', ['A  하루 종일 이불 속에서 뒹굴', 'B  처음 가 보는 동네 구경', 'C  집에서 혼자 조용히 내 시간', 'D  친구들 불러서 수다']),
    ('Q2. 카페에 가면 앉는 자리는?', ['A  햇살 드는 푹신한 소파', 'B  바깥이 보이는 창가 자리', 'C  아무도 안 보는 구석 자리', 'D  여럿이 앉는 큰 테이블']),
    ('Q3. 메뉴판 앞에서 나는?', ['A  일단 제일 달달한 거', 'B  처음 보는 신메뉴 도전', 'C  늘 먹던 그거', 'D  친구랑 나눠 먹을 거']),
]
for qi, (q, opts) in enumerate(Q):
    im, d = base()
    ctext(d, q, 170, F(64), INK)
    for i, o in enumerate(opts):
        y = 380 + i * 170
        d.rounded_rectangle((110, y, W - 110, y + 130), radius=26, fill=PAPER, outline=INK, width=4)
        d.text((160, y + 42), o, font=F(42), fill=INK)
    ctext(d, page(qi + 2), 1150, F(32), SOFT)
    im.save(f'{OUT}/0{qi + 2}_q{qi + 1}.png')

# 5 결과
R4 = [('A', 'cheese', '치즈냥', '햇살 자리 지키는 느긋파'), ('B', 'kitten', '꼬물냥', '다 만져 봐야 하는 탐험가'),
      ('C', 'black', '까망냥', '구석에서 지켜보는 관찰자'), ('D', 'cow', '젖소냥', '누구랑도 친해지는 인싸')]
im, d = base()
ctext(d, '가장 많이 고른 건?', 110, F(64), INK)
for i, (k, c, nm, desc) in enumerate(R4):
    x, y = 70 + (i % 2) * 480, 260 + (i // 2) * 430
    d.rounded_rectangle((x, y, x + 460, y + 400), radius=28, fill=PAPER, outline=INK, width=4)
    p = close(c, 'happy', 1); im.paste(p, (x + (460 - p.width) // 2, y + 20), p)
    d.text((x + (460 - d.textlength(f'{k} → {nm}', font=F(40))) / 2, y + 225), f'{k} → {nm}', font=F(40), fill=PINK)
    d.text((x + (460 - d.textlength(desc, font=F(28))) / 2, y + 290), desc, font=F(28), fill=INK)
ctext(d, '댓글로 결과 알려 주세요!', 1150, F(40), PINK)
im.save(f'{OUT}/05_result.png')

# 6 게임 소개
im, d = base()
ctext(d, '이 고양이들이\n손님으로 오는 카페', 140, F(70), INK)
ctext(d, '손님마다 좋아하는 게 달라요.\n관찰해서 취향을 맞혀 주세요.', 400, F(38), SOFT)
for i, c in enumerate(['moon', 'siamese', 'tortie', 'fold', 'persian', 'senior']):
    p = close(c, 'happy', 1); im.paste(p, (66 + (i % 3) * 320, 560 + (i // 3) * 210), p)
ctext(d, 'Purrsonal Taste · 여기카페냐옹', 1030, F(46), PINK)
ctext(d, 'Steam 2027 · 저장해 두고 친구한테 공유!', 1110, F(32), SOFT)
im.save(f'{OUT}/06_game.png')
print('ok')
