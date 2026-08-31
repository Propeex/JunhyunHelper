# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.13.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

v1.13.3은 v1.13.2 실사용에서 확인된 Farming Guide 장비·수납 interaction 회귀를 수정한 PATCH 릴리즈입니다. 보안 컨테이너 판정, nested storage, 실제 attachment/armor-plate drop slot, weapon preset 중복 검색을 바로잡았습니다.

공식 프로젝트 기억은 대화가 아니라 저장소 문서와 코드입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태와 유지 계약
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 설계·제품 결정

## 현재 공개 릴리즈

```text
version: v1.13.3
Desktop target version: 1.13.3
exact product release source/tag target:
9a0064d81dca4c2cffcb01c55742d46298d235de
PR: #248 — MERGED
validated PR head: b39f7156f458fd6fd513b5eca551e522d5a12343
PR exact-head CI: 33382678094 — SUCCESS
PR exact-head Shutdown Race CI: 33382678096 — SUCCESS
PR exact-head Documentation Consistency: 33382678065 — SUCCESS
exact-main CI: 33382979766 — SUCCESS
exact-main Shutdown Race CI: 33382979902 — SUCCESS
exact-main Documentation Consistency: 33382979845 — SUCCESS
Release workflow: 33383407835 — SUCCESS
release id: 379676479
513 passed / 0 failed / 0 skipped
published UTC: 2026-08-31T10:40:13Z
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537835859
bytes: 80,620,064
SHA-256:
704afb5e376f9087dd57c1795d8b95397c06a020acd9545fe80c5fc1b546b7b7
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 537835858
bytes: 86
asset SHA-256:
2c74d9c4e4f096c35eb3b4e45deb734af5b9df31306c9961d66c9aa7cd4e5b4d
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9754610879
archive bytes: 241,795,611
archive SHA-256:
ae3fb9857920ab61e79c46da01d030fbded4a90eca27ec306e7f5661beb0cc3a
```

GitHub `/releases/latest`, release target, `refs/tags/v1.13.3`, exact-main product source가 모두 `9a0064d81dca4c2cffcb01c55742d46298d235de`에 일치합니다. 공개 release는 `draft=false`, `prerelease=false`입니다.

공식 v1.13.3 공개 기록:

- `docs/RELEASE_1.13.3.md`
- `docs/RELEASE_NOTES_V1.13.3.md`
- `docs/.release-v1.13.3-status.json`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

후속 documentation-only commit은 v1.13.3 제품 릴리즈 소스가 아닙니다. product source/tag/assets는 위 exact source에 고정된 historical identity입니다.

## v1.13.3 — Farming Guide 인게임식 장비·수납 interaction 수정

- Epsilon/Gamma/Kappa 등 실제 Secure Container를 current Tarkov data에서 정상 장착하며 Medicine Case 같은 일반 container/case는 오인하지 않습니다.
- `ParentInstanceId` 기반 nested storage를 사용해 가방 안 가방·가방 안 리그와 내부 아이템을 실제 상태로 저장·복원합니다.
- 별도 `장비 정보/장비 설정` Window를 제거하고 가운데 in-page workbench에서 실제 내부 구조를 직접 조작합니다.
- stored bag/rig는 실제 storage grid를, weapon/helmet/armor는 actionable attachment/mod/replaceable armor plate slot을 표시합니다.
- attachment/plate slot은 한 슬롯 한 아이템 계약이며 기존 아이템을 묵시적으로 덮어쓰지 않습니다.
- nested container 이동/삭제 시 descendants를 보존하거나 subtree로 제거하며 cycle/orphan을 fail closed합니다.
- upstream assembled weapon preset은 Farming Guide 검색에서 제외해 동일 총기 중복을 제거하고 canonical base weapon의 실제 mod slots를 사용합니다.
- 열린 workbench owner를 이동하기 시작하면 workbench를 먼저 닫아 stale write-back을 방지합니다.

## 파밍 가이드 제품 의미

Scanner 오른쪽의 `파밍 가이드`는 실제 레이드 중 inventory grid를 계속 추적하는 기능이 아니라, **레이드 출발 장비와 수납 상태를 구성하는 Loadout / Inventory Editor**입니다.

주요 동작:

- 헤드셋, 헬멧, 얼굴/안경, 방탄복/아머드 리그, 무기, 권총 등 출발 장비 구성
- Pocket / Rig / Backpack / Secure Container / Special Slot 표현
- 검색 결과 item을 실제 Tarkov `width × height` 크기로 drag
- drag 중 `R` 키로 90도 회전
- grid snap / bounds / overlap / contiguous-space / current filter 검증
- current Tarkov data의 storage grid / equipment slot / attachment slot / armor plate slot / conflict 사용
- nested bag/rig 내부 grid 직접 drag/drop
- weapon/helmet/armor의 actual attachment/plate slot 직접 drag/drop
- 전체 raid-start 상태 preset 저장/복원
- 근접무기와 PMC 인식표는 preset과 분리된 fixed setting
- 총 무게와 사용/전체 storage cell 요약
- 내용물이 든 carrier의 destructive replacement 방지
- Tarkov 변화로 오래된 preset이 불가능해지면 invalid placement를 fail closed

현재 파밍 가이드에는 다음을 포함하지 않습니다.

- loot 가치 판단
- 획득/폐기/교체 추천
- Scanner 실시간 추천 연동
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
- .NET 10 WPF
- self-contained executable
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요

사용자 데이터는 `%LocalAppData%/JunhyunHelper` 아래에 저장됩니다.

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
- Scanner 아이템 정보 DB / Favorites / Recents
- Farming Guide raid-start Loadout / Inventory Editor
- opt-in PC capture/Scanner 지원 진단
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## 주요 안전·유지 계약

- Scanner는 external screen pixels + OCR만 사용하며 game memory read, injection, hook, kernel/driver 접근, input automation, network manipulation, anti-cheat bypass를 사용하지 않습니다.
- false positive보다 miss를 선호하며 actual Tarkov evidence 없이 OCR/matcher/candidate acceptance를 임의 완화하지 않습니다.
- Game Content update는 candidate → validation → active/LKG 전환의 fail-closed 계약을 유지합니다.
- Quest exact ProfileVariable은 runtime compatibility보다 항상 우선합니다.
- Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산합니다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 보존합니다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 합니다.
- Farming Guide는 current validated Tarkov item structure를 사용하고 불가능한 persisted placement/nested relationship은 fail closed합니다.
- 내용물이 든 carrier를 묵시적으로 교체해 contents를 유실시키지 않습니다.
- Map/MiniMap donor는 pinned revision `d933792b6042a51cea38dc44b686a096fe30de67`입니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop target version: 1.13.3
Content schema write: v9
Readable Content schemas: v3~v9
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.13.2 → v1.13.3에는 mandatory user data migration이 없습니다. 과거 Farming Guide schema-v1 저장 파일에는 `ParentInstanceId`가 없으므로 null root placement로 호환됩니다.

## 검증

v1.13.3 exact product source `9a0064d81dca4c2cffcb01c55742d46298d235de`은 513 deterministic tests, Windows Release build, Windows x64 self-contained publish, actual published EXE Product UI / Farming Guide / Map smoke, Farming Guide live nested-storage/attachment interaction smoke, graceful shutdown, active-async Shutdown Race, package/checksum audit, exact-main Documentation Consistency, artifact upload, verified Release workflow, public tag/release/assets/latest-stable readback을 통과했습니다.

사용자의 실제 PC/Tarkov v1.13.3 최종 실사용 확인과 김태영 실제 PC diagnostic ZIP의 수집·분석은 자동화 검증과 별개이며 현재 `PENDING`입니다.
