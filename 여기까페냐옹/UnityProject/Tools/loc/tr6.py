# 화면 QA(4개 언어 자동 스크린샷) 후 표시 문제 수정분 — 언어별 부분 덮어쓰기 {key: {lang: value}}
# 근거는 Tools/loc/QA_LOG.md
PATCH = {
    # HUD: 버튼 폭(104/112px)에 한 줄로 — 'Options'·'Field Guide'·'Requests 0/3'이 두 줄로 접혔다
    "opt.btn":            {"en": "Menu"},
    "ui.quest.btn":       {"en": "Tasks"},
    "ui.quest.title":     {"en": "Today's Tasks"},
    "quest.done":         {"en": "Task done! {q} (+{g}G)"},
    "ui.pause.catalog":   {"en": "Journal"},
    "ui.catalog.title":   {"en": "Cat Journal"},
    "title.hint":         {"en": "ESC Settings · M Mute · C Journal · B Shop · K Kitchen"},
    # 상점 탭: 'Recipe/Furniture Upgrades'가 12px까지 줄어들었다 → 별(강화 표시)로
    "ui.shop.tab.recipe": {"en": "Recipes ★"},
    "ui.shop.tab.furnup": {"en": "Furniture ★"},
    "ui.tool.tabhint":    {"en": "Each tool adds 2 menus — more tools, easier to read tastes."},
    "ui.furnup.uses":     {"en": "Uses: {n}"},                 # 'Used 1 times' 문법 오류
    # 시설 축: 도감 칸에서 'Facility'가 11px로 줄었다. 일본어 'スポット'와 같은 감각으로
    "ui.axis.facility":   {"en": "Spot"},
    "ui.shop.type.facility": {"en": "Spot"},
    "narr.facility.generic": {"en": "{name} is using a play spot."},
    "tut.settle":         {"en": "Use today's clues to guess each cat's favorite food, seat, and spot. Learning everyone's taste is the final goal!"},
    # 주방 메뉴 고르기 창: 제목이 닫기 버튼과 겹침 / 'hint orders' 복수 오류
    "ui.tool.pick.title": {"en": "{tool}: what to make?"},
    "ui.tool.pick.vague": {"en": "Hint orders: {n} — your pick answers one"},
    # 추리 창 제목: 'What Food does…' 대문자 축 이름이 문장 가운데 들어갔다
    "ui.guess.title":     {"en": "What does {name} like? ({axis})"},
    # 냥슐랭 결과: 두 줄로 접혀 별과 붙었다
    "nyan.report.pass":   {"en": "One more star, meow! Keep it up."},
    # 정산 도장 칸(104px): 일본어 7자 → 6자
    "ui.settle.stamp":    {"ja": "今日のハンコ"},
    # 상점 카드 이름 칸(~108px, 17px 글꼴): 13~14px까지 줄던 이름을 짧게. 두 키(furn.X / furn.X.name) 모두
    "furn.furn_deco_frame.name": {'en': 'Painting'},
    "furn.furn_deco_frame": {'en': 'Painting'},
    "furn.furn_seat_corner.name": {'en': 'Nook Chair'},
    "furn.furn_seat_corner": {'en': 'Nook Chair'},
    "furn.furn_fac_plant.name": {'en': 'Houseplant'},
    "furn.furn_fac_plant": {'en': 'Houseplant'},
    "furn.furn_seat_window.name": {'en': 'Window Seat'},
    "furn.furn_seat_window": {'en': 'Window Seat'},
    "furn.furn_fac_heatpad.name": {'en': 'Heat Pad', 'ja': 'ホットマット'},
    "furn.furn_fac_heatpad": {'en': 'Heat Pad', 'ja': 'ホットマット'},
    "furn.furn_fac_wand.name": {'en': 'Auto Wand', 'ja': '電動じゃらし'},
    "furn.furn_fac_wand": {'en': 'Auto Wand', 'ja': '電動じゃらし'},
    "furn.furn_fac_wheel.name": {'ja': 'ネコ回し車'},
    "furn.furn_fac_wheel": {'ja': 'ネコ回し車'},
    # 위 이름 변경에 맞춘 대사
    "ep.cat_mackerel.regular3.02": {"en": "A heat pad and shrimp skewers… this place gets me.", "ja": "ホットマットにエビ串…この店、わかってるニャ。"},
    "ep.cat_mackerel.grad.03": {"en": "Keep my spot on the heat pad warm for me. — Tabby", "ja": "ホットマットの場所、取っといてニャ。— サバトラニャン"},
    # 이름 불일치: 졸업 편지 서명은 'Tabby'인데 이름은 'Mackerel' → 영어권에 자연스러운 Tabby로 통일
    "cat.cat_mackerel.name": {"en": "Tabby"},
    # 관계도 태그(13px 칸): 11px까지 줄던 단어를 짧게
    "ui.rel.known":  {"en": "Familiar"},
    "ui.rel.bicker": {"en": "Bickering"},
}
