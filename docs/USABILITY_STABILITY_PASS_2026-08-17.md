# Usability / stability pass — 2026-08-17

## 목적

2026-08-17 사용자 피드백 묶음의 구현/검증 상태를 다음 대화에서도 복구할 수 있도록 기록한다.

## 반영 항목

### Quest / Needed Items

- live `dialogue` availability 12건을 전수 감사했다.
- 검증된 12개 ID에만 fail-closed compatibility를 적용한다.
- 진짜 시작 Quest 3개는 dialogue gate만 제거한다.
- 나머지 Quest는 검증된 prerequisite / minimum level을 복원한다.
- unknown/new dialogue는 계속 `확인 필요`다.
- 기존 content snapshot에도 read-time으로 동일 규칙을 적용하여 강제 업데이트가 필요 없다.
- Future Needed Items는 unresolved availability를 `IndeterminatePotential`로 보호하므로 불명확한 Quest 때문에 필요한 아이템을 정리 가능으로 만들지 않는다.
- post-fix live audit에서 세 GameMode 모두 raw dialogue 12건이 compatibility 후 잔여 0건임을 확인했다.
- 남는 구조적 unresolved source는 `globalVariable` 162 Quest + availability delay 13 Quest = 175 Quest다. 실제 UI 개수는 프로필 완료/잠김/사용 불가 상태가 우선하므로 이보다 작을 수 있다.
- 세부 근거: `DIALOGUE_GATE_AUDIT_2026-08-17.md`.

### Map / MiniMap

- 제품용 Map marker settings persistence에서 hidden legacy Quest toggle이 저장값을 `true`로 덮을 수 있던 초기화 경로를 제거했다.
- Map 선택 UI와 공유 `MapTrackerService`/MiniMap map key가 서로 다른 상태로 남지 않도록 동기화 경계를 보강했다.
- 사용자 표시 `나들목` 명칭은 `인터체인지`로 정규화한다.
- Map Quest sidebar 행 높이와 checkbox / A-B-C marker / text lane을 고정하여 행별 크기 흔들림을 제거했다.
- Quest sidebar의 LayoutUpdated 보정은 ContextIdle에 batch하여 지속적인 중복 visual-tree 작업을 줄였다.

### Items

- 유동 제출 후보 item 행을 고정 높이와 고정 수량/status lane으로 정돈했다.
- 긴 이름은 ellipsis + tooltip으로 처리한다.
- Inventory 수량 변경은 더 이상 Quest workspace 전체 재계산/재렌더링을 하지 않는다.
- Hideout level 변경도 Quest availability에 영향을 주지 않으므로 Quest workspace 재렌더링을 제거했다.
- Quest 완료/실패는 실제 prerequisite와 Needed Items에 영향을 주므로 Quest + Items 재계산을 유지한다.

### Ammo

- Ammo 이름/구경 검색을 추가했다.
- 검색 결과는 기존 `AmmoRow`를 직접 참조하여 클릭 시 해당 caliber table로 이동하고 정확한 ammo row를 선택/상세 표시한다.
- 하단 `탄약 / 수급 경로 상세정보`를 접고 펼칠 수 있게 했다.
- 접을 때 detail row 자체가 `Auto/MinHeight=0`으로 축소되고 splitter가 숨겨져 표가 실제로 더 넓게 사용된다.

## 검증 결과

PR #86의 Windows CI에서 다음을 통과했다.

- Desktop Release build 성공
- unit tests: **203 passed / 0 failed / 0 skipped**
- Windows x64 self-contained single-file publish 성공
- published executable 실제 시작 성공
- Map / Factory / MiniMap runtime smoke 성공
- 정상 Main Window 종료 및 portable root 정리 검증 성공

Build warning은 기존 vendor/Tarkov-Helper 이식 코드의 allowlisted nullable / unawaited / unused-event warning뿐이며 이번 제품 코드에서 새 compile error는 없다.

별도 temporary live audit도 성공 후 workflow를 제거했다.

## 현재 상태

사용자 피드백 묶음의 코드 구현과 자동 검증은 완료됐다. 작업 브랜치는 `fix/2026-08-17-usability-stability-pass`, 검증용 draft PR은 #86이다.

최종 병합/릴리즈 단계에서는 이 브랜치의 latest CI가 다시 green인지 확인하고, release version/state 문서를 갱신한다.
