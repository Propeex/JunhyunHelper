# 준현 헬퍼 v1.8.1

## Game Content 관계 데이터 안전성 보강

v1.8.0에서 추가된 Scanner 아이템 정보 DB의 관계 데이터 보호를 기존 Game Content LKG 계약과 같은 수준으로 강화했습니다.

- 정상 v8+ baseline이 있으면 상인 구매, 상인 교환, 은신처 제작, 플리마켓 관계 개수를 각각 비교합니다.
- 새 데이터가 기존 정상 관계의 50% 미만으로 급감하면 candidate를 적용하지 않고 기존 LKG를 유지합니다.
- 상인 교환과 은신처 제작은 relation 개수뿐 아니라 내부 재료 관계의 대량 누락도 별도로 감시합니다.
- 새로 수집한 v8+ 관계 그래프에서 상인 구매/교환/제작/플리 관계 집합이 통째로 비어 있으면 fail closed합니다.
- 저장된 candidate를 다시 읽은 뒤에도 관계 무결성과 LKG completeness를 재검증합니다.
- 실제 activation 및 active snapshot recovery 경계에서도 item relationship integrity를 다시 확인합니다.
- v3~v7 구형 snapshot은 관계 그래프가 없는 legacy 데이터로 계속 정상적으로 읽을 수 있습니다.

## 유지되는 계약

- Scanner OCR/아이템 인식 임계값, matcher, visual recovery 정책은 변경하지 않았습니다.
- `structural floor 0.34`, `HEADER_FRAME_LOCKED 0.68`, continuous 8 / one-shot 12, 200ms observation target을 유지합니다.
- 관계 정보는 Item ID가 확정된 뒤 presentation에만 사용하며 identity proof로 사용하지 않습니다.
- Map/MiniMap donor revision은 변경하지 않았습니다.
- 공개 v1.8.0 ZIP/tag/source는 immutable historical release로 유지하며 교체하지 않았습니다.

## 릴리즈 검증 완료

v1.8.1은 전체 PATCH release gate를 통과해 공개 stable로 검증되었습니다.

```text
exact product source/tag target:
dade2ef4dadbf58659b75c80d421bd3738003ff8

exact-main CI: 33132600931 — SUCCESS
Release workflow: 33132798167 — SUCCESS
418 passed / 0 failed / 0 skipped
Product UI / Scanner / Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS

Junhyun-Helper.zip
bytes: 80,520,704
SHA-256:
b30cbb045cc089c90108e2d3394510ef6778019ea0a50f6ae16d14de7aaafe9a
```

GitHub `/releases/latest`, `v1.8.1` release target, tag ref, 공개 ZIP asset digest를 readback했으며 모두 exact-main source/package와 일치합니다.

상세 증거는 `docs/RELEASE_1.8.1.md`와 `docs/.release-v1.8.1-status.json`을 따릅니다.
