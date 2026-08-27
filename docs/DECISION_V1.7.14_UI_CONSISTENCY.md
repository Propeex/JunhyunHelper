# v1.7.14 UI Consistency — 제품 결정

기준일: 2026-08-27
상태: `IMPLEMENTED / RELEASE CANDIDATE VALIDATION`

## 사용자 결정

이번 PATCH의 목적은 새 기능을 추가하는 것이 아니라, 이미 존재하는 popup·설정·검색 UI의 interaction을 같은 제품 규칙으로 정리하고 실사용에서 발견된 WPF 동작 문제를 수정하는 것이다.

확정 요구사항:

1. Ammo `즐겨찾기 선택`, `표시 열` popup은 같은 launcher 재클릭으로 닫혀야 한다.
2. Map MiniMap launcher 주변 donor 잔여 공간을 제거한다.
3. Map marker panel은 일반 화면에서 모든 현재 checkbox를 스크롤 없이 볼 수 있을 만큼 세로 공간을 확보한다.
4. `지도 마커` launcher는 평범한 JunhyunHelper Button으로 보여야 하며 접힌 상태에서 큰 빈 panel이 남지 않아야 한다.
5. Scanner Advanced는 별도 Windows 창이 아니라 Scanner Settings와 같은 MainWindow in-app overlay를 사용한다.
6. Scanner Advanced는 내용 자체의 닫기 버튼을 두지 않고 같은 launcher 재클릭 또는 backdrop/common overlay close로 닫는다.
7. Scanner hotkey editor는 Scanner Settings에 통합한다.
8. Profile Edit의 presentation도 Scanner Settings와 같은 overlay/card 계열을 사용한다.
9. Map/MiniMap Settings, Scanner Settings, Scanner Advanced, Profile Edit는 같은 overlay interaction 계약을 사용한다.
10. Quest/Hideout/Items/Ammo/Scanner 주요 검색창은 입력창 우측 내부 `×` clear affordance를 사용한다.

## 공통 overlay 계약

`MainWindow`가 사용자-facing 설정/편집 overlay의 표시 수명주기를 소유한다.

- window-backed editor는 `ToggleInAppWindowAsync`로 host한다.
- 기존 visual tree의 UIElement surface는 `ShowInAppElementAsync`로 host한다.
- 같은 key의 launcher 재클릭은 현재 overlay를 dismiss한다.
- backdrop click과 common overlay X도 같은 dismiss path를 사용한다.
- child surface가 자체 validation/save semantics를 가진 경우 `IInAppOverlayDialog`를 통해 dismiss를 중재하며 MainWindow가 그 의미를 재구현하지 않는다.
- UIElement를 임시 re-parent하는 Map Settings는 원래 visual tree 위치를 caller가 복원한다.

이 방식은 Profile / Scanner Settings / Scanner Advanced / Map-MiniMap Settings의 interaction을 통일한다.

## Ammo popup 결정

`Popup.StaysOpen=False`인 WPF popup은 launcher를 누르면 Preview 단계에서 먼저 닫힌 뒤 Button Click handler가 실행되어 다시 열릴 수 있다. 따라서 열린 popup의 launcher 재클릭은 `AmmoPage.OnPreviewMouseDown`에서:

1. 해당 popup을 닫고,
2. event를 handled 처리하며,
3. 기존 Click 재오픈 path까지 진행하지 않는다.

이것이 이번 증상의 원인 기반 수정이며 timer/delay 같은 우회는 사용하지 않는다.

## Map / MiniMap 결정

pinned donor XAML은 유지한다. JunhyunHelper first-party partial/bridge 경계에서만 presentation을 수정한다.

- 숨긴 MiniMap help button이 차지하던 자리와 donor parent Border chrome을 제거한다.
- Map marker launcher의 donor transparent local values를 clear하여 제품 기본 Button style이 적용되게 한다.
- marker panel collapsed 상태에서는 min width / padding / background / border를 제거한다.
- expanded 상태에서는 viewport 기반 세로 공간을 확보한다.
- product Map/MiniMap settings surface는 기존 donor `SettingsPanel` 및 JunhyunHelper bridge가 구성한 설정을 그대로 사용하되 오른쪽 drawer에서 MainWindow shared overlay로 이동한다.
- donor `OverlaySettingsWindow`의 historical compatibility source는 계속 빌드되지만 JunhyunHelper product hotkey/launcher의 settings authority가 아니다. 제품 hidden `Ctrl+L` settings path는 이미 비활성 상태이며 복구하지 않는다.

## Scanner 결정

이번 변경은 Scanner presentation/settings ownership만 다룬다.

- recognition pipeline은 변경하지 않는다.
- Scanner Settings가 Mini Scanner display configuration과 Scanner hotkey configuration을 함께 소유한다.
- hotkey persistence는 기존 `ScannerCoordinator` methods를 그대로 사용한다.
- Scanner Advanced는 기존 고급 action들의 behavior를 유지한 채 shared overlay에 host한다.
- standalone window 전용 owner assumptions는 MainWindow owner로 정리한다.

## 검색창 결정

`ProductSearchClearButtonBehavior`가 기존 filtering logic을 변경하지 않고 clear affordance만 추가한다.

- Quest / Hideout / Items는 class-level Loaded attachment로 기존 `SearchBox`에 적용한다.
- Ammo와 Scanner는 runtime-owned search 구성 시 명시적으로 attach한다.
- clear button은 same Grid lane에 overlay되고 오른쪽 padding을 확보하므로 별도 외부 button column을 요구하지 않는다.

## 검증

새 deterministic regression:

```text
V1714UiConsistencyContractTests
```

검증 항목:
- Ammo popup true toggle
- shared MainWindow overlay owner
- Scanner Settings hotkey ownership
- Scanner Advanced overlay/no-content-close contract
- Map launcher/panel/settings overlay contract
- Profile card presentation
- primary product search internal clear affordance

code-only candidate evidence:

```text
PR #200
head: 46a68c26d04f37fb05725310cb47a97508278679
CI run: 33059733240 — SUCCESS
407 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
actual Product UI + full Map/Factory/MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
```

첫 PR CI `33059554861`은 Scanner Advanced의 `Application.Current`가 WPF class가 아니라 `JunhyunHelper.Application` namespace로 해석되는 C# 이름 충돌 때문에 Build 단계에서 실패했다. `System.Windows.Application.Current`로 명시한 뒤 다음 full CI에서 전체 gate가 성공했다.

최종 versioned PR CI와 public release evidence는 실제 exact release source가 확정된 뒤 이 문서와 release record에 추가한다.

## 변경하지 않는 계약

- Scanner structural floor = `0.34`
- HEADER_FRAME_LOCKED floor = `0.68`
- continuous candidate cap = `8`
- one-shot candidate cap = `12`
- continuous observation target = `200ms`
- false positive보다 miss 선호
- Item ID 전에 price/needed/slot/previous-frame evidence 금지
- cross-frame OCR/visual identity proof 금지
- Needed authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- Map donor = `d933792b6042a51cea38dc44b686a096fe30de67`
- Game Content download → parse → canonical build → validation → activate / LKG 계약
