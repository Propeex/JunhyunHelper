# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.12.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태입니다. 새로운 실제 회귀, Tarkov 호환성 변화, 또는 사용자가 명시적으로 확정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 추측성 대규모 구조 변경을 시작하지 않습니다.

공식 프로젝트 기억은 대화가 아니라 저장소 문서와 코드입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 사람이 읽는 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태와 유지 계약
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 설계·제품 결정
- `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md` — 구현 구조

## 현재 공개 릴리즈

```text
version: v1.12.0
Desktop target version: 1.12.0
exact product release source/tag target:
b2fcec460df256c581e87b53c6293dc4d2177b9c
final PR: #238 — MERGED
superseded draft PR: #237 — CLOSED / NOT MERGED
validated feature head: 5216ab410c8a4384aee7d9f1a69fbd30302ad0a8
feature-head CI: 33348681591 — SUCCESS
feature-head Shutdown Race CI: 33348681589 — SUCCESS
feature-head Documentation Consistency: 33348681555 — SUCCESS
exact-main CI: 33348916340 — SUCCESS
exact-main Shutdown Race CI: 33348916440 — SUCCESS
exact-main Documentation Consistency: 33348916365 — SUCCESS
Release workflow: 33349066686 — SUCCESS
release id: 379463868
482 passed / 0 failed
published UTC: 2026-08-31T01:56:23Z
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537304923
bytes: 80,572,903
SHA-256:
d8ad140ee39ef533471a229ae01e80bc4ad7baeb5b513490c645bdbd3af137c0
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 537304924
bytes: 86
asset SHA-256:
76a0dfb4e7734001a938798c2f6180f815d79b914e7d2b3933423f1f827673d7
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9742966369
archive bytes: 241,651,154
archive SHA-256:
c6122103fefa1c0b5ffd30787a4a60f6af1e151c3dd4694dca3584c7081145e9
```

GitHub `/releases/latest`, release `target_commitish`, `refs/tags/v1.12.0`, exact-main product source가 모두 `b2fcec460df256c581e87b53c6293dc4d2177b9c`로 일치합니다. 공개 release는 `draft=false`, `prerelease=false`입니다. Release workflow는 exact-main CI artifact를 사용해 package manifest와 실제 ZIP hash를 검증한 뒤 stable release를 공개했으며 공개 ZIP의 GitHub asset digest도 위 SHA-256과 일치합니다.

공식 v1.12.0 공개 기록:

- `docs/RELEASE_1.12.0.md`
- `docs/RELEASE_NOTES_V1.12.0.md`
- `docs/.release-v1.12.0-status.json`
- `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`

이 README와 이후 documentation-only commit은 v1.12.0 제품 릴리즈 소스가 아닙니다. v1.12.0의 product source/tag/assets는 위 exact source에 고정된 historical release identity입니다.

## v1.12.0 핵심 변경

### Quest `확인 필요` 폭증 수정

현재 EFT 1.1의 audited staged task-pool 구조에서 Trader Loyalty Level이 이미 다음 단계로 올라간 뒤에도 이전 단계의 hidden pool value를 알 수 없다는 이유로 최대 48개의 과거 side-task가 `확인 필요`로 되돌아갈 수 있던 회귀를 수정했습니다.

- exact ProfileVariable 값이 있으면 항상 최우선
- 현재 trader LL이 pool stage보다 낮으면 기존 잠금 의미 유지
- 현재 stage에서는 기존 보수적 reconstruction / fail-closed 유지
- **현재 trader LL이 audited pool stage보다 높으면 과거 stage threshold가 충족된 runtime-only availability floor 사용**
- 이 값은 숨은 서버 counter의 exact 값을 저장·추정하는 값이 아님
- 구조 drift 시 fail-closed
- Future Needed Items / cleanup 안전성에는 이 current-UI compatibility를 낙관적으로 전파하지 않음

### 은신처 검색창 `×`

공통 clear 버튼이 TextBox의 실제 외부 margin 전체를 반영하도록 수정해, 은신처 검색창에서만 `×`가 아래로 어긋나던 문제를 해결했습니다.

### 김태영 PC 진단

메인 헤더 좌측 프로필 이미지를 클릭하면 전용 지원 진단을 실행할 수 있습니다.

```text
프로필 이미지 클릭
→ 김태영 본인 확인
→ 로컬 진단 실행
→ 바탕화면 ZIP 생성
→ hyune4784@naver.com 으로 전달 안내
```

진단은 자동 전송하지 않습니다. Scanner/capture에 영향을 줄 수 있는 Windows/display/DPI, GPU/driver/monitor, HDR/color/luminance, allowlisted capture/overlay app, Scanner 상태, 화면/Tarkov capture 비교와 휘도 통계를 수집합니다. 사용자명·컴퓨터명·IP/MAC·네트워크 목록·secret/token·임의 전체 프로세스 목록·설치 경로는 수집하지 않습니다. 단, 사용자가 명시적으로 진단을 실행하면 실제 화면 PNG가 포함될 수 있음을 실행 전에 알립니다.

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
- 특정 PC capture/Scanner 환경을 위한 opt-in 지원 진단
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## Scanner 안전 경계

Scanner는 외부 화면 pixels와 OCR만 사용합니다. game process memory read, DLL/code injection, process hook, kernel/driver 접근, input automation, network manipulation, anti-cheat bypass를 사용하지 않습니다. false positive보다 miss를 선호하며 actual Tarkov evidence 없이 OCR/matcher/candidate acceptance를 임의 완화하지 않습니다.

## 주요 유지 계약

- Game Content update는 candidate → validation → active/LKG 전환의 fail-closed 계약을 유지합니다.
- Quest exact ProfileVariable은 runtime compatibility보다 항상 우선합니다.
- Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산합니다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 canonical requirement에 보존합니다.
- Ammo pickup은 same-caliber penetration과 현재 profile의 직접 구매 가능 상태를 기준으로 합니다.
- barter/craft/flea/higher-LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않습니다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선합니다.
- correction hotkey는 evidence-only Saved Case를 저장하고 Ground Truth를 자동 생성하지 않습니다.
- Map/MiniMap donor는 pinned revision `d933792b6042a51cea38dc44b686a096fe30de67`입니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop target version: 1.12.0
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.11.4 → v1.12.0에서 mandatory Game Content migration, user.db migration, Scanner display settings migration은 없습니다.

## 검증

v1.12.0 exact product source `b2fcec460df256c581e87b53c6293dc4d2177b9c`은 Windows Release build, deterministic tests, Windows x64 self-contained publish, actual published EXE Product UI / Map / Scanner smoke, graceful shutdown, active-async Shutdown Race, package/checksum audit, exact-main Documentation Consistency, artifact upload, verified Release workflow, public tag/release/assets/latest-stable readback을 통과했습니다.

사용자의 실제 PC/Tarkov 플레이 환경에서 v1.12.0 최종 실사용 검증과 김태영 PC에서의 실제 diagnostic ZIP 수집·분석은 자동화 검증과 별개이며 현재 `PENDING`입니다.

## 개발 원칙

기존 코드를 단순히 현재 동작한다는 이유로 올바른 설계로 간주하지 않습니다. 반대로 근거가 없는 전면 리팩터링도 하지 않습니다. 실제 사용자 증상, 공식 제품 요구사항, 현재 코드와 테스트를 함께 확인해 문제 범위에 비례한 수정을 수행합니다.

새 작업이 시작되면 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 상태를 복구합니다.
