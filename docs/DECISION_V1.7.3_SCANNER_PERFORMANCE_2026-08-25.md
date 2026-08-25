# v1.7.3 Scanner Performance Pass

기준일: 2026-08-25
상태: IMPLEMENTED — RELEASE CANDIDATE VALIDATION

## 목표

실사용에서 체감한 약 1초 단위 semantic recognition cadence를 줄이되 Scanner의 false-positive 방지 계약을 변경하지 않는다.

## 고정 안전 불변식

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
false positive보다 miss 선호
current official Korean catalog authority
ambiguity / low confidence fail closed
cross-frame OCR identity cache 금지
scan-time network / game memory / injection / packet interception 금지
```

## 적용

- Windows OCR PNG encode/stream/decode round-trip 제거. 동일 BGRA 픽셀을 SoftwareBitmap으로 직접 전달.
- continuous observation 350 ms → 200 ms.
- 동일 안정 후보 반복 실패만 250 / 500 / 800 / 1200 ms adaptive backoff. 1200 ms ceiling 유지.
- verified detail rectangle을 24px margin의 좁은 fresh capture로 먼저 재검증.
- tracked path도 close-X + magnifier + HEADER_FRAME_LOCKED >= 0.68 + fresh title signature를 모두 요구.
- tracked path는 새 Item ID를 결정하지 않으며, 실패/제목 변화 시 같은 cycle에서 full detector로 fallback.

## 의도적으로 제외

- 첫 OCR 성공 후 나머지 candidate 무조건 생략
- deep OCR candidate cap 축소
- full-catalog visual recovery 생략/threshold 완화
- cross-frame OCR cache
- structural/header/matcher threshold 완화

위 항목들은 결과 선택/정확도에 영향을 줄 수 있으므로 이번 accuracy-neutral pass에서 적용하지 않는다.
