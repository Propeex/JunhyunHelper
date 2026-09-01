# RELEASE — v1.14.1

Status: **PUBLIC VERIFIED**  
Published UTC: **2026-09-01T01:01:22Z**

## Immutable product identity

```text
version/tag: v1.14.1
exact product source/tag target:
add12c1b160f54e494d549978073f25e27cc4191
release id: 380147230
draft: false
prerelease: false
latest stable: true
```

`refs/tags/v1.14.1`, release target, and GitHub `/releases/latest` all resolve to the exact product source above. Later documentation-only main commits are not v1.14.1 product sources and must not replace its public assets.

## Validation evidence

PR #253 final exact head:

```text
42abdc7945c8f12a26553c6d0386cdadc6e41803
CI: 33456589868 — SUCCESS
Shutdown Race CI: 33456589884 — SUCCESS
Documentation Consistency: 33456589878 — SUCCESS
```

Exact-main product source:

```text
add12c1b160f54e494d549978073f25e27cc4191
CI: 33456851817 — SUCCESS
Shutdown Race CI: 33456851818 — SUCCESS
Documentation Consistency: 33456851901 — SUCCESS
529 passed / 0 failed / 0 skipped
ProductVersion: 1.14.1+add12c1b160f54e494d549978073f25e27cc4191
```

The exact-main CI verified Windows Release build, self-contained win-x64 publish, actual published EXE Product UI/Farming Guide/Map smoke, exact storage-layout runtime smoke, graceful shutdown, clean portable root, package/checksum verification, and artifact upload.

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9781796510
archive bytes: 241,822,850
archive SHA-256:
c55c6da388c078c9cf011b5db35b2797424daa8d59cdd7a7c9ed232acfd97031
```

Release workflow:

```text
run: 33457066723 — SUCCESS
source main commit: add12c1b160f54e494d549978073f25e27cc4191
verified artifact id: 9781796510
```

## Public assets

```text
Junhyun-Helper.zip
asset id: 538731592
bytes: 80,630,913
SHA-256:
b1216d9c661be909aee8c4a3f4eeb199b03eae46ba1f91799172bf8fd0074921

SHA256SUMS.txt
asset id: 538731593
bytes: 86
asset SHA-256:
a3817550bf8d8ed0813606ddc4ae511d3f989b473cedea8c1e137e9209b7944a
```

The public ZIP size/hash exactly match the exact-main package produced by CI.

## Corrective scope

v1.14.0 introduced recursive assembly editing, inline compatible-item selection and product-owned exact multi-grid visual layouts. During post-release closure review, one contract gap was found: the public v1.14.0 resolver did not persist and compare the expected width/height for every grid index. A Tarkov dimension-only drift could therefore keep stale exact coordinates when the resulting rectangles still did not overlap.

v1.14.1 corrects this without changing v1.14.0 historical bytes:

- every product-owned exact profile contains expected per-index grid width/height;
- exact layout activation requires exact layout identity, grid count and per-index width/height match;
- any dimension mismatch falls back to the finite compact visual layout;
- current Game Content remains the authority for storage legality, filters and item footprints;
- non-overlap remains a secondary corruption guard;
- deterministic tests cover normal signatures, grid-count drift, width drift and non-overlapping height drift;
- the actual published-runtime A18 smoke fixture uses the same verified signature.

## Historical v1.14.0 note

v1.14.0 remains an immutable public historical release at source `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`. Its recursive assembly and inline picker functionality remains part of the current product, while its incomplete exact-dimension activation guard is superseded by v1.14.1.
