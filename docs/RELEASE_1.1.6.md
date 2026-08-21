# v1.1.6 — Scanner catalog synchronization regression fix

Status: **RELEASE CANDIDATE / VALIDATION IN PROGRESS**

v1.1.6 is a PATCH release that fixes the public v1.1.5 Scanner `아이템 목록 최신화` regression.

## Reported symptom

After Game Content update, manual Scanner catalog synchronization could immediately end with:

```text
state=CatalogUnavailable
message=카탈로그 동기화에 실패했습니다. 기존 정상 캐시가 없으면 Scanner는 식별을 수행하지 않습니다.
```

The failure could occur even when the official item ID/name catalog itself was valid.

## Root cause

v1.1.5 added a catalog-wide health requirement that at least 500 items must have a positive trader sell price. That coupled optional market coverage to the Scanner identity catalog.

This contradicted the existing Scanner contract:

- identity must fail closed when the official ID/name set is structurally incomplete;
- market and dimensions are optional presentation fields and must fail closed per field.

A temporary or source-specific trader-price coverage gap therefore incorrectly disabled all Scanner identity resolution.

## Fix

`ScannerCatalogService` now separates the two concerns.

Identity catalog health requires:

```text
item count >= 4000
AND every accepted item has a non-empty Item ID
AND every accepted item has a non-empty official name
```

Trader/flea availability no longer decides whether the identity catalog is usable.

Market behavior remains:

- raw `traderPrices` is accepted when present;
- derived `sellFor` is accepted when present;
- flea rows are excluded from best-trader selection;
- `avg24hPrice` remains the independent flea average;
- missing/invalid price or dimensions leave only the affected display field empty.

Cache schema remains v2 and existing v1/v2 Scanner caches remain readable.

## Diagnostics

Manual `아이템 목록 최신화` now writes a `catalog-sync` entry to `scanner.log` with:

- game mode;
- success;
- outcome code;
- candidate/loaded item count;
- items with trader price;
- items with flea price;
- whether an existing healthy catalog was used as fallback.

No response body, screenshot, or raw pixels are persisted.

## Regression coverage

The Scanner market-shape suite now requires:

1. 4,000 items with raw `traderPrices` synchronize and project trader/per-slot values.
2. 4,000 valid item identities with **zero trader-price coverage** still synchronize and remain usable for recognition; trader fields are null.
3. 3,999 items are rejected as an incomplete identity catalog.
4. Existing full-catalog `sellFor`, flea, dimension and per-slot regressions remain.

Expected automated test count: **250**.

## Compatibility

```text
Desktop version: 1.1.6
Content schema: v7
Readable Content schemas: v3-v7
user.db schema: v1
Scanner cache schema: v1/v2 readable, v2 written
v1.1.5 -> v1.1.6 mandatory Game Content update: none
v1.1.5 -> v1.1.6 user.db migration: none
```

## Release gate

Before public release:

1. Windows Release build.
2. 250 automated tests, 0 failed, 0 skipped.
3. win-x64 self-contained single-file publish.
4. actual published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke.
5. graceful shutdown and clean portable root.
6. exact v1.1.6 ProductVersion and FIRST_RUN identity verification.
7. Draft-first ZIP + SHA256SUMS.
8. Draft asset re-download checksum/root/ProductVersion/FIRST_RUN verification and EXE smoke.
9. public/latest transition on the exact release source.
10. exact tag verification.
11. public asset re-download verification and public-downloaded EXE smoke.
