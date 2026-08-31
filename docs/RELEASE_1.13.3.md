# RELEASE v1.13.3 — PUBLIC / VERIFIED

Date: **2026-08-31 KST**

## Release identity

```text
version: v1.13.3
status: PUBLIC STABLE / VERIFIED
exact product release source:
9a0064d81dca4c2cffcb01c55742d46298d235de
PR: #248 — MERGED
superseded draft PR: #247 — CLOSED UNMERGED (same validated head; connector could not mark draft ready)
validated PR head: b39f7156f458fd6fd513b5eca551e522d5a12343
PR exact-head CI: 33382678094 — SUCCESS
PR exact-head Shutdown Race CI: 33382678096 — SUCCESS
PR exact-head Documentation Consistency: 33382678065 — SUCCESS
exact-main CI: 33382979766 — SUCCESS
exact-main Shutdown Race CI: 33382979902 — SUCCESS
exact-main Documentation Consistency: 33382979845 — SUCCESS
release workflow: 33383407835 — SUCCESS
release id: 379676479
published UTC: 2026-08-31T10:40:13Z
513 passed / 0 failed / 0 skipped
```

Tag `refs/tags/v1.13.3`, release `target_commitish`, GitHub `/releases/latest`, and the exact-main product source all resolve to `9a0064d81dca4c2cffcb01c55742d46298d235de`. The release is `draft=false` and `prerelease=false`.

## Exact-main artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9754610879
archive bytes: 241,795,611
archive SHA-256:
ae3fb9857920ab61e79c46da01d030fbded4a90eca27ec306e7f5661beb0cc3a
```

The Release workflow downloaded this exact Actions artifact from exact-main CI and verified its digest before publication. The public product was not rebuilt separately.

## Public assets

```text
Junhyun-Helper.zip
asset id: 537835859
bytes: 80,620,064
SHA-256 / GitHub digest:
704afb5e376f9087dd57c1795d8b95397c06a020acd9545fe80c5fc1b546b7b7

SHA256SUMS.txt
asset id: 537835858
bytes: 86
SHA-256 / GitHub digest:
2c74d9c4e4f096c35eb3b4e45deb734af5b9df31306c9961d66c9aa7cd4e5b4d
```

Exact-main CI generated `Junhyun-Helper.zip` with SHA-256 `704afb5e376f9087dd57c1795d8b95397c06a020acd9545fe80c5fc1b546b7b7`; the Release workflow independently verified the same package hash, and GitHub's public asset digest matches it exactly.

## Product changes

v1.13.3 is a PATCH release that corrects Farming Guide live inventory interaction semantics found during v1.13.2 real-use validation.

- current Tarkov Secure Container classification is accepted while generic containers/cases such as Medicine Case are not misclassified as secure containers.
- nested bag/rig storage uses explicit parent instance identity, allowing bag-in-bag / rig-in-bag state to be represented and persisted.
- the old separate `장비 정보/장비 설정` Window and read-only preview workflow are removed.
- double-click opens an in-page workbench in the center column while item search remains usable.
- stored bags/rigs expose their actual storage grids as direct drag/drop surfaces.
- weapons, helmets, and armor expose actionable attachment/mod/replaceable plate slots as direct one-item drop targets.
- occupied one-item slots are not silently overwritten; the existing child must be dragged out first.
- moving nested containers preserves descendants; destructive delete/replacement removes the subtree; orphan/cycle states fail closed.
- upstream assembled weapon preset records (`ItemPropertiesPreset` / `preset`) are excluded only from Farming Guide search so the canonical base weapon and its actual mod slots are used.
- moving an item that owns an open workbench closes the workbench first, preventing stale write-back into a moved/removed owner.

## Preserved contracts

- current Tarkov `width × height` footprint and drag-time `R` rotation
- bounded grid snap, bounds, overlap, contiguous-space, and current filter validation
- current validated storage grids / equipment slots / attachment slots / armor slots / conflicts
- full raid-start preset save/load
- melee / PMC dogtag fixed setting separation
- profile-aware standard/expanded pockets
- filled carrier destructive replacement fail-closed
- impossible persisted state sanitization against current content/profile
- Farming Guide state schema v1; existing files remain backward compatible because missing `ParentInstanceId` is interpreted as a root placement

Loot value judgment, pickup/discard/replace recommendation, Scanner live recommendation, and continuous 1:1 mirroring of the actual raid inventory remain outside the current Farming Guide scope.

## Regression coverage / release gate

The exact-main product source passed:

- 513 deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained single-file publish
- ProductVersion `1.13.3+9a0064d81dca4c2cffcb01c55742d46298d235de`
- actual published EXE Product UI / Farming Guide / Map smoke
- Farming Guide live nested-storage and attachment-slot interaction smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- package root / forbidden dependency audit
- ZIP checksum manifest / actual hash equality
- exact-main Documentation Consistency
- exact-main Actions artifact upload and digest readback
- automatic verified Release workflow
- public tag / latest release / assets / GitHub digest readback

## Public verification

- release workflow `33383407835`: SUCCESS
- `/releases/latest`: `v1.13.3`
- tag ref `refs/tags/v1.13.3`: exact product source `9a0064d81dca4c2cffcb01c55742d46298d235de`
- release target: exact product source
- public ZIP metadata/digest: verified
- checksum asset metadata/digest: verified
- stable release: `draft=false`, `prerelease=false`

Any later documentation-only main commit is not the v1.13.3 product release source. The immutable historical product identity remains `9a0064d81dca4c2cffcb01c55742d46298d235de` and the public assets listed above.
