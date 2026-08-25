# 준현 헬퍼 v1.7.2 Release Notes

기준일: 2026-08-25

## Mini Scanner 표시 안정성 개선

v1.7.2는 새 기능 추가가 아니라 Mini Scanner의 표시 생명주기 회귀를 수정하는 PATCH 릴리즈다.

실사용에서 Scanner Item identity 로그는 같은 아이템을 연속 성공으로 식별하고 있는데도 Mini Scanner가 반복적으로 표시/숨김되는 문제가 확인되었다.

원인은 Item identity recognition과 별개로 수행되는 inventory/stash context OCR 및 continuous runtime의 중간 상태가 이미 표시 중인 결과를 즉시 숨길 수 있었기 때문이다.

### 변경 후 동작

- Item A가 정상 확정되면 A 정보를 계속 표시한다.
- 같은 A가 다시 정상 확정되면 표시를 유지하고 presentation miss budget을 0으로 되돌린다.
- 일시적인 실제 인식 실패 1회와 2회는 A 정보를 숨기지 않는다.
- 실제 인식 실패가 3회 연속 누적될 때 A 표시를 종료한다.
- A 표시 중 Item B가 정상 확정되면 기다리지 않고 즉시 B 정보로 교체한다.
- `상세창 후보 확인`, `아이템 제목 변화 확인`, `아이템 이름 읽는 중` 같은 pipeline 진행 상태는 실패 횟수로 계산하지 않는다.

### Inventory/stash context OCR

- hidden Mini Scanner의 최초 표시는 기존처럼 foreground Tarkov inventory/stash context를 확인한 뒤 허용한다.
- 최초 안전 게이트를 통과해 Item이 이미 표시된 이후에는 authoritative Scanner Item success가 presentation liveness의 기준이다.
- 보조 context OCR 한 번의 `false` 또는 일시적 오류 때문에 이미 표시 중인 정상 결과를 숨기지 않는다.
- Mini Scanner가 정상적으로 숨겨진 뒤 다시 표시할 때는 최초 context gate를 다시 적용한다.

### Recognition safety 유지

이번 변경은 UI presentation 안정성에만 한정한다.

다음 Scanner recognition 계약은 변경하지 않았다.

- structural floor `0.34`
- `HEADER_FRAME_LOCKED >= 0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- continuous scan interval `350 ms`
- semantic retry interval `1200 ms`
- false positive보다 miss 선호
- current official Item catalog identity authority
- cross-frame OCR identity proof 재사용 금지

Scanner 내부 verified identity의 기존 보수적 geometry miss 경계(`MissesToHide = 2`)도 유지하고, Mini Scanner presentation만 별도 3-miss latch로 안정화한다.

## 회귀 검증

새 deterministic `ScannerPresentationRetention` 테스트가 다음 상태 전이를 고정한다.

- A 확정 → miss 1/2에서도 A 유지
- miss 3 → clear/hide
- 동일 A 재확정 → miss budget reset
- 다른 B 확정 → 즉시 B로 교체 및 budget reset
- hard reset → 즉시 clear
- Item이 없는 상태의 miss → 잘못된 held state 생성 금지

최종 공개 릴리즈 검증 결과는 `docs/DECISION_V1.7.2_MINI_SCANNER_STABILITY_2026-08-25.md`에 기록한다.
