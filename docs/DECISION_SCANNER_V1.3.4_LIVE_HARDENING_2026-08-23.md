# DECISION — Scanner v1.3.4 live recognition hardening

날짜: 2026-08-23

상태: **CONFIRMED / IMPLEMENTED / RELEASE CANDIDATE**

## 배경

v1.3.3 공개 후 실제 Tarkov 사용 피드백에서 다음 네 문제가 재현되었습니다.

1. `Esmarch 에스마르호 지혈대` 계열에서 WinRT OCR이 lower-case Latin glyph를 `「` 같은 현재 공식 카탈로그 밖 문장부호로 반복 출력하는 사례
2. title glyph가 magnifier처럼 보이며 search icon 후보로 승격되는 사례
3. 초기 structural detail-window rectangle이 실제 inspect header 경계와 어긋난 상태로 진단/후속 처리에 남는 사례
4. 진단창 화면에는 색상 rectangle이 보이지만 사용자 저장 PNG에는 raw frame만 기록되어 피드백 이미지에서 실제 detector 좌표를 확인할 수 없는 문제

이 변경은 새로운 사용자 기능을 추가하는 것이 아니라 기존 Scanner의 실전 recognition/diagnostics 결함을 수정하는 PATCH입니다.

## 결정 1 — embedded impossible punctuation을 특정 문자로 치환하지 않는다

`「 = r` 같은 고정 치환은 금지합니다.

대신 현재 공식 카탈로그에 존재하지 않는 기호가 **영숫자 사이에 정확히 한 번** 나타난 경우 ordinary sanitized matcher text와 별도로 one-unknown-glyph pattern을 보존합니다.

예:

```text
raw OCR:        Esma「ch 에스마르호 지혈대
ordinary text:  Esmach 에스마르호 지혈대
unknown pattern: Esma?ch 에스마르호 지혈대
```

`?`는 특정 글자를 뜻하지 않고 그 위치에 OCR이 판별하지 못한 한 glyph가 있었다는 evidence만 뜻합니다.

복구 조건:

```text
normalized pattern length >= 7
AND exactly one unknown glyph
AND complete current official catalog에서 exact-slot candidate가 정확히 하나
AND duplicate official name이 아님
AND best - global wildcard runner-up >= 10 percentage points
```

조건을 충족하지 않으면 기존 원칙대로 fail closed합니다.

## 결정 2 — magnifier는 fixed search-icon lane + normalized shape template가 소유한다

일반 bright connected-component 후보끼리 경쟁하여 magnifier를 고르지 않습니다.

`HEADER_FRAME_LOCKED`의 long neutral top frame과 red close/X로부터 실제 search icon이 존재해야 하는 frame-left lane을 계산하고, 그 lane 안의 작은 patch만 magnifier 후보가 될 수 있습니다.

magnifier template evidence:

- circular/ring bright band
- dark/hollow center
- lower-right diagonal handle
- ring 밖 background
- expected header-relative location/size

제목 glyph가 ring처럼 보여도 fixed lane 밖이면 magnifier 후보가 될 수 없습니다.

## 결정 3 — close/X도 color blob 하나가 아니라 shape evidence를 결합한다

red close control은 다음을 함께 사용합니다.

- red dominance/body
- expected right/top geometry
- diagonal X contrast
- compactness/fill

따라서 단순 red component가 header authority를 얻지 못하도록 합니다.

## 결정 4 — full header lock을 통과한 후보만 OCR identity path에 남긴다

다음 조건이 충족되지 않은 structural candidate는 Scanner candidate 목록에서 제거합니다.

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND valid magnifier bounds
AND valid close bounds
```

partial header structure를 가진 후보에서 OCR/identity를 계속 시도하지 않습니다.

## 결정 5 — detail-window 진단 bounds는 locked header로 다시 정렬한다

초기 structural rectangle은 candidate discovery 용도입니다.

full header lock 이후에는 magnifier/X의 실측 위치에서 inspect header의 top/left/right를 다시 계산하여 최종 selected detail bounds와 geometry signature를 정렬합니다. Window bottom은 아이템별 stat panel 높이가 달라질 수 있으므로 기존 structural bottom을 보수적으로 유지합니다.

## 결정 6 — 사용자 export PNG에는 detector rectangle을 실제 픽셀에 합성한다

자동 screenshot persistence는 계속 금지합니다.

사용자가 `이미지 저장`을 명시적으로 선택했을 때만 PNG를 생성하며, 다음 색상 rectangle을 실제 캡처에 합성합니다.

- 초록: selected detail window
- 파랑: OCR title ROI
- 노랑: magnifier
- 빨강: close/X

이렇게 저장된 PNG 하나만으로도 detector/ROI 좌표 drift를 후속 분석할 수 있어야 합니다.

## 유지되는 불변 조건

- false positive보다 miss 선호
- current official Korean item catalog가 Item identity 권위
- 일반 confidence threshold 완화 없음
- ordinary fuzzy top1/top2 margin 완화 없음
- bounded unique one-edit recovery 조건 완화 없음
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- icon 단독 identity 금지
- 최고 상점가 / flea `avg24hPrice` / `RequiredTotal` 의미 변경 없음
- Content schema v7 / user.db v1 / Scanner display settings v4 / Scanner catalog cache schema 변경 없음

## 검증

첫 통합 Windows gate:

```text
PR: #146
CI run: 32635992721 — SUCCESS
Release build: SUCCESS
automated tests: 267 passed / 0 failed / 0 skipped
win-x64 publish: SUCCESS
packaged EXE Product UI + Scanner + Mini Scanner + Main Map + Factory + MiniMap smoke: SUCCESS
diagnostic PNG overlay renderer smoke: SUCCESS
graceful shutdown: SUCCESS
```

최종 v1.3.4 exact release source / public asset 검증 값은 `docs/RELEASE_1.3.4.md`에 기록합니다.
