# STATE — 현재 프로젝트 상태

> 복구 순서는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md`입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`이 기준입니다.

기준일: **2026-09-04 KST**  
상태: **v1.17.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. v1.17.2 product-purity release candidate

PR #292 contains the current product-purity maintenance pass. Public stable remains v1.17.1 until exact-main release publication succeeds.

Final validated code/CI head before this documentation checkpoint:

```text
f00e6871db6afa9f1cca6532e69d201674536687
CI: 33839991885 — SUCCESS
Shutdown Race: 33839991847 — SUCCESS
Documentation Consistency: 33839991837 — SUCCESS
488 passed / 0 failed / 0 skipped
```

Validation covered:

- Windows Release build;
- win-x64 self-contained publish;
- actual published EXE Product UI / full Map/Factory/MiniMap / Scanner smoke;
- graceful shutdown and clean portable root;
- active-async Shutdown Race;
- stable release package/checksum verification;
- Documentation Consistency.

Candidate evidence:

```text
Junhyun-Helper.zip SHA-256:
7e23087ba447cbd81a46edf82b59e583cc2f2fd38746fc180d1bc61ef36ff920

JunhyunHelper-win-x64 artifact id: 9924637693
artifact bytes: 241595338
artifact SHA-256:
c94ab864d16037841c693260f6b3a10cffe9b53d159ee521324f997d098c4f5c
```

The cleanup removes only evidence-backed impurities: hidden/superseded UI ownership, runtime rebinding/repair paths, orphan lifecycle code, retired Scanner/Ammo/Profile/search-clear paths, transitional updater compatibility, stale current-state documentation and tests that required removed structures. Current Quest/Hideout/Items/Ammo/Map/MiniMap/Scanner behavior and validated Map donor/OCR contracts are preserved.

## 2. 공개 제품 상태

```text
public stable: v1.17.1
exact product source/tag target:
4ad1f76ed7c2469e60d0822b229fe03f83c75816
validated PR head:
edd6fa6f5a2edc9d52be84bf1625266d5ad6abec
merge PR: #290
PR CI / Shutdown / Docs:
33826796756 / 33826796665 / 33826796667 — SUCCESS
exact-main CI / Shutdown / Docs:
33827008615 / 33827008595 / 33827008638 — SUCCESS
Release workflow:
33827205735 — SUCCESS
release id: 382428841
published UTC: 2026-09-04T01:49:57Z
485 passed / 0 failed / 0 skipped
```

Public release:

```text
Junhyun-Helper.zip
asset id: 543627042
bytes: 80,573,737
SHA-256: fad73f3987c04cae73c5a473ccbce6c3a70ff8ca22da04a95a942e66ebea3b6c

SHA256SUMS.txt
asset id: 543627044
bytes: 86
asset SHA-256: d665b07efa2d3e402937701f903d1eb5da8001feab0b54bcb2a9d8a93e46f9b1
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9920376580
bytes: 241,651,630
SHA-256: 94cb4670b2889c42efaeaa50874b8bb0a186c3849f09a814184b82609bb2ad22
```

Release workflow `33827205735` checked out exact source `4ad1f76ed7c2469e60d0822b229fe03f83c75816`, downloaded exact-main artifact `9920376580`, verified the artifact digest, ProductVersion/FIRST_RUN identity and `Junhyun-Helper.zip` checksum, then published stable `v1.17.1`.

Public API readback confirmed:

- tag: `v1.17.1`;
- target: exact product source `4ad1f76ed7c2469e60d0822b229fe03f83c75816`;
- latest release: v1.17.1;
- draft: false;
- prerelease: false;
- both required assets present;
- public ZIP digest equals exact-main package SHA-256.

## 3. v1.17.1 Farming Guide removal

The user explicitly decided to remove Farming Guide completely.

Current product therefore has no Farming Guide subsystem.

Removed implementation:

- all first-party `Core/FarmingGuide` policies/models;
- all Desktop Farming Guide page/editor/raid/session/runtime-smoke code;
- Farming Guide persistence store and Desktop service wiring;
- MainWindow Farming Guide navigation/section/busy-state integration;
- Scanner Farming Guide bridge, simulated scan path, accept hotkey/settings, Mini Scanner instruction and Farming Guide quantity-input state;
- Farming Guide-only GameItem extension metadata/import logic;
- Farming Guide-only deterministic tests and product smokes;
- current specialist Farming Guide architecture document.

This is not a hidden/disabled feature state. The active product contains no Farming Guide UI or runtime decision path.

## 4. Preserved product boundaries

The removal preserves:

- Quest / Hideout / Needed Items;
- Items inventory/progress behavior;
- Ammo comparison/pickup/favorites;
- Map / MiniMap;
- Scanner recognition, catalog, search, ordinary Mini Scanner fields, correction, Ground Truth and diagnostics;
- Game Content update, Program Update and user-owned state safety contracts.

No remaining source/test file in the inspected active removal surface retained a Farming Guide runtime reference.

## 5. Legacy compatibility / data handling

### Farming Guide user file

Historical:

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
```

v1.17.1 does not read or write this file.

The application intentionally does not delete it automatically. It is inert legacy user data and not current product state.

### Scanner settings

Scanner display settings remain schema v10.

Older settings JSON may contain obsolete Farming Guide properties or a `farming_guide` Mini Scanner order entry. The current settings type no longer exposes those properties, and `ScannerInfoOrderPolicy.Normalize` drops unknown order keys while preserving known user order.

### Game Content

Content write/read remains v12 / v3-v12.

Farming Guide-only item storage/equipment/attachment/armor/layout extension metadata is no longer part of the canonical `GameItem` runtime model or importer contract. Older snapshots may contain unknown historical JSON properties; current deserialization does not promote them into another feature.

## 6. Validation evidence

### PR #290

Final validated PR head:

```text
edd6fa6f5a2edc9d52be84bf1625266d5ad6abec
```

Passed:

- CI `33826796756`;
- Shutdown Race `33826796665`;
- Documentation Consistency `33826796667`;
- **485 passed / 0 failed / 0 skipped**;
- Windows Release build;
- win-x64 self-contained publish;
- actual published EXE Product UI / Map / Scanner smoke;
- graceful shutdown;
- package/checksum verification.

During final static review, one leftover MainWindow header `ColumnDefinition` was found after the Farming Guide tab removal. It was corrected before the final PR validation above, preventing star-column width allocation from shifting to the wrong header control.

### Exact main

Exact product source:

```text
4ad1f76ed7c2469e60d0822b229fe03f83c75816
```

Passed:

- exact-main CI `33827008615`;
- exact-main Shutdown Race `33827008595`;
- exact-main Documentation Consistency `33827008638`;
- **485 passed / 0 failed / 0 skipped**;
- ProductVersion `1.17.1+4ad1f76ed7c2469e60d0822b229fe03f83c75816`;
- Windows publish;
- actual Product UI / full Map/Factory/MiniMap / Scanner runtime smoke;
- graceful shutdown + clean portable root;
- release package SHA-256 `fad73f3987c04cae73c5a473ccbce6c3a70ff8ca22da04a95a942e66ebea3b6c`;
- Actions artifact digest `94cb4670b2889c42efaeaa50874b8bb0a186c3849f09a814184b82609bb2ad22`.

### Public release

Release workflow `33827205735` succeeded.

Public `v1.17.1` release ID: `382428841`.

Public `Junhyun-Helper.zip` asset:

- id `543627042`;
- 80,573,737 bytes;
- SHA-256 `fad73f3987c04cae73c5a473ccbce6c3a70ff8ca22da04a95a942e66ebea3b6c`.

Public `SHA256SUMS.txt` asset:

- id `543627044`;
- 86 bytes;
- asset SHA-256 `d665b07efa2d3e402937701f903d1eb5da8001feab0b54bcb2a9d8a93e46f9b1`.

## 7. Current schemas / pinned dependencies

```text
Desktop: 1.17.1
Content write/read: v12 / v3-v12
user.db: v1
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
Map donor revision: d933792b6042a51cea38dc44b686a096fe30de67
```

There is no current Farming Guide persistence schema in the active product contract.

## 8. Canonical references

- `docs/PROJECT_STATE.json`
- `docs/.release-v1.17.1-status.json`
- `docs/RELEASE_NOTES_V1.17.1.md`
- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`
- `docs/CURRENT_STATE.md`
- `docs/ACTIVE_WORK.md`

## 9. Current work status

The v1.17.1 Farming Guide removal is implemented, validated, merged, published and publicly verified.

Actual Tarkov play validation on the user's own environment remains separately tracked as `PENDING` evidence. It does not make the v1.17.1 implementation or public release incomplete.
