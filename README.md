# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.15.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.15.2
exact product source/tag target:
f4974ee6bed5047865581240197f7f0e2787ba7c
validated PR head: 1662cc86f6298fc3a13bbcc591d38ae8c8e0787d
merge PR: #262 — MERGED
PR CI / Shutdown / Docs:
33481383672 / 33481383604 / 33481383640 — SUCCESS
exact-main CI / Shutdown / Docs:
33481524940 / 33481524896 / 33481524999 — SUCCESS
Release workflow: 33481956300 — SUCCESS
release id: 380290463
published UTC: 2026-09-01T07:24:43Z
562 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539168506
bytes: 80,654,539
SHA-256:
642fa3845ccb4491c2d0b520000316d79067c3957144814b0b3b77516d14ad34

SHA256SUMS.txt
asset id: 539168503
bytes: 86
asset SHA-256:
077160c0ac6076e07d061a0feb8e386f131327ad82bc4281a619afc4ecd91741
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9790251740
archive bytes: 241,895,658
archive SHA-256:
57665346651872dd4f351241dabe77de09349150ebb2d8664f8d5f626a8daf65
```

GitHub `/releases/latest`, release target and `refs/tags/v1.15.2` all resolve to `f4974ee6bed5047865581240197f7f0e2787ba7c`. The release is neither draft nor prerelease. Later documentation-only commits are not v1.15.2 product sources and may not replace these assets.

Release evidence:

- `docs/RELEASE_NOTES_V1.15.2.md`
- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`

## v1.15.2 Farming Guide

`파밍 가이드`는 raid-start Loadout / Inventory Editor와 Scanner 기반 raid-session advisor를 함께 제공합니다. Tarkov 내부 inventory를 직접 읽거나 게임 입력을 자동화하지 않습니다.

### 장비 완제품 모델

- 총기·헬멧·방탄복 등 장비는 내부 부품을 따로 관리하지 않는 **완제품 하나**로 처리합니다.
- 총기 부착물·헬멧 부착물·방탄판 편집 UI와 장비 내부 장착/교체 파밍 지시는 없습니다.
- Primary Weapon, Pistol/Holster, Helmet, Body Armor, Rig, Backpack, Secure Container 등 최상위 장비 칸 장착/교체 판단은 유지됩니다.
- 예전 Farming Guide 파일의 attachment/armor state는 읽을 수 있지만 current runtime에서는 root Item ID만 남깁니다.
- canonical default preset/source의 완제품 이미지가 있으면 이를 우선 사용하며 근거 없는 조립 이미지는 만들지 않습니다.
- 무기/장비 아이콘은 aspect ratio를 유지하면서 실제 장비 칸을 더 크게 채우도록 표시합니다.

### Nested storage

- `ParentInstanceId` 기반 nested storage를 유지합니다.
- 상세 내부 화면을 열 수 있는 stored item은 Backpack 또는 Rig입니다.
- 가방 안 가방, 가방 안 리그 등 current Tarkov grid/filter가 허용하는 배치는 유지됩니다.
- 상세 화면은 실제 수납 grid 크기에 맞춘 compact view로 열리며 전체 storage 영역을 불필요하게 가리지 않습니다.
- root Rig / Backpack / Secure Container 수납칸은 메인 화면에 그대로 표시합니다.
- generic case/container나 일반 장비 내부는 Farming Guide 상세 surface로 노출하지 않습니다.

### Raid advisor

- `레이드 시작` 시 현재 장비·수납·잠금 상태를 독립적인 raid session으로 snapshot
- Scanner가 확인한 아이템을 current Needed quantity, 경제 가치, footprint, 장비·수납·잠금 상태와 함께 평가
- 새 scan은 이전 미수락 지시를 상태에 반영하지 않고 폐기한 뒤 current state에서 새 아이템을 판단
- Mini Scanner에는 incoming item 이름을 반복하지 않고 행동만 짧게 표시
- 사용자가 설정한 `파밍 가이드 수락` 단축키를 눌러야 session state에 반영하며 성공 피드백은 `반영 완료`
- Special Slots는 canonical `specialSlot` item만 허용하고 compatible item은 ordinary size와 관계없이 정확히 1칸을 사용
- locks는 automation removal/replacement를 제한하지만 direct user editing은 허용
- `레이드 종료` 시 raid-session 변경을 폐기하고 시작 baseline으로 복귀

## 설치 / 실행

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
- portable ZIP / installer 없음
- 일반 사용에 관리자 권한 불필요
- mutable user data는 `%LocalAppData%/JunhyunHelper`에 저장

## 주요 기능

- GameMode별 Profile / User Progress
- Quest / Hideout / Needed Items / Inventory / cleanup
- Items / Ammo 조회와 profile-aware 판단
- Map / MiniMap
- Scanner / Mini Scanner
- Farming Guide loadout / nested storage / raid advisor
- Game Content update with validation + Last Known Good
- Program Update
- opt-in diagnostics

## 안전 경계

Scanner와 준현 헬퍼는 외부 화면 픽셀과 사용자 입력을 기반으로 동작합니다.

다음은 제품 범위가 아닙니다.

- Tarkov game memory read
- DLL/code injection
- process/game hook
- kernel/driver access
- packet/network manipulation
- anti-cheat bypass
- 자동 loot / 게임 입력 자동화

## 개발 / 복구

새 작업은 `AGENTS.md`와 `docs/PROJECT_STATE.json`을 먼저 확인합니다. `docs/ACTIVE_WORK.md`가 `ACTIVE`이면 기록된 지점에서 이어가고, `NONE`이면 현재 public stable 상태에서 새 요청을 시작합니다.
