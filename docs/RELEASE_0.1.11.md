# RELEASE 0.1.11 — 2026-08-17

## Status

**PUBLIC RELEASE / VERIFIED — Windows x64**

```text
release tag: v0.1.11
release baseline: 88a732c70380b4c764634eff6fd01a16eb849b14
Desktop ProductVersion: 0.1.11+88a732c70380b4c764634eff6fd01a16eb849b14
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
feature PR: #92
feature PR CI: 32014857527 — SUCCESS
feature main CI: 32015175679 — SUCCESS
release candidate PR: #93
release candidate PR CI: 32015691464 — SUCCESS
release baseline main CI: 32015968523 — SUCCESS
release workflow: 32018616694 — SUCCESS
automated tests: 210 passed / 0 failed / 0 skipped
public asset: Junhyun-Helper-v0.1.11-win-x64.zip
public asset size: 74,063,248 bytes
public SHA-256: 1293cc20c09240c4bdafd6fb45ecb5d0bc37857e12e58f60e31dff620e01b426
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.11
```

공개 ZIP은 GitHub Release 생성 뒤 다시 다운로드했고 SHA-256이 빌드 직후 값과 정확히 일치했습니다. Release metadata도 `draft=false`, `prerelease=false`, target commit=`88a732c70380b4c764634eff6fd01a16eb849b14`로 확인했습니다.

## Why v0.1.11 exists

v0.1.10에서 Quest `확인 필요` 개선은 실제로 동작했지만, Items / Ammo / Map UI 피드백 일부를 runtime visual-tree 후처리로 구현했습니다. 이 방식은 소스상 코드가 존재하고 빌드/스모크가 통과하더라도 실제 화면 인스턴스와 생성 순서에 따라 원하는 배치가 유지되지 않을 수 있었습니다.

v0.1.11은 이 부분을 **후처리 bridge가 아니라 실제 XAML / 실제 UI 생성 코드 자체에서 수정**한 교정 릴리즈입니다.

Quest availability / Needed Items 정책은 v0.1.10의 검증된 동작을 그대로 유지합니다.

## User-facing changes

### Flexible hand-in Items

`FlexibleCandidateTemplate` 자체가 다음 레이아웃을 소유합니다.

- row: 68px fixed rhythm
- icon frame: 44px
- 좌측: icon + item name + category
- 우측: `인레이드` / `일반` 보유량 고정 lane
- long item name: one line + ellipsis

기존 `ItemsPage.FlexibleLayout.cs` runtime visual-tree rewrite는 제거했습니다. 36px frame에 44px 이미지를 런타임 보정하던 구조도 제거했습니다.

### Ammo

Ammo header/detail layout을 원본 XAML에 직접 반영했습니다.

- 중복 `구경` label 제거
- 중복 `즐겨찾기` label 제거
- caliber selector: 160px fixed
- favorite star button: 38px fixed / `☆` 또는 `★`
- favorites selector: 170px fixed
- 상세정보 접기/펼치기 버튼을 원본 XAML의 중앙 행에 직접 배치
- detail host도 이름 있는 XAML element로 고정

runtime 코드는 더 이상 상세정보 UI element를 새로 만들거나 visual tree를 재배치하지 않고 expansion state만 제어합니다.

### Map current Quest sidebar

Quest row 생성 코드 자체가 다음 3열 구조를 만듭니다.

```text
30px checkbox | 34px A/B/C marker | remaining quest text
```

- row: 68px fixed height
- checkbox와 marker가 quest title 영역을 밀어내지 않음
- quest title은 marker 바로 뒤에서 좌측 정렬
- 긴 제목은 한 줄 ellipsis
- 기존 `LegacyMapQuestSidebarPolishBridge` 제거
- MainWindow lifecycle의 obsolete bridge 참조도 제거

### Quest / Needed Items

v0.1.10에서 확인된 Quest `확인 필요` 개선은 그대로 유지합니다.

- exact profile variable 값 최우선
- audited EFT 1.1 LL2~LL4 task-pool reconstruction 유지
- audited 구조 + 현재 LL1 + 해당 trader 완료 Quest 0개인 pristine 초기 상태만 LL1 pool counter=0으로 확정
- 진행된 LL1 값은 exact 값 없이 추측하지 않음
- 12개 audited dialogue gate compatibility 유지
- 실제 completion timestamp가 필요한 availability delay는 timestamp가 없으면 계속 `확인 필요`
- future Needed Items는 unresolved future Quest도 계속 `IndeterminatePotential`로 보호

## Safety / compatibility

- Content schema: **v7** 유지
- v3~v7 snapshot 읽기 지원 유지
- `user.db` SQLite schema: **v1** 유지
- v0.1.10 → v0.1.11 필수 데이터 업데이트 없음
- 기존 Profile / Quest / Inventory / Hideout / Map 설정 유지
- runtime GPT/AI 의존성 없음

## Verification

### Feature correction

PR #92 최종 head에 대해:

- Release build SUCCESS
- automated tests SUCCESS
- Windows x64 self-contained publish SUCCESS
- startup + Main Map + Factory + MiniMap smoke SUCCESS
- graceful shutdown SUCCESS

Feature PR CI: `32014857527`

병합 후 main CI: `32015175679` — SUCCESS

### Release candidate

PR #93에서 ProductVersion/FIRST_RUN을 v0.1.11로 고정한 뒤:

- Release build SUCCESS
- automated tests SUCCESS
- Windows x64 publish SUCCESS
- Map/MiniMap runtime smoke SUCCESS

Release candidate CI: `32015691464`

Release baseline main CI: `32015968523` — SUCCESS

### Public package verification

Release workflow `32018616694`은 정확히 release baseline `88a732c70380b4c764634eff6fd01a16eb849b14`를 checkout했습니다.

검증 결과:

```text
ProductVersion=0.1.11+88a732c70380b4c764634eff6fd01a16eb849b14
Passed: 210
Failed: 0
Skipped: 0
Public size: 74,063,248 bytes
Public SHA-256: 1293cc20c09240c4bdafd6fb45ecb5d0bc37857e12e58f60e31dff620e01b426
PUBLIC_RELEASE_VERIFIED=true
```

공개 자산을 GitHub Release에서 재다운로드한 SHA-256도 동일했습니다.

## Remaining conservative boundaries

다음은 release-blocking defect가 아니라 정확성을 위한 현재 제품 경계입니다.

- 진행된 LL1 task-pool에서 exact profile variable 값이 없으면 일부 Quest는 계속 `확인 필요`일 수 있습니다.
- 실제 Quest completion timestamp가 필요한 delay는 timestamp를 모르면 `확인 필요`입니다.
- 새로운/변경된 unsupported availability condition은 자동 통과시키지 않습니다.
- Scanner 실제 기능은 아직 PRODUCT OPEN입니다.
