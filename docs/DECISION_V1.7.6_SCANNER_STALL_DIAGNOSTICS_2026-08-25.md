# Decision — v1.7.6 Scanner stall root-cause diagnostics

Date: 2026-08-25
Status: DIAGNOSTIC CANDIDATE / DESKTOP VERIFICATION REQUIRED

## Problem

v1.7.5 did not resolve the severe Scanner delay observed on the Ryzen + GTX 1080 Ti desktop. The same desktop reproduces the problem in both Tarkov-window scanning and display-test mode, while screenshot-based testing on the lower-spec Intel laptop remains fast.

The observed product state reaches `아이템 이름을 읽는 중입니다.` and then remains stalled for an abnormally long period before either recognizing the item or failing closed.

v1.7.2 also reproduces the desktop problem, so the v1.7.3 observation-cadence change is not the root cause.

## Confirmed code facts

The following are now established from the production execution path and are not hypotheses.

1. Continuous Scanner recognition is launched with `Task.Run`, so the continuous scan loop itself does not normally execute on the WPF UI thread.
2. The v1.7.5 `ocr-backend-call` diagnostic wraps a complete raw normal/deep OCR operation. In deep mode that operation can contain four actual Windows `OcrEngine.RecognizeAsync` calls plus preprocessing. Therefore the event does **not** satisfy the v1.7.5 decision requirement to measure each actual OS OCR invocation.
3. `SerializedScannerOcrEngine` can spend time waiting on its shared OCR semaphore and synchronously creating an exact-image SHA-256 key via `CopyPixels` before the guarded raw OCR operation begins. Those phases were not separately measured in v1.7.5.
4. Raw Scanner Lab 3.8 OCR performs image enlargement, optional deep image variants, BGRA conversion/`CopyPixels`, WinRT buffer/`SoftwareBitmap` construction, and only then calls `OcrEngine.RecognizeAsync`.
5. `FontAwareScannerOcrEngine` can continue into tight-crop retries and strict targeted/full-catalog Tarkov-font visual recovery after raw OCR returns.
6. `ScannerDiagnosticLog.Write` appends synchronously to `scanner.log`. The log is bounded, but the latency of each append can still vary by filesystem/antivirus environment and must be distinguished from OCR latency rather than assumed harmless.
7. One-shot scanning triggered by the global hotkey can resume on the WPF dispatcher and execute synchronous scan work before a naturally asynchronous boundary. This is a separate confirmed UI-responsiveness defect to harden in the final fix even if it is not the root cause of the continuous-scanner symptom.

## Why v1.7.5 was insufficient

The v1.7.5 circuit breaker remains a valid safety mechanism for one specific failure mode: a completed OCR operation that is both slow and empty.

However it cannot establish or solve all remaining cases:

- a single actual `OcrEngine.RecognizeAsync` call itself may take a very long time before returning;
- slow calls may return text and therefore intentionally remain enabled under the v1.7.5 policy;
- delay may occur before WinRT OCR, such as semaphore wait, `CopyPixels`, variant creation, or `SoftwareBitmap` construction;
- delay may occur after raw OCR in strict visual recovery or matching;
- UI starvation may be separate from backend latency.

Therefore changing thresholds or reducing OCR attempts again without new evidence is prohibited.

## v1.7.6 diagnostic design

v1.7.6 is a diagnostic candidate, not yet the final performance fix.

### Exact OCR phase trace

Keep the existing Scanner Lab 3.8 OCR algorithm and existing Windows OCR engine instance, but record bounded in-memory start/end markers for:

- shared serialized OCR gate wait;
- exact-image `CopyPixels` + SHA-256 key creation;
- title enlargement;
- deep image variant generation;
- BGRA conversion;
- OCR input `CopyPixels`;
- WinRT buffer/`SoftwareBitmap` creation;
- each individual `OcrEngine.RecognizeAsync` call, including pass, variant, dimensions, duration, text presence and line count;
- overall serialized/raw OCR operation.

Fine-grained markers are held in a bounded in-memory trace rather than appended to `scanner.log` one by one. This avoids making a slow filesystem or antivirus path part of the instrumentation itself.

The diagnostic adapter reuses the already-created `ScannerLab38OcrEngine` WinRT engine instance. It does not activate a second OCR engine, change language, change image variants, change recognition thresholds, or introduce a new OCR backend.

### UI responsiveness probe

Independently post a low-frequency probe to the WPF dispatcher. Record only stalls at or above 750 ms and their eventual recovery duration. This separates:

- backend work that is slow while UI remains responsive; and
- actual WPF dispatcher starvation corresponding to Windows `Not Responding` behavior.

### Environment/support bundle

Add one user-facing action under `Scanner > 고급`:

`Scanner 성능 진단 자료 내보내기`

It produces one ZIP containing:

- bounded Scanner performance trace;
- existing `scanner.log` / rotated log when present;
- startup log when present;
- Windows/runtime/process architecture;
- culture/UI culture;
- available Windows OCR languages and `ko-KR` availability;
- display bounds and WPF DPI scale;
- CPU identifier/count available from the process environment;
- a small on-demand append benchmark in the same LocalAppData log directory to identify unusually slow synchronous diagnostic-file I/O.

It deliberately excludes Ground Truth images, profile database contents, and game account information.

## Interpretation contract

The desktop bundle will be interpreted as follows.

- Long `ocr-winrt-recognize-start` → `end` with short surrounding phases: Windows OCR backend stall is proven.
- Long `ocr-copy-pixels`, variant, image-key, or SoftwareBitmap phase: preprocessing/runtime conversion is proven.
- Long serialized wait with another OCR operation active: shared OCR serialization/contention is proven.
- Fast raw OCR boundaries followed by large completed `visualRecoveryMs`: strict visual recovery is the bottleneck.
- Fast semantic stages but slow diagnostic file append benchmark: synchronous file I/O is a material environment factor and should be removed from the recognition hot path.
- `ui-dispatcher-stall-*` overlapping the recognition interval: actual UI starvation is proven independently of backend latency.
- No dispatcher stall while recognition is slow: product status is waiting on a worker, not a true UI-thread freeze; UI feedback can then be hardened separately without misidentifying the backend cause.

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
- no cross-frame OCR identity cache;
- no mapped price/need data as identity evidence before Item ID;
- no scan-time network dependency;
- no game memory reading, DLL injection, packet interception, or Tarkov process hook.

## Release gate

Do **not** call v1.7.6 a resolved performance release until the problematic desktop reproduces the symptom with this diagnostic candidate and the exported bundle identifies the blocking phase.

After root cause is proven:

1. implement the smallest architecture-level fix that addresses the measured bottleneck;
2. independently remove the confirmed one-shot UI-thread blocking path;
3. keep UI responsive even when OCR itself is slow;
4. run Windows build, full tests, publish/package verification and product/Scanner smoke;
5. run Scanner Ground Truth regression with zero acceptance regressions;
6. repeat desktop test mode and in-game validation;
7. update `STATE.md`, `CURRENT_SCANNER_WORK.md`, release notes and final decision status before public release.
