from PIL import Image, ImageDraw, ImageFont
F='/home/claude/essentialhwan/essentialhwan/여기까페냐옹/UnityProject/Assets/Resources/Fonts/Galmuri11.ttf'
OUT='/home/claude/essentialhwan/essentialhwan/여기까페냐옹/Marketing/studio_logo/'
INK=(52,62,92,255); SNOW=(150,190,230,255); WHITE=(255,255,255,255)
# 7x7 snowflake
flake=["......#......","...#..#..#...","....#.#.#....",".#...###...#.","..#...#...#..","...#######...","######.######","...#######...","..#...#...#..",".#...###...#.","....#.#.#....","...#..#..#...","......#......"]
def base(sub=True):
    w,h=140,40; im=Image.new('RGBA',(w,h),(0,0,0,0)); d=ImageDraw.Draw(im)
    f=ImageFont.truetype(F,12)
    for y,r in enumerate(flake):
        for x,c in enumerate(r):
            if c=='#': im.putpixel((4+x,(8 if sub else 2)+y),SNOW)
    d.text((21,(3 if sub else 3)),"Wintery",font=f,fill=INK)
    if sub:
        f2=ImageFont.truetype(F,12)
        tw=d.textlength("Wintery",font=f); ws=[d.textlength(c,font=f2) for c in "STUDIO"]
        gap=(tw-sum(ws))/5; x=21
        for c,cw in zip("STUDIO",ws): d.text((round(x),18),c,font=f2,fill=SNOW); x+=cw+gap
    bb=im.getbbox(); im=im.crop((bb[0]-2,bb[1]-2,bb[2]+2,bb[3]+2))
    return im
def outline(im,col):
    o=Image.new('RGBA',im.size,(0,0,0,0)); a=im.split()[3]
    for dx,dy in [(-1,0),(1,0),(0,-1),(0,1),(-1,-1),(1,1),(-1,1),(1,-1)]:
        o.paste(Image.new('RGBA',im.size,col),(dx,dy),a)
    o.alpha_composite(im); return o
for name,sub in [('logo',True),('logo_oneline',False)]:
    im=outline(base(sub),WHITE)
    for k in (4,8):
        im.resize((im.width*k,im.height*k),Image.NEAREST).save(f'{OUT}{name}_x{k}.png')
# 라이브러리 로고 1280x720 투명
im=outline(base(True),WHITE); k=min(1100//im.width,600//im.height)
big=im.resize((im.width*k,im.height*k),Image.NEAREST)
c=Image.new('RGBA',(1280,720),(0,0,0,0)); c.alpha_composite(big,((1280-big.width)//2,(720-big.height)//2)); c.save(OUT+'library_logo_1280x720.png')
# 미리보기: 밝은/어두운 배경
pv=Image.new('RGBA',(1280,480),(255,246,224,255)); ImageDraw.Draw(pv).rectangle((640,0,1280,480),fill=(40,44,66,255))
s=im.resize((im.width*6,im.height*6),Image.NEAREST)
pv.alpha_composite(s,((640-s.width)//2,(480-s.height)//2)); pv.alpha_composite(s,(640+(640-s.width)//2,(480-s.height)//2))
pv.convert('RGB').save('logo_preview.png')
