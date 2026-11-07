from PIL import Image, ImageDraw, ImageFont
F='/home/claude/essentialhwan/essentialhwan/여기까페냐옹/UnityProject/Assets/Resources/Fonts/Galmuri11.ttf'
INK=(53,61,104,255); BLUE=(148,183,222,255); WHITE=(255,255,255,255); PALE=(210,228,250,255)
src=Image.open('clean_fixed.png'); src=src.crop(src.getbbox())
def outline(im,col=WHITE):
    o=Image.new('RGBA',im.size,(0,0,0,0)); a=im.split()[3]
    for dx,dy in [(-1,0),(1,0),(0,-1),(0,1)]:
        o.paste(Image.new('RGBA',im.size,col),(dx,dy),a)
    o.alpha_composite(im); return o
def padded(im,p): o=Image.new('RGBA',(im.width+2*p,im.height+2*p),(0,0,0,0)); o.alpha_composite(im,(p,p)); return o
f=ImageFont.truetype(F,12); D=ImageDraw.Draw(Image.new('RGBA',(1,1)))
def text(s,col):
    bb=D.textbbox((0,0),s,font=f); im=Image.new('RGBA',(bb[2]+2,bb[3]+2),(0,0,0,0))
    ImageDraw.Draw(im).text((1,1),s,font=f,fill=col); return im.crop(im.getbbox())
def block(c1,c2):
    w1=text("Wintery",c1); ws=[D.textbbox((0,0),c,font=f)[2] for c in "STUDIO"]; gap=(w1.width-sum(ws))/5
    st=Image.new('RGBA',(w1.width+6,14),(0,0,0,0)); x=0
    for c,cw in zip("STUDIO",ws): ImageDraw.Draw(st).text((round(x),0),c,font=f,fill=c2); x+=cw+gap
    st=st.crop(st.getbbox()); b=Image.new('RGBA',(max(w1.width,st.width),w1.height+3+st.height),(0,0,0,0))
    b.alpha_composite(w1,(0,0)); b.alpha_composite(st,(0,w1.height+3)); return b
res={}
import numpy as np
from scipy.ndimage import label, binary_fill_holes
for theme in ('light','dark'):
    art=padded(src,2)
    if theme=='dark':   # 어두운 배경: 고양이에만 연한 테두리(발자국·눈은 그대로)
        import numpy as np; from scipy.ndimage import label
        a=np.array(art); dark=(a[...,3]>0)&(a[...,:3].sum(2)<260)      # 짙은 외곽선 = 고양이
        lab,n=label(dark,structure=np.ones((3,3))); big=np.argmax(np.bincount(lab.ravel())[1:])+1
        from scipy.ndimage import binary_fill_holes
        catmask=binary_fill_holes(lab==big)
        m=Image.fromarray((catmask*255).astype('uint8'))
        cat=Image.new('RGBA',art.size,(0,0,0,0)); cat.paste(art,(0,0),m)
        ring=outline(cat,PALE); art=art.copy(); art.alpha_composite(ring)
    tc1,tc2=(INK,BLUE) if theme=='light' else (WHITE,BLUE)
    # 가로형: 글자는 발자국 길 위(왼쪽 위)
    blk=block(tc1,tc2)
    import numpy as np
    top=6; A=Image.new('RGBA',(art.width,art.height+top),(0,0,0,0)); A.alpha_composite(art,(0,top))
    occ=np.array(A.split()[3])>0
    from scipy.ndimage import binary_dilation
    occ=binary_dilation(occ,iterations=3)
    bm=np.array(blk.split()[3])>0; best=None
    for y in range(0,A.height-blk.height+1):
        for x in range(0,A.width-90-blk.width):
            if not (occ[y:y+blk.height,x:x+blk.width]&bm).any():
                score=(A.height-y)*2+abs((x+blk.width)-(A.width-92))   # 아래쪽·고양이 가까이
                if best is None or score<best[0]: best=(score,x,y)
    A.alpha_composite(blk,(best[1],best[2]))
    A=padded(A.crop(A.getbbox()),3)
    # 세로형: 그림 아래 한 줄
    one=text("Wintery Studio",tc1)
    B=Image.new('RGBA',(art.width,art.height+one.height+4),(0,0,0,0)); B.alpha_composite(art,(0,0)); B.alpha_composite(one,((art.width-one.width)//2,art.height+3))
    B=padded(B.crop(B.getbbox()),3)
    # 아이콘
    ic=padded(Image.open('cat_icon_clean_1x.png').crop(Image.open('cat_icon_clean_1x.png').getbbox()),2)
    if theme=='dark':
        a2=np.array(ic); dk=(a2[...,3]>0)&(a2[...,:3].sum(2)<260); lb,_=label(dk,structure=np.ones((3,3)))
        cm=binary_fill_holes(lb==np.argmax(np.bincount(lb.ravel())[1:])+1)
        cc=Image.new('RGBA',ic.size,(0,0,0,0)); cc.paste(ic,(0,0),Image.fromarray((cm*255).astype('uint8'))); ic=ic.copy(); ic.alpha_composite(outline(cc,PALE))
    cat=ic; s=max(cat.size)+6
    C=Image.new('RGBA',(s,s),(0,0,0,0)); C.alpha_composite(cat,((s-cat.width)//2,(s-cat.height)//2))
    for k,im in (('wide',A),('stacked',B),('icon',C)):
        res[(theme,k)]=im
        im.save(f'out_{theme}_{k}_1x.png')
        for m in (4,8): im.resize((im.width*m,im.height*m),Image.NEAREST).save(f'out_{theme}_{k}_x{m}.png')
    A=res[(theme,'wide')]; m=min(1200//A.width,680//A.height); big=A.resize((A.width*m,A.height*m),Image.NEAREST)
    L=Image.new('RGBA',(1280,720),(0,0,0,0)); L.alpha_composite(big,((1280-big.width)//2,(720-big.height)//2)); L.save(f'out_{theme}_library_logo_1280x720.png')
pv=Image.new('RGBA',(1300,1000),(255,246,224,255)); ImageDraw.Draw(pv).rectangle((0,500,1300,1000),fill=(40,44,66,255))
for i,theme in enumerate(('light','dark')):
    y=500*i+20
    a=res[(theme,'wide')]; a=a.resize((a.width*4,a.height*4),Image.NEAREST); pv.alpha_composite(a,(20,y))
    b=res[(theme,'stacked')]; b=b.resize((b.width*2,b.height*2),Image.NEAREST); pv.alpha_composite(b,(20,y+a.height+20))
    c=res[(theme,'icon')]; c=c.resize((c.width*3,c.height*3),Image.NEAREST); pv.alpha_composite(c,(1300-c.width-20,y+a.height+10))
pv.convert('RGB').save('preview2.png')
