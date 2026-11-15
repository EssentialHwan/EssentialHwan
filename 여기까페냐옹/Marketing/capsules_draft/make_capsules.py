# 스팀 캡슐 임시안: 게임 스크린샷(1920x1080, 도트 4배) 원본 배율 그대로 잘라 + 갈무리 글꼴 제목 간판
import sys
from PIL import Image, ImageDraw, ImageFont
R=sys.argv[1]; OUT=R+'/Marketing/capsules_draft'
FONT=R+'/UnityProject/Assets/Resources/Fonts/Galmuri11.ttf'
shots={k:Image.open(f'{R}/Marketing/screenshots_v1/{v}').convert('RGB') for k,v in
       {'rain':'01_rain_morning_6cats.png','sun':'02_sunny_lunch.png','eve':'03_rain_evening_lamps.png'}.items()}
INK=(84,52,40); CREAM=(255,246,226); WOOD=(150,98,66); PINK=(214,96,120)

def sign(img, title, sub, cx, cy, scale):
    d=ImageDraw.Draw(img)
    ft=ImageFont.truetype(FONT, 11*scale*2); fs=ImageFont.truetype(FONT, 11*scale)
    tw,th=d.textbbox((0,0),title,font=ft)[2:]; sw,sh=d.textbbox((0,0),sub,font=fs)[2:] if sub else (0,0)
    w=max(tw,sw)+scale*16; h=th+(sh+scale*3 if sub else 0)+scale*12
    x0,y0=cx-w//2,cy-h//2
    p=scale
    d.rectangle([x0-2*p,y0-2*p,x0+w+2*p,y0+h+2*p],fill=INK)
    d.rectangle([x0,y0,x0+w,y0+h],fill=WOOD)
    d.rectangle([x0+2*p,y0+2*p,x0+w-2*p,y0+h-2*p],fill=CREAM)
    ty=y0+scale*5
    d.text((cx-tw//2+p,ty+p),title,font=ft,fill=(230,200,170)); d.text((cx-tw//2,ty),title,font=ft,fill=INK)
    if sub: d.text((cx-sw//2,ty+th+scale*3),sub,font=fs,fill=PINK)
    return img

def crop(img,cx,cy,w,h):
    x0=max(0,min(img.width-w,cx-w//2)); y0=max(0,min(img.height-h,cy-h//2))
    return img.crop((x0,y0,x0+w,y0+h))

for lang,title,sub in [('ko','여기카페냐옹','고양이 손님을 살피는 작은 카페'),('en','Purrsonal Taste','A tiny café for curious cats')]:
    # 헤더 920x430: 맑은 점심, 고양이 줄
    h=crop(shots['rain'],960,640,920,430); sign(h,title,sub,460,95,3); h.save(f'{OUT}/header_920x430_{lang}.png')
    # 메인 1232x706
    m=crop(shots['eve'],960,600,1232,706); sign(m,title,sub,616,120,4); m.save(f'{OUT}/main_1232x706_{lang}.png')
    # 세로 748x896
    v=crop(shots['rain'],820,560,748,896); sign(v,title,None,374,120,4); v.save(f'{OUT}/vertical_748x896_{lang}.png')
    # 라이브러리 600x900
    l=crop(shots['eve'],760,560,600,900); sign(l,title,None,300,110,3); l.save(f'{OUT}/library_600x900_{lang}.png')
    # 작은 캡슐 462x174: 924x348 잘라 반으로(최근접)
    s=crop(shots['rain'],960,650,924,348).resize((462,174),Image.NEAREST); sign(s,title,None,231,60,2); s.save(f'{OUT}/small_462x174_{lang}.png')
# 라이브러리 히어로 3840x1240(글자 없음): 2배 최근접
hero=crop(shots['eve'],960,600,1920,620).resize((3840,1240),Image.NEAREST); hero.save(f'{OUT}/library_hero_3840x1240.png')
# 페이지 배경 1438x810(글자 없음, 은은하게)
bg=crop(shots['rain'],960,540,1438,810); bg=Image.blend(bg,Image.new('RGB',bg.size,(40,30,28)),0.55); bg.save(f'{OUT}/page_bg_1438x810.png')
print('ok')
