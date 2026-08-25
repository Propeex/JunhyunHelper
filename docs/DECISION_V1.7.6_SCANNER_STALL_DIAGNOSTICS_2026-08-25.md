# Decision — v1.7.6 Scanner stall root cause and fix candidate

Date: 2026-08-26
Status: **ROOT CAUSE CONFIRMED / FIX CANDIDATE / DESKTOP RE-VERIFICATION REQUIRED**

## Problem

v1.7.5 did not resolve the severe Scanner delay observed on the Ryzen + GTX 1080 Ti desktop. The same PC reproduced the problem in Tarkov-window scanning and Display Test. The product stayed responsive after the v1.7.6 one-shot worker hardening, but recognition itself remained unacceptably slow.

The user ran the v1.7.6 diagnostic candidate and exported a support bundle. That evidence is now authoritative for this defect.

## Confirmed measured root cause

The long Scanner delay on the problematic desktop is **not caused by Windows OCR latency**.

Representative continuous Tarkov cycle:

```text
end-to-end                12,540.77 ms
OCR normal                    12.26 ms
actual WinRT RecognizeAsync    10.57 ms
visual recovery            12,306.61 ms / 16 calls
catalog matching               75.16 ms
capture                         21.57 ms
rectangle proposal              53.57 ms
semantic header                 53.51 ms
```

Other measured slow cycles:

```text
Display Test one-shot: 13,156.10 ms total / 12,277.39 ms visual recovery / 16 visual calls
Display Test one-shot: 11,127.35 ms total / 10,745.07 ms visual recovery / 14 visual calls
Tarkov continuous:      4,898.39 ms total /  4,624.02 ms visual recovery /  6 visual calls
```

File I/O probes were sub-millisecond per append and no WPF dispatcher stall overlapped the long Scanner cycles. Therefore filesystem logging and UI-thread starvation are not the principal cause of this measured latency.

## Why visual recovery amplified the delay

In the representative cycle Windows OCR returned `하프 마스크 (Lower half-mask)` in about 10.57 ms and the catalog matcher resolved it as `EXACT` with confidence 1.0.

The runtime still evaluates multiple structurally valid candidates because candidate count is an accuracy safeguard. Candidates 0 through 7 in this cycle shared the same exact title bitmap and OCR result. `SerializedScannerOcrEngine` correctly reused the raw OCR result inside the cycle, but `FontAwareScannerOcrEngine` sits outside that raw cache and independently reran visual corroboration for every candidate.

Each successful-text corroboration may run:

1. targeted Tarkov-font verification;
2. full-catalog visual verification when targeted verification does not accept.

On this desktop each measured visual stage consumed roughly 0.75–0.78 seconds. Eight equivalent candidates therefore produced sixteen expensive visual stages and more than twelve seconds of latency even though the primary OCR result itself was already available in milliseconds.

## Font provider hot-path defect

`TarkovTitleFontProvider.TryGetFonts()` also had a structural retry bug.

The old order was effectively:

```text
FindResourcesAssets()
→ TryGetSourceStamp()
→ only then check 5-second retry window
```

`FindResourcesAssets()` enumerates `EscapeFromTarkov` processes and reads `process.MainModule.FileName`. That environment lookup can be expensive or restricted on a protected game process. Because the retry guard came after the lookup, an unavailable/retry state did not protect the expensive part of the operation. Every targeted/full visual pass could pay the same environment-discovery cost before returning no font evidence.

The original diagnostic candidate did not have an internal font-provider marker, so the exact 0.75–0.78 second subphase cannot be attributed solely to `MainModule` from the first bundle. The new fix candidate therefore records `title-font-source-probe` timing. What is already proven is that the repeated delay is inside visual recovery and that the provider retry ordering permits repeated expensive discovery in that path.

## Fix candidate architecture

### 1. Current-cycle exact visual evidence reuse

`FontAwareScannerOcrEngine` now caches a completed corroboration result only within the current `ScannerLatencyTelemetry` cycle, keyed by:

- cycle ID;
- exact title bitmap dimensions;
- SHA-256 of current title pixels;
- OCR text.

This is not cross-frame identity caching. The cache is cleared on cycle change and cannot prove a future frame. It merely prevents multiple structurally equivalent candidates in one decision cycle from recomputing the same deterministic visual proof.

No candidate count is reduced. No candidate is accepted without the same evidence that would have been evaluated before. The optimization removes duplicate computation, not validation.

Trace event: `visual-cycle-cache-hit`.

### 2. Font source discovery outside repeated candidate hot path

`TarkovTitleFontProvider` now:

- checks unavailable retry state before process/source discovery;
- uses a 30-second retry window after an unavailable extraction/source attempt;
- caches a successfully discovered `resources.assets` path in the current process;
- revalidates an already-loaded font generation at a bounded 5-second interval rather than for every candidate;
- continues to invalidate/re-extract when the live source length/timestamp changes.

Trace event: `title-font-source-probe` with elapsed time and source availability.

The longer unavailable retry window trades repeated expensive optional visual-provider discovery for a conservative miss when that optional evidence is temporarily unavailable. It does not weaken Item ID acceptance and does not suppress successful Windows OCR.

### 3. Existing v1.7.6 hardening retained

- each actual `OcrEngine.RecognizeAsync` call is measured independently;
- actual slow-empty WinRT calls use the same conservative circuit breaker;
- OCR semaphore/image-key/preprocessing phases are separately traced;
- one-shot recognition is explicitly dispatched off the WPF message pump;
- WPF dispatcher responsiveness is independently measured;
- one-click support bundle export remains available.

## Accuracy and safety invariants

Unchanged:

- structural floor `0.34`;
- trusted `HEADER_FRAME_LOCKED` floor `0.68`;
- continuous candidate cap `8`;
- one-shot candidate cap `12`;
- existing deep OCR candidate limit;
- existing matcher acceptance semantics;
- existing targeted/full-catalog Tarkov-font visual acceptance semantics;
- false positive remains worse than miss;
- no stale Item ID as current identity evidence;
- no cross-frame OCR or visual identity cache;
- no mapped price/need data as identity evidence before Item ID;
- no scan-time network dependency;
- no game memory reading, DLL injection, packet interception, or Tarkov process hook.

## CI proof for root-cause fix candidate

Code HEAD:

`d04f39697a4ea4d6ff4eabcb2acdc6bc535c8f9c`

CI run `32866068233`:

- Desktop build: SUCCESS
- 380 passed / 0 failed / 0 skipped
- Windows x64 self-contained publish: SUCCESS
- Product UI / Map / Factory / MiniMap smoke: SUCCESS
- graceful shutdown: SUCCESS
- release package verification: SUCCESS
- artifact upload: SUCCESS

Extracted user package:

```text
bytes: 80,462,063
SHA-256: 96af948b2cd24caeb612d1d89a368bf30329606d3e934a292758292f70dcae30
```

## Release gate

Root cause has been identified, but v1.7.6 is still not a resolved public release.

Required before release:

1. run this fix candidate on the same problematic desktop and same Display Test screenshot;
2. export a second support bundle and compare end-to-end/visual recovery timing with the 12.54-second baseline;
3. confirm `visual-cycle-cache-hit` and `title-font-source-probe` behavior;
4. verify actual Tarkov scan responsiveness;
5. confirm no Item ID accuracy regression and run reviewed Ground Truth regression with zero regressions;
6. remove or normalize temporary diagnostic-only implementation details that should not remain in the final production architecture;
7. run final full Windows build/tests/publish/smoke/package gate;
8. update `STATE.md`, release notes and final release proof before public v1.7.6 publication.
