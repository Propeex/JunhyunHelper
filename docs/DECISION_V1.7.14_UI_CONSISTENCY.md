# v1.7.14 UI Consistency — 제품 결정

기준일: 2026-08-27
상태: `IMPLEMENTED / PUBLIC VERIFIED v1.7.14`

## 사용자 결정

이번 PATCH의 목적은 새 기능을 추가하는 것이 아니라, 이미 존재하는 popup·설정·검색 UI의 interaction을 같은 제품 규칙으로 정리하고 실사용에서 발견된 WPF 동작 문제를 수정하는 것이다.

확정 요구사항:

1. Ammo `즐겨찾기 선택`, `표시 열` popup은 같은 launcher 재클릭으로 닫혀야 한다.
2. Map MiniMap launcher 주변 donor 잔여 공간을 제거한다.
3. Map marker panel은 일반 화면에서 현재 checkbox를 가능한 한 스크롤 없이 볼 수 있을 만큼 세로 공간을 확보한다.
4. `지도 마커` launcher는 평범한 JunhyunHelper Button으로 보여야 하며 접힌 상태에서 큰 빈 panel이 남지 않아야 한다.
5. Scanner Advanced는 별도 Windows 창이 아니라 Scanner Settings와 같은 MainWindow in-app overlay를 사용한다.
6. Scanner Advanced는 내용 자체의 닫기 버튼을 두지 않고 같은 launcher 재클릭 또는 backdrop/common overlay close로 닫는다.
7. Scanner hotkey editor는 Scanner Settings에 통합한다.
8. Profile Edit presentation도 Scanner Settings와 같은 overlay/card 계열을 사용한다.
9. Map/MiniMap Settings, Scanner Settings, Scanner Advanced, Profile Edit는 같은 overlay interaction 계약을 사용한다.
10. Quest/Hideout/Items/Ammo/Scanner 주요 검색창은 입력창 우측 내부 `×` clear affordance를 사용한다.

## 공통 overlay 계약

`MainWindow`가 사용자-facing 설정/편집 overlay의 표시 수명주기를 소유한다.

- Window-backed editor는 `ToggleInAppWindowAsync`로 host한다.
- 기존 visual tree의 UIElement surface는 `ShowInAppElementAsync`로 host한다.
- 같은 key의 launcher 재클릭은 current overlay를 dismiss한다.
- backdrop click과 common overlay X도 같은 dismiss path를 사용한다.
- child surface가 자체 validation/save semantics를 가진 경우 `IInAppOverlayDialog`를 통해 dismiss를 중재하며 MainWindow가 그 의미를 재구현하지 않는다.
- UIElement를 temporary re-parent하는 Map Settings는 original visual tree 위치를 caller가 복원한다.

현재 이 계약을 사용하는 주요 surface:

- Profile Edit
- Scanner Settings
- Scanner Advanced
- Map / MiniMap Settings

## Ammo popup 결정

`Popup.StaysOpen=False`인 WPF popup은 launcher를 누르면 Preview 단계에서 먼저 닫힌 뒤 Button Click handler가 실행되어 다시 열릴 수 있다.

따라서 이미 열린 popup의 launcher 재클릭은 `AmmoPage.OnPreviewMouseDown`에서:

1. target popup을 닫고,
2. event를 handled 처리하며,
3. 기존 Click 재오픈 path까지 진행하지 않는다.

이것이 증상의 원인 기반 수정이며 timer/delay 우회는 사용하지 않는다.

## Map / MiniMap 결정

Pinned donor XAML/source는 유지한다. JunhyunHelper first-party partial/bridge 경계에서만 presentation을 수정한다.

- hidden MiniMap help button이 차지하던 자리와 donor parent Border chrome을 제거한다.
- Map marker launcher의 donor transparent local values를 clear하여 product default Button style이 적용되게 한다.
- marker panel collapsed 상태에서는 min width / padding / background / border를 제거한다.
- expanded 상태에서는 map viewport 기반 bounded vertical space를 확보한다.
- product Map/MiniMap Settings surface는 기존 donor `SettingsPanel`과 JunhyunHelper bridge가 구성한 settings authority를 그대로 사용하되 오른쪽 drawer에서 MainWindow shared overlay로 이동한다.
- overlay completion 뒤 `SettingsPanel`을 original parent/index에 복원한다.
- donor `OverlaySettingsWindow` historical compatibility source는 제품 hotkey/launcher settings authority가 아니다.
- hidden legacy settings path를 제품 동작으로 복구하지 않는다.

## Scanner 결정

이번 변경은 Scanner presentation/settings ownership만 다룬다.

- recognition pipeline은 변경하지 않는다.
- Scanner Settings가 Mini Scanner display configuration과 Scanner global hotkey configuration을 함께 소유한다.
- hotkey persistence는 existing `ScannerCoordinator` methods를 그대로 사용한다.
- old dedicated `ScannerHotkeySettingsWindow.xaml/.cs`는 제거한다.
- Scanner Advanced는 existing advanced actions를 유지한 채 shared overlay에 host한다.
- Scanner Advanced content-local `닫기` button은 제거한다.
- standalone product Window path를 복원하지 않는다.

## 검색창 결정

`ProductSearchClearButtonBehavior`가 기존 filtering logic을 변경하지 않고 clear affordance만 추가한다.

- Quest / Hideout / Items는 class-level Loaded attachment로 existing `SearchBox`에 적용한다.
- Ammo와 Scanner는 runtime-owned search 구성 시 명시적으로 attach한다.
- clear button은 same Grid lane의 오른쪽에 overlay되고 input padding을 확보한다.
- clear 후 search box focus를 유지한다.

## deterministic regression

```text
V1714UiConsistencyContractTests
```

검증 항목:

- Ammo popup true-toggle
- shared MainWindow overlay owner
- Scanner Settings hotkey ownership
- Scanner Advanced overlay/no-content-close contract
- old dedicated Scanner hotkey Window 재도입 금지
- Map launcher/panel/settings overlay contract
- Profile card presentation
- primary product search internal clear affordance

## PR 검증

첫 PR CI:

```text
run: 33059554861
result: FAILED at Build
```

원인:

`ScannerAdvancedWindow`의 `Application.Current`가 WPF `System.Windows.Application`이 아니라 `JunhyunHelper.Application` namespace로 해석되는 C# 이름 충돌이었다.

수정:

```text
System.Windows.Application.Current
```

으로 명시했다.

Code-only full candidate:

```text
CI run: 33059733240 — SUCCESS
407 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
actual Product UI + full Map/Factory/MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
```

Final PR #200:

```text
final head: 1a2f0189c6a6f2a21dc70f50cb092217f0977c13
final PR CI: 33060440860 — SUCCESS
407 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
actual Product UI + Scanner + Main Map + Factory + MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
```

Actual Product UI smoke는 Scanner Advanced를 standalone Window로 검사하지 않고 실제 `MainWindow` shared overlay에 host한 상태로 렌더링/닫기 계약을 검증한다.

## exact product release / public verification

PR #200 merge / exact v1.7.14 product source:

```text
0a51375de36cd13047216006c2c0311728b1bd89
```

Main CI:

```text
run: 33060827905
CI #2053
result: SUCCESS
407 passed / 0 failed / 0 skipped
ProductVersion: 1.7.14+0a51375de36cd13047216006c2c0311728b1bd89
Windows x64 publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
package verification: SUCCESS
```

Main-CI stable package:

```text
Junhyun-Helper.zip
bytes: 80,488,363
SHA-256: 341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
```

Release workflow:

```text
run: 33061059154
Release #45
result: SUCCESS
```

Public readback:

```text
release id: 377720327
tag: v1.7.14
tag/release target: 0a51375de36cd13047216006c2c0311728b1bd89
draft: false
prerelease: false
latest stable: true
asset id: 532104142
asset bytes: 80,488,363
asset SHA-256: 341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
```

Public ZIP digest는 exact main-CI package SHA-256과 일치한다.

상세 evidence는 `docs/RELEASE_1.7.14.md`와 `docs/.release-v1.7.14-status.json`에 기록한다.

이후 documentation-only commit은 v1.7.14 product release source가 아니다. Published v1.7.14 tag/source/assets는 immutable historical product release다.

## 변경하지 않는 계약

- Scanner structural floor = `0.34`
- HEADER_FRAME_LOCKED floor = `0.68`
- continuous candidate cap = `8`
- one-shot candidate cap = `12`
- continuous observation target = `200ms`
- false positive보다 miss 선호
- Item ID 전에 price/needed/slot/source/previous-frame evidence 금지
- cross-frame OCR/visual identity proof 금지
- Needed quantity authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- Needed source authority = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`
- Map donor = `d933792b6042a51cea38dc44b686a096fe30de67`
- Game Content download → parse → canonical build → validation → activate / LKG 계약
