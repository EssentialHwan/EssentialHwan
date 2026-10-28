from PIL import Image, ImageFilter, ImageEnhance
import os
from logo import title, scaled
OUT='out'; os.makedirs(OUT,exist_ok=True)
T={'ko':title('여기카페냐옹'),'en':title('Purrsonal\nTaste')}
def fit(src,W,H,cx=0.5,cy=0.5):
    im=Image.open(src).convert('RGB'); r=W/H
    if im.width/im.height>r: w=round(im.height*r); x=round((im.width-w)*cx); im=im.crop((x,0,x+w,im.height))
    else: h=round(im.width/r); y=round((im.height-h)*cy); im=im.crop((0,y,im.width,y+h))
    return im.resize((W,H),Image.LANCZOS).convert('RGBA')
def put(bg,lang,k,pos):
    t=scaled(T[lang],k); x,y=pos
    if x=='c': x=(bg.width-t.width)//2
    bg.alpha_composite(t,(x,y)); return bg
spec={
 'header_920x430':('header_src.png',920,430,0.5,0.5,4,(26,24)),
 'small_462x174':('header_src.png',462,174,0.5,0.35,3,(10,10)),
 'main_1232x706':('main_src.png',1232,706,0.5,0.5,5,(34,30)),
 'vertical_748x896':('vert_src.png',748,896,0.5,0.5,5,(30,30)),
 'library_600x900':('vert_src.png',600,900,0.49,0.5,4,('c',34)),
}
for name,(src,W,H,cx,cy,k,pos) in spec.items():
    for lang in ('ko','en'):
        bg=fit(src,W,H,cx,cy); put(bg,lang,k,pos); bg.convert('RGB').save(f'{OUT}/{name}_{lang}.png')
fit('hero_src.png',3840,1240).convert('RGB').save(f'{OUT}/library_hero_3840x1240.png')
pb=fit('hero_src.png',1438,810); pb=ImageEnhance.Brightness(pb.filter(ImageFilter.GaussianBlur(2))).enhance(0.92)
pb.convert('RGB').save(f'{OUT}/page_bg_1438x810.png')
for lang in ('ko','en'):
    t=scaled(T[lang],12 if lang=='ko' else 11); c=Image.new('RGBA',(1280,720),(0,0,0,0))
    c.alpha_composite(t,((1280-t.width)//2,(720-t.height)//2)); c.save(f'{OUT}/library_logo_1280x720_{lang}.png')
# 자가 테스트: 블러 / 암전
for n in ('header_920x430_ko','small_462x174_ko','main_1232x706_ko'):
    im=Image.open(f'{OUT}/{n}.png')
    im.filter(ImageFilter.GaussianBlur(im.width/120)).save(f'{OUT}/_test_blur_{n}.png')
    ImageEnhance.Brightness(ImageEnhance.Color(im).enhance(0)).enhance(0.45).save(f'{OUT}/_test_dark_{n}.png')
print(sorted(os.listdir(OUT)))
