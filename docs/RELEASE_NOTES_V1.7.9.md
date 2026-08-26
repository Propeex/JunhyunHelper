# 준현 헬퍼 v1.7.9

## Mini Scanner 표시 회귀 수정

Scanner 로그에는 아이템 인식 성공이 기록되지만 Mini Scanner 창이 열리지 않던 문제를 수정했다.

원인은 Scanner가 Item ID를 정상 확정한 뒤에도 Mini Scanner가 별도의 상단 inventory/stash OCR을 다시 수행하고, 해당 auxiliary OCR이 실패하면 이미 확정된 결과의 표시까지 막던 구조였다.

v1.7.9에서는:

- Scanner semantic success로 Item ID가 확정되면 그 결과를 Mini Scanner presentation authority로 사용한다.
- hidden Mini Scanner의 real Scanner initial show는 Tarkov client가 foreground인지 여부만 fail-closed guard로 확인한다.
- `장비`, `건강상태`, `스킬`, `지도`, `종합정보` 계열을 다시 OCR하는 보조 gate는 표시 허가 조건에서 제거한다.
- 이미 visible인 Mini Scanner는 기존처럼 새 authoritative Item success로 즉시 갱신한다.
- v1.7.2의 3회 presentation miss retention은 유지한다.

## Scanner 인식 안전 계약

이번 패치는 presentation-only 수정이며 recognition acceptance를 변경하지 않는다.

- structural floor `0.34`
- `HEADER_FRAME_LOCKED` floor `0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- OCR / matcher / visual recovery acceptance 변경 없음
- 200ms continuous observation target 유지
- false positive보다 miss 선호
- scan-time network 없음
- game memory / DLL injection / packet interception / process hook 없음

## 검증

Product smoke에 confirmed-item Mini Scanner initial visibility policy를 추가한다.

- preview 허용
- display-test 허용
- real Scanner + foreground Tarkov 허용
- real Scanner + non-foreground Tarkov 거부

기존 실제 WPF Mini Scanner render/topmost/layout smoke도 그대로 유지한다.
