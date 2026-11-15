# 발자국을 고양이 진행 방향(오른쪽)으로 다시 찍는다: 발가락이 오른쪽, 좌우 발이 번갈아 위아래로 살짝 엇갈리게
from PIL import Image;import numpy as np,sys
from scipy.ndimage import label
src,out=sys.argv[1],sys.argv[2]
E=int(sys.argv[3]) if len(sys.argv)>3 else 1
a=np.array(Image.open(src).convert('RGBA'))
rgb=a[...,:3].astype(int); op=a[...,3]>0
# 고양이 = 가장 큰 짙은 외곽선 덩어리
dark=op&(rgb.sum(2)<300); lab,_=label(dark,structure=np.ones((3,3)))
cat=lab==np.argmax(np.bincount(lab.ravel())[1:])+1; cx0=np.where(cat)[1].min()
# 발자국 색 = 채도 높은 중간 파랑(가장 진한 파랑 계열)
cols,cnt=np.unique(rgb[op].reshape(-1,3),axis=0,return_counts=True)
blue=[tuple(c) for c in cols if c[2]-c[0]>60 and 120<c[0]<175]
pc=max(blue,key=lambda c:c[2]-c[0]); print('print color',pc)
ink=op&(rgb[...,0]<180)&(rgb[...,2]-rgb[...,0]>40)&(rgb.sum(2)>300)   # 발자국 계열(눈보다 진한 파랑)
pm=ink.copy(); pm[:,cx0-1:]=False
from scipy.ndimage import binary_dilation
lab,n=label(binary_dilation(pm,iterations=1),structure=np.ones((3,3))); lab=lab*pm; prints=[]
for k in range(1,n+1):
    ys,xs=np.where(lab==k)
    if len(ys)<5: continue
    prints.append(((ys.min()+ys.max())/2,(xs.min()+xs.max())/2,max(np.ptp(ys),np.ptp(xs))+1,ys,xs))
prints.sort(key=lambda p:p[1]); print(len(prints),[p[2] for p in prints])
SMALL=["#.#..","...#.","##..#","##.#.","...#.","#.#.."]   # unused
UP_S=[".#.#.","#...#",".###.",".###."]
UP_L=["..#.#..","#.....#","..###..",".#####.",".#####.","..###.."]
def stamp(t):
    m=np.array([[c=='#' for c in r] for r in t]); return np.rot90(m,-1)   # 발가락 위 → 오른쪽
for i,(cy,cx,sz,ys,xs) in enumerate(prints):
    sl=(slice(max(0,ys.min()-E),ys.max()+E+1),slice(max(0,xs.min()-E),min(xs.max()+E+1,cx0-1)))
    kill=ink[sl] if E<=1 else op[sl]&~dark[sl]
    sub=a[sl]; sub[kill,3]=0; a[sl]=sub
    m=stamp(UP_L if np.median([p[2] for p in prints])>=6 else UP_S); h,w=m.shape
    dy=-1 if i%2==0 else 1
    y0=int(round(cy-h/2))+dy; x0=int(round(cx-w/2))
    for yy,xx in zip(*np.where(m)):
        Y,X=y0+yy,x0+xx
        if 0<=Y<a.shape[0] and 0<=X<a.shape[1]: a[Y,X]=(*pc,255)
Image.fromarray(a).save(out)
