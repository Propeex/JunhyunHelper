# RELEASE — v1.15.4

Status: **PUBLIC VERIFIED**  
Published UTC: **2026-09-01T11:12:15Z**

## Immutable product identity

```text
version/tag: v1.15.4
exact product source/tag target:
c27daf2177b643ee16d4a3d5b0997e54a267c2c7
release id: 380429049
draft: false
prerelease: false
latest stable: true
```

`refs/tags/v1.15.4`, the release target and GitHub `/releases/latest` all resolve to the exact product source above. Documentation-only commits after this release are not v1.15.4 product sources and may not replace its public assets.

## Candidate validation

Final non-draft PR:

```text
PR: #268
validated head: da9e788a8494734149cfa0e65eff3535e14d2bac
CI: 33500484624 — SUCCESS
Shutdown Race CI: 33500484673 — SUCCESS
Documentation Consistency: 33500484510 — SUCCESS
585 passed / 0 failed / 0 skipped
```

The exact same branch/head was first validated through draft PR #267. The connected GitHub draft-to-ready GraphQL action failed on the repository API schema field `fullDatabaseId`, so #267 was closed without merge and the unchanged branch/head was opened as non-draft PR #268. #268 re-ran all required gates and passed before merge.

## Exact-main validation

```text
exact product source:
c27daf2177b643ee16d4a3d5b0997e54a267c2c7
CI: 33500904378 — SUCCESS
Shutdown Race CI: 33500904396 — SUCCESS
Documentation Consistency: 33500904356 — SUCCESS
585 passed / 0 failed / 0 skipped
```

The exact-main CI verified Release/XAML build, deterministic tests, self-contained Windows x64 publish, actual published EXE Product UI/Map/Farming Guide runtime smoke, graceful shutdown, package/checksum verification and artifact upload.

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9797756949
archive bytes: 242,014,938
archive SHA-256:
2ab185334c441dfa44f8d1afb774e7c7c6815df07849563ba865210a9b5857bb
```

## Release workflow

```text
run: 33501233130 — SUCCESS
source main commit: c27daf2177b643ee16d4a3d5b0997e54a267c2c7
verified artifact id: 9797756949
```

The Release workflow checked out the exact verified main commit, downloaded the exact-main artifact, verified Desktop version/FIRST_RUN identity and package checksums, created `v1.15.4`, uploaded the public assets and published it as latest stable.

## Public assets

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

GitHub public release metadata reports the asset sizes and SHA-256 digests above. The release workflow independently verified `SHA256SUMS.txt` against the exact-main `Junhyun-Helper.zip` before publication.

## v1.15.4 scope

v1.15.4 is a Farming Guide real-raid planning and storage hardening PATCH.

- before destructive replacement/discard, the advisor can legally move/rotate multiple unlocked stored items to recover fragmented contiguous capacity;
- repacking remains bounded and deterministic and preserves source-backed filters, dedicated-container preference, reservations, locks and nested parent/descendant constraints;
- populated nested containers are protected from value-only destructive replacement;
- source-backed nested Workbench grids avoid manufactured horizontal scrolling/cell clipping when the complete surface physically fits the viewport;
- market/trader value is not treated as equipment performance;
- source-backed protective class, carrier capacity and headset facts support conservative top-level equipment upgrades only when superiority is proven and current modeled contents can be preserved;
- body armor + ordinary rig -> superior armored rig is one atomic fail-closed pending transaction with full modeled rig-content preservation;
- complete-equipment internals stay closed: no weapon/helmet attachment or armor-plate user-state inference;
- Game Content write schema is v11, persisting armor/headset comparison facts while v3-v10 remain readable as offline last-known-good and can be opportunistically refreshed through the normal transactional update boundary.

Canonical decision:

- `docs/DECISION_V1.15.4_FARMING_GUIDE_REPACKING_EQUIPMENT_UPGRADES.md`

## External validation

Automated release verification is complete. Separate actual-PC/Tarkov play validation remains pending and is not required for the immutable automated release identity above.
