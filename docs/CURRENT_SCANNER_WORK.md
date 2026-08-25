# Current Scanner Work

기준일: 2026-08-26
상태: **P0 — ROOT CAUSE CONFIRMED / v1.7.6 FIX CANDIDATE / DESKTOP RE-VERIFICATION REQUIRED**

## 1. 현재 최우선 결함

일부 Windows 데스크탑에서 Scanner가 상세창을 찾은 뒤 `아이템 이름을 읽는 중입니다.` 상태에 5~13초 이상 머무르는 심각한 제품 결함을 P0으로 처리한다.

현재 public stable은 **v1.7.5**다.

```text
public stable: v1.7.5
exact release source: 215541a694459e9484716c4942a436c26defe919
stable asset: Junhyun-Helper.zip
stable bytes: 80,450,225
stable SHA-256: 6706f12e63caa2039cf3f89c6823b457d125e43f8af47779082caa843282923f
```

v1.7.6은 아직 public stable이 아니다. 문제 데스크탑에서 root cause를 실측했고 이를 교정한 fix candidate 단계다.

Reference:

- `docs/DECISION_V1.7.5_OCR_ENVIRONMENT_GUARD_2026-08-25.md`
- `docs/DECISION_V1.7.6_SCANNER_STALL_DIAGNOSTICS_2026-08-25.md`
- `docs/RELEASE_NOTES_V1.7.6.md`

## 2. 문제 데스크탑 실측 결과

사용자가 v1.7.6 diagnostic candidate를 문제 데스크탑에서 실행하고 support bundle을 제공했다.

결론은 명확하다.

**이 환경의 장시간 지연 root cause는 Windows OCR backend가 아니다.**

대표 continuous Tarkov cycle:

```text
end-to-end             12,540.77 ms
OCR normal                 12.26 ms
actual WinRT RecognizeAsync 10.57 ms
visual recovery         12,306.61 ms / 16 calls
catalog matching            75.16 ms
capture                      21.57 ms
rectangle proposal           53.57 ms
semantic header              53.51 ms
```

같은 bundle의 다른 지연 사례:

```text
Display Test one-shot #1
end-to-end       13,156.10 ms
visual recovery  12,277.39 ms / 16 calls

Display Test one-shot #2
end-to-end       11,127.35 ms
visual recovery  10,745.07 ms / 14 calls

Tarkov continuous
end-to-end        4,898.39 ms
visual recovery   4,624.02 ms / 6 calls
```

LocalAppData file append는 평균 약 0.14 ms, 최대 약 0.27 ms였으므로 filesystem/log I/O도 root cause가 아니다.

지연 Scanner cycle과 겹치는 WPF dispatcher stall도 없었다. 따라서 현재 증상은 UI thread freeze가 아니라 **worker-side visual recovery latency**다.

## 3. root cause 세부 구조

대표 cycle에서 Windows OCR은 `하프 마스크 (Lower half-mask)`를 약 10.57 ms 만에 읽었고 catalog matcher는 `EXACT`, confidence 1.0으로 판정했다.

그러나 후보 0~7이 동일한 title bitmap/text를 공유했음에도 각 후보가 `FontAwareScannerOcrEngine`의 Tarkov-font visual corroboration을 다시 실행했다.

```text
8 equivalent candidates
× targeted visual pass
× full-catalog visual fallback
= 16 visual-recovery calls
```

각 visual pass는 문제 PC에서 대략 0.75~0.78초를 소비했다. 따라서 이미 raw OCR cycle cache가 재사용되더라도 그 바깥의 visual corroboration이 같은 current-frame evidence를 반복 계산하면서 12초 이상으로 증폭됐다.

또한 `TarkovTitleFontProvider.TryGetFonts()`의 retry guard는 `FindResourcesAssets()`와 source-stamp 조회 **뒤**에 있었다. `FindResourcesAssets()`는 `EscapeFromTarkov` process의 `MainModule.FileName`을 조회한다. 따라서 font provider가 unavailable/retry 상태여도 비싼 source discovery가 매 visual call마다 먼저 실행될 수 있었다. 5초 retry가 실제 expensive lookup을 보호하지 못하는 구조적 버그였다.

새 fix candidate는 `title-font-source-probe` timing을 추가해 재검증 bundle에서 이 세부 비용도 직접 확인한다.

## 4. v1.7.6 fix candidate

### current-cycle exact visual evidence reuse

`FontAwareScannerOcrEngine`은 같은 Scanner decision cycle 안에서 다음이 모두 동일한 경우 visual corroboration 결과를 재사용한다.

- latency cycle ID
- title bitmap width/height
- exact pixel SHA-256
- OCR text

이는 cross-frame OCR/Item identity cache가 아니다.

- cycle ID가 바뀌면 즉시 폐기
- 현재 cycle의 exact current pixels만 대상
- 동일 입력에 이미 수행한 동일 visual acceptance 결과만 재사용
- candidate count와 matcher/visual threshold는 그대로 유지

따라서 대표 cycle처럼 동일 title image/text가 8개 후보에 반복되더라도 같은 expensive visual proof를 8번 다시 계산하지 않는다.

Trace event:

```text
visual-cycle-cache-hit
```

### Tarkov font source discovery hot-path 제거

`TarkovTitleFontProvider`는 이제:

- unavailable retry window를 **비싼 process/source discovery 전에** 검사한다.
- 실패 상태에서는 source discovery를 30초 동안 재실행하지 않는다.
- 성공적으로 찾은 `resources.assets` path를 process-local cache로 재사용한다.
- loaded font generation의 live source validation은 candidate마다 하지 않고 5초 주기로 제한한다.
- source file timestamp/length가 바뀌면 기존 generation 검증/재추출 안전 계약은 유지한다.

이 변경은 Item ID acceptance를 완화하지 않고, 동일한 local font evidence를 얻기 위한 반복 환경 탐색만 hot path에서 제거한다.

### 기존 v1.7.6 hardening 유지

- actual `OcrEngine.RecognizeAsync` 단위 timing
- slow-empty actual WinRT call circuit breaker
- serialized OCR/image-key/preprocessing timing
- one-shot scan worker dispatch
- independent WPF dispatcher stall probe
- one-click support bundle export

## 5. 정확도·안전 불변식

변경하지 않는다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
deep OCR candidate limit = existing value
```

추가 계약:

- false positive보다 miss 우선
- current official Tarkov catalog가 identity authority
- stale Item ID를 current identity proof로 사용하지 않음
- cross-frame OCR/visual Item identity cache 금지
- price/flea/slots/needed는 Item ID 이후 mapped data
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음
- matcher 및 targeted/full-catalog visual acceptance 완화 없음

## 6. CI proof

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

Fix-candidate user package extracted from that CI artifact:

```text
bytes: 80,462,063
SHA-256: 96af948b2cd24caeb612d1d89a368bf30329606d3e934a292758292f70dcae30
```

## 7. 다음 필수 검증

문제 데스크탑에서 동일 screenshot Display Test를 먼저 반복한다.

목표는 기존 실측과 직접 비교하는 것이다.

```text
baseline representative cycle:
end-to-end        12,540.77 ms
actual WinRT OCR      10.57 ms
visual recovery   12,306.61 ms / 16 calls
```

새 bundle에서는 다음을 확인한다.

- `visualRecoveryMs`가 수십 회 반복되지 않는지
- `visual-cycle-cache-hit`이 equivalent candidates에서 발생하는지
- `title-font-source-probe` 실제 latency와 횟수
- end-to-end가 실사용 가능한 수준으로 감소하는지
- Item ID 결과가 동일한지
- UI dispatcher stall이 없는지

Display Test가 정상화되면 actual Tarkov에서도 같은 검증을 수행한다.

## 8. 완료 조건

이 P0 결함은 다음이 모두 만족되어야 해결 완료다.

- desktop Display Test abnormal long stall 제거
- desktop actual Tarkov scan abnormal long stall 제거
- one backend/provider fault가 serial delay로 증폭되지 않음
- UI remains responsive
- false Item ID 증가 없음
- recognition thresholds/acceptance safety 유지
- reviewed Scanner Ground Truth regression = 0
- full Windows build/tests/publish/Product/Scanner/Map smoke 성공
- final `STATE.md`, release notes, decision status 갱신

문제 데스크탑 재검증 전에는 v1.7.6을 public resolved release로 선언하지 않는다.
