# v1.2.1 — Scanner stability and accuracy hardening

Status: **RELEASE CANDIDATE — public release not yet published**

Prepared: 2026-08-22 KST

## Product scope

v1.2.1 is a PATCH hardening release. It does not add a new Scanner product feature and does not tune recognition thresholds from uncollected live-game data. The scope is deterministic reliability, lifecycle, cache-consistency and runtime-efficiency work that can be justified from code and automated validation alone.

The existing v1.2.0 recognition contract remains:

```text
Tarkov display pixels
→ detail structural candidates
→ close + magnifier + title-field refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ official-name semantic matching
   OR conservative current-catalog Tarkov-font visual recovery
→ confidence + top1/top2 margin gates
→ Item ID
→ local presentation / Mini Scanner
```

False positives remain worse than misses. No confidence or margin threshold is loosened in this release.

## Hardening changes

### Tarkov title-font cache generation

- `resources.assets` is no longer loaded as one large managed byte array merely to find embedded title fonts.
- SFNT discovery uses a bounded streaming scan and reads only validated font payloads by random access.
- extracted Bender/Noto cache files are tied to the live Tarkov `resources.assets` path, length and last-write stamp through `scanner/fonts/font-cache.json`;
- the locally cached font binaries are hashed into a generation key;
- loaded fonts and rendered visual templates are invalidated when the Tarkov source generation changes;
- an interrupted extraction cannot silently combine an older Bender variant with a newer Noto font;
- legacy v1.2.0 cache freshness checks consider every present Bender variant plus Noto rather than only one Bender file;
- corrupt or unusable font metadata/cache remains fail-soft: font recovery is skipped rather than turning Scanner into a fatal error.

Game font binaries are still not redistributed. Scanner only reads the user's installed Tarkov resources read-only and keeps the required extracted font payloads in JunhyunHelper's local Scanner cache.

### Visual recovery cache bounds

- OCR-guided template cache is bounded;
- full-catalog glyph-mask and aspect caches are bounded;
- all visual cache keys include the exact font generation;
- generation changes clear stale rendered templates before reuse.

This prevents long Scanner sessions from accumulating an unbounded number of rendered masks and prevents templates rendered with an older Tarkov font generation from participating in a newer comparison.

### Mini Scanner inventory-context probe coalescing

Continuous Scanner may refresh a verified item every 350 ms, but inventory/stash visibility OCR no longer queues another independent probe for every refresh.

- at most one inventory-context probe runs at a time;
- repeated `Show` calls replace the pending snapshot instead of building a serialized OCR backlog;
- item changes cancel the stale probe;
- epoch validation prevents a late result from an older item from becoming visible;
- the existing two-anchor Korean inventory/stash fail-closed gate is unchanged.

### One-shot/context lifecycle

- a one-shot scan restores a paused continuous mode only when the latest user state still requests exactly that same mode;
- profile/GameMode monitor changes are serialized against one-shot state mutation and re-read the latest context after acquiring the gate;
- a stale context tick therefore cannot restart a previous profile/mode after a one-shot scan.

### Shutdown-safe font-aware OCR

Font-aware OCR now uses an active-operation lease. Shutdown requests disposal immediately, but Skia/font resources are released only after the last already-running title recognition leaves the operation. New operations are rejected after disposal begins. This avoids a use-after-dispose race without blocking the WPF UI thread waiting for Scanner work to finish.

### Capture allocation hardening

`PrintWindow` visual-content validation now samples the locked bitmap directly. It no longer copies the entire 1440p/4K frame into a managed `byte[]` merely to inspect a sparse set of pixels before the actual detector performs its required frame copy.

### Diagnostic evidence fidelity

Title-anchor diagnostics now preserve the actual close/magnifier component scores instead of treating every merely-present anchor as perfect confidence. This changes diagnostic fidelity, not Item ID acceptance thresholds.

## Compatibility

```text
Desktop version: 1.2.1
Content schema: v7
Readable Content schemas: v3-v7
user.db schema: v1
Scanner display settings schema: v3
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.2.0 -> v1.2.1 mandatory Game Content update: none
v1.2.0 -> v1.2.1 user.db migration: none
```

Existing Profile / Quest / Hideout / Inventory / Items / Ammo / Map / MiniMap / Scanner catalog and display settings remain compatible.

## Pre-release verification

The final static-hardening candidate before the version/documentation commit was validated in CI run `32539676032`:

- Windows Release build: PASS;
- automated tests: **255 passed / 0 failed / 0 skipped**;
- win-x64 self-contained single-file publish: PASS;
- package-root/dependency audit: PASS;
- actual published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke: PASS;
- normal Main Window close / process exit / clean portable root: PASS.

The version/documentation commit must pass the same gate before merge.

## Deferred empirical work

Latest live Tarkov end-to-end calibration remains a separate user-assisted validation activity. Misses or false positives found in actual play should be investigated from observed evidence (`scanner.log` + `인식 이미지`) before any detector/OCR/visual threshold is changed. v1.2.1 deliberately does not guess those live thresholds in advance.

## Public verification

This section is completed only after the exact merged release source is built, a Draft asset is re-downloaded and verified, the release becomes public/latest, and the public asset is independently re-downloaded and smoke-tested.
