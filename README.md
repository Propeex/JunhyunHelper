# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.15.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.15.4
exact product source/tag target:
c27daf2177b643ee16d4a3d5b0997e54a267c2c7
validated PR head: da9e788a8494734149cfa0e65eff3535e14d2bac
merge PR: #268 — MERGED
PR CI / Shutdown / Docs:
33500484624 / 33500484673 / 33500484510 — SUCCESS
exact-main CI / Shutdown / Docs:
33500904378 / 33500904396 / 33500904356 — SUCCESS
Release workflow: 33501233130 — SUCCESS
release id: 380429049
published UTC: 2026-09-01T11:12:15Z
585 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539435772
bytes: 80,695,104
SHA-256:
a0a5d6f19beecab7b656250e3d1ae56d3073aae442b7cdc9b19b865a7d8a9e81

SHA256SUMS.txt
asset id: 539435771
bytes: 86
asset SHA-256:
86627e394474b4fb69b27c5db6cc380a2f0a3ebf1876ee6d842159436014ac89
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9797756949
archive bytes: 242,014,938
archive SHA-256:
2ab185334c441dfa44f8d1afb774e7c7c6815df07849563ba865210a9b5857bb
```

GitHub `/releases/latest`, release target and `refs/tags/v1.15.4` all resolve to `c27daf2177b643ee16d4a3d5b0997e54a267c2c7`. The release is neither draft nor prerelease. Later documentation-only commits are not v1.15.4 product sources and may not replace these assets.

Release evidence:

- `docs/RELEASE_1.15.4.md`
- `docs/.release-v1.15.4-status.json`
- `docs/RELEASE_NOTES_V1.15.4.md`
- `docs/DECISION_V1.15.4_FARMING_GUIDE_REPACKING_EQUIPMENT_UPGRADES.md`

## v1.15.4 Farming Guide

`파밍 가이드`는 raid-start Loadout / Inventory Editor와 Scanner 기반 raid-session advisor를 함께 제공합니다. Tarkov 내부 inventory를 직접 읽거나 게임 입력을 자동화하지 않습니다.

### 수납 / nested storage

- current validated Game Content의 실제 `StorageGrids`와 allowed/excluded item/category filter가 저장 관계의 authority입니다.
- Key tool 같은 특수 컨테이너를 이름으로 하드코딩하지 않습니다.
- Secure Container 안 컨테이너, 컨테이너 안 컨테이너를 `ParentInstanceId`로 재귀 관리합니다.
- compatible positive-allow-list nested grid는 일반 root storage보다 우선하는 dedicated storage 후보입니다.
- nested Workbench는 전체 grid가 물리적으로 viewport에 들어갈 때 불필요한 가로 스크롤/셀 잘림을 만들지 않습니다.

### 보존 우선 파밍 판단

새 아이템이 바로 들어가지 않아도 전체 수납 여유가 있다면, 파괴/버리기 전에 잠기지 않은 기존 아이템을 합법적으로 이동·회전·재배치할 수 있는지 먼저 판단합니다.

```text
빈 장비칸
→ 증명된 안전한 장비 업그레이드
→ 직접 수납
→ 비파괴 재배치
→ 필요도/가치 기반 파괴 교체
→ 마지막으로 버리기
```

`F` 잠금, 잠긴 ancestor, 예약 칸, source filter, dedicated-container 우선순위, nested parent/descendant 관계는 재배치에서도 보존됩니다. 내용물이 든 nested container는 부모 자체의 가격만 보고 자동 파괴 교체하지 않습니다.

### 장비 업그레이드

시장가/상점가는 장비 성능의 근거가 아닙니다. 자동 장비 교체는 source-backed 사실로 명백한 우위를 증명할 수 있고 현재 modeled contents를 합법적으로 보존할 때만 제안합니다.

- 보호 장비: 대표 top-level armor class가 엄격히 높을 때만 업그레이드
- Backpack/Rig: 실제 source-backed 수납 capacity가 엄격히 우수하고 기존 내용물을 모두 보존할 수 있어야 함
- Armored Rig끼리: 방어등급과 capacity가 모두 비열화되지 않고 하나 이상 엄격히 개선
- Headset: `distanceModifier`와 `distortion`이 모두 비열화되지 않고 하나 이상 엄격히 개선
- Body Armor + ordinary Rig → superior Armored Rig: 내용물 보존까지 포함한 하나의 atomic fail-closed 전환

총기/헬멧 attachment와 armor plate 내부 편집은 계속 제공하지 않으며 완제품 장비 모델을 유지합니다.

### 잠금 / 테스트 스캔

- 일반 stored item: neutral border
- `F` 잠금: accent/노란색 border
- 잠금 해제: neutral로 복귀
- 검색 결과 hover + `T`: Search TextBox 포커스가 남아 있어도 실제 Farming Guide recommendation path로 simulated scan
- Scanner capture가 꺼져 있거나 restart 후 catalog가 memory에 없으면 verified same-mode local catalog를 필요 시 로드

## 데이터 / 호환성

```text
Desktop: 1.15.4
Game Content write/read: v11 / v3-v11
Farming Guide state: v2
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

Game Content v11은 Farming Guide 장비 판단에 쓰는 대표 armor class와 headset 비교 fact를 저장합니다. readable v3-v10 정상 snapshot은 오프라인 last-known-good로 유지되며 정상 transactional Data Update 경계에서 최신 schema로 갱신할 수 있습니다.

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
- Farming Guide loadout / source-backed nested storage / live raid advisor
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
