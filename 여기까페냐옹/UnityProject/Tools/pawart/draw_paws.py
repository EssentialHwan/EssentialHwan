# 4발가락 1인칭 앞발 도트 시트 생성: 4x3 칸, 칸 86x80 → x3 확대 = 1032x720
import math, random
from PIL import Image
W,H=86,80; S=3
OUT=(92,50,42,255)
FUR=(255,247,230,255); FUR_SH=(238,220,196,255); FUR_DK=(220,196,170,255); FUR_HI=(255,255,250,255)
ORG=(236,162,92,255); ORG_SH=(214,134,70,255); ORG_ST=(196,112,58,255)
PAD=(242,134,156,255); PAD_SH=(214,98,128,255); PAD_HI=(255,190,205,255)
TONG=(240,110,130,255); TONG_SH=(200,80,105,255)

def blank(): return [[None]*W for _ in range(H)]
def inell(x,y,cx,cy,rx,ry): return ((x-cx)/rx)**2+((y-cy)/ry)**2<=1.0

class Paw:
    def __init__(s, seed=1): s.m=[[0]*W for _ in range(H)]; s.c=blank(); s.rng=random.Random(seed)
    def ell(s,cx,cy,rx,ry,val=1):
        for y in range(H):
            for x in range(W):
                if inell(x+.5,y+.5,cx,cy,rx,ry): s.m[y][x]=val
    def rect(s,x0,y0,x1,y1,val=1):
        for y in range(max(0,y0),min(H,y1+1)):
            for x in range(max(0,x0),min(W,x1+1)): s.m[y][x]=val
    def carve(s,cx,cy,rx,ry): s.ell(cx,cy,rx,ry,0)

def build(kind, f):
    """kind: back / palm. f: dict of params"""
    p=Paw(f.get('seed',1))
    cx=43+f.get('dx',0); top=f.get('top',22); wid=f.get('wid',23)   # 손바닥 반폭
    by=f.get('by',44)     # 손 몸통 중심 y
    # 팔뚝(아래로 갈수록 살짝 넓게)
    for y in range(by, H):
        t=(y-by)/(H-by); hw=int(wid*0.82+t*2)
        p.rect(cx-hw, y, cx+hw-1, y)
    p.ell(cx, by, wid, f.get('bry',17))
    # 발가락 4개: 두 안쪽이 살짝 높고, 바깥 둘이 낮게(사진 2)
    spread=f.get('spread',0); tr=f.get('tr',6.6); tlen=f.get('tlen',0)
    xs=[-17.0,-5.9,5.9,17.0]; ys=[top+5,top,top,top+5]
    toes=[]
    for i,(ox,oy) in enumerate(zip(xs,ys)):
        sx=ox*(1+spread*0.35); sy=oy - (spread*2 if i in (1,2) else spread*1) 
        ang=math.atan2(sx, 30)  # 퍼질 때 바깥으로 기울기
        tx=cx+sx; ty=sy
        if tlen: # 길쭉하게(퍼짐)
            p.ell(tx, ty+3, tr*0.95, tr+tlen)
        else:
            p.ell(tx, ty+2, tr, tr*f.get('tsq',1.0))
        toes.append((tx,ty+2))
    # 발가락 사이를 몸통과 이어 줌
    p.ell(cx, by-3, wid-1, max(f.get('bry',17)-3, by-3-(top+4)))   # 몸통이 발가락 뿌리까지 올라와 붙게
    return p, toes

def finish(p, kind, toes, f, extra=None):
    m=p.m; img=[[None]*W for _ in range(H)]
    cx=43+f.get('dx',0); by=f.get('by',44)
    sock=f.get('sock', 63)
    rng=random.Random(f.get('seed',1))
    # 털 들쭉날쭉(팔뚝 양옆)
    for y in range(by+4,H):
        row=m[y]; xs=[x for x in range(W) if row[x]]
        if not xs: continue
        l,r=xs[0],xs[-1]
        if rng.random()<0.35 and l>0: row[l-1]=1
        if rng.random()<0.35 and r<W-1: row[r+1]=1
        if rng.random()<0.25: row[l]=0
        if rng.random()<0.25: row[r]=0
    for y in range(H):
        for x in range(W):
            if not m[y][x]: continue
            # 기본 털색: 아래(팔뚝)는 주황 태비, 위는 흰 양말 — 경계는 지그재그
            edge=sock + (2 if (x//3)%2 else 0) + (1 if (x//5)%2 else -1)
            if y>=edge:
                c=ORG
                if (y + (x - cx)//6) % 6 == 0: c=ORG_ST
                xs_=[xx for xx in range(W) if m[y][xx]]
                if xs_ and x>=xs_[-1]-3: c=ORG_SH if c==ORG else ORG_ST
            else:
                c=FUR
                xs_=[xx for xx in range(W) if m[y][xx]]
                if xs_:
                    r=xs_[-1]; l=xs_[0]
                    if x>=r-1: c=FUR_DK
                    elif x>=r-4: c=FUR_SH
                    elif x<=l: c=FUR_SH
                if y-1>=0 and not m[y-1][x] and c==FUR: c=FUR_HI
            img[y][x]=c
    # 발가락 하이라이트·주름(손등)
    if kind=='back':
        for (tx,ty) in toes:
            for dy in range(-4,-1):
                for dx in range(-3,0):
                    x,y=int(tx+dx),int(ty+dy)
                    if 0<=x<W and 0<=y<H and m[y][x] and img[y][x]==FUR: img[y][x]=FUR_HI
        # 발가락 사이 주름 3줄(사진 2의 털 골)
        for i in range(3):
            ax=(toes[i][0]+toes[i+1][0])/2; ay=max(toes[i][1],toes[i+1][1])-4
            ln=f.get('crease',6)
            x=int(ax)
            for k in range(ln+3):
                y=int(ay+k)
                if 0<=y<H and m[y][x]: img[y][x]=OUT if k<2 else FUR_DK
        if f.get('beans_peek'):
            for (tx,ty) in toes:
                for (x,y) in [(int(tx)-1,int(ty)-6),(int(tx),int(ty)-6),(int(tx)-1,int(ty)-5),(int(tx),int(ty)-5)]:
                    if 0<=y<H and m[y][x]: img[y][x]=PAD
    else:  # palm: 큰 패드 + 콩 4개
        pc=f.get('padc',(cx, by+3)); pw=f.get('padw',13); ph=f.get('padh',10)
        for y in range(H):
            for x in range(W):
                if not m[y][x]: continue
                X,Y=x+.5,y+.5
                # 둥근 삼각형 + 아래 세 갈래
                inpad = inell(X,Y,pc[0],pc[1]-ph*0.15,pw*0.62,ph*0.7) \
                        or inell(X,Y,pc[0]-pw*0.58,pc[1]+ph*0.42,pw*0.42,ph*0.5) \
                        or inell(X,Y,pc[0]+pw*0.58,pc[1]+ph*0.42,pw*0.42,ph*0.5) \
                        or inell(X,Y,pc[0],pc[1]+ph*0.5,pw*0.44,ph*0.52) \
                        or (abs(X-pc[0])<pw*0.9 and pc[1]<=Y<=pc[1]+ph*0.5)
                if inpad: img[y][x]=PAD
        for (tx,ty) in toes:
            br=f.get('bean',4.2)
            for y in range(H):
                for x in range(W):
                    if m[y][x] and inell(x+.5,y+.5,tx,ty+2.2,br*0.88,br*1.05): img[y][x]=PAD
        # 패드 음영·하이라이트
        for y in range(H):
            for x in range(W):
                if img[y][x]==PAD:
                    below = y+1<H and img[y+1][x] not in (PAD,PAD_SH,PAD_HI)
                    right = x+1<W and img[y][x+1] not in (PAD,PAD_SH,PAD_HI)
                    if below or right: img[y][x]=PAD_SH
        for y in range(H):
            for x in range(W):
                if img[y][x]==PAD and y-1>=0 and x-1>=0 and img[y-1][x] not in (PAD,PAD_SH,PAD_HI) and img[y][x-1] not in (PAD,PAD_SH,PAD_HI):
                    img[y][x]=PAD_HI
    if extra: extra(img, m, f)
    # 외곽선
    out=[[None]*W for _ in range(H)]
    for y in range(H):
        for x in range(W):
            if img[y][x] is not None: out[y][x]=img[y][x]; continue
            if any(0<=x+dx<W and 0<=y+dy<H and img[y+dy][x+dx] is not None for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))):
                if y < H-1: out[y][x]=OUT
    return out

def wid_of(m,y):
    xs=[x for x in range(W) if m[y][x]]
    return (xs[-1]-xs[0])/2 if xs else 0

def to_img(cells):
    sheet=Image.new('RGBA',(W*4,H*3),(0,0,0,0))
    for i,c in enumerate(cells):
        ox,oy=(i%4)*W,(i//4)*H
        for y in range(H):
            for x in range(W):
                if c[y][x] is not None: sheet.putpixel((ox+x,oy+y),c[y][x])
    return sheet.resize((W*4*S,H*3*S),Image.NEAREST)

# ── 효과(혀·반짝·하트·젖은 털) ──
def tongue(level):
    def f(img,m,fr):
        cx=43; base=H-1; h=[10,18,24][level]; w=8
        for y in range(base-h, base+1):
            for x in range(cx-w-6, cx+w-6):
                X,Y=x+.5,y+.5
                if inell(X,Y,cx-6,base,w,h+1):
                    edge = not inell(X,Y,cx-6,base,w-1.3,h-0.3)
                    img[y][x]=OUT if edge else (TONG_SH if (x==cx-6 or X>cx-6+w-3) else TONG)
    return f
def sparkle(img,m,f):
    for (sx,sy) in [(74,14),(78,26)]:
        for d in range(-3,4):
            for (x,y) in [(sx+d,sy),(sx,sy+d)]:
                if 0<=x<W and 0<=y<H: img[y][x]=FUR_HI if abs(d)<2 else (255,236,170,255)
def heart(img,m,f):
    pts=["01100110","11111111","11111111","01111110","00111100","00011000"]
    for j,row in enumerate(pts):
        for i,ch in enumerate(row):
            if ch=='1': img[3+j][58+i]=(250,160,180,255)
def wavy(img,m,f):
    for k,(x0,y0) in enumerate([(70,14),(75,12)]):
        for d in range(7):
            x=x0+(1 if d in (2,3,4) else 0); y=y0+d
            img[y][x]=(255,250,240,255)
def ruffle(img,m,f):
    rng=random.Random(5)
    for y in range(H):
        for x in range(W):
            if m[y][x] and y<46:
                for dx,dy in ((1,0),(-1,0),(0,-1)):
                    X,Y=x+dx,y+dy
                    if 0<=X<W and 0<=Y<H and not m[Y][X] and rng.random()<0.18:
                        img[Y][X]=FUR

frames=[]
def add(kind, extra=None, **f):
    p,toes=build(kind,f)
    frames.append(finish(p,kind,toes,f,extra))

# 1줄 손등
add('back', top=22)                                                    # 0 기본
add('back', top=30, wid=27, bry=14, by=48, tsq=0.85, crease=4)       # 1 누름(납작·넓게)
add('back', top=20, spread=1.0, tlen=3, beans_peek=True, crease=8)   # 2 꾹꾹(폄)
add('back', top=28, wid=22, tr=6.6, tsq=0.8, crease=3, by=47)        # 3 꾹꾹(오므림)
# 2줄 그루밍(손바닥이 보임)
add('palm', top=20)                                                   # 4 들어올림
add('palm', top=20, extra=tongue(0))                                  # 5 혀 닿음
add('palm', top=19, extra=lambda i,m,f:(tongue(2)(i,m,f), ruffle(i,m,f)))   # 6 핥기
add('palm', top=20, extra=lambda i,m,f:(ruffle(i,m,f), sparkle(i,m,f)))     # 7 젖은 털 반짝
# 3줄 쭙쭙이(작게, 손바닥)
small=dict(top=34, wid=19, bry=13, by=55, tr=5.6, bean=3.4, padw=10, padh=7.5, sock=74)
add('palm', **dict(small, tsq=0.8))                                   # 8 오므림
add('palm', extra=wavy, **dict(small, spread=0.3))                    # 9 폄
add('palm', extra=heart, **dict(small, spread=0.3))                   # 10 하트
add('palm', extra=wavy, **dict(small, spread=0.35, seed=3))            # 11 폄(반복)

sheet=to_img(frames)
sheet.save('paws_new.png')
bg=Image.new('RGBA',sheet.size,(90,90,110,255)); bg.alpha_composite(sheet); bg.convert('RGB').save('preview.png')
print(sheet.size)
