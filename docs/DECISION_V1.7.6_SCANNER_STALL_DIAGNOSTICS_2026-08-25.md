# Decision — v1.7.6 Scanner stall root cause and verified fix

Date: 2026-08-26
Status: **P0 RESOLVED / PERFORMANCE FIX VERIFIED / RELEASE FINALIZATION**

## Decision

The severe Scanner delay reproduced on the Ryzen + GTX 1080 Ti desktop is considered resolved by the v1.7.6 fix candidate.

The decision is based on two support bundles from the same problematic desktop plus user validation. The first bundle established the root cause. The second bundle measured the post-fix result in both Display Test and actual `TarkovWindow` mode.

No further performance tuning should be made without new runtime evidence. In particular, recognition thresholds, candidate caps and recovery acceptance rules must not be relaxed merely to chase lower latency.

## Baseline root cause

The original long delay was not caused by Windows OCR.

Representative slow Tarkov cycle:

```text
end-to-end                  12,540.77 ms
OCR normal                      12.26 ms
actual WinRT RecognizeAsync     10.57 ms
visual recovery             12,306.61 ms / 16 calls
catalog matching                75.16 ms
capture                         21.57 ms
rectangle proposal              53.57 ms
semantic header                 53.51 ms
```

Windows OCR returned `하프 마스크 (Lower half-mask)` in about 10.57 ms and catalog matching resolved it as `EXACT`, confidence 1.0.

The runtime kept multiple structurally valid candidates as an accuracy safeguard. Eight equivalent candidates shared the same current title bitmap and OCR text, but `FontAwareScannerOcrEngine` reran targeted plus full-catalog visual corroboration independently for each candidate.

```text
8 equivalent candidates
× targeted visual pass
× full-catalog visual fallback
= 16 visual-recovery calls
```

Each optional visual pass took roughly 0.75–0.78 seconds on the problem PC, amplifying a millisecond OCR result into more than twelve seconds.

`TarkovTitleFontProvider.TryGetFonts()` also checked its unavailable retry window only after process/source discovery. `FindResourcesAssets()` enumerates `EscapeFromTarkov` processes and reads `MainModule.FileName`; therefore an unavailable optional font provider could still repeat expensive environment discovery before the retry guard returned.

File-I/O probes were sub-millisecond and no WPF dispatcher stall overlapped the slow cycles. Those were rejected as primary root causes.

## Implemented fix

### Current-cycle exact visual evidence reuse

A completed visual corroboration result is reused only when all of the following are identical within the same Scanner latency cycle:

- cycle ID
- exact title bitmap dimensions
- SHA-256 of current title pixels
- OCR text

The cache is cleared when the Scanner cycle changes. It does not prove a future frame and is not a cross-frame identity cache. It removes repeated computation of the same deterministic current-frame proof while retaining the original candidate count and acceptance rules.

Trace marker:

```text
visual-cycle-cache-hit
```

### Font-provider hot-path protection

`TarkovTitleFontProvider` now:

- checks unavailable retry state before expensive process/source discovery;
- applies a 30-second retry window after an unavailable source/extraction attempt;
- reuses a successfully discovered `resources.assets` path in the process;
- limits loaded-generation live source validation to a 5-second cadence instead of every candidate;
- retains source timestamp/length invalidation and re-extraction safety.

### UI and OCR diagnostics retained

v1.7.6 also retains:

- actual `OcrEngine.RecognizeAsync` call-level timing;
- slow-empty actual WinRT call health protection;
- OCR semaphore/image-key/preprocessing timing;
- explicit one-shot worker dispatch;
- WPF dispatcher responsiveness probes;
- one-click Scanner support bundle export.

## Post-fix verification

### Display Test

Same problematic PC and same test flow:

```text
하프 마스크
before: 10,840.877 ms
 after:     70.603 ms
reduction: about 99.35%

USB 보안 플래시 드라이브
before: 12,686.278 ms
 after:  1,354.775 ms
reduction: about 89.32%
```

Additional post-fix Display Test candidate-to-result times:

```text
Maska-1SCh:          106.619 ms
Domontovich 우샨카:   88.190 ms
Wires 전선:          100.802 ms
PSU 전원공급장치:     48.123 ms
```

The USB case requires corrupted-normal-OCR recovery and deep OCR. A bounded roughly one-second difficult case is considered acceptable and is fundamentally different from the previous repeated 5–13 second serial stall.

### Actual Tarkov

The second bundle contains real `mode=TarkovWindow` recognition.

Twelve successful `ShowingItem` cases had `ReadingTitle → ShowingItem` timing:

```text
minimum:  38.07 ms
median:   63.92 ms
maximum:   1.05 s
mean:     211.47 ms
```

Recognized examples included:

- USB 보안 플래시 드라이브
- 하프 마스크 (Lower half-mask)
- Metal fuel tank 금속 연료통
- Tech manual 기술 매뉴얼
- T-Shaped plug T자형 멀티탭
- Shustrilo 슈스트릴로 실링 폼
- Plexiglass 조각
- BEAR Buddy 인형
- Power cord 파워코드
- Filter 방독면 정화통
- Cat 고양이 조각상

The retained performance trace contains eleven complete OCR-active Scanner cycles after the oldest bounded entries were dropped:

```text
minimum end-to-end: 178.04 ms
median end-to-end:  210.82 ms
maximum end-to-end: 517.74 ms
```

The initial roughly one-second USB cycle predates the retained section and is therefore not included in that eleven-cycle aggregate.

### Duplicate-work and UI evidence

Retained trace:

```text
visual-cycle-cache-hit: 73
visual-recovery stages during repeated OCR cycles: 0–0.01 ms
ui-dispatcher-stall events: 0
actual WinRT OCR calls: generally about 4–13 ms
```

Environment file-I/O probe:

```text
ScannerDiagnosticLogWriteProbeMs: 0.30 ms
DiagnosticFileAppendAverageMs:    0.14 ms
DiagnosticFileAppendMaximumMs:    0.25 ms
```

The user independently reported the fixed candidate as `엄청 괜찮아졌다` and satisfactory in use.

## Accuracy and safety invariants

Unchanged:

- structural floor `0.34`;
- trusted `HEADER_FRAME_LOCKED` floor `0.68`;
- continuous candidate cap `8`;
- one-shot candidate cap `12`;
- existing deep OCR candidate limit;
- existing matcher acceptance semantics;
- existing targeted/full-catalog visual acceptance semantics;
- false positive remains worse than miss;
- no stale Item ID as current identity evidence;
- no cross-frame OCR or visual identity cache;
- no mapped price/need data as identity evidence before Item ID;
- no scan-time network dependency;
- no game memory reading, DLL injection, packet interception or process hook.

The performance fix does not reduce candidate count and does not lower any recognition threshold.

## CI proof

Root-cause fix code HEAD:

`d04f39697a4ea4d6ff4eabcb2acdc6bc535c8f9c`

CI run `32866068233`:

- Desktop build: SUCCESS
- 380 passed / 0 failed / 0 skipped
- Windows x64 self-contained publish: SUCCESS
- Product UI / Map / Factory / MiniMap smoke: SUCCESS
- graceful shutdown: SUCCESS
- release package verification: SUCCESS
- artifact upload: SUCCESS

User-verified fix candidate:

```text
bytes: 80,462,063
SHA-256: 96af948b2cd24caeb612d1d89a368bf30329606d3e934a292758292f70dcae30
```

Subsequent documentation-only HEAD CI also passed before this final evidence update.

## Finalization policy

The performance defect itself is closed. Do not introduce another performance algorithm change before v1.7.6 release unless new evidence shows a regression.

Before public release:

1. confirm reviewed Scanner Ground Truth has `REGRESSION=0` where available;
2. review temporary diagnostic-only implementation details and remove them only if this can be done without changing the user-verified execution behavior; otherwise record them as follow-up technical debt;
3. reconcile `STATE.md` with the actual v1.7.5 public stable and v1.7.6 verified fix state;
4. update final release notes/proof;
5. run final Windows build/tests/publish/product-smoke/package gate;
6. merge PR #185;
7. publish v1.7.6 as stable and verify public asset size/hash/source readback.
