# Decision — v1.7.5 Scanner OCR environment guard

Date: 2026-08-25
Status: IMPLEMENTING / VERIFICATION REQUIRED

## User-observed evidence

Development and screenshot Scanner testing had previously been performed on a lower-spec Intel laptop with integrated graphics. Test-mode screenshot recognition was fast and reliable there.

On the Ryzen + GTX 1080 Ti + 32 GiB desktop, both real Tarkov scanning and the same screenshot-style test mode show severe delay. The symptom reproduces on v1.7.2 as well as v1.7.3, so the v1.7.3 pacing/performance changes are not the root cause.

The Scanner continues to detect/attempt candidates and reaches `아이템 이름을 읽는 중입니다.`. The long stall occurs there, frequently followed by `아이템 이름을 읽지 못해 식별을 보류했습니다.`. This isolates the primary regression to OCR/semantic recognition rather than capture or detail-window detection.

## Root cause model

Production title recognition currently uses the OS-provided `Windows.Media.Ocr` Korean recognizer. Its runtime behavior can differ across Windows installations and language/OCR environments.

The existing fail-safe recognition pipeline magnifies a slow-empty OCR backend:

- normal pass: up to 8 candidates
- `FontAwareScannerOcrEngine` may retry an empty normal result with a proven tight title crop
- deep pass: up to 3 candidates
- raw deep OCR evaluates 4 image variants
- the tight-title path can repeat those deep variants

Therefore one semantic cycle can reach roughly 40 serialized OS OCR invocations in the worst slow-empty case. This is appropriate for a fast healthy OCR backend but catastrophic when the OS backend itself is slow and empty.

## Product decision

Do not lower recognition confidence or remove strict recovery paths.

Instead add an OCR backend circuit breaker before the serialized retry amplification:

1. Measure actual OS OCR call duration.
2. A fast empty result remains an ordinary miss.
3. A slow successful result remains fully valid.
4. Only a result that is both `>= 800 ms` and empty marks the backend degraded.
5. Degraded cooldown is 30 seconds.
6. During cooldown, additional OS OCR calls return empty immediately instead of repeating the known degraded backend operation.
7. The existing strict Tarkov-font recovery path remains available and uses current pixels/current catalog evidence.
8. If that recovery is not strong enough, fail closed.
9. After cooldown, allow a probe. A successful OCR result clears the degraded state.

This reduces a pathological semantic cycle from potentially dozens of slow OS OCR operations to one slow-empty operation plus bounded local recovery.

## Diagnostics

Record:

- OCR backend type/availability
- Windows version
- process architecture
- each actual backend call duration
- normal/deep pass
- input dimensions
- text presence/length
- degraded entry and cooldown expiry
- suppression event

This makes future PC-specific performance diagnosis evidence-based rather than inferred from UI timing.

## Accuracy invariants

Unchanged:

- structural floor `0.34`
- `HEADER_FRAME_LOCKED` `0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- matcher acceptance semantics
- targeted/full-catalog Tarkov-font visual acceptance semantics
- false positive is worse than miss
- no cross-frame identity proof reuse
- no game memory/DLL injection/packet interception/scan-time network dependency

## Long-term architecture

The current OS OCR backend should no longer be treated as a universally deterministic portable-runtime dependency. A future portable OCR backend may be evaluated against reviewed Scanner Ground Truth. It must not replace the current recognizer until accuracy and latency are measured and regressions are zero on reviewed cases.
