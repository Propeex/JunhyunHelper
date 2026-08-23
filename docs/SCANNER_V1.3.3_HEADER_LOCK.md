# Scanner v1.3.3 — live inspect-header frame lock

기준일: 2026-08-23

상태: **RELEASE CANDIDATE**

## 1. 근거

v1.3.2 이후 사용자가 제공한 실제 2048×1280 Tarkov 상세창 12개를 다시 측정했다. raw 이미지는 저장소에 넣지 않고 `docs/.scanner-v1.3.3-header-evidence.json`에 상대 측정치만 보존한다.

반복 관측 범위:

- long neutral header frame: 822~862 px
- red close control: 25~27 × 16~17 px
- magnifier bright core: 12개 모두 13 × 13 px
- frame left → magnifier core X: 11~13 px
- frame top → magnifier core Y: 12개 모두 7 px
- magnifier core right → first bright title evidence: 5~6 px

`Awl` 같은 짧은 제목, 한글/영문 혼합 제목, 긴 무기명, 서로 다른 화면 위치에서도 같은 상대 구조가 유지됐다.

## 2. v1.3.2 회귀 원인

기존 title refinement에서는 fragmented Korean first-glyph connected component가 title ROI의 왼쪽 경계에 영향을 줄 수 있었고, title glyph가 magnifier morphology 경쟁에 참여할 수 있었다. 그 결과 실제 게임에서 제목 앞부분이 잘리거나 잘못된 search-icon anchor가 선택될 가능성이 남아 있었다.

## 3. v1.3.3 인식 계약

```text
structural detail candidate
→ red close/X
→ long neutral top frame
→ bounded frame-left search-icon lane
→ 13px-class magnifier core + ring/hollow/handle morphology
→ dark title field + text-presence corroboration
→ HEADER_FRAME_LOCKED
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog sanitation
→ semantic/visual resolver
→ Item ID or fail closed
```

`ScannerInspectHeaderLock`이 title ROI의 구조적 소유자다. first title glyph는 제목 존재를 corroborate할 수 있지만 ROI left edge를 결정하거나 오른쪽으로 이동시킬 수 없다.

완전한 `HEADER_FRAME_LOCKED` 결과는 score 0.68 이상이어야 한다. partial/failed result는 `ScannerTitleAnchorRefiner`에서 0.47 이하로 제한되므로 현재 runtime의 trusted-anchor floor를 통과하지 못하며 OCR identity path로 들어가지 않는다.

## 4. OCR / matcher diagnostics

Windows OCR이 반환한 raw text와 `ScannerOcrCharacterPolicy`가 current official Korean item catalog를 기준으로 정리한 matcher input을 별도로 기록한다.

예를 들어 current catalog에 존재하지 않는 `「` 같은 punctuation이 raw OCR에 나타날 수는 있지만 matcher input에서는 제거된다. raw OCR 노이즈와 실제 Item identity evidence를 동일한 값처럼 표시하지 않는다.

## 5. 유지되는 안전 계약

- current official Korean full-item catalog가 Item identity 권위
- normal semantic confidence와 top1/top2 margin 유지
- normalized length >= 7의 bounded one-edit 예외는 whole-catalog unique candidate + global runner-up 10 percentage-point margin 필요
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지
- icon 단독 identity 금지
- highest trader = valid non-flea RUB maximum
- flea average = positive `avg24hPrice`
- needed count = `NeededItems[itemId].RequiredTotal`
- Content schema v7 / user.db v1 / Scanner display settings v4 / Scanner catalog cache v1-v2 유지

## 6. 회귀 검증

packaged-EXE product smoke가 12개 실제 측정 geometry를 모두 합성해 검사한다. 각 표본은 측정된 header width, close size, magnifier offset, title gap을 재생한다.

추가 검증:

- fragmented first glyph가 있어도 ROI left edge 불변
- title lane 안에 search icon보다 ring-like한 decoy를 넣어도 bounded icon lane 밖이면 선택 금지
- 짧은 제목에서도 구조가 유지됨
- 상세창이 화면 오른쪽으로 크게 이동해도 header-relative 좌표로 동일 동작
- magnifier가 없으면 반드시 fail closed

상세 측정치는 `docs/.scanner-v1.3.3-header-evidence.json`을 참조한다.
