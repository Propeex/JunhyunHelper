# Current Scanner work

현재 작업: **v1.3.2 public verified 이후 실제 Tarkov live calibration**

## 공개 기준선

- `docs/RELEASE_1.3.2.md`
- `docs/.release-v1.3.2-status.json`

## 현재 recognition 계약 / 근거

- `docs/DECISION_SCANNER_LIVE_EVIDENCE_2026-08-23.md`
- `docs/SCANNER_V1.3.2_LIVE_EVIDENCE.md`
- `docs/SCANNER_SYMBOL_POLICY.md`
- `docs/.scanner-v1.3.2-evidence.json`
- `docs/SCANNER_TEST_PLAN.md`

## 현재 작업 원칙

- 새 기능 추가보다 실제 인게임 결과를 우선한다.
- 새 문제는 capture → structural candidate → title anchors/ROI → OCR → catalog matcher/visual → presentation → overlay 단계로 분리한다.
- 실제 실패 evidence가 발생한 단계만 수정한다.
- 오인식보다 미인식을 허용하는 fail-closed 원칙을 유지한다.
- live evidence 없이 global confidence/margin을 낮추지 않는다.
- 정확히 인식된 Item ID에 대해 최고 상점가 / flea `avg24hPrice` / `RequiredTotal`의 end-to-end 연결도 실제 표본으로 계속 확인한다.
