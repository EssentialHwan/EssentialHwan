# 11/02 새 홍보 포맷: ② 집사 공감 POV · ③ 비 오는 날 루프. 원본 = Unity F11 녹화(1920×1080, 30fps)
import os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.dirname(os.path.abspath(__file__)) + '/../../UnityProject/Assets/Resources'
FONT = f'{ROOT}/Fonts/Galmuri11.ttf'
AUD = f'{ROOT}/Audio'
REC = '/mnt/user-data/uploads/D:--YeogiCafe/YeogiCafe_Unity/Build/gif'
CREAM, INK, SOFT, PINK = (250, 240, 222), (92, 64, 52), (150, 112, 86), (196, 92, 110)
TMP = '/tmp/promo2'; os.makedirs(TMP, exist_ok=True)
W, H = 1080, 1920

def run(*a): subprocess.run(a, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

def frame_png(path, top, top2, bottom=True, box_y=420, box_h=1080, cta=None):
    im = Image.new('RGBA', (W, H), CREAM + (255,)); d = ImageDraw.Draw(im)
    def center(t, f, y, col):
        lines = t.split('\n')
        for i, ln in enumerate(lines):
            d.text(((W - d.textlength(ln, font=f)) / 2, y + i * (f.size + 14)), ln, font=f, fill=col)
    f1 = ImageFont.truetype(FONT, 58); f2 = ImageFont.truetype(FONT, 34)
    center(top, f1, 150, INK)
    if top2: center(top2, f2, 150 + (top.count('\n') + 1) * 72 + 10, SOFT)
    d.rectangle((0, box_y - 8, W, box_y + box_h + 8), fill=INK)      # 영상 테두리
    im.paste((0, 0, 0, 0), (0, box_y, W, box_y + box_h))            # 영상이 비쳐 보일 구멍
    if cta:
        fc = ImageFont.truetype(FONT, 40); fs = ImageFont.truetype(FONT, 28)
        bw = max(d.textlength(cta[0], font=fc), d.textlength(cta[1], font=fs)) + 70
        y0 = box_y + box_h + 40
        d.rounded_rectangle(((W - bw) / 2, y0, (W + bw) / 2, y0 + 116), radius=24, fill=PINK)
        center(cta[0], fc, y0 + 16, (255, 250, 244)); center(cta[1], fs, y0 + 70, (255, 236, 236))
    elif bottom:
        fb = ImageFont.truetype(FONT, 40); fs = ImageFont.truetype(FONT, 28)
        center('Purrsonal Taste · 여기카페냐옹', fb, box_y + box_h + 60, PINK)
        center('Steam 2027 · @winterystudio', fs, box_y + box_h + 120, SOFT)
    im.save(path); return path

def seg(src, t0, dur, crop, png, out, box_y=420, box_h=1080, slow=1.0):
    x, y, w, h = crop
    vf = (f"[0:v]trim=start={t0}:duration={dur},setpts={slow}*(PTS-STARTPTS),crop={w}:{h}:{x}:{y},scale={W}:{box_h}:flags=neighbor,fps=30[v];"
          f"color=c=0xFAF0DE:s={W}x{H}:d={dur*slow}[bg];[bg][v]overlay=0:{box_y}[b];[b][1:v]overlay=0:0,format=yuv420p")
    run('ffmpeg', '-y', '-i', f'{REC}/{src}', '-loop', '1', '-i', png, '-filter_complex', vf, '-t', str(dur * slow), '-an', '-c:v', 'libx264', '-crf', '18', '-r', '30', out)
    return out

def concat(parts, out):
    lst = f'{TMP}/list.txt'; open(lst, 'w').write(''.join(f"file '{p}'\n" for p in parts)); 
    run('ffmpeg', '-y', '-f', 'concat', '-safe', '0', '-i', lst, '-c', 'copy', out)

def add_audio(video, out, total, layers):
    # layers: [(file, start_sec, vol, loop)]
    args = ['ffmpeg', '-y', '-i', video]; fl = []; ins = []
    for i, (f, st, vol, loop) in enumerate(layers):
        if loop: args += ['-stream_loop', '-1']
        args += ['-i', f'{AUD}/{f}.ogg']
        fl.append(f"[{i+1}:a]adelay={int(st*1000)}|{int(st*1000)},volume={vol}[a{i}]"); ins.append(f'[a{i}]')
    fl.append(f"{''.join(ins)}amix=inputs={len(ins)}:normalize=0,afade=t=out:st={total-0.8}:d=0.8,atrim=0:{total}[a]")
    args += ['-filter_complex', ';'.join(fl), '-map', '0:v', '-map', '[a]', '-c:v', 'copy', '-c:a', 'aac', '-shortest', out]
    run(*args)

def rain():
    # 저녁 비(따뜻한 조명) 위주. 창 + 고양이 쪽을 정사각으로. 끝 1초를 처음과 섞어 이어 붙이면 무한 반복처럼
    png = frame_png(f'{TMP}/rain_bg.png', '비 오는 날의 고양이 카페', '소리 켜고 잠깐 쉬어 가세요', box_y=430, box_h=1080)
    a = seg('rec_1102_185606.mp4', 4.6, 13.0, (1000, 300, 540, 540), png, f'{TMP}/rain_a.mp4', 430, 1080)   # 창가 자리에서 치즈냥이 몸을 말고 잠든다
    # 루프: 마지막 1초와 처음 1초를 크로스페이드
    run('ffmpeg', '-y', '-i', a, '-filter_complex',
        "[0:v]split[m][h];[h]trim=0:1,setpts=PTS-STARTPTS[head];[m]trim=1:13,setpts=PTS-STARTPTS[main];"
        "[main][head]xfade=transition=fade:duration=1:offset=11,format=yuv420p", '-c:v', 'libx264', '-crf', '18', f'{TMP}/rain_loop.mp4')
    add_audio(f'{TMP}/rain_loop.mp4', 'reel15_rainy_cafe.mp4', 12,
              [('amb_rain', 0, 0.9, True), ('sfx_purr', 1.5, 0.5, False), ('sfx_purr', 6.5, 0.45, False), ('sfx_mrrng', 9.5, 0.4, False)])

def pov():
    parts = []
    s = [
        ('rec_1102_185318.mp4', 0.0, 2.3, (90, 490, 540, 540), 'POV: 우리 집 냥이,\n쿠션만 보면 쭙쭙이', '(이 게임 고양이도 그럼)', 1.6),
        ('rec_1102_185407.mp4', 0.5, 2.6, (470, 460, 620, 620), '집사한텐 안 해 주는 부비부비,\n친구한텐 해 줌', None, 1.5),
        ('rec_1102_185440.mp4', 0.0, 3.4, (630, 540, 540, 540), '그러다 결국\n서로 그루밍해 주는 사이', None, 1.25),
    ]
    for i, (src, t0, dur, crop, top, top2, sl) in enumerate(s):
        png = frame_png(f'{TMP}/pov_{i}.png', top, top2, box_y=470, box_h=1080)
        parts.append(seg(src, t0, dur, crop, png, f'{TMP}/pov_{i}.mp4', 470, 1080, sl))
    png = frame_png(f'{TMP}/pov_end.png', '손님마다 좋아하는 게 달라요', '관찰해서 맞혀 주세요', box_y=470, box_h=1080,
                    cta=('팔로우하고 다음 손님 만나기', 'Follow to meet the next guest'))
    parts.append(seg('rec_1102_185606.mp4', 7.0, 3.0, (1000, 300, 540, 540), png, f'{TMP}/pov_end.mp4', 470, 1080))
    concat(parts, f'{TMP}/pov_all.mp4')
    add_audio(f'{TMP}/pov_all.mp4', 'reel16_pov_owner.mp4', 15.13,
              [('amb_rain', 0, 0.35, True), ('sfx_suck', 0.6, 0.55, False), ('sfx_suck', 1.8, 0.55, False), ('sfx_trill', 4.6, 0.55, False),
               ('sfx_lick', 8.8, 0.5, False), ('sfx_lick', 9.6, 0.5, False), ('sfx_purr', 12.8, 0.5, False)])


def quiz():
    # ① 맞히기 퀴즈: 관찰 장면(힌트) → 마감 후 추리 창. 정답(쿠션)은 고정 댓글로.
    png = frame_png(f'{TMP}/quiz_0.png', 'Q. 회색냥이 좋아하는\n시설은 뭘까요?', '오늘 관찰한 장면이 힌트예요', box_y=470, box_h=1080)
    a = seg('rec_1102_185121.mp4', 6.0, 5.0, (20, 540, 540, 540), png, f'{TMP}/quiz_0.mp4', 470, 1080)
    pop = Image.open(f'{TMP}/pop_src.jpg').crop((490, 300, 1436, 744)).resize((1080, 507), Image.LANCZOS)
    png2 = frame_png(f'{TMP}/quiz_1.png', '정답을 댓글로 남겨 주세요!', '① 창문  ② 쿠션  ③ 스크래처  ④ 장난감 바구니',
                     box_y=620, box_h=507, cta=('정답은 고정 댓글에 ', 'Answer in the pinned comment'))
    bg = Image.open(png2).convert('RGBA'); base = Image.new('RGBA', (W, H), CREAM + (255,)); base.paste(pop, (0, 620)); base.alpha_composite(bg)
    base.convert('RGB').save(f'{TMP}/quiz_1_full.png')
    run('ffmpeg', '-y', '-loop', '1', '-t', '5', '-i', f'{TMP}/quiz_1_full.png', '-vf', 'fps=30,format=yuv420p', '-c:v', 'libx264', '-crf', '18', f'{TMP}/quiz_1.mp4')
    concat([a, f'{TMP}/quiz_1.mp4'], f'{TMP}/quiz_all.mp4')
    add_audio(f'{TMP}/quiz_all.mp4', 'reel17_quiz_gray.mp4', 10,
              [('amb_rain', 0, 0.3, True), ('sfx_purr', 1.0, 0.5, False), ('sfx_boop', 5.0, 0.6, False), ('sfx_meow_small', 5.6, 0.5, False)])


def relations():
    # ④ 단짝·앙숙: 서로 그루밍 장면 → 도감 관계도(고양이별로 넘겨 보기)
    png = frame_png(f'{TMP}/rel_0.png', '고양이 손님들끼리도\n사이가 생겨요', '단짝 · 친구 · 티격태격 · 앙숙', box_y=470, box_h=1080)
    a = seg('rec_1102_185440.mp4', 0.0, 3.4, (630, 540, 540, 540), png, f'{TMP}/rel_0.mp4', 470, 1080, 1.25)
    png2 = frame_png(f'{TMP}/rel_1.png', '누가 누구랑 단짝일까?', '도감 → 관계도에서 고양이별로 볼 수 있어요', box_y=560, box_h=757,
                     cta=('팔로우하고 다음 손님 만나기', 'Follow to meet the next guest'))
    b = seg('rec_1102_191317.mp4', 0.0, 6.4, (430, 90, 1070, 750), png2, f'{TMP}/rel_1.mp4', 560, 757)
    concat([a, b], f'{TMP}/rel_all.mp4')
    add_audio(f'{TMP}/rel_all.mp4', 'reel18_relations.mp4', 10.65,
              [('amb_rain', 0, 0.3, True), ('sfx_lick', 0.5, 0.5, False), ('sfx_lick', 1.5, 0.5, False), ('sfx_purr', 2.5, 0.45, False),
               ('sfx_click', 6.8, 0.5, False), ('sfx_click', 9.3, 0.5, False), ('sfx_click', 11.8, 0.5, False)])

if __name__ == '__main__':
    for k in sys.argv[1:] or ['rain', 'pov']: globals()[k]()
    print('ok')
