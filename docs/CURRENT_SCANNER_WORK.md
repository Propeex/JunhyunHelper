# Current Scanner Work

기준일: 2026-08-25
상태: **P0 — v1.7.6 SCANNER STALL DIAGNOSTIC CANDIDATE / DESKTOP VERIFICATION REQUIRED**

## 현재 우선순위

현재 Scanner의 최우선 작업은 일부 Windows 데스크탑에서 발생하는 심각한 아이템 이름 인식 지연과 UI 응답성 문제를 해결하는 것이다.

이 문제는 일반적인 OCR miss나 몇 초의 지연이 아니라, 상세창을 찾은 뒤 `아이템 이름을 읽는 중입니다.` 상태에서 장시간 멈춘 것처럼 보이는 제품 수준의 중대 결함으로 취급한다.

현재 public stable은 **v1.7.5**다.

```text
public stable: v1.7.5
exact release source: 215541a694459e9484716c4942a436c26defe919
stable asset: Junhyun-Helper.zip
stable bytes: 80,450,225
stable SHA-256: 6706f12e63caa2039cf3f89c6823b457d125e43f8af47779082caa843282923f
```

v1.7.6은 아직 public stable이 아니다. 문제 PC에서 실제 blocking phase를 확인하기 위한 diagnostic candidate다.

Reference:

- `docs/DECISION_V1.7.5_OCR_ENVIRONMENT_GUARD_2026-08-25.md`
- `docs/DECISION_V1.7.6_SCANNER_STALL_DIAGNOSTICS_2026-08-25.md`
- `docs/RELEASE_NOTES_V1.7.6.md`

## 재현 근거

문제 데스크탑:

- AMD Ryzen CPU
- NVIDIA GTX 1080 Ti
- RAM 32 GiB
- 실제 Tarkov Scanner에서 심각한 지연
- Display Test에서 같은 screenshot을 사용해도 동일한 심각한 지연

비교 노트북:

- Intel CPU
- integrated graphics
- 전체적으로 더 낮은 사양
- screenshot 기반 Display Test는 빠름

버전 비교:

```text
v1.7.2 desktop: slow
v1.7.3 desktop: slow
v1.7.5 desktop: slow
```

따라서 v1.7.3의 cadence 변경은 root cause가 아니다. GPU/CPU vendor, Windows OCR, DPI, filesystem, visual recovery 중 어느 하나도 실행 evidence 없이 원인으로 단정하지 않는다.

## 현재 production recognition pipeline

```text
Tarkov window / Display Test pixels
→ capture
→ detail rectangle proposals
→ red close-X + magnifier + neutral inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative official-catalog matching
→ optional deep OCR / tight-title retry
→ optional strict Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

## 인식 안전 불변식

성능 문제 해결을 위해 다음 값을 완화하지 않는다.

```text
structural floor = 0.34
trusted HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
deep OCR candidate limit = existing value
```

추가 계약:

- false positive보다 miss 선호
- geometry는 proposal이며 identity proof가 아님
- magnifier + red close-X semantic evidence 필수
- current official Korean Tarkov catalog가 identity authority
- stale Item ID를 current identity proof로 사용하지 않음
- cross-frame OCR identity cache 금지
- price/flea/slots/needed는 Item ID 확정 이후 mapped presentation data
- scan-time network 없음
- game memory read / DLL injection / packet interception / Tarkov process hook 없음
- reviewed Ground Truth 없이 acceptance threshold/candidate cap 완화 금지

## v1.7.5에서 확인된 한계

v1.7.5는 OCR operation이 800 ms 이상 걸리고 empty일 때 30초 circuit breaker를 두었다.

그러나 구현 검토 결과 `ocr-backend-call`은 실제 Windows OCR 호출 1회가 아니라 complete normal/deep operation을 측정했다.

Deep OCR 한 operation 내부에는 최대 4회의 실제 `OcrEngine.RecognizeAsync`가 들어간다. 따라서 첫 actual WinRT call이 slow-empty여도 complete deep operation이 끝나기 전에는 v1.7.5 outer guard가 degraded 상태를 알 수 없었다.

즉 v1.7.5 decision의 “each actual OS OCR invocation” telemetry/containment 계약이 구현에서 충분히 세분화되지 않았다.

## 확인된 UI 결함

Continuous Scanner loop 자체는 `Task.Run` worker에서 동작한다.

반면 global hotkey one-shot path는 WPF message pump에서 진입하고, 초기 await가 synchronous completion될 경우 capture/detection/OCR setup이 UI dispatcher에서 시작될 수 있었다.

v1.7.6 diagnostic candidate에서는 one-shot `ScanOnceAsync`를 explicit worker로 보내 이 confirmed UI-thread blocking path를 제거한다.

Mini Scanner Window access와 Scanner UI status subscriber는 기존 dispatcher marshalling을 유지한다.

## v1.7.6 diagnostic candidate

### 전체 pipeline timing

Bounded in-memory performance trace에 다음 start/end를 연결한다.

- whole Scanner cycle
- capture
- rectangle proposal
- semantic header
- OCR normal
- OCR deep
- visual recovery
- catalog matching
- presentation

### exact OCR timing

추가로 다음을 분리한다.

- serialized OCR semaphore wait
- exact-image key `CopyPixels` + SHA-256
- title enlargement
- deep image variant generation
- BGRA conversion
- OCR input `CopyPixels`
- WinRT buffer / `SoftwareBitmap` creation
- **each actual `OcrEngine.RecognizeAsync`**
- pass / variant / image dimensions / duration / text presence / line count

Fine-grained trace는 per-event synchronous file append를 사용하지 않는다. 최근 4,000 entries만 memory에 bounded 보존한다.

### actual-call circuit breaker

v1.7.5 outer operation guard를 유지하면서 같은 health policy를 실제 WinRT call 단위에도 적용한다.

```text
actual RecognizeAsync < 800 ms + empty
→ ordinary miss

actual RecognizeAsync >= 800 ms + text
→ valid result, no suppression

actual RecognizeAsync >= 800 ms + empty
→ degraded for 30 s
→ later OS OCR calls in the same recovery chain suppressed
→ current-pixel/current-catalog strict visual recovery remains available
→ insufficient evidence => fail closed
```

Recognition acceptance semantics는 변경하지 않는다.

### UI responsiveness probe

WPF dispatcher를 별도로 probe한다.

- probe interval: 500 ms
- normal dispatcher priority
- stall threshold: 750 ms
- pending / eventual recovery duration 기록

이를 통해 backend latency와 실제 Windows `Not Responding` 성격의 dispatcher starvation을 분리한다.

### support bundle

`Scanner > 고급 > Scanner 성능 진단 자료 내보내기`

ZIP 포함:

- `scanner-performance-trace.txt`
- `scanner.log`
- `scanner.log.1` when present
- `startup.log` when present
- `environment.txt`
- README

Environment:

- product/runtime/Windows information
- OS/process architecture
- culture/UI culture
- Windows OCR languages / ko-KR availability
- CPU identifier/count
- display bounds
- WPF DPI / render tier
- GC/process memory/thread/CPU information
- LocalAppData diagnostic file append timing

제외:

- Ground Truth images
- profile database
- game account information

ZIP 생성 자체도 worker에서 수행한다. WPF-only environment property는 dispatcher를 통해 읽는다.

## diagnostic candidate CI proof

Code-complete diagnostic HEAD `d03b11ee16ddc9d201c904f460a8050d0397a2a9` CI run `32863058685`:

```text
Desktop build: SUCCESS
Tests: 380 passed / 0 failed / 0 skipped
Windows x64 self-contained publish: SUCCESS
Product UI smoke: SUCCESS
Map / Factory / MiniMap smoke: SUCCESS
Graceful shutdown: SUCCESS
Release package verification: SUCCESS
Artifact upload: SUCCESS
```

Diagnostic user package produced by that CI:

```text
Junhyun-Helper.zip
bytes: 80,461,362
SHA-256: ecbd92f7c67f5af9a37f12a1074e3f51a7aab648e538c62c48233a628b058c89
```

Later commits that only update documentation must still pass normal CI before merge, but the executable code at the above HEAD already passed the full Windows gate.

## 다음 필수 검증

문제 desktop에서:

```text
v1.7.6 diagnostic candidate 실행
→ 기존에 사용한 동일 screenshot으로 Display Test 재현
→ 가능하면 실제 Tarkov에서도 재현
→ 결과/지연 직후 Scanner > 고급 > Scanner 성능 진단 자료 내보내기
→ generated ZIP 분석
```

사용자는 raw log를 직접 찾거나 해석할 필요가 없다.

## bundle 해석 계약

```text
long ocr-winrt-recognize
→ Windows OCR backend stall proven

long ocr-copy-pixels / variant / image-key / software-bitmap
→ preprocessing/runtime conversion bottleneck proven

long serialized wait
→ shared OCR contention proven

fast OCR + long visual-recovery stage
→ Tarkov-font visual recovery bottleneck proven

slow file append probe
→ filesystem / antivirus I/O is material

ui-dispatcher-stall overlapping recognition
→ actual WPF UI starvation proven

slow recognition without dispatcher stall
→ worker-side backend/recovery delay; UI thread itself is responsive
```

## 완료 조건

이 P0 결함은 다음이 모두 만족되어야 해결 완료다.

- desktop Display Test에서 abnormal long stall 없음
- desktop actual Tarkov scan에서 abnormal long stall 없음
- OCR 실패/느린 환경에서도 UI remains responsive
- one backend fault가 dozens of serial delays로 증폭되지 않음
- notebook/desktop 간 동일 screenshot latency 차이가 비정상적이지 않음
- false Item ID 증가 없음
- recognition thresholds/acceptance safety 유지
- reviewed Scanner Ground Truth regression = 0
- future environment issues를 재현 가능한 telemetry 유지
- full Windows build/tests/publish/Product/Scanner/Map smoke 성공
- final `STATE.md`, release notes, decision status 갱신

문제 desktop evidence가 확보되기 전에는 v1.7.6을 resolved public performance release로 선언하지 않는다.
