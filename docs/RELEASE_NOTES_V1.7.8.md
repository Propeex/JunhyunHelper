# 준현 헬퍼 v1.7.8

## 레이드 Scanner header 인식 회귀 수정

v1.7.8은 v1.7.7 공개 이후 실제 레이드에서 확인된 Scanner 인식 저하를 수정하는 유지보수 PATCH입니다.

사용자 reviewed 교정 데이터 분석 결과, 실패 사례 대부분은 OCR이 글자를 잘못 읽은 것이 아니라 **빨간 X/돋보기 semantic header 검증 단계에서 OCR 이전에 중단**되고 있었습니다.

## 확인된 문제

reviewed Case 8건 중 6건에서:

- 상세보기 창 rectangle은 정상 검출
- item-name ROI도 정상 검출
- 실제 화면에는 빨간 X와 돋보기가 존재
- 프로그램 진단에서는 close/magnifier가 null
- `HEADER_CLOSE_NOT_LOCKED`
- `TITLE_ANCHOR_INCOMPLETE`
- raw OCR empty

레이드 인벤토리의 긴 회색 수평선이 상세보기 header와 이어져 보이면서 기존 fallback이 상세창의 왼쪽 경계를 실제보다 47~132px 왼쪽으로 잡았습니다. 이 오차 때문에 돋보기 검색 위치까지 왼쪽으로 이동했고, 결국 OCR 단계에 진입하지 못했습니다.

## 수정 내용

- 기존 정상 header lock 경로는 그대로 유지합니다.
- 기존 경로가 실패하고 강한 상세창 후보가 존재하는 경우에만 raid ownership recovery를 추가합니다.
- 주변 UI 선이 왼쪽으로 이어져도 상세창 proposal의 왼쪽 경계를 header ownership 기준으로 사용합니다.
- 빨간 X와 돋보기는 별도의 template/evidence 검증을 그대로 요구합니다.
- item-name field의 어두운 배경과 실제 text evidence도 다시 확인합니다.
- 최종 `HEADER_FRAME_LOCKED >= 0.68` 기준을 그대로 유지합니다.

따라서 geometry만 맞는 화면을 상세보기 창으로 강제 승인하는 수정이 아닙니다.

## 현재 결과 교정 접근성

Scanner 메인 상단에 `현재 결과 교정` 버튼을 옮겼습니다.

```text
스캐너 ON/OFF / 설정 / 고급 / 현재 결과 교정
```

이제 인식 결과를 확인한 직후 고급 창을 한 번 더 열지 않고 바로 교정할 수 있습니다.

고급 창에는 테스트 스캐너, 교정 데이터 관리, Scanner 성능 진단 자료 내보내기만 남깁니다.

## Scanner 안전 기준

이번 PATCH에서 다음 값은 변경하지 않습니다.

- structural floor `0.34`
- `HEADER_FRAME_LOCKED` floor `0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- OCR variant 정책
- catalog matcher acceptance
- visual recovery acceptance
- 200ms continuous observation target
- false positive보다 miss 우선
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음

v1.7.7의 사용자 선택형 교정 저장, legacy automatic sample 안전 정리, 반복 실패 로그 억제, Scanner/Map 단축키 계약도 그대로 유지합니다.

## 검증

사용자가 제공한 reviewed 8 Case의 실제 픽셀 evidence를 수정된 ownership 모델로 대조했을 때 모두 기존 final semantic floor 0.68을 넘었습니다. 공개 저장소에는 사용자의 Ground Truth 이미지를 포함하지 않습니다.

CI에는 레이드 수평선 bleed를 절차적으로 재현하는 positive smoke와 red close가 없는 negative fail-closed smoke를 추가합니다.

최종 v1.7.8은 Desktop build, 전체 자동 테스트, Windows x64 publish, Product UI/Scanner/Map smoke, 패키지 검증, main CI, release workflow와 공개 asset readback을 모두 통과한 경우에만 stable로 확정합니다.
