# DECISION — v1.14.1 exact storage-layout dimension signature guard

Status: **CONFIRMED / PUBLIC VERIFIED**  
Date: **2026-09-01 KST**  
Release: **v1.14.1**

## Context

v1.14.0 introduced product-owned exact visual layouts for a deliberately small set of verified multi-grid carriers. The intended contract was that exact coordinates are presentation metadata only and may be used only while current Tarkov storage mechanics still match the verified structure.

Post-release review found that the public v1.14.0 resolver verified layout identity, grid count, positive dimensions and resulting non-overlap but did not persist/compare each grid index's expected width/height. A dimension-only Tarkov change could therefore preserve stale exact coordinates when rectangles happened not to overlap.

The v1.14.0 release remains immutable historical evidence. The implementation gap is corrected by v1.14.1.

## Decision

A product-owned exact storage-layout profile is a pair of facts for every grid index:

```text
visual coordinate: X / Y
mechanical signature: expected Width / Height
```

`FarmingGuideStorageVisualLayoutResolver.TryResolve` may return an exact layout only when all of the following hold:

1. a verified product-owned layout profile is resolved by the current layout identity or explicit verified item alias;
2. live grid count exactly equals profile grid count;
3. every live grid has positive dimensions;
4. every live grid index has exactly the expected `Width` and `Height`;
5. computed coordinates/bounds are finite;
6. resulting rectangles do not overlap.

Any failure returns `false`. The caller then uses the existing finite compact visual fallback.

## Authority boundary

The exact visual profile never overrides current Game Content mechanics.

Current validated Game Content remains authoritative for:

- grid count and width/height;
- allowed/excluded filters;
- item footprint and rotation;
- actual placement legality.

Product-owned coordinates control presentation only. A signature mismatch therefore does not fabricate new grids or alter compatibility; it merely stops claiming the stale coordinates are exact.

## Verification

Regression coverage includes:

- verified A18 signature accepted;
- verified ANA Tactical M1 signature accepted;
- current product-owned MBSS profile signature accepted;
- grid-count drift rejected;
- width drift rejected;
- non-overlapping height drift rejected;
- unknown profile rejected;
- published-runtime A18 exact-layout smoke uses the same verified dimensions and retains each `GridDropTarget.GridIndex` identity.

Public v1.14.1 exact product source:

```text
add12c1b160f54e494d549978073f25e27cc4191
529 passed / 0 failed / 0 skipped
```

See `docs/RELEASE_1.14.1.md` for complete public release evidence.
