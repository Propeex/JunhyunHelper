# STATE — 현재 프로젝트 상태

> 복구 순서는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md`입니다.

기준일: **2026-09-05 KST**  
상태: **v1.17.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 공개 제품

```text
public stable: v1.17.4
exact product source/tag target:
2297a27332069e18ade56c53931002f7a4728338
validated PR head:
5ba3c504e4da8b8758b685715498437d3a7862b2
merge PR: #295
PR CI / Shutdown / Docs:
33939249250 / 33939249290 / 33939249230 — SUCCESS
exact-main CI / Shutdown / Docs:
33939474734 / 33939474738 / 33939474753 — SUCCESS
Release workflow:
33939616674 — SUCCESS
release id: 383108819
504 passed / 0 failed / 0 skipped
```

Public ZIP:

- asset id `545248484`
- 80,559,673 bytes
- SHA-256 `bc174bfe1e58aee46fe8af4aeb3d9f680ac2320b09c8fab70f112914e1f076aa`

Exact-main Actions artifact:

- `JunhyunHelper-win-x64`
- id `9961347314`
- 241,610,142 bytes
- SHA-256 `7eddd2430970b346279585c3464706dc9fcc1dcb573d925560681ea08aaf1d32`

## 2. v1.17.4 Mini Scanner needed-count presentation

User-confirmed contract:

```text
FIR 3 / other 4 -> 3(인레이드) + 4개
FIR 0 / other 4 -> 0(인레이드) + 4개
FIR 4 / other 0 -> 4(인레이드) + 0개
```

Implementation boundary:

```text
ItemsWorkspace.Plan.NeededItems[itemId]
├─ RemainingFir   -> FIR display component
└─ RemainingTotal - RemainingFir
                    -> other/unrestricted display component
```

The Scanner does not recompute requirement semantics. `MiniScannerWindow` only formats the already-derived presentation values.

Preserved:

- Quest/Hideout requirement planning
- FIR semantics and inventory accounting
- Scanner OCR/matcher/pacing
- Scanner catalog and persistence
- Mini Scanner information ordering/layout
- supported schemas
- pinned Map donor revision `d933792b6042a51cea38dc44b686a096fe30de67`

## 3. Validation

PR #295 final head `5ba3c504e4da8b8758b685715498437d3a7862b2` passed CI `33939249250`, Shutdown Race `33939249290`, Docs `33939249230`.

Exact product source `2297a27332069e18ade56c53931002f7a4728338` passed CI `33939474734`, Shutdown Race `33939474738`, Docs `33939474753`.

Release workflow `33939616674` published stable `v1.17.4`; public tag directly targets exact product source and public ZIP digest matches the exact-main package hash.

## 4. Current work

`docs/ACTIVE_WORK.md`: **NONE**

User real-PC/Tarkov validation remains separately `PENDING`.
