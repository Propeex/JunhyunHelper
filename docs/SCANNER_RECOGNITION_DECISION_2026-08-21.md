# DEC-053 — Scanner geometry는 후보 생성이며 최종 상세창은 OCR + 현재 공식 카탈로그 semantic validation으로 확정한다

날짜: 2026-08-21

상태: **`CONFIRMED / IMPLEMENTED FOR v1.1.3 / LIVE TARKOV REVALIDATION PENDING`**

## 배경

JunhyunHelper Scanner 통합 과정에서 과거 Scanner Lab v3.8의 recognition pipeline이 단순화되었습니다.

잘 작동했던 v3.8은 여러 structural candidate를 만들고 후보마다 title OCR을 수행한 뒤 실제 official item으로 안전하게 resolve되는 후보만 최종 상세창으로 채택했습니다.

통합 중 한때 geometry 최고점 하나를 먼저 확정하고 그 영역만 OCR하는 구조가 들어갔고, 실제 사용자 환경에서 인식률이 크게 하락했습니다.

사용자가 보존하고 있던 `TarkovHelper-ScannerLab-v3.8` 원본을 다시 확보해 실제 구현을 비교했고 이 차이를 회귀 원인으로 확정했습니다.

## 결정

Scanner의 recognition pipeline은 다음 책임 분리를 유지합니다.

```text
screen pixels
→ structural candidate generation
→ candidate title ROI
→ OCR
→ current official full-item catalog semantic validation
→ final inspect candidate
→ Item ID
```

### 1. Geometry의 책임

Geometry detector는 **상세창 후보를 생성하고 순위를 매길 뿐**, Item identity나 최종 상세창을 단독 확정하지 않습니다.

복원하는 v3.8 구조:

- RED-X connected-component detection
- RED-X anchored outer-window reconstruction
- rectangle/edge projection fallback
- IoU deduplication
- maximum 8 candidates
- structural floor 0.34

### 2. Semantic validation의 책임

각 structural candidate의 title ROI를 OCR하고 current official Korean full-item catalog와 대조합니다.

- official item으로 안전하게 resolve된 candidate만 final inspect가 될 수 있습니다.
- geometry score가 높아도 catalog resolution이 실패하면 최종 상세창이 아닙니다.
- false positive보다 miss를 선호합니다.

### 3. OCR 전략

Scanner Lab v3.8에서 검증된 adaptive OCR 전략을 유지합니다.

- title height <=14px: 8x
- <=20px: 6x
- otherwise: 4x

1차 resolution 실패 시 상위 3개 candidate에:

- original
- high-contrast grayscale
- binary white-on-black
- inverse black-on-white

을 적용합니다.

OCR line 개별 값과 인접 두 line 결합 값도 resolver candidate로 사용합니다.

### 4. Matcher는 느슨하게 만들지 않는다

복원 작업은 OCR 입력과 candidate selection을 고치는 작업입니다.

다음은 유지합니다.

- current official Korean displayed string이 identity truth
- exact-first
- conservative fuzzy
- high confidence threshold
- top1/top2 margin
- ambiguous / low confidence fail-closed
- historical alias production 금지

과거 Scanner Lab의 테스트용 alias를 production catalog/matcher에 추가하지 않습니다.

### 5. 현재 제품 경계 유지

다음은 v3.8으로 되돌리지 않습니다.

- current Tarkov-window / Display Test capture infrastructure
- Borderless window targeting
- current catalog/cache subsystem
- Item ID 이후 JunhyunHelper data bridge
- RequiredTotal 의미
- Scanner tab / activity UI
- Mini Scanner
- diagnostics/persistence

즉 v3.8은 **recognition reference**이며 Scanner Lab 앱 전체를 제품으로 복원하는 것이 아닙니다.

## 검증 기준

공식 reference:

- `docs/SCANNER_LAB_3_8_REFERENCE.md`

고정 회귀:

- cropped `Ophthalmoscope 검안경` inspect/title ROI
- full `Water 0.6L 물병` screenshot inspect/title ROI
- strong inner rectangle coexistence
- no-RED-X rectangle fallback
- uniform frame fail-closed

복원 제품 코드 validation:

```text
CI #1222
run 32466187224
245 passed / 0 failed / 0 skipped
Windows Release build: SUCCESS
win-x64 publish: SUCCESS
published candidate EXE Scanner/Product UI + Map/Factory/MiniMap smoke: SUCCESS
```

## 버전

새 사용자 기능이 아니라 기존 Scanner의 recognition 회귀 복구이므로 DEC-048에 따라 **v1.1.3 PATCH**입니다.

## 후속

최신 Tarkov Borderless 실제 E2E는 릴리즈 후 사용자 환경에서 다시 검증합니다.

후속 수정은 `scanner.log`의 structural candidate / OCR pass / match / semantic-selected 기록을 기준으로 계층을 분리해 진행합니다.
