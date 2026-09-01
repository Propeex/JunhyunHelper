# 준현 헬퍼 v1.14.1

Status: **PUBLIC VERIFIED**

## Farming Guide exact storage-layout fail-closed 보강

v1.14.0에서 추가한 product-owned exact multi-grid layout은 current Tarkov storage mechanics와 구조가 정확히 일치할 때만 사용해야 한다.

v1.14.0 공개 이후 release-closure review에서 `FarmingGuideStorageVisualLayoutResolver`가 layout identity, grid count, positive dimensions, resulting non-overlap은 검증하지만 **각 grid index의 expected width/height signature 자체는 비교하지 않는 구현 누락**이 확인됐다.

따라서 Tarkov가 grid의 가로 또는 세로 크기만 변경하고 기존 exact 좌표가 우연히 서로 겹치지 않는 경우 stale visual coordinates가 계속 적용될 수 있었다.

v1.14.1은 이 회귀를 수정한다.

- product-owned exact profile에 grid별 expected width/height signature를 함께 보존한다.
- exact layout 적용 전 각 live grid index의 width/height가 expected signature와 정확히 일치하는지 검증한다.
- 단 하나의 dimension mismatch라도 exact layout을 거부한다.
- mismatch 시 storage legality/filter/item footprint는 current Game Content를 그대로 사용하며 presentation만 finite compact fallback을 사용한다.
- 기존 non-overlap 검증은 profile corruption에 대한 secondary defense로 유지한다.
- A18 / ANA Tactical M1 / current product-owned exact profile의 정상 signature와 dimension drift 거부를 deterministic regression으로 고정한다.
- actual published-runtime A18 smoke fixture도 동일 verified signature를 사용한다.

## 변경하지 않는 계약

- v1.14.0 recursive assembly / inline compatible-item picker
- weapon / helmet / armor attachment compatibility
- nested storage drag/drop 및 persistence
- Farming Guide user-state schema v1
- Game Content write schema v10 / readable v3-v10
- Scanner / Map / Quest / Hideout / Ammo behavior

## 공개 검증

```text
exact source/tag target:
add12c1b160f54e494d549978073f25e27cc4191
529 passed / 0 failed / 0 skipped
exact-main CI: 33456851817 — SUCCESS
Shutdown Race: 33456851818 — SUCCESS
Documentation Consistency: 33456851901 — SUCCESS
Release workflow: 33457066723 — SUCCESS
release id: 380147230
```

Public ZIP:

```text
bytes: 80,630,913
SHA-256:
b1216d9c661be909aee8c4a3f4eeb199b03eae46ba1f91799172bf8fd0074921
```

Full evidence: `docs/RELEASE_1.14.1.md`.
