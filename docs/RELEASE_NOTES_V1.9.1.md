# 준현 헬퍼 v1.9.1

v1.9.1은 v1.9.0 공개 후 실사용에서 확인된 세 가지 UI/동기화 회귀만 수정하는 PATCH 릴리즈다. 기능 확장이나 Scanner 인식 알고리즘 변경은 포함하지 않는다.

## 변경 사항

- Scanner 아이템 상세의 즐겨찾기 별 버튼을 인접 Wiki 버튼과 동일한 34px 높이로 맞추고 별 글리프를 중앙 정렬해 잘림을 제거했다.
- 지도 마커 창의 `탈출구` 그룹에는 donor의 실제 `PMC 탈출구`, `Scav 탈출구`, `트랜짓 탈출구` 세 체크박스만 표시한다. duplicate `탈출 / 이동`, 빈 wrapper 행, visible master checkbox는 제거했다.
- donor master extract checkbox는 사용자에게 보이지 않는 internal render gate로 유지하며 실제 donor filter handler/persistence/rendering/MiniMap refresh 의미를 보존한다.
- Main Map에서 저장된 맵 A 대신 현재 선택한 맵 B로 변경한 뒤 MiniMap을 열면 첫 화면부터 B를 사용하도록 visible Main Map selection → `MapTrackerService` 동기화 경계를 고정했다. 이미 열린 MiniMap도 이후 변경을 즉시 반영한다.

## 변경하지 않은 것

Scanner OCR threshold, matcher, candidate cap, visual recovery acceptance, capture geometry, Ground Truth, Item ID 판정, Game Content LKG/fail-closed, Ammo filtering/favorite persistence, Factory floor 및 기존 Map/MiniMap 기능 의미는 변경하지 않았다.

## 공개 검증

```text
exact product release source: 723760910ff250a515ed8db456d3f045656ecacb
main CI: 33184811972 — SUCCESS
Release workflow: 33185056113 — SUCCESS
435 passed / 0 failed / 0 skipped
Junhyun-Helper.zip: 80,540,488 bytes
SHA-256: 7a282f58d6cf2e4916c55daddf828a70643b35669bc71fbeaca1e7a4e8176f54
```

Published EXE에서 Scanner 별/Wiki 34px Render, 지도 PMC/Scav/Transit 3필터, Main Map→MiniMap selection sync, Product UI/Main Map/Factory/MiniMap, Scanner detail/Favorites/Recents, graceful shutdown, clean portable root가 모두 성공했다.
