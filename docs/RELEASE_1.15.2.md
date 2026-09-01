# RELEASE — v1.15.2

Status: **PUBLIC VERIFIED**  
Published UTC: **2026-09-01T07:24:43Z**

## Immutable product identity

```text
version/tag: v1.15.2
exact product source/tag target:
f4974ee6bed5047865581240197f7f0e2787ba7c
release id: 380290463
draft: false
prerelease: false
latest stable: true
```

`refs/tags/v1.15.2`, the release target, and GitHub `/releases/latest` all resolve to the exact product source above. Later documentation-only main commits are not v1.15.2 product sources and must not replace its public assets.

## Candidate validation

Final non-draft PR:

```text
PR: #262
validated head: 1662cc86f6298fc3a13bbcc591d38ae8c8e0787d
CI: 33481383672 — SUCCESS
Shutdown Race CI: 33481383604 — SUCCESS
Documentation Consistency: 33481383640 — SUCCESS
562 passed / 0 failed / 0 skipped
```

The same implementation was initially carried by draft PR #261. The GitHub connector's draft-to-ready mutation failed because of a connector-side GraphQL schema mismatch, so the exact same branch/head was reopened as non-draft PR #262. No product source change was introduced by that administrative workaround.

## Exact-main validation

```text
exact product source:
f4974ee6bed5047865581240197f7f0e2787ba7c
CI: 33481524940 — SUCCESS
Shutdown Race CI: 33481524896 — SUCCESS
Documentation Consistency: 33481524999 — SUCCESS
562 passed / 0 failed / 0 skipped
```

The exact-main CI verified Release build, deterministic tests, self-contained Windows x64 publish, actual published EXE Product UI + Farming Guide + Map + Scanner runtime smoke, graceful shutdown, release-package/checksum verification and artifact upload. Shutdown Race independently verified Main Window close during active async work.

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9790251740
archive bytes: 241,895,658
archive SHA-256:
57665346651872dd4f351241dabe77de09349150ebb2d8664f8d5f626a8daf65
```

## Release workflow

```text
run: 33481956300 — SUCCESS
source main commit: f4974ee6bed5047865581240197f7f0e2787ba7c
verified artifact id: 9790251740
```

The Release workflow downloaded the exact-main artifact, verified the executable/version and `FIRST_RUN_KO.txt`, verified the package against `SHA256SUMS.txt`, published v1.15.2 as latest stable and read the public release back successfully.

## Public assets

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

GitHub release asset metadata reports the same package digest published by the verified release workflow.

## v1.15.2 scope

v1.15.2 is a Farming Guide PATCH correction that replaces the previous user-facing equipment-assembly model with complete equipment.

- weapons, helmets, armor and other gear are opaque complete items;
- weapon/helmet attachment and armor-plate editing UI is removed;
- equipment-internal raid Equip/ReplaceEquip targets are removed;
- top-level equipment-slot equip/replace guidance remains;
- legacy persisted equipment assembly state is normalized to root-only equipment state;
- only stored Backpack/Rig items expose nested storage detail;
- nested storage detail is compact and sized from actual grid footprint;
- root carrier storage remains on the main Farming Guide surface;
- authoritative complete/default-preset imagery is preferred and fabricated assembly composites are not used;
- equipment images use more of their slot while preserving aspect ratio.

The v1.15.1 pending, lock, Special Slot and explicit acceptance contracts remain in force unless superseded by the complete-equipment boundary.

## External validation

Automated release verification is complete. Separate real-environment evidence remains open:

- user actual-PC/Tarkov play validation of v1.15.2 Farming Guide visuals/behavior;
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis when that work resumes.
