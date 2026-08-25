# 준현 헬퍼 v1.7.6

## Scanner 장시간 지연 fix candidate

v1.7.6은 아직 정식 공개 릴리즈가 아닙니다. 문제 데스크탑의 진단 자료로 장시간 지연의 root cause를 확인했고, 해당 병목을 직접 수정한 재검증 후보입니다.

### 확인된 원인

문제 PC의 대표 실제 Tarkov Scanner cycle:

```text
전체              12,540.77 ms
Windows OCR            12.26 ms
실제 RecognizeAsync     10.57 ms
visual recovery     12,306.61 ms / 16 calls
```

Windows OCR, 파일 로그 I/O, WPF UI thread가 주 병목이 아니었습니다. 동일한 현재-frame title evidence를 여러 Scanner 후보가 공유하는데도 Tarkov-font visual corroboration을 후보마다 반복하고, optional font provider의 source discovery도 retry 보호 밖에서 반복될 수 있던 구조가 지연을 증폭했습니다.

### 이번 fix candidate

- 동일 Scanner decision cycle에서 title pixels + OCR text가 정확히 동일한 경우 완료된 visual corroboration 결과를 cycle-local로 재사용합니다.
- cycle이 바뀌면 해당 결과는 폐기합니다. cross-frame Item identity/OCR cache를 만들지 않습니다.
- candidate cap을 줄이지 않으며 동일한 후보 검증 의미를 유지합니다.
- Tarkov font provider의 unavailable retry를 비싼 process/source discovery보다 먼저 적용합니다.
- 발견한 `resources.assets` 경로를 process-local로 재사용하고, loaded font source 검증을 candidate마다 반복하지 않습니다.
- `visual-cycle-cache-hit`, `title-font-source-probe`를 추가해 수정 효과를 다음 support bundle에서 직접 확인할 수 있습니다.

### 기존 v1.7.6 진단/응답성 개선 유지

- 실제 `Windows.Media.Ocr.OcrEngine.RecognizeAsync` 호출별 latency 기록
- actual slow-empty OCR call circuit breaker
- OCR semaphore / image-key / preprocessing timing 분리
- 1회 스캔 recognition worker 분리
- WPF dispatcher stall 독립 측정
- `Scanner > 고급 > Scanner 성능 진단 자료 내보내기`

## 정확도·안전 계약

변경하지 않았습니다.

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

## 자동 검증

Root-cause fix code HEAD `d04f39697a4ea4d6ff4eabcb2acdc6bc535c8f9c`, CI run `32866068233`:

- Desktop build 성공
- 380/380 tests 통과
- Windows x64 self-contained publish 성공
- Product UI / Map / Factory / MiniMap smoke 성공
- graceful shutdown 성공
- release package verification 성공

재검증용 패키지:

```text
bytes: 80,462,063
SHA-256: 96af948b2cd24caeb612d1d89a368bf30329606d3e934a292758292f70dcae30
```

## 릴리즈 게이트

동일 문제 데스크탑에서 Display Test와 실제 Tarkov Scanner의 latency가 정상화되고, 새 support bundle에서 반복 visual recovery가 제거된 것을 확인한 뒤에만 final production cleanup, Ground Truth regression, 최종 CI와 공개 v1.7.6 릴리즈를 진행합니다.
