# Current Scanner Work

기준일: 2026-08-26
상태: **P0 RESOLVED — v1.7.6 PERFORMANCE FIX VERIFIED / RELEASE FINALIZATION**

## 현재 결론

v1.7.5까지 문제 데스크탑에서 재현되던 Scanner 장시간 인식 지연은 v1.7.6 fix candidate에서 실사용 가능한 수준으로 정상화되었다.

사용자 체감 평가도 `엄청 괜찮아졌다`이며, 두 번째 support bundle의 수치가 이를 뒷받침한다.

현재 public stable은 **v1.7.5**다. v1.7.6은 아직 public stable이 아니며, 성능 수정은 완료 판정하고 release finalization만 남긴다.

```text
public stable: v1.7.5
exact release source: 215541a694459e9484716c4942a436c26defe919
stable asset: Junhyun-Helper.zip
stable bytes: 80,450,225
stable SHA-256: 6706f12e63caa2039cf3f89c6823b457d125e43f8af47779082caa843282923f
```

Reference:

- `docs/DECISION_V1.7.5_OCR_ENVIRONMENT_GUARD_2026-08-25.md`
- `docs/DECISION_V1.7.6_SCANNER_STALL_DIAGNOSTICS_2026-08-25.md`
- `docs/RELEASE_NOTES_V1.7.6.md`

## Root cause

첫 diagnostic bundle에서 장시간 지연의 실제 원인은 Windows OCR backend가 아니라 `FontAwareScannerOcrEngine` 이후의 optional Tarkov-font visual recovery였다.

대표 baseline Tarkov cycle:

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

같은 current-frame title bitmap/text를 공유하는 구조 후보 8개가 각각 targeted + full-catalog visual verification을 반복하면서 동일 증거를 16회 계산했다.

또한 `TarkovTitleFontProvider`의 unavailable retry check가 expensive process/source discovery 뒤에 있어 optional font source discovery 자체도 candidate hot path에서 반복될 수 있었다.

## v1.7.6 수정

### Current-cycle exact visual evidence reuse

동일 Scanner latency cycle 안에서 다음 값이 모두 같은 visual corroboration 결과는 한 번만 계산한다.

- cycle ID
- title bitmap width/height
- exact current-pixel SHA-256
- OCR text

이는 cross-frame Item identity cache가 아니다. cycle이 바뀌면 즉시 폐기되며, 현재 frame의 동일 deterministic visual proof만 재사용한다.

Trace:

```text
visual-cycle-cache-hit
```

### Tarkov font source discovery hot-path 차단

`TarkovTitleFontProvider`는:

- unavailable retry state를 expensive process/source discovery 전에 확인
- failed/unavailable source attempt는 30초 동안 재실행 억제
- 성공적으로 찾은 `resources.assets` path를 process-local cache로 재사용
- loaded generation source validation을 candidate마다 하지 않고 5초 주기로 제한
- live source length/timestamp 변경 시 기존 invalidation/re-extraction 안전 계약 유지

### 기존 v1.7.6 hardening 유지

- actual `OcrEngine.RecognizeAsync` call별 timing
- actual slow-empty WinRT circuit breaker
- serialized OCR semaphore/image-key/preprocessing timing
- one-shot scan worker dispatch
- independent WPF dispatcher stall probe
- one-click Scanner support bundle export

## 두 번째 문제 데스크탑 검증

사용자가 root-cause fix candidate를 같은 문제 PC에서 다시 시험하고 support bundle을 제공했다.

### 동일 Display Test 직접 비교

```text
하프 마스크
before: 10,840.877 ms
 after:     70.603 ms
reduction: 약 99.35%

USB 보안 플래시 드라이브
before: 12,686.278 ms
 after:  1,354.775 ms
reduction: 약 89.32%
```

USB 사례는 corrupted normal OCR 뒤 deep OCR을 수행하는 어려운 첫 인식 사례다. 약 1.35초는 기존 12.7초 직렬 stall과 성격이 다르며 사용자가 실사용상 만족한다고 평가했다.

그 외 새 Display Test 결과:

```text
Maska-1SCh:          106.619 ms
Domontovich 우샨카:   88.190 ms
Wires 전선:          100.802 ms
PSU 전원공급장치:     48.123 ms
```

### 실제 Tarkov 검증

새 bundle에는 `mode=TarkovWindow` 실제 게임 인식이 포함된다.

확정 Item 예:

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

12개의 ShowingItem 사례에서 `ReadingTitle → ShowingItem`:

```text
minimum:  38.07 ms
median:   63.92 ms
maximum:   1.05 s
mean:     211.47 ms
```

최대 약 1.05초인 USB 사례는 deep/retry가 필요한 어려운 OCR 사례다.

retained performance trace에서 OCR이 실제 실행된 complete Scanner cycle 11개의 end-to-end:

```text
minimum: 178.04 ms
median:  210.82 ms
maximum: 517.74 ms
```

첫 약 1초 USB cycle은 bounded trace의 oldest-entry drop 이전 구간이라 이 11-cycle 집계에는 포함되지 않는다.

### 병목 제거 증거

retained trace:

```text
visual-cycle-cache-hit: 73회
visual-recovery stage: 반복 cycle에서 0~0.01 ms 수준
WPF ui-dispatcher-stall: 0회
actual WinRT OCR: 대체로 약 4~13 ms
```

Environment/file I/O:

```text
ScannerDiagnosticLogWriteProbeMs: 0.30 ms
DiagnosticFileAppendAverageMs:    0.14 ms
DiagnosticFileAppendMaximumMs:    0.25 ms
```

따라서 기존 5~13초 지연 증폭은 제거되었고 UI thread starvation이나 filesystem I/O가 남은 병목으로 보이지 않는다.

## 성능 최종 판단

**P0 Scanner 장시간 stall은 해결 완료로 판정한다.**

추가로 sub-100ms를 목표로 recovery/acceptance 구조를 변경하지 않는다. 현재 어려운 OCR에서 약 1초까지 발생하는 것은 허용 가능한 bounded recovery cost이며, 더 공격적인 성능 최적화는 false positive 방지와 복구 정확도에 불필요한 위험을 만든다.

성능 관련 threshold/candidate cap은 변경하지 않는다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
deep OCR candidate limit = existing value
```

추가 안전 계약:

- false positive보다 miss 우선
- current official Tarkov catalog가 identity authority
- stale Item ID를 current identity proof로 사용하지 않음
- cross-frame OCR/visual Item identity cache 금지
- price/flea/slots/needed는 Item ID 이후 mapped data
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음
- matcher 및 targeted/full-catalog visual acceptance 완화 없음

## CI proof

Root-cause fix code HEAD:

```text
d04f39697a4ea4d6ff4eabcb2acdc6bc535c8f9c
CI run: 32866068233
Desktop build: SUCCESS
Tests: 380 passed / 0 failed / 0 skipped
Windows x64 self-contained publish: SUCCESS
Product UI smoke: SUCCESS
Map / Factory / MiniMap smoke: SUCCESS
Graceful shutdown: SUCCESS
Release package verification: SUCCESS
Artifact upload: SUCCESS
```

사용자가 검증한 fix-candidate package:

```text
bytes: 80,462,063
SHA-256: 96af948b2cd24caeb612d1d89a368bf30329606d3e934a292758292f70dcae30
```

현재 documentation HEAD CI도 SUCCESS다.

## 남은 작업

성능 알고리즘 자체는 더 수정하지 않는다.

public v1.7.6 finalization 전에:

1. reviewed Scanner Ground Truth regression에서 REGRESSION=0 확인
2. temporary diagnostic implementation 중 release에 불필요한 부분은 동작 변화 없이 정리 가능한지 검토; 위험하면 그대로 두고 후속 기술부채로 기록
3. `STATE.md`를 v1.7.5/v1.7.6 실제 상태에 맞게 갱신
4. final release notes/proof 갱신
5. final HEAD Windows build/tests/publish/smoke/package gate
6. PR #185 merge
7. v1.7.6 public stable publication 및 release asset hash/size readback

성능 문제 재수정은 새로운 실측 evidence가 생긴 경우에만 재개한다.
