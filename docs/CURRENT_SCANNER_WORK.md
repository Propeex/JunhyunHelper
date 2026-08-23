# Current Scanner work

기준일: 2026-08-23

현재 작업: **v1.3.3 public verified 이후 실제 Tarkov live calibration**

## 공개 기준선

- `docs/RELEASE_1.3.3.md`
- `docs/.release-v1.3.3-status.json`

## v1.3.3 공식 근거

- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/.scanner-v1.3.3-header-evidence.json`
- `docs/DECISION_SCANNER_HEADER_LOCK_2026-08-23.md`

## 현재 recognition 원칙

- 사용자가 제공한 실제 2048×1280 상세창 12개의 header-relative 구조를 회귀 근거로 사용한다.
- title glyph는 ROI left edge를 소유하거나 오른쪽으로 이동시킬 수 없다.
- red X + long neutral frame + bounded left icon lane + magnifier + dark title field/text evidence가 완성되어 `HEADER_FRAME_LOCKED`가 되지 않으면 fail closed한다.
- runtime은 `HEADER_FRAME_LOCKED` + anchor score 0.68 이상을 다시 요구한다.
- raw OCR과 sanitized matcher input을 별도 진단한다.
- false positive보다 miss를 선호한다.
- live evidence 없이 global confidence/margin을 낮추지 않는다.
- 정확한 Item ID 이후 highest trader / flea `avg24hPrice` / `RequiredTotal` 연결은 기존 권위 경로를 유지한다.

## 다음 실제 검증

```text
실제 Tarkov 사용
→ success / miss / wrong identity 수집
→ 문제 직후 인식 원본 PNG 저장
→ 필요 시 scanner.log 확보
→ capture / candidate / header lock / ROI / OCR / matcher / visual / presentation 단계 분류
→ 같은 evidence를 regression으로 고정
→ 해당 단계만 수정
```

새 기능 추가보다 실제 인게임 evidence 기반 정확성·안정성 보완을 우선한다.
