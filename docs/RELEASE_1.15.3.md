# RELEASE — v1.15.3

Status: **PUBLIC VERIFIED**  
Published UTC: **2026-09-01T08:35:55Z**

## Immutable product identity

```text
version/tag: v1.15.3
exact product source/tag target:
c35204da66eb0af454b50550c830b071a0897835
release id: 380333729
draft: false
prerelease: false
latest stable: true
```

`refs/tags/v1.15.3`, the release target and GitHub `/releases/latest` all resolve to the exact product source above. Documentation-only commits after this release are not v1.15.3 product sources and may not replace its public assets.

## Candidate validation

Final non-draft PR:

```text
PR: #265
validated head: db82512e6e723f2d85ed0ddf3f3c7c9b0e3a70af
CI: 33487099126 — SUCCESS
Shutdown Race CI: 33487099119 — SUCCESS
Documentation Consistency: 33487099201 — SUCCESS
563 passed / 0 failed / 0 skipped
```

The same implementation was first validated through draft PR #264. The connector's draft-to-ready GraphQL mutation failed on a repository schema field (`fullDatabaseId`), so the same branch was reopened as non-draft PR #265. This administrative workaround did not roll back or replace the implementation.

## Exact-main validation

```text
exact product source:
c35204da66eb0af454b50550c830b071a0897835
CI: 33487466031 — SUCCESS
Shutdown Race CI: 33487466005 — SUCCESS
Documentation Consistency: 33487465946 — SUCCESS
563 passed / 0 failed / 0 skipped
```

The exact-main CI verified Release/XAML build, deterministic tests, self-contained Windows x64 publish, actual published EXE Product UI/Scanner/Farming Guide/Map runtime smoke, graceful shutdown, package/checksum verification and artifact upload. Farming Guide runtime smoke included source-backed specialized nested storage filter behavior, neutral↔locked accent border behavior, and compatible dedicated nested storage priority over general root storage.

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9792459273
archive bytes: 241,909,375
archive SHA-256:
c0aba02d6a465734c841b044776dfcf087bab9b29141b23c71ffb5a0a65c6cb2
```

## Release workflow

```text
run: 33487795730 — SUCCESS
source main commit: c35204da66eb0af454b50550c830b071a0897835
verified artifact id: 9792459273
```

The Release workflow checked out the exact verified main commit, downloaded the exact-main artifact, verified release identity and package checksums, created `v1.15.3`, uploaded the public assets and published it as latest stable.

## Public assets

```text
Junhyun-Helper.zip
asset id: 539249489
bytes: 80,659,355
SHA-256:
a22a426de32aa20a4c158018d98a6eec96b39d460d367d33d9d970d7e2581d99

SHA256SUMS.txt
asset id: 539249490
bytes: 86
asset SHA-256:
286e27a9db1394d1a4487c5b26598f08998bb03e07e21fa116dc4fca5844fdde
```

GitHub public release metadata reports the exact same ZIP byte count and SHA-256 digest produced by exact-main CI.

## v1.15.3 scope

v1.15.3 is a Farming Guide PATCH correction.

- ordinary stored-item cards use the neutral border; explicit `F` lock uses accent/yellow;
- all source-backed real storage grids survive complete-equipment runtime projection;
- specialized containers such as Key tool are not hardcoded by name and use current source grids/filters;
- recursive container-in-container storage remains addressable through `ParentInstanceId`, including containers inside Secure Container;
- a positive-allow-list nested grid that accepts an incoming item is preferred over general root storage empty space;
- search-result hover + `T` simulated scan works even while the search TextBox retains keyboard focus;
- simulated scans use the real Farming Guide planning path and can on-demand load verified same-mode local Scanner catalog data when capture is disabled/uninitialized;
- v1.15.2 complete-equipment boundary remains intact: no weapon/helmet/armor internal editing or internal raid Equip/ReplaceEquip targets.

## External validation

Automated release verification is complete. Separate real-environment evidence remains open:

- user actual-PC/Tarkov play validation of v1.15.3 Farming Guide visuals/behavior;
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis when that work resumes.
