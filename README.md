# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.15.5 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.15.5
exact product source/tag target:
62466a957a7e32a623a0ffcfad96bfb16504f823
validated PR head: 2d9f01da32e3e80860c5a87b2d2e73bc87c31b17
merge PR: #271 — MERGED
PR CI / Shutdown / Docs:
33516899412 / 33516899393 / 33516899505 — SUCCESS
exact-main CI / Shutdown / Docs:
33520705401 / 33520705533 / 33520705395 — SUCCESS
Release workflow: 33521076146 — SUCCESS
release id: 380587916
published UTC: 2026-09-01T14:42:06Z
593 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539684740
bytes: 80,705,841
SHA-256:
32df6c471cf79349932a83a5d7598fecb8971548e4b38bb7bdab917602898d69

SHA256SUMS.txt
asset id: 539684739
bytes: 86
asset SHA-256:
683a2374431389efdc7d3176816917ef8ef466c2b493aa9bc78dfd6416be4f98
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9805674187
archive bytes: 242,052,034
archive SHA-256:
6281d8f2ef0f5ab0d0b6414b6cded95852f9006d23806527c8467badb8bfc088
```

GitHub `/releases/latest`, release target and `refs/tags/v1.15.5` all resolve to `62466a957a7e32a623a0ffcfad96bfb16504f823`. The release is neither draft nor prerelease. Later documentation-only commits are not v1.15.5 product sources and may not replace these assets.

Release evidence:

- `docs/RELEASE_1.15.5.md`
- `docs/.release-v1.15.5-status.json`
- `docs/RELEASE_NOTES_V1.15.5.md`
- `docs/DECISION_V1.15.5_FARMING_GUIDE_PRESENTATION_VIEWPORT.md`
- `docs/DECISION_V1.15.5_FARMING_GUIDE_STATE_TRANSITION_PLANNER.md`

## v1.15.5 Farming Guide

`파밍 가이드`는 raid-start Loadout / Inventory Editor와 Scanner 기반 raid-session advisor를 제공합니다. Tarkov 내부 inventory를 직접 읽거나 게임 입력을 자동화하지 않습니다.

### 보존 우선 상태 전이

새 아이템을 판단할 때 현재 전체 modeled raid inventory에서 도달 가능한 합법 상태를 비교합니다.

```text
빈 장비칸
→ source-backed 안전한 장비 업그레이드
→ 직접 수납
→ 비파괴 global repacking
→ 교체로 벗겨진 장비/컨테이너 보존 및 nesting
→ 가치 기반 bounded eviction + repacking
→ 마지막으로 버리기
```

장비 교체로 벗겨진 기존 장비는 즉시 소멸하지 않습니다. 일반 장비, 총기, 리그, 가방은 다시 loot candidate가 되어 합법적으로 보관·재배치할 수 있는지 먼저 평가됩니다. Storage grid가 있는 displaced carrier는 다른 수납공간 안에 들어간 뒤 그 내부 공간도 같은 candidate state에서 사용할 수 있습니다.

Needed 획득량은 과거 수락 횟수가 아니라 현재 snapshot과 raid baseline의 Item ID 수량 차이에서 계산되므로, 획득 후 버린 Needed item을 계속 보유한 것으로 오판하지 않습니다.

### 짧은 raid 지시

Mini Scanner는 `장착`, `교체`, `보관`, `버리기`, `방탄 리그 전환` 중심으로 짧게 표시합니다. 같은 가방·리그·컨테이너 내부의 좌표/회전 재배치는 말하지 않고, 실제 다른 storage area로 이동하거나 버려야 하는 기존 아이템만 `+ 아이템 이동 위치` / `+ 아이템 버리기`로 표시합니다. 여러 작업은 쉼표로 구분합니다.

### Source-backed nested storage

- current validated Game Content의 실제 `StorageGrids`와 allowed/excluded item/category filter가 저장 관계의 authority입니다.
- Key tool 같은 특수 컨테이너를 이름으로 하드코딩하지 않습니다.
- Secure Container 안 컨테이너, 컨테이너 안 컨테이너를 `ParentInstanceId`로 재귀 관리합니다.
- compatible positive-allow-list nested grid는 일반 root storage보다 우선하는 dedicated storage 후보입니다.
- nested Workbench는 전체 grid가 물리적으로 viewport에 들어갈 때 가로/세로 scrollbar를 명시적으로 비활성화하여 하단 셀 잘림을 만들지 않습니다.

### 장비 우위 판단

시장가/상점가는 장비 성능의 근거가 아닙니다. 자동 장비 교체는 source-backed 사실로 명백한 우위를 증명할 수 있고 최종 proposed snapshot이 합법적일 때만 제안합니다.

- 보호 장비: 대표 top-level armor class가 엄격히 높을 때만 업그레이드
- Backpack/Rig: 실제 source-backed 수납 capacity가 엄격히 우수하고 기존 내용물을 합법적으로 처리할 수 있어야 함
- Armored Rig끼리: 방어등급과 capacity가 모두 비열화되지 않고 하나 이상 엄격히 개선
- Headset: `distanceModifier`와 `distortion`이 모두 비열화되지 않고 하나 이상 엄격히 개선
- Body Armor + ordinary Rig → superior Armored Rig: 하나의 atomic fail-closed 전환

총기/헬멧 attachment와 armor plate 내부 편집은 계속 제공하지 않으며 완제품 장비 모델을 유지합니다.

## 데이터 / 호환성

```text
Desktop: 1.15.5
Game Content write/read: v11 / v3-v11
Farming Guide state: v2
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

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

## 안전 경계

Scanner와 준현 헬퍼는 외부 화면 픽셀과 사용자 입력을 기반으로 동작합니다. Tarkov game memory read, DLL/code injection, process/game hook, kernel/driver access, packet manipulation, anti-cheat bypass, 자동 loot/게임 입력 자동화는 제품 범위가 아닙니다.

## 개발 / 복구

새 작업은 `AGENTS.md`와 `docs/PROJECT_STATE.json`을 먼저 확인합니다. `docs/ACTIVE_WORK.md`가 `ACTIVE`이면 기록된 지점에서 이어가고, `NONE`이면 현재 public stable 상태에서 새 요청을 시작합니다.
