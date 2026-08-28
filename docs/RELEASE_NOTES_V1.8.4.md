# 준현 헬퍼 v1.8.4

## 요약

v1.8.4는 Ammo 툴바 배치와 Scanner 아이템 상세 표시를 정리하는 PATCH 릴리즈다. Scanner 인식 정책, Game Content 안전 계약, Map/MiniMap 기능 의미는 변경하지 않는다.

## Ammo

- 즐겨찾기 선택을 왼쪽 선택 영역에 유지한다.
- `표시 열` 버튼을 툴바 오른쪽 끝에 표시한다.
- 구경/즐겨찾기 드롭다운은 기존 공유 탄약 아이콘과 순환 애니메이션을 그대로 사용한다.

## Scanner 아이템 상세

- 기본 정보는 크기, 플리마켓 평균가, 최고 상인 판매가, 현재 필요한 개수의 네 항목만 표시한다.
- Quest/Hideout 사용처는 기존 이동 동작을 유지한다.
- 제작/교환은 결과 아이템과 전체 재료를 레시피 카드로 표시한다.
- 재료가 좁은 폭을 넘으면 다음 줄로 자동 배치된다.
- 제작/교환 관련 아이템을 클릭하면 같은 Scanner 아이템 상세로 이동한다.
- 수급처는 제작, 교환, 구매, 레이드 획득으로 구분한다.
- 데이터가 없는 빈 구역은 표시하지 않는다.

## 안전성 및 검증

- Scanner OCR threshold, matcher, candidate cap, visual recovery acceptance를 변경하지 않았다.
- Game Content candidate/LKG/50% completeness/fail-closed 계약을 변경하지 않았다.
- actual self-contained Windows EXE에서 Ammo animated dropdown, Ammo toolbar layout, Scanner item-detail control tree를 직접 검사한다.
- CI는 각 runtime smoke marker가 존재해야 통과한다.
- 기능 후보 기준 424개 테스트가 모두 통과했다.
- 2026-08-28 현재 json.tarkov.dev Regular/PvE live canonical probe도 fatal 0으로 통과했다.

최종 공개 source commit, main CI, release workflow, ZIP SHA-256, release/asset ID는 공개 후 `docs/RELEASE_1.8.4.md`와 `docs/STATE.md`에 기록한다.
