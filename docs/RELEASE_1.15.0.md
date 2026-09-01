# RELEASE — v1.15.0

Status: **PUBLIC VERIFIED**  
Published UTC: **2026-09-01T03:49:49Z**

## Immutable product identity

```text
version/tag: v1.15.0
exact product source/tag target:
b974d56f32d073ce21a5de4171737670f83261f3
release id: 380200480
draft: false
prerelease: false
latest stable: true
```

`refs/tags/v1.15.0`, release target, and GitHub `/releases/latest` all resolve to the exact product source above. Later documentation-only main commits are not v1.15.0 product sources and must not replace its public assets.

## Validation evidence

Validated candidate head:

```text
397c82b8911597128c5878e7974db6a7822888d8
CI: 33466090956 — SUCCESS
Shutdown Race CI: 33466090958 — SUCCESS
Documentation Consistency: 33466090940 — SUCCESS
```

PR #255 contained the Draft implementation and accumulated the candidate verification above. The GitHub connector's draft-to-ready mutation failed due to a connector-side GraphQL schema incompatibility. The same branch/head was therefore reopened unchanged as non-draft PR #256 and squash-merged.

Exact-main product source:

```text
b974d56f32d073ce21a5de4171737670f83261f3
CI: 33467376556 — SUCCESS
Shutdown Race CI: 33467376508 — SUCCESS
Documentation Consistency: 33467376529 — SUCCESS
540 passed / 0 failed / 0 skipped
ProductVersion: 1.15.0+b974d56f32d073ce21a5de4171737670f83261f3
```

The exact-main CI verified Windows Release build, deterministic Core tests, self-contained win-x64 publish, actual published EXE Product UI/Map/graceful-shutdown smoke, release-package/checksum verification, and artifact upload. Shutdown Race independently verified closing the Main Window during active async product work.

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9785383239
archive bytes: 241,875,746
archive SHA-256:
6ba4c5819119a230ee02e4f7c2cb093679527623e3ab9665b8ebc05dee5936ae
```

Release workflow:

```text
run: 33467575493 — SUCCESS
source main commit: b974d56f32d073ce21a5de4171737670f83261f3
verified artifact id: 9785383239
```

## Public assets

```text
Junhyun-Helper.zip
asset id: 538909239
bytes: 80,647,419
SHA-256:
95f62c7d795f1954c3fd3437b17d9e15db05f5ab113f95df97055d15061bc76a

SHA256SUMS.txt
asset id: 538909237
bytes: 86
asset SHA-256:
5b8101bf0e086952ee12d4070e678cd1e0b5406e0c32ae91b7bf2562e7ab2ecb
```

The public ZIP size/hash are the verified release package produced from the exact-main CI artifact.

## v1.15.0 scope

v1.15.0 extends Farming Guide from a raid-start loadout/inventory editor into an explicit raid-session advisor while preserving the existing editor contracts.

- `레이드 시작 / 레이드 종료` isolated session snapshot/rollback;
- manual raid-session edits immediately become recommendation input without overwriting the saved preset;
- Scanner-recognized items can create one pending Farming Guide instruction;
- Mini Scanner presents the current recommendation;
- recommendation commit requires the configured explicit accept hotkey;
- session revision invalidates stale guidance after user state changes;
- hover + `F` item/equipment/storage/cell locks protect automatic placement/replacement and allow reserved empty cells;
- hovering a Farming Guide search result and pressing `T` exercises the same recommendation pipeline with simulated Scanner input;
- Scanner worker callbacks cross a WPF Dispatcher boundary before Farming Guide UI/state interaction;
- Farming Guide state schema v2 persists locks while remaining compatible with v1;
- Scanner display settings schema v10 adds the Farming Guide accept/display settings while preserving v9 settings.

The recommendation system remains advisory only. It does not read Tarkov process memory, mirror internal raid inventory, automate game input, or estimate extraction probability.

## Historical note

v1.14.1 remains the immediately preceding immutable release and continues to document the exact-storage-layout dimension-signature guard introduced there. v1.15.0 preserves that guard and adds the raid-session recommendation layer on top of it.
