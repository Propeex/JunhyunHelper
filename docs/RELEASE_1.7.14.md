# 준현 헬퍼 v1.7.14 — verified release record

기준일: 2026-08-27
상태: **PUBLIC STABLE / VERIFIED**

## 목적

v1.7.14는 새 도메인 기능이나 Scanner identity 정책을 추가하지 않고, 실사용에서 확인된 UI interaction 불일치와 WPF popup/overlay 동작을 정리한 PATCH release다.

주요 범위는 Ammo popup true-toggle, Map/MiniMap launcher 및 marker panel 정리, Map/MiniMap/Scanner/Profile의 MainWindow 공통 in-app overlay 사용, Scanner hotkey 설정 통합, 주요 검색창의 입력창 내부 clear affordance 통일이다.

Scanner recognition threshold/candidate cap/matcher/visual acceptance, Needed Items authority, Map/MiniMap donor revision, Game Content validation/LKG 계약은 변경하지 않았다.

## 제품 변경

1. Ammo
   - `즐겨찾기 선택`, `표시 열` popup은 열린 상태에서 같은 launcher를 다시 누르면 닫힌 상태를 유지한다.
   - `Popup.StaysOpen=False`가 Preview 단계에서 먼저 닫힌 뒤 기존 Click handler가 다시 여는 WPF interaction을 원인 수준에서 차단한다.
2. Map / MiniMap
   - MiniMap launcher 주변 donor 잔여 padding/background/help-button 공간을 제거했다.
   - `지도 마커` launcher는 JunhyunHelper 일반 Button chrome을 사용한다.
   - 접힌 marker panel은 빈 panel chrome을 남기지 않고, 펼친 상태는 일반 desktop viewport에서 현재 marker checkbox를 가능한 한 한 화면에 표시할 충분한 높이를 확보한다.
   - Map/MiniMap 제품 설정 surface는 기존 donor 오른쪽 drawer 대신 MainWindow 공통 in-app overlay에 표시한다.
3. Scanner
   - Scanner Advanced는 별도 Window 표시가 아니라 MainWindow 공통 overlay에 host한다.
   - Advanced 내용 자체의 별도 `닫기` 버튼을 제거했다.
   - Scanner hotkey 편집을 Scanner Settings에 통합하고 기존 전용 `ScannerHotkeySettingsWindow`를 제거했다.
   - hotkey 저장 authority와 중복/Windows modifier/미지정 처리 의미는 기존 ScannerCoordinator 계약을 유지한다.
4. Profile
   - Profile editor의 content card presentation을 Scanner 설정과 같은 overlay/card 계열로 정리했다.
   - 기존 validation/save authority는 유지한다.
5. Search
   - Quest / Hideout / Items / Ammo / Scanner 주요 검색창은 입력창 우측 내부 `×` clear affordance를 사용한다.
   - 기존 filtering 의미는 변경하지 않는다.
6. Shared in-app overlay
   - MainWindow가 사용자-facing 설정/편집 surface의 표시/닫기 lifetime을 소유한다.
   - 같은 launcher 재클릭, backdrop click, 공통 X가 동일 dismiss path를 사용한다.
   - child editor의 validation/save semantics는 overlay host가 재구현하지 않는다.

공식 결정:

- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`
- `docs/RELEASE_NOTES_V1.7.14.md`

## 회귀 검증

PR #200 final head:

```text
1a2f0189c6a6f2a21dc70f50cb092217f0977c13
```

Final PR CI:

```text
run: 33060440860
CI number: #2052
result: SUCCESS
407 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
published EXE Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

첫 PR CI `33059554861`은 Scanner Advanced의 `Application.Current`가 WPF `System.Windows.Application`이 아니라 프로젝트 namespace로 해석되는 C# 이름 충돌을 Build 단계에서 탐지했다. `System.Windows.Application.Current`로 명시한 뒤 전체 Windows gate를 다시 통과시켰다.

`V1714UiConsistencyContractTests`가 다음 계약을 고정한다.

- Ammo popup true-toggle
- shared MainWindow overlay owner
- Scanner Settings hotkey ownership
- Scanner Advanced overlay/no-content-close contract
- old `ScannerHotkeySettingsWindow` 재도입 금지
- Map launcher/panel/settings overlay contract
- Profile card presentation
- primary product search in-field clear affordance

Actual published EXE Product UI smoke는 Scanner Advanced를 standalone Window로 검사하지 않고 실제 MainWindow shared overlay에 host한 상태로 렌더링/닫기 계약을 검증한다. 기존 full Map/Factory/MiniMap smoke도 그대로 유지한다.

## exact product release source

PR #200 merge/main release source:

```text
0a51375de36cd13047216006c2c0311728b1bd89
```

이 커밋이 v1.7.14 tag의 exact target이며 v1.7.14 ProductVersion metadata에도 이 SHA가 포함된다. 이후 documentation-only main commit은 제품 릴리즈 소스로 해석하지 않는다.

## main CI

```text
run: 33060827905
CI number: #2053
result: SUCCESS
407 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

Published ProductVersion:

```text
1.7.14+0a51375de36cd13047216006c2c0311728b1bd89
```

Main-CI release package:

```text
name: Junhyun-Helper.zip
bytes: 80,488,363
SHA-256: 341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
```

GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9641695152
artifact archive bytes: 241,396,019
artifact archive SHA-256: 43a0e4e68d578dfb458fdbd70764a34c21dc59bca4116c2a1ec63345f0aed3a7
```

## Release workflow

```text
run: 33061059154
Release number: #45
result: SUCCESS
```

Release workflow는 exact main CI artifact `9641695152`를 내려받아 artifact digest, ProductVersion, FIRST_RUN identity, package checksum을 다시 검증한 뒤 stable release를 게시했다.

Release workflow 검증값:

```text
source main commit: 0a51375de36cd13047216006c2c0311728b1bd89
Junhyun-Helper.zip bytes: 80,488,363
Junhyun-Helper.zip SHA-256: 341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
```

## public release readback

GitHub `/releases/latest` 및 tag ref readback:

```text
release id: 377720327
tag: v1.7.14
name: 준현 헬퍼 v1.7.14
target commitish: 0a51375de36cd13047216006c2c0311728b1bd89
tag ref object: 0a51375de36cd13047216006c2c0311728b1bd89
draft: false
prerelease: false
latest stable: true
published at UTC: 2026-08-27T10:00:11Z
```

Public asset:

```text
name: Junhyun-Helper.zip
asset id: 532104142
bytes: 80,488,363
GitHub asset digest: sha256:341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
```

공개 asset digest는 exact main CI가 생성·검증한 package SHA-256과 일치한다.

Checksum asset:

```text
name: SHA256SUMS.txt
asset id: 532104140
bytes: 86
GitHub asset digest: sha256:30e66cd988c85491d1a0f369dedec53ddb5afc430ce2bca65a47893ddc1d055d
```

현재 도구 세션에서는 public binary asset을 별도 anonymous client로 다시 내려받아 byte-level 재검증하지 않았다. 따라서 이 문서는 실제 수행한 exact main-CI package verification, Release workflow artifact/package verification, public GitHub metadata/digest/tag-ref readback의 일치를 기록한다.

## immutable release 정책

공개된 stable release는 이후 documentation-only main commit이 같은 assembly version으로 다른 bytes를 생성하더라도 교체하거나 덮어쓰지 않는다.

Release workflow는 이미 공개된 동일 version을 만나면 `Junhyun-Helper.zip`과 `SHA256SUMS.txt` 존재만 확인하고 기존 공개 release를 유지한다. 이후 docs-only 작업에서 이 immutable-existing-release path를 다시 검증한다.

## 보존된 안전 계약

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss 선호
- stale/cross-frame OCR/visual identity proof 금지
- Item ID 확정 전 price/needed/slot/previous-frame metadata를 identity evidence로 사용 금지
- needed quantity authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- needed source authority = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`
- pinned Map donor = `d933792b6042a51cea38dc44b686a096fe30de67`
- Game Content download → parse → canonical build → validation → activate / LKG 계약 유지

## 관련 문서

- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`
- `docs/RELEASE_NOTES_V1.7.14.md`
- `docs/.release-v1.7.14-status.json`
- `docs/MAINTENANCE_CONTRACTS.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
