# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.11.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

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
version: v1.11.4
Desktop target version: 1.11.4
exact product release source/tag target:
f9d3497004241ea80193e5a0d242e7219cf04f2a
PR: #236 — MERGED
superseded draft PR: #235 — CLOSED / NOT MERGED
PR exact-head CI: 33345630940 — SUCCESS
PR exact-head Shutdown Race CI: 33345630896 — SUCCESS
PR exact-head Documentation Consistency: 33345630871 — SUCCESS
exact-main CI: 33345851673 — SUCCESS
exact-main Shutdown Race CI: 33345851704 — SUCCESS
exact-main Documentation Consistency: 33345851658 — SUCCESS
Release workflow: 33346020525 — SUCCESS
release id: 379449740
478 passed / 0 failed / 0 skipped
published UTC: 2026-08-31T00:56:10Z
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537252429
bytes: 80,564,330
SHA-256:
99ad5d7ce75bc5211edf79a6e80c93b666489bb4a47f4358b2ece70c183f2643
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 537252430
bytes: 86
asset SHA-256:
6b81b3816b63b49999e225244214f3d2a3eeabc67fa88da2dd38542c0969f092
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9741999225
archive bytes: 241,626,166
archive SHA-256:
0af92581d315e2e69d7ff319f1c9968e52fa0093d8635db0eec894e954e2a450
```

GitHub `/releases/latest`, release target, `refs/tags/v1.11.4`, exact-main product source가 모두 `f9d3497004241ea80193e5a0d242e7219cf04f2a`로 일치함을 확인했습니다. 공개 release는 `draft=false`, `prerelease=false`입니다. Release workflow는 exact-main CI에서 검증·업로드한 artifact를 다운로드해 ZIP manifest/actual hash를 검증하고 stable release를 공개했습니다. 공개 ZIP의 GitHub digest도 검증값 `99ad5d7ce75bc5211edf79a6e80c93b666489bb4a47f4358b2ece70c183f2643`과 일치합니다.

공식 v1.11.4 공개 기록:

- `docs/RELEASE_1.11.4.md`
- `docs/RELEASE_NOTES_V1.11.4.md`
- `docs/.release-v1.11.4-status.json`

이 README와 이후 documentation-only commit은 v1.11.4 제품 릴리즈 소스가 아닙니다. v1.11.4의 product source/tag/assets는 위 exact source에 고정된 historical release identity입니다.

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

## v1.11.4 — MiniMap lifecycle / Mini Scanner 유지보수

### MiniMap 최초 생성 지도 동기화

Main Map에서 지도를 변경한 직후 MiniMap을 처음 열어도 이전 지도가 첫 프레임에 보이지 않도록 product state synchronization 순서를 수정했습니다.

- Main Map selection 변경 시 tracker/registry state를 동기적으로 먼저 갱신
- queued reconciliation은 유지
- fresh first-create MiniMap과 reused MiniMap 모두 현재 선택 지도 사용
- actual published EXE smoke에서 first-create boundary 직접 검증

### PMC / Scav / Transit 및 일반 마커

MiniMap의 extract/marker lifecycle을 실제 렌더링 기준으로 강화했습니다.

- PMC / Scav / Transit filter state 유지
- packaged data의 실제 Transit grouped extract 수와 rendered Transit marker 수 비교
- donor async refresh 취소로 standard marker layer만 비는 경우 loaded marker DB에서 해당 레이어 직접 복구
- 복구 과정에서 또 다른 refresh race를 만들지 않음

### Player Marker Size

Player Marker Size 변경은 player marker scale에만 적용됩니다.

- Name Size 보존
- MiniMap Marker Size 보존
- 일반 / Quest / Extract marker presentation 보존
- whole-view refresh로 unrelated presentation을 다시 덮지 않음

### Mini Scanner

Mini Scanner 우클릭 `현재 결과 교정` context menu를 제거했습니다.

유지되는 기능:

- 좌클릭 드래그 이동
- topmost
- recognition/result 표시
- `교정 데이터 추가` 전역 hotkey를 통한 evidence 저장

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
- v1.11.3 correction semantic carry는 동일 title/capture mode/3초의 fail-closed 조건에서 correction snapshot에만 적용됩니다.
- Map/MiniMap donor는 pinned revision `d933792b6042a51cea38dc44b686a096fe30de67`입니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop target version: 1.11.4
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.11.3 → v1.11.4에서 mandatory Game Content migration, user.db migration, Scanner display settings migration은 없습니다.

## 검증

v1.11.4 exact product source `f9d3497004241ea80193e5a0d242e7219cf04f2a`은 다음을 통과했습니다.

- 478 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- fresh MiniMap first-create synchronization
- actual Transit marker rendering
- standard marker direct recovery
- Player Marker Size isolation
- Mini Scanner context-menu absence
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback
- public ZIP digest = verified exact-main package hash

사용자의 실제 PC/Tarkov 플레이 환경에서 v1.11.4 최종 실사용 검증은 자동화 검증과 별개이며 현재 `PENDING`입니다.

## 개발 원칙

기존 코드를 단순히 현재 동작한다는 이유로 올바른 설계로 간주하지 않습니다. 반대로 근거가 없는 전면 리팩터링도 하지 않습니다. 실제 사용자 증상, 공식 제품 요구사항, 현재 코드와 테스트를 함께 확인해 문제 범위에 비례한 수정을 수행합니다.

새 작업이 시작되면 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 상태를 복구합니다.
