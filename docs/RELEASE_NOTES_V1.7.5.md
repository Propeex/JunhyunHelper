# 준현 헬퍼 v1.7.5

## Scanner OCR 환경 호환성 보호

- 노트북에서는 빠르지만 다른 Windows 데스크탑에서 `아이템 이름을 읽는 중입니다.` 단계가 장시간 지속되는 환경 차이를 OCR backend 문제로 분리했습니다.
- 현재 portable 준현 헬퍼가 사용하는 Windows 한국어 OCR이 빈 결과를 늦게 반환할 경우, 기존 normal/deep/tight-crop 복구가 같은 OS OCR을 여러 번 직렬 호출하며 지연을 크게 증폭할 수 있었습니다.
- 실제 OS OCR 한 번이 800 ms 이상 걸리고 결과까지 비어 있을 때만 30초 circuit breaker를 활성화합니다.
- circuit breaker 동안 동일 OS OCR 호출은 즉시 생략하고 기존 strict Tarkov-font visual recovery로 진행합니다.
- 느리더라도 유효한 텍스트를 반환한 OCR은 circuit breaker를 활성화하지 않습니다.
- 실제 OCR backend 호출마다 `ocr-backend-call` latency를 기록하고, 환경 정보/degraded/suppressed 상태도 scanner.log에 남깁니다.

## 정확도 계약

다음 기준은 변경하지 않았습니다.

- structural floor `0.34`
- `HEADER_FRAME_LOCKED` floor `0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- normal/deep OCR catalog matching acceptance
- Tarkov-font targeted/full-catalog visual recovery acceptance
- false positive보다 miss 우선
- cross-frame Item identity/OCR 결과 재사용 금지

OS OCR이 degraded된 경우에도 기준을 낮추지 않습니다. strict visual recovery가 Item ID를 충분히 입증하지 못하면 결과를 내지 않습니다.

## 기존 성능 개선 유지

- v1.7.4 non-backlogging 200 ms observation pacing
- v1.7.3 adaptive semantic retry `250 → 500 → 800 → 1200 ms`
- direct BGRA Windows OCR input
- verified-detail tracked fast path

게임 메모리 읽기, DLL injection, packet interception, scan-time network 요청은 사용하지 않습니다.
