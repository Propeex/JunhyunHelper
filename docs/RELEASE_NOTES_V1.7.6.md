# 준현 헬퍼 v1.7.6

## Scanner 장시간 정지 진단 후보

v1.7.6은 v1.7.5에서도 해결되지 않은 PC별 Scanner 장시간 정지의 실제 blocking phase를 측정하기 위한 진단 후보입니다. 문제 데스크탑의 실행 자료로 root cause가 확인되기 전에는 성능 문제 해결 완료 릴리즈로 취급하지 않습니다.

- 기존 Windows `OcrEngine` 인스턴스와 기존 Scanner 인식 경로를 유지합니다.
- serialized OCR semaphore 대기 시간을 측정합니다.
- exact-image key 생성의 `CopyPixels` 및 SHA-256 시간을 측정합니다.
- title 확대와 deep OCR variant 생성을 각각 측정합니다.
- BGRA 변환, OCR input `CopyPixels`, WinRT `SoftwareBitmap` 생성을 각각 측정합니다.
- 실제 `Windows.Media.Ocr.OcrEngine.RecognizeAsync` 호출 하나하나의 시작/종료와 latency를 기록합니다.
- WPF dispatcher 지연을 OCR latency와 독립적으로 관측해 실제 UI starvation 여부를 구분합니다.
- 세부 timing은 bounded in-memory trace에 저장하여 진단 기록 자체가 파일 I/O 병목을 만들지 않도록 합니다.
- `Scanner > 고급 > Scanner 성능 진단 자료 내보내기`에서 환경 정보, 기존 Scanner 로그와 세부 performance trace를 ZIP 하나로 저장할 수 있습니다.
- 진단 ZIP에는 Ground Truth 이미지, 프로필 DB, 게임 계정 정보가 포함되지 않습니다.

## 정확도·안전 계약

다음 기준은 변경하지 않았습니다.

- structural floor `0.34`
- `HEADER_FRAME_LOCKED` floor `0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- 기존 deep OCR candidate limit
- 기존 catalog matcher acceptance
- 기존 Tarkov-font targeted/full-catalog visual recovery acceptance
- false positive보다 miss 우선
- stale Item ID를 현재 identity 근거로 사용하지 않음
- cross-frame OCR identity cache 금지
- Item ID 확정 전 가격/필요 개수 등의 mapped data를 identity 근거로 사용하지 않음
- scan-time network 없음
- game memory reading, DLL injection, packet interception, Tarkov process hook 없음

## 릴리즈 게이트

문제 데스크탑에서 동일 증상을 재현하고 진단 ZIP으로 실제 blocking phase가 확인된 뒤에만 최종 수정과 정식 patch release 여부를 결정합니다.
