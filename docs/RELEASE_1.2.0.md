# v1.2.0 — Scanner title recognition overhaul

Status: **PUBLIC / VERIFIED**

Released: 2026-08-22 KST

```text
version: v1.2.0
release source: a7601f8498e8d75e832962fb9dd60f4112d28dc6
exact-source release run: 32514322439 — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.0-win-x64.zip
bytes: 80,298,514
SHA-256: ab5e9ef35b300268d16a1c5eece86cd8c6e57c91c83364caf4b7d02cde1d27d1
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

## Product scope

This MINOR release adds user-facing Scanner diagnostic-image and one-shot high-precision scan capabilities while structurally hardening item-name recognition.

Recognition pipeline:

```text
Tarkov display pixels
→ detail-window structural candidates
→ red close + magnifier + title-field anchor refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ semantic official-name matching
   OR OCR-independent full-catalog Tarkov-font visual recovery
→ confidence + top1/top2 margin gates
→ Item ID
→ local presentation / Mini Scanner
```

## Recognition contracts

- Existing successful OCR remains the primary path.
- Detail-title geometry is refined using the red close button, magnifier-side evidence and title-field structure.
- When the magnifier anchor is detected, title OCR starts to the right of it so magnifier pixels cannot become OCR characters.
- Anchor failure falls back to the validated Scanner Lab geometry ROI instead of inventing an unverified region.
- The current official Korean item-name catalog defines valid title characters. Han ideographs are invalid OCR evidence for this Korean-client contract.
- Corrupted or empty OCR may fall back to full-catalog visual comparison using locally available Tarkov title-font/glyph rendering support.
- Visual recovery requires conservative score and top1/top2 margin gates and fails closed when ambiguous.
- Geometry, icon or visual similarity alone never bypasses the official Item ID/name catalog contract.
- Scan-time network remains prohibited. No game memory, DLL injection or packet interception is used.

## User-facing additions

### 인식 이미지

`인식 이미지` keeps exactly one latest diagnostic capture in process memory and can show:

- captured screen region;
- selected detail-window bounds;
- actual title ROI;
- magnifier and close-button anchors;
- recognition pass;
- OCR text;
- candidate official name;
- confidence and second-candidate score.

The diagnostic screenshot is not persisted to disk.

### 1회 고정밀 스캔

- Performs one precision recognition pass even when continuous Scanner is OFF.
- Default global hotkey: `Ctrl+Shift+F10`.
- Hotkey can be changed or disabled in Scanner UI.
- Scanner display settings schema is v3.
- One-shot scanning reuses the local Scanner catalog; it never starts a scan-time network refresh.

If continuous Scanner/Test mode is active, one-shot scanning stops the existing runtime loop and awaits its actual completion before sharing detector/OCR/presentation state. The previous mode is restored afterward only if the current user setting still requests it.

## Runtime hardening

- Title OCR and inventory-context OCR still share the serialized WinRT OCR boundary.
- One-shot capture/state mutation is additionally serialized against the continuous runtime loop.
- In-memory recognition diagnostics are updated from the finally selected recognition result so displayed score metadata cannot describe a discarded candidate.
- Mini Scanner remains matched-item-only, topmost/no-activate, scan-time offline and independent of MiniMap.

## Compatibility

```text
Desktop version: 1.2.0
Content schema: v7
Readable Content schemas: v3-v7
user.db schema: v1
Scanner display settings schema: v3
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.1.6 -> v1.2.0 mandatory Game Content update: none
v1.1.6 -> v1.2.0 user.db migration: none
```

Existing Profile / Quest / Hideout / Inventory / Items / Ammo / Map / MiniMap / Scanner catalog data remain compatible.

## Verification

Exact-source release run `32514322439` used release source `a7601f8498e8d75e832962fb9dd60f4112d28dc6` and completed:

- exact source identity verification;
- pinned Map donor verification;
- Windows Release build;
- exactly 255 automated tests, 0 failed, 0 skipped;
- win-x64 self-contained single-file publish;
- exact ProductVersion provenance check (`1.2.0+a7601f8498e8d75e832962fb9dd60f4112d28dc6`);
- package root / PDB / nested archive / forbidden dependency audit;
- published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke;
- Scanner settings schema v3/default hotkey smoke;
- synthetic Tarkov inspect-header smoke proving a detected magnifier is outside the title OCR ROI;
- graceful shutdown and clean portable-root verification;
- Draft release creation;
- Draft asset re-download, SHA-256, root layout, ProductVersion and FIRST_RUN verification;
- Draft-downloaded EXE smoke;
- public/latest transition;
- exact public tag → release-source SHA verification;
- public asset re-download and checksum verification;
- public-downloaded EXE smoke.

The first attempt of the same release run stopped before ZIP/Draft creation because the existing Main Map asynchronous presentation smoke hit a timing-sensitive off-floor marker assertion. The same exact source had already passed that product smoke in the PR gate. A single clean rerun of the release job passed the same Main Map gate and every subsequent Draft/Public gate. No product source, package source or release SHA was changed between attempts.

## Public package

```text
asset: Junhyun-Helper-v1.2.0-win-x64.zip
bytes: 80,298,514
SHA-256: ab5e9ef35b300268d16a1c5eece86cd8c6e57c91c83364caf4b7d02cde1d27d1
ProductVersion: 1.2.0+a7601f8498e8d75e832962fb9dd60f4112d28dc6
```

## Post-release Main Map smoke hardening

The first-attempt Map failure was investigated after v1.2.0 became public instead of being left as test debt.

Root cause:

- the product-owned cross-floor marker opacity is `0.75`;
- the pinned donor runs a bounded 200 ms floor-filter settle timer;
- JunhyunHelper restores donor floor suppression and reapplies the product presentation after each donor tick;
- the old smoke used a fixed 3.2 second delay and then sampled once;
- that single sample could land in the narrow interval after a donor tick had temporarily written opacity `0.50` but before JunhyunHelper's queued recovery callback restored `0.75`.

Post-release PR #134 replaced the wall-clock guess with a deterministic product-state check:

- expose only the donor settle timer's read-only `IsEnabled` state to the in-process smoke;
- wait for the donor settle timer to be inactive;
- require every known off-floor standard marker to be `Visible`;
- require opacity to match the product-owned `OtherFloorOpacity = 0.75` within a 0.01 tolerance;
- require the correct Above/Below floor indicator;
- require the complete invariant to remain stable continuously for 650 ms;
- retain a bounded timeout and richer marker-state diagnostics.

CI run `32515954774` passed build, all automated tests, publish and the actual published EXE Product UI / Main Map / Factory / MiniMap smoke on its first run with this deterministic check. PR #134 was merged as `36c6f42e159583c5d799682e0a6015b9f6334220`.

This is test-harness robustness work only. It does **not** change the public v1.2.0 binary, tag, release source or package hash. The public release source remains `a7601f8498e8d75e832962fb9dd60f4112d28dc6`.

## Post-release validation

Latest live Tarkov end-to-end validation remains an ongoing product-quality activity rather than a reason to weaken fail-closed recognition. Real-game misses or false positives should be investigated from capture → candidate → title ROI → OCR/visual matcher → catalog → presentation → overlay using `scanner.log` and the in-memory `인식 이미지` view.
