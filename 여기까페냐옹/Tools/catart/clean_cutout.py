import sys, numpy as np
from PIL import Image
from scipy import ndimage as nd
def clean(im, peel=2, min_blob=4, outline=(52,34,30)):
    a=np.array(im.convert('RGBA')).astype(np.int32); A=a[...,3]>0
    rgb=a[...,:3]; lum=rgb.mean(-1)
    # 1) 떨어진 점 제거
    lab,n=nd.label(A, structure=np.ones((3,3)))
    sizes=nd.sum(A,lab,range(1,n+1))
    for i,s in enumerate(sizes,1):
        if s<min_blob: A[lab==i]=False
    # 2) 바깥 테두리의 밝은 픽셀(배경 찌꺼기) 벗기기 — 어두운 외곽선 바깥쪽에 붙은 것만
    for _ in range(peel):
        edge=A & ~nd.binary_erosion(A, structure=np.ones((3,3)))
        light=edge & (lum>110)
        # 안쪽 이웃에 어두운 선이 있을 때만(=외곽선 바깥 찌꺼기)
        dark=(lum<90)&A
        near_dark=nd.binary_dilation(dark, structure=np.ones((3,3)))
        A[light & near_dark]=False
    # 3) 윤곽 다듬기(1px 구멍·가시 정리)
    A=nd.binary_closing(A, structure=np.ones((2,2))) & (A | nd.binary_dilation(A))
    lab,n=nd.label(A, structure=np.ones((3,3))); sizes=nd.sum(A,lab,range(1,n+1))
    for i,s in enumerate(sizes,1):
        if s<min_blob: A[lab==i]=False
    # 4) 바깥 1px을 외곽선 색으로 통일(밝은 픽셀만 바꿈)
    edge=A & ~nd.binary_erosion(A, structure=np.ones((3,3)))
    lab2,n2=nd.label(A, structure=np.ones((3,3))); sz2=nd.sum(A,lab2,range(1,n2+1))
    small=np.isin(lab2,[i+1 for i,s in enumerate(sz2) if s<300])
    edge&=~small
    out=a.copy(); out[...,3]=np.where(A,255,0)
    m=edge & (lum>90)
    out[m,0],out[m,1],out[m,2]=outline
    out[~A]=0
    return Image.fromarray(out.astype(np.uint8),'RGBA')
if __name__=='__main__':
    clean(Image.open(sys.argv[1])).save(sys.argv[2])
