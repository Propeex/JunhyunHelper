# Scanner semantic candidate / OCR alphabet decision — 2026-08-24

상태: **CONFIRMED / IMPLEMENTING**

## 배경

v1.4.1~v1.4.2 실제 Tarkov Ground Truth에서 다음 문제가 반복됐다.

- 실제 상세보기 창과 겹치는 정답 구조 후보가 존재해도 IoU 기반 geometry dedupe에서 먼저 제거될 수 있음
- stash/inventory의 큰 사각형이 더 높은 구조 점수를 받아 실제 상세창보다 앞설 수 있음
- 상세보기 창 높이가 아이템/패널 구성에 따라 달라지는데 기존 geometry가 폭/높이 약 1.3 비율을 강하게 선호함
- 사용자가 잘못 잡힌 상세창을 교정해 확인한 경우, 그 오탐 후보에는 실제 Tarkov 제목 헤더의 돋보기/X가 없는 사례가 존재함
- OCR은 lower-case `r`, slash-zero 계열 `0` 등이 카탈로그에 존재하지 않는 괄호/기호/Unicode letter로 출력될 수 있음

## 상세보기 창 결정

상세창 판정의 책임을 geometry score에서 semantic header evidence로 옮긴다.

```text
visible Tarkov pixels
→ 가능한 rectangle proposals 생성
→ 불가능한 최소 크기/화면 범위/극단 비율만 제거
→ semantic 검증 전에 IoU만으로 겹치는 후보를 제거하지 않음
→ 각 proposal에서 red close X + magnifier + neutral header + dark title field + text evidence 검증
→ HEADER_FRAME_LOCKED >= 0.68인 후보를 semantic-ready로 승격
→ semantic-ready 후보를 geometry-only 후보보다 우선
→ OCR / current catalog identity
```

원칙:

1. 사각형은 **후보 생성**만 담당한다.
2. 사각형 점수만으로 상세보기 창을 확정하지 않는다.
3. 실제 X와 돋보기를 찾지 못한 후보는 OCR에 진입하지 않는다.
4. 서로 많이 겹쳐도 edge geometry가 실질적으로 다른 후보는 semantic 검증 전 보존한다.
5. 거의 동일한 jitter duplicate만 제거한다.
6. 상세창 height/aspect는 아이템에 따라 변하므로 1.3 근처 비율은 정답 조건이 아니라 약한 ranking hint만 된다.
7. 기존 fail-closed `HEADER_FRAME_LOCKED >= 0.68` 계약은 유지한다.
8. 추가 Ground Truth 없이 X/돋보기/header 신뢰 임계값 자체는 낮추지 않는다.

## OCR 문자 정책 결정

Scanner가 읽는 것은 자유 문장이 아니라 **현재 공식 Tarkov 한국어 아이템명 카탈로그 중 하나**다. 따라서 OCR 문자 유효성도 현재 카탈로그에서 파생한다.

```text
current official item names
→ NFKC/case-normalized 실제 사용 문자 집합 구축
→ OCR character validation
```

정책:

- 현재 공식 아이템명에서 실제 사용하는 punctuation/symbol만 ordinary OCR evidence에 허용한다.
- 이 원칙을 punctuation뿐 아니라 letter/digit Unicode code point에도 확장한다.
- 예: OCR이 slash-zero를 `Ø`처럼 현재 카탈로그에 없는 Unicode letter로 출력하면 정상 문자로 신뢰하지 않는다.
- 카탈로그에 존재하지 않는 embedded glyph는 특정 문자(`r`, `0` 등)로 강제 치환하지 않고 `?` unknown-glyph evidence로 보존한다.
- unknown glyph는 current catalog 전체에서 exact pattern candidate가 유일하고 충분히 분리된 경우에만 복구한다.
- 긴 이름에서는 최대 2개의 embedded unknown glyph를 bounded recovery 대상으로 허용하되, 유일 후보/known-character ratio/global margin을 모두 요구한다.
- 실제 공식 이름에 존재하는 따옴표, 하이픈, 괄호 등의 기호는 제거하지 않는다.
- CJK Han ideograph hard rejection은 유지한다.
- 일반 fuzzy confidence/margin은 낮추지 않는다.

## 회귀 요구사항

- 기존 Scanner unit/product smoke 전부 통과
- tall/large detail rectangle이 aspect prior 때문에 제거되지 않는 회귀 추가
- 높은 IoU지만 bottom/edge가 실질적으로 다른 rectangle proposals가 semantic 검증 전에 함께 보존되는 회귀 추가
- 카탈로그에 없는 Unicode letter/symbol이 ordinary identity character로 통과하지 않는 회귀 추가
- `r`/slash-zero류 불가능 glyph의 unknown-pattern recovery가 unique candidate에서만 성공하는 회귀 추가
- ambiguous pattern은 계속 fail closed
- 기존 정상 Ground Truth는 full-pipeline replay에서 `REGRESSION=0`을 요구

## 성능

이번 변경의 우선순위는 정확도/안정성이다. 후보 생성/semantic 검증 비용 최적화는 정확도 구조가 고정된 뒤 측정 기반으로 별도 수행한다.
