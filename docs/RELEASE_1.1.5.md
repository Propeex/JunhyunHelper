# v1.1.5 — Scanner / Mini Scanner hardening

Status: **RELEASE CANDIDATE / VALIDATION IN PROGRESS**

v1.1.5 is a PATCH release for the existing Scanner and Mini Scanner. It does not add game-memory access or a new recognition identity source.

## User-confirmed requirements

1. Mini Scanner must remain topmost without stealing Tarkov keyboard focus.
2. Trader sell price and trader price per slot must render when valid data exists.
3. Mini Scanner must not render runtime, OCR, diagnostic, waiting, or error status text. It renders matched item information only after a successful item match.
4. Real Mini Scanner should automatically appear only while Tarkov inventory/stash UI is visible and disappear otherwise.
5. Dragging Mini Scanner must not change the mouse pointer.
6. The whole rendered Mini Scanner card is the drag hit surface; the user must not need to grab exact text/icon pixels.
7. Item icons must be available for the full canonical item catalog after Game Content update, not only quest/hideout/ammo subsets.
8. Scanner stability, accuracy, reliability, and performance must remain fail-closed and be hardened while implementing these fixes.

## Implementation contract

### Matched-item-only overlay

`MiniScannerOverlayService.ShowStandby(...)` clears/hides the overlay. Runtime status stays in the Scanner page and diagnostic log only. A Mini Scanner window is created/rendered only for an item snapshot or explicit developer/test preview.

### Inventory/stash auto visibility

Real Scanner overlay visibility is gated by `ScannerInventoryContextDetector`:

- only the foreground `EscapeFromTarkov` client is eligible;
- only display pixels are captured;
- only a small top client-area band is OCRed;
- current Korean character/inventory navigation anchors are used;
- at least two independent anchors are required;
- uncertain/missing OCR => hidden;
- the decision is cached briefly to avoid redundant OCR;
- raw pixels/screenshots are not persisted;
- no memory reading, DLL injection, packet interception, or scan-time network request is introduced.

Display-test/explicit preview bypass this gate so product smoke and developer validation remain deterministic.

### OCR concurrency

Title OCR and inventory-context OCR use one `SerializedScannerOcrEngine` boundary. This prevents concurrent WinRT OCR operations and avoids creating a second Windows OCR runtime for overlay context alone.

### Topmost / no-activate

Mini Scanner keeps WPF `Topmost=True` and `ShowActivated=False` and also reasserts native `HWND_TOPMOST` using `SetWindowPos(..., SWP_NOACTIVATE)`. `WS_EX_NOACTIVATE` and `WS_EX_TOOLWINDOW` remain enabled.

The target is Tarkov Borderless/windowed presentation. Exclusive-fullscreen behavior is not claimed.

### Drag interaction

The full root card is a hit-testable near-transparent Border. Preview mouse-down tunnels through text/icon children to this surface. Cursor is forced to the normal Arrow cursor across the card. Drag completion persists the window coordinates exactly as before, including negative multi-monitor coordinates.

### Trader market data

The Scanner catalog accepts both current/raw `traderPrices` and derived `sellFor` market representations.

- best trader price = highest positive RUB-equivalent trader offer;
- flea rows are excluded from `sellFor` trader selection;
- flea average remains the independent positive `avg24hPrice` field;
- price/slot is computed only when both price and positive dimensions are valid.

A catalog with >= 4,000 valid names but implausibly missing trader market coverage is rejected instead of replacing a known-good cache. This specifically prevents the failure mode where recognition works but trader/trader-per-slot fields silently become blank.

### Display settings migration

Scanner display settings schema is v2. Existing installs are normalized once so these intended matched-item fields are enabled:

- icon;
- trader price;
- trader price per slot.

After migration, normal user checkbox choices continue to persist.

### Full item icon cache

`ImageCacheService.PrefetchAsync` now queues every canonical `GameContentCatalog.Items` icon during explicit Game Content update. Existing valid cached PNGs are reused. Individual image failures remain nonfatal. Scanner presentation still performs local-cache-only icon lookup during recognition.

## Automated regression additions

- raw 4,000-item `traderPrices` fixture must populate trader price and trader price/slot;
- a 4,000-item market-empty catalog must be rejected;
- existing 4,000-item market projection regression remains;
- actual WPF Mini Scanner smoke verifies:
  - no status text element;
  - icon/trader/trader-per-slot intended defaults;
  - topmost/no-activate/taskbar contract;
  - full-card nonzero-alpha hit surface;
  - forced Arrow cursor;
  - rendered trader and trader-per-slot lines.

Expected automated test count after the two new core/infrastructure tests: **249**.

## Release gate

Before public release:

1. Windows Release build.
2. 249 automated tests, 0 failed, 0 skipped.
3. win-x64 self-contained single-file publish.
4. actual published EXE Product UI / Mini Scanner / Scanner / Main Map / Factory / MiniMap smoke.
5. graceful shutdown and clean portable package root.
6. exact v1.1.5 ProductVersion and FIRST_RUN verification.
7. Draft-first ZIP + SHA256SUMS release.
8. Draft asset re-download checksum/root/ProductVersion verification and EXE smoke.
9. public/latest transition on the exact release source.
10. public asset re-download checksum/root/ProductVersion verification and EXE smoke.

Latest live Tarkov Borderless inventory/stash OCR is an empirical environment validation that continues after the automated release gate. Any residual UI-anchor issue must fail closed (overlay hidden), be diagnosed from `scanner.log`, and be corrected in a subsequent PATCH without weakening item identity confidence thresholds.
