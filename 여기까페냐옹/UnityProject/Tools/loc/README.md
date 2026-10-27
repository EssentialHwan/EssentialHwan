# 번역(ko·en·zh-Hans·ja)
- 원본: Assets/Localization/strings.csv (열: key,ko,en,zh,ja) → Resources/strings.csv로 복사(전체 셋업이 동기화).
- tr1~4.py: 번역 사전(key: (en 또는 None=기존 유지, 중국어 간체, 일본어)). build_csv.py가 합쳐 CSV를 만든다.
- 새 문자열 추가 시: CSV에 직접 4개 언어를 넣거나 tr*.py에 추가 후 build_csv.py 실행.
- 중국어 폰트: Resources/Fonts/FusionPixelZh.ttf = Fusion Pixel Font 12px(OFL) 중 게임에 쓰는 글자만 담은 판.
  중국어 문장을 새로 넣으면 mkfont.py를 다시 돌려 글자를 추가해야 한다(npm @vp-tw/cjk-web-fonts-fusion-pixel-font).
- 일본어·영어는 갈무리11로 모두 표시됨.
- 용어: 냥(말끝)=喵/ニャ, 단골=熟客/常連, 꾹꾹이=踩奶/ふみふみ, 쭙쭙이=嘬嘬/ちゅぱちゅぱ, 우다다=暴冲/運動会,
  냥냥펀치=喵喵拳/ネコパンチ, 냥슐랭=喵其林/ニャシュラン, 점장(주인공 고양이)=店长/店長, 옛 사장님(원래 주인인 사람)=老店长/前の店長 — 세계관은 기획/15_세계관_스토리_정리.md
