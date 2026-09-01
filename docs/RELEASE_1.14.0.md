# RELEASE v1.14.0 — PUBLIC / VERIFIED

Date: **2026-09-01 KST**

## Release identity

```text
version: v1.14.0
status: PUBLIC STABLE / VERIFIED
exact product release source/tag target:
9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
PR: #251 — MERGED
superseded draft PR: #250 — CLOSED UNMERGED
validated PR head:
c5ee50ba60f2bc7db461328608ec591f4320ccca
PR exact-head CI: 33453431628 — SUCCESS
PR exact-head Shutdown Race CI: 33453431625 — SUCCESS
PR exact-head Documentation Consistency: 33453431595 — SUCCESS
exact-main CI: 33453784868 — SUCCESS
exact-main Shutdown Race CI: 33453784901 — SUCCESS
exact-main Documentation Consistency: 33453784893 — SUCCESS
release workflow: 33454002732 — SUCCESS
release id: 380133403
published UTC: 2026-09-01T00:15:44Z
527 passed / 0 failed / 0 skipped
```

Draft PR #250 used the same validated branch but the connected GitHub ready-for-review mutation failed because its GraphQL response requested the removed `Repository.fullDatabaseId` field. The draft was closed unmerged and non-draft PR #251 was opened from the same exact branch head; no product code changed as part of that administrative replacement.

`refs/tags/v1.14.0`, the release `target_commitish`, GitHub `/releases/latest`, and the exact-main product source all resolve to `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`. The public release is `draft=false` and `prerelease=false`.

## Exact-main artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9780762947
archive bytes: 241,830,878
archive SHA-256:
1898028e10ef336b2dce35add94d2e1cf83b5c58c27c98649691fe11bdbe8632
```

Release workflow `33454002732` downloaded artifact `9780762947` from exact-main CI run `33453784868` and independently verified the Actions artifact digest before publication. The public release was not rebuilt separately.

Exact-main published executable identity:

```text
ProductVersion:
1.14.0+9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
```

## Public assets

```text
Junhyun-Helper.zip
asset id: 538692301
bytes: 80,633,458
SHA-256 / GitHub digest:
87728ce9e34a30a9b1eb735fe92b1a4a39f172f3b9cf536dfd12d88c8c35667b

SHA256SUMS.txt
asset id: 538692300
bytes: 86
SHA-256 / GitHub digest:
06ae3473f7fe87d62b0d05dac0d16640a55e30e8a8fd83e4770f962a8fc5dfe3
```

Exact-main CI generated `Junhyun-Helper.zip` with SHA-256 `87728ce9e34a30a9b1eb735fe92b1a4a39f172f3b9cf536dfd12d88c8c35667b`; Release workflow verified the same package hash, and GitHub's public asset digest matches it exactly.

## v1.14.0 product changes

v1.14.0 is the Farming Guide assembly/layout MINOR release.

- removes the obsolete PMC dogtag equipment surface while continuing to read legacy persisted values safely.
- introduces recursive weapon/helmet/armor assembly editing rather than limiting editing to root-level slots.
- exposes an in-page icon-based compatible-item picker for empty attachment/armor slots; no separate OS configuration window is used.
- keeps search-result drag/drop and inline click-to-equip on the same Core compatibility/conflict policy.
- uses authoritative imported default-preset composition images only for exact preset membership; arbitrary builds use a deterministic assembly-aware fallback.
- preserves `GridLayoutName` / `RigLayoutName` family identity in canonical Game Content.
- applies product-owned exact multi-grid visual coordinates only when the current live grid count/width/height signature matches exactly; unknown or stale metadata falls back to a finite compact layout instead of guessing authenticity.
- advances Game Content snapshot write schema to v10 while retaining v3-v10 read compatibility.
- keeps Farming Guide user-state schema v1.

## Preserved Farming Guide contracts

- raid-start Loadout / Inventory Editor; not a live raid inventory mirror.
- current Tarkov `width × height` footprint, carrier grids, filters, attachment/armor slots and conflicts remain mechanics authority.
- `R` rotation, bounded grid snap, bounds/overlap/contiguous-space/filter validation.
- nested storage parent-instance semantics and fail-closed orphan/cycle handling.
- occupied one-item attachment/plate slots are not silently overwritten.
- filled carrier destructive replacement remains fail closed.
- profile-aware standard/expanded pockets remain centralized.
- melee remains a user-level fixed setting outside per-profile presets.

## Regression / release gate

Exact product source `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1` passed:

- 527 deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained single-file publish
- ProductVersion/FIRST_RUN release-identity checks
- actual published EXE Product UI / Farming Guide / Map smoke
- Farming Guide recursive assembly / inline picker / exact-layout rendering and drop-target identity smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- package-root / forbidden-dependency audit
- ZIP/checksum equality
- exact-main Documentation Consistency
- exact-main Actions artifact upload/digest verification
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

## Immutability

Any later documentation-only main commit is **not** the v1.14.0 product release source. The immutable historical product identity remains the exact source/tag/assets listed above. A documentation-only main commit may produce different assembly metadata bytes for version 1.14.0, but Release workflow must only verify that the already-published v1.14.0 assets still exist and must not replace them.
