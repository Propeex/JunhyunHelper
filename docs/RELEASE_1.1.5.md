# v1.1.5 — Scanner / Mini Scanner hardening

Status: **v1.1.5 PUBLIC RELEASE / VERIFIED**

Published: **2026-08-21 14:59:17 UTC** (`2026-08-21 23:59:17 KST`)

Release: https://github.com/Propeex/JunhyunHelper/releases/tag/v1.1.5

v1.1.5 is a PATCH release for the existing Scanner and Mini Scanner. It hardens overlay behavior, market/icon data, title recognition, and runtime reliability without adding game-memory access or changing the authoritative Item identity source.

## 1. Exact public identity

```text
version: v1.1.5
release source / public tag: 3541bab6536ff91a00f394c4f7b03d5cbf112746
PR final candidate head: 91ef32becfd053c2074f96b656f2f6ad3f5295b4
PR final CI: 32493986403 — SUCCESS
initial exact-source release run: 32494487841 — package gates SUCCESS; Draft tag-order check failed after Draft creation
Draft resume/public verification run: 32495042444 — SUCCESS
independent public verification run: 32495225958 — SUCCESS
automated tests: 249 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.5-win-x64.zip
bytes: 80,269,429
SHA-256: dc31177ae1bd4d152453a010dffe6cbb1e6c1d2a4a7e2eb82fb7444fa99c0748
ProductVersion: 1.1.5+3541bab6536ff91a00f394c4f7b03d5cbf112746
FIRST_RUN first line: 준현 헬퍼 v1.1.5 — Windows x64
public release id: 374480701
public/latest: VERIFIED
exact public tag source: VERIFIED
Draft-downloaded EXE smoke: SUCCESS
public-downloaded EXE smoke: SUCCESS
independent public-downloaded EXE smoke: SUCCESS
```

The final public ZIP was independently downloaded again after publication and its byte size, SHA-256, root layout, `ProductVersion`, `FIRST_RUN_KO.txt`, rendered product smoke, normal shutdown, and clean portable root were verified.

## 2. User-confirmed requirements

1. Mini Scanner must remain topmost without stealing Tarkov keyboard focus.
2. Trader sell price and trader price per slot must render when valid data exists.
3. Mini Scanner must not render runtime, OCR, diagnostic, waiting, or error status text. It renders matched Item information only after a successful Item match.
4. Real Mini Scanner should automatically appear only while Tarkov inventory/stash UI is visible and disappear otherwise.
5. Dragging Mini Scanner must not change the mouse pointer.
6. The whole rendered Mini Scanner card is the drag hit surface; the user must not need to grab exact text/icon pixels.
7. Item icons must be available for the full canonical Item catalog after Game Content update, not only quest/hideout/ammo subsets.
8. Scanner stability, accuracy, reliability, and performance must remain fail-closed and be hardened while implementing these fixes.
9. The inspect-window top Item-name recognizer must use the actual Tarkov title font stack identified from the current client assets so OCR misses can be recovered more accurately without weakening existing semantic acceptance.

## 3. Mini Scanner implementation contract

### Matched-item-only overlay

`MiniScannerOverlayService.ShowStandby(...)` clears/hides the overlay. Runtime status stays in the Scanner page and diagnostic log only. A Mini Scanner window is rendered only for a matched Item snapshot or explicit test/developer preview.

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

Title OCR and inventory-context OCR share one `SerializedScannerOcrEngine` boundary. This prevents concurrent WinRT OCR operations and avoids a second Windows OCR runtime solely for overlay visibility.

### Topmost / no-activate

Mini Scanner keeps WPF `Topmost=True` and `ShowActivated=False` and also reasserts native `HWND_TOPMOST` using `SetWindowPos(..., SWP_NOACTIVATE)`. `WS_EX_NOACTIVATE` and `WS_EX_TOOLWINDOW` remain enabled.

The supported target is Tarkov Borderless/windowed presentation. Exclusive-fullscreen behavior is not claimed.

### Drag interaction

The full root card is a hit-testable near-transparent Border. Preview mouse-down tunnels through text/icon children to this surface. Cursor is forced to the normal Arrow cursor across the card. Drag completion persists window coordinates, including negative multi-monitor coordinates.

## 4. Market and icon reliability

### Trader market data

The Scanner catalog accepts both current/raw `traderPrices` and derived `sellFor` market representations.

- best trader price = highest positive RUB-equivalent trader offer;
- flea rows are excluded from `sellFor` trader selection;
- flea average remains independent positive `avg24hPrice`;
- price/slot is computed only when both price and positive dimensions are valid.

A catalog with >= 4,000 valid names but implausibly missing trader coverage is rejected instead of replacing a known-good cache. This prevents the failure mode where recognition works but trader/trader-per-slot fields silently become blank.

### Display settings migration

Scanner display settings schema is v2. Existing installs are normalized once so icon, trader price, and trader price/slot are enabled as the intended matched-item defaults. After migration, normal user checkbox choices persist.

### Full Item icon cache

`ImageCacheService.PrefetchAsync` queues every canonical `GameContentCatalog.Items` icon during explicit Game Content update. Existing valid cached PNGs are reused. Individual image failure remains nonfatal. Scanner presentation remains local-cache-only during recognition.

## 5. Inspect-title Tarkov font-aware recovery

Research of the current inspect window established the top Item name as the `ItemInfoWindowLabels._caption` TextMeshPro text. Tarkov's UI font stack uses Bender-family primary glyphs with `Noto Sans CJK KR` as the Korean fallback.

v1.1.5 preserves the existing identity pipeline and adds a conservative recovery stage:

```text
candidate title ROI
→ Windows ko-KR normal OCR
→ current official Korean catalog semantic validation
→ if needed: existing Deep OCR
→ if existing semantic gate still fails:
   official-name shortlist
   → render with Tarkov Bender + Noto Sans CJK KR fallback
   → compare rendered glyph mask with observed title ROI
   → require semantic score + visual score + top1/top2 margin
→ only strong combined evidence may recover the official Item name
→ Item ID
```

Important invariants:

- an OCR result already accepted by the old semantic gate is never rejected or replaced by the font verifier;
- font recovery runs only on the failed Deep-OCR path;
- short Item names use stricter thresholds;
- ambiguous or weak visual evidence remains rejected;
- the current official Korean full-Item catalog remains the authoritative Item identity source;
- font shape is supporting evidence, not an independent Item database.

### Font acquisition / redistribution boundary

Bender font binaries are **not** shipped in the JunhyunHelper public package.

`TarkovTitleFontProvider` locates the running user's own:

```text
EscapeFromTarkov_Data/resources.assets
```

and reads it read-only. It identifies embedded SFNT payloads by actual font metadata and copies only the required Bender Regular/Bold and Noto Sans CJK KR payloads into the JunhyunHelper app-local Scanner font cache. The game directory is never modified.

If the current Tarkov asset cannot be located, read, parsed, or validated, font-aware recovery disables itself and Scanner continues on the previously proven OCR-only path. Font extraction/rendering failure is nonfatal.

The title runtime receives `FontAwareScannerOcrEngine`; the inventory/stash context detector deliberately continues to use the serialized OCR engine directly so Item-font verification cannot affect context gating or add unnecessary work there.

## 6. Automated regression and smoke coverage

Core/infrastructure tests:

- raw 4,000-Item `traderPrices` fixture populates trader price and trader price/slot;
- a 4,000-Item market-empty catalog is rejected;
- existing 4,000-Item market projection regression remains;
- total: **249 passed / 0 failed / 0 skipped**.

Published-EXE product smoke verifies, among other existing product contracts:

- Mini Scanner has no runtime/status text element;
- icon/trader/trader-per-slot intended defaults;
- topmost/no-activate/taskbar contract;
- full-card nonzero-alpha drag hit surface;
- forced Arrow cursor;
- rendered trader and trader-per-slot lines;
- Tarkov-title SFNT parser acceptance/rejection contract;
- Korean fallback segmentation contract;
- Scanner/Product UI/Main Map/Factory/MiniMap startup and rendered smoke;
- graceful shutdown;
- no runtime logs pollute the portable package root.

CI cannot contain the user's current Tarkov `resources.assets`, so the exact live asset extraction and current Korean UI anchors remain empirical environment validation. Their failure mode is deliberately fail-closed/fallback-only and is not allowed to produce a false Item identity.

## 7. Release verification history

### Final candidate CI — `32493986403`

All steps passed: Release build, **249/249 tests**, win-x64 publish, actual published EXE Product UI/Mini Scanner/title-font parser/Scanner/Map/Factory/MiniMap smoke, graceful shutdown, clean package root.

### Initial exact-source release run — `32494487841`

The run checked out exact source `3541bab...`, passed Release build, **249/249 tests**, publish audit, exact `ProductVersion`, published EXE smoke, and created the final ZIP:

```text
bytes: 80,269,429
SHA-256: dc31177ae1bd4d152453a010dffe6cbb1e6c1d2a4a7e2eb82fb7444fa99c0748
```

It then created the Draft Release successfully but failed because the workflow tried to query the Git tag immediately. GitHub Draft Releases do not create the public tag ref until publication. This was a release-automation ordering defect, not a product/package failure. No public transition had occurred at the failure point.

### Draft resume/public verification — `32495042444`

The existing Draft was located by release metadata and required to target exact source `3541bab...`. Its assets were re-downloaded and verified against the exact byte count/SHA above, `ProductVersion`, `FIRST_RUN`, and package root. The Draft-downloaded EXE smoke passed.

The release was then published as latest. The workflow verified:

- public release not Draft/prerelease;
- `/releases/latest` = `v1.1.5`;
- `v1.1.5` tag = exact source `3541bab...`;
- public ZIP re-download byte count/SHA;
- public `ProductVersion` and `FIRST_RUN`;
- public package root;
- public-downloaded EXE smoke and normal shutdown.

### Independent public verification — `32495225958`

A separate workflow independently repeated public metadata/latest/tag verification, downloaded the public package again, verified byte count/SHA/`SHA256SUMS.txt`/root/`ProductVersion`/`FIRST_RUN`, and ran the downloaded EXE product smoke plus normal shutdown. All steps passed.

## 8. Remaining empirical live validation

Latest live Tarkov Borderless inventory/stash OCR and the user's current `resources.assets` font extraction are environment-dependent empirical checks that continue after the automated release gate.

The intended diagnostic events include:

- `inventory-context`
- `title-font-extract-ready`
- `title-font-extract-missing`
- `title-font-extract-failed`
- `title-font-verify-accepted`
- `title-font-verify-rejected`
- `title-font-recovery-error`

Any residual anchor/font issue must remain fail-closed or OCR-only fallback and be corrected in a subsequent PATCH without weakening Item matcher confidence or margin thresholds.
