# Current Scanner work

기준일: 2026-08-24

현재 작업: **v1.4.3 공개 완료 후 실제 Tarkov Ground Truth 추가 수집 및 안정성 검증**

## 공개 기준선

현재 public stable / latest는 **v1.4.3**입니다.

```text
release source/tag: f7e3870c81a7d7be025f1fe56d5b7f607546b250
feature PR #165 CI: 32660568132 — SUCCESS
release-prep PR #166 CI: 32674399495 — SUCCESS
279 tests / 0 failed / 0 skipped
release run: 32674812862 — SUCCESS
independent public verifier: 32675069359 — SUCCESS
asset: Junhyun-Helper-v1.4.3-win-x64.zip
bytes: 80,389,336
SHA-256: fa5da9f2a6b9ea62f8a9a2ddfb1062bed81609fb96516a01089238b92067a8be
ProductVersion: 1.4.3+f7e3870c81a7d7be025f1fe56d5b7f607546b250
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download / SHA256SUMS / package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

공식 기록:

- `docs/RELEASE_1.4.3.md`
- `docs/.release-v1.4.3-status.json`
- `docs/DECISION_SCANNER_SEMANTIC_CANDIDATE_AND_OCR_ALPHABET_2026-08-24.md`
- `docs/SCANNER_GROUND_TRUTH.md`

완료된 one-shot release/verifier workflow는 검증 후 제거하고 정상 `.github/workflows/ci.yml`만 유지합니다.

## v1.4.3 production recognition 기준선

### Detail rectangle proposals

Scanner Lab 3.8 계열 geometry는 상세창을 최종 확정하지 않고 **검증할 rectangle proposal을 생성**합니다.

```text
capture
→ red-X / rectangle-edge proposals
→ broad impossible-shape filtering
→ near-duplicate edge-jitter removal
→ semantic inspect-header validation
→ title ROI
→ OCR
```

현재 규칙:

- structural floor `0.34`
- historical `aspect ≈ 1.3`은 약한 ranking hint만 제공
- tall/large detail window가 aspect prior만으로 제거되지 않음
- 높은 IoU만으로 candidate를 제거하지 않음
- top/bottom/left/right가 실질적으로 다르면 서로 겹쳐도 semantic stage까지 보존
- 사실상 동일한 edge-jitter candidate만 near-duplicate로 제거
- rough red-X proximity는 proposal ranking hint일 뿐 최종 close evidence가 아님
- one-shot 최대 12 candidates
- continuous Scanner 최대 8 candidates

### Inspect-header semantic gate

모든 production OCR은 다음 gate 뒤에만 실행합니다.

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND magnifier evidence present
AND close-X evidence present
```

사용 evidence:

- red close body/edge + diagonal X contrast
- neutral header/frame evidence
- frame-left magnifier/search lane
- magnifier ring/hollow center/handle/background evidence
- dark title field
- title text evidence

Fallback 순서:

```text
ScannerInspectHeaderLock
→ 실패 시 v1.4.1 live Ground Truth refiner
→ 둘 다 실패한 oversized candidate에서 v1.4.2 contained-subpanel proposal
→ 같은 semantic evidence 재검증
→ HEADER_FRAME_LOCKED >= 0.68
```

v1.4.3에서도 이 trusted gate를 낮추지 않았습니다.

## OCR / catalog matching

Primary recognizer는 Windows `ko-KR` OCR입니다.

- primary + deep/high-contrast/binary/inverse variants
- Tarkov-font visual corroboration/recovery
- current official Korean full item catalog가 identity authority
- exact-first + conservative fuzzy + margin
- v1.4.2 reviewed GT 기반 unique 2-edit / long-suffix bounded recovery 유지
- v1.4.3 current-catalog character inventory / unknown-glyph recovery 추가

### Current-catalog character inventory

OCR 결과의 문자가 Unicode letter/digit라는 이유만으로 자동 신뢰하지 않습니다.

- current official item names에 실제 존재하는 문자·기호 집합을 생성
- ASCII letter/digit는 정상 noisy evidence로 유지
- current catalog에서 실제 사용하는 quote/hyphen/bracket 등은 유지
- `Ø` 등 catalog-impossible Unicode glyph는 정상 identity 문자로 신뢰하지 않음
- impossible embedded glyph는 특정 `r`, `0`, `I`, `l` 등으로 전역 치환하지 않고 `?` evidence로 보존
- 1~2 unknown glyph pattern은 catalog 전체에서 유일하고 충분히 분리된 경우에만 복구
- ambiguous pattern은 fail-closed

따라서 v1.4.3은 `r`, `0`, complex Hangul OCR을 일반적으로 해결했다고 간주하지 않습니다. **불가능한 glyph를 걸러내고 closed-domain catalog evidence로 안전한 경우만 복구**하도록 개선한 것입니다.

다음은 그대로 유지합니다.

- generic OCR confidence 하향 없음
- matcher minimum confidence/margin 일반 완화 없음
- global glyph replacement table 없음
- current catalog 밖 Item 생성 금지
- ambiguity / low confidence fail-closed

## Scanner 표시 데이터 의미

Production Scanner가 OCR하는 필드는 **아이템명(`item_name`) 하나**입니다.

Item ID 확정 이후 아래 값을 `mapped_data`로 계산/조회합니다.

- 최고 상점가: flea 제외 유효 판매처 RUB 환산 가격 최댓값
- 플리마켓 평균가: positive `avg24hPrice`
- slots: positive `width × height`
- price/slot: price와 slots가 모두 유효할 때만
- 필요한 개수: 현재 Needed Items의 `RequiredTotal`

가격·플리·슬롯·필요 개수 화면 OCR 필드를 새로 만들지 않습니다. 특정 market/dimension 정보가 누락되면 해당 표시 필드만 비우고 Item identity를 버리지 않습니다.

## Ground Truth 기반 개발 계약

공식 계약: `docs/SCANNER_GROUND_TRUTH.md`

사용자 교정:

- `맞음`
- 상세보기 영역 수정
- 아이템명 영역 수정
- 정답 아이템명 입력
- 영역 + 텍스트 동시 교정

사용자 rectangle과 정답 text가 Ground Truth입니다. 자동 diagnostic Case는 정답으로 취급하지 않습니다.

대표 보존물:

- `full.png`
- `detail_window.png`
- `detected_roi.png`
- `corrected_roi.png`
- `processed_roi.png`
- `annotated.png`
- `case.json`
- raw OCR / normalized / matcher candidates / final decision
- structural/header evidence
- user Ground Truth
- mapped presentation

Export:

- `ScannerDiagnostics_YYYY-MM-DD.zip`
- dataset.jsonl / summary / environment / cases / images / Scanner logs

Full-pipeline regression:

```text
full.png
→ current detail proposals
→ inspect-header / contained-subpanel semantic lock
→ title ROI
→ OCR/deep OCR/font recovery
→ current catalog character policy/matching
→ final Item ID
```

결과:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

기존 정상 Case가 현재 실패하면 평균 정확도가 올라가도 regression으로 취급합니다.

## v1.4.3 검증 상태

```text
feature PR #165: 32660568132 — SUCCESS
release-prep PR #166: 32674399495 — SUCCESS
exact source/tag: f7e3870c81a7d7be025f1fe56d5b7f607546b250
279 tests / 0 failed / 0 skipped
release run: 32674812862 — SUCCESS
independent public verifier: 32675069359 — SUCCESS
public latest/tag: VERIFIED
public ZIP SHA256SUMS/layout: VERIFIED
public-downloaded EXE smoke/graceful shutdown: SUCCESS
durable status: docs/.release-v1.4.3-status.json
```

**v1.4.3 release blocker는 없습니다.**

## 다음 실제 작업

다음 단계는 새로운 추측성 threshold 조정이 아니라 **v1.4.3 실사용 Ground Truth 수집**입니다.

특히 확인할 표본:

- tall/large 상세창
- stash/inventory frame과 크게 겹치는 실제 detail window
- 이전에 correct rectangle이 높은 IoU 때문에 후보에서 사라지던 유형
- `r`, `0`, slash-zero-like glyph 및 이상한 Unicode symbol 오인식
- 복잡한 한글 glyph 오인식
- 정상 punctuation이 포함된 공식 item name
- near-name/ambiguous item에서 false positive가 생기지 않는지
- Item ID가 맞을 때 trader/flea/slots/RequiredTotal mapped_data가 정확한지
- 빠른 연속 사용에서 stale-result isolation
- 장시간 CPU/memory/UI responsiveness

권장 실사용 흐름:

```text
v1.4.3 실제 Tarkov 사용
→ 정상 결과 대표 표본 `맞음`
→ 미인식/오인식 직후 `교정`
→ reviewed Ground Truth 축적
→ diagnostics ZIP export
→ summary / OCR confusion / ROI delta / candidate 분석
→ 실제 실패 stage 특정
→ 해당 stage만 수정
→ 전체 reviewed dataset regression
→ 기존 정상 REGRESSION=0 확인
```

## 의도적으로 보류한 작업

**Scanner 속도 최적화는 아직 보류합니다.**

정확도/안정성이 더 고정된 뒤 다음 비용을 실제 측정해 별도 최적화합니다.

- capture 비용
- rectangle candidate budget
- semantic header validation 반복
- OCR/deep OCR 반복
- Tarkov-font visual path
- catalog-wide recovery 비용

정확도 문제와 성능 문제를 동시에 변경하지 않습니다.

## 알려진 잔여 과제

- 일부 historical case에서 header/title은 맞아도 structural bottom 보존 때문에 detail bottom이 실제보다 낮게 남을 수 있음
- `TITLE_ANCHOR_INCOMPLETE` diagnostic stage classification이 일부 경우 잘못 분류될 수 있음
- 추가 해상도/DPI/UI 배치 live validation 필요
- `r`, `0`, complex Hangul OCR engine 자체는 일반적으로 해결되지 않음
- exact OCR-consumed processed bitmap 진단 구조는 추가 개선 여지 있음
- rendered sample dictionary는 reviewed Ground Truth가 쌓인 뒤 확장
- 추가 Ground Truth 없이 generic matcher/header threshold를 완화하지 않음
