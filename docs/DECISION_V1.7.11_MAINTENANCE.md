# v1.7.11 Maintenance Product Decisions

기준일: 2026-08-27
상태: **APPROVED / IMPLEMENTATION TARGET**

이 문서는 v1.7.11 유지보수 작업에서 사용자와 확정한 제품 동작을 기록한다. 아래 항목과 충돌하는 과거 문서의 표현은 이 결정이 우선한다.

## 1. Scanner 필요 개수

Scanner / Mini Scanner의 `필요 개수`는 Item ID 확정 뒤 `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`을 표시한다.

- `RequiredTotal`은 전체 요구량이며 Scanner의 사용자 표시값이 아니다.
- 현재 Inventory와 FIR 조건을 반영한 canonical Needed Items 계산 결과를 그대로 사용한다.
- Scanner가 Quest/Hideout/Inventory 계산을 별도로 재구현하지 않는다.
- 이 값은 Item identity 증거가 아니며 Item ID 확정 전에는 읽거나 사용하지 않는다.

이 결정은 `docs/PRODUCT.md`와 과거 결정 문서에 남아 있는 `RequiredTotal` 표시 요구를 해당 부분에 한해 명시적으로 대체한다.

## 2. configurable hotkey modifier matching

Map과 Scanner의 configurable hotkey는 다음 규칙을 사용한다.

- 등록된 primary key는 일치해야 한다.
- 등록된 `Ctrl` / `Alt` / `Shift`는 모두 눌려 있어야 한다.
- 등록하지 않은 `Ctrl` / `Alt` / `Shift`가 추가로 눌린 것은 허용한다.
- 같은 primary key에 여러 compatible binding이 있으면 required modifier 수가 더 많은, 즉 더 구체적인 binding을 우선한다.
- 동률은 기존 기능 우선순위/안정적인 등록 순서로 결정하며 임의로 중복 실행하지 않는다.
- Windows modifier는 지원하지 않는다.
- Map의 bare NumPad 0–5 floor selection 계약은 변경하지 않는다.

## 3. MiniMap first-open synchronization

MiniMap이 처음 표시되기 전에 현재 Main Map UI 선택을 shared `MapTrackerService`에 동기화한다.

목표는 MiniMap 첫 프레임부터 사용자가 Main Map에서 보고 있는 지도와 일치하게 하는 것이다. 이전 tracker state를 첫 표시 source로 사용해서는 안 된다.

## 4. MiniMap window-size persistence

MiniMap 창의 width/height는 mutable user preference다.

- `%LocalAppData%/JunhyunHelper` 아래 first-party user-data file에 저장한다.
- 프로그램 재시작 뒤 복원한다.
- donor가 정의한 안전한 최소/최대 크기로 clamp한다.
- 프로그램 업데이트가 이 값을 덮어쓰지 않는다.

## 5. standard Tooltip removal

표준 WPF `ToolTip`으로 나타나는 설명 UI는 제품 전역에서 표시하지 않는다.

- 흰색 기본 Tooltip을 다크 테마로 바꾸는 것이 아니라 열리지 않게 한다.
- 버튼/검색/설정 등의 설명용 standard Tooltip이 대상이다.
- 지도 marker detail처럼 기능 자체인 custom `Popup`/information surface는 유지한다.

## 6. Scanner safety invariants

v1.7.11은 Scanner identity recognition을 조정하는 릴리즈가 아니다. 다음 값과 원칙을 변경하지 않는다.

- structural floor `0.34`
- `HEADER_FRAME_LOCKED` floor `0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- continuous observation target `200 ms`
- false positive보다 miss 선호
- cross-frame OCR/visual result를 Item identity proof로 사용하지 않음
- Item ID 확정 전 mapped metadata를 identity proof로 사용하지 않음
- scan-time network identity work 없음
- matcher / visual recovery acceptance 완화 없음

## 7. release classification

이 변경은 기존 기능의 잘못된 표시/동기화/입력 UX를 바로잡는 patch-level maintenance이며 목표 버전은 **v1.7.11**이다.
