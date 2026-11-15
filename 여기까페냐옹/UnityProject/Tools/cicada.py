# 여름 매미 환경음(직접 합성, 외부 샘플 없음): 멀리서 깔리는 말매미 합창 + 가끔 참매미 '맴맴맴~ 매앰'. 16초 심리스 루프.
import numpy as np, scipy.signal as sg, subprocess, wave
SR=32000; L=16.0; N=int(SR*L); rng=np.random.default_rng(3)
t=np.arange(N)/SR
def bp(x,lo,hi,o=4):
    b,a=sg.butter(o,[lo/(SR/2),hi/(SR/2)],'band'); return sg.lfilter(b,a,x)
out=np.zeros(N)
# 1) 말매미 합창(먼 곳): 띠대역 잡음 + 빠른 진동(버즈) + 느린 숨결
for k,(lo,hi,buzz,amp) in enumerate([(4200,5600,72,0.5),(5000,6600,88,0.35),(3600,4600,60,0.25)]):
    n=bp(rng.standard_normal(N),lo,hi)
    am=0.55+0.45*np.sin(2*np.pi*buzz*t+rng.random()*6)
    swell=0.6+0.4*np.sin(2*np.pi*t/L*(k+1)+rng.random()*6)      # 루프 길이에 맞춘 주기 → 이음매 없음
    out+=n*am*swell*amp
out/=np.max(np.abs(out))+1e-9; out*=0.35
# 2) 참매미 한 마리: 맴(0.32s) x 9 → 매애앰(1.2s), 반복 사이 쉼
def call(start, gain, f0):
    seg=np.zeros(N); pos=int(start*SR)
    pulses=[(0.32,1.0)]*9+[(1.2,1.0)]
    for i,(d,_) in enumerate(pulses):
        n=int(d*SR); tt=np.arange(n)/SR
        f=f0*(1+0.04*np.minimum(1,tt/d))+ (i*6)
        car=np.sin(2*np.pi*np.cumsum(np.full(n,f))/SR)
        trem=(0.5+0.5*np.sin(2*np.pi*130*tt))**2                  # 진동막 떨림(부드럽게)
        env=np.minimum(1,tt/0.04)*np.minimum(1,(d-tt)/0.08)
        if i==len(pulses)-1: env*=np.exp(-tt*1.4)
        x=car*(0.35+0.65*trem)*env
        x+=0.3*bp(rng.standard_normal(n),f0*0.9,f0*1.6,2)*env
        x=bp(x,f0*0.75,f0*1.7,3)
        idx=(pos+np.arange(n))%N; seg[idx]+=x*gain                   # 루프를 넘어가면 앞으로 감김
        pos+=n+int(0.05*SR)
    return seg
out+=call(1.5,0.22,3900)+call(9.8,0.14,4300)
out=bp(out,1800,9000,2)
out/=np.max(np.abs(out))+1e-9; out*=0.45
# 시작/끝 맞추기(이음매 짧은 크로스페이드)
F=int(0.25*SR); out[:F]=out[:F]*np.linspace(0,1,F)+out[-F:]*np.linspace(1,0,F); out=out[:-F]
pcm=(out*32767).astype(np.int16)
with wave.open('amb_cicada.wav','wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
subprocess.run(['ffmpeg','-y','-loglevel','error','-i','amb_cicada.wav','-c:a','libvorbis','-q:a','4','amb_cicada.ogg'],check=True)
print('ok', len(pcm)/SR)
