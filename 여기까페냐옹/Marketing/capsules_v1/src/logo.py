from PIL import Image, ImageDraw, ImageFont
F='/home/claude/essentialhwan/essentialhwan/여기까페냐옹/UnityProject/Assets/Resources/Fonts/Galmuri11.ttf'
INK=(74,54,47,255); PINK=(232,150,165,255); PINKD=(196,92,110,255); WHITE=(255,250,240,255)
PAW=["..#.#..","#.....#","..###..",".#####.",".#####.","..###.."]
def text_native(s,size=12,col=INK):
    f=ImageFont.truetype(F,size); d=ImageDraw.Draw(Image.new('RGBA',(1,1)))
    bb=d.textbbox((0,0),s,font=f); im=Image.new('RGBA',(bb[2]+4,bb[3]+4),(0,0,0,0))
    d2=ImageDraw.Draw(im)
    if s.isascii():   # 영문: 두껍게 하면 rr이 붙어서 글자마다 1px 더 띄운다
        im=Image.new('RGBA',(int(bb[2]+len(s)*1.5)+6,bb[3]+4),(0,0,0,0)); d2=ImageDraw.Draw(im); x=2
        for ch in s:
            d2.text((x,2),ch,font=f,fill=col); d2.text((x+1,2),ch,font=f,fill=col); x+=d2.textlength(ch,font=f)+1
    else:
        d2.text((2,2),s,font=f,fill=col); d2.text((3,2),s,font=f,fill=col)   # 가로 1px 두껍게
    return im.crop(im.getbbox())
def dilate(im,col,r=1):
    o=Image.new('RGBA',(im.width+2*r,im.height+2*r),(0,0,0,0)); a=im.split()[3]
    for dx in range(-r,r+1):
        for dy in range(-r,r+1):
            if abs(dx)+abs(dy)<=r+ (1 if r>1 else 0): o.paste(Image.new('RGBA',im.size,col),(r+dx,r+dy),a)
    return o
def title(s, sub=None):
    if '\n' in s:
        ls=[text_native(x,12,INK) for x in s.split('\n')]
        t=Image.new('RGBA',(max(l.width for l in ls),sum(l.height for l in ls)+3*(len(ls)-1)),(0,0,0,0)); y=0
        for l in ls: t.alpha_composite(l,(0,y)); y+=l.height+3
    else: t=text_native(s,12,INK)
    # 오른쪽에 분홍 발바닥
    paw=Image.new('RGBA',(7,6),(0,0,0,0))
    for y,r in enumerate(PAW):
        for x,c in enumerate(r):
            if c=='#': paw.putpixel((x,y),PINKD)
    W=t.width+3+7; H=max(t.height,6)
    line=Image.new('RGBA',(W,H),(0,0,0,0)); line.alpha_composite(t,(0,0)); line.alpha_composite(paw,(t.width+3,0))
    if sub:
        st=text_native(sub,12,PINKD); scale_sub=1
        blk=Image.new('RGBA',(max(W,st.width),H+3+st.height),(0,0,0,0))
        blk.alpha_composite(line,((blk.width-W)//2,0)); blk.alpha_composite(st,((blk.width-st.width)//2,H+3)); line=blk
    # 그림자(분홍, 아래 1) + 흰 테두리 2
    ol=dilate(line,WHITE,2)
    sh=Image.new('RGBA',(ol.width+1,ol.height+2),(0,0,0,0))
    shadow=dilate(line,PINK,2)
    sh.alpha_composite(shadow,(1,2)); sh.alpha_composite(ol,(0,0))
    inner=Image.new('RGBA',sh.size,(0,0,0,0)); inner.alpha_composite(line,(2,2)); sh.alpha_composite(inner)
    return sh
def scaled(im,k): return im.resize((im.width*k,im.height*k),Image.NEAREST)
if __name__=='__main__':
    for lang,s in (('ko','여기카페냐옹'),('en','Purrsonal Taste'),('en2','Purrsonal\nTaste')):
        t=title(s); scaled(t,8).save(f'title_{lang}_x8.png'); print(lang,t.size)
