# STATE — 현재 프로젝트 상태

> 복구 순서는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md`입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`이 기준입니다.

기준일: **2026-09-04 KST**  
상태: **v1.17.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 공개 제품 상태

```text
public stable: v1.17.3
exact product source/tag target:
8ec677b1552f9deed55f98931c1df317e9bc4a4b
validated PR head:
230a5284f58f9d5eb8954c6042164bc5635fd35c
merge PR: #294
PR CI / Shutdown / Docs:
33846545486 / 33846545485 / 33846545484 — SUCCESS
exact-main CI / Shutdown / Docs:
33846852935 / 33846852933 / 33846852922 — SUCCESS
Release workflow:
33847077606 — SUCCESS
release id: 382534812
published UTC: 2026-09-04T07:04:53Z
503 passed / 0 failed / 0 skipped
```

Public release:

```text
Junhyun-Helper.zip
asset id: 543938413
bytes: 80,560,157
SHA-256:
1384f2d42b843617ed61f90d4b2b0c5aa46bc616fd54e808cafabef2eb24f1f7

SHA256SUMS.txt
asset id: 543938412
bytes: 86
asset SHA-256:
4944f6e04b6ae191272db805dd8b60c8ef82fd6d7c0e4f4629e53d41755f5b0a
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9926904439
bytes: 241,611,421
SHA-256:
ce1946f12f8da5de755ac91696f2f1ed1b137bf76da5a32b198c36c0228e12a3
```

Release workflow `33847077606` checked out exact source `8ec677b1552f9deed55f98931c1df317e9bc4a4b`, downloaded exact-main artifact `9926904439`, verified ProductVersion/FIRST_RUN identity and package checksum, then published stable `v1.17.3`.

Public API readback confirmed:

- tag `v1.17.3` directly targets exact product source `8ec677b1552f9deed55f98931c1df317e9bc4a4b`;
- latest release is v1.17.3;
- draft: false;
- prerelease: false;
- both required assets are present;
- public ZIP digest equals exact-main package SHA-256 `1384f2d42b843617ed61f90d4b2b0c5aa46bc616fd54e808cafabef2eb24f1f7`.

## 2. v1.17.3 Stability / Optimization / UI Finishing

This PATCH added no new user-facing capability. It hardened the existing product after the v1.17.2 purity cleanup.

### Repeated-work efficiency

- Quest/Hideout/Items cache stable canonical content lookups.
- MainWindow derives all three workspaces from one authoritative immutable profile snapshot.
- Scanner reuses catalog snapshots and canonical item/quest/trader/station indexes.
- Scanner item requirement usage uses item→Quest/Hideout reverse indexes instead of repeated full scans.
- shared image caching uses per-path single-flight and weak decoded-image reuse.
- Map Quest marker scale follows `ScaleTransform.Changed` instead of a permanent 120ms polling timer.

### Correctness / concurrency

- manual Data Update, startup schema refresh, Map-triggered refresh, first-run provisioning and recovery use one content-operation gate.
- callers re-read after waiting where necessary, avoiding redundant network rebuilds.
- mutation failures rebuild authoritative presentation.
- Hideout pending level debounce is flushed before station switch.
- cancelling a Hideout rollback restores authoritative presentation rather than leaving an optimistic preview.

### Shutdown / lifetime

- MainWindow lifetime cancellation reaches profile I/O, Quest/Hideout/Items mutations, data update/prefetch, Scanner sync and PC diagnostics.
- queued progress UI callbacks fail closed after shutdown begins.
- ProgramUpdateCoordinator cancels release lookup/preparation on disposal and does not report shutdown cancellation as an update failure.
- existing Scanner runtime epoch/cancellation and Map async lifecycle contracts remain intact.

### UI / WPF

- shared Button style visibly indicates keyboard focus using the product accent.
- Quest/Hideout/Items/Ammo/Scanner minimum widths, scrolling, clipping and virtualization were audited.
- no speculative redesign or new UI feature was introduced.

## 3. Explicitly preserved boundaries

The pass intentionally did not change:

- Quest/Hideout domain semantics;
- current Items or Ammo product behavior;
- Scanner OCR thresholds, recognition pacing or matcher safety;
- Scanner correction/Ground Truth/diagnostic meaning;
- pinned Map/MiniMap donor revision;
- active Map compatibility bridges;
- supported old content/scanner schema read compatibility;
- user-owned profile/settings state.

Pinned Map donor revision remains:

`d933792b6042a51cea38dc44b686a096fe30de67`

## 4. Farming Guide status

Farming Guide remains completely removed from the current product.

There is no Farming Guide UI, planner, optimizer, Scanner bridge, persistence service or runtime domain model. Historical `%LocalAppData%/JunhyunHelper/farming-guide.json` remains inert and is not automatically deleted.

## 5. Validation evidence

### PR #294

Final validated PR head:

```text
230a5284f58f9d5eb8954c6042164bc5635fd35c
```

Passed:

- CI `33846545486`;
- Shutdown Race `33846545485`;
- Documentation Consistency `33846545484`;
- **503 passed / 0 failed / 0 skipped**;
- Windows Release build;
- win-x64 self-contained publish;
- actual published EXE Product UI / Map / Scanner smoke;
- graceful shutdown;
- package/checksum verification.

### Exact main

Exact product source:

```text
8ec677b1552f9deed55f98931c1df317e9bc4a4b
```

Passed:

- exact-main CI `33846852935`;
- exact-main Shutdown Race `33846852933`;
- exact-main Documentation Consistency `33846852922`;
- **503 passed / 0 failed / 0 skipped**;
- ProductVersion `1.17.3+8ec677b1552f9deed55f98931c1df317e9bc4a4b`;
- Windows publish;
- actual Product UI / full Map/Factory/MiniMap / Scanner runtime smoke;
- graceful shutdown + clean portable root;
- release package SHA-256 `1384f2d42b843617ed61f90d4b2b0c5aa46bc616fd54e808cafabef2eb24f1f7`;
- Actions artifact digest `ce1946f12f8da5de755ac91696f2f1ed1b137bf76da5a32b198c36c0228e12a3`.

### Public release

Release workflow `33847077606` succeeded.

Public `v1.17.3` release ID: `382534812`.

Public `Junhyun-Helper.zip`:

- id `543938413`;
- 80,560,157 bytes;
- SHA-256 `1384f2d42b843617ed61f90d4b2b0c5aa46bc616fd54e808cafabef2eb24f1f7`.

Public `SHA256SUMS.txt`:

- id `543938412`;
- 86 bytes;
- asset SHA-256 `4944f6e04b6ae191272db805dd8b60c8ef82fd6d7c0e4f4629e53d41755f5b0a`.

## 6. Current schemas / pinned dependencies

```text
Desktop: 1.17.3
Content write/read: v12 / v3-v12
user.db: v1
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
Map donor revision: d933792b6042a51cea38dc44b686a096fe30de67
```

## 7. Canonical references

- `docs/PROJECT_STATE.json`
- `docs/.release-v1.17.3-status.json`
- `docs/RELEASE_NOTES_V1.17.3.md`
- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`
- `docs/CURRENT_STATE.md`
- `docs/ACTIVE_WORK.md`

## 8. Current work status

v1.17.3 stability/optimization/UI-finishing maintenance is implemented, validated, merged, published and publicly verified.

`docs/ACTIVE_WORK.md` is closed (`NONE`).

Actual Tarkov play validation on the user's own environment remains separately tracked as `PENDING`; this does not make the automated implementation/release validation incomplete.
