# 준현 헬퍼 v1.7.12

v1.7.12는 새로운 사용자 기능을 추가하지 않는 장기 유지보수 패치입니다. 기존 기능과 UX를 유지하면서 Desktop 초기화 ownership과 WPF lifecycle 결합을 정리했습니다.

## 유지보수 개선

- **Desktop page infrastructure ownership**
  - Quest/Hideout/Items/Ammo image cache, Ammo favorites store, 화면 간 navigation wiring을 개별 Page `Loaded` 순서가 아니라 `MainWindow`의 product initialization 경계에서 연결합니다.
  - 특정 탭이 먼저 `Loaded`되어야 다른 탭의 infrastructure가 준비되는 간접 의존을 제거했습니다.

- **Ammo presentation lifecycle**
  - Ammo 검색/상세정보 presentation 초기화가 부모 XAML의 incidental `Loaded` subscription과 class-level `Loaded` handler에 간접 의존하던 구조를 제거했습니다.
  - Ammo 화면이 자체 `OnInitialized`에서 Loaded-priority dispatcher 작업을 명시적으로 예약해 자기 presentation lifecycle을 직접 소유합니다.

- **회귀 방지**
  - `DesktopStartupWiringContractTests`가 product/page ownership과 제거된 Loaded handler가 되살아나지 않는 계약을 고정합니다.
  - 첫 정리안에서 자동 테스트가 잡지 못한 WPF lifecycle 회귀를 실제 published EXE Product UI smoke가 검출했고, 수정 후 같은 smoke로 복구를 검증했습니다.

## 변경하지 않은 것

- 사용자-visible 기능/동작
- Core/Application/Infrastructure의 domain/data ownership
- Game Content download/validation/LKG activation 계약
- Map/MiniMap pinned donor revision
- Scanner recognition 전략 및 안전 기준

Scanner 안전 기준은 그대로입니다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

cross-frame OCR/visual identity proof, 가격/필요 개수 기반 identity 판정, scan-time network identity work를 추가하지 않았습니다.
