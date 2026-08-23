# v1.2.2 — Scanner catalog mode-transition hardening

Status: **PUBLIC / VERIFIED**

Released: 2026-08-23 KST

```text
version: v1.2.2 PUBLIC RELEASE / VERIFIED
release source: e3925cbc55215c7de0502c9b6b1ff1428d2f272b
final PR CI: 32590303579 — SUCCESS
exact-source release run: 32590701086 — SUCCESS
independent public finalizer: 32607942093 — SUCCESS
automated tests: 256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.2-win-x64.zip
bytes: 80,302,910
SHA-256: 125d4a5b0e6db64f6772cc63c112f13cbcdac2fb7bc9ce501313ca2fc3645d7c
ProductVersion: 1.2.2+e3925cbc55215c7de0502c9b6b1ff1428d2f272b
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

Public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v1.2.2

## Product scope

v1.2.2 is a PATCH stability release. It does not add a new Scanner feature and does not alter OCR, detector, title-anchor, Tarkov-font visual-recovery, confidence, top1/top2 margin, market-price or Needed Items semantics.

The release fixes a catalog state-ordering race discovered during post-v1.2.1 static analysis.

Scanner catalog operations can replace the same in-memory Item identity/market state from two sources:

```text
network RefreshAsync
local LoadCacheAsync
```

Before v1.2.2, network refreshes were serialized by `_refreshGate`, but local cache loads were not. During a profile/GameMode transition, this ordering was possible:

```text
old-mode network refresh starts
→ newer profile transition loads new-mode disk cache
→ old-mode refresh completes later
→ old mode overwrites the newer in-memory catalog
```

That could leave Scanner identity and market data associated with an older GameMode after a newer profile transition.

## Changes

### One catalog-state operation boundary

`ScannerCatalogService.LoadCacheAsync` now participates in the same operation gate as `RefreshAsync`.

Both operations can replace:

- loaded GameMode;
- generated-at timestamp;
- Item ID dictionary;
- semantic matcher catalog;
- OCR character-policy catalog;
- Scanner catalog diagnostics.

Serializing those writers makes operation ordering deterministic.

### Cross-mode clear moved inside the gate

`RefreshAsync` no longer clears the in-memory catalog for a different mode before entering the operation gate.

The cross-mode clear occurs only after that refresh owns the gate. This prevents an older queued operation from mutating shared catalog state outside the serialized boundary.

### Shutdown/cancellation behavior

`LoadCacheAsync` uses the Scanner catalog lifetime cancellation token while waiting for the shared gate. Application shutdown therefore does not leave a cache-load operation blocked indefinitely behind an in-flight refresh.

The gate itself remains undisposed during shutdown because an already-running refresh or cache load can still execute its `finally` release path.

### Deterministic regression test

`ScannerCatalogConcurrencyTests.LoadCacheAsync_WaitsForInFlightRefreshAndKeepsNewestMode` reproduces the dangerous ordering explicitly:

1. seed a valid PvE cache;
2. start and deliberately block an older Regular refresh;
3. request a newer PvE cache load;
4. prove the newer load waits rather than replacing shared state outside the operation boundary;
5. release the older refresh;
6. prove the newer PvE load is the final writer;
7. verify a healthy PvE catalog remains loaded.

This raised the automated test count from 255 to 256.

## Scanner contracts preserved

v1.2.2 deliberately keeps the v1.2.1 recognition and presentation contracts:

- Scanner Lab v3.8 structural candidate architecture;
- structural floor `0.34`;
- close/magnifier/title-field refinement;
- Windows `ko-KR` OCR primary path;
- current official Korean catalog as Item identity authority;
- conservative Tarkov-font visual recovery only when needed;
- OCR/visual acceptance confidence thresholds unchanged;
- top1/top2 margin unchanged;
- ambiguous/low-confidence result = no Item ID;
- scan-time network prohibited;
- game memory read / DLL injection / packet interception prohibited;
- `최고 상점가` = highest valid non-flea RUB sell price;
- `플리마켓 평균가` = positive `avg24hPrice`;
- `필요 개수` = `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`;
- Inventory-adjusted shortage is not Scanner `필요 개수`.

## Compatibility

```text
Desktop version: 1.2.2
Content schema: v7
Readable Content schemas: v3-v7
user.db schema: v1
Scanner display settings schema: v3
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.2.1 -> v1.2.2 mandatory Game Content update: none
v1.2.1 -> v1.2.2 user.db migration: none
```

Existing Profile / Quest / Hideout / Inventory / Items / Ammo / Map / MiniMap / Scanner catalog and Scanner display settings remain compatible.

## Verification history

The code fix and deterministic regression test first passed Windows CI before the release identity was changed. The final versioned PR #137 head then passed CI run `32590303579` with:

- Windows Release build;
- 256/256 automated tests;
- win-x64 self-contained single-file publish;
- ProductVersion / FIRST_RUN identity audit;
- package-root / PDB / nested-archive / forbidden-dependency audit;
- actual published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke;
- graceful Main Window close and clean portable-root verification.

PR #137 was squash-merged as exact product release source `e3925cbc55215c7de0502c9b6b1ff1428d2f272b`.

Exact-source release workflow run `32590701086` independently checked out that SHA and completed the v1.2.2 release pipeline. The public tag resolves exactly to the release source.

Because release metadata was not directly exposed by the connected inspection interface, an additional one-shot independent finalizer was added after release. Finalizer run `32607942093` independently required:

- successful v1.2.2 release-controller run;
- public, non-prerelease v1.2.2 state;
- v1.2.2 as GitHub latest release;
- exact public tag → `e3925cbc55215c7de0502c9b6b1ff1428d2f272b`;
- public asset set containing exactly the Windows ZIP and `SHA256SUMS.txt`;
- downloaded ZIP SHA-256 matching `SHA256SUMS.txt`;
- exact public package root;
- exact ProductVersion and FIRST_RUN identity;
- public-downloaded EXE Product UI / Scanner / Map smoke;
- graceful shutdown and no portable-root log pollution.

The resulting independent verification record is preserved in `docs/.release-v1.2.2-status.json`.

## Deferred empirical work

Latest live Tarkov end-to-end calibration remains separate from deterministic code hardening. Any recognition miss or false positive found during actual play should be investigated using `scanner.log` and `인식 이미지` before detector/OCR/visual thresholds are changed.
