import numpy as np
from PIL import Image
from scipy import ndimage
from seg import fg_mask
import convert as C, convert2 as C2, build_v2 as V
a,fg0=fg_mask('black_v3_all_src.png')
fg=ndimage.binary_closing(fg0,iterations=2)
lab,n=ndimage.label(fg)
objs=[]
for i,sl in enumerate(ndimage.find_objects(lab),1):
    m=(lab==i)&fg0
    if m.sum()<400: continue
    ys,xs=np.nonzero(m); objs.append(((ys.min()+ys.max())/2,xs.min(),m))
objs.sort(key=lambda o:o[0]); rows=[]
for o in objs:
    if rows and abs(o[0]-rows[-1][0][0])<35: rows[-1].append(o)
    else: rows.append([o])
rows=[sorted(r,key=lambda o:o[1]) for r in rows]
masks=[o[2] for r in rows for o in r]
print(len(masks))
allfg=np.zeros(fg0.shape,bool)
for m in masks: allfg|=m
pal,_=C.quantize(a,allfg,64); pal=np.vstack([pal,V.accent_palette(a,allfg)]).astype(np.float32)
def bb(m): ys,xs=np.nonzero(m); return xs.max()-xs.min()+1, ys.max()-ys.min()+1
# 기준: 시트A 앉기 높이 → 42px, 걷기 몸길이 → 46px
sitH=bb(masks[0])[1]; s0=42.0/sitH
idx={'A':list(range(0,16)),'walk8x2':list(range(16,32)),'run':list(range(32,36)),'idle':list(range(36,40)),'C':list(range(40,56))}
def sc_for(group):
    if group=='walk8x2': return 46.0/np.median([bb(masks[i])[0] for i in idx[group]])
    if group=='run': return s0*np.median([bb(masks[i])[0] for i in idx['A'][4:8]])/np.median([bb(masks[i])[0] for i in idx['run']])*1.0
    if group=='idle': return 42.0/np.median([bb(masks[i])[1] for i in idx['idle']])
    if group=='C': return 42.0/np.median([bb(masks[i])[1] for i in idx['C'][0:4]])*0.95
    return s0
frames={}
for g,ii in idx.items():
    s=sc_for(g); print(g,'scale',round(s,3))
    for i in ii: frames[i]=C2.down2(a,pal,masks[i],s)
def cell(fr,F=56):
    c=Image.new('RGBA',(F,F),(0,0,0,0)); h,w=fr.shape[:2]; c.alpha_composite(Image.fromarray(fr),((F-w)//2,55-h)); return c
def gif(ids,name,fps,k=4):
    ims=[]
    for i in ids:
        bg=Image.new('RGBA',(56,56),(214,190,150,255)); bg.alpha_composite(cell(frames[i])); ims.append(bg.resize((56*k,56*k),Image.NEAREST).convert('P',palette=Image.ADAPTIVE))
    ims[0].save(name,save_all=True,append_images=ims[1:],duration=int(1000/fps),loop=0)
gif(range(16,32),'v3_walk16.gif',11); gif(range(16,24),'v3_walk8a.gif',11); gif([4,5,6,7],'v3_walkA4.gif',8)
gif(range(32,36),'v3_run.gif',14); gif([0,36,0,37,0,38,0,39],'v3_sitidle.gif',4); gif(range(40,44),'v3_groom.gif',6); gif([46,47],'v3_knead.gif',4); gif([48,49],'v3_sleep.gif',2); gif(range(52,56),'v3_pounce.gif',8)
sheet=Image.new('RGBA',(56*8,56*7),(214,190,150,255))
for j,i in enumerate(range(56)): sheet.alpha_composite(cell(frames[i]),((j%8)*56,(j//8)*56))
sheet.resize((sheet.width*2,sheet.height*2),Image.NEAREST).save('v3_all56.png')
import pickle; pickle.dump(frames,open('v3_frames.pkl','wb'))
