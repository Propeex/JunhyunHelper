# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.14.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

v1.14.0은 Farming Guide의 총기·장비 조립 편집과 수납 배치 신뢰성을 확장한 MINOR 릴리즈입니다. 재귀 attachment 편집, 빈 슬롯 inline 호환 아이템 선택, assembly-aware image presentation, 검증된 exact multi-grid layout과 fail-safe fallback, Game Content schema v10을 포함합니다.

공식 프로젝트 기억은 대화가 아니라 저장소 문서와 코드입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태와 유지 계약
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 설계·제품 결정

## 현재 공개 릴리즈

```text
version: v1.14.0
Desktop version: 1.14.0
exact product release source/tag target:
9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
PR: #251 — MERGED
validated PR head: c5ee50ba60f2bc7db461328608ec591f4320ccca
PR exact-head CI: 33453431628 — SUCCESS
PR exact-head Shutdown Race CI: 33453431625 — SUCCESS
PR exact-head Documentation Consistency: 33453431595 — SUCCESS
exact-main CI: 33453784868 — SUCCESS
exact-main Shutdown Race CI: 33453784901 — SUCCESS
exact-main Documentation Consistency: 33453784893 — SUCCESS
Release workflow: 33454002732 — SUCCESS
release id: 380133403
published UTC: 2026-09-01T00:15:44Z
527 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 538692301
bytes: 80,633,458
SHA-256:
87728ce9e34a30a9b1eb735fe92b1a4a39f172f3b9cf536dfd12d88c8c35667b
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 538692300
bytes: 86
asset SHA-256:
06ae3473f7fe87d62b0d05dac0d16640a55e30e8a8fd83e4770f962a8fc5dfe3
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9780762947
archive bytes: 241,830,878
archive SHA-256:
1898028e10ef336b2dce35add94d2e1cf83b5c58c27c98649691fe11bdbe8632
```

GitHub `/releases/latest`, release target, `refs/tags/v1.14.0`, exact-main product source가 모두 `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`에 일치합니다. 공개 release는 `draft=false`, `prerelease=false`입니다.

공식 v1.14.0 공개 기록:

- `docs/RELEASE_1.14.0.md`
- `docs/RELEASE_NOTES_V1.14.0.md`
- `docs/.release-v1.14.0-status.json`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

후속 documentation-only main commit은 v1.14.0 제품 릴리즈 소스가 아닙니다. product source/tag/assets는 위 exact source에 고정된 historical identity입니다.

## v1.14.0 — Farming Guide 조립·수납 배치 강화

- 현재 사용자가 직접 장착할 수 없는 PMC 인식표 equipment surface를 제거하고 legacy persisted value는 안전하게 읽습니다.
- 총기·헬멧·방어구 attachment tree를 root 한 단계가 아니라 하위 부품 slot까지 재귀적으로 편집합니다.
- 빈 attachment/armor slot을 클릭하면 같은 화면에 호환 가능한 item icon picker를 열고 한 번의 클릭으로 장착할 수 있습니다.
- 별도 Windows 설정 창을 사용하지 않으며 기존 search drag → slot drop도 유지합니다.
- inline picker와 drag/drop은 동일 Core compatibility/filter/conflict policy를 공유합니다.
- current build가 authoritative imported default preset membership과 정확히 일치할 때만 composed preset image를 사용합니다.
- 임의 조립은 base image + 설치 부품을 이용한 deterministic fallback으로 표현합니다.
- storage legality는 current Tarkov grid mechanics가 계속 권위입니다.
- multi-grid 상대 배치는 product-owned exact metadata와 current grid count/width/height signature가 정확히 일치할 때만 적용합니다.
- metadata가 없거나 stale하면 실제 배치라고 추측하지 않고 finite compact layout으로 fallback합니다.
- importer가 `GridLayoutName` / `RigLayoutName` 계열 identity를 보존합니다.
- Game Content snapshot write schema는 v10, readable compatibility는 v3~v10입니다.

## Farming Guide 제품 의미

Scanner 오른쪽의 `파밍 가이드`는 실제 레이드 중 inventory를 계속 추적하는 기능이 아니라, **레이드 출발 장비와 수납 상태를 구성하는 Loadout / Inventory Editor**입니다.

주요 동작:

- raid-start equipment 구성
- Pocket / Rig / Backpack / Secure Container / Special Slot 표현
- current Tarkov `width × height` item footprint
- 검색 결과 drag-and-drop
- drag 중 `R` 90도 회전
- grid snap / bounds / overlap / contiguous-space / current filter 검증
- nested bag/rig/container storage
- recursive weapon/helmet/armor assembly editing
- attachment / replaceable armor plate direct manipulation
- 전체 raid-start preset save/load/delete
- melee user-level fixed setting
- profile-aware standard/expanded pockets
- 총 무게 / 사용·전체 storage cell 요약
- filled carrier destructive replacement fail-closed
- stale/impossible persisted state fail-closed sanitization

현재 포함하지 않습니다.

- loot 가치 판단
- pickup/discard/replace 추천
- Scanner 실시간 recommendation
- 실제 raid inventory 좌표의 지속적인 1:1 동기화

Farming Guide 사용자 상태는 `%LocalAppData%/JunhyunHelper/farming-guide.json` schema v1에 저장됩니다.

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
- .NET 10 / WPF
- self-contained single-file executable
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요

사용자 데이터는 `%LocalAppData%/JunhyunHelper` 아래에 저장됩니다.

## 주요 기능

- GameMode별 Profile / User Progress
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger / cleanup
- Items / cross-navigation
- Ammo / favorites / profile-aware pickup 판단
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth / diagnostics / Saved Case / regression dataset
- Scanner item database / Favorites / Recents
- Farming Guide raid-start Loadout / Inventory Editor
- opt-in PC capture/Scanner 지원 진단
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## 안전·유지 계약

- Scanner는 external screen pixels + OCR만 사용하며 game memory read, injection, hook, kernel/driver 접근, input automation, network manipulation, anti-cheat bypass를 사용하지 않습니다.
- false positive보다 miss를 선호하며 reviewed actual Tarkov evidence 없이 recognition acceptance를 완화하지 않습니다.
- Game Content update는 candidate → validation → active/LKG 전환의 fail-closed 계약을 유지합니다.
- Quest exact ProfileVariable은 runtime compatibility보다 항상 우선합니다.
- Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산합니다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 보존합니다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 합니다.
- Farming Guide mechanics는 current validated Tarkov structure가 권위이며 visual exact layout metadata는 mechanics를 변경하지 않습니다.
- 내용물이 든 carrier를 묵시적으로 교체해 contents를 유실시키지 않습니다.
- Map/MiniMap donor는 pinned revision `d933792b6042a51cea38dc44b686a096fe30de67`입니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.
- public stable tag/source/assets는 immutable historical identity로 취급합니다.

## Schema / compatibility

```text
Desktop version: 1.14.0
Content schema write: v10
Readable Content schemas: v3~v10
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

## 검증

v1.14.0 exact product source `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`은 527 deterministic tests, Windows Release build, self-contained win-x64 publish, actual published EXE Product UI / Farming Guide / Map smoke, recursive assembly/inline picker/exact-layout smoke, graceful shutdown, active-async Shutdown Race, package/checksum audit, exact-main Documentation Consistency, Actions artifact digest verification, verified Release workflow, public tag/release/assets/latest-stable readback을 통과했습니다.

사용자의 실제 PC/Tarkov v1.14.0 최종 실사용 확인과 김태영 실제 PC diagnostic ZIP 수집·분석은 자동화 검증과 별개이며 현재 `PENDING`입니다.
