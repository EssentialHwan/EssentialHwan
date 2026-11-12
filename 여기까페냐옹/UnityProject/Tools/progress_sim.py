#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
여기카페냐옹 — 엔딩 도달 시점 몬테카를로 시뮬레이터 (2026-11 코드 기준, 13마리 엔딩 규칙)

코드에서 그대로 옮긴 규칙 (파일 근거)
  - 하루 720 시뮬초, Rate = speed*2 (DayManager). 스폰 타이머만 Rate 배로 흐르고,
    고양이 행동(체류 30초/느긋 45초, 휴식 7~14초, 시설 9~15초)·조리·인내심은 '실제 초'(Time.deltaTime, 배속 미적용).
    → 배속을 올리면 하루가 짧아지지만 고양이 한 마리의 체류 실시간은 그대로라 좌석 회전이 줄어든다.
  - 스폰: CustomerSpawner (간격 30/20/15 × 진행도 배수 1.6→1.0, 1·2일차 ≤0.9, 평판·냥슐랭 배수,
    주문 대기 3 이상이면 보류, 빈자리 없으면 타이머만 리셋, 고양이당 하루 2회(+첫 만남 1, +1·2일차 1),
    아침 재방문 굴림(RevisitSystem), 최소 손님 2(첫 주 3), 풀 순환(queueIndex), 졸업 고양이 67% 건너뜀,
    마감 직전 (최대 조리+12)*Rate 시뮬초는 입장 금지).
  - 좌석: SeatSelector (선호 태그 100 / 아니면 10, 내향 +30(구석·조용), 거리, ±10).
  - 주문: OrderDecider (최애 60% / 같은 계열 25% / 나머지 15%, 식탐 식사 메뉴 가로채기).
    최애 기구가 없으면 음식 단서는 전부 fallback(0.5, 태그당 상한 2) → 음식 축이 '짐작'이 될 수 없다.
  - 단서: 음식 3.0 / 선호 좌석 1.5(+창밖 응시 1.5) / 선호 시설 1.5 / fallback 0.5(상한 2).
    하루 끝에 합산 → 최고 태그 합 ≥3 이면 '짐작'(추리 가능), ≥7 & 격차 ≥3 이면 '거의 확실'.
    단서 키는 가구의 첫 태그(Tags[0]) — 캣타워 → "Play", 화분 → "Quiet", 쿠션 → "Soft".
  - Box·Warm 태그 시설(과일박스·온열방석·햇볕)과 다락(LoftSpot)은 단서를 남기지 않는다(CatBrain.RecordFacilityClue).
  - 추리: 그날 온 고양이만, 축당 하루 1번, '짐작 이상인 축 전부'를 골라야 [확인] 가능, 전부 맞아야 기록.
    틀린 보기는 다음부터 ×(선택 불가), 맞은 칸은 연필 ○로 유지. 보기는 '지금 가진' 메뉴/좌석/시설뿐.
  - 선물: 만족 ≥70 이면 60% (고양이당 대기 1개) → 다음 날 주우면 그 축 진짜 취향에 비-fallback 단서.
    (Warm 시설·라떼·새우꼬치는 선물 정의가 없어 그 축이 뽑히면 선물 없음.)
  - 경제: 시작 300G, 결제 = 가격×팁(만족)×별 배수×냥슐랭, 재료비 차감, 기록 보너스 20G(짐작 단계 30G)+묶음 보너스,
    단골 3단계 에피소드 보상, Lv2 800G(발견 2 또는 가구 3), Lv3 2000G(발견 3·축 3 또는 가구 5),
    기구: 도마 350(Lv1) / 주전자 300(Lv2) / 그릴 600(Lv3).
  - 날씨: System.Random(day*7919+13) — 일차로 고정(여기서 .NET 알고리즘 그대로 재현). 맑은 날 햇볕 자리(Warm 시설).
  - 엔딩: 보호소 3마리를 뺀 13마리 × 3축 전부 기록(EndingPanel). 기억 하루 2개 제한은 졸업만 늦추고 엔딩은 막지 않는다.

가정 (코드로 정할 수 없는 것 — 플레이어 행동)
  A1. 플레이어 3유형: 하트/말풍선을 알아챌 확률, 리뷰·선물로 추론할 확률, 오선택률, 추리 참여율,
      주방 반응 속도, 구매 지연·낭비(장식·강화·테마 등 진행과 무관한 지출 비율).
  A2. 구매 순서는 '종류별로 하나씩 + 기구 + 레벨업' 고정 큐(BUY_QUEUE). careful 은 순서대로, 나머지는 순서가 흐트러진다.
  A3. 모르는 축은 남은 보기 중 무작위. 아는 축은 정답 가구/메뉴를 고른다(오선택률 제외).
  A4. 보호소 후원 안 함(보호소 고양이가 손님 풀에 끼지 않음). 과일박스 안 삼. 가구·레시피 강화 효과 무시.
  A5. 걷는 시간(문→자리 3~7초, 시설 1.5~5초), 의뢰 수입(하루 25~90G), 배달냥 수입은 근사.
      쥐 소동·대장냥이 자리 뺏기·관계 이벤트·테마 보너스·좌석 강화 만족은 생략.
  A6. warm_fix=True(기본): 온열방석 이용이 Warm 선호 고양이(고등어냥)에게 시설 단서를 남기도록 '고쳤다'고 가정.
      코드 그대로(warm_fix=False)면 고등어냥 시설 축은 영원히 '?' → 엔딩 불가. 이 경우는 day38(그 축만 뺀 완료일)로 본다.
"""
import heapq, random, statistics as st, sys, json, os
from multiprocessing import Pool

# ───────────────────────── 데이터 (Editor/ContentGenerator.cs) ─────────────────────────
MENUS = {  # id: (price, cost, cookTime, tags, tool)
    'americano': (30, 8, 3, set(), 'coffee'),
    'latte': (40, 12, 4, {'Warm', 'Sweet'}, 'coffee'),
    'milk': (30, 10, 3, {'Warm'}, 'kettle'),
    'fishcake': (60, 20, 5, {'Fish', 'Sweet'}, 'oven'),
    'parfait': (70, 25, 6, {'Fruit', 'Sweet'}, 'board'),
    'tuna_sand': (80, 30, 6, {'Fish', 'Meal'}, 'board'),
    'catnip_tea': (40, 12, 4, {'Warm'}, 'kettle'),
    'pudding': (50, 18, 5, {'Sweet'}, 'oven'),
    'grilled_fish': (90, 35, 8, {'Fish', 'Meal', 'Warm'}, 'grill'),
    'skewer': (85, 30, 7, {'Fish', 'Meal'}, 'grill'),
}
MENU_ORDER = list(MENUS)
TOOLS = {'board': (1, 350), 'kettle': (2, 300), 'grill': (3, 600)}  # (cafeLevel, price)
SEATS = {  # id: (price, tags) — 첫 태그가 단서 키
    'normal': (80, ['Normal']), 'window': (200, ['Window', 'Quiet', 'OutsideView']),
    'corner': (150, ['Corner', 'Quiet']), 'two': (220, ['TwoSeat']),
    'sofa': (300, ['Sofa']), 'bar': (260, ['BarTable']),
}
FACS = {
    'tower': (400, ['Play', 'Height', 'Active']), 'cushion': (200, ['Soft', 'Relax', 'Quiet']),
    'toybox': (350, ['Play', 'Social', 'Active']), 'scratch': (250, ['Play', 'Active']),
    'fruitbox': (600, ['Box']), 'heatpad': (280, ['Warm', 'Soft']),
    'wand': (380, ['Play', 'Active']), 'wheel': (450, ['Play', 'Active']),
    'plant': (180, ['Quiet', 'Natural']), 'wallwindow': (150, ['Natural']),
    'sunbeam': (0, ['Warm']), 'loft': (0, ['Height']),   # 가구가 아닌 특수 시설(단서 없음)
}
CATS = {  # id: (성격1, 성격2, 최애, 좌석, 시설, 분위기, 해금)
    'cheese': ('Relaxed', 'Glutton', 'fishcake', 'Window', 'Soft', 'Quiet', ('always',)),
    'calico': ('Curious', 'Fickle', 'parfait', 'Corner', 'Play', 'Lively', ('disc', 1)),
    'cow': ('Social', 'Lonely', 'tuna_sand', 'TwoSeat', 'Social', 'Lively', ('disc', 1)),
    'black': ('Introvert', 'Lonely', 'milk', 'Corner', 'Natural', 'Quiet', ('lv', 2)),
    'tuxedo': ('Active', 'Playful', 'americano', 'Sofa', 'Play', 'Lively', ('lv', 2)),
    'mackerel': ('Glutton', 'Playful', 'skewer', 'Sofa', 'Warm', 'Warm', ('lv', 3)),
    'siamese': ('Fickle', 'Introvert', 'catnip_tea', 'BarTable', 'Height', 'Luxury', ('lv', 3)),
    'gray': ('Relaxed', 'Lonely', 'pudding', 'TwoSeat', 'Quiet', 'Warm', ('axes', 6)),
    'moon': ('Curious', 'Relaxed', 'grilled_fish', 'Window', 'Height', 'Quiet', ('moon', 3)),
    'fold': ('Relaxed', 'Introvert', 'latte', 'Normal', 'Relax', 'Quiet', ('axes', 3)),
    'munchkin': ('Active', 'Playful', 'americano', 'BarTable', 'Social', 'Lively', ('lv', 2)),
    'persian': ('Introvert', 'Glutton', 'parfait', 'Sofa', 'Soft', 'Luxury', ('axes', 12)),
    'tortie': ('Fickle', 'Active', 'grilled_fish', 'TwoSeat', 'Play', 'Natural', ('lv', 3)),
}
CAT_ORDER = list(CATS)
EP_REWARD = {'cheese': 50, 'calico': 50, 'cow': 60, 'black': 50, 'tuxedo': 60, 'mackerel': 60, 'siamese': 70,
             'gray': 70, 'moon': 100, 'fold': 50, 'munchkin': 60, 'persian': 80, 'tortie': 80}
GIFT_FOOD = {'americano', 'milk', 'fishcake', 'parfait', 'tuna_sand', 'catnip_tea', 'pudding', 'grilled_fish'}
GIFT_FAC = {'Play', 'Height', 'Active', 'Soft', 'Relax', 'Quiet', 'Social', 'Natural'}
WARM_FAVS = {'milk', 'catnip_tea', 'grilled_fish'}
AXES = ('food', 'seat', 'fac')

# ───────────────────────── 플레이어 유형 (가정 A1·A2) ─────────────────────────
PROFILES = {
    'careful': dict(notice=0.95, infer=0.60, err=0.02, submit=1.00, react=(1, 4), wrongdish=0.05, touch=1.0,
                    quests=90, buyprob=1.0, waste=0.05, jitter=0.0, delivery=0.95, handle=1.5),
    'average': dict(notice=0.80, infer=0.35, err=0.07, submit=0.90, react=(2, 9), wrongdish=0.15, touch=0.5,
                    quests=55, buyprob=0.70, waste=0.15, jitter=2.5, delivery=0.80, handle=2.5),
    'casual':  dict(notice=0.55, infer=0.15, err=0.15, submit=0.70, react=(3, 16), wrongdish=0.30, touch=0.0,
                    quests=25, buyprob=0.45, waste=0.30, jitter=6.0, delivery=0.60, handle=4.0),
}
BUY_QUEUE = [('seat', 'window'), ('tool', 'board'), ('seat', 'corner'), ('fac', 'cushion'), ('seat', 'two'),
             ('fac', 'toybox'), ('lv', 2), ('tool', 'kettle'), ('seat', 'sofa'), ('fac', 'plant'), ('seat', 'bar'),
             ('lv', 3), ('tool', 'grill'), ('fac', 'tower'), ('fac', 'heatpad'), ('seat', 'two'), ('seat', 'sofa')]

# ───────────────────────── 날씨: .NET(Mono) System.Random 재현 ─────────────────────────
def dotnet_first_double(seed):
    MBIG = 2147483647
    sa = [0] * 56
    mj = 161803398 - abs(seed); sa[55] = mj; mk = 1
    for i in range(1, 55):
        ii = (21 * i) % 55
        sa[ii] = mk; mk = mj - mk
        if mk < 0: mk += MBIG
        mj = sa[ii]
    for _ in range(4):
        for i in range(1, 56):
            sa[i] -= sa[1 + (i + 30) % 55]
            if sa[i] < 0: sa[i] += MBIG
    r = sa[1] - sa[22]
    if r == MBIG: r -= 1
    if r < 0: r += MBIG
    return r * (1.0 / MBIG)
_W = {}
def weather(day):  # 0 맑음 1 흐림 2 비
    if day <= 1: return 0
    if day not in _W:
        r = dotnet_first_double(day * 7919 + 13)
        _W[day] = 0 if r < 0.5 else 1 if r < 0.75 else 2
    return _W[day]

DEFAULT_OPTS = dict(
    warm_fix=True,          # A6
    max_visits=2,           # CustomerSpawner.maxVisitsPerDay
    batch_all=True,         # 묶음 채점: 전부 맞아야 기록
    thr_suspected=3.0,      # BalanceConfig.thrSuspected
    fav_prob=0.6,           # OrderDecider 최애 확률
    lv_cost=(800, 2000),
    price_mult=1.0,         # 메뉴 가격 배수
    sit=30.0,               # CatBrain.BaseSit
    start_seats=3,
    spawn_mult=1.0,         # 스폰 간격 배수
    loft_clue=False,        # 다락이 Height 단서를 남긴다면
    scale_cats=False,       # 고양이 행동도 배속을 따른다면
    fac_clue=1.5,           # 선호 시설 단서 가중치(clueMedium)
    max_days=150,
)

def run(seed, profile='average', speed=1, **kw):
    o = dict(DEFAULT_OPTS); o.update(kw)
    P = PROFILES[profile]
    R = random.Random(seed * 7919 + {'careful': 1, 'average': 2, 'casual': 3}[profile])
    rnd, uni = R.random, R.uniform
    gold = 300; lv = 1; stars = 0
    tools = {'coffee', 'oven'}
    seats = ['normal'] * o['start_seats']            # 좌석 인스턴스(가구 id)
    seat_dist = [uni(3, 8) for _ in seats]
    facs = []                                         # 산 시설 인스턴스
    furn_count = 0
    reviews = []                                      # 최근 리뷰 발바닥
    insp_last = -1
    cs = {c: dict(v=0, sats=[], last=-1, lfav=False, lseat=False, stage=1, grad=False, mem=False, gift=None,
                  obs={a: dict(state=0, tal={}, wrong=set(), lg=-1, pencil=None) for a in AXES},
                  know={a: False for a in AXES}) for c in CATS}
    # obs.state: 0 ? / 1 짐작 / 2 거의 확실 / 3 기록
    queue = list(BUY_QUEUE)
    if P['jitter'] > 0:
        keyed = sorted(((i + uni(0, P['jitter']), it) for i, it in enumerate(queue)), key=lambda x: x[0])
        queue = [it for _, it in keyed]
    log = {}

    def recorded(c): return sum(1 for a in AXES if cs[c]['obs'][a]['state'] == 3)
    def n_disc(): return sum(1 for c in CATS if recorded(c) > 0)
    def n_axes(): return sum(recorded(c) for c in CATS)
    def unlocked(c):
        u = CATS[c][6]
        if u[0] == 'always': return True
        if u[0] == 'disc': return n_disc() >= u[1]
        if u[0] == 'lv': return lv >= u[1]
        if u[0] == 'axes': return n_axes() >= u[1]
        if u[0] == 'moon': return lv >= 3 and 'window' in seats and 'tower' in facs
    def reputation():
        if not reviews: return 3.0
        w = reviews[-20:]; n = len(w); avg = sum(w) / n
        return 3.0 + (avg - 3.0) * (n / 20.0)
    def rep_bonus(rep): return 0.15 if rep >= 4.5 else 0.1 if rep >= 4 else 0.05 if rep >= 3.5 else 0.0
    def revisit_chance(c, day):
        k = cs[c]
        p = 0.2 + (k['sats'][-1] if k['sats'] else 0) / 100 * 0.5
        if k['lfav']: p += 0.1
        if k['lseat']: p += 0.1
        p += (0, 0, 0.15, 0.3)[min(k['stage'], 3)]
        if k['last'] >= 0: p -= (day - k['last']) * 0.02
        return min(0.95, max(0.05, p))
    def promote(ax):
        if ax['state'] == 3: return
        vals = sorted((t[0] for t in ax['tal'].values()), reverse=True)
        top = vals[0] if vals else 0.0; sec = vals[1] if len(vals) > 1 else 0.0
        if top >= 7 and top - sec >= 3: ax['state'] = 2
        elif top >= o['thr_suspected']: ax['state'] = max(ax['state'], 1)

    day = 0
    while day < o['max_days']:
        day += 1
        wth = weather(day)
        rate = 2.0 * speed
        cat_ts = speed if o['scale_cats'] else 1.0     # 고양이 행동 배속(기본: 실제 초)
        T_end = 720.0 / rate
        income0 = gold
        staged = {}                                    # catId → [(axis, key, weight, fallback)]
        def clue(c, axis, key, w, fb): staged.setdefault(c, []).append((axis, key, w, fb))
        # 아침: 선물 줍기(전날 받은 것) — 단서 + 플레이어가 취향을 알게 됨
        for c in CATS:
            g = cs[c]['gift']
            if g and g[2] < day and rnd() < 0.5 + 0.5 * P['notice']:
                axis, key = g[0], g[1]
                clue(c, axis, key, 3.0 if axis == 'food' else 1.5, False)
                if rnd() < P['notice']: cs[c]['know'][axis] = True
                cs[c]['gift'] = None
        # 메뉴판
        menus = [m for m in MENU_ORDER if MENUS[m][4] in tools]
        max_cook = max(MENUS[m][2] for m in menus)
        # 오늘 올 고양이 굴림
        rep = reputation()
        bonus = rep_bonus(rep) + 0.03 * stars
        def pool_now(phase_morning):
            pl = [c for c in CAT_ORDER if unlocked(c)]
            if day <= 2 and (day >= 2 or not phase_morning):
                for c in CAT_ORDER:
                    if c not in pl and CATS[c][6] == ('disc', 1): pl.append(c)
            return pl
        pool0 = pool_now(True)
        pool_late = pool_now(False)
        comers = {c for c in pool0 if cs[c]['v'] == 0 or rnd() < revisit_chance(c, day) + bonus}
        minc = 3 if day <= 7 else 2
        for _ in range(20):
            if len(comers) >= min(minc, len(pool0)): break
            comers.add(R.choice(pool0))
        visits_today = {}; first_met = set()
        prog = min(1.0, max(0.0, (lv - 1) * 0.34 + n_disc() * 0.08))
        imult = 1.6 + (1.0 - 1.6) * prog
        if day <= 2: imult = min(imult, 0.9)
        imult *= min(1.1, max(0.84, 1 - (rep - 3) * 0.08)) * (1 - 0.04 * stars) * o['spawn_mult']
        # 시설 인스턴스(특수 시설 포함)
        fac_inst = list(facs)
        if wth == 0: fac_inst.append('sunbeam')
        if lv >= 3: fac_inst += ['loft'] * 4
        fac_busy = [False] * len(fac_inst)
        seat_busy = [False] * len(seats)
        tool_free = {t: 0.0 for t in tools}
        player_free = [0.0]                            # 플레이어는 한 번에 한 주문만 손댄다(가정)
        in_cafe = {}                                   # catId → visit
        waiting = 0
        ev = []; seq = 0
        def push(t, kind, v):
            nonlocal seq
            seq += 1; heapq.heappush(ev, (t, seq, kind, v))
        visited_today = set()

        def complete(v, t, failed):
            nonlocal gold
            c = v['c']; k = cs[c]; d = CATS[c]
            served = v['menu'] if not failed else None
            sat = 50.0 + 5.0
            if served and v['fav']: sat += 25
            elif v['forced']: sat -= 5
            if v['pseat']: sat += 15
            if v['pfac']: sat += 15
            if served is None: sat -= 10
            sat += 5 * v['touch']
            if v['wrong']: sat -= 12
            if v['mind']: sat += 8
            sat = min(100.0, max(0.0, sat))
            if served: sat += {1: -6, 2: 4, 3: 12}[v['stars']]
            sat += 2.5 * stars
            sat = min(100.0, max(0.0, sat))
            if served:
                tip = 0.8 if sat < 40 else 1.0 if sat < 70 else 1.15 if sat < 90 else 1.3
                pay = round(MENUS[served][0] * o['price_mult'] * tip * {1: 0.85, 2: 1.0, 3: 1.25}[v['stars']] * (1 + 0.06 * stars))
                gold += pay
                cost = MENUS[served][1]
                gold = gold - cost if gold >= cost else min(gold, 0)
            if sat >= 70 and rnd() < 0.6 and k['gift'] is None:
                un = [a for a in AXES if k['obs'][a]['state'] != 3]
                if un:
                    a = R.choice(un)
                    if a == 'food' and d[2] in GIFT_FOOD: k['gift'] = ('food', d[2], day)
                    elif a == 'seat': k['gift'] = ('seat', d[3], day)
                    elif a == 'fac' and d[4] in GIFT_FAC: k['gift'] = ('fac', d[4], day)
            k['sats'].append(sat); k['v'] += 1; k['last'] = day
            k['lfav'] = bool(served and v['fav']); k['lseat'] = v['pseat']
            reviews.append(1 if served is None else min(5, max(1, round(sat / 20))))
            visited_today.add(c)

        def spawn(c, t):
            if cs[c]['v'] == 0: first_met.add(c)
            visits_today[c] = visits_today.get(c, 0) + 1
            d = CATS[c]
            # 좌석 선택
            best = -1; bs = -1e9
            intro = 'Introvert' in d[:2]
            for i, sid in enumerate(seats):
                if seat_busy[i]: continue
                tags = SEATS[sid][1]
                s = (100 if d[3] in tags else 10) + (30 if intro and ('Corner' in tags or 'Quiet' in tags) else 0) \
                    - seat_dist[i] * 0.5 + uni(-10, 10)
                if s > bs: bs, best = s, i
            seat_busy[best] = True
            tags = SEATS[seats[best]][1]
            pseat = d[3] in tags
            clue(c, 'seat', tags[0], 1.5 if pseat else 0.5, not pseat)
            if pseat and 'OutsideView' in tags: clue(c, 'seat', tags[0], 1.5, False)
            if pseat and rnd() < P['notice']: cs[c]['know']['seat'] = True
            v = dict(c=c, seat=best, pseat=pseat, pfac=False, menu=None, fav=False, forced=False, stars=0,
                     wrong=False, mind=False, touch=0, fac=-1, state='walk', used=set(), sit_end=0.0)
            in_cafe[c] = v
            push(t + uni(3, 7) / cat_ts, 'order', v)

        def do_order(v, t):
            nonlocal waiting
            c = v['c']; d = CATS[c]; fav = d[2]
            has_fav = fav in menus
            rel = []; oth = []
            if has_fav:
                ft = MENUS[fav][3]
                for m in menus:
                    if m == fav: continue
                    (rel if MENUS[m][3] & ft else oth).append(m)
            else:
                oth = list(menus)                       # foodPreferredTags 미설정 → 전부 others
            r = rnd(); pick = None
            if 'Glutton' in d[:2] and r >= 0.6 and rnd() < 0.35:
                meals = [m for m in menus if 'Meal' in MENUS[m][3] and m != fav]
                if meals: pick = R.choice(meals)
            if pick is None:
                if has_fav and r < o['fav_prob']: pick = fav
                elif rel and (r < 0.85 or not oth): pick = R.choice(rel)
                elif oth: pick = R.choice(oth)
                else: pick = fav
            v['menu'] = pick; v['fav'] = pick == fav; v['forced'] = not has_fav
            clue(c, 'food', pick, 0.5 if v['forced'] else 3.0, v['forced'])
            vague = cs[c]['stage'] >= 2 and rnd() < 0.6
            patience = 60.0 if vague else 45.0
            tool = MENUS[pick][4]
            start = max(t + uni(*P['react']), tool_free[tool], player_free[0])
            waiting += 1; v['state'] = 'wait'
            if start - t > patience:
                push(t + patience, 'cancel', v); return
            if vague:
                if rnd() < P['wrongdish']:
                    others = [m for m in menus if MENUS[m][4] == tool and m != pick]
                    if others:
                        v['wrong'] = True; pick = others[0]; v['menu'] = pick; v['fav'] = pick == fav
                else: v['mind'] = True
            sd = uni(0.5, 3) * (1 + (P['react'][1] > 8))
            lvl_odds = (0.5, 0.9)
            x = rnd(); s = 1 if x < lvl_odds[0] else 2 if x < lvl_odds[1] else 3
            if sd > 8: s = max(1, s - 1)
            v['stars'] = s
            player_free[0] = start + P['handle']
            done = start + MENUS[pick][2] + sd
            tool_free[tool] = done
            push(done, 'served', v)

        def new_rest(v, t, extra=0.0):
            d = CATS[v['c']]
            k = 1.6 if 'Relaxed' in d[:2] else 0.7 if 'Active' in d[:2] else 1.0
            rest = uni(7, 14) * k * (1.35 if wth == 2 else 1.0)
            push(t + (extra + max(rest, 3.0)) / cat_ts, 'decide', v)

        def decide(v, t):
            if t >= v['sit_end']: return pay(v, t)
            c = v['c']; d = CATS[c]
            best = -1; bs = 15 + uni(-10, 10)
            warm = d[5] == 'Warm' or d[2] in WARM_FAVS
            for i, f in enumerate(fac_inst):
                if fac_busy[i] or i in v['used']: continue      # 한 번 쓴 시설은 페널티 1.0 → 사실상 재방문 없음
                tags = FACS[f][1]
                p = 0.15
                if d[4] in tags: p += 0.5
                if 'Box' in tags: p += 0.55
                if 'Warm' in tags: p += (0.75 if wth == 2 else 0.5) if warm else -0.05
                if 'Curious' in d[:2]: p += 0.3
                if 'Playful' in d[:2] and 'Play' in tags: p += 0.3
                s = min(1.0, max(0.0, p)) * 100 + uni(-10, 10)
                if s > bs: bs, best = s, i
            if best >= 0:
                fac_busy[best] = True; v['fac'] = best; v['used'].add(best)
                push(t + uni(1.5, 5) / cat_ts, 'arrive', v); return
            kid = d[0] in ('Active', 'Playful')
            if rnd() < (0.35 if kid else 0.08) * (1.5 if wth == 0 else 0.4 if wth == 2 else 1.0):
                return new_rest(v, t, uni(4, 8))                 # 우다다
            roam = 0.35 + (0.25 if ('Active' in d[:2] or 'Playful' in d[:2]) else 0) + (0.15 if 'Curious' in d[:2] else 0) \
                   - (0.2 if ('Relaxed' in d[:2] or 'Introvert' in d[:2]) else 0) + (0.1 if wth == 0 else -0.2 if wth == 2 else 0)
            new_rest(v, t, uni(2, 5) if rnd() < roam else 0.0)

        def arrive(v, t):
            c = v['c']; d = CATS[c]; f = fac_inst[v['fac']]; tags = FACS[f][1]
            pref = d[4] in tags
            silent = f == 'loft' or 'Box' in tags or 'Warm' in tags
            if f == 'loft' and o['loft_clue']: silent = False
            if f == 'heatpad' and o['warm_fix'] and d[4] == 'Warm': silent = False
            if not silent:
                if pref: v['pfac'] = True
                clue(c, 'fac', tags[0], o['fac_clue'] if pref else 0.5, not pref)
                if pref and rnd() < P['notice']: cs[c]['know']['fac'] = True
            dur = uni(9, 15) * (1.4 if pref else 1.0) * (1.3 if 'Relaxed' in d[:2] else 1.0) / cat_ts
            end = min(t + dur, max(t, v['sit_end']))
            push(end + 0.55, 'facend', v)

        def pay(v, t):
            if v['fac'] >= 0: fac_busy[v['fac']] = False; v['fac'] = -1
            seat_busy[v['seat']] = False
            v['state'] = 'leaving'
            complete(v, t, False)
            push(t + 4.0 / cat_ts, 'gone', v)

        # ── 영업 루프(실제 초, 0.5초 간격으로 스포너 갱신) ──
        t = 0.0; dt = 0.5; timer = 0.0; first_done = False; qi = 0
        while t < T_end:
            while ev and ev[0][0] <= t:
                et, _, kind, v = heapq.heappop(ev)
                if kind == 'order': do_order(v, et)
                elif kind == 'served':
                    waiting -= 1; v['state'] = 'in'
                    if v['fav'] and rnd() < P['notice']: cs[v['c']]['know']['food'] = True
                    if rnd() < P['touch']: v['touch'] = 1
                    sitd = o['sit'] * (1.5 if 'Relaxed' in CATS[v['c']][:2] else 1.0)
                    v['sit_end'] = et + min(sitd, 90.0) / cat_ts
                    new_rest(v, et)
                elif kind == 'cancel':
                    waiting -= 1; seat_busy[v['seat']] = False; v['state'] = 'leaving'
                    complete(v, et, True); push(et + 4.0, 'gone', v)
                elif kind == 'decide':
                    if v['state'] == 'in': decide(v, et)
                elif kind == 'arrive': arrive(v, et)
                elif kind == 'facend':
                    fac_busy[v['fac']] = False; v['fac'] = -1
                    if et >= v['sit_end']: pay(v, et)
                    else: new_rest(v, et, uni(2, 5))
                elif kind == 'gone':
                    if in_cafe.get(v['c']) is v: del in_cafe[v['c']]
            dayt = t * rate
            morning = dayt < 180
            interval = 30 if morning else 20 if dayt < 420 else 15
            close = (not morning and dayt >= 420) and (720 - dayt) < (max_cook + 12) * rate
            if not close:
                pool = pool0 if morning else pool_late
                trial = False
                if not first_done:
                    first_done = True; trial = True; timer = 0.0
                else:
                    timer += dt * rate
                    if timer >= interval * imult and waiting < 3:
                        timer = 0.0; trial = True
                if trial and not all(seat_busy):
                    cap = o['max_visits'] + (1 if day <= 2 else 0)
                    for _ in range(len(pool)):
                        c = pool[qi % len(pool)]; qi += 1
                        if c in in_cafe: continue
                        n = visits_today.get(c, 0)
                        if n >= (cap + 1 if c in first_met else cap): continue
                        if not (cs[c]['v'] == 0 or c in comers or c in first_met): continue
                        if cs[c]['grad'] and rnd() < 0.67: continue
                        spawn(c, t); break
            t += dt
        # 영업 종료: 남은 손님 정리
        for c, v in list(in_cafe.items()):
            if v['state'] in ('walk', 'wait'): complete(v, T_end, True)     # 못 받고 떠남(결제 없음, 방문은 집계)
            elif v['state'] == 'in': complete(v, T_end, False)
        # 배달냥·의뢰(근사, A5)
        if day == 5 or (day > 7 and True):
            nv = 1 if day == 5 else (1 if rnd() < 0.75 else 0) + (1 if lv >= 2 and rnd() < 0.5 else 0)
            for _ in range(nv):
                for _ in range(min(4, 2 + (lv >= 2) + (rnd() < 0.35))):
                    if rnd() < P['delivery']:
                        m = R.choice(menus)
                        gold += round(MENUS[m][0] * o['price_mult'] * 0.7 * 0.95 * (1 + 0.06 * stars)) - MENUS[m][1]
        gold += round(P['quests'] * uni(0.5, 1.5))
        # 냥슐랭 심사원(근사): 총점 = 음식 35% + 속도 25% + 가구 25% + 청결 15%
        since = day if insp_last < 0 else day - insp_last
        visit = (day == 6) if day <= 7 else (stars < 5 and since >= 2 and (since >= 8 or rnd() < 0.18 + rep_bonus(rep) + 0.04 * since))
        if visit and stars < 5:
            x = rnd(); dish = 35 if x < 0.5 else 70 if x < 0.9 else 100
            wait = uni(*P['react']) + 6 + uni(0, 6)
            spd = min(1.0, max(0.0, 1 - (wait - 25) / 50)) * 100
            fscore = min(100, furn_count * 4 + lv * 12)
            total = round(dish * 0.35 + spd * 0.25 + fscore * 0.25 + 100 * 0.15)
            if total >= (50, 60, 70, 80, 88)[stars]: stars += 1
            insp_last = day
        # ── 하루 끝: 단서 합산·승격, 단골, 기억, 졸업 ──
        for c, lst in staged.items():
            ob = cs[c]['obs']
            for axis, key, w, fb in lst:
                tl = ob[axis]['tal'].setdefault(key, [0.0, 0.0])
                if fb:
                    w = min(w, max(0.0, 2.0 - tl[1])); tl[1] += w
                tl[0] += w
            for a in AXES: promote(ob[a])
        for c in CATS:
            k = cs[c]
            if k['stage'] < 2 and k['v'] >= 3 and st.fmean(k['sats'][-10:]) >= 60: k['stage'] = 2
            if k['stage'] < 3 and k['v'] >= 6 and recorded(c) >= 2: k['stage'] = 3; gold += EP_REWARD[c]
        shown = 0
        for c in CATS:
            k = cs[c]
            if not k['mem'] and recorded(c) == 3 and shown < 2: k['mem'] = True; shown += 1
        for c in CATS:
            k = cs[c]
            if not k['grad'] and k['stage'] >= 3 and recorded(c) == 3 and k['mem']: k['grad'] = True
        # ── 정산: 추리(오늘 온 고양이만, 묶음 채점) ──
        seat_types = list(dict.fromkeys(seats)); fac_types = [f for f in dict.fromkeys(facs) if f != 'fruitbox']
        for c in CAT_ORDER:
            if c not in visited_today: continue
            k = cs[c]; d = CATS[c]
            elig = [a for a in AXES if k['obs'][a]['state'] in (1, 2) and k['obs'][a]['lg'] != day]
            if not elig or rnd() > P['submit']: continue
            picks = []; ok_all = True
            for a in elig:
                ob = k['obs'][a]
                if a == 'food': opts = menus; good = lambda x: x == d[2]
                elif a == 'seat': opts = seat_types; good = lambda x: d[3] in SEATS[x][1]
                else: opts = fac_types; good = lambda x: d[4] in FACS[x][1]
                opts = [x for x in opts if x not in ob['wrong']]
                if not opts: ok_all = None; break            # 고를 보기가 없으면 [확인] 불가
                if not k['know'][a] and rnd() < P['infer'] and (a != 'food' or k['v'] >= 3): k['know'][a] = True
                rights = [x for x in opts if good(x)]
                if ob['pencil'] in opts: pick = ob['pencil']
                elif k['know'][a] and rights and rnd() >= P['err']: pick = R.choice(rights)
                else: pick = R.choice(opts)
                picks.append((a, pick, good(pick)))
            if ok_all is None: continue
            if o['batch_all']:
                if all(g for _, _, g in picks):
                    b = 0
                    for a, pick, _ in picks:
                        b += 30 if k['obs'][a]['state'] == 1 else 20
                        k['obs'][a]['state'] = 3; k['obs'][a]['lg'] = day
                    if len(picks) >= 2: b += 20 * (len(picks) - 1)
                    gold += b
                else:
                    for a, pick, g in picks:
                        ob = k['obs'][a]; ob['lg'] = day
                        if g: ob['pencil'] = pick
                        else: ob['wrong'].add(pick)
            else:                                             # 민감도: 칸마다 따로 채점
                for a, pick, g in picks:
                    ob = k['obs'][a]; ob['lg'] = day
                    if g: gold += 30 if ob['state'] == 1 else 20; ob['state'] = 3
                    else: ob['wrong'].add(pick)
            if recorded(c) == 3: log.setdefault('cat_' + c, day)
        # ── 구매·레벨업(큐 순서, 가정 A2) + 낭비 ──
        net = gold - income0
        log.setdefault('income', []).append(net)
        if day <= 3: log['net_d%d' % day] = net
        log.setdefault('fails', 0)
        if net > 0: gold -= int(net * P['waste'])
        i = 0
        while i < len(queue):
            kind, x = queue[i]
            if kind == 'tool':
                if lv < TOOLS[x][0]: i += 1; continue
                price = TOOLS[x][1]
            elif kind == 'seat': price = SEATS[x][0]
            elif kind == 'fac': price = FACS[x][0]
            else:
                if lv != x - 1: i += 1; continue
                price = o['lv_cost'][x - 2]
                okc = (n_disc() >= 2 or furn_count >= 3) if x == 2 else ((n_disc() >= 3 and n_axes() >= 3) or furn_count >= 5)
                if not okc: i += 1; continue
            if gold < price or rnd() > P['buyprob']: break
            gold -= price; queue.pop(i)
            if kind == 'tool': tools.add(x)
            elif kind == 'seat': seats.append(x); seat_dist.append(uni(2, 12)); furn_count += 1
            elif kind == 'fac': facs.append(x); furn_count += 1
            else: lv = x; log['lv%d' % x] = day
        for c in CATS:
            if unlocked(c): log.setdefault('unlock_' + c, day + 1)
        tot = n_axes()
        if 'day38' not in log and tot - (1 if cs['mackerel']['obs']['fac']['state'] == 3 else 0) >= 38: log['day38'] = day
        if tot >= 39:
            log['done'] = day; break
    log['visits_per_day'] = sum(cs[c]['v'] for c in CATS) / day
    log['income'] = sum(log['income']) / len(log['income'])
    log['stars'] = stars
    log['last_cat'] = max((c for c in CATS), key=lambda c: log.get('cat_' + c, 10 ** 6))
    return log

def _job(a):
    seed, prof, speed, kw = a
    return run(seed, prof, speed, **kw)

def batch(prof, speed, n, pool, **kw):
    return pool.map(_job, [(s, prof, speed, kw) for s in range(n)], chunksize=20)

def pct(xs, p):
    xs = sorted(xs); i = (len(xs) - 1) * p; lo = int(i); hi = min(lo + 1, len(xs) - 1)
    return xs[lo] + (xs[hi] - xs[lo]) * (i - lo)

def summarize(runs, key='done', cap=None):
    xs = [r[key] for r in runs if key in r]
    if not xs: return None
    return dict(n=len(xs), miss=len(runs) - len(xs), mean=round(st.fmean(xs), 1), med=st.median(xs),
                p10=round(pct(xs, .1), 1), p90=round(pct(xs, .9), 1))

if __name__ == '__main__':
    N = int(sys.argv[1]) if len(sys.argv) > 1 else 1000
    NS = int(sys.argv[2]) if len(sys.argv) > 2 else 300
    out = {}
    with Pool(os.cpu_count()) as pool:
        print('=== 기본 결과 (warm_fix=True) ===')
        for speed in (1, 2):
            for prof in PROFILES:
                runs = batch(prof, speed, N, pool)
                s = summarize(runs); out[f'{prof}@{speed}x'] = s
                print(f'[{prof:8s} {speed}x] 엔딩일 {s}')
                print('   lv2', summarize(runs, 'lv2'), ' lv3', summarize(runs, 'lv3'))
                print('   하루 방문 %.1f, 하루 순수입 %.0fG, 냥슐랭 %.1f★' % (
                    st.fmean(r['visits_per_day'] for r in runs), st.fmean(r['income'] for r in runs), st.fmean(r['stars'] for r in runs)))
                print('   고양이별 3축 완료일(중앙값):', {c: summarize(runs, 'cat_' + c)['med'] if summarize(runs, 'cat_' + c) else None for c in CATS})
                print('   해금일(중앙값):', {c: summarize(runs, 'unlock_' + c)['med'] if summarize(runs, 'unlock_' + c) else None for c in CATS})
                last = {}
                for r in runs: last[r['last_cat']] = last.get(r['last_cat'], 0) + 1
                print('   마지막 고양이 분포:', dict(sorted(last.items(), key=lambda x: -x[1])[:5]))
                print('   day38(고등어냥 시설 뺀 38축):', summarize(runs, 'day38'))
        print('\n=== 코드 그대로 (warm_fix=False, 150일 상한) ===')
        for prof in PROFILES:
            runs = batch(prof, 1, NS, pool, warm_fix=False)
            print(f'[{prof} 1x] 엔딩 도달 {sum(1 for r in runs if "done" in r)}/{len(runs)}, day38 {summarize(runs, "day38")}')
        print('\n=== 민감도 (average, 1x, n=%d) ===' % NS)
        base = summarize(batch('average', 1, NS, pool))
        print('기준', base)
        tests = {
            '하루 방문 상한 2→3': dict(max_visits=3), '하루 방문 상한 2→1': dict(max_visits=1),
            '칸마다 따로 채점(묶음 해제)': dict(batch_all=False),
            '짐작 임계 3→1.5': dict(thr_suspected=1.5),
            '최애 주문 확률 0.6→0.8': dict(fav_prob=0.8),
            '레벨업 비용 절반(400/1000)': dict(lv_cost=(400, 1000)),
            '메뉴 가격 +30%': dict(price_mult=1.3), '메뉴 가격 -30%': dict(price_mult=0.7),
            '체류 30→20초': dict(sit=20.0), '체류 30→45초': dict(sit=45.0),
            '시작 좌석 3→5': dict(start_seats=5),
            '스폰 간격 ×0.7': dict(spawn_mult=0.7),
            '다락도 Height 단서': dict(loft_clue=True),
            '선호 시설 단서 1.5→3.0': dict(fac_clue=3.0),
        }
        for name, kw in tests.items():
            s = summarize(batch('average', 1, NS, pool, **kw))
            print(f'{name:28s} 평균 {s["mean"]:5.1f} (Δ{s["mean"] - base["mean"]:+.1f})  중앙 {s["med"]}  p90 {s["p90"]}  미도달 {s["miss"]}')
        b2 = summarize(batch('average', 2, NS, pool))
        for name, kw in {'2x 기준': {}, '2x + 고양이 행동도 배속': dict(scale_cats=True), '2x + 시작 좌석 5': dict(start_seats=5)}.items():
            s = summarize(batch('average', 2, NS, pool, **kw))
            print(f'{name:28s} 평균 {s["mean"]:5.1f} (Δ{s["mean"] - b2["mean"]:+.1f})  중앙 {s["med"]}  p90 {s["p90"]}  미도달 {s["miss"]}')
    json.dump(out, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'summary.json'), 'w'), ensure_ascii=False, indent=1)
