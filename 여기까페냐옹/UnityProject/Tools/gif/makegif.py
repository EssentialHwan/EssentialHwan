# python3 makegif.py <frames_dir> <out.gif> <start> <end> <x> <y> <w> <h> <outW> [fps]
import sys, glob, os, subprocess, tempfile
from PIL import Image
d,out,s,e,x,y,w,h,ow=sys.argv[1:10]; fps=int(sys.argv[10]) if len(sys.argv)>10 else 15
s,e,x,y,w,h,ow=map(int,(s,e,x,y,w,h,ow))
fs=sorted(glob.glob(os.path.join(d,'f_*.png')))[s:e]
oh=round(h*ow/w)
tmp=tempfile.mkdtemp()
for i,f in enumerate(fs):
    im=Image.open(f).convert('RGB').crop((x,y,x+w,y+h)).resize((ow,oh),Image.LANCZOS)
    im.save(os.path.join(tmp,f'{i:04d}.png'))
pal=os.path.join(tmp,'pal.png')
subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate',str(fps),'-i',os.path.join(tmp,'%04d.png'),'-vf','palettegen=max_colors=256:stats_mode=full',pal],check=True)
subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate',str(fps),'-i',os.path.join(tmp,'%04d.png'),'-i',pal,'-lavfi','paletteuse=dither=none','-loop','0',out],check=True)
print(out, len(fs),'frames', ow,'x',oh, os.path.getsize(out)//1024,'KB')
