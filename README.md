# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.11.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태입니다. 새로운 실제 회귀, Tarkov 호환성 변화, 또는 사용자가 명시적으로 확정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 추측성 대규모 구조 변경을 시작하지 않습니다.

공식 프로젝트 기억은 대화가 아니라 저장소 문서와 코드입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 사람이 읽는 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태와 유지 계약
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 주요 설계/제품 결정
- `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md` — 구현 구조

## 현재 공개 릴리즈

```text
version: v1.11.3
Desktop target version: 1.11.3
exact product release source/tag target:
043abad38f4c3ebc9101463a162614ef67df7536
PR: #234 — MERGED
superseded draft PR: #233 — CLOSED / NOT MERGED
PR exact-head CI: 33319386444 — SUCCESS
PR exact-head Shutdown Race CI: 33319386465 — SUCCESS
PR exact-head Documentation Consistency: 33319386455 — SUCCESS
exact-main CI: 33319592093 — SUCCESS
exact-main Shutdown Race CI: 33319592115 — SUCCESS
exact-main Documentation Consistency: 33319592111 — SUCCESS
Release workflow: 33319769016 — SUCCESS
release id: 379321405
474 passed / 0 failed / 0 skipped
published UTC: 2026-08-30T15:29:47Z
```

Public package:

```text
Junhyun-Helper.zip
asset id: 536758239
bytes: 80,558,970
SHA-256:
e43892ecafc9920a7e3b7295f94b8a5324865977028b3573437d8ff7de4f327e
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 536758240
bytes: 86
asset SHA-256:
5b3cc0468ad6a11076b547883fbd16d1276c74bc51779251c0c3421a070d63c3
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9734538554
archive bytes: 241,607,396
archive SHA-256:
cf10ab86f31c44dff00414b9f4e47ff9bf5a64df18210084bd2b41c42e3ac2a7
```

GitHub `/releases/latest`, release target, `refs/tags/v1.11.3`, exact-main product source가 모두 `043abad38f4c3ebc9101463a162614ef67df7536`로 일치함을 확인했습니다. 공개 release는 `draft=false`, `prerelease=false`입니다. Release workflow는 exact-main CI에서 검증·업로드한 artifact를 다운로드해 검증하고 stable release를 공개했습니다.

공식 v1.11.3 공개 기록:

- `docs/RELEASE_1.11.3.md`
- `docs/RELEASE_NOTES_V1.11.3.md`
- `docs/.release-v1.11.3-status.json`

이 README와 이후 documentation-only commit은 v1.11.3 제품 릴리즈 소스가 아닙니다. v1.11.3의 product source/tag/assets는 위 exact source에 고정된 historical release identity입니다.

## 설치 / 실행

배포 형태는 Windows x64 portable ZIP입니다.

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

- Windows x64
- .NET 10 WPF
- self-contained executable
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요

사용자 데이터는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper` 아래에 저장됩니다.

## 주요 기능

- GameMode별 Profile / User Progress
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger / cleanup
- Items / cross-navigation
- Ammo / favorites / 현재 프로필 기반 pickup 판단
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth / diagnostics / Saved Case / regression dataset
- Scanner 아이템 정보 DB
- Scanner Favorites / Recents
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## v1.11.3 — UI / Map / Scanner 교정 유지보수

### Items / Hideout 검색창

Items와 Hideout는 기존 product-owned `ProductSearchClearButtonBehavior`를 실제 page lifecycle에서 안정적으로 attach합니다.

- query empty → inline `×` 숨김
- query non-empty → inline `×` 표시
- 클릭 → 기존 검색 경로로 query clear
- clear 후 TextBox focus 복구
- Quest/Items/Hideout 동일 canonical behavior 사용

v1.11.2 smoke가 behavior를 직접 attach해 실사용 회귀를 숨길 수 있던 검증 결함도 제거했습니다. 이제 published smoke는 실제 page lifecycle이 만든 결과를 검사합니다.

### Map 지도 마커 패널

expanded marker panel은 content-sized popup이 아니라 map 영역의 **available-height viewport**로 동작합니다.

- 큰 창에서는 가용 세로 공간을 정상 사용
- 하단 탈출구 체크박스/필터 영역 클리핑 방지
- 실제 내용이 넘칠 때만 내부 `ScrollViewer`가 scrollbar 표시
- rendered overflow와 scrollbar state를 actual published EXE smoke에서 검증

### Scanner 교정 이미지 확대/축소

Scanner Saved Case 교정 이미지에서 마우스 휠 확대/축소를 지원합니다.

- fit 상태부터 최대 8× multiplier
- 확대된 이미지는 스크롤/pan 가능
- pointer 위치 기준 anchor 보존
- image/canvas의 source pixel coordinate system은 항상 원본 해상도 유지
- Ground Truth rectangle 및 직접 지정 좌표 저장 의미는 zoom과 무관

최초 runtime smoke에서 Auto scrollbar 출현 때문에 fit scale이 확대 전후 달라지는 문제를 발견했고, stable arranged control bounds를 기준으로 fit scale을 계산하도록 수정했습니다.

### Scanner correction evidence 보존

사용자가 전달한 calibration/diagnostics batch에서 저장된 일부 case가 `NOT_RUN`이었지만 runtime log에는 실제 OCR/matcher가 실행된 증거가 있었습니다. 분석 완료 frame 뒤의 새 geometry-only capture가 단일 latest debug frame을 덮어써 correction save 시 의미 있는 semantics가 유실되는 timing defect였습니다.

v1.11.3은 correction snapshot에 한해서 다음 조건을 모두 만족할 때만 직전 analyzed semantics를 보존합니다.

- 동일한 non-empty title signature
- 동일 capture mode
- analyzed frame age 3초 이내

현재 screenshot/geometry는 최신 frame을 유지합니다. 이 보존 정보는 diagnostic/교정 품질을 위한 것이며 live recognition 결정에는 사용하지 않습니다. OCR/matcher/candidate acceptance threshold도 완화하지 않았습니다.

## Scanner 안전 경계

Scanner는 외부 화면 pixels와 OCR만 사용합니다.

사용하지 않습니다.

- game process memory read
- DLL/code injection
- game/process hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

false positive보다 miss를 선호하며, actual Tarkov evidence 없이 OCR/matcher/candidate acceptance를 임의 완화하지 않습니다.

## 주요 유지 계약

- Game Content update는 candidate → validation → active/LKG 전환의 fail-closed 계약을 유지합니다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 canonical requirement에 보존합니다.
- Ammo pickup은 same-caliber penetration과 현재 profile의 직접 구매 가능 상태를 기준으로 합니다.
- barter/craft/flea/higher-LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않습니다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선합니다.
- correction hotkey는 evidence-only Saved Case를 저장하고 Ground Truth를 자동 생성하지 않습니다.
- correction semantic carry는 동일 title/capture mode/3초의 fail-closed 조건에서 correction snapshot에만 적용합니다.
- Map/MiniMap donor는 pinned revision `d933792b6042a51cea38dc44b686a096fe30de67`입니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop target version: 1.11.3
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.11.2 → v1.11.3에서 mandatory Game Content migration, user.db migration, Scanner display settings migration은 없습니다.

## 검증

v1.11.3 exact product source `043abad38f4c3ebc9101463a162614ef67df7536`은 다음을 통과했습니다.

- 474 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- Items / Hideout lifecycle-attached inline search clear runtime validation
- Map marker full-height / rendered-overflow scrollbar validation
- Scanner correction mouse-wheel zoom / stable fit / source-pixel coordinate validation
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

사용자의 실제 PC/Tarkov 플레이 환경에서 v1.11.3 최종 실사용 검증은 자동화 검증과 별개이며 현재 `PENDING`입니다.

## 개발 원칙

기존 코드를 단순히 현재 동작한다는 이유로 올바른 설계로 간주하지 않습니다. 반대로 근거가 없는 전면 리팩터링도 하지 않습니다. 실제 사용자 증상, 공식 제품 요구사항, 현재 코드와 테스트를 함께 확인해 문제 범위에 비례한 수정을 수행합니다.

새 작업이 시작되면 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 상태를 복구합니다.
