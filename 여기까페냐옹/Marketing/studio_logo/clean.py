from PIL import Image;import numpy as np,sys
P=float(sys.argv[1]) if len(sys.argv)>1 else 8.2
a=np.array(Image.open('src.png').convert('RGBA')).astype(float)
H,W=a.shape[:2]; nw,nh=int(W/P),int(H/P)
out=np.zeros((nh,nw,4))
for j in range(nh):
  for i in range(nw):
    y0,y1=int(j*P+P*0.25),int(j*P+P*0.75)+1; x0,x1=int(i*P+P*0.25),int(i*P+P*0.75)+1
    c=a[y0:y1,x0:x1].reshape(-1,4); out[j,i]=np.median(c,0)
# palette via kmeans on opaque
op=out[...,3]>110
px=out[op][:,:3]
from sklearn.cluster import KMeans
km=KMeans(9,n_init=10,random_state=0).fit(px)
pal=km.cluster_centers_
print(np.round(pal).astype(int))
q=np.zeros((nh,nw,4),np.uint8)
q[op,:3]=pal[km.predict(px)].round(); q[op,3]=255
Image.fromarray(q).save('clean.png'); print(nw,nh)
Image.fromarray(q).resize((nw*6,nh*6),Image.NEAREST).save('clean_x6.png')
