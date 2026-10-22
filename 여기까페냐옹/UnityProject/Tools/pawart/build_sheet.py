# 받은 원화로 4x3 앞발 시트 조립 (칸 258x240, 팔뚝은 칸 바닥에 같은 폭·높이로)
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
CW,CH=258,240

def extract(path, minarea=2000):
    im=Image.open(path).convert('RGBA'); a=np.array(im).astype(int)
    rgb=a[...,:3]; mn=rgb.min(-1); mx=rgb.max(-1)
    bgish=(mn>=247)&((mx-mn)<=5)                 # 흰 배경(털은 푸른빛이 빠진 크림색이라 걸러짐)
    lab,n=ndi.label(bgish)
    border=set(np.unique(np.concatenate([lab[0],lab[-1],lab[:,0],lab[:,-1]])))-{0}
    bg=np.isin(lab,list(border))
    fg=~bg
    fg=ndi.binary_opening(fg,iterations=1)
    lab2,n2=ndi.label(fg)
    out=[]
    for i,sl in enumerate(ndi.find_objects(lab2),1):
        ys,xs=sl; area=(lab2[sl]==i).sum()
        if area<minarea: continue
        piece=a[sl].copy().astype(np.uint8); m=(lab2[sl]==i)
        piece[...,3]=np.where(m,255,0)
        out.append((xs.start, Image.fromarray(piece,'RGBA')))
    out.sort(key=lambda t:t[0])
    return [p for _,p in out]

def forearm(p):
    a=np.array(p)[...,3]>0
    rows=[r for r in range(a.shape[0]-1,-1,-1) if a[r].any()]
    r=rows[min(6,len(rows)-1)]; xs=np.where(a[r])[0]
    return xs[-1]-xs[0]+1, (xs[0]+xs[-1])/2

def place(p, target_w, scale_extra=1.0):
    w,cx=forearm(p)
    k=target_w/w*scale_extra
    q=p.resize((max(1,round(p.width*k)),max(1,round(p.height*k))),Image.LANCZOS)
    # 알파를 딱 잘라 도트 느낌 유지
    arr=np.array(q); arr[...,3]=np.where(arr[...,3]>110,255,0); q=Image.fromarray(arr,'RGBA')
    cell=Image.new('RGBA',(CW,CH),(0,0,0,0))
    # 아래 끝을 칸 바닥에(잘린 팔뚝 아래쪽은 칸 밖으로 살짝 내려가게)
    x=round(CW/2 - cx*k); y=CH-q.height+2
    cell.alpha_composite(q,(max(-q.width,x),y)) if x>=0 else cell.paste(q,(x,y),q)
    return cell

back=extract('back4.png',800)
gro=extract('groom3.png',20000)
pa=extract('palmA.png',500)[0]; pb=extract('palmB.png',500)[0]
print(len(back),len(gro))
TW=118   # 팔뚝 폭(칸 258 중) — 예전 시트와 비슷한 화면 크기
cells=[place(b,TW) for b in back[:4]]
g4=place(gro[2],TW); cells+= [g4, place(gro[0],TW), place(gro[1],TW), place(gro[2],TW)]
small=0.82
cells+= [place(pa,TW,small), place(pb,TW,small), place(pb,TW,small), place(pb,TW,small)]

# 효과 얹기(3px 도트)
def dots(cell, pts, col, ox, oy, s=3):
    for (x,y) in pts:
        for dx in range(s):
            for dy in range(s): cell.putpixel((ox+x*s+dx, oy+y*s+dy), col)
def sparkle(cell, ox, oy):
    pts=[(2,0),(2,1),(0,2),(1,2),(2,2),(3,2),(4,2),(2,3),(2,4)]
    dots(cell,pts,(255,236,170,255),ox,oy); dots(cell,[(2,2)],(255,255,255,255),ox,oy)
def heart(cell, ox, oy):
    rows=["01100110","11111111","11111111","01111110","00111100","00011000"]
    pts=[(i,j) for j,r in enumerate(rows) for i,c in enumerate(r) if c=='1']
    dots(cell,pts,(240,130,155,255),ox,oy)
def wave(cell, ox, oy):
    pts=[(0,0),(1,1),(1,2),(1,3),(0,4)]
    dots(cell,pts,(120,70,60,255),ox,oy); dots(cell,[(x+3,y-1) for x,y in pts],(120,70,60,255),ox,oy)
sparkle(cells[7],172,62); sparkle(cells[7],186,98)
heart(cells[10],150,58)
wave(cells[9],170,84); wave(cells[11],176,78)

sheet=Image.new('RGBA',(CW*4,CH*3),(0,0,0,0))
for i,c in enumerate(cells): sheet.alpha_composite(c,((i%4)*CW,(i//4)*CH))
sheet.save('paws_v5.png')
bgv=Image.new('RGBA',sheet.size,(90,90,110,255)); bgv.alpha_composite(sheet); bgv.convert('RGB').save('preview.png')
print(sheet.size)
