# Scanner 상세창/제목 ROI 회귀 결정 — 2026-08-21

상태: **CONFIRMED / IMPLEMENTING FOR v1.1.2**

## 결정

실제 사용자 검증에서 아이템 제목 대신 바로 아래 분류 행을 OCR하는 문제가 확인되면 matcher confidence를 낮추거나 분류 문자열을 사후 필터링하는 방식으로 해결하지 않는다.

Scanner의 화면 의미는 항상:

```text
상세창 외곽 구조
→ 상세창 최상단 제목 한 줄
→ OCR
→ current official Item catalog matcher
```

순서를 유지한다.

## 이번 결함의 판정

사용자가 제공한 현재 한국어 클라이언트 `Ophthalmoscope 검안경` 상세창에서 v1.1.1 증상을 재현했다.

v1.1.1 통합 detector는 현재 상세창 전체가 아닌 내부의 더 작은 고대비 사각형을 상세창 후보로 선택할 수 있었고, 그 결과 title ROI가 실제 `교환용 물품 > 의료용품` 분류 행 높이에 걸렸다.

또한 기존 title ROI 자체도 제목 한 줄보다 높아 분류 행을 함께 포함할 수 있었다.

따라서 이번 문제는:

- catalog 문제 아님
- matcher threshold 문제 아님
- OCR 언어 선택 문제로 우선 판정하지 않음
- **geometry/ROI integration regression**

으로 기록한다.

## v1.1.2 구현 원칙

- current observed detail geometry를 기준으로 외곽 프레임을 탐지한다.
- 외곽 상/하/좌/우 테두리와 우상단 close-control 구조를 함께 요구한다.
- 내부의 작은 고대비 사각형이 외곽창을 이기지 않게 한다.
- title ROI는 breadcrumb/category 행 시작 전에 끝나야 한다.
- stricter geometry 때문에 search precision이 떨어지지 않도록 픽셀 단위로 탐색한다.
- 늘어난 후보 수는 close-control/edge 순차 early-reject로 보완한다.
- exact/fuzzy matcher confidence 및 margin은 완화하지 않는다.
- 인식 불확실 시 기존 fail-closed를 유지한다.

## 회귀 기준

사용자가 제공한 676x522 상세창에 새 title ROI를 적용했을 때 OCR 입력 영역에는:

```text
Ophthalmoscope 검안경
```

만 들어가야 하며:

```text
교환용 물품 > 의료용품
```

은 들어가면 안 된다.

자동 테스트는 강한 내부 사각형이 있어도 외곽창을 선택하는 조건을 포함한다.

## 후속

v1.1.2 실제 사용자 검증에서 제목 ROI가 올바른데도 OCR 자체가 불안정한 경우에만 Windows OCR preprocessing/variant 전략을 다음 계층 문제로 다룬다. geometry와 OCR 문제를 섞어서 해결하지 않는다.
