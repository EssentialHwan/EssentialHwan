// 여기카페냐옹 v0.9.8 플레이테스트 설문 — 구글 폼 자동 생성
// 쓰는 법: 시트 "여기까페냐옹 플레이테스트" 열기 → 확장 프로그램 → Apps Script → 이 파일 내용 붙여넣기 → createPlaytestForm 실행
//   (처음 실행 때 권한 허용 창이 뜹니다 — 본인 계정으로 직접 허용)
// 결과: 새 폼이 만들어지고, 응답은 이 시트의 새 탭 "폼 응답"으로 들어옵니다. 로그에 공유 링크가 찍힙니다.
function createPlaytestForm() {
  var ss = SpreadsheetApp.getActiveSpreadsheet();
  var f = FormApp.create('여기카페냐옹 플레이테스트 (v0.9.8)');
  f.setDescription('플레이해 줘서 고마워요! 5분이면 끝나요.\n정답은 없어요 — 지루했으면 지루했다고, 별로면 별로라고 써 주는 게 제일 도움이 됩니다.');
  f.setCollectEmail(false).setLimitOneResponsePerUser(false).setProgressBar(true);

  f.addTextItem().setTitle('닉네임').setRequired(true);
  f.addMultipleChoiceItem().setTitle('어디까지 했나요?')
    .setChoiceValues(['1~2일차', '3~4일차', '5~6일차', '7일차(끝까지)']).setRequired(true);
  f.addMultipleChoiceItem().setTitle('플레이 시간은?')
    .setChoiceValues(['30분 미만', '30분~1시간', '1~2시간', '2시간 이상']).setRequired(true);

  f.addSectionHeaderItem().setTitle('느낌');
  f.addParagraphTextItem().setTitle('친구한테 이 게임을 한 문장으로 설명한다면?').setRequired(true);
  f.addScaleItem().setTitle('고양이들이 귀여웠나요?').setBounds(1, 5).setLabels('별로', '너무 귀여움').setRequired(true);
  f.addListItem().setTitle('제일 좋았던 고양이').setChoiceValues(['치즈냥','삼색냥','젖소냥','까망냥','턱시도냥','고등어냥','샴냥','회색냥','달냥','폴드냥','먼치냥','페르냥','카오스냥','스핑냥','꼬물냥','어르신냥','냥슐랭 심사원']);
  f.addMultipleChoiceItem().setTitle('"손님마다 취향이 다르다"는 걸 언제 알았나요?')
    .setChoiceValues(['1일차에 바로', '2~3일차쯤', '나중에야', '잘 모르겠다']).setRequired(true);
  f.addGridItem().setTitle('각 부분은 어땠나요?')
    .setRows(['관찰(고양이 지켜보기)', '취향 맞히기', '주방(메뉴 만들기)', '가구 배치', '마감 정산'])
    .setColumns(['재미없음', '보통', '재밌음', '안 해봄']);
  f.addMultipleChoiceItem().setTitle('취향 맞히기 난이도').setChoiceValues(['너무 쉬움', '적당', '너무 어려움', '안 해봄']);

  f.addSectionHeaderItem().setTitle('막힌 곳');
  f.addParagraphTextItem().setTitle('뭘 해야 할지 몰라 막혔던 순간이 있었나요? (언제, 어디서)');
  f.addParagraphTextItem().setTitle('지루해지거나 그만두고 싶었던 순간은?');
  f.addParagraphTextItem().setTitle('버그나 이상한 화면 (고양이 깨짐, 그림자, 글자 겹침 등) — 몇 일차·어디서였는지도');

  f.addSectionHeaderItem().setTitle('마지막');
  f.addScaleItem().setTitle('내일 또 켜고 싶은가요?').setBounds(1, 5).setLabels('아니요', '당연히').setRequired(true);
  f.addMultipleChoiceItem().setTitle('스팀에서 얼마면 사겠어요?')
    .setChoiceValues(['안 산다', '세일 때만 (~5,000원)', '~9,000원', '~13,000원', '그 이상도 OK']).setRequired(true);
  f.addParagraphTextItem().setTitle('하고 싶은 말 아무거나');

  f.setDestination(FormApp.DestinationType.SPREADSHEET, ss.getId());
  Logger.log('공유 링크: ' + f.getPublishedUrl());
  Logger.log('편집 링크: ' + f.getEditUrl());
}
