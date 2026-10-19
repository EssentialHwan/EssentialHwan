# 여기까페냐옹 진행 시뮬(코드 규칙을 단순화해 옮김). 몬테카를로로 일차·시간 추정.
import random, statistics as st

CATS = {  # id: (price of favorite, seat pref, facility pref, unlock)
 'cheese':  (60, 'Window', 'Soft',    ('always',)),
 'calico':  (70, 'Corner', 'Play',    ('disc', 1)),
 'cow':     (80, 'TwoSeat','Social',  ('disc', 1)),
 'black':   (30, 'Corner', 'Natural', ('lv', 2)),
 'tuxedo':  (30, 'Sofa',   'Play',    ('lv', 2)),
 'mackerel':(60, 'Sofa',   'Soft',    ('lv', 3)),
 'siamese': (70, 'Window', 'Play',    ('lv', 3)),
 'gray':    (80, 'TwoSeat','Social',  ('axes', 6)),
 'moon':    (90, 'Window', 'Height',  ('moon',)),
}
CURIOUS = {'calico', 'siamese', 'moon'}
SEAT_ITEM = {'Window': ('seat_window', 200), 'Corner': ('seat_corner', 150), 'TwoSeat': ('seat_two', 220), 'Sofa': ('seat_sofa', 300)}
FAC_ITEM = {'Soft': ('fac_cushion', 200), 'Play': ('fac_scratch', 250), 'Social': ('fac_toybox', 350), 'Natural': ('fac_plant', 180), 'Height': ('fac_tower', 400)}
FAC_TAGS = {'fac_cushion': {'Soft'}, 'fac_scratch': {'Play'}, 'fac_toybox': {'Play', 'Social'}, 'fac_plant': {'Natural'}, 'fac_tower': {'Play', 'Height'}}

def run(seed, cost_deducted=False, guess_skill=0.75, buy_all_needed=True, speed=2.0, menu_mode='fav', once_per_day=False, lv_cost=(800,2000), food_easy=True, cap=1):
    R = random.Random(seed)
    gold = 300; lv = 1; day = 0
    seats = ['normal'] * 3; facs = []
    cats = {c: dict(v=0, sat=[], obs={'food': 0, 'seat': {}, 'fac': {}}, rec=set(), tried=set(), stage=1, grad=False) for c in CATS}
    log = {}
    def unlocked(c):
        u = CATS[c][3]
        if u[0] == 'always': return True
        if u[0] == 'disc': return sum(1 for k in cats.values() if k['v'] > 0) >= u[1]
        if u[0] == 'lv': return lv >= u[1]
        if u[0] == 'axes': return sum(len(k['rec']) for k in cats.values()) >= u[1]
        if u[0] == 'moon': return lv >= 3 and 'seat_window' in seats and 'fac_tower' in facs
    total_real = 0.0
    while day < 200:
        day += 1
        income = 0; cost = 0
        # ── 영업 720초(시뮬초) ── 손님: 스폰 간격(단계별) × 진행도 배수, 좌석·풀 제한
        disc = sum(1 for k in cats.values() if len(k['rec']) > 0)
        mult = 1.6 + (1.0 - 1.6) * min(1, (lv - 1) * 0.34 + disc * 0.08)
        t = 0; active = []  # (leave_time, cat)
        visited_today = set(); visit_n = {}
        comers = {c for c in CATS if (not once_per_day) or c not in cats or R.random() < min(0.95, 0.2 + (st.mean(cats[c]['sat'][-3:]) if cats[c]['sat'] else 60)/100*0.5 + [0,0,0.15,0.3][cats[c]['stage']])}
        timer = 0; first = True
        while t < 720:
            active = [a for a in active if a[0] > t]
            interval = (30 if t < 180 else 20 if t < 420 else 15) * mult
            timer += 1
            pool = [c for c in CATS if unlocked(c) and c not in [a[1] for a in active]]
            pool = [c for c in pool if not (cats[c]['grad'] and R.random() < 0.67)]
            if once_per_day: pool = [c for c in pool if c in comers and visit_n.get(c,0) < cap]
            if (first or timer >= interval) and pool and len(active) < len(seats):
                first = False; timer = 0
                c = R.choice(pool); visited_today.add(c); visit_n[c] = visit_n.get(c,0)+1
                stay = R.gauss(95, 15) + (15 if c in ('cheese', 'mackerel') else 0)
                active.append((t + stay, c))
                k = cats[c]; price, sp, fp, _ = CATS[c]
                # 음식: 최애 메뉴가 열려 있으면 항상 최애(점수 100)
                fav_open = not (c == 'moon' and lv < 3)
                if fav_open: k['obs']['food'] += 3
                # 좌석: 선호 좌석이 있고 비어 있으면 거기
                used_seat = sp if SEAT_ITEM[sp][0] in seats and R.random() < 0.8 else 'Normal'
                w = 1.5 if used_seat == sp else 0.5
                k['obs']['seat'][used_seat] = k['obs']['seat'].get(used_seat, 0) + w
                # 시설: 결정 2~3번, 선호 시설 0.65 / 기타 0.15(+호기심 0.3)
                used_pref = False
                for _ in range(R.choice([1, 2, 3])):
                    best = None; bs = 15 + R.uniform(-10, 10)
                    for f in facs:
                        p = 0.15 + (0.5 if fp in FAC_TAGS[f] else 0) + (0.3 if c in CURIOUS else 0)
                        s = p * 100 + R.uniform(-10, 10)
                        if s > bs: bs, best = s, f
                    if best:
                        tag = fp if fp in FAC_TAGS[best] else sorted(FAC_TAGS[best])[0]
                        k['obs']['fac'][tag] = k['obs']['fac'].get(tag, 0) + 1.5
                        used_pref |= tag == fp
                sat = 50 + (25 if fav_open else -5) + (15 if used_seat == sp else 0) + (15 if used_pref else 0)
                tip = 0.8 if sat < 40 else 1.0 if sat < 70 else 1.15 if sat < 90 else 1.3
                pay = round((price if fav_open else 50) * tip)
                income += pay; cost += 20
                k['v'] += 1; k['sat'].append(sat)
            t += 1
        gold += income - (cost if cost_deducted else 0)
        # ── 정산: 추리 기록(축당 하루 1번) ──
        for c, k in cats.items():
            for ax in ('food', 'seat', 'fac'):
                if ax in k['rec']: continue
                if ax == 'food': top = k['obs']['food']; ok = top >= 3; correct_p = 1.0 if food_easy else guess_skill
                else:
                    d = k['obs'][ax]
                    if not d: continue
                    pref = CATS[c][1] if ax == 'seat' else CATS[c][2]
                    top = max(d.values()); ok = top >= 3
                    correct_p = guess_skill if d.get(pref, 0) == top else 0.1
                    if d.get(pref, 0) >= 7 and d.get(pref, 0) - sorted(d.values())[-2 if len(d) > 1 else 0] * (len(d) > 1) >= 3: correct_p = 1.0
                if ok and R.random() < correct_p:
                    k['rec'].add(ax); gold += 20
            avg = st.mean(k['sat'][-10:]) if k['sat'] else 0
            if k['stage'] < 2 and k['v'] >= 3 and avg >= 60: k['stage'] = 2
            if k['stage'] < 3 and k['v'] >= 6 and len(k['rec']) >= 2: k['stage'] = 3; gold += 60
            if not k['grad'] and k['stage'] >= 3 and len(k['rec']) == 3: k['grad'] = True; log.setdefault('grad_' + c, day)
        # ── 구매: 해금된 고양이의 선호 가구 우선, 그다음 레벨업 ──
        need = []
        for c in CATS:
            if not unlocked(c): continue
            si, sprc = SEAT_ITEM[CATS[c][1]]; fi, fprc = FAC_ITEM[CATS[c][2]]
            if si not in seats: need.append((si, sprc, 'seat'))
            if fi not in facs: need.append((fi, fprc, 'fac'))
        if lv >= 3 and 'fac_tower' not in facs: need.append(('fac_tower', 400, 'fac'))
        if lv >= 3 and 'seat_window' not in seats: need.append(('seat_window', 200, 'seat'))
        for item, prc, kind in sorted(set(need), key=lambda x: x[1]):
            if gold >= prc:
                gold -= prc
                (seats if kind == 'seat' else facs).append(item)
        disc_v = sum(1 for k in cats.values() if k['v'] > 0); axes = sum(len(k['rec']) for k in cats.values())
        nfurn = len(seats) - 3 + len(facs)
        if lv == 1 and gold >= lv_cost[0] and (disc_v >= 2 or nfurn >= 3): gold -= lv_cost[0]; lv = 2; log.setdefault('lv2', day)
        if lv == 2 and gold >= lv_cost[1] and ((disc_v >= 3 and axes >= 3) or nfurn >= 5): gold -= lv_cost[1]; lv = 3; log.setdefault('lv3', day)
        for c in CATS:
            if unlocked(c): log.setdefault('unlock_' + c, day)
        log.setdefault('income_d%d' % day, income)
        if all(k['grad'] for k in cats.values()):
            log['done'] = day; break
    log['gold_end'] = gold
    return log

def summary(**kw):
    runs = [run(s, **kw) for s in range(300)]
    def med(key):
        xs = [r[key] for r in runs if key in r]
        return (st.median(xs), min(xs), max(xs)) if xs else None
    return runs, med

if __name__ == '__main__':
    for label, kw in [('E. 고양이당 하루 최대 2회 + 재료비 차감', dict(once_per_day=True, cost_deducted=True, cap=2)),
                      ('F. E + 성장비 1200/3000', dict(once_per_day=True, cost_deducted=True, cap=2, lv_cost=(1200,3000)))]:
        runs, med = summary(**kw)
        print('==', label)
        for k in ['income_d1', 'income_d5', 'income_d10', 'lv2', 'lv3', 'unlock_gray', 'unlock_moon', 'grad_cheese', 'done']:
            print(f'  {k:12s}', med(k))
