# 준현 헬퍼 v1.7.3

## Scanner performance

- continuous 화면 관측: 350 ms → 200 ms
- 동일 안정 후보 semantic 실패 retry: 고정 1200 ms → 250/500/800/1200 ms adaptive backoff
- Windows OCR 입력의 PNG encode → stream → PNG decode 왕복 제거
- verified detail window의 좁은 영역 fresh revalidation fast-path 추가
- fast-path 불확실/제목 변화 시 같은 cycle에서 기존 full detector로 fail-safe fallback

## Accuracy contract unchanged

- structural floor 0.34
- HEADER_FRAME_LOCKED 0.68
- continuous candidate cap 8
- one-shot candidate cap 12
- OCR 확대/deep variant, catalog matcher, visual corroboration/recovery 판정 기준 변경 없음
- cross-frame OCR identity cache 없음
- false-positive보다 miss 선호 유지
