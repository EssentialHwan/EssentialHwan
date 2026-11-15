from PIL import Image;import numpy as np
a=np.array(Image.open('clean2.png')).copy()
H=(48,55,94); I=(253,253,253); E=(193,216,243); A=(135,173,222)
def put(pts,c):
    for y,x in pts:
        if c is None: a[y,x,3]=0
        else: a[y,x]=(*c,255)
# 외곽선 끊김·잡티 (y,x)
put([(2,55)],H); put([(2,54)],I)
put([(6,45),(11,31),(13,32),(14,33),(15,33),(15,37),(16,32),(16,38),(16,40),(16,41),(17,39),(18,42),(19,41),(12,58)],I)
put([(12,37),(13,35),(13,36),(24,7),(25,7)],None)
put([(17,30),(17,31),(20,30),(22,29)],E)
rgb=a[...,:3].astype(int); op=a[...,3]>0
# 발자국: 고양이 왼쪽(x<24)의 파랑(A,B) 지우고 간격 맞춰 다시
B=(162,186,220)
for y in range(a.shape[0]):
    for x in range(0,26):
        if op[y,x] and tuple(rgb[y,x]) in (A,B): a[y,x,3]=0
# 눈 조각 외톨이 정리
op=a[...,3]>0
for y,x in zip(*np.where(op)):
    if op[max(0,y-1):y+2,max(0,x-1):x+2].sum()<=1: a[y,x,3]=0
UP=["..#.#..","#.....#","..###..",".#####.",".#####.","..###.."]
m=np.rot90(np.array([[c=='#' for c in r] for r in UP]),-1); h,w=m.shape
for cx,cy in ((3,14),(11,20),(19,25)):
    for yy,xx in zip(*np.where(m)): a[cy-h//2+yy,cx-w//2+xx]=(*A,255)
Image.fromarray(a).save('icon_touch.png')
