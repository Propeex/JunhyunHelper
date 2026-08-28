# DECISION — v1.9.1 Final UI / MiniMap Synchronization

상태: **CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED v1.9.1**

기준일: 2026-08-29 KST

## Scanner detail action

- 즐겨찾기 별 버튼과 Wiki 버튼의 제품 높이는 34px다.
- 별은 `Segoe UI Symbol` 기반, zero padding, 중앙 정렬을 사용한다.
- 실제 Render lifecycle에서 높이/정렬을 검증한다. Collapsed 상태의 `ActualHeight=0`을 실패로 취급하지 않는다.
- favorite persistence/canonical Item ID 의미는 변경하지 않는다.

## Map extract group

사용자-visible `탈출구` 그룹에는 정확히 다음 donor checkbox 세 개만 표시한다.

- PMC 탈출구
- Scav 탈출구
- 트랜짓 탈출구

별도의 visible master checkbox나 duplicate row를 만들지 않는다. donor `ChkShowExtractMarkers`는 hidden internal render gate로 유지하며 활성 상태를 보장한다. donor Checked/Unchecked handler, settings persistence, marker rendering, MiniMap refresh 의미를 유지한다.

## Main Map / MiniMap map selection

- visible Main Map selector가 현재 지도 선택의 사용자-facing source다.
- 선택을 canonical map key로 해석해 shared `MapTrackerService`에 동기화한다.
- MiniMap을 active product window로 등록하기 전에 현재 Main Map selection을 tracker에 반영한다.
- donor Loaded 뒤에도 동일 boundary를 한 번 더 적용한다.
- 이미 열린 MiniMap도 Main Map 변경을 즉시 반영한다.
- Factory floor / map marker / viewport 의미는 변경하지 않는다.

## Runtime evidence policy

v1.9.1부터 Scanner detail action render와 MiniMap map-selection sync marker를 CI에서 optional diagnostic이 아니라 required fail-closed published-EXE evidence로 취급한다.

Public proof:

```text
exact product release source/tag target:
723760910ff250a515ed8db456d3f045656ecacb
main CI: 33184811972 — SUCCESS
Release workflow: 33185056113 — SUCCESS
435 passed / 0 failed / 0 skipped
public ZIP SHA-256:
7a282f58d6cf2e4916c55daddf828a70643b35669bc71fbeaca1e7a4e8176f54
```

## 비변경 영역

- Scanner OCR threshold / matcher / candidate cap
- visual corroboration / recovery acceptance
- capture geometry / Ground Truth
- Scanner Item ID identity policy
- Game Content LKG / completeness / fail-closed
- Scanner Favorites / Recents 의미
- Ammo filtering / favorite persistence
- Map / Factory / MiniMap 기존 기능 의미
