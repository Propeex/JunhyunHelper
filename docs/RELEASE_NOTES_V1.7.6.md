# 준현 헬퍼 v1.7.6

## Scanner 장시간 인식 지연 해결

v1.7.6은 일부 Windows PC에서 Scanner가 아이템 이름을 읽는 단계에 수 초 이상 머무르던 문제를 해결합니다.

문제 PC의 진단 자료를 통해 Windows OCR 자체가 아니라 **같은 현재 화면의 visual recovery를 여러 후보에서 반복 계산하던 구조**가 실제 병목임을 확인했고, 정확도 기준을 낮추지 않고 중복 계산을 제거했습니다.

### 문제 PC 실측

수정 전 대표 실제 Tarkov Scanner cycle:

```text
전체                  12,540.77 ms
Windows OCR                12.26 ms
실제 RecognizeAsync         10.57 ms
visual recovery         12,306.61 ms / 16 calls
```

동일 title bitmap과 OCR 결과를 공유하는 8개 구조 후보가 targeted/full-catalog visual corroboration을 각각 반복하면서 동일 증거를 16회 계산하고 있었습니다.

또한 optional Tarkov font provider의 retry check가 expensive process/source discovery 뒤에 있어, 사용 불가능한 환경에서도 source discovery가 반복될 수 있었습니다.

파일 로그 I/O와 WPF UI thread는 이 PC에서 장시간 지연의 주 원인이 아니었습니다.

## 수정 내용

- 같은 Scanner cycle에서 **정확히 동일한 title pixels + OCR text**에 대한 visual corroboration 결과를 한 번만 계산합니다.
- cycle이 바뀌면 재사용 결과를 즉시 폐기합니다. cross-frame Item identity cache를 만들지 않습니다.
- candidate cap이나 recognition threshold는 줄이거나 낮추지 않았습니다.
- Tarkov font provider의 unavailable retry를 비싼 process/source discovery보다 먼저 적용합니다.
- 확인된 `resources.assets` path를 process-local로 재사용합니다.
- 이미 로드된 font generation의 source validation을 candidate마다 반복하지 않습니다.

### OCR / UI 안정성 보강

- 실제 `Windows.Media.Ocr.OcrEngine.RecognizeAsync` 호출 하나하나의 latency를 분리해 진단할 수 있습니다.
- actual slow-empty OCR call은 bounded circuit breaker로 후속 실패 증폭을 막습니다.
- OCR semaphore, image-key, image preprocessing 단계별 timing을 분리합니다.
- 1회 스캔은 WPF message-pump thread에서 recognition 작업을 직접 시작하지 않고 worker에서 실행합니다.
- WPF dispatcher stall을 OCR latency와 독립적으로 관측합니다.
- `Scanner > 고급 > Scanner 성능 진단 자료 내보내기`에서 support ZIP을 한 번에 저장할 수 있습니다.

## 수정 후 문제 PC 검증

### 동일 Display Test

```text
하프 마스크
10,840.877 ms → 70.603 ms
약 99.35% 감소

USB 보안 플래시 드라이브
12,686.278 ms → 1,354.775 ms
약 89.32% 감소
```

USB 보안 플래시 드라이브는 normal OCR 이후 deep/recovery가 필요한 어려운 사례입니다. 수정 후 약 1초 수준의 bounded recovery로 끝나며, 기존 5~13초 직렬 지연 증폭과 구분됩니다.

추가 Display Test:

```text
Maska-1SCh            106.619 ms
Domontovich 우샨카      88.190 ms
Wires 전선             100.802 ms
PSU 전원공급장치         48.123 ms
```

### 실제 Tarkov

문제 PC에서 실제 `TarkovWindow` Scanner도 재검증했습니다.

성공 12건의 `ReadingTitle → ShowingItem`:

```text
minimum   38.07 ms
median    63.92 ms
maximum    1.05 s
mean      211.47 ms
```

retained performance trace의 OCR-active full cycle 11건:

```text
minimum end-to-end   178.04 ms
median               210.82 ms
maximum              517.74 ms
```

추가 확인:

- 동일 visual evidence 재사용 `73회`
- 반복 visual recovery는 사실상 `0~0.01 ms`
- WPF dispatcher stall `0회`
- actual WinRT OCR은 대체로 약 `4~13 ms`
- LocalAppData diagnostic append는 sub-ms

사용자 실사용 평가에서도 수정 후 속도가 충분히 만족스러운 수준임을 확인했습니다.

## 정확도·안전 계약

다음 기준은 변경하지 않았습니다.

- structural floor `0.34`
- `HEADER_FRAME_LOCKED` floor `0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- 기존 deep OCR candidate limit
- 기존 catalog matcher acceptance
- 기존 Tarkov-font targeted/full-catalog visual acceptance
- false positive보다 miss 우선
- stale Item ID를 현재 identity 근거로 사용하지 않음
- cross-frame OCR/visual identity cache 금지
- Item ID 확정 전 가격/필요 개수 등의 mapped data를 identity 근거로 사용하지 않음
- scan-time network 없음
- game memory reading, DLL injection, packet interception, Tarkov process hook 없음

성능을 더 낮추기 위해 위 안전 기준을 완화하지 않습니다.

## 자동 검증

Root-cause fix code HEAD `d04f39697a4ea4d6ff4eabcb2acdc6bc535c8f9c`, CI run `32866068233`:

- Desktop build 성공
- 380/380 tests 통과
- Windows x64 self-contained publish 성공
- Product UI / Map / Factory / MiniMap smoke 성공
- graceful shutdown 성공
- release package verification 성공

사용자가 실제 검증한 fix candidate:

```text
bytes: 80,462,063
SHA-256: 96af948b2cd24caeb612d1d89a368bf30329606d3e934a292758292f70dcae30
```

최종 공개 릴리즈는 final source CI/package 검증과 release asset/source identity readback까지 완료한 뒤 확정합니다.
