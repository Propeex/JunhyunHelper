# Usability / stability pass — 2026-08-17

## 목적

2026-08-17 사용자 피드백 묶음의 구현 상태를 다음 대화에서도 복구할 수 있도록 기록한다.

## 반영 항목

### Quest / Needed Items

- live `dialogue` availability 12건을 전수 감사했다.
- 검증된 12개 ID에만 fail-closed compatibility를 적용한다.
- 진짜 시작 Quest 3개는 dialogue gate만 제거한다.
- 나머지 Quest는 검증된 prerequisite / minimum level을 복원한다.
- unknown/new dialogue는 계속 `확인 필요`다.
- 기존 content snapshot에도 read-time으로 동일 규칙을 적용하여 강제 업데이트가 필요 없다.
- Future Needed Items는 unresolved availability를 `IndeterminatePotential`로 보호하므로 불명확한 Quest 때문에 필요한 아이템을 정리 가능으로 만들지 않는다.
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

## 아직 검증해야 하는 항목

- 전체 Release build / unit tests
- Windows Map/MiniMap smoke
- live content three-mode validation 후 `확인 필요` 수 재측정
- Map selector → tracker → MiniMap map identity 회귀 확인
- UI 동적 생성 코드가 Windows WPF build에서 warning/error 없이 통과하는지 확인

이 문서는 구현 중 상태 기록이며 최종 릴리즈 완료 문서가 아니다. CI/스모크 결과에 따라 이 상태를 갱신한다.
