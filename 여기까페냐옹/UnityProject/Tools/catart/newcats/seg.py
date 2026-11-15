import numpy as np
from PIL import Image
from scipy import ndimage
def fg_mask(path):
    a=np.array(Image.open(path).convert('RGB')).astype(int)
    lum=a.mean(2); sat=a.max(2)-a.min(2)
    light=(lum>222)&(sat<14)
    lab,n=ndimage.label(light)
    border=set(np.unique(np.concatenate([lab[0],lab[-1],lab[:,0],lab[:,-1]])))-{0}
    bg=np.isin(lab,list(border))
    fg=~bg
    fg=ndimage.binary_opening(fg,np.ones((3,3)))   # 체크무늬 잡티 제거
    return a,fg
if __name__=='__main__':
    for n in ['delivery','chef']:
        a,fg=fg_mask(f'{n}_src.png')
        out=np.zeros(a.shape[:2]+(4,),np.uint8); out[...,:3]=a; out[...,3]=fg*255
        Image.fromarray(out).save(f'{n}_cut.png')
        # 셀별 bbox
        H=a.shape[0]//4
        for r in range(4):
            print(n,r,[ (lambda ys,xs:(xs.max()-xs.min(),ys.max()-ys.min()) if len(xs) else None)(*np.where(fg[r*H:(r+1)*H,c*H:(c+1)*H])) for c in range(4)])
        # 원 도트 크기 추정: 가로로 같은 색이 이어지는 길이 분포
        row=a[600]; runs=[];cur=1
        for x in range(1,a.shape[1]):
            if np.abs(row[x]-row[x-1]).sum()<30: cur+=1
            else: runs.append(cur);cur=1
        from collections import Counter; print(Counter(runs).most_common(10))
