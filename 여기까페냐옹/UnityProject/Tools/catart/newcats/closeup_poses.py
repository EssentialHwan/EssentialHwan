# 마감 클로즈업 v3: 표정을 새로 그리지 않고, 홀 도트 시트의 '기존 표정 칸'과 같은 원화 포즈를 고해상도(4.5배)로 다시 뽑는다.
#   python3 closeup_poses.py <출력폴더> [배율=4.5]
# 시트 칸 20~24(마감용 표정)는 원화 포즈 MAP: 20·21→2(눈웃음) · 22→14(웅크린 하악) · 23→3(야옹) · 24→8(식빵)
# → 그림체가 원화 그대로라 부자연스러운 덧그림이 없다. (closeup_faces.py의 눈·입 덧그림 방식은 폐기, 10/28 형 피드백)
import sys, os
import closeup as CU, eye_rim as ER, numpy as np
from PIL import Image
RIM = {'cow', 'tuxedo'}   # 검은 털이 눈에 닿는 고양이: 눈 둘레 크림색 띠(eye_rim.py)
EXPR = {'open': 8, 'happy': 2, 'closed': 2, 'angry': 14, 'wide': 3, 'half': 8}
CATS = ['cheese', 'calico', 'cow', 'gray', 'mackerel', 'siamese', 'tuxedo', 'moon', 'fold', 'munchkin']
if __name__ == '__main__':
    out = sys.argv[1]; mult = float(sys.argv[2]) if len(sys.argv) > 2 else 4.5
    os.makedirs(out, exist_ok=True)
    for c in CATS:
        done = {}
        for e, p in EXPR.items():
            if p not in done:
                im = CU.make(c, f'{c}_A_src.png', mult, p)
                if c in RIM: im = Image.fromarray(ER.rim(np.array(im))[0])
                done[p] = im
            done[p].save(os.path.join(out, f'close_{c}_{e}.png'))
        print('ok', c)
