from PIL import Image;import numpy as np,sys
from scipy.ndimage import label
src,P,out=sys.argv[1],float(sys.argv[2]),sys.argv[3]
OX=float(sys.argv[4]) if len(sys.argv)>4 else 0; OY=float(sys.argv[5]) if len(sys.argv)>5 else 0
a=np.array(Image.open(src).convert('RGBA')).astype(float)
H,W=a.shape[:2]; nw,nh=int((W-OX)/P),int((H-OY)/P); o=np.zeros((nh,nw,4))
for j in range(nh):
  for i in range(nw):
    y0,y1=int(OY+j*P+P*0.3),int(OY+j*P+P*0.7)+1; x0,x1=int(OX+i*P+P*0.3),int(OX+i*P+P*0.7)+1
    o[j,i]=np.median(a[y0:y1,x0:x1].reshape(-1,4),0)
# 흰 배경 → 투명 (가장자리와 이어진 거의 흰 칸만)
bright=(o[...,:3].min(2)>238)&(o[...,3]>0)
lab,_=label(bright); edge=set(lab[0])|set(lab[-1])|set(lab[:,0])|set(lab[:,-1]); edge.discard(0)
bg=np.isin(lab,list(edge)); o[bg,3]=0
op=o[...,3]>110; px=o[op][:,:3]
from sklearn.cluster import KMeans
km=KMeans(9,n_init=10,random_state=0).fit(px); pal=km.cluster_centers_
q=np.zeros((nh,nw,4),np.uint8); q[op,:3]=pal[km.predict(px)].round(); q[op,3]=255
im=Image.fromarray(q); im=im.crop(im.getbbox()); im.save(out); print(im.size); print(np.round(pal).astype(int))
