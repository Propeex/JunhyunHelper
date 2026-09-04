# STATE — 현재 프로젝트 상태

> 복구 순서는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md`입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`이 기준입니다.

기준일: **2026-09-04 KST**  
상태: **v1.17.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 공개 제품 상태

```text
public stable: v1.17.2
exact product source/tag target:
73f0386a45818408c2a68530b90de7946ecaf1d1
validated PR head:
121d060db102eed0f4af241ef5f37c51164c6a04
merge PR: #292
PR CI / Shutdown / Docs:
33840328932 / 33840328963 / 33840329237 — SUCCESS
exact-main CI / Shutdown / Docs:
33840553320 / 33840553329 / 33840553303 — SUCCESS
Release workflow:
33840780902 — SUCCESS
release id: 382500195
published UTC: 2026-09-04T05:31:31Z
488 passed / 0 failed / 0 skipped
```

Public release:

```text
Junhyun-Helper.zip
asset id: 543847934
bytes: 80,554,487
SHA-256:
a64d202046505273964b0735976d71e382624c68f16699c6844b193599b43971

SHA256SUMS.txt
asset id: 543847933
bytes: 86
asset SHA-256:
a105826dcc518a58412a521b221a2e7842ccfb716662418981005b4d276505a0
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9924825161
bytes: 241,595,886
SHA-256:
864f971ebe799df881ac4d69318ae331cd3c4c4e783013836bceaacb33232ba4
```

Release workflow `33840780902` checked out exact source `73f0386a45818408c2a68530b90de7946ecaf1d1`, downloaded exact-main artifact `9924825161`, verified ProductVersion/FIRST_RUN identity and package checksum, then published stable `v1.17.2`.

Public API readback confirmed:

- tag `v1.17.2` directly targets exact product source `73f0386a45818408c2a68530b90de7946ecaf1d1`;
- latest release is v1.17.2;
- draft: false;
- prerelease: false;
- both required assets are present;
- public ZIP digest equals the exact-main package SHA-256.

## 2. v1.17.2 Product Purity Cleanup

The maintenance goal was to remove only implementation impurities proven to have no current product role, without adding features or performing performance optimization.

Completed cleanup includes:

### MainWindow / lifecycle

- removed hidden `StatusText` event/state plumbing;
- direct Items cleanup indicator ownership;
- removed runtime mutation-handler rebinding;
- canonical mutation and content-navigation ownership names;
- removed hidden Profile proxy controls and orphan deletion path.

The audit exposed and fixed a real regression where `RefreshItemsCleanupIndicator()` could lose its caller.

### Profile

- canonical Save/Cancel/Delete XAML controls;
- one lifecycle for standalone/overlay behavior;
- removed runtime visual-tree button discovery/rebinding and runtime card wrapping.

### Quest / Hideout / Items search

- explicit page-owned `ProductSearchClearButtonBehavior.Attach(SearchBox)`;
- removed global class-handler/module-initializer lifecycle and page-specific duplicate search-clear shims.

### Ammo

- removed hidden legacy summary/favorites/search/popup controls;
- XAML owns the current selector/search/toolbar/detail presentation;
- removed runtime toolbar/control creation and layout repair;
- current caliber/favorites/search/detail/700ms icon-cycle contracts preserved;
- stale `Polish`/`Fixes` ownership names replaced with responsibility-based names.

### Scanner / Mini Scanner

Removed retired UI/runtime paths:

- OCR substitution editor UI;
- recognition-debug Window;
- dedicated hotkey-capture Window;
- old `필요한 곳` source presentation;
- hidden old three-row summary;
- dead Mini Scanner identity/flea-minimum display settings;
- unused preview/position-edit/reset subsystem;
- unused OCR constructor dependency;
- runtime detail ScrollViewer reparenting;
- runtime favorite/Wiki action repair.

Retained current contracts:

- OCR substitution engine;
- diagnostic image rendering;
- Scanner recognition thresholds/pacing/matching;
- catalog/search/favorites/recents;
- correction/Ground Truth/diagnostics;
- Mini Scanner direct drag position persistence.

Current Scanner smoke files were renamed by responsibility instead of historical version tags where the version was no longer meaningful.

### Update / packaging

The current updater now accepts only the stable canonical package `Junhyun-Helper.zip` and stable wrapper root. Obsolete versioned-package/root transition fallbacks were removed.

### Documentation / tests

- removed current-looking `docs/NEXT.md` and retired `docs/FARMING_GUIDE.md`;
- current release/schema facts centralized around `docs/PROJECT_STATE.json`;
- Documentation Consistency now checks implementation constants against canonical schema facts;
- stale tests were updated to verify current canonical ownership rather than revive removed lifecycle structures.

## 3. Explicitly preserved boundaries

The cleanup intentionally did **not** rewrite or optimize:

- Quest/Hideout domain semantics;
- current Items behavior;
- Ammo product behavior;
- Scanner recognition algorithms;
- Scanner active OCR wrapper chain;
- Map/MiniMap donor integration;
- active Map `Legacy*` bridges;
- supported old JSON/schema read compatibility;
- historical decision/release evidence.

The Map donor revision remains:

`d933792b6042a51cea38dc44b686a096fe30de67`

## 4. Farming Guide status

Farming Guide remains completely removed from the current product.

There is no Farming Guide UI, planner, optimizer, Scanner bridge, persistence service or runtime domain model.

Historical `%LocalAppData%/JunhyunHelper/farming-guide.json` remains inert user data and is not automatically deleted.

## 5. Validation evidence

### PR #292

Final validated PR head:

```text
121d060db102eed0f4af241ef5f37c51164c6a04
```

Passed:

- CI `33840328932`;
- Shutdown Race `33840328963`;
- Documentation Consistency `33840329237`;
- **488 passed / 0 failed / 0 skipped**;
- Windows Release build;
- win-x64 self-contained publish;
- actual published EXE Product UI / Map / Scanner smoke;
- graceful shutdown;
- package/checksum verification.

### Exact main

Exact product source:

```text
73f0386a45818408c2a68530b90de7946ecaf1d1
```

Passed:

- exact-main CI `33840553320`;
- exact-main Shutdown Race `33840553329`;
- exact-main Documentation Consistency `33840553303`;
- **488 passed / 0 failed / 0 skipped**;
- ProductVersion `1.17.2+73f0386a45818408c2a68530b90de7946ecaf1d1`;
- Windows publish;
- actual Product UI / full Map/Factory/MiniMap / Scanner runtime smoke;
- graceful shutdown + clean portable root;
- release package SHA-256 `a64d202046505273964b0735976d71e382624c68f16699c6844b193599b43971`;
- Actions artifact digest `864f971ebe799df881ac4d69318ae331cd3c4c4e783013836bceaacb33232ba4`.

### Public release

Release workflow `33840780902` succeeded.

Public `v1.17.2` release ID: `382500195`.

Public `Junhyun-Helper.zip`:

- id `543847934`;
- 80,554,487 bytes;
- SHA-256 `a64d202046505273964b0735976d71e382624c68f16699c6844b193599b43971`.

Public `SHA256SUMS.txt`:

- id `543847933`;
- 86 bytes;
- asset SHA-256 `a105826dcc518a58412a521b221a2e7842ccfb716662418981005b4d276505a0`.

## 6. Current schemas / pinned dependencies

```text
Desktop: 1.17.2
Content write/read: v12 / v3-v12
user.db: v1
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
Map donor revision: d933792b6042a51cea38dc44b686a096fe30de67
```

## 7. Canonical references

- `docs/PROJECT_STATE.json`
- `docs/.release-v1.17.2-status.json`
- `docs/RELEASE_NOTES_V1.17.2.md`
- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`
- `docs/CURRENT_STATE.md`
- `docs/ACTIVE_WORK.md`

## 8. Current work status

v1.17.2 Product Purity Cleanup is implemented, validated, merged, published and publicly verified.

`docs/ACTIVE_WORK.md` is closed (`NONE`).

Actual Tarkov play validation on the user's own environment remains separately tracked as `PENDING`; this does not make the automated implementation/release validation incomplete.
