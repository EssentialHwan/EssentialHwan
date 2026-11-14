# 새 그림 고양이들이 제자리 걷기하는 GIF(X 글 3번). 여기까페냐옹/ 폴더에서 실행:
#   python3 Marketing/x_assets/make_newcats_gif.py cheese calico cow black tuxedo mackerel siamese gray
import sys, csv
from PIL import Image, ImageDraw, ImageFont
F = 56; k = 4
cats = sys.argv[1:] or ['cheese', 'calico', 'cow', 'black', 'tuxedo', 'mackerel', 'siamese', 'gray']
names = {r[0]: r[2] for r in csv.reader(open('UnityProject/Assets/Resources/strings.csv', encoding='utf-8'))
         if r and r[0].startswith('cat.cat_') and r[0].endswith('.name')}
sheets = {c: Image.open(f'UnityProject/Assets/Resources/CatSheets/cat_{c}.bytes').convert('RGBA') for c in cats}
font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 22)
cols = 4; rows = (len(cats) + cols - 1) // cols
W, H = 960, 40 + rows * 260
frames = []
for t in range(8):
    can = Image.new('RGBA', (W, H), (250, 240, 222, 255)); d = ImageDraw.Draw(can)
    for j, c in enumerate(cats):
        cell = 52 + t
        img = sheets[c].crop(((cell % 4) * F, (cell // 4) * F, (cell % 4 + 1) * F, (cell // 4 + 1) * F)).resize((F * k, F * k), Image.NEAREST)
        cx = 120 + (j % cols) * 240; top = -20 + (j // cols) * 260
        d.ellipse((cx - 70, top + 212, cx + 70, top + 230), fill=(228, 210, 186, 255))
        can.alpha_composite(img, (cx - F * k // 2, top))
        n = names.get(f'cat.cat_{c}.name', c); w = d.textlength(n, font=font)
        d.text((cx - w / 2, top + 234), n, font=font, fill=(110, 80, 64, 255))
    frames.append(can.convert('RGB'))
frames[0].save('Marketing/x_assets/post03_new_cats.gif', save_all=True, append_images=frames[1:], duration=110, loop=0)
print('ok', len(cats), 'cats', W, 'x', H)
