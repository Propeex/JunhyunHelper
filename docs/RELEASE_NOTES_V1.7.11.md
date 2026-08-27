# 준현 헬퍼 v1.7.11

v1.7.11은 v1.7.10 공개 안정판 이후 확인된 사용성 문제만 수정하는 유지보수 패치입니다. 새로운 Scanner 인식 전략이나 기능 범위 확장은 포함하지 않습니다.

## 수정 사항

- **Scanner 필요 개수**
  - Item ID 확정 후 표시하는 필요 개수를 전체 요구량(`RequiredTotal`)이 아니라 현재 보유량과 FIR 조건을 반영한 실제 남은 필요량(`RemainingTotal`)으로 변경했습니다.
  - Quest/Hideout 계산 로직을 Scanner가 재구현하지 않고 기존 `ItemsWorkspace`의 canonical 결과를 그대로 사용합니다.

- **Map / Scanner 단축키 modifier 처리**
  - 등록한 키 조합에 등록하지 않은 `Ctrl`, `Alt`, `Shift`가 추가로 눌린 상태에서도 동작합니다.
  - 같은 기본 키에 여러 호환 조합이 있으면 요구 modifier가 더 많은, 즉 더 구체적인 등록 조합을 우선합니다.
  - Windows 키 modifier는 기존과 같이 지원하지 않습니다.

- **MiniMap 최초 지도 동기화**
  - MiniMap을 처음 표시하기 직전에 Main Map의 현재 선택을 `MapTrackerService`에 동기화합니다.
  - 이전 tracker 상태 때문에 첫 화면에 다른 지도가 잠깐 또는 계속 표시되는 문제를 방지합니다.

- **MiniMap 창 크기 저장**
  - MiniMap의 가로/세로 크기를 `%LocalAppData%\JunhyunHelper\minimap-window-state.json`에 저장합니다.
  - 다음 실행 시 안전 범위 내에서 해당 크기를 복원합니다.

- **일반 Tooltip 제거**
  - 프로그램 전반의 표준 WPF 설명 Tooltip이 열리지 않도록 전역 정책을 적용했습니다.
  - 지도 마커 상세정보처럼 기능 자체인 커스텀 `Popup` UI는 영향을 받지 않습니다.

## Scanner 안전 계약

이번 패치에서 Scanner의 인식 안전 기준은 변경하지 않았습니다.

- structural floor: `0.34`
- `HEADER_FRAME_LOCKED` floor: `0.68`
- continuous candidate cap: `8`
- one-shot candidate cap: `12`
- continuous observation target: `200 ms`
- matcher / visual recovery acceptance 완화 없음
- cross-frame OCR/visual identity proof 사용 없음
- Item ID 확정 전 가격·필요 개수 등 mapped metadata 사용 없음
- scan-time network identity work 없음

v1.7.11은 기존 공개 안정판의 제품 범위와 Scanner fail-closed 원칙을 그대로 유지합니다.
