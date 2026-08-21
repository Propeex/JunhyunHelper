# v1.1.6 — Scanner catalog synchronization regression fix

Status: **PUBLIC / VERIFIED**

Released: 2026-08-22 KST

```text
version: v1.1.6
release source: 8efee02e5966adb9b67b47847f95a12dfc357d0a
exact-source release run: 32500707112 — SUCCESS
automated tests: 250 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.6-win-x64.zip
bytes: 80,271,024
SHA-256: 986d0d2855381060267f63d2902317eabedc5d5738448fbd6c2b09e764c3477e
public/latest: VERIFIED
exact tag source: VERIFIED
Draft-downloaded EXE smoke: SUCCESS
public-downloaded EXE smoke: SUCCESS
```

## Reported symptom

After Game Content update, manual Scanner catalog synchronization could immediately end with:

```text
state=CatalogUnavailable
message=카탈로그 동기화에 실패했습니다. 기존 정상 캐시가 없으면 Scanner는 식별을 수행하지 않습니다.
```

The failure could occur even when the official item ID/name catalog itself was valid.

## Root cause

v1.1.5 incorrectly made broad trader-price coverage part of the all-or-nothing Scanner identity catalog health gate. A valid official ID/name catalog could therefore be rejected only because optional market fields were temporarily sparse.

## Fix

Identity catalog health now requires:

```text
item count >= 4000
AND every accepted item has a non-empty Item ID
AND every accepted item has a non-empty official name
```

Market data is independent presentation data:

- raw `traderPrices` is accepted when present;
- derived `sellFor` is accepted when present;
- flea rows are excluded from best-trader selection;
- `avg24hPrice` remains the independent flea average;
- missing/invalid market or dimensions leave only the affected display field empty;
- sparse market data no longer disables item identification.

A 4,000-item identity catalog with zero trader prices is accepted. A 3,999-item catalog is still rejected as structurally incomplete.

## Diagnostics

Manual `아이템 목록 최신화` writes a `catalog-sync` entry to `scanner.log` with game mode, outcome, item count, trader-price count, flea-price count and healthy-cache fallback state. Response bodies, screenshots and raw pixels are not persisted.

## Verification

Exact-source release workflow `32500707112` successfully completed:

- Windows Release build;
- exactly 250 automated tests, 0 failed, 0 skipped;
- win-x64 self-contained single-file publish;
- published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke;
- graceful shutdown and clean portable-root verification;
- Draft release creation and re-download checksum/root/ProductVersion/FIRST_RUN verification;
- Draft-downloaded EXE smoke;
- Public/latest transition and exact-tag verification;
- Public asset re-download verification;
- Public-downloaded EXE smoke.

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
