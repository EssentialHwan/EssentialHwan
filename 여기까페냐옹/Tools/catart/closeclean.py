# 11/12 마감 클로즈업(252x192) AI 느낌 줄이기 — 원본은 Tools/catart/backup_closeup_1112
# 사용: python3 closeclean.py <cat> <src 폴더(.bytes)> <dst 폴더> [색 수=16]   (dst 확장자는 원본과 같게 .bytes 로 저장)
# 팔레트 축소 → 실루엣 가시·구멍 정리 → 내부 노이즈 다수결 → 1px 외곽선 통일
import sys, numpy as np
from PIL import Image
def kmeans(X, k, it=25, seed=0):
    r=np.random.default_rng(seed); C=X[r.choice(len(X),k,replace=False)].astype(float)
    for _ in range(it):
        d=((X[:,None,:]-C[None])**2).sum(-1); l=d.argmin(1)
        for j in range(k):
            m=X[l==j]
            if len(m): C[j]=m.mean(0)
    return C
N4=((1,0),(-1,0),(0,1),(0,-1)); N8=N4+((1,1),(1,-1),(-1,1),(-1,-1))
def shift(m,dy,dx,fill=False):
    H,W=m.shape; o=np.full_like(m,fill)
    o[max(dy,0):H+min(dy,0), max(dx,0):W+min(dx,0)] = m[max(-dy,0):H+min(-dy,0), max(-dx,0):W+min(-dx,0)]
    return o
def smooth_mask(a, r=3.5):
    # 알파를 반경 r로 흐린 뒤 반으로 자름 = 털 가시(2~3px)까지 둥글게 다듬은 실루엣
    from PIL import ImageFilter
    m=Image.fromarray((a*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(r))
    return np.array(m)>=128
def clean_set(paths, outs, k=16):
    ims=[np.array(Image.open(p).convert('RGBA')) for p in paths]
    X=np.concatenate([im[im[...,3]>0][:,:3] for im in ims]).astype(float)
    C=kmeans(X[np.random.default_rng(1).choice(len(X),min(12000,len(X)),replace=False)],k)
    lum=C@np.array([.3,.59,.11]); line=C[lum.argmin()]
    for im,o in zip(ims,outs):
        a=im[...,3]>0; H,W=a.shape
        rgb=im[...,:3].astype(float)
        lbl=((rgb.reshape(-1,1,3)-C[None])**2).sum(-1).argmin(1).reshape(H,W)
        a2=smooth_mask(a)
        # 새로 메운 칸은 이웃 라벨로
        for _ in range(4):
            for dy,dx in N4:
                fill=a2&~a&shift(a,dy,dx); src=shift(lbl,dy,dx,0); lbl[fill]=src[fill]
            a=a|(a2&~a)
        a=a2
        # 내부: 색 영역 경계를 둥글게(라벨별 가우시안 → 최댓값). 눈·입·하이라이트(가장 어둡거나 밝은 색)는 원래대로 보존
        from PIL import ImageFilter
        order=np.argsort(lum); keep=set(order[:3])|set(order[-1:])
        stack=np.stack([np.array(Image.fromarray(((lbl==i)&a).astype(np.uint8)*255).filter(ImageFilter.GaussianBlur(1.6))).astype(float) for i in range(len(C))])
        L2=stack.argmax(0)
        prot=np.isin(lbl,list(keep))&a
        # 가는 선(입·수염·눈썹): 주변보다 확 어두운 칸은 원래 라벨 유지
        Y=rgb@np.array([.3,.59,.11])
        Yb=np.array(Image.fromarray(np.clip(Y,0,255).astype(np.uint8)).filter(ImageFilter.BoxBlur(2))).astype(float)
        prot|=(Y<Yb-35)&a
        # 작은 덩어리(입·코·속눈썹 등 5~120px)는 지우지 않는다. 1~4px 점 노이즈만 정리
        from scipy import ndimage
        for i in range(len(C)):
            cc,n=ndimage.label((lbl==i)&a)
            if n==0: continue
            sz=ndimage.sum(np.ones_like(cc),cc,range(1,n+1))
            small=np.isin(cc,1+np.where((sz>=5)&(sz<=120))[0])
            prot|=small
        L2[prot]=lbl[prot]
        out=C[L2]
        # 팔레트에 없는 포인트 색(노란 눈 등): 원래 색 그대로
        err=np.sqrt(((rgb-C[lbl])**2).sum(-1)); far=(err>40)&im[...,3].astype(bool)&a
        out[far]=rgb[far]
        # 1px 외곽선: 실루엣 가장자리 전부 외곽선 색
        edge=a & ~(shift(a,1,0)&shift(a,-1,0)&shift(a,0,1)&shift(a,0,-1))
        out[edge]=line
        res=np.zeros((H,W,4),np.uint8); res[...,:3]=np.clip(out,0,255); res[...,3]=np.where(a,255,0)
        Image.fromarray(res).save(o,format='PNG')
if __name__=='__main__':
    cat=sys.argv[1]; src=sys.argv[2]; dst=sys.argv[3]; k=int(sys.argv[4]) if len(sys.argv)>4 else 16
    ex=['open','half','closed','happy','wide','angry']
    clean_set([f'{src}/close_cat_{cat}_{e}.bytes' for e in ex],[f'{dst}/close_cat_{cat}_{e}.bytes' for e in ex],k)
